/* =============================================================================
   v9) Comprobantes anulados (formato 608 de la DGII).
       Registra los NCF anulados con su tipo de anulación. Se alimenta al
       anular una venta (la venta queda en estado 'Anulada' y su inventario
       se revierte desde la aplicación).
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ComprobanteAnulado')
BEGIN
    CREATE TABLE dbo.ComprobanteAnulado (
        ComprobanteAnuladoId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Ncf              NVARCHAR(19) NOT NULL,
        FechaComprobante DATE         NOT NULL,
        TipoAnulacion    NVARCHAR(2)  NOT NULL,   -- 01-09 según DGII
        Motivo           NVARCHAR(200) NULL,
        VentaId          INT          NULL,
        UsuarioId        INT          NULL,
        FechaRegistro    DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_ComprobanteAnulado_Venta FOREIGN KEY (VentaId) REFERENCES dbo.Venta(VentaId)
    );
END
GO
SELECT 'OK' AS Estado;
