/* =============================================================================
   v8) Gestión de NCF (Números de Comprobante Fiscal) — comprobantes
       pre-autorizados por la DGII.
       - Venta.Ncf: el NCF asignado a la venta.
       - SecuenciaNcf: rango autorizado por tipo (01 Crédito Fiscal, 02 Consumo,
         04 Nota de Crédito), con secuencia actual, límite y vencimiento.
       - usp_Ncf_Siguiente: entrega atómicamente el próximo NCF de un tipo.
   ============================================================================= */
USE [GestionCelulares];
GO

IF COL_LENGTH('dbo.Venta', 'Ncf') IS NULL
    ALTER TABLE dbo.Venta ADD Ncf NVARCHAR(19) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SecuenciaNcf')
BEGIN
    CREATE TABLE dbo.SecuenciaNcf (
        SecuenciaNcfId  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TipoComprobante NVARCHAR(2)  NOT NULL,          -- 01, 02, 04...
        Serie           NVARCHAR(2)  NOT NULL DEFAULT('B'),
        Secuencia       INT          NOT NULL DEFAULT(1),   -- próximo a asignar
        Hasta           INT          NOT NULL DEFAULT(0),   -- límite autorizado
        Vencimiento     DATE         NULL,
        Activo          BIT          NOT NULL DEFAULT(0),
        CONSTRAINT UQ_SecuenciaNcf_Tipo UNIQUE (TipoComprobante)
    );

    -- Plantillas inactivas para configurar (Hasta=0 => no asigna hasta configurar)
    INSERT INTO dbo.SecuenciaNcf (TipoComprobante, Serie, Secuencia, Hasta, Activo) VALUES
        ('01', 'B', 1, 0, 0),   -- Factura de Crédito Fiscal (cliente con RNC)
        ('02', 'B', 1, 0, 0),   -- Factura de Consumo
        ('04', 'B', 1, 0, 0);   -- Nota de Crédito
END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_Ncf_Siguiente]
    @Tipo NVARCHAR(2),
    @Ncf  NVARCHAR(19) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Ncf = NULL;

    DECLARE @out TABLE (Ncf NVARCHAR(19));

    -- Incremento atómico: solo si está activo, hay rango disponible y no venció
    UPDATE dbo.SecuenciaNcf
        SET Secuencia = Secuencia + 1
    OUTPUT (deleted.Serie + deleted.TipoComprobante
            + RIGHT('00000000' + CAST(deleted.Secuencia AS VARCHAR(8)), 8)) INTO @out
    WHERE TipoComprobante = @Tipo
      AND Activo = 1
      AND Secuencia <= Hasta
      AND (Vencimiento IS NULL OR Vencimiento >= CAST(GETDATE() AS DATE));

    SELECT @Ncf = Ncf FROM @out;
END
GO
