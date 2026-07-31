/* =============================================================================
   v5) Numeración secuencial de facturas
       Tabla de secuencia para generar números de factura correlativos.
       Base para la futura facturación electrónica (e-CF/DGII).
   ============================================================================= */
USE [GestionCelulares];
GO

IF OBJECT_ID('dbo.Secuencia') IS NULL
BEGIN
    CREATE TABLE dbo.Secuencia (
        Nombre   NVARCHAR(30)  NOT NULL PRIMARY KEY,
        Prefijo  NVARCHAR(15)  NOT NULL DEFAULT(N''),
        Valor    INT           NOT NULL DEFAULT(0),   -- último valor emitido
        Longitud INT           NOT NULL DEFAULT(8)    -- dígitos del correlativo
    );
END
GO

-- Secuencia de facturas (idempotente)
IF NOT EXISTS (SELECT 1 FROM dbo.Secuencia WHERE Nombre = N'Factura')
    INSERT INTO dbo.Secuencia (Nombre, Prefijo, Valor, Longitud)
    VALUES (N'Factura', N'FAC-', 0, 6);
GO

/* El backend obtiene el próximo número de forma atómica con:
   UPDATE dbo.Secuencia SET Valor = Valor + 1
   OUTPUT inserted.Prefijo, inserted.Valor, inserted.Longitud
   WHERE Nombre = N'Factura';
   y lo formatea como Prefijo + RIGHT(REPLICATE('0',Longitud)+Valor, Longitud). */
