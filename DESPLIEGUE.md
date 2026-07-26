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
| `Seed__AdminPassword` | ⚠️ (secreto) | *(clave del primer admin)* | Solo para el **primer arranque**: crea/activa el usuario `admin` con esa contraseña. Ver §4. Quítala después. |
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

### Imágenes de producto (Cloudinary, opcional)
Las fotos de producto se suben **directo del navegador a Cloudinary** (unsigned upload) y la BD solo
guarda el URL. Para habilitarlo:
1. Crea una cuenta gratis en [cloudinary.com](https://cloudinary.com).
2. Settings → Upload → **Add upload preset** con *Signing Mode = Unsigned* (opcional: fija una carpeta y
   formatos permitidos para acotar abusos).
3. Pon el **Cloud name** y el **nombre del preset** en `environment.cloudinary` (`environment.ts` y
   `environment.production.ts`). Son valores **públicos**, no secretos.

Si se dejan vacíos, la app funciona igual pero el botón de subir imagen queda deshabilitado. Para cambiar
de proveedor (Supabase Storage, etc.) solo se reescribe `imagen.service.ts`.

### Autenticación por cookies (importante)
La sesión viaja en cookies **HttpOnly + Secure + SameSite=Strict** (`gc_access`, `gc_refresh`), no en
`localStorage`. Esto impone dos condiciones de despliegue:
- **HTTPS obligatorio** (las cookies son `Secure`). En dev funciona sobre `http://localhost` porque el
  navegador trata localhost como contexto seguro.
- **Frontend y API deben estar en el mismo *site*** (mismo dominio registrable) por `SameSite=Strict`.
  El montaje recomendado (mismo origen, `/api` por reverse proxy) lo cumple. Si el front vive en un
  **dominio distinto** al de la API, `SameSite=Strict` **no enviará las cookies** y el login no
  funcionará; en ese caso hay que reconsiderar el `SameSite` (Lax) y añadir orígenes a `Cors__Origins`
  con credenciales. La API ya envía `Access-Control-Allow-Credentials`; el front usa `withCredentials`.
- CSRF: mitigado por `SameSite=Strict` (las cookies no se envían en peticiones cross-site).

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
El *seeder* de arranque (`SeedAdminAsync`) deja el `admin` con una contraseña utilizable en **cualquier
entorno**:
- Define **`Seed__AdminPassword`** con la clave deseada y arranca la API **una vez**. El seeder:
  - crea el usuario `admin` (rol Admin, sucursal principal) si no existe, **o**
  - le fija la contraseña si quedó con el placeholder `CAMBIAR_HASH`.
- **Quita `Seed__AdminPassword`** del entorno tras ese primer arranque (ya no hace falta; el seeder no
  pisa una contraseña ya establecida).

Sin `Seed__AdminPassword`, en producción el seeder **no toca nada** (no hay contraseñas por defecto en
prod). El bootstrap SQL de arriba y este seeder se complementan: con solo definir la variable y arrancar,
tienes un admin funcional aunque no hayas insertado la fila `admin` a mano.

---

## 5. Monitoreo (health checks)

La API expone endpoints **anónimos** de salud para balanceadores, orquestadores y uptime monitors:

| Endpoint | Qué verifica | Uso |
|---|---|---|
| `GET /health/live` | El proceso responde (no toca dependencias). | *Liveness* — reinicio si no responde. |
| `GET /health/ready` | La base de datos es accesible. | *Readiness* — no enrutar tráfico si falla. |
| `GET /health` | Resumen **JSON** de todos los checks. | Diagnóstico / dashboards. |

Responden `200` si sano y `503` si no. Ejemplo de `/health`:
```json
{ "status": "Healthy", "durationMs": 12.3, "checks": [ { "name": "database", "status": "Healthy", "description": "Base de datos accesible." } ] }
```

### Logs (Serilog)
La API registra con **Serilog**: a consola y a **archivo rotativo diario** en `logs/gc-YYYYMMDD.log`
(relativo al directorio de la app), con **retención de 30 días**. Incluye una línea estructurada por
request (método, ruta, estado, tiempo) vía `UseSerilogRequestLogging`.
- El proceso necesita **permiso de escritura** sobre la carpeta `logs/` (en IIS, la identidad del
  Application Pool).
- Los niveles se ajustan sin recompilar en la sección `Serilog` de `appsettings.{Entorno}.json`
  (`MinimumLevel` / `Override`). Para menos ruido en prod, sube `Default` a `Warning`.

## Respaldos de la base de datos (obligatorio para operar)

Toda la operación (ventas, créditos, inventario, contabilidad) vive en SQL Server. Sin respaldos, un
fallo de disco borra el negocio. Mínimo recomendado:

- **Modelo de recuperación `FULL`** para poder restaurar a un punto en el tiempo (pérdida mínima):
  ```sql
  ALTER DATABASE GestionCelulares SET RECOVERY FULL;
  ```
  (Si aceptas perder hasta un día, `SIMPLE` + un full diario también sirve, pero para un POS con dinero
  se recomienda `FULL`.)
- **Frecuencia:** `FULL` diario + `LOG` cada 15–30 min (con modelo FULL) para minimizar la pérdida.
  ```sql
  BACKUP DATABASE GestionCelulares TO DISK = N'D:\Backups\GC_full.bak' WITH INIT, COMPRESSION;
  BACKUP LOG      GestionCelulares TO DISK = N'D:\Backups\GC_log.trn'  WITH INIT;
  ```
- **Automatización:** con SQL Server *Express* (común en tiendas) **no hay SQL Agent**, así que se
  programa con el **Programador de tareas de Windows** ejecutando `sqlcmd -Q "BACKUP ..."`.
- **Fuera del servidor de BD:** copia los `.bak` a **otro disco/máquina/nube**. Un respaldo en el mismo
  disco que la BD no protege de un fallo de disco.
- **Retención:** p. ej. 30 días en línea + una copia mensual archivada más tiempo.
- **Restore probado:** un respaldo que nunca restauraste **no es un respaldo**. Prueba la restauración
  en otra instancia cada cierto tiempo:
  ```sql
  RESTORE DATABASE GestionCelulares_test FROM DISK = N'D:\Backups\GC_full.bak' WITH MOVE ..., REPLACE;
  ```
- Los respaldos corren bajo una cuenta con privilegios (sysadmin/db_backupoperator), **no** el login
  `gc_app` de la app (que es de mínimo privilegio).

## Docker (opcional)

La API se empaqueta con el [`Dockerfile`](Dockerfile) multi-stage (SDK para compilar,
`aspnet:8.0` para runtime, usuario no-root). Los secretos van por variables de entorno, no en
la imagen.

```bash
# Construir
docker build -t gestioncelulares-api .

# Ejecutar (mapea 8080 y pasa la configuración por -e)
docker run -d -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Jwt__Key="<clave-32+>" \
  -e ConnectionStrings__Default="Server=SQLPROD;Database=GestionCelulares;User Id=gc_app;Password=***;TrustServerCertificate=True;Encrypt=True" \
  -e Cors__Origins__0="https://tienda.tudominio.com" \
  --name gc-api gestioncelulares-api
```

Notas:
- La imagen escucha en **8080** (default de .NET 8). Pon un reverse proxy delante que termine TLS y
  enrute `/api` (y sirva el frontend en el mismo origen; ver §3 y la restricción SameSite).
- Los **logs** de Serilog quedan dentro del contenedor en `/app/logs`; móntalos en un volumen si los
  quieres persistir (`-v gc-logs:/app/logs`).
- La migración de esquema (`Apply-Migrations.ps1`) y el bootstrap de datos (§4) se ejecutan **aparte**
  contra la BD; la imagen no corre migraciones al arrancar.

## 6. Checklist de go-live

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
- [ ] Monitoreo apuntando a `/health/ready` (readiness) y `/health/live` (liveness)
- [ ] **Respaldos** de la BD automatizados, fuera del servidor y con **restore probado**

## Pendientes conocidos (no bloquean el arranque, sí conviene antes de operar)
- **e-CF (Ley 32-23)** — facturación electrónica DGII pendiente (H1). Es una obligación **fiscal del
  negocio**, no técnica: define si aplica antes del go-live.
