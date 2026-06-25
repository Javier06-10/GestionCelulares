/* =============================================================================
   v15) Exponer StockMinimo en la vista de inventario disponible (por IMEI).
        Permite marcar dispositivos serializados como "próximo a agotar"
        cuando el conteo de unidades disponibles <= StockMinimo de la variante.
   ============================================================================= */
USE [GestionCelulares];
GO

CREATE OR ALTER VIEW [dbo].[vw_InventarioDisponible] AS
-- Inventario disponible por modelo/variante/sucursal
SELECT
    p.ProductoId, p.Nombre AS Producto, m.Nombre AS Marca,
    v.VarianteId, v.Color, v.Almacenamiento, v.Condicion, v.PrecioVenta,
    v.StockMinimo,
    i.SucursalId, COUNT(i.ImeiId) AS Disponibles
FROM dbo.InventarioImei i
JOIN dbo.ProductoVariante v ON v.VarianteId = i.VarianteId
JOIN dbo.Producto p        ON p.ProductoId = v.ProductoId
LEFT JOIN dbo.Marca m      ON m.MarcaId = p.MarcaId
WHERE i.Estado = N'Disponible'
GROUP BY p.ProductoId, p.Nombre, m.Nombre, v.VarianteId, v.Color, v.Almacenamiento, v.Condicion, v.PrecioVenta, v.StockMinimo, i.SucursalId;
GO

PRINT 'v15 aplicado: vw_InventarioDisponible expone StockMinimo.';
GO
