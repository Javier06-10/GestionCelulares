/* =============================================================================
   v17) Bloqueo de inicio de sesión por intentos fallidos.
        IntentosFallidos: contador de fallos consecutivos.
        BloqueadoHasta:   si es futuro, la cuenta está bloqueada temporalmente.
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuario') AND name = 'IntentosFallidos')
    ALTER TABLE dbo.Usuario ADD IntentosFallidos INT NOT NULL CONSTRAINT DF_Usuario_IntentosFallidos DEFAULT(0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuario') AND name = 'BloqueadoHasta')
    ALTER TABLE dbo.Usuario ADD BloqueadoHasta DATETIME NULL;
GO

PRINT 'v17 aplicado: columnas de bloqueo de login listas.';
GO
