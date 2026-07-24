# Guía de despliegue a producción

Checklist y configuración para llevar GestiónCelulares a un servidor real. La ingeniería de la
app ya soporta producción (JWT validado, rate limiting, HSTS/HTTPS, headers de seguridad); lo que
sigue es **plomería de despliegue**: secretos por entorno, la base de datos y los datos iniciales.

> Los secretos **nunca** van en el repo. En producción se inyectan por **variables de entorno**.
> El repo solo trae plantillas sin secretos (`appsettings.json`, `appsettings.Production.json`).

---

## 1. Variables de entorno (backend)

.NET mapea variables de entorno a la configuración usando `__` (doble guion bajo) como separador
de sección. Estas son **obligatorias** en producción:

| Variable | Obligatoria | Ejemplo | Notas |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | ✅ | `Production` | Activa HTTPS/HSTS y **desactiva** Swagger y la siembra de dev. Sin esto la API corre como desarrollo. |
| `Jwt__Key` | ✅ (secreto) | *(32+ chars aleatorios)* | La API **no arranca** si falta, es corta (<32) o contiene "CAMBIA". Ver cómo generarla abajo. |
| `ConnectionStrings__Default` | ✅ (secreto) | `Server=SQLPROD;Database=GestionCelulares;User Id=gc_app;Password=***;TrustServerCertificate=True;Encrypt=True` | Usar el login **`gc_app`** (mínimo privilegio), no `sa`. |
| `Cors__Origins__0` | ⚠️ | `https://tienda.tudominio.com` | Solo si el frontend se sirve en **otro origen** que la API. Si van en el mismo dominio (recomendado, ver §3), CORS no se activa. |
| `Jwt__Issuer` / `Jwt__Audience` | opcional | `GestionCelulares` | Tienen default; cámbialos si quieres. |
| `Jwt__AccessTokenMinutes` | opcional | `60` | Vida del access token. |
| `Jwt__RefreshTokenDays` | opcional | `7` | Vida del refresh token. |
| `Mora__Habilitado` / `Mora__HoraDiaria` | opcional | `true` / `01:00` | Job diario de mora. |

**Generar una `Jwt__Key` fuerte (PowerShell):**
```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Max 256 }) -as [byte[]])
```

**Fijar variables en Windows / IIS:** en el sitio de IIS → *Configuration Editor* →
`system.webServer/aspNetCore` → `environmentVariables`, o en `web.config`. En línea de comandos
(persistente por máquina): `setx ASPNETCORE_ENVIRONMENT Production /M` (reiniciar el proceso).

**En Linux (systemd):** `Environment=ASPNETCORE_ENVIRONMENT=Production` en el `.service`.

**En Docker:** `-e ASPNETCORE_ENVIRONMENT=Production -e Jwt__Key=... -e ConnectionStrings__Default=...`

---

## 2. Base de datos

1. **Login de aplicación:** el script `v7` crea el login **`gc_app`** con solo lectura/escritura y
   `EXECUTE` (sin `sysadmin`/DDL). Trae una **contraseña placeholder** `CAMBIA_ESTA_CLAVE_Fuerte#2026`
   — **cámbiala** antes de producción:
   ```sql
   ALTER LOGIN [gc_app] WITH PASSWORD = N'<clave-fuerte-nueva>';
   ```
   y usa esa clave en `ConnectionStrings__Default`.

2. **Esquema:** aplica las migraciones con el runner (ver [MIGRACIONES.md](MIGRACIONES.md)):
   ```powershell
   ./Apply-Migrations.ps1 -Server <host> -Database GestionCelulares
   ```

3. **Datos iniciales:** ver §4 — una BD nueva queda **vacía de datos de referencia**.

---

## 3. Frontend

```bash
cd gestioncelulares-web
npm ci
npx ng build --configuration production   # usa environment.production.ts (apiUrl '/api')
```
El build sale en `dist/gestioncelulares-web/`. Sírvelo como estático detrás de un **reverse proxy**
(IIS/nginx) que enrute:
- `/`      → los archivos estáticos del `dist` (con fallback a `index.html` para el router de Angular).
- `/api`   → la API .NET (Kestrel).

Así frontend y API comparten origen, `apiUrl: '/api'` funciona y **no hace falta CORS**. Si prefieres
hosts separados, pon el dominio del front en `Cors__Origins__0` y cambia `apiUrl` en
`environment.production.ts` por la URL absoluta de la API.

---

## 4. Datos iniciales (⚠️ paso obligatorio para una BD nueva)

**Los scripts de esquema NO siembran datos de referencia.** Una base recién creada no tiene Empresa,
Sucursal, Roles, Métodos de pago ni usuario admin — la app **no funcionaría ni habría con quién
entrar**. Ejecuta este bootstrap **una vez** (ajusta los valores de tu negocio). Es idempotente:

```sql
USE [GestionCelulares];

-- Empresa (RD): RNC y % de ITBIS reales de tu negocio
IF NOT EXISTS (SELECT 1 FROM dbo.Empresa)
    INSERT INTO dbo.Empresa (Nombre, RNC, Moneda, PorcentajeItbis, FechaCreacion)
    VALUES (N'MI TIENDA SRL', N'000000000', N'DOP', 18, SYSDATETIME());

-- Sucursal principal
IF NOT EXISTS (SELECT 1 FROM dbo.Sucursal)
    INSERT INTO dbo.Sucursal (Nombre, Activa, FechaCreacion)
    VALUES (N'Principal', 1, SYSDATETIME());

-- Roles que la app espera por nombre
INSERT INTO dbo.Rol (Nombre)
SELECT v FROM (VALUES (N'Admin'), (N'Vendedor'), (N'Tecnico')) x(v)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Rol r WHERE r.Nombre = x.v);

-- Métodos de pago ('Efectivo' y 'Apartado' son requeridos por el código)
INSERT INTO dbo.MetodoPago (Nombre)
SELECT v FROM (VALUES (N'Efectivo'), (N'Tarjeta'), (N'Transferencia'), (N'Apartado')) x(v)
WHERE NOT EXISTS (SELECT 1 FROM dbo.MetodoPago m WHERE m.Nombre = x.v);

-- Usuario admin inicial (la contraseña se fija aparte, ver abajo)
IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE NombreUsuario = N'admin')
    INSERT INTO dbo.Usuario (NombreUsuario, NombreCompleto, HashContrasena, RolId, SucursalId, Activo)
    SELECT N'admin', N'Administrador', N'CAMBIAR_HASH',
           (SELECT RolId FROM dbo.Rol WHERE Nombre = N'Admin'),
           (SELECT TOP 1 SucursalId FROM dbo.Sucursal ORDER BY SucursalId), 1;
```

> Verifica los nombres de columna contra tu esquema final antes de correrlo (la tabla `Usuario`
> ganó columnas de bloqueo en `v17`; deben tener default o admitir NULL).

### Contraseña del admin en producción
La siembra automática de contraseña (`SeedDevPasswordAsync`) **solo corre en `Development`**. En
producción el admin queda con `HashContrasena = 'CAMBIAR_HASH'` y **no puede entrar** hasta fijar una
contraseña real (hash BCrypt). Hoy **no hay un mecanismo de producción** para esto.

**Recomendado (pendiente, ver nota):** añadir un *seeder* de producción que cree/active el admin desde
una variable de entorno (p. ej. `Seed__AdminPassword`) cuando no haya usuarios. Es un cambio pequeño
de arranque; puedo implementarlo como paso siguiente (B4).

**Interino sin cambio de código:** generar un hash BCrypt con la misma librería que usa la app
(`BCrypt.Net-Next`) y hacer `UPDATE dbo.Usuario SET HashContrasena = N'<hash>' WHERE NombreUsuario = N'admin';`.

---

## 5. Checklist de go-live

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `Jwt__Key` fuerte (32+), por variable de entorno
- [ ] `ConnectionStrings__Default` con `gc_app` y su **contraseña cambiada**
- [ ] Migraciones aplicadas (`Apply-Migrations.ps1`)
- [ ] Bootstrap de datos iniciales (§4) ejecutado
- [ ] Contraseña real del admin fijada
- [ ] Frontend `ng build --configuration production` desplegado tras reverse proxy con `/api`
- [ ] `Cors__Origins__0` puesto **solo si** front y API van en orígenes distintos
- [ ] TLS/HTTPS terminado en el proxy o Kestrel; verificar HSTS
- [ ] (Recomendado) `AllowedHosts` fijado al dominio real en `appsettings.Production.json`

## Pendientes conocidos (no bloquean el arranque, sí conviene antes de operar)
- **Health check** `/health` para monitoreo/orquestador (alta prioridad H4).
- **Refresh de token** en el frontend: hoy expulsa al usuario a los 60 min (H3).
- **Seeder de admin de producción** (§4) — recomendado para no depender del hash manual.
- **e-CF (Ley 32-23)** — facturación electrónica DGII pendiente (H1).
