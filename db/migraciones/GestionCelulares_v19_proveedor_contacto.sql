/* =============================================================================
   v19) Contacto directo del proveedor (persona de la empresa proveedora).
        Nombre, cargo, teléfono y email de la persona de contacto.
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Proveedor') AND name = 'ContactoNombre')
    ALTER TABLE dbo.Proveedor ADD ContactoNombre NVARCHAR(150) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Proveedor') AND name = 'ContactoCargo')
    ALTER TABLE dbo.Proveedor ADD ContactoCargo NVARCHAR(100) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Proveedor') AND name = 'ContactoTelefono')
    ALTER TABLE dbo.Proveedor ADD ContactoTelefono NVARCHAR(30) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Proveedor') AND name = 'ContactoEmail')
    ALTER TABLE dbo.Proveedor ADD ContactoEmail NVARCHAR(150) NULL;
GO

PRINT 'v19 aplicado: contacto directo del proveedor listo.';
GO
