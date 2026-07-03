# Configuración de secretos (por máquina)

Los secretos **no van al repositorio**. La API (`GestionCelulares.Api`) los lee de
**user-secrets** en desarrollo (o de variables de entorno en producción). El
`appsettings.json` solo trae un placeholder de `Jwt:Key`, y **la API se niega a
arrancar** si la clave sigue siendo el placeholder o mide menos de 32 caracteres.

> ⚠️ Al copiar el proyecto a **otra PC**, los user-secrets NO se copian. Si no
> configuras la clave, la API lanzará "Jwt:Key inválida" y no iniciará (y el
> frontend "no conectará"). Configúrala una vez por máquina.

## 1) Clave JWT (obligatoria)

Genera una clave larga y aleatoria y guárdala en user-secrets:

```powershell
# PowerShell, desde la carpeta del repo
$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($bytes)) --project GestionCelulares.Api
```

## 2) Cadena de conexión (recomendado: login de mínimos `gc_app`)

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Database=GestionCelulares;User Id=gc_app;Password=<LA_CLAVE_DE_gc_app>;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" --project GestionCelulares.Api
```

Si prefieres autenticación de Windows en tu equipo, el `appsettings.json` ya trae
`Trusted_Connection=True` por defecto; no configures esta clave y usará esa.

## Verificar

```powershell
dotnet user-secrets list --project GestionCelulares.Api
```

## En producción

Usar variables de entorno (no user-secrets): `Jwt__Key`, `ConnectionStrings__Default`.
