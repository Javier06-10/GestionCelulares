/* =============================================================================
   v26) Categoría "Reparaciones" para clasificar las piezas de repuesto.
        El selector de repuestos del taller filtra por esta categoría.
        (Las piezas siguen siendo accesorios vendibles en el POS.)
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Categoria WHERE Nombre = N'Reparaciones')
    INSERT INTO dbo.Categoria (Nombre) VALUES (N'Reparaciones');
GO

PRINT 'v26 aplicado: categoria Reparaciones lista.';
GO
