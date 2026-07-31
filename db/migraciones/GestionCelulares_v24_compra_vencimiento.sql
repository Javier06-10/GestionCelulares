/* =============================================================================
   v24) Vencimiento de las compras a crédito (Cuentas por Pagar).
        FechaVencimiento = Fecha + días de crédito del proveedor (snapshot).
        Null en compras al contado. Permite alertar de facturas vencidas.
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Compra') AND name = 'FechaVencimiento')
    ALTER TABLE dbo.Compra ADD FechaVencimiento DATETIME NULL;
GO

-- Backfill: las compras existentes SIN pago vinculado se consideran a crédito.
-- Se les fija el vencimiento con los días acordados actuales del proveedor.
UPDATE c
    SET c.FechaVencimiento = DATEADD(DAY, p.DiasCredito, CAST(c.Fecha AS DATE))
FROM dbo.Compra c
    INNER JOIN dbo.Proveedor p ON p.ProveedorId = c.ProveedorId
WHERE c.FechaVencimiento IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PagoProveedor pp WHERE pp.CompraId = c.CompraId);
GO

PRINT 'v24 aplicado: vencimiento de compras a credito listo.';
GO
