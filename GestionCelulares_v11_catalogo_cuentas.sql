/* =============================================================================
   v11) Catálogo de Cuentas (plan contable).
        Las 6 cuentas principales (Activo, Pasivo, Capital, Ingreso, Costo,
        Gasto) con sub-cuentas estándar. El usuario puede crear las suyas.
        EsSistema=1 => cuenta base (no se puede eliminar).
   ============================================================================= */
USE [GestionCelulares];
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CuentaContable')
BEGIN
    CREATE TABLE dbo.CuentaContable (
        CuentaContableId  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Codigo            NVARCHAR(20)  NOT NULL,
        Nombre            NVARCHAR(150) NOT NULL,
        Tipo              NVARCHAR(15)  NOT NULL,   -- Activo, Pasivo, Capital, Ingreso, Costo, Gasto
        CuentaPadreId     INT NULL,
        Naturaleza        NVARCHAR(10)  NOT NULL,   -- Deudora / Acreedora
        PermiteMovimiento BIT NOT NULL DEFAULT(1),  -- hojas: se les puede registrar; grupos: no
        EsSistema         BIT NOT NULL DEFAULT(0),
        Activo            BIT NOT NULL DEFAULT(1),
        CONSTRAINT UQ_CuentaContable_Codigo UNIQUE (Codigo),
        CONSTRAINT FK_CuentaContable_Padre FOREIGN KEY (CuentaPadreId) REFERENCES dbo.CuentaContable(CuentaContableId)
    );

    -- 6 cuentas principales (grupos)
    INSERT INTO dbo.CuentaContable (Codigo, Nombre, Tipo, CuentaPadreId, Naturaleza, PermiteMovimiento, EsSistema, Activo) VALUES
        ('1', 'ACTIVO',   'Activo',  NULL, 'Deudora',   0, 1, 1),
        ('2', 'PASIVO',   'Pasivo',  NULL, 'Acreedora', 0, 1, 1),
        ('3', 'CAPITAL',  'Capital', NULL, 'Acreedora', 0, 1, 1),
        ('4', 'INGRESOS', 'Ingreso', NULL, 'Acreedora', 0, 1, 1),
        ('5', 'COSTOS',   'Costo',   NULL, 'Deudora',   0, 1, 1),
        ('6', 'GASTOS',   'Gasto',   NULL, 'Deudora',   0, 1, 1);

    -- Sub-cuentas estándar (hojas)
    INSERT INTO dbo.CuentaContable (Codigo, Nombre, Tipo, CuentaPadreId, Naturaleza, PermiteMovimiento, EsSistema, Activo)
    SELECT v.Codigo, v.Nombre, v.Tipo, p.CuentaContableId, v.Naturaleza, 1, 1, 1
    FROM (VALUES
        ('1.01', 'Caja',                      'Activo',  '1', 'Deudora'),
        ('1.02', 'Bancos',                    'Activo',  '1', 'Deudora'),
        ('1.03', 'Cuentas por Cobrar',        'Activo',  '1', 'Deudora'),
        ('1.04', 'Inventario de Mercancía',   'Activo',  '1', 'Deudora'),
        ('1.05', 'ITBIS Adelantado',          'Activo',  '1', 'Deudora'),
        ('1.06', 'Mobiliario y Equipo',       'Activo',  '1', 'Deudora'),
        ('2.01', 'Cuentas por Pagar',         'Pasivo',  '2', 'Acreedora'),
        ('2.02', 'ITBIS por Pagar',           'Pasivo',  '2', 'Acreedora'),
        ('2.03', 'Retenciones por Pagar',     'Pasivo',  '2', 'Acreedora'),
        ('3.01', 'Capital del Propietario',   'Capital', '3', 'Acreedora'),
        ('3.02', 'Resultados Acumulados',     'Capital', '3', 'Acreedora'),
        ('4.01', 'Ventas',                    'Ingreso', '4', 'Acreedora'),
        ('4.02', 'Ingresos por Reparaciones', 'Ingreso', '4', 'Acreedora'),
        ('4.03', 'Otros Ingresos',            'Ingreso', '4', 'Acreedora'),
        ('5.01', 'Costo de Mercancía Vendida','Costo',   '5', 'Deudora'),
        ('5.02', 'Costo de Repuestos',        'Costo',   '5', 'Deudora'),
        ('6.01', 'Sueldos y Salarios',        'Gasto',   '6', 'Deudora'),
        ('6.02', 'Alquiler',                  'Gasto',   '6', 'Deudora'),
        ('6.03', 'Servicios (luz, agua, internet)', 'Gasto', '6', 'Deudora'),
        ('6.04', 'Comisiones',                'Gasto',   '6', 'Deudora'),
        ('6.05', 'Depreciación',              'Gasto',   '6', 'Deudora'),
        ('6.06', 'Otros Gastos',              'Gasto',   '6', 'Deudora')
    ) v(Codigo, Nombre, Tipo, PadreCodigo, Naturaleza)
    JOIN dbo.CuentaContable p ON p.Codigo = v.PadreCodigo;
END
GO
SELECT COUNT(*) AS TotalCuentas FROM dbo.CuentaContable;
GO
