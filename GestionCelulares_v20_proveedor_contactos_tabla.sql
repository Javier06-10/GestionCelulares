/* =============================================================================
   v20) Contactos del proveedor en tabla propia (varios contactos por proveedor).
        - Crea dbo.ContactoProveedor.
        - Migra el contacto único (columnas v19) como contacto principal.
        - Elimina las columnas v19 del Proveedor.
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContactoProveedor')
BEGIN
    CREATE TABLE dbo.ContactoProveedor (
        ContactoProveedorId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProveedorId         INT           NOT NULL,
        Nombre              NVARCHAR(150) NOT NULL,
        Cargo               NVARCHAR(100) NULL,
        Telefono            NVARCHAR(30)  NULL,
        Email               NVARCHAR(150) NULL,
        EsPrincipal         BIT           NOT NULL CONSTRAINT DF_ContactoProveedor_EsPrincipal DEFAULT(0),
        FechaCreacion       DATETIME      NOT NULL CONSTRAINT DF_ContactoProveedor_Fecha DEFAULT(GETDATE()),
        CONSTRAINT FK_ContactoProveedor_Proveedor FOREIGN KEY (ProveedorId)
            REFERENCES dbo.Proveedor(ProveedorId) ON DELETE CASCADE
    );
    CREATE INDEX IX_ContactoProveedor_Proveedor ON dbo.ContactoProveedor(ProveedorId);
END
GO

-- Migra el contacto único (v19) al nuevo modelo, como principal
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Proveedor') AND name = 'ContactoNombre')
BEGIN
    INSERT INTO dbo.ContactoProveedor (ProveedorId, Nombre, Cargo, Telefono, Email, EsPrincipal, FechaCreacion)
    SELECT ProveedorId, ContactoNombre, ContactoCargo, ContactoTelefono, ContactoEmail, 1, GETDATE()
    FROM dbo.Proveedor
    WHERE ContactoNombre IS NOT NULL AND LTRIM(RTRIM(ContactoNombre)) <> '';

    ALTER TABLE dbo.Proveedor DROP COLUMN ContactoNombre, ContactoCargo, ContactoTelefono, ContactoEmail;
END
GO

PRINT 'v20 aplicado: tabla ContactoProveedor lista y contacto unico migrado.';
GO
