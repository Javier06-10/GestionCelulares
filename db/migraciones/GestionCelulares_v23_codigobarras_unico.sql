/* =============================================================================
   v23) Unicidad del código de barras a nivel de base de datos.
        Índice único filtrado: impide dos variantes con el mismo CodigoBarras
        (cierra la condición de carrera de la validación por código). Los NULL
        no cuentan, así que las variantes sin código no chocan entre sí.
   ============================================================================= */
USE [GestionCelulares];
GO

-- Los índices filtrados exigen estas opciones activas
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ProductoVariante_CodigoBarras' AND object_id = OBJECT_ID('dbo.ProductoVariante'))
    CREATE UNIQUE INDEX UX_ProductoVariante_CodigoBarras
        ON dbo.ProductoVariante(CodigoBarras)
        WHERE CodigoBarras IS NOT NULL;
GO

PRINT 'v23 aplicado: indice unico de CodigoBarras listo.';
GO
