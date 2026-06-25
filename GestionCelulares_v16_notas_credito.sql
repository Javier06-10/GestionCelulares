/* =============================================================================
   v16) Notas de Crédito (tipo 04).
        Documento fiscal con NCF propio (04) que reduce/revierte una factura ya
        emitida y declarada. A diferencia del 608 (anulación del NCF original),
        la Nota de Crédito se reporta en el 607 con su propio NCF y el NCF
        modificado (el original). Plantilla de secuencia 04 ya existe (v8).
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotaCredito')
BEGIN
    CREATE TABLE dbo.NotaCredito (
        NotaCreditoId  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        VentaId        INT            NOT NULL,
        Ncf            NVARCHAR(19)   NULL,            -- NCF 04 emitido
        NcfModificado  NVARCHAR(19)   NULL,            -- NCF original (01/02)
        Fecha          DATETIME       NOT NULL,        -- fecha del comprobante original
        Monto          DECIMAL(18,2)  NOT NULL DEFAULT(0),   -- base imponible revertida
        Itbis          DECIMAL(18,2)  NOT NULL DEFAULT(0),
        Total          DECIMAL(18,2)  NOT NULL DEFAULT(0),
        Motivo         NVARCHAR(200)  NULL,
        UsuarioId      INT            NULL,
        FechaRegistro  DATETIME       NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT FK_NotaCredito_Venta FOREIGN KEY (VentaId) REFERENCES dbo.Venta(VentaId)
    );
    CREATE INDEX IX_NotaCredito_Fecha ON dbo.NotaCredito(FechaRegistro);
END
GO

PRINT 'v16 aplicado: tabla NotaCredito lista.';
GO
