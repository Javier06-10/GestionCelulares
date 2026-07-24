# Migraciones de base de datos

El esquema de GestiónCelulares se versiona con **scripts SQL numerados** (`GestionCelulares_v5..vN.sql`, con `GestionCelulares_setup_completo.sql` como base) que se aplican con el runner **[`Apply-Migrations.ps1`](Apply-Migrations.ps1)**.

El runner reemplaza el "aplicar scripts a mano con sqlcmd": lleva una tabla de control `dbo.SchemaVersion`, aplica en **orden numérico** solo lo que falta y es **idempotente** (correrlo dos veces no repite nada). Cada script, además, es idempotente por sí mismo (guardas `IF [NOT] EXISTS` o `CREATE OR ALTER`).

## Requisitos
- `sqlcmd` en el PATH (viene con SQL Server / *SQL Server Command Line Utilities*).

## Casos de uso

**Instalar en una BD nueva (producción / staging / CI):**
```powershell
./Apply-Migrations.ps1 -Server <host> -Database GestionCelulares
```
Crea la BD si no existe y aplica `setup_completo` + `v5..vN` en orden.

**Adoptar el runner en una BD que YA tiene el esquema (una sola vez):**
```powershell
./Apply-Migrations.ps1 -Baseline
```
Registra todos los scripts como aplicados **sin ejecutarlos**. (Ya se hizo en la BD de desarrollo local.)

**Ver qué falta sin aplicar:**
```powershell
./Apply-Migrations.ps1 -WhatIf
```

## Flujo al agregar un cambio de esquema
1. Crea `GestionCelulares_v{N+1}_descripcion.sql` (con guardas idempotentes o `CREATE OR ALTER`).
2. Corre `./Apply-Migrations.ps1` en local para aplicarlo.
3. Versiónalo. En cada entorno, correr el runner lo aplica una sola vez.

## Limitación conocida
Los scripts contienen `USE [GestionCelulares]` **hardcodeado**, por lo que el nombre de la base
debe ser `GestionCelulares`. El parámetro `-Database` sirve para crear/controlar esa BD, pero
para desplegar bajo otro nombre habría que parametrizar el `USE` en los 21 scripts (pendiente si
alguna vez se necesita multi-tenant o un staging con nombre distinto).

## Nota
No se usan *EF Core Migrations*: el esquema incluye vistas y procedimientos almacenados
(`usp_Venta_Registrar`, `usp_Creditos_ActualizarMora`, etc.) que se mantienen mejor como SQL
explícito. El runner da el control de versión y la aplicación una-sola-vez que faltaba.
