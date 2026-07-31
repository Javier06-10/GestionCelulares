/* =============================================================================
   v7) Login de la aplicación con privilegios mínimos
       Objetivo: que la API NO se conecte como sysadmin. Este usuario puede
       leer/escribir datos y ejecutar los procedimientos, pero NO puede hacer
       DDL (crear/alterar/borrar objetos), administrar logins, ni acceder a
       otras bases de datos. Reduce el radio de impacto ante una SQLi.

       IMPORTANTE: cambia la contraseña de ejemplo por una fuerte y guárdala
       fuera del repositorio (user-secrets / variable de entorno).
   ============================================================================= */
USE [master];
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'gc_app')
    CREATE LOGIN [gc_app] WITH PASSWORD = 'CAMBIA_ESTA_CLAVE_Fuerte#2026', CHECK_POLICY = ON;
GO

USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'gc_app')
    CREATE USER [gc_app] FOR LOGIN [gc_app];
GO

-- Lectura/escritura de datos (la app usa EF Core para la mayoría del CRUD)
ALTER ROLE db_datareader ADD MEMBER [gc_app];
ALTER ROLE db_datawriter ADD MEMBER [gc_app];

-- Ejecutar los procedimientos almacenados (ventas, caja, créditos…)
GRANT EXECUTE ON SCHEMA::dbo TO [gc_app];

-- Necesario para pasar el TVP a usp_Venta_Registrar
GRANT EXECUTE ON TYPE::dbo.VentaDetalleTipo TO [gc_app];

-- NO se otorga db_owner / sysadmin / ALTER: el usuario no puede modificar el
-- esquema ni saltarse la estructura de la base.
GO

-- Verificación rápida (ejecutar conectado como gc_app):
--   SELECT IS_SRVROLEMEMBER('sysadmin');  -- debe dar 0
--   SELECT IS_MEMBER('db_owner');         -- debe dar 0
