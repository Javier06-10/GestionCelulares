/* =============================================================================
   v25) Imagen del producto.
        ImagenUrl: URL de la imagen del producto, subida a un store externo
        (p. ej. Cloudinary). La BD solo guarda el enlace, no los bytes.
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Producto') AND name = 'ImagenUrl')
    ALTER TABLE dbo.Producto ADD ImagenUrl NVARCHAR(500) NULL;
GO

PRINT 'v25 aplicado: columna ImagenUrl del producto lista.';
GO
