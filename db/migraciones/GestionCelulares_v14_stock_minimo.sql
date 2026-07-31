/* =============================================================================
   v14) Stock mínimo por variante.
        Umbral de alerta: cuando StockNoSerial <= StockMinimo el producto se
        marca como "próximo a agotar"; en 0 queda "agotado".
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ProductoVariante') AND name = 'StockMinimo'
)
BEGIN
    ALTER TABLE dbo.ProductoVariante
        ADD StockMinimo INT NOT NULL CONSTRAINT DF_ProductoVariante_StockMinimo DEFAULT(0);
END
GO

PRINT 'v14 aplicado: columna StockMinimo lista.';
GO
