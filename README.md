# Gestión Celulares

Backend del ERP/POS/Taller/Créditos/Inventario IMEI para comercios de celulares.
Stack: **ASP.NET Core 8 (Web API) + EF Core + SQL Server**.
Enfoque **Database-First**: la base de datos `GestionCelulares` (SQL Server) es la fuente de
verdad; las entidades y el `DbContext` la reflejan.

## Arquitectura (Clean Architecture)
Toda la solución vive bajo esta carpeta, con la regla de dependencias hacia adentro:

```
GestionCelulares/                         (raíz de la solución)
├─ GestionCelulares.sln
├─ GestionCelulares.Domain/               Entidades POCO, sin dependencias externas
├─ GestionCelulares.Application/          Casos de uso + contratos
│   ├─ Common/Interfaces  (IApplicationDbContext, ITokenService, IPasswordHasher)
│   ├─ Common/Exceptions
│   ├─ Auth                (DTOs, IAuthService, AuthService)
│   ├─ Inventario          (DTOs, read model, IInventarioService, InventarioService)
│   └─ DependencyInjection (AddApplication)
├─ GestionCelulares.Infrastructure/       Detalles técnicos
│   ├─ Persistence/GestionCelularesContext   (EF Core, implementa IApplicationDbContext)
│   ├─ Identity/TokenService (JWT) + BCryptPasswordHasher
│   ├─ Settings/JwtSettings
│   └─ DependencyInjection (AddInfrastructure)
└─ GestionCelulares.Api/                   Composition root (controladores + Program.cs)
```

Dependencias:  `Domain ← Application ← Infrastructure`  y  `Api → Application + Infrastructure`.

## Requisitos
- .NET 8 SDK
- SQL Server 2019+ con la base `GestionCelulares` ya creada (ver [Base de datos](#base-de-datos)).

## Configuración
Edita `GestionCelulares.Api/appsettings.json`:

1. **Cadena de conexión** (`ConnectionStrings:Default`)
   - Autenticación Windows: `Server=localhost;Database=GestionCelulares;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False`
   - Autenticación SQL: `Server=localhost;Database=GestionCelulares;User Id=sa;Password=TU_PASS;TrustServerCertificate=True;Encrypt=False`
   - SQL Server Express: usa `Server=.\SQLEXPRESS;...`
2. **Clave JWT** (`Jwt:Key`): reemplázala por una cadena secreta de 32+ caracteres.

## Compilar y ejecutar
Desde esta carpeta (raíz de la solución):

```bash
dotnet build
dotnet run --project GestionCelulares.Api
```
Abre Swagger en `https://localhost:<puerto>/swagger` (el puerto está en
`GestionCelulares.Api/Properties/launchSettings.json`).

> Si usas Visual Studio, abre `GestionCelulares.sln` desde esta carpeta.

## Primer login
En **desarrollo**, al arrancar se inicializa la contraseña del usuario `admin` a `Admin123*`
(solo si en la BD quedó el placeholder `CAMBIAR_HASH`). Cámbiala cuanto antes.

```
POST /api/auth/login
{ "nombreUsuario": "admin", "contrasena": "Admin123*" }
```
Pega el `accessToken` en el botón **Authorize** de Swagger para llamar a los endpoints protegidos.

## Endpoints
- `POST /api/auth/login` — inicia sesión (JWT + refresh).
- `POST /api/auth/refresh` — renueva el token.
- `GET  /api/inventario/disponibles?sucursalId=` — stock disponible (vista `vw_InventarioDisponible`).
- `GET  /api/inventario/imei/{imei}` — consulta por IMEI.
- `GET  /api/inventario/{id}` — consulta por Id.
- `POST /api/inventario` — registra un IMEI (genera movimiento de entrada / kardex).
- `POST /api/inventario/{id}/transferir` — transfiere a otra sucursal.

## Base de datos
Para un servidor nuevo, ejecuta un único script:
- `GestionCelulares_setup_completo.sql` — instalación inicial completa (esquema,
  tipos, vistas, índices, foreign keys, procedimientos y el login/usuario
  `gc_app` con privilegios mínimos). Es idempotente frente al login/usuario y
  se autoprotege: si la base `GestionCelulares` ya existe, aborta sin tocar
  nada. Instrucciones de uso (SSMS o `sqlcmd`) dentro del propio archivo.
  Reemplaza a la cadena antigua de scripts `v1..v4` (ya no se conservan; este
  archivo es un volcado completo generado desde SSMS que incluye todo lo que
  esos scripts y los incrementales `v5..v16` fueron aplicando con el tiempo).

Los scripts `GestionCelulares_v5..v16*.sql` de esta carpeta son el historial
de cambios incrementales que ya quedaron incorporados en `setup_completo`;
solo hace falta volver a ellos como referencia de *qué* cambió y *por qué* en
cada paso, no para instalar desde cero.

## Regenerar entidades desde la BD (scaffold real, opcional)
Las entidades y el `DbContext` se escribieron a mano reflejando la BD. Si quieres regenerarlos
contra tu servidor (ojo: el scaffold los pondría juntos; aquí el `DbContext` vive en
Infrastructure y las entidades en Domain, así que tras un scaffold habría que reubicarlos):
```bash
cd GestionCelulares.Infrastructure
dotnet ef dbcontext scaffold "Server=localhost;Database=GestionCelulares;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False" Microsoft.EntityFrameworkCore.SqlServer -o Persistence/Scaffolded -c GestionCelularesContext --no-onconfiguring --force
```
