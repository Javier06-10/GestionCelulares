<#
.SYNOPSIS
    Aplica los scripts SQL de GestionCelulares en orden y una sola vez (migraciones versionadas).

.DESCRIPTION
    Reemplaza el "aplicar scripts a mano con sqlcmd" por un proceso repetible y seguro:
      - Crea (si falta) la tabla dbo.SchemaVersion que registra qué scripts ya corrieron.
      - Aplica en ORDEN NUMÉRICO (setup_completo primero, luego v5..vN) solo los que faltan.
      - Cada script se ejecuta con QUOTED_IDENTIFIER ON (-I, requerido por índices filtrados)
        y abortando ante error (-b). Tras aplicarlo con éxito, lo registra en SchemaVersion.
      - Es idempotente: correrlo de nuevo no repite nada. Todos los scripts, además, son
        idempotentes por sí mismos (guardas IF [NOT] EXISTS o CREATE OR ALTER).

    Requisitos: sqlcmd en el PATH (viene con SQL Server / herramientas de línea de comandos).

.PARAMETER Server
    Instancia SQL Server. Por defecto: localhost.

.PARAMETER Database
    Base de datos destino. Por defecto: GestionCelulares. (Los scripts hacen USE [GestionCelulares].)

.PARAMETER Path
    Carpeta con las migraciones .sql. Por defecto: db\migraciones (junto a este script).

.PARAMETER Baseline
    Marca TODOS los scripts como aplicados SIN ejecutarlos. Úsalo una sola vez para adoptar
    esta herramienta sobre una base que YA tiene el esquema (p. ej. la BD de desarrollo actual).

.PARAMETER WhatIf
    Muestra qué scripts se aplicarían, sin ejecutarlos.

.EXAMPLE
    # BD nueva (o CI): crea el esquema completo desde cero
    ./Apply-Migrations.ps1 -Server localhost -Database GestionCelulares

.EXAMPLE
    # Adoptar la herramienta sobre la BD de desarrollo existente (no re-ejecuta nada)
    ./Apply-Migrations.ps1 -Baseline

.EXAMPLE
    # Ver qué falta sin aplicar
    ./Apply-Migrations.ps1 -WhatIf
#>
[CmdletBinding()]
param(
    [string]$Server = "localhost",
    [string]$Database = "GestionCelulares",
    [string]$Path = "",
    [switch]$Baseline,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

# Carpeta de migraciones por defecto: db\migraciones junto a este script.
if ([string]::IsNullOrWhiteSpace($Path)) { $Path = Join-Path $PSScriptRoot 'db\migraciones' }

function Invoke-Sql {
    param([string]$Query, [switch]$Master)
    $db = if ($Master) { "master" } else { $Database }
    # -b: aborta con exit code != 0 ante error de SQL.  -I: QUOTED_IDENTIFIER ON.
    $out = & sqlcmd -S $Server -d $db -b -I -h -1 -W -Q $Query 2>&1
    if ($LASTEXITCODE -ne 0) { throw "SQL falló ($db): $out" }
    return $out
}

# 1) Descubre y ordena los scripts (setup_completo = versión 0; luego vN por número).
$scripts =
    Get-ChildItem -Path $Path -Filter "GestionCelulares_*.sql" |
    ForEach-Object {
        $ver = if ($_.Name -match "_v(\d+)_") { [int]$Matches[1] }
               elseif ($_.Name -like "*setup_completo*") { 0 }
               else { $null }
        if ($null -ne $ver) { [pscustomobject]@{ Version = $ver; Name = $_.Name; File = $_.FullName } }
    } |
    Sort-Object Version

if (-not $scripts) { Write-Host "No se encontraron scripts en $Path" -ForegroundColor Yellow; return }

Write-Host "Servidor: $Server   Base: $Database   Scripts: $($scripts.Count)" -ForegroundColor Cyan

# 2) Asegura la BD y la tabla de control.
Invoke-Sql -Master -Query "IF DB_ID(N'$Database') IS NULL CREATE DATABASE [$Database];" | Out-Null
Invoke-Sql -Query @"
IF OBJECT_ID('dbo.SchemaVersion') IS NULL
    CREATE TABLE dbo.SchemaVersion(
        Script       NVARCHAR(200) NOT NULL PRIMARY KEY,
        Version      INT           NOT NULL,
        AppliedAtUtc DATETIME2     NOT NULL CONSTRAINT DF_SchemaVersion_AppliedAt DEFAULT SYSUTCDATETIME()
    );
"@ | Out-Null

# 3) Scripts ya aplicados.
$aplicados = @{}
(Invoke-Sql -Query "SET NOCOUNT ON; SELECT Script FROM dbo.SchemaVersion;") |
    ForEach-Object { $_.Trim() } | Where-Object { $_ } | ForEach-Object { $aplicados[$_] = $true }

$pendientes = $scripts | Where-Object { -not $aplicados.ContainsKey($_.Name) }
if (-not $pendientes) { Write-Host "Todo al dia: nada que aplicar." -ForegroundColor Green; return }

# 4) Aplica (o marca baseline).
foreach ($s in $pendientes) {
    if ($WhatIf) { Write-Host "[WhatIf] aplicaria  v$($s.Version)  $($s.Name)" -ForegroundColor DarkGray; continue }

    if ($Baseline) {
        Write-Host "baseline  v$($s.Version)  $($s.Name)" -ForegroundColor Yellow
    } else {
        Write-Host "aplicando v$($s.Version)  $($s.Name) ..." -ForegroundColor White
        $out = & sqlcmd -S $Server -d $Database -b -I -i $s.File 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Error aplicando $($s.Name):`n$out" }
    }

    $script = $s.Name.Replace("'", "''")
    Invoke-Sql -Query "INSERT INTO dbo.SchemaVersion(Script, Version) VALUES (N'$script', $($s.Version));" | Out-Null
}

if (-not $WhatIf) {
    $modo = if ($Baseline) { "marcados como baseline" } else { "aplicados" }
    Write-Host "Listo: $($pendientes.Count) script(s) $modo." -ForegroundColor Green
}
