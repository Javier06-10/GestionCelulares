/* =============================================================================
   v12) Libro Diario: asientos contables con partida doble.
        Cada asiento tiene N líneas (detalle); la suma de débitos debe igualar
        la suma de créditos. Alimenta el Libro Mayor y el Balance de Comprobación.
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AsientoContable')
BEGIN
    CREATE TABLE dbo.AsientoContable (
        AsientoContableId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Numero        INT NOT NULL,
        Fecha         DATE NOT NULL,
        Concepto      NVARCHAR(300) NOT NULL,
        Origen        NVARCHAR(20) NOT NULL DEFAULT('Manual'),  -- Manual, Venta, Compra, Caja, Nomina, Ajuste
        ReferenciaId  INT NULL,
        Estado        NVARCHAR(15) NOT NULL DEFAULT('Registrado'),  -- Registrado, Anulado
        UsuarioId     INT NULL,
        FechaRegistro DATETIME2(0) NOT NULL DEFAULT(SYSDATETIME())
    );

    CREATE TABLE dbo.AsientoDetalle (
        AsientoDetalleId  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AsientoContableId INT NOT NULL,
        CuentaContableId  INT NOT NULL,
        Debito  DECIMAL(18,2) NOT NULL DEFAULT(0),
        Credito DECIMAL(18,2) NOT NULL DEFAULT(0),
        Descripcion NVARCHAR(200) NULL,
        CONSTRAINT FK_AsientoDetalle_Asiento FOREIGN KEY (AsientoContableId) REFERENCES dbo.AsientoContable(AsientoContableId),
        CONSTRAINT FK_AsientoDetalle_Cuenta FOREIGN KEY (CuentaContableId) REFERENCES dbo.CuentaContable(CuentaContableId)
    );
    CREATE INDEX IX_AsientoDetalle_Cuenta ON dbo.AsientoDetalle(CuentaContableId);
    CREATE INDEX IX_AsientoContable_Fecha ON dbo.AsientoContable(Fecha);
END
GO
SELECT 'OK' AS Estado;
