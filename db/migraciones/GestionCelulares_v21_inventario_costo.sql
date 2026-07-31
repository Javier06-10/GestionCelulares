/* =============================================================================
   v21) Exponer el costo del inventario disponible (por IMEI) en la vista.
        CostoTotal = suma del PrecioCosto de las unidades disponibles de la
        variante (lo invertido en ese stock). Permite mostrar el dinero en
        inventario a costo, no solo el valor de venta.
   ============================================================================= */
USE [GestionCelulares];
GO

CREATE OR ALTER VIEW [dbo].[vw_InventarioDisponible] AS
-- Inventario disponible por modelo/variante/sucursal
SELECT
    p.ProductoId, p.Nombre AS Producto, m.Nombre AS Marca,
    v.VarianteId, v.Color, v.Almacenamiento, v.Condicion, v.PrecioVenta,
    v.StockMinimo,
    i.SucursalId,
    COUNT(i.ImeiId) AS Disponibles,
    SUM(i.PrecioCosto) AS CostoTotal
FROM dbo.InventarioImei i
JOIN dbo.ProductoVariante v ON v.VarianteId = i.VarianteId
JOIN dbo.Producto p        ON p.ProductoId = v.ProductoId
LEFT JOIN dbo.Marca m      ON m.MarcaId = p.MarcaId
WHERE i.Estado = N'Disponible'
GROUP BY p.ProductoId, p.Nombre, m.Nombre, v.VarianteId, v.Color, v.Almacenamiento, v.Condicion, v.PrecioVenta, v.StockMinimo, i.SucursalId;
GO

PRINT 'v21 aplicado: vw_InventarioDisponible expone CostoTotal.';
GO
