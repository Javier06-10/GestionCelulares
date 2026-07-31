# Base de datos — GestionCelulares

Dos formas de crear/actualizar la base, según el caso:

## 1) Instalación NUEVA (desde cero) — un solo script

Usa **`GestionCelulares_Esquema_Completo.sql`**: crea toda la base en un paso
(tipos, tablas con PK/UNIQUE/CHECK/DEFAULT/índices/triggers, llaves foráneas,
vistas, procedimientos y las semillas de configuración).

```bash
sqlcmd -S localhost -i db/GestionCelulares_Esquema_Completo.sql
```

- Es **idempotente**: tablas con `IF NOT EXISTS`, programables con `CREATE OR ALTER`,
  semillas con guardas. Correrlo dos veces no rompe nada ni duplica datos.
- Crea la BD `GestionCelulares` si no existe.
- Fue **generado desde el esquema real** (fuente de verdad) y verificado: produce
  exactamente los mismos objetos que aplicar `setup_completo` + `v5..v26`.

> Este script **no** siembra datos operativos (usuarios, roles, métodos de pago,
> sucursal). Ese bootstrap inicial va documentado en `DESPLIEGUE.md` (§4).

## 2) Base EXISTENTE (o ir agregando cambios) — migraciones versionadas

Las migraciones históricas viven en **`migraciones/`** y las aplica
**`Apply-Migrations.ps1`** (en la raíz del repo), que lleva el control en
`dbo.SchemaVersion` y solo corre lo que falta.

```powershell
# Aplica lo pendiente (crea la BD si no existe)
./Apply-Migrations.ps1 -Server localhost -Database GestionCelulares

# Adoptar la herramienta sobre una BD que YA tiene el esquema (no re-ejecuta nada)
./Apply-Migrations.ps1 -Baseline

# Ver qué falta sin aplicar
./Apply-Migrations.ps1 -WhatIf
```

## ¿Cuál uso?

| Situación | Qué correr |
|-----------|------------|
| Servidor nuevo / entorno limpio / CI | `GestionCelulares_Esquema_Completo.sql` (opción 1) |
| BD en producción/desarrollo ya existente | `Apply-Migrations.ps1` (opción 2) |
| Nuevo cambio de esquema a futuro | Agrega `migraciones/GestionCelulares_vN_*.sql` y corre `Apply-Migrations.ps1` |

## Regenerar el consolidado

Si el esquema cambia y quieres refrescar el consolidado, se regenera scriptando
la BD real con SMO (Windows PowerShell) y reensamblando con las semillas. El
procedimiento quedó documentado en el historial del PR que lo introdujo.

## Estructura

```
db/
  GestionCelulares_Esquema_Completo.sql   ← instalación nueva (1 script)
  migraciones/                            ← historial versionado (setup_completo + v5..v26)
  README.md
Apply-Migrations.ps1                      ← runner de migraciones (raíz)
```
