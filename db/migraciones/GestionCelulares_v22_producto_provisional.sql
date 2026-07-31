/* =============================================================================
   v22) Productos provisionales (venta rápida del POS).
        Provisional = 1 cuando el producto se crea al vuelo en el POS con solo
        nombre y precio; queda pendiente de formalizar (marca, categoría, costo).
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Producto') AND name = 'Provisional')
    ALTER TABLE dbo.Producto ADD Provisional BIT NOT NULL CONSTRAINT DF_Producto_Provisional DEFAULT(0);
GO

PRINT 'v22 aplicado: columna Provisional lista.';
GO
