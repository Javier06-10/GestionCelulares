/* =============================================================================
   v18) Condiciones de pago por proveedor.
        CondicionPago: Contado | Credito | Acuerdo
        DiasCredito:   días de crédito acordados (si Credito)
        NotaCondicion: detalle libre del acuerdo (si Acuerdo)
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Proveedor') AND name = 'CondicionPago')
    ALTER TABLE dbo.Proveedor ADD CondicionPago NVARCHAR(20) NOT NULL CONSTRAINT DF_Proveedor_CondicionPago DEFAULT(N'Contado');
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Proveedor') AND name = 'DiasCredito')
    ALTER TABLE dbo.Proveedor ADD DiasCredito INT NOT NULL CONSTRAINT DF_Proveedor_DiasCredito DEFAULT(0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Proveedor') AND name = 'NotaCondicion')
    ALTER TABLE dbo.Proveedor ADD NotaCondicion NVARCHAR(300) NULL;
GO

PRINT 'v18 aplicado: condiciones de pago del proveedor listas.';
GO
