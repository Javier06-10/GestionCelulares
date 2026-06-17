USE [GestionCelulares];
GO

ALTER PROCEDURE [dbo].[usp_Venta_Registrar]
    @SucursalId    INT,
    @UsuarioId     INT,
    @ClienteId     INT = NULL,
    @SesionCajaId  INT = NULL,
    @EsCredito     BIT = 0,
    @MetodoPagoId  INT = NULL,
    @NumeroFactura NVARCHAR(30) = NULL,
    @Detalles      dbo.VentaDetalleTipo READONLY,
    @VentaId       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM @Detalles)
    BEGIN
        RAISERROR(N'La venta no tiene detalles.', 16, 1);
        RETURN;
    END

    -- VALIDACIÓN CRÍTICA: Prevenir Stock Negativo en productos no serializados
    IF EXISTS (
        SELECT 1
        FROM @Detalles d
        JOIN dbo.ProductoVariante pv ON pv.VarianteId = d.VarianteId
        WHERE d.ImeiId IS NULL AND (pv.StockNoSerial - d.Cantidad) < 0
    )
    BEGIN
        RAISERROR(N'Stock insuficiente para uno o más accesorios/productos no serializados.', 16, 1);
        RETURN;
    END

    DECLARE @Itbis DECIMAL(9,4) = ISNULL((SELECT TOP 1 PorcentajeItbis FROM dbo.Empresa ORDER BY EmpresaId), 0);

    BEGIN TRAN;

    -- Bloqueo y validación de IMEIs concurrentes
    IF EXISTS (
        SELECT 1 FROM @Detalles d
        JOIN dbo.InventarioImei i WITH (UPDLOCK, HOLDLOCK) ON i.ImeiId = d.ImeiId
        WHERE d.ImeiId IS NOT NULL AND i.Estado <> N'Disponible'
    )
    BEGIN
        ROLLBACK TRAN;
        RAISERROR(N'Uno o más IMEI seleccionados ya no están disponibles.', 16, 1);
        RETURN;
    END

    -- Insertar Cabecera
    INSERT INTO dbo.Venta
        (NumeroFactura, SucursalId, ClienteId, UsuarioId, SesionCajaId, EsCredito, Estado, Subtotal, Descuento, Impuesto, Total)
    VALUES
        (@NumeroFactura, @SucursalId, @ClienteId, @UsuarioId, @SesionCajaId, @EsCredito, N'Completada', 0, 0, 0, 0);

    SET @VentaId = SCOPE_IDENTITY();

    -- Insertar Detalle con generación dinámica de la descripción para auditoría/facturación
    INSERT INTO dbo.VentaDetalle
        (VentaId, ImeiId, VarianteId, Descripcion, Cantidad, PrecioUnitario, Descuento, Impuesto, Total)
    SELECT
        @VentaId, d.ImeiId, d.VarianteId,
        CONCAT(p.Nombre, N' ', v.Color, N' ', v.Almacenamiento), -- Solución a la descripción NULL
        d.Cantidad, d.PrecioUnitario, d.Descuento,
        ROUND((d.PrecioUnitario * d.Cantidad - d.Descuento) * @Itbis / 100.0, 2),
        (d.PrecioUnitario * d.Cantidad - d.Descuento) + ROUND((d.PrecioUnitario * d.Cantidad - d.Descuento) * @Itbis / 100.0, 2)
    FROM @Detalles d
    JOIN dbo.ProductoVariante v ON v.VarianteId = d.VarianteId
    JOIN dbo.Producto p ON p.ProductoId = v.ProductoId;

    -- Actualizaciones de Inventario
    UPDATE i SET i.Estado = N'Vendido'
    FROM dbo.InventarioImei i
    JOIN @Detalles d ON d.ImeiId = i.ImeiId;

    INSERT INTO dbo.MovimientoInventario (ImeiId, Tipo, SucursalOrigen, Referencia, UsuarioId)
    SELECT d.ImeiId, N'Venta', @SucursalId, CONCAT(N'Venta #', @VentaId), @UsuarioId
    FROM @Detalles d
    WHERE d.ImeiId IS NOT NULL;

    -- Deducción controlada de Stock No Serializado
    UPDATE v SET v.StockNoSerial = v.StockNoSerial - d.Cantidad
    FROM dbo.ProductoVariante v
    JOIN @Detalles d ON d.VarianteId = v.VarianteId
    WHERE d.ImeiId IS NULL;

    -- Recálculo de Totales
    UPDATE vta
       SET vta.Subtotal  = x.Sub, vta.Descuento = x.Des, vta.Impuesto  = x.Imp, vta.Total     = x.Tot
    FROM dbo.Venta vta
    CROSS APPLY (
        SELECT SUM(PrecioUnitario * Cantidad) AS Sub, SUM(Descuento) AS Des, SUM(Impuesto) AS Imp, SUM(Total) AS Tot
        FROM dbo.VentaDetalle WHERE VentaId = @VentaId
    ) x
    WHERE vta.VentaId = @VentaId;

    IF @EsCredito = 0
    BEGIN
        INSERT INTO dbo.VentaPago (VentaId, MetodoPagoId, Monto)
        SELECT @VentaId, @MetodoPagoId, Total FROM dbo.Venta WHERE VentaId = @VentaId;
    END

    COMMIT TRAN;
END;
GO
