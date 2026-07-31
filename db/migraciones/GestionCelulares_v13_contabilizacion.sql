/* =============================================================================
   v13) Contabilización automática.
        Índice único filtrado para garantizar idempotencia: un documento de
        origen (Venta / Nomina / Caja) solo puede tener UN asiento generado.
        Los asientos manuales (Origen='Manual', ReferenciaId NULL) quedan fuera.
   ============================================================================= */
USE [GestionCelulares];
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Asiento_Origen_Referencia')
BEGIN
    CREATE UNIQUE INDEX UX_Asiento_Origen_Referencia
        ON dbo.AsientoContable (Origen, ReferenciaId)
        WHERE ReferenciaId IS NOT NULL;
END
GO

PRINT 'v13 aplicado: índice de idempotencia de contabilización listo.';
GO
