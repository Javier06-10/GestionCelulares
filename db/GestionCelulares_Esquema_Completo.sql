/* ============================================================================
   GestionCelulares — ESQUEMA COMPLETO (instalación desde cero)
   ----------------------------------------------------------------------------
   UN solo script que crea toda la base de datos: tipos, tablas (con PK, UNIQUE,
   CHECK, DEFAULT, índices y triggers), llaves foráneas, vistas, funciones,
   procedimientos y las semillas de configuración.

   Reemplaza, para instalaciones NUEVAS, la ejecución encadenada de
   setup_completo + v5..v26. Las bases de datos YA existentes siguen usando
   Apply-Migrations.ps1 / dbo.SchemaVersion (este script NO las altera).

   Es idempotente: tablas con IF NOT EXISTS, programables con CREATE OR ALTER,
   semillas con guardas. Correrlo dos veces no rompe nada.

   Generado el 2026-07-31 a partir del esquema real (fuente de verdad) con SMO.
   Uso:  sqlcmd -S localhost -i GestionCelulares_Esquema_Completo.sql
   ============================================================================ */
SET NOCOUNT ON;
GO

IF DB_ID(N'GestionCelulares') IS NULL
    CREATE DATABASE [GestionCelulares];
GO

USE [GestionCelulares];
GO

/* ============================================================================ */
/*  SECUENCIAS */
/* ============================================================================ */

/* ============================================================================ */
/*  TIPOS DEFINIDOS POR EL USUARIO */
/* ============================================================================ */
IF NOT EXISTS (SELECT * FROM sys.types st JOIN sys.schemas ss ON st.schema_id = ss.schema_id WHERE st.name = N'VentaDetalleTipo' AND ss.name = N'dbo')
CREATE TYPE [dbo].[VentaDetalleTipo] AS TABLE(
	[ImeiId] [int] NULL,
	[VarianteId] [int] NOT NULL,
	[Cantidad] [int] NOT NULL,
	[PrecioUnitario] [decimal](18, 2) NOT NULL,
	[Descuento] [decimal](18, 2) NOT NULL
)
GO

/* ============================================================================ */
/*  TABLAS (con PK, UNIQUE, CHECK, DEFAULT, indices y triggers) */
/* ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AbonoApartado]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[AbonoApartado](
	[AbonoApartadoId] [int] IDENTITY(1,1) NOT NULL,
	[ApartadoId] [int] NOT NULL,
	[Monto] [decimal](18, 2) NOT NULL,
	[MetodoPagoId] [int] NULL,
	[SesionCajaId] [int] NULL,
	[UsuarioId] [int] NULL,
	[Tipo] [nvarchar](15) NOT NULL,
	[Fecha] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[AbonoApartadoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__AbonoApart__Tipo__5C6CB6D7]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AbonoApartado') AND c.name = N'Tipo')
ALTER TABLE [dbo].[AbonoApartado] ADD  DEFAULT ('Abono') FOR [Tipo]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__AbonoApar__Fecha__5D60DB10]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AbonoApartado') AND c.name = N'Fecha')
ALTER TABLE [dbo].[AbonoApartado] ADD  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Apartado]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Apartado](
	[ApartadoId] [int] IDENTITY(1,1) NOT NULL,
	[ClienteId] [int] NOT NULL,
	[ImeiId] [int] NULL,
	[VarianteId] [int] NOT NULL,
	[SucursalId] [int] NOT NULL,
	[UsuarioId] [int] NULL,
	[PrecioTotal] [decimal](18, 2) NOT NULL,
	[TotalAbonado] [decimal](18, 2) NOT NULL,
	[Estado] [nvarchar](15) NOT NULL,
	[VentaId] [int] NULL,
	[Notas] [nvarchar](300) NULL,
	[FechaInicio] [datetime2](0) NOT NULL,
	[FechaCierre] [datetime2](0) NULL,
PRIMARY KEY CLUSTERED 
(
	[ApartadoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Apartado__TotalA__54CB950F]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Apartado') AND c.name = N'TotalAbonado')
ALTER TABLE [dbo].[Apartado] ADD  DEFAULT ((0)) FOR [TotalAbonado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Apartado__Estado__55BFB948]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Apartado') AND c.name = N'Estado')
ALTER TABLE [dbo].[Apartado] ADD  DEFAULT ('Activo') FOR [Estado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Apartado__FechaI__56B3DD81]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Apartado') AND c.name = N'FechaInicio')
ALTER TABLE [dbo].[Apartado] ADD  DEFAULT (sysdatetime()) FOR [FechaInicio]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AsientoContable]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[AsientoContable](
	[AsientoContableId] [int] IDENTITY(1,1) NOT NULL,
	[Numero] [int] NOT NULL,
	[Fecha] [date] NOT NULL,
	[Concepto] [nvarchar](300) NOT NULL,
	[Origen] [nvarchar](20) NOT NULL,
	[ReferenciaId] [int] NULL,
	[Estado] [nvarchar](15) NOT NULL,
	[UsuarioId] [int] NULL,
	[FechaRegistro] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[AsientoContableId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AsientoContable]') AND name = N'IX_AsientoContable_Fecha')
CREATE NONCLUSTERED INDEX [IX_AsientoContable_Fecha] ON [dbo].[AsientoContable]
(
	[Fecha] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AsientoContable]') AND name = N'UX_Asiento_Origen_Referencia')
CREATE UNIQUE NONCLUSTERED INDEX [UX_Asiento_Origen_Referencia] ON [dbo].[AsientoContable]
(
	[Origen] ASC,
	[ReferenciaId] ASC
)
WHERE ([ReferenciaId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__AsientoCo__Orige__3AD6B8E2]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AsientoContable') AND c.name = N'Origen')
ALTER TABLE [dbo].[AsientoContable] ADD  DEFAULT ('Manual') FOR [Origen]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__AsientoCo__Estad__3BCADD1B]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AsientoContable') AND c.name = N'Estado')
ALTER TABLE [dbo].[AsientoContable] ADD  DEFAULT ('Registrado') FOR [Estado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__AsientoCo__Fecha__3CBF0154]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AsientoContable') AND c.name = N'FechaRegistro')
ALTER TABLE [dbo].[AsientoContable] ADD  DEFAULT (sysdatetime()) FOR [FechaRegistro]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AsientoDetalle]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[AsientoDetalle](
	[AsientoDetalleId] [int] IDENTITY(1,1) NOT NULL,
	[AsientoContableId] [int] NOT NULL,
	[CuentaContableId] [int] NOT NULL,
	[Debito] [decimal](18, 2) NOT NULL,
	[Credito] [decimal](18, 2) NOT NULL,
	[Descripcion] [nvarchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[AsientoDetalleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AsientoDetalle]') AND name = N'IX_AsientoDetalle_Cuenta')
CREATE NONCLUSTERED INDEX [IX_AsientoDetalle_Cuenta] ON [dbo].[AsientoDetalle]
(
	[CuentaContableId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__AsientoDe__Debit__3F9B6DFF]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AsientoDetalle') AND c.name = N'Debito')
ALTER TABLE [dbo].[AsientoDetalle] ADD  DEFAULT ((0)) FOR [Debito]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__AsientoDe__Credi__408F9238]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AsientoDetalle') AND c.name = N'Credito')
ALTER TABLE [dbo].[AsientoDetalle] ADD  DEFAULT ((0)) FOR [Credito]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CasoGarantia]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[CasoGarantia](
	[CasoGarantiaId] [int] IDENTITY(1,1) NOT NULL,
	[NumeroCaso] [nvarchar](30) NULL,
	[GarantiaId] [int] NULL,
	[ImeiId] [int] NULL,
	[ClienteId] [int] NULL,
	[OrdenTallerId] [int] NULL,
	[FechaApertura] [datetime2](0) NOT NULL,
	[TipoFalla] [nvarchar](120) NULL,
	[DescripcionFalla] [nvarchar](500) NULL,
	[TipoResolucion] [nvarchar](15) NULL,
	[ImeiReemplazoId] [int] NULL,
	[Estado] [nvarchar](12) NOT NULL,
	[FechaCierre] [datetime2](0) NULL,
	[Notas] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[CasoGarantiaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CasoGarantia]') AND name = N'IX_Caso_Estado')
CREATE NONCLUSTERED INDEX [IX_Caso_Estado] ON [dbo].[CasoGarantia]
(
	[Estado] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CasoGarantia]') AND name = N'IX_Caso_Imei')
CREATE NONCLUSTERED INDEX [IX_Caso_Imei] ON [dbo].[CasoGarantia]
(
	[ImeiId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Caso_Apertura]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[CasoGarantia] ADD  CONSTRAINT [DF_Caso_Apertura]  DEFAULT (sysdatetime()) FOR [FechaApertura]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Caso_Estado]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[CasoGarantia] ADD  CONSTRAINT [DF_Caso_Estado]  DEFAULT (N'Abierto') FOR [Estado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Caso_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Caso_Estado' AND parent_object_id = OBJECT_ID(N'dbo.CasoGarantia'))
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [CK_Caso_Estado] CHECK  (([Estado]=N'Cerrado' OR [Estado]=N'Rechazado' OR [Estado]=N'Resuelto' OR [Estado]=N'EnProceso' OR [Estado]=N'Abierto'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Caso_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [CK_Caso_Estado]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Caso_Resolucion]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Caso_Resolucion' AND parent_object_id = OBJECT_ID(N'dbo.CasoGarantia'))
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [CK_Caso_Resolucion] CHECK  (([TipoResolucion] IS NULL OR ([TipoResolucion]=N'Rechazado' OR [TipoResolucion]=N'NotaCredito' OR [TipoResolucion]=N'Reemplazo' OR [TipoResolucion]=N'Reparacion')))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Caso_Resolucion]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [CK_Caso_Resolucion]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categoria]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Categoria](
	[CategoriaId] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](80) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CategoriaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Categoria_Nombre] UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Cliente]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Cliente](
	[ClienteId] [int] IDENTITY(1,1) NOT NULL,
	[Cedula] [nvarchar](20) NULL,
	[Nombre] [nvarchar](150) NOT NULL,
	[Telefono] [nvarchar](30) NULL,
	[Email] [nvarchar](150) NULL,
	[Direccion] [nvarchar](250) NULL,
	[EsMoroso] [bit] NOT NULL,
	[Bloqueado] [bit] NOT NULL,
	[FechaCreacion] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ClienteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Cliente]') AND name = N'IX_Cliente_Cedula')
CREATE NONCLUSTERED INDEX [IX_Cliente_Cedula] ON [dbo].[Cliente]
(
	[Cedula] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Cliente]') AND name = N'IX_Cliente_Telefono')
CREATE NONCLUSTERED INDEX [IX_Cliente_Telefono] ON [dbo].[Cliente]
(
	[Telefono] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Cliente_Moroso]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Cliente] ADD  CONSTRAINT [DF_Cliente_Moroso]  DEFAULT ((0)) FOR [EsMoroso]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Cliente_Bloqueado]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Cliente] ADD  CONSTRAINT [DF_Cliente_Bloqueado]  DEFAULT ((0)) FOR [Bloqueado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Cliente_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Cliente] ADD  CONSTRAINT [DF_Cliente_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Comision]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Comision](
	[ComisionId] [int] IDENTITY(1,1) NOT NULL,
	[UsuarioId] [int] NOT NULL,
	[VentaId] [int] NULL,
	[OrigenTipo] [nvarchar](15) NOT NULL,
	[Monto] [decimal](18, 2) NOT NULL,
	[Fecha] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ComisionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Comision_Origen]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Comision] ADD  CONSTRAINT [DF_Comision_Origen]  DEFAULT (N'Venta') FOR [OrigenTipo]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Comision_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Comision] ADD  CONSTRAINT [DF_Comision_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Comision_Origen]') AND parent_object_id = OBJECT_ID(N'[dbo].[Comision]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Comision_Origen' AND parent_object_id = OBJECT_ID(N'dbo.Comision'))
ALTER TABLE [dbo].[Comision]  WITH CHECK ADD  CONSTRAINT [CK_Comision_Origen] CHECK  (([OrigenTipo]=N'Taller' OR [OrigenTipo]=N'Venta'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Comision_Origen]') AND parent_object_id = OBJECT_ID(N'[dbo].[Comision]'))
ALTER TABLE [dbo].[Comision] CHECK CONSTRAINT [CK_Comision_Origen]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Compra]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Compra](
	[CompraId] [int] IDENTITY(1,1) NOT NULL,
	[ProveedorId] [int] NOT NULL,
	[SucursalId] [int] NOT NULL,
	[NumeroFactura] [nvarchar](50) NULL,
	[Fecha] [datetime2](0) NOT NULL,
	[Total] [decimal](18, 2) NOT NULL,
	[Notas] [nvarchar](300) NULL,
	[Subtotal] [decimal](18, 2) NULL,
	[Itbis] [decimal](18, 2) NULL,
	[TipoBienServicio] [nvarchar](2) NULL,
	[MetodoPagoId] [int] NULL,
	[FechaVencimiento] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[CompraId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Compra_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Compra] ADD  CONSTRAINT [DF_Compra_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Compra_Total]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Compra] ADD  CONSTRAINT [DF_Compra_Total]  DEFAULT ((0)) FOR [Total]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ComprobanteAnulado]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ComprobanteAnulado](
	[ComprobanteAnuladoId] [int] IDENTITY(1,1) NOT NULL,
	[Ncf] [nvarchar](19) NOT NULL,
	[FechaComprobante] [date] NOT NULL,
	[TipoAnulacion] [nvarchar](2) NOT NULL,
	[Motivo] [nvarchar](200) NULL,
	[VentaId] [int] NULL,
	[UsuarioId] [int] NULL,
	[FechaRegistro] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ComprobanteAnuladoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Comproban__Fecha__02925FBF]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ComprobanteAnulado') AND c.name = N'FechaRegistro')
ALTER TABLE [dbo].[ComprobanteAnulado] ADD  DEFAULT (sysdatetime()) FOR [FechaRegistro]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ContactoProveedor]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ContactoProveedor](
	[ContactoProveedorId] [int] IDENTITY(1,1) NOT NULL,
	[ProveedorId] [int] NOT NULL,
	[Nombre] [nvarchar](150) NOT NULL,
	[Cargo] [nvarchar](100) NULL,
	[Telefono] [nvarchar](30) NULL,
	[Email] [nvarchar](150) NULL,
	[EsPrincipal] [bit] NOT NULL,
	[FechaCreacion] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ContactoProveedorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ContactoProveedor]') AND name = N'IX_ContactoProveedor_Proveedor')
CREATE NONCLUSTERED INDEX [IX_ContactoProveedor_Proveedor] ON [dbo].[ContactoProveedor]
(
	[ProveedorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_ContactoProveedor_EsPrincipal]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[ContactoProveedor] ADD  CONSTRAINT [DF_ContactoProveedor_EsPrincipal]  DEFAULT ((0)) FOR [EsPrincipal]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_ContactoProveedor_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[ContactoProveedor] ADD  CONSTRAINT [DF_ContactoProveedor_Fecha]  DEFAULT (getdate()) FOR [FechaCreacion]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Credito]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Credito](
	[CreditoId] [int] IDENTITY(1,1) NOT NULL,
	[VentaId] [int] NULL,
	[ClienteId] [int] NOT NULL,
	[MontoFinanciado] [decimal](18, 2) NOT NULL,
	[Inicial] [decimal](18, 2) NOT NULL,
	[TasaInteres] [decimal](9, 4) NOT NULL,
	[NumeroCuotas] [int] NOT NULL,
	[MontoTotal] [decimal](18, 2) NOT NULL,
	[Saldo] [decimal](18, 2) NOT NULL,
	[Estado] [nvarchar](15) NOT NULL,
	[FechaInicio] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CreditoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Credito]') AND name = N'IX_Credito_Cliente')
CREATE NONCLUSTERED INDEX [IX_Credito_Cliente] ON [dbo].[Credito]
(
	[ClienteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Credito_Inicial]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Credito] ADD  CONSTRAINT [DF_Credito_Inicial]  DEFAULT ((0)) FOR [Inicial]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Credito_Tasa]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Credito] ADD  CONSTRAINT [DF_Credito_Tasa]  DEFAULT ((0)) FOR [TasaInteres]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Credito_Estado]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Credito] ADD  CONSTRAINT [DF_Credito_Estado]  DEFAULT (N'Activo') FOR [Estado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Credito_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Credito] ADD  CONSTRAINT [DF_Credito_Fecha]  DEFAULT (sysdatetime()) FOR [FechaInicio]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Credito_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[Credito]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Credito_Estado' AND parent_object_id = OBJECT_ID(N'dbo.Credito'))
ALTER TABLE [dbo].[Credito]  WITH CHECK ADD  CONSTRAINT [CK_Credito_Estado] CHECK  (([Estado]=N'Reestructurado' OR [Estado]=N'Saldado' OR [Estado]=N'EnMora' OR [Estado]=N'Activo'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Credito_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[Credito]'))
ALTER TABLE [dbo].[Credito] CHECK CONSTRAINT [CK_Credito_Estado]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CuentaContable]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[CuentaContable](
	[CuentaContableId] [int] IDENTITY(1,1) NOT NULL,
	[Codigo] [nvarchar](20) NOT NULL,
	[Nombre] [nvarchar](150) NOT NULL,
	[Tipo] [nvarchar](15) NOT NULL,
	[CuentaPadreId] [int] NULL,
	[Naturaleza] [nvarchar](10) NOT NULL,
	[PermiteMovimiento] [bit] NOT NULL,
	[EsSistema] [bit] NOT NULL,
	[Activo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CuentaContableId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_CuentaContable_Codigo] UNIQUE NONCLUSTERED 
(
	[Codigo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__CuentaCon__Permi__351DDF8C]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.CuentaContable') AND c.name = N'PermiteMovimiento')
ALTER TABLE [dbo].[CuentaContable] ADD  DEFAULT ((1)) FOR [PermiteMovimiento]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__CuentaCon__EsSis__361203C5]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.CuentaContable') AND c.name = N'EsSistema')
ALTER TABLE [dbo].[CuentaContable] ADD  DEFAULT ((0)) FOR [EsSistema]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__CuentaCon__Activ__370627FE]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.CuentaContable') AND c.name = N'Activo')
ALTER TABLE [dbo].[CuentaContable] ADD  DEFAULT ((1)) FOR [Activo]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Cuota]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Cuota](
	[CuotaId] [int] IDENTITY(1,1) NOT NULL,
	[CreditoId] [int] NOT NULL,
	[NumeroCuota] [int] NOT NULL,
	[FechaVencimiento] [date] NOT NULL,
	[MontoCuota] [decimal](18, 2) NOT NULL,
	[MontoPagado] [decimal](18, 2) NOT NULL,
	[Saldo] [decimal](18, 2) NOT NULL,
	[Estado] [nvarchar](12) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CuotaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Cuota]') AND name = N'IX_Cuota_Credito')
CREATE NONCLUSTERED INDEX [IX_Cuota_Credito] ON [dbo].[Cuota]
(
	[CreditoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Cuota]') AND name = N'IX_Cuota_Vencimiento')
CREATE NONCLUSTERED INDEX [IX_Cuota_Vencimiento] ON [dbo].[Cuota]
(
	[FechaVencimiento] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Cuota_Pagado]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Cuota] ADD  CONSTRAINT [DF_Cuota_Pagado]  DEFAULT ((0)) FOR [MontoPagado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Cuota_Estado]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Cuota] ADD  CONSTRAINT [DF_Cuota_Estado]  DEFAULT (N'Pendiente') FOR [Estado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Cuota_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[Cuota]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Cuota_Estado' AND parent_object_id = OBJECT_ID(N'dbo.Cuota'))
ALTER TABLE [dbo].[Cuota]  WITH CHECK ADD  CONSTRAINT [CK_Cuota_Estado] CHECK  (([Estado]=N'Vencida' OR [Estado]=N'Pagada' OR [Estado]=N'Parcial' OR [Estado]=N'Pendiente'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Cuota_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[Cuota]'))
ALTER TABLE [dbo].[Cuota] CHECK CONSTRAINT [CK_Cuota_Estado]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Empresa]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Empresa](
	[EmpresaId] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](150) NOT NULL,
	[RNC] [nvarchar](20) NULL,
	[Direccion] [nvarchar](250) NULL,
	[Telefono] [nvarchar](30) NULL,
	[Email] [nvarchar](150) NULL,
	[Moneda] [nvarchar](10) NOT NULL,
	[PorcentajeItbis] [decimal](9, 4) NOT NULL,
	[FechaCreacion] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[EmpresaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Empresa_Moneda]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Empresa] ADD  CONSTRAINT [DF_Empresa_Moneda]  DEFAULT (N'DOP') FOR [Moneda]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Empresa_Itbis]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Empresa] ADD  CONSTRAINT [DF_Empresa_Itbis]  DEFAULT ((18.0000)) FOR [PorcentajeItbis]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Empresa_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Empresa] ADD  CONSTRAINT [DF_Empresa_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Faltante]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Faltante](
	[FaltanteId] [int] IDENTITY(1,1) NOT NULL,
	[Descripcion] [nvarchar](200) NOT NULL,
	[VarianteId] [int] NULL,
	[CantidadDeseada] [int] NOT NULL,
	[Notas] [nvarchar](300) NULL,
	[Resuelto] [bit] NOT NULL,
	[FechaCreacion] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FaltanteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Faltante__Cantid__2EA5EC27]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Faltante') AND c.name = N'CantidadDeseada')
ALTER TABLE [dbo].[Faltante] ADD  DEFAULT ((1)) FOR [CantidadDeseada]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Faltante__Resuel__2F9A1060]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Faltante') AND c.name = N'Resuelto')
ALTER TABLE [dbo].[Faltante] ADD  DEFAULT ((0)) FOR [Resuelto]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Faltante__FechaC__308E3499]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Faltante') AND c.name = N'FechaCreacion')
ALTER TABLE [dbo].[Faltante] ADD  DEFAULT (sysdatetime()) FOR [FechaCreacion]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Garantia]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Garantia](
	[GarantiaId] [int] IDENTITY(1,1) NOT NULL,
	[VentaId] [int] NULL,
	[VentaDetalleId] [int] NULL,
	[ImeiId] [int] NULL,
	[ClienteId] [int] NULL,
	[FechaInicio] [date] NOT NULL,
	[MesesCobertura] [int] NOT NULL,
	[FechaVencimiento] [date] NOT NULL,
	[Condiciones] [nvarchar](400) NULL,
	[Estado] [nvarchar](12) NOT NULL,
	[FechaCreacion] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[GarantiaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Garantia]') AND name = N'IX_Garantia_Imei')
CREATE NONCLUSTERED INDEX [IX_Garantia_Imei] ON [dbo].[Garantia]
(
	[ImeiId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Garantia_Inicio]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Garantia] ADD  CONSTRAINT [DF_Garantia_Inicio]  DEFAULT (CONVERT([date],sysdatetime())) FOR [FechaInicio]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Garantia_Meses]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Garantia] ADD  CONSTRAINT [DF_Garantia_Meses]  DEFAULT ((3)) FOR [MesesCobertura]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Garantia_Estado]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Garantia] ADD  CONSTRAINT [DF_Garantia_Estado]  DEFAULT (N'Vigente') FOR [Estado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Garantia_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Garantia] ADD  CONSTRAINT [DF_Garantia_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Garantia_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[Garantia]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Garantia_Estado' AND parent_object_id = OBJECT_ID(N'dbo.Garantia'))
ALTER TABLE [dbo].[Garantia]  WITH CHECK ADD  CONSTRAINT [CK_Garantia_Estado] CHECK  (([Estado]=N'Anulada' OR [Estado]=N'Vencida' OR [Estado]=N'Vigente'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Garantia_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[Garantia]'))
ALTER TABLE [dbo].[Garantia] CHECK CONSTRAINT [CK_Garantia_Estado]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InventarioImei]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[InventarioImei](
	[ImeiId] [int] IDENTITY(1,1) NOT NULL,
	[Imei] [nvarchar](20) NOT NULL,
	[VarianteId] [int] NOT NULL,
	[SucursalId] [int] NOT NULL,
	[CompraId] [int] NULL,
	[PrecioCosto] [decimal](18, 2) NOT NULL,
	[Estado] [nvarchar](20) NOT NULL,
	[FechaIngreso] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ImeiId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Imei] UNIQUE NONCLUSTERED 
(
	[Imei] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InventarioImei]') AND name = N'IX_Imei_Estado')
CREATE NONCLUSTERED INDEX [IX_Imei_Estado] ON [dbo].[InventarioImei]
(
	[Estado] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InventarioImei]') AND name = N'IX_Imei_Variante')
CREATE NONCLUSTERED INDEX [IX_Imei_Variante] ON [dbo].[InventarioImei]
(
	[VarianteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Imei_Costo]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[InventarioImei] ADD  CONSTRAINT [DF_Imei_Costo]  DEFAULT ((0)) FOR [PrecioCosto]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Imei_Estado]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[InventarioImei] ADD  CONSTRAINT [DF_Imei_Estado]  DEFAULT (N'Disponible') FOR [Estado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Imei_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[InventarioImei] ADD  CONSTRAINT [DF_Imei_Fecha]  DEFAULT (sysdatetime()) FOR [FechaIngreso]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Imei_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[InventarioImei]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Imei_Estado' AND parent_object_id = OBJECT_ID(N'dbo.InventarioImei'))
ALTER TABLE [dbo].[InventarioImei]  WITH CHECK ADD  CONSTRAINT [CK_Imei_Estado] CHECK  (([Estado]=N'Reservado' OR [Estado]=N'Baja' OR [Estado]=N'Transferido' OR [Estado]=N'Devuelto' OR [Estado]=N'EnTaller' OR [Estado]=N'Vendido' OR [Estado]=N'Disponible'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Imei_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[InventarioImei]'))
ALTER TABLE [dbo].[InventarioImei] CHECK CONSTRAINT [CK_Imei_Estado]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogAuditoria]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[LogAuditoria](
	[LogId] [bigint] IDENTITY(1,1) NOT NULL,
	[UsuarioId] [int] NULL,
	[Entidad] [nvarchar](60) NOT NULL,
	[EntidadId] [nvarchar](40) NULL,
	[Accion] [nvarchar](30) NOT NULL,
	[Detalle] [nvarchar](1000) NULL,
	[Ip] [nvarchar](45) NULL,
	[Fecha] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[LogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[LogAuditoria]') AND name = N'IX_Log_Fecha')
CREATE NONCLUSTERED INDEX [IX_Log_Fecha] ON [dbo].[LogAuditoria]
(
	[Fecha] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Log_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[LogAuditoria] ADD  CONSTRAINT [DF_Log_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Marca]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Marca](
	[MarcaId] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](80) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MarcaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Marca_Nombre] UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MetodoPago]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[MetodoPago](
	[MetodoPagoId] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](40) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MetodoPagoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_MetodoPago_Nombre] UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MovimientoCaja]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[MovimientoCaja](
	[MovimientoCajaId] [bigint] IDENTITY(1,1) NOT NULL,
	[SesionCajaId] [int] NOT NULL,
	[Tipo] [nvarchar](10) NOT NULL,
	[Concepto] [nvarchar](150) NOT NULL,
	[Monto] [decimal](18, 2) NOT NULL,
	[Referencia] [nvarchar](100) NULL,
	[Fecha] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MovimientoCajaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_MovCaja_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[MovimientoCaja] ADD  CONSTRAINT [DF_MovCaja_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_MovCaja_Tipo]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoCaja]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_MovCaja_Tipo' AND parent_object_id = OBJECT_ID(N'dbo.MovimientoCaja'))
ALTER TABLE [dbo].[MovimientoCaja]  WITH CHECK ADD  CONSTRAINT [CK_MovCaja_Tipo] CHECK  (([Tipo]=N'Egreso' OR [Tipo]=N'Ingreso'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_MovCaja_Tipo]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoCaja]'))
ALTER TABLE [dbo].[MovimientoCaja] CHECK CONSTRAINT [CK_MovCaja_Tipo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[MovimientoInventario](
	[MovimientoId] [bigint] IDENTITY(1,1) NOT NULL,
	[ImeiId] [int] NOT NULL,
	[Tipo] [nvarchar](20) NOT NULL,
	[SucursalOrigen] [int] NULL,
	[SucursalDestino] [int] NULL,
	[Referencia] [nvarchar](100) NULL,
	[UsuarioId] [int] NULL,
	[Fecha] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MovimientoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]') AND name = N'IX_MovInv_Imei')
CREATE NONCLUSTERED INDEX [IX_MovInv_Imei] ON [dbo].[MovimientoInventario]
(
	[ImeiId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_MovInv_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[MovimientoInventario] ADD  CONSTRAINT [DF_MovInv_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_MovInv_Tipo]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_MovInv_Tipo' AND parent_object_id = OBJECT_ID(N'dbo.MovimientoInventario'))
ALTER TABLE [dbo].[MovimientoInventario]  WITH CHECK ADD  CONSTRAINT [CK_MovInv_Tipo] CHECK  (([Tipo]=N'Baja' OR [Tipo]=N'Taller' OR [Tipo]=N'Ajuste' OR [Tipo]=N'Devolucion' OR [Tipo]=N'Transferencia' OR [Tipo]=N'Venta' OR [Tipo]=N'Entrada'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_MovInv_Tipo]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]'))
ALTER TABLE [dbo].[MovimientoInventario] CHECK CONSTRAINT [CK_MovInv_Tipo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NotaCredito]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[NotaCredito](
	[NotaCreditoId] [int] IDENTITY(1,1) NOT NULL,
	[VentaId] [int] NOT NULL,
	[Ncf] [nvarchar](19) NULL,
	[NcfModificado] [nvarchar](19) NULL,
	[Fecha] [datetime] NOT NULL,
	[Monto] [decimal](18, 2) NOT NULL,
	[Itbis] [decimal](18, 2) NOT NULL,
	[Total] [decimal](18, 2) NOT NULL,
	[Motivo] [nvarchar](200) NULL,
	[UsuarioId] [int] NULL,
	[FechaRegistro] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotaCreditoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[NotaCredito]') AND name = N'IX_NotaCredito_Fecha')
CREATE NONCLUSTERED INDEX [IX_NotaCredito_Fecha] ON [dbo].[NotaCredito]
(
	[FechaRegistro] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__NotaCredi__Monto__5A4F643B]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.NotaCredito') AND c.name = N'Monto')
ALTER TABLE [dbo].[NotaCredito] ADD  DEFAULT ((0)) FOR [Monto]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__NotaCredi__Itbis__5B438874]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.NotaCredito') AND c.name = N'Itbis')
ALTER TABLE [dbo].[NotaCredito] ADD  DEFAULT ((0)) FOR [Itbis]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__NotaCredi__Total__5C37ACAD]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.NotaCredito') AND c.name = N'Total')
ALTER TABLE [dbo].[NotaCredito] ADD  DEFAULT ((0)) FOR [Total]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__NotaCredi__Fecha__5D2BD0E6]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.NotaCredito') AND c.name = N'FechaRegistro')
ALTER TABLE [dbo].[NotaCredito] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrdenTaller]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[OrdenTaller](
	[OrdenTallerId] [int] IDENTITY(1,1) NOT NULL,
	[NumeroOrden] [nvarchar](30) NULL,
	[SucursalId] [int] NOT NULL,
	[ClienteId] [int] NULL,
	[ImeiId] [int] NULL,
	[EquipoDescripcion] [nvarchar](200) NULL,
	[Diagnostico] [nvarchar](500) NULL,
	[TecnicoId] [int] NULL,
	[Estado] [nvarchar](15) NOT NULL,
	[Anticipo] [decimal](18, 2) NOT NULL,
	[CostoEstimado] [decimal](18, 2) NOT NULL,
	[CostoFinal] [decimal](18, 2) NULL,
	[ComisionTecnico] [decimal](18, 2) NOT NULL,
	[FechaRecepcion] [datetime2](0) NOT NULL,
	[FechaEntrega] [datetime2](0) NULL,
	[SesionCajaId] [int] NULL,
	[UsuarioRecepcion] [int] NULL,
	[SesionCajaEntrega] [int] NULL,
	[MetodoPagoAnticipoId] [int] NULL,
	[MetodoPagoEntregaId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[OrdenTallerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[OrdenTaller]') AND name = N'IX_Orden_Estado')
CREATE NONCLUSTERED INDEX [IX_Orden_Estado] ON [dbo].[OrdenTaller]
(
	[Estado] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Orden_Estado]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenTaller] ADD  CONSTRAINT [DF_Orden_Estado]  DEFAULT (N'Recibido') FOR [Estado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Orden_Anticipo]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenTaller] ADD  CONSTRAINT [DF_Orden_Anticipo]  DEFAULT ((0)) FOR [Anticipo]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Orden_CostoEst]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenTaller] ADD  CONSTRAINT [DF_Orden_CostoEst]  DEFAULT ((0)) FOR [CostoEstimado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Orden_Comision]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenTaller] ADD  CONSTRAINT [DF_Orden_Comision]  DEFAULT ((0)) FOR [ComisionTecnico]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Orden_FRecep]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenTaller] ADD  CONSTRAINT [DF_Orden_FRecep]  DEFAULT (sysdatetime()) FOR [FechaRecepcion]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Orden_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTaller]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Orden_Estado' AND parent_object_id = OBJECT_ID(N'dbo.OrdenTaller'))
ALTER TABLE [dbo].[OrdenTaller]  WITH CHECK ADD  CONSTRAINT [CK_Orden_Estado] CHECK  (([Estado]=N'Cancelado' OR [Estado]=N'Entregado' OR [Estado]=N'Reparado' OR [Estado]=N'EnReparacion' OR [Estado]=N'Recibido'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Orden_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTaller]'))
ALTER TABLE [dbo].[OrdenTaller] CHECK CONSTRAINT [CK_Orden_Estado]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrdenTallerFoto]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[OrdenTallerFoto](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OrdenTallerId] [int] NOT NULL,
	[Url] [nvarchar](400) NOT NULL,
	[Fecha] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_OrdFoto_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenTallerFoto] ADD  CONSTRAINT [DF_OrdFoto_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrdenTallerRepuesto]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[OrdenTallerRepuesto](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OrdenTallerId] [int] NOT NULL,
	[VarianteId] [int] NULL,
	[Descripcion] [nvarchar](200) NOT NULL,
	[Cantidad] [int] NOT NULL,
	[Costo] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_OrdRep_Cant]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenTallerRepuesto] ADD  CONSTRAINT [DF_OrdRep_Cant]  DEFAULT ((1)) FOR [Cantidad]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_OrdRep_Costo]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenTallerRepuesto] ADD  CONSTRAINT [DF_OrdRep_Costo]  DEFAULT ((0)) FOR [Costo]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PadronRnc]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PadronRnc](
	[Rnc] [nvarchar](11) NOT NULL,
	[Nombre] [nvarchar](200) NOT NULL,
	[Estado] [nvarchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[Rnc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PagoCredito]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PagoCredito](
	[PagoCreditoId] [int] IDENTITY(1,1) NOT NULL,
	[CreditoId] [int] NOT NULL,
	[CuotaId] [int] NULL,
	[MetodoPagoId] [int] NULL,
	[SesionCajaId] [int] NULL,
	[UsuarioId] [int] NULL,
	[Monto] [decimal](18, 2) NOT NULL,
	[NumeroRecibo] [nvarchar](30) NULL,
	[Fecha] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PagoCreditoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_PagoCred_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PagoCredito] ADD  CONSTRAINT [DF_PagoCred_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PagoEmpleado]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PagoEmpleado](
	[PagoEmpleadoId] [int] IDENTITY(1,1) NOT NULL,
	[EmpleadoId] [int] NOT NULL,
	[Tipo] [nvarchar](20) NOT NULL,
	[Monto] [decimal](18, 2) NOT NULL,
	[Periodo] [nvarchar](40) NULL,
	[Notas] [nvarchar](300) NULL,
	[RegistradoPor] [int] NULL,
	[Fecha] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PagoEmpleadoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__PagoEmplea__Tipo__41B8C09B]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.PagoEmpleado') AND c.name = N'Tipo')
ALTER TABLE [dbo].[PagoEmpleado] ADD  DEFAULT ('Salario') FOR [Tipo]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__PagoEmple__Fecha__42ACE4D4]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.PagoEmpleado') AND c.name = N'Fecha')
ALTER TABLE [dbo].[PagoEmpleado] ADD  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PagoProveedor]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PagoProveedor](
	[PagoProveedorId] [int] IDENTITY(1,1) NOT NULL,
	[ProveedorId] [int] NOT NULL,
	[CompraId] [int] NULL,
	[Monto] [decimal](18, 2) NOT NULL,
	[Fecha] [datetime2](0) NOT NULL,
	[Referencia] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[PagoProveedorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_PagoProv_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[PagoProveedor] ADD  CONSTRAINT [DF_PagoProv_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Permiso]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Permiso](
	[PermisoId] [int] IDENTITY(1,1) NOT NULL,
	[Codigo] [nvarchar](80) NOT NULL,
	[Descripcion] [nvarchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[PermisoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Permiso_Codigo] UNIQUE NONCLUSTERED 
(
	[Codigo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Producto]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Producto](
	[ProductoId] [int] IDENTITY(1,1) NOT NULL,
	[MarcaId] [int] NULL,
	[CategoriaId] [int] NULL,
	[Nombre] [nvarchar](150) NOT NULL,
	[Descripcion] [nvarchar](300) NULL,
	[Serializado] [bit] NOT NULL,
	[Activo] [bit] NOT NULL,
	[FechaCreacion] [datetime2](0) NOT NULL,
	[Provisional] [bit] NOT NULL,
	[ImagenUrl] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[ProductoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Producto_Serial]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Producto] ADD  CONSTRAINT [DF_Producto_Serial]  DEFAULT ((1)) FOR [Serializado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Producto_Activo]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Producto] ADD  CONSTRAINT [DF_Producto_Activo]  DEFAULT ((1)) FOR [Activo]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Producto_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Producto] ADD  CONSTRAINT [DF_Producto_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Producto_Provisional]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Producto] ADD  CONSTRAINT [DF_Producto_Provisional]  DEFAULT ((0)) FOR [Provisional]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProductoVariante]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ProductoVariante](
	[VarianteId] [int] IDENTITY(1,1) NOT NULL,
	[ProductoId] [int] NOT NULL,
	[Color] [nvarchar](40) NULL,
	[Almacenamiento] [nvarchar](40) NULL,
	[Condicion] [nvarchar](30) NULL,
	[CodigoBarras] [nvarchar](60) NULL,
	[PrecioVenta] [decimal](18, 2) NOT NULL,
	[PrecioCosto] [decimal](18, 2) NOT NULL,
	[StockNoSerial] [int] NOT NULL,
	[Activo] [bit] NOT NULL,
	[StockMinimo] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[VarianteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductoVariante]') AND name = N'IX_Variante_Producto')
CREATE NONCLUSTERED INDEX [IX_Variante_Producto] ON [dbo].[ProductoVariante]
(
	[ProductoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductoVariante]') AND name = N'UX_ProductoVariante_CodigoBarras')
CREATE UNIQUE NONCLUSTERED INDEX [UX_ProductoVariante_CodigoBarras] ON [dbo].[ProductoVariante]
(
	[CodigoBarras] ASC
)
WHERE ([CodigoBarras] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Variante_PVenta]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[ProductoVariante] ADD  CONSTRAINT [DF_Variante_PVenta]  DEFAULT ((0)) FOR [PrecioVenta]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Variante_PCosto]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[ProductoVariante] ADD  CONSTRAINT [DF_Variante_PCosto]  DEFAULT ((0)) FOR [PrecioCosto]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Variante_Stock]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[ProductoVariante] ADD  CONSTRAINT [DF_Variante_Stock]  DEFAULT ((0)) FOR [StockNoSerial]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Variante_Activo]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[ProductoVariante] ADD  CONSTRAINT [DF_Variante_Activo]  DEFAULT ((1)) FOR [Activo]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_ProductoVariante_StockMinimo]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[ProductoVariante] ADD  CONSTRAINT [DF_ProductoVariante_StockMinimo]  DEFAULT ((0)) FOR [StockMinimo]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Proveedor]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Proveedor](
	[ProveedorId] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](150) NOT NULL,
	[RNC] [nvarchar](20) NULL,
	[Telefono] [nvarchar](30) NULL,
	[Email] [nvarchar](150) NULL,
	[Direccion] [nvarchar](250) NULL,
	[Balance] [decimal](18, 2) NOT NULL,
	[Activo] [bit] NOT NULL,
	[FechaCreacion] [datetime2](0) NOT NULL,
	[CondicionPago] [nvarchar](20) NOT NULL,
	[DiasCredito] [int] NOT NULL,
	[NotaCondicion] [nvarchar](300) NULL,
PRIMARY KEY CLUSTERED 
(
	[ProveedorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Proveedor_Balance]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Proveedor] ADD  CONSTRAINT [DF_Proveedor_Balance]  DEFAULT ((0)) FOR [Balance]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Proveedor_Activo]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Proveedor] ADD  CONSTRAINT [DF_Proveedor_Activo]  DEFAULT ((1)) FOR [Activo]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Proveedor_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Proveedor] ADD  CONSTRAINT [DF_Proveedor_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Proveedor_CondicionPago]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Proveedor] ADD  CONSTRAINT [DF_Proveedor_CondicionPago]  DEFAULT (N'Contado') FOR [CondicionPago]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Proveedor_DiasCredito]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Proveedor] ADD  CONSTRAINT [DF_Proveedor_DiasCredito]  DEFAULT ((0)) FOR [DiasCredito]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RefreshToken]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[RefreshToken](
	[RefreshTokenId] [bigint] IDENTITY(1,1) NOT NULL,
	[UsuarioId] [int] NOT NULL,
	[Token] [nvarchar](400) NOT NULL,
	[Expira] [datetime2](0) NOT NULL,
	[Revocado] [bit] NOT NULL,
	[FechaCreacion] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RefreshTokenId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_RefreshToken_Rev]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[RefreshToken] ADD  CONSTRAINT [DF_RefreshToken_Rev]  DEFAULT ((0)) FOR [Revocado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_RefreshToken_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[RefreshToken] ADD  CONSTRAINT [DF_RefreshToken_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Rol]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Rol](
	[RolId] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](60) NOT NULL,
	[Descripcion] [nvarchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[RolId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Rol_Nombre] UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolPermiso]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[RolPermiso](
	[RolId] [int] NOT NULL,
	[PermisoId] [int] NOT NULL,
 CONSTRAINT [PK_RolPermiso] PRIMARY KEY CLUSTERED 
(
	[RolId] ASC,
	[PermisoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Secuencia]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Secuencia](
	[Nombre] [nvarchar](30) NOT NULL,
	[Prefijo] [nvarchar](15) NOT NULL,
	[Valor] [int] NOT NULL,
	[Longitud] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Secuencia__Prefi__1B9317B3]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Secuencia') AND c.name = N'Prefijo')
ALTER TABLE [dbo].[Secuencia] ADD  DEFAULT (N'') FOR [Prefijo]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Secuencia__Valor__1C873BEC]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Secuencia') AND c.name = N'Valor')
ALTER TABLE [dbo].[Secuencia] ADD  DEFAULT ((0)) FOR [Valor]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Secuencia__Longi__1D7B6025]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Secuencia') AND c.name = N'Longitud')
ALTER TABLE [dbo].[Secuencia] ADD  DEFAULT ((8)) FOR [Longitud]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecuenciaNcf]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SecuenciaNcf](
	[SecuenciaNcfId] [int] IDENTITY(1,1) NOT NULL,
	[TipoComprobante] [nvarchar](2) NOT NULL,
	[Serie] [nvarchar](2) NOT NULL,
	[Secuencia] [int] NOT NULL,
	[Hasta] [int] NOT NULL,
	[Vencimiento] [date] NULL,
	[Activo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SecuenciaNcfId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_SecuenciaNcf_Tipo] UNIQUE NONCLUSTERED 
(
	[TipoComprobante] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Secuencia__Serie__7BE56230]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SecuenciaNcf') AND c.name = N'Serie')
ALTER TABLE [dbo].[SecuenciaNcf] ADD  DEFAULT ('B') FOR [Serie]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Secuencia__Secue__7CD98669]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SecuenciaNcf') AND c.name = N'Secuencia')
ALTER TABLE [dbo].[SecuenciaNcf] ADD  DEFAULT ((1)) FOR [Secuencia]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Secuencia__Hasta__7DCDAAA2]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SecuenciaNcf') AND c.name = N'Hasta')
ALTER TABLE [dbo].[SecuenciaNcf] ADD  DEFAULT ((0)) FOR [Hasta]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Secuencia__Activ__7EC1CEDB]') AND type = 'D')
BEGIN
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SecuenciaNcf') AND c.name = N'Activo')
ALTER TABLE [dbo].[SecuenciaNcf] ADD  DEFAULT ((0)) FOR [Activo]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SesionCaja]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SesionCaja](
	[SesionCajaId] [int] IDENTITY(1,1) NOT NULL,
	[SucursalId] [int] NOT NULL,
	[UsuarioApertura] [int] NOT NULL,
	[UsuarioCierre] [int] NULL,
	[MontoApertura] [decimal](18, 2) NOT NULL,
	[MontoCierre] [decimal](18, 2) NULL,
	[Diferencia] [decimal](18, 2) NULL,
	[Estado] [nvarchar](15) NOT NULL,
	[FechaApertura] [datetime2](0) NOT NULL,
	[FechaCierre] [datetime2](0) NULL,
PRIMARY KEY CLUSTERED 
(
	[SesionCajaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Sesion_Apertura]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[SesionCaja] ADD  CONSTRAINT [DF_Sesion_Apertura]  DEFAULT ((0)) FOR [MontoApertura]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Sesion_Estado]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[SesionCaja] ADD  CONSTRAINT [DF_Sesion_Estado]  DEFAULT (N'Abierta') FOR [Estado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Sesion_FApertura]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[SesionCaja] ADD  CONSTRAINT [DF_Sesion_FApertura]  DEFAULT (sysdatetime()) FOR [FechaApertura]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Sesion_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[SesionCaja]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Sesion_Estado' AND parent_object_id = OBJECT_ID(N'dbo.SesionCaja'))
ALTER TABLE [dbo].[SesionCaja]  WITH CHECK ADD  CONSTRAINT [CK_Sesion_Estado] CHECK  (([Estado]=N'Cerrada' OR [Estado]=N'Abierta'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Sesion_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[SesionCaja]'))
ALTER TABLE [dbo].[SesionCaja] CHECK CONSTRAINT [CK_Sesion_Estado]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Sucursal]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Sucursal](
	[SucursalId] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](120) NOT NULL,
	[Direccion] [nvarchar](250) NULL,
	[Telefono] [nvarchar](30) NULL,
	[Activa] [bit] NOT NULL,
	[FechaCreacion] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SucursalId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Sucursal_Activa]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Sucursal] ADD  CONSTRAINT [DF_Sucursal_Activa]  DEFAULT ((1)) FOR [Activa]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Sucursal_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Sucursal] ADD  CONSTRAINT [DF_Sucursal_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Usuario]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Usuario](
	[UsuarioId] [int] IDENTITY(1,1) NOT NULL,
	[NombreUsuario] [nvarchar](60) NOT NULL,
	[NombreCompleto] [nvarchar](150) NOT NULL,
	[Email] [nvarchar](150) NULL,
	[HashContrasena] [nvarchar](256) NOT NULL,
	[RolId] [int] NOT NULL,
	[SucursalId] [int] NULL,
	[Activo] [bit] NOT NULL,
	[UltimoAcceso] [datetime2](0) NULL,
	[FechaCreacion] [datetime2](0) NOT NULL,
	[IntentosFallidos] [int] NOT NULL,
	[BloqueadoHasta] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[UsuarioId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Usuario_NombreUsuario] UNIQUE NONCLUSTERED 
(
	[NombreUsuario] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Usuario_Activo]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Usuario] ADD  CONSTRAINT [DF_Usuario_Activo]  DEFAULT ((1)) FOR [Activo]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Usuario_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Usuario] ADD  CONSTRAINT [DF_Usuario_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Usuario_IntentosFallidos]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Usuario] ADD  CONSTRAINT [DF_Usuario_IntentosFallidos]  DEFAULT ((0)) FOR [IntentosFallidos]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Venta]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Venta](
	[VentaId] [int] IDENTITY(1,1) NOT NULL,
	[NumeroFactura] [nvarchar](30) NULL,
	[SucursalId] [int] NOT NULL,
	[ClienteId] [int] NULL,
	[UsuarioId] [int] NOT NULL,
	[SesionCajaId] [int] NULL,
	[Fecha] [datetime2](0) NOT NULL,
	[Subtotal] [decimal](18, 2) NOT NULL,
	[Descuento] [decimal](18, 2) NOT NULL,
	[Impuesto] [decimal](18, 2) NOT NULL,
	[Total] [decimal](18, 2) NOT NULL,
	[EsCredito] [bit] NOT NULL,
	[Estado] [nvarchar](15) NOT NULL,
	[Ncf] [nvarchar](19) NULL,
PRIMARY KEY CLUSTERED 
(
	[VentaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Venta]') AND name = N'IX_Venta_Cliente')
CREATE NONCLUSTERED INDEX [IX_Venta_Cliente] ON [dbo].[Venta]
(
	[ClienteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Venta]') AND name = N'IX_Venta_Fecha')
CREATE NONCLUSTERED INDEX [IX_Venta_Fecha] ON [dbo].[Venta]
(
	[Fecha] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Venta_Fecha]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Venta_Subtotal]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Subtotal]  DEFAULT ((0)) FOR [Subtotal]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Venta_Descuento]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Descuento]  DEFAULT ((0)) FOR [Descuento]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Venta_Impuesto]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Impuesto]  DEFAULT ((0)) FOR [Impuesto]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Venta_Total]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Total]  DEFAULT ((0)) FOR [Total]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Venta_EsCredito]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_EsCredito]  DEFAULT ((0)) FOR [EsCredito]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Venta_Estado]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Estado]  DEFAULT (N'Completada') FOR [Estado]
END

GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Venta_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Venta_Estado' AND parent_object_id = OBJECT_ID(N'dbo.Venta'))
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [CK_Venta_Estado] CHECK  (([Estado]=N'Devuelta' OR [Estado]=N'Anulada' OR [Estado]=N'Completada'))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Venta_Estado]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [CK_Venta_Estado]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VentaDetalle]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[VentaDetalle](
	[VentaDetalleId] [int] IDENTITY(1,1) NOT NULL,
	[VentaId] [int] NOT NULL,
	[ImeiId] [int] NULL,
	[VarianteId] [int] NOT NULL,
	[Descripcion] [nvarchar](200) NULL,
	[Cantidad] [int] NOT NULL,
	[PrecioUnitario] [decimal](18, 2) NOT NULL,
	[Descuento] [decimal](18, 2) NOT NULL,
	[Impuesto] [decimal](18, 2) NOT NULL,
	[Total] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[VentaDetalleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[VentaDetalle]') AND name = N'IX_VtaDet_Venta')
CREATE NONCLUSTERED INDEX [IX_VtaDet_Venta] ON [dbo].[VentaDetalle]
(
	[VentaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_VtaDet_Cant]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[VentaDetalle] ADD  CONSTRAINT [DF_VtaDet_Cant]  DEFAULT ((1)) FOR [Cantidad]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_VtaDet_Desc]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[VentaDetalle] ADD  CONSTRAINT [DF_VtaDet_Desc]  DEFAULT ((0)) FOR [Descuento]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_VtaDet_Imp]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[VentaDetalle] ADD  CONSTRAINT [DF_VtaDet_Imp]  DEFAULT ((0)) FOR [Impuesto]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VentaPago]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[VentaPago](
	[VentaPagoId] [int] IDENTITY(1,1) NOT NULL,
	[VentaId] [int] NOT NULL,
	[MetodoPagoId] [int] NOT NULL,
	[Monto] [decimal](18, 2) NOT NULL,
	[Referencia] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[VentaPagoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

/* ============================================================================ */
/*  LLAVES FORANEAS */
/* ============================================================================ */
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AbonoApartado_Apartado]') AND parent_object_id = OBJECT_ID(N'[dbo].[AbonoApartado]'))
ALTER TABLE [dbo].[AbonoApartado]  WITH CHECK ADD  CONSTRAINT [FK_AbonoApartado_Apartado] FOREIGN KEY([ApartadoId])
REFERENCES [dbo].[Apartado] ([ApartadoId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AbonoApartado_Apartado]') AND parent_object_id = OBJECT_ID(N'[dbo].[AbonoApartado]'))
ALTER TABLE [dbo].[AbonoApartado] CHECK CONSTRAINT [FK_AbonoApartado_Apartado]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Apartado_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[Apartado]'))
ALTER TABLE [dbo].[Apartado]  WITH CHECK ADD  CONSTRAINT [FK_Apartado_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Apartado_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[Apartado]'))
ALTER TABLE [dbo].[Apartado] CHECK CONSTRAINT [FK_Apartado_Cliente]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Apartado_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[Apartado]'))
ALTER TABLE [dbo].[Apartado]  WITH CHECK ADD  CONSTRAINT [FK_Apartado_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Apartado_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[Apartado]'))
ALTER TABLE [dbo].[Apartado] CHECK CONSTRAINT [FK_Apartado_Imei]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Apartado_Sucursal_Fix]') AND parent_object_id = OBJECT_ID(N'[dbo].[Apartado]'))
ALTER TABLE [dbo].[Apartado]  WITH CHECK ADD  CONSTRAINT [FK_Apartado_Sucursal_Fix] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Apartado_Sucursal_Fix]') AND parent_object_id = OBJECT_ID(N'[dbo].[Apartado]'))
ALTER TABLE [dbo].[Apartado] CHECK CONSTRAINT [FK_Apartado_Sucursal_Fix]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Apartado_Usuario_Fix]') AND parent_object_id = OBJECT_ID(N'[dbo].[Apartado]'))
ALTER TABLE [dbo].[Apartado]  WITH CHECK ADD  CONSTRAINT [FK_Apartado_Usuario_Fix] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Apartado_Usuario_Fix]') AND parent_object_id = OBJECT_ID(N'[dbo].[Apartado]'))
ALTER TABLE [dbo].[Apartado] CHECK CONSTRAINT [FK_Apartado_Usuario_Fix]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Apartado_Variante]') AND parent_object_id = OBJECT_ID(N'[dbo].[Apartado]'))
ALTER TABLE [dbo].[Apartado]  WITH CHECK ADD  CONSTRAINT [FK_Apartado_Variante] FOREIGN KEY([VarianteId])
REFERENCES [dbo].[ProductoVariante] ([VarianteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Apartado_Variante]') AND parent_object_id = OBJECT_ID(N'[dbo].[Apartado]'))
ALTER TABLE [dbo].[Apartado] CHECK CONSTRAINT [FK_Apartado_Variante]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AsientoDetalle_Asiento]') AND parent_object_id = OBJECT_ID(N'[dbo].[AsientoDetalle]'))
ALTER TABLE [dbo].[AsientoDetalle]  WITH CHECK ADD  CONSTRAINT [FK_AsientoDetalle_Asiento] FOREIGN KEY([AsientoContableId])
REFERENCES [dbo].[AsientoContable] ([AsientoContableId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AsientoDetalle_Asiento]') AND parent_object_id = OBJECT_ID(N'[dbo].[AsientoDetalle]'))
ALTER TABLE [dbo].[AsientoDetalle] CHECK CONSTRAINT [FK_AsientoDetalle_Asiento]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AsientoDetalle_Cuenta]') AND parent_object_id = OBJECT_ID(N'[dbo].[AsientoDetalle]'))
ALTER TABLE [dbo].[AsientoDetalle]  WITH CHECK ADD  CONSTRAINT [FK_AsientoDetalle_Cuenta] FOREIGN KEY([CuentaContableId])
REFERENCES [dbo].[CuentaContable] ([CuentaContableId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AsientoDetalle_Cuenta]') AND parent_object_id = OBJECT_ID(N'[dbo].[AsientoDetalle]'))
ALTER TABLE [dbo].[AsientoDetalle] CHECK CONSTRAINT [FK_AsientoDetalle_Cuenta]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Caso_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [FK_Caso_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Caso_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [FK_Caso_Cliente]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Caso_Garantia]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [FK_Caso_Garantia] FOREIGN KEY([GarantiaId])
REFERENCES [dbo].[Garantia] ([GarantiaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Caso_Garantia]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [FK_Caso_Garantia]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Caso_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [FK_Caso_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Caso_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [FK_Caso_Imei]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Caso_ImeiReemp]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [FK_Caso_ImeiReemp] FOREIGN KEY([ImeiReemplazoId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Caso_ImeiReemp]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [FK_Caso_ImeiReemp]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Caso_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [FK_Caso_Orden] FOREIGN KEY([OrdenTallerId])
REFERENCES [dbo].[OrdenTaller] ([OrdenTallerId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Caso_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[CasoGarantia]'))
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [FK_Caso_Orden]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Comision_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[Comision]'))
ALTER TABLE [dbo].[Comision]  WITH CHECK ADD  CONSTRAINT [FK_Comision_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Comision_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[Comision]'))
ALTER TABLE [dbo].[Comision] CHECK CONSTRAINT [FK_Comision_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Comision_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[Comision]'))
ALTER TABLE [dbo].[Comision]  WITH CHECK ADD  CONSTRAINT [FK_Comision_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Comision_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[Comision]'))
ALTER TABLE [dbo].[Comision] CHECK CONSTRAINT [FK_Comision_Venta]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Compra_MetodoPago]') AND parent_object_id = OBJECT_ID(N'[dbo].[Compra]'))
ALTER TABLE [dbo].[Compra]  WITH CHECK ADD  CONSTRAINT [FK_Compra_MetodoPago] FOREIGN KEY([MetodoPagoId])
REFERENCES [dbo].[MetodoPago] ([MetodoPagoId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Compra_MetodoPago]') AND parent_object_id = OBJECT_ID(N'[dbo].[Compra]'))
ALTER TABLE [dbo].[Compra] CHECK CONSTRAINT [FK_Compra_MetodoPago]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Compra_Proveedor]') AND parent_object_id = OBJECT_ID(N'[dbo].[Compra]'))
ALTER TABLE [dbo].[Compra]  WITH CHECK ADD  CONSTRAINT [FK_Compra_Proveedor] FOREIGN KEY([ProveedorId])
REFERENCES [dbo].[Proveedor] ([ProveedorId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Compra_Proveedor]') AND parent_object_id = OBJECT_ID(N'[dbo].[Compra]'))
ALTER TABLE [dbo].[Compra] CHECK CONSTRAINT [FK_Compra_Proveedor]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Compra_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[Compra]'))
ALTER TABLE [dbo].[Compra]  WITH CHECK ADD  CONSTRAINT [FK_Compra_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Compra_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[Compra]'))
ALTER TABLE [dbo].[Compra] CHECK CONSTRAINT [FK_Compra_Sucursal]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ComprobanteAnulado_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[ComprobanteAnulado]'))
ALTER TABLE [dbo].[ComprobanteAnulado]  WITH CHECK ADD  CONSTRAINT [FK_ComprobanteAnulado_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ComprobanteAnulado_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[ComprobanteAnulado]'))
ALTER TABLE [dbo].[ComprobanteAnulado] CHECK CONSTRAINT [FK_ComprobanteAnulado_Venta]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ContactoProveedor_Proveedor]') AND parent_object_id = OBJECT_ID(N'[dbo].[ContactoProveedor]'))
ALTER TABLE [dbo].[ContactoProveedor]  WITH CHECK ADD  CONSTRAINT [FK_ContactoProveedor_Proveedor] FOREIGN KEY([ProveedorId])
REFERENCES [dbo].[Proveedor] ([ProveedorId])
ON DELETE CASCADE
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ContactoProveedor_Proveedor]') AND parent_object_id = OBJECT_ID(N'[dbo].[ContactoProveedor]'))
ALTER TABLE [dbo].[ContactoProveedor] CHECK CONSTRAINT [FK_ContactoProveedor_Proveedor]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Credito_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[Credito]'))
ALTER TABLE [dbo].[Credito]  WITH CHECK ADD  CONSTRAINT [FK_Credito_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Credito_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[Credito]'))
ALTER TABLE [dbo].[Credito] CHECK CONSTRAINT [FK_Credito_Cliente]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Credito_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[Credito]'))
ALTER TABLE [dbo].[Credito]  WITH CHECK ADD  CONSTRAINT [FK_Credito_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Credito_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[Credito]'))
ALTER TABLE [dbo].[Credito] CHECK CONSTRAINT [FK_Credito_Venta]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CuentaContable_Padre]') AND parent_object_id = OBJECT_ID(N'[dbo].[CuentaContable]'))
ALTER TABLE [dbo].[CuentaContable]  WITH CHECK ADD  CONSTRAINT [FK_CuentaContable_Padre] FOREIGN KEY([CuentaPadreId])
REFERENCES [dbo].[CuentaContable] ([CuentaContableId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CuentaContable_Padre]') AND parent_object_id = OBJECT_ID(N'[dbo].[CuentaContable]'))
ALTER TABLE [dbo].[CuentaContable] CHECK CONSTRAINT [FK_CuentaContable_Padre]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Cuota_Credito]') AND parent_object_id = OBJECT_ID(N'[dbo].[Cuota]'))
ALTER TABLE [dbo].[Cuota]  WITH CHECK ADD  CONSTRAINT [FK_Cuota_Credito] FOREIGN KEY([CreditoId])
REFERENCES [dbo].[Credito] ([CreditoId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Cuota_Credito]') AND parent_object_id = OBJECT_ID(N'[dbo].[Cuota]'))
ALTER TABLE [dbo].[Cuota] CHECK CONSTRAINT [FK_Cuota_Credito]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Faltante_Variante]') AND parent_object_id = OBJECT_ID(N'[dbo].[Faltante]'))
ALTER TABLE [dbo].[Faltante]  WITH CHECK ADD  CONSTRAINT [FK_Faltante_Variante] FOREIGN KEY([VarianteId])
REFERENCES [dbo].[ProductoVariante] ([VarianteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Faltante_Variante]') AND parent_object_id = OBJECT_ID(N'[dbo].[Faltante]'))
ALTER TABLE [dbo].[Faltante] CHECK CONSTRAINT [FK_Faltante_Variante]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Garantia_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[Garantia]'))
ALTER TABLE [dbo].[Garantia]  WITH CHECK ADD  CONSTRAINT [FK_Garantia_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Garantia_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[Garantia]'))
ALTER TABLE [dbo].[Garantia] CHECK CONSTRAINT [FK_Garantia_Cliente]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Garantia_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[Garantia]'))
ALTER TABLE [dbo].[Garantia]  WITH CHECK ADD  CONSTRAINT [FK_Garantia_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Garantia_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[Garantia]'))
ALTER TABLE [dbo].[Garantia] CHECK CONSTRAINT [FK_Garantia_Imei]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Garantia_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[Garantia]'))
ALTER TABLE [dbo].[Garantia]  WITH CHECK ADD  CONSTRAINT [FK_Garantia_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Garantia_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[Garantia]'))
ALTER TABLE [dbo].[Garantia] CHECK CONSTRAINT [FK_Garantia_Venta]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Garantia_VtaDet]') AND parent_object_id = OBJECT_ID(N'[dbo].[Garantia]'))
ALTER TABLE [dbo].[Garantia]  WITH CHECK ADD  CONSTRAINT [FK_Garantia_VtaDet] FOREIGN KEY([VentaDetalleId])
REFERENCES [dbo].[VentaDetalle] ([VentaDetalleId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Garantia_VtaDet]') AND parent_object_id = OBJECT_ID(N'[dbo].[Garantia]'))
ALTER TABLE [dbo].[Garantia] CHECK CONSTRAINT [FK_Garantia_VtaDet]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Imei_Compra]') AND parent_object_id = OBJECT_ID(N'[dbo].[InventarioImei]'))
ALTER TABLE [dbo].[InventarioImei]  WITH CHECK ADD  CONSTRAINT [FK_Imei_Compra] FOREIGN KEY([CompraId])
REFERENCES [dbo].[Compra] ([CompraId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Imei_Compra]') AND parent_object_id = OBJECT_ID(N'[dbo].[InventarioImei]'))
ALTER TABLE [dbo].[InventarioImei] CHECK CONSTRAINT [FK_Imei_Compra]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Imei_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[InventarioImei]'))
ALTER TABLE [dbo].[InventarioImei]  WITH CHECK ADD  CONSTRAINT [FK_Imei_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Imei_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[InventarioImei]'))
ALTER TABLE [dbo].[InventarioImei] CHECK CONSTRAINT [FK_Imei_Sucursal]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Imei_Sucursal_Fix]') AND parent_object_id = OBJECT_ID(N'[dbo].[InventarioImei]'))
ALTER TABLE [dbo].[InventarioImei]  WITH CHECK ADD  CONSTRAINT [FK_Imei_Sucursal_Fix] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Imei_Sucursal_Fix]') AND parent_object_id = OBJECT_ID(N'[dbo].[InventarioImei]'))
ALTER TABLE [dbo].[InventarioImei] CHECK CONSTRAINT [FK_Imei_Sucursal_Fix]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Imei_Variante]') AND parent_object_id = OBJECT_ID(N'[dbo].[InventarioImei]'))
ALTER TABLE [dbo].[InventarioImei]  WITH CHECK ADD  CONSTRAINT [FK_Imei_Variante] FOREIGN KEY([VarianteId])
REFERENCES [dbo].[ProductoVariante] ([VarianteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Imei_Variante]') AND parent_object_id = OBJECT_ID(N'[dbo].[InventarioImei]'))
ALTER TABLE [dbo].[InventarioImei] CHECK CONSTRAINT [FK_Imei_Variante]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Log_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[LogAuditoria]'))
ALTER TABLE [dbo].[LogAuditoria]  WITH CHECK ADD  CONSTRAINT [FK_Log_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Log_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[LogAuditoria]'))
ALTER TABLE [dbo].[LogAuditoria] CHECK CONSTRAINT [FK_Log_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_MovCaja_Sesion]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoCaja]'))
ALTER TABLE [dbo].[MovimientoCaja]  WITH CHECK ADD  CONSTRAINT [FK_MovCaja_Sesion] FOREIGN KEY([SesionCajaId])
REFERENCES [dbo].[SesionCaja] ([SesionCajaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_MovCaja_Sesion]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoCaja]'))
ALTER TABLE [dbo].[MovimientoCaja] CHECK CONSTRAINT [FK_MovCaja_Sesion]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_MovInv_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]'))
ALTER TABLE [dbo].[MovimientoInventario]  WITH CHECK ADD  CONSTRAINT [FK_MovInv_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_MovInv_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]'))
ALTER TABLE [dbo].[MovimientoInventario] CHECK CONSTRAINT [FK_MovInv_Imei]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_MovInv_SucDest]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]'))
ALTER TABLE [dbo].[MovimientoInventario]  WITH CHECK ADD  CONSTRAINT [FK_MovInv_SucDest] FOREIGN KEY([SucursalDestino])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_MovInv_SucDest]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]'))
ALTER TABLE [dbo].[MovimientoInventario] CHECK CONSTRAINT [FK_MovInv_SucDest]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_MovInv_SucOrig]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]'))
ALTER TABLE [dbo].[MovimientoInventario]  WITH CHECK ADD  CONSTRAINT [FK_MovInv_SucOrig] FOREIGN KEY([SucursalOrigen])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_MovInv_SucOrig]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]'))
ALTER TABLE [dbo].[MovimientoInventario] CHECK CONSTRAINT [FK_MovInv_SucOrig]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_MovInv_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]'))
ALTER TABLE [dbo].[MovimientoInventario]  WITH CHECK ADD  CONSTRAINT [FK_MovInv_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_MovInv_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[MovimientoInventario]'))
ALTER TABLE [dbo].[MovimientoInventario] CHECK CONSTRAINT [FK_MovInv_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_NotaCredito_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[NotaCredito]'))
ALTER TABLE [dbo].[NotaCredito]  WITH CHECK ADD  CONSTRAINT [FK_NotaCredito_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_NotaCredito_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[NotaCredito]'))
ALTER TABLE [dbo].[NotaCredito] CHECK CONSTRAINT [FK_NotaCredito_Venta]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Orden_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTaller]'))
ALTER TABLE [dbo].[OrdenTaller]  WITH CHECK ADD  CONSTRAINT [FK_Orden_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Orden_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTaller]'))
ALTER TABLE [dbo].[OrdenTaller] CHECK CONSTRAINT [FK_Orden_Cliente]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Orden_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTaller]'))
ALTER TABLE [dbo].[OrdenTaller]  WITH CHECK ADD  CONSTRAINT [FK_Orden_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Orden_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTaller]'))
ALTER TABLE [dbo].[OrdenTaller] CHECK CONSTRAINT [FK_Orden_Imei]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Orden_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTaller]'))
ALTER TABLE [dbo].[OrdenTaller]  WITH CHECK ADD  CONSTRAINT [FK_Orden_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Orden_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTaller]'))
ALTER TABLE [dbo].[OrdenTaller] CHECK CONSTRAINT [FK_Orden_Sucursal]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Orden_Tecnico]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTaller]'))
ALTER TABLE [dbo].[OrdenTaller]  WITH CHECK ADD  CONSTRAINT [FK_Orden_Tecnico] FOREIGN KEY([TecnicoId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Orden_Tecnico]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTaller]'))
ALTER TABLE [dbo].[OrdenTaller] CHECK CONSTRAINT [FK_Orden_Tecnico]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrdFoto_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTallerFoto]'))
ALTER TABLE [dbo].[OrdenTallerFoto]  WITH CHECK ADD  CONSTRAINT [FK_OrdFoto_Orden] FOREIGN KEY([OrdenTallerId])
REFERENCES [dbo].[OrdenTaller] ([OrdenTallerId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrdFoto_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTallerFoto]'))
ALTER TABLE [dbo].[OrdenTallerFoto] CHECK CONSTRAINT [FK_OrdFoto_Orden]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrdRep_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTallerRepuesto]'))
ALTER TABLE [dbo].[OrdenTallerRepuesto]  WITH CHECK ADD  CONSTRAINT [FK_OrdRep_Orden] FOREIGN KEY([OrdenTallerId])
REFERENCES [dbo].[OrdenTaller] ([OrdenTallerId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrdRep_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTallerRepuesto]'))
ALTER TABLE [dbo].[OrdenTallerRepuesto] CHECK CONSTRAINT [FK_OrdRep_Orden]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrdRep_Variante]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTallerRepuesto]'))
ALTER TABLE [dbo].[OrdenTallerRepuesto]  WITH CHECK ADD  CONSTRAINT [FK_OrdRep_Variante] FOREIGN KEY([VarianteId])
REFERENCES [dbo].[ProductoVariante] ([VarianteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrdRep_Variante]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenTallerRepuesto]'))
ALTER TABLE [dbo].[OrdenTallerRepuesto] CHECK CONSTRAINT [FK_OrdRep_Variante]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoCred_Credito]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoCredito]'))
ALTER TABLE [dbo].[PagoCredito]  WITH CHECK ADD  CONSTRAINT [FK_PagoCred_Credito] FOREIGN KEY([CreditoId])
REFERENCES [dbo].[Credito] ([CreditoId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoCred_Credito]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoCredito]'))
ALTER TABLE [dbo].[PagoCredito] CHECK CONSTRAINT [FK_PagoCred_Credito]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoCred_Cuota]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoCredito]'))
ALTER TABLE [dbo].[PagoCredito]  WITH CHECK ADD  CONSTRAINT [FK_PagoCred_Cuota] FOREIGN KEY([CuotaId])
REFERENCES [dbo].[Cuota] ([CuotaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoCred_Cuota]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoCredito]'))
ALTER TABLE [dbo].[PagoCredito] CHECK CONSTRAINT [FK_PagoCred_Cuota]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoCred_Metodo]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoCredito]'))
ALTER TABLE [dbo].[PagoCredito]  WITH CHECK ADD  CONSTRAINT [FK_PagoCred_Metodo] FOREIGN KEY([MetodoPagoId])
REFERENCES [dbo].[MetodoPago] ([MetodoPagoId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoCred_Metodo]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoCredito]'))
ALTER TABLE [dbo].[PagoCredito] CHECK CONSTRAINT [FK_PagoCred_Metodo]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoCred_Sesion]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoCredito]'))
ALTER TABLE [dbo].[PagoCredito]  WITH CHECK ADD  CONSTRAINT [FK_PagoCred_Sesion] FOREIGN KEY([SesionCajaId])
REFERENCES [dbo].[SesionCaja] ([SesionCajaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoCred_Sesion]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoCredito]'))
ALTER TABLE [dbo].[PagoCredito] CHECK CONSTRAINT [FK_PagoCred_Sesion]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoCred_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoCredito]'))
ALTER TABLE [dbo].[PagoCredito]  WITH CHECK ADD  CONSTRAINT [FK_PagoCred_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoCred_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoCredito]'))
ALTER TABLE [dbo].[PagoCredito] CHECK CONSTRAINT [FK_PagoCred_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoEmpleado_Empleado]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoEmpleado]'))
ALTER TABLE [dbo].[PagoEmpleado]  WITH CHECK ADD  CONSTRAINT [FK_PagoEmpleado_Empleado] FOREIGN KEY([EmpleadoId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoEmpleado_Empleado]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoEmpleado]'))
ALTER TABLE [dbo].[PagoEmpleado] CHECK CONSTRAINT [FK_PagoEmpleado_Empleado]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoProv_Compra]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoProveedor]'))
ALTER TABLE [dbo].[PagoProveedor]  WITH CHECK ADD  CONSTRAINT [FK_PagoProv_Compra] FOREIGN KEY([CompraId])
REFERENCES [dbo].[Compra] ([CompraId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoProv_Compra]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoProveedor]'))
ALTER TABLE [dbo].[PagoProveedor] CHECK CONSTRAINT [FK_PagoProv_Compra]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoProv_Proveedor]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoProveedor]'))
ALTER TABLE [dbo].[PagoProveedor]  WITH CHECK ADD  CONSTRAINT [FK_PagoProv_Proveedor] FOREIGN KEY([ProveedorId])
REFERENCES [dbo].[Proveedor] ([ProveedorId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PagoProv_Proveedor]') AND parent_object_id = OBJECT_ID(N'[dbo].[PagoProveedor]'))
ALTER TABLE [dbo].[PagoProveedor] CHECK CONSTRAINT [FK_PagoProv_Proveedor]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Producto_Categoria]') AND parent_object_id = OBJECT_ID(N'[dbo].[Producto]'))
ALTER TABLE [dbo].[Producto]  WITH CHECK ADD  CONSTRAINT [FK_Producto_Categoria] FOREIGN KEY([CategoriaId])
REFERENCES [dbo].[Categoria] ([CategoriaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Producto_Categoria]') AND parent_object_id = OBJECT_ID(N'[dbo].[Producto]'))
ALTER TABLE [dbo].[Producto] CHECK CONSTRAINT [FK_Producto_Categoria]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Producto_Marca]') AND parent_object_id = OBJECT_ID(N'[dbo].[Producto]'))
ALTER TABLE [dbo].[Producto]  WITH CHECK ADD  CONSTRAINT [FK_Producto_Marca] FOREIGN KEY([MarcaId])
REFERENCES [dbo].[Marca] ([MarcaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Producto_Marca]') AND parent_object_id = OBJECT_ID(N'[dbo].[Producto]'))
ALTER TABLE [dbo].[Producto] CHECK CONSTRAINT [FK_Producto_Marca]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Variante_Producto]') AND parent_object_id = OBJECT_ID(N'[dbo].[ProductoVariante]'))
ALTER TABLE [dbo].[ProductoVariante]  WITH CHECK ADD  CONSTRAINT [FK_Variante_Producto] FOREIGN KEY([ProductoId])
REFERENCES [dbo].[Producto] ([ProductoId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Variante_Producto]') AND parent_object_id = OBJECT_ID(N'[dbo].[ProductoVariante]'))
ALTER TABLE [dbo].[ProductoVariante] CHECK CONSTRAINT [FK_Variante_Producto]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RefreshToken_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[RefreshToken]'))
ALTER TABLE [dbo].[RefreshToken]  WITH CHECK ADD  CONSTRAINT [FK_RefreshToken_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RefreshToken_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[RefreshToken]'))
ALTER TABLE [dbo].[RefreshToken] CHECK CONSTRAINT [FK_RefreshToken_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RolPermiso_Permiso]') AND parent_object_id = OBJECT_ID(N'[dbo].[RolPermiso]'))
ALTER TABLE [dbo].[RolPermiso]  WITH CHECK ADD  CONSTRAINT [FK_RolPermiso_Permiso] FOREIGN KEY([PermisoId])
REFERENCES [dbo].[Permiso] ([PermisoId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RolPermiso_Permiso]') AND parent_object_id = OBJECT_ID(N'[dbo].[RolPermiso]'))
ALTER TABLE [dbo].[RolPermiso] CHECK CONSTRAINT [FK_RolPermiso_Permiso]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RolPermiso_Rol]') AND parent_object_id = OBJECT_ID(N'[dbo].[RolPermiso]'))
ALTER TABLE [dbo].[RolPermiso]  WITH CHECK ADD  CONSTRAINT [FK_RolPermiso_Rol] FOREIGN KEY([RolId])
REFERENCES [dbo].[Rol] ([RolId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RolPermiso_Rol]') AND parent_object_id = OBJECT_ID(N'[dbo].[RolPermiso]'))
ALTER TABLE [dbo].[RolPermiso] CHECK CONSTRAINT [FK_RolPermiso_Rol]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Sesion_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[SesionCaja]'))
ALTER TABLE [dbo].[SesionCaja]  WITH CHECK ADD  CONSTRAINT [FK_Sesion_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Sesion_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[SesionCaja]'))
ALTER TABLE [dbo].[SesionCaja] CHECK CONSTRAINT [FK_Sesion_Sucursal]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Sesion_UsrAbre]') AND parent_object_id = OBJECT_ID(N'[dbo].[SesionCaja]'))
ALTER TABLE [dbo].[SesionCaja]  WITH CHECK ADD  CONSTRAINT [FK_Sesion_UsrAbre] FOREIGN KEY([UsuarioApertura])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Sesion_UsrAbre]') AND parent_object_id = OBJECT_ID(N'[dbo].[SesionCaja]'))
ALTER TABLE [dbo].[SesionCaja] CHECK CONSTRAINT [FK_Sesion_UsrAbre]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Sesion_UsrCierra]') AND parent_object_id = OBJECT_ID(N'[dbo].[SesionCaja]'))
ALTER TABLE [dbo].[SesionCaja]  WITH CHECK ADD  CONSTRAINT [FK_Sesion_UsrCierra] FOREIGN KEY([UsuarioCierre])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Sesion_UsrCierra]') AND parent_object_id = OBJECT_ID(N'[dbo].[SesionCaja]'))
ALTER TABLE [dbo].[SesionCaja] CHECK CONSTRAINT [FK_Sesion_UsrCierra]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Usuario_Rol]') AND parent_object_id = OBJECT_ID(N'[dbo].[Usuario]'))
ALTER TABLE [dbo].[Usuario]  WITH CHECK ADD  CONSTRAINT [FK_Usuario_Rol] FOREIGN KEY([RolId])
REFERENCES [dbo].[Rol] ([RolId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Usuario_Rol]') AND parent_object_id = OBJECT_ID(N'[dbo].[Usuario]'))
ALTER TABLE [dbo].[Usuario] CHECK CONSTRAINT [FK_Usuario_Rol]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Usuario_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[Usuario]'))
ALTER TABLE [dbo].[Usuario]  WITH CHECK ADD  CONSTRAINT [FK_Usuario_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Usuario_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[Usuario]'))
ALTER TABLE [dbo].[Usuario] CHECK CONSTRAINT [FK_Usuario_Sucursal]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Venta_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [FK_Venta_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Venta_Cliente]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [FK_Venta_Cliente]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Venta_Sesion]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [FK_Venta_Sesion] FOREIGN KEY([SesionCajaId])
REFERENCES [dbo].[SesionCaja] ([SesionCajaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Venta_Sesion]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [FK_Venta_Sesion]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Venta_SesionCaja_Fix]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [FK_Venta_SesionCaja_Fix] FOREIGN KEY([SesionCajaId])
REFERENCES [dbo].[SesionCaja] ([SesionCajaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Venta_SesionCaja_Fix]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [FK_Venta_SesionCaja_Fix]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Venta_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [FK_Venta_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Venta_Sucursal]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [FK_Venta_Sucursal]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Venta_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [FK_Venta_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Venta_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[Venta]'))
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [FK_Venta_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VtaDet_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[VentaDetalle]'))
ALTER TABLE [dbo].[VentaDetalle]  WITH CHECK ADD  CONSTRAINT [FK_VtaDet_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VtaDet_Imei]') AND parent_object_id = OBJECT_ID(N'[dbo].[VentaDetalle]'))
ALTER TABLE [dbo].[VentaDetalle] CHECK CONSTRAINT [FK_VtaDet_Imei]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VtaDet_Variante]') AND parent_object_id = OBJECT_ID(N'[dbo].[VentaDetalle]'))
ALTER TABLE [dbo].[VentaDetalle]  WITH CHECK ADD  CONSTRAINT [FK_VtaDet_Variante] FOREIGN KEY([VarianteId])
REFERENCES [dbo].[ProductoVariante] ([VarianteId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VtaDet_Variante]') AND parent_object_id = OBJECT_ID(N'[dbo].[VentaDetalle]'))
ALTER TABLE [dbo].[VentaDetalle] CHECK CONSTRAINT [FK_VtaDet_Variante]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VtaDet_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[VentaDetalle]'))
ALTER TABLE [dbo].[VentaDetalle]  WITH CHECK ADD  CONSTRAINT [FK_VtaDet_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VtaDet_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[VentaDetalle]'))
ALTER TABLE [dbo].[VentaDetalle] CHECK CONSTRAINT [FK_VtaDet_Venta]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VtaPago_Metodo]') AND parent_object_id = OBJECT_ID(N'[dbo].[VentaPago]'))
ALTER TABLE [dbo].[VentaPago]  WITH CHECK ADD  CONSTRAINT [FK_VtaPago_Metodo] FOREIGN KEY([MetodoPagoId])
REFERENCES [dbo].[MetodoPago] ([MetodoPagoId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VtaPago_Metodo]') AND parent_object_id = OBJECT_ID(N'[dbo].[VentaPago]'))
ALTER TABLE [dbo].[VentaPago] CHECK CONSTRAINT [FK_VtaPago_Metodo]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VtaPago_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[VentaPago]'))
ALTER TABLE [dbo].[VentaPago]  WITH CHECK ADD  CONSTRAINT [FK_VtaPago_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VtaPago_Venta]') AND parent_object_id = OBJECT_ID(N'[dbo].[VentaPago]'))
ALTER TABLE [dbo].[VentaPago] CHECK CONSTRAINT [FK_VtaPago_Venta]
GO

/* ============================================================================ */
/*  VISTAS */
/* ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =============================================================================
   D) VISTAS DE APOYO
   ============================================================================= */

-- Cuotas vencidas (morosidad)
CREATE OR ALTER VIEW [dbo].[vw_CuotasVencidas] AS
SELECT
    c.CuotaId, cr.CreditoId, cr.ClienteId, cl.Nombre AS Cliente, cl.Telefono,
    c.NumeroCuota, c.FechaVencimiento, c.MontoCuota, c.MontoPagado, c.Saldo,
    DATEDIFF(DAY, c.FechaVencimiento, CAST(SYSDATETIME() AS DATE)) AS DiasVencido
FROM dbo.Cuota c
JOIN dbo.Credito cr ON cr.CreditoId = c.CreditoId
JOIN dbo.Cliente cl ON cl.ClienteId = cr.ClienteId
WHERE c.Estado IN (N'Pendiente', N'Parcial', N'Vencida')
  AND c.FechaVencimiento < CAST(SYSDATETIME() AS DATE);

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Garantia vigente por IMEI (consulta rapida)
CREATE OR ALTER VIEW [dbo].[vw_GarantiaVigentePorImei] AS
SELECT
    g.GarantiaId, g.ImeiId, i.Imei, g.ClienteId, g.VentaId,
    g.FechaInicio, g.FechaVencimiento, g.MesesCobertura, g.Estado,
    CASE WHEN g.Estado = N'Vigente' AND g.FechaVencimiento >= CAST(SYSDATETIME() AS DATE)
         THEN 1 ELSE 0 END AS Vigente
FROM dbo.Garantia g
LEFT JOIN dbo.InventarioImei i ON i.ImeiId = g.ImeiId;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Indice de fallas por modelo (a partir de los casos de garantia/RMA)
CREATE OR ALTER VIEW [dbo].[vw_IndiceFallasPorModelo] AS
SELECT
    p.ProductoId, p.Nombre AS Producto, m.Nombre AS Marca,
    COUNT(cg.CasoGarantiaId) AS TotalCasos
FROM dbo.CasoGarantia cg
JOIN dbo.InventarioImei i   ON i.ImeiId = cg.ImeiId
JOIN dbo.ProductoVariante v ON v.VarianteId = i.VarianteId
JOIN dbo.Producto p         ON p.ProductoId = v.ProductoId
LEFT JOIN dbo.Marca m       ON m.MarcaId = p.MarcaId
GROUP BY p.ProductoId, p.Nombre, m.Nombre;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE OR ALTER VIEW [dbo].[vw_InventarioDisponible] AS
-- Inventario disponible por modelo/variante/sucursal
SELECT
    p.ProductoId, p.Nombre AS Producto, m.Nombre AS Marca,
    v.VarianteId, v.Color, v.Almacenamiento, v.Condicion, v.PrecioVenta,
    v.StockMinimo,
    i.SucursalId,
    COUNT(i.ImeiId) AS Disponibles,
    SUM(i.PrecioCosto) AS CostoTotal
FROM dbo.InventarioImei i
JOIN dbo.ProductoVariante v ON v.VarianteId = i.VarianteId
JOIN dbo.Producto p        ON p.ProductoId = v.ProductoId
LEFT JOIN dbo.Marca m      ON m.MarcaId = p.MarcaId
WHERE i.Estado = N'Disponible'
GROUP BY p.ProductoId, p.Nombre, m.Nombre, v.VarianteId, v.Color, v.Almacenamiento, v.Condicion, v.PrecioVenta, v.StockMinimo, i.SucursalId;

GO

/* ============================================================================ */
/*  FUNCIONES */
/* ============================================================================ */

/* ============================================================================ */
/*  PROCEDIMIENTOS ALMACENADOS */
/* ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =============================================================================
   C.3) CIERRE DE CAJA: calcula el esperado, la diferencia y cierra la sesion
        Esperado = Apertura + Ingresos - Egresos
                   + Ventas en efectivo de la sesion
                   + Pagos de credito en efectivo de la sesion
        (Las ventas/pagos NO se duplican como MovimientoCaja; estos ultimos son
         para ingresos/egresos manuales como caja chica.)
   ============================================================================= */
CREATE OR ALTER PROCEDURE [dbo].[usp_Caja_Cerrar]
    @SesionCajaId  INT,
    @UsuarioCierre INT,
    @MontoContado  DECIMAL(18,2),
    @MontoEsperado DECIMAL(18,2) = NULL OUTPUT,
    @Diferencia    DECIMAL(18,2) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.SesionCaja WHERE SesionCajaId = @SesionCajaId AND Estado = N'Abierta')
    BEGIN
        RAISERROR(N'La sesion de caja no existe o ya esta cerrada.', 16, 1);
        RETURN;
    END

    DECLARE @Apertura DECIMAL(18,2) = (SELECT MontoApertura FROM dbo.SesionCaja WHERE SesionCajaId = @SesionCajaId);

    DECLARE @Ingresos DECIMAL(18,2) =
        ISNULL((SELECT SUM(Monto) FROM dbo.MovimientoCaja WHERE SesionCajaId = @SesionCajaId AND Tipo = N'Ingreso'), 0);
    DECLARE @Egresos DECIMAL(18,2) =
        ISNULL((SELECT SUM(Monto) FROM dbo.MovimientoCaja WHERE SesionCajaId = @SesionCajaId AND Tipo = N'Egreso'), 0);

    DECLARE @VentasEfectivo DECIMAL(18,2) = ISNULL((
        SELECT SUM(vp.Monto)
        FROM dbo.VentaPago vp
        JOIN dbo.Venta v       ON v.VentaId = vp.VentaId
        JOIN dbo.MetodoPago mp ON mp.MetodoPagoId = vp.MetodoPagoId
        WHERE v.SesionCajaId = @SesionCajaId AND v.Estado = N'Completada' AND mp.Nombre = N'Efectivo'), 0);

    DECLARE @PagosCredEfectivo DECIMAL(18,2) = ISNULL((
        SELECT SUM(pc.Monto)
        FROM dbo.PagoCredito pc
        JOIN dbo.MetodoPago mp ON mp.MetodoPagoId = pc.MetodoPagoId
        WHERE pc.SesionCajaId = @SesionCajaId AND mp.Nombre = N'Efectivo'), 0);

    SET @MontoEsperado = @Apertura + @Ingresos - @Egresos + @VentasEfectivo + @PagosCredEfectivo;
    SET @Diferencia    = @MontoContado - @MontoEsperado;

    UPDATE dbo.SesionCaja
       SET MontoCierre = @MontoContado,
           Diferencia  = @Diferencia,
           Estado      = N'Cerrada',
           FechaCierre = SYSDATETIME(),
           UsuarioCierre = @UsuarioCierre
     WHERE SesionCajaId = @SesionCajaId;

    SELECT @MontoEsperado AS MontoEsperado, @MontoContado AS MontoContado, @Diferencia AS Diferencia;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =============================================================================
   C.2) CREDITO: crear financiamiento y generar su tabla de cuotas
        Interes simple mensual: InteresTotal = Principal * (Tasa%/100) * NumeroCuotas
        La ultima cuota absorbe el redondeo para que la suma cuadre con el total.
   ============================================================================= */
CREATE OR ALTER PROCEDURE [dbo].[usp_Credito_Crear]
    @ClienteId              INT,
    @MontoFinanciado        DECIMAL(18,2),         -- principal a financiar (sin inicial)
    @NumeroCuotas           INT,
    @FechaPrimerVencimiento DATE,
    @VentaId                INT = NULL,
    @Inicial                DECIMAL(18,2) = 0,
    @TasaInteresMensual     DECIMAL(9,4)  = 0,     -- % mensual
    @CreditoId              INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @NumeroCuotas <= 0
    BEGIN RAISERROR(N'NumeroCuotas debe ser mayor que cero.', 16, 1); RETURN; END
    IF @MontoFinanciado <= 0
    BEGIN RAISERROR(N'MontoFinanciado debe ser mayor que cero.', 16, 1); RETURN; END

    DECLARE @InteresTotal DECIMAL(18,2) = ROUND(@MontoFinanciado * (@TasaInteresMensual / 100.0) * @NumeroCuotas, 2);
    DECLARE @MontoTotal   DECIMAL(18,2) = @MontoFinanciado + @InteresTotal;
    DECLARE @MontoCuota   DECIMAL(18,2) = ROUND(@MontoTotal / @NumeroCuotas, 2);

    BEGIN TRAN;

    INSERT INTO dbo.Credito
        (VentaId, ClienteId, MontoFinanciado, Inicial, TasaInteres, NumeroCuotas, MontoTotal, Saldo, Estado, FechaInicio)
    VALUES
        (@VentaId, @ClienteId, @MontoFinanciado, @Inicial, @TasaInteresMensual, @NumeroCuotas, @MontoTotal, @MontoTotal, N'Activo', SYSDATETIME());

    SET @CreditoId = SCOPE_IDENTITY();

    DECLARE @i INT = 1;
    DECLARE @acum DECIMAL(18,2) = 0;
    DECLARE @cuota DECIMAL(18,2);

    WHILE @i <= @NumeroCuotas
    BEGIN
        IF @i < @NumeroCuotas
            SET @cuota = @MontoCuota;
        ELSE
            SET @cuota = @MontoTotal - @acum;   -- ultima cuota = resto exacto

        INSERT INTO dbo.Cuota
            (CreditoId, NumeroCuota, FechaVencimiento, MontoCuota, MontoPagado, Saldo, Estado)
        VALUES
            (@CreditoId, @i, DATEADD(MONTH, @i - 1, @FechaPrimerVencimiento), @cuota, 0, @cuota, N'Pendiente');

        SET @acum = @acum + @cuota;
        SET @i = @i + 1;
    END

    COMMIT TRAN;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_Creditos_ActualizarMora]
    @FechaCorte DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @FechaCorte IS NULL
        SET @FechaCorte = CAST(SYSDATETIME() AS DATE);

    DECLARE @CuotasVencidas INT = 0,
            @CreditosEnMora  INT = 0,
            @CreditosReactivados INT = 0,
            @ClientesActualizados INT = 0;

    BEGIN TRAN;

    -- 1) Cuotas que pasaron de fecha y aun deben -> Vencida
    UPDATE dbo.Cuota
       SET Estado = N'Vencida'
     WHERE Saldo > 0
       AND Estado IN (N'Pendiente', N'Parcial')
       AND FechaVencimiento < @FechaCorte;
    SET @CuotasVencidas = @@ROWCOUNT;

    -- 2) Creditos Activo con alguna cuota vencida -> EnMora
    UPDATE cr
       SET cr.Estado = N'EnMora'
    FROM dbo.Credito cr
    WHERE cr.Estado = N'Activo'
      AND EXISTS (SELECT 1 FROM dbo.Cuota c
                  WHERE c.CreditoId = cr.CreditoId AND c.Estado = N'Vencida');
    SET @CreditosEnMora = @@ROWCOUNT;

    -- 3) Creditos EnMora que ya no tienen cuotas vencidas -> Activo
    UPDATE cr
       SET cr.Estado = N'Activo'
    FROM dbo.Credito cr
    WHERE cr.Estado = N'EnMora'
      AND NOT EXISTS (SELECT 1 FROM dbo.Cuota c
                      WHERE c.CreditoId = cr.CreditoId AND c.Estado = N'Vencida');
    SET @CreditosReactivados = @@ROWCOUNT;

    -- 4) Bandera de cliente moroso (1 si tiene algun credito EnMora)
    UPDATE cl
       SET cl.EsMoroso = CASE WHEN EXISTS (
                                  SELECT 1 FROM dbo.Credito cr
                                  WHERE cr.ClienteId = cl.ClienteId AND cr.Estado = N'EnMora')
                              THEN 1 ELSE 0 END
    FROM dbo.Cliente cl
    WHERE cl.EsMoroso <> CASE WHEN EXISTS (
                                  SELECT 1 FROM dbo.Credito cr
                                  WHERE cr.ClienteId = cl.ClienteId AND cr.Estado = N'EnMora')
                              THEN 1 ELSE 0 END;
    SET @ClientesActualizados = @@ROWCOUNT;

    COMMIT TRAN;

    -- Resumen de la corrida
    SELECT @FechaCorte           AS FechaCorte,
           @CuotasVencidas       AS CuotasMarcadasVencidas,
           @CreditosEnMora       AS CreditosPuestosEnMora,
           @CreditosReactivados  AS CreditosReactivados,
           @ClientesActualizados AS ClientesMorososActualizados;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_Ncf_Siguiente]
    @Tipo NVARCHAR(2),
    @Ncf  NVARCHAR(19) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Ncf = NULL;

    DECLARE @out TABLE (Ncf NVARCHAR(19));

    -- Incremento atÃ³mico: solo si estÃ¡ activo, hay rango disponible y no venciÃ³
    UPDATE dbo.SecuenciaNcf
        SET Secuencia = Secuencia + 1
    OUTPUT (deleted.Serie + deleted.TipoComprobante
            + RIGHT('00000000' + CAST(deleted.Secuencia AS VARCHAR(8)), 8)) INTO @out
    WHERE TipoComprobante = @Tipo
      AND Activo = 1
      AND Secuencia <= Hasta
      AND (Vencimiento IS NULL OR Vencimiento >= CAST(GETDATE() AS DATE));

    SELECT @Ncf = Ncf FROM @out;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_PagoCredito_Registrar]
    @CreditoId     INT,
    @Monto         DECIMAL(18,2),
    @MetodoPagoId  INT = NULL,
    @SesionCajaId  INT = NULL,
    @UsuarioId     INT = NULL,
    @CuotaId       INT = NULL,           -- NULL = distribuir mas antigua primero
    @NumeroRecibo  NVARCHAR(30) = NULL,
    @PagoCreditoId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Monto <= 0
    BEGIN
        RAISERROR(N'El monto del abono debe ser mayor que cero.', 16, 1);
        RETURN;
    END

    BEGIN TRAN;

    -- Bloquear el credito para evitar carreras concurrentes
    DECLARE @Saldo DECIMAL(18,2), @Estado NVARCHAR(15);
    SELECT @Saldo = Saldo, @Estado = Estado
    FROM dbo.Credito WITH (UPDLOCK, HOLDLOCK)
    WHERE CreditoId = @CreditoId;

    IF @Saldo IS NULL
    BEGIN
        ROLLBACK TRAN;
        RAISERROR(N'El credito no existe.', 16, 1);
        RETURN;
    END

    IF @Estado = N'Saldado' OR @Saldo <= 0
    BEGIN
        ROLLBACK TRAN;
        RAISERROR(N'El credito ya esta saldado.', 16, 1);
        RETURN;
    END

    IF @Monto > @Saldo
    BEGIN
        DECLARE @msg1 NVARCHAR(250) =
            CONCAT(N'El abono (', @Monto, N') excede el saldo del credito (', @Saldo, N').');
        ROLLBACK TRAN;
        RAISERROR(@msg1, 16, 1);
        RETURN;
    END

    -- Cuotas objetivo (mas antigua primero)
    DECLARE @cuotas TABLE (Orden INT IDENTITY(1,1), CuotaId INT, Saldo DECIMAL(18,2));

    IF @CuotaId IS NOT NULL
    BEGIN
        INSERT INTO @cuotas (CuotaId, Saldo)
        SELECT CuotaId, Saldo
        FROM dbo.Cuota
        WHERE CuotaId = @CuotaId AND CreditoId = @CreditoId AND Estado <> N'Pagada';

        IF NOT EXISTS (SELECT 1 FROM @cuotas)
        BEGIN
            ROLLBACK TRAN;
            RAISERROR(N'La cuota indicada no existe, no pertenece al credito o ya esta pagada.', 16, 1);
            RETURN;
        END
    END
    ELSE
    BEGIN
        INSERT INTO @cuotas (CuotaId, Saldo)
        SELECT CuotaId, Saldo
        FROM dbo.Cuota
        WHERE CreditoId = @CreditoId AND Estado <> N'Pagada'
        ORDER BY NumeroCuota;
    END

    -- Aplicar el abono cuota por cuota
    DECLARE @rem DECIMAL(18,2) = @Monto;
    DECLARE @orden INT = 1;
    DECLARE @maxOrden INT = (SELECT ISNULL(MAX(Orden), 0) FROM @cuotas);
    DECLARE @cId INT, @cSaldo DECIMAL(18,2), @aplicar DECIMAL(18,2);
    DECLARE @ultimoPago INT = NULL;

    WHILE @orden <= @maxOrden AND @rem > 0
    BEGIN
        SELECT @cId = CuotaId, @cSaldo = Saldo FROM @cuotas WHERE Orden = @orden;

        SET @aplicar = CASE WHEN @rem >= @cSaldo THEN @cSaldo ELSE @rem END;

        UPDATE dbo.Cuota
           SET MontoPagado = MontoPagado + @aplicar,
               Saldo       = Saldo - @aplicar,
               Estado      = CASE WHEN (Saldo - @aplicar) <= 0 THEN N'Pagada' ELSE N'Parcial' END
         WHERE CuotaId = @cId;

        INSERT INTO dbo.PagoCredito
            (CreditoId, CuotaId, MetodoPagoId, SesionCajaId, UsuarioId, Monto, NumeroRecibo, Fecha)
        VALUES
            (@CreditoId, @cId, @MetodoPagoId, @SesionCajaId, @UsuarioId, @aplicar, @NumeroRecibo, SYSDATETIME());

        SET @ultimoPago = SCOPE_IDENTITY();
        SET @rem   = @rem - @aplicar;
        SET @orden = @orden + 1;
    END

    -- Si quedo remanente (caso @CuotaId con monto mayor al saldo de esa cuota)
    IF @rem > 0
    BEGIN
        ROLLBACK TRAN;
        RAISERROR(N'El monto excede el saldo de la(s) cuota(s) seleccionada(s).', 16, 1);
        RETURN;
    END

    -- Actualizar saldo y estado del credito
    UPDATE dbo.Credito
       SET Saldo  = Saldo - @Monto,
           Estado = CASE WHEN (Saldo - @Monto) <= 0 THEN N'Saldado' ELSE Estado END
     WHERE CreditoId = @CreditoId;

    SET @PagoCreditoId = @ultimoPago;

    COMMIT TRAN;

    -- Resumen del credito tras el abono
    SELECT CreditoId, MontoTotal, Saldo, Estado
    FROM dbo.Credito
    WHERE CreditoId = @CreditoId;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_Venta_Registrar]
    @SucursalId    INT,
    @UsuarioId     INT,
    @ClienteId     INT = NULL,
    @SesionCajaId  INT = NULL,
    @EsCredito     BIT = 0,
    @MetodoPagoId  INT = NULL,
    @NumeroFactura NVARCHAR(30) = NULL,
    @Detalles      dbo.VentaDetalleTipo READONLY,
    @VentaId       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM @Detalles)
    BEGIN
        RAISERROR(N'La venta no tiene detalles.', 16, 1);
        RETURN;
    END

    -- VALIDACIÃ“N CRÃTICA: Prevenir Stock Negativo en productos no serializados
    IF EXISTS (
        SELECT 1
        FROM @Detalles d
        JOIN dbo.ProductoVariante pv ON pv.VarianteId = d.VarianteId
        WHERE d.ImeiId IS NULL AND (pv.StockNoSerial - d.Cantidad) < 0
    )
    BEGIN
        RAISERROR(N'Stock insuficiente para uno o mÃ¡s accesorios/productos no serializados.', 16, 1);
        RETURN;
    END

    DECLARE @Itbis DECIMAL(9,4) = ISNULL((SELECT TOP 1 PorcentajeItbis FROM dbo.Empresa ORDER BY EmpresaId), 0);

    BEGIN TRAN;

    -- ValidaciÃ³n: IMEIs inexistentes (mensaje claro antes del error de FK)
    IF EXISTS (
        SELECT 1 FROM @Detalles d
        WHERE d.ImeiId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM dbo.InventarioImei i WHERE i.ImeiId = d.ImeiId)
    )
    BEGIN
        ROLLBACK TRAN;
        RAISERROR(N'Uno o mÃ¡s IMEI no existen.', 16, 1);
        RETURN;
    END

    -- Bloqueo y validaciÃ³n de IMEIs concurrentes
    IF EXISTS (
        SELECT 1 FROM @Detalles d
        JOIN dbo.InventarioImei i WITH (UPDLOCK, HOLDLOCK) ON i.ImeiId = d.ImeiId
        WHERE d.ImeiId IS NOT NULL AND i.Estado <> N'Disponible'
    )
    BEGIN
        ROLLBACK TRAN;
        RAISERROR(N'Uno o mÃ¡s IMEI seleccionados ya no estÃ¡n disponibles.', 16, 1);
        RETURN;
    END

    -- Insertar Cabecera
    INSERT INTO dbo.Venta
        (NumeroFactura, SucursalId, ClienteId, UsuarioId, SesionCajaId, EsCredito, Estado, Subtotal, Descuento, Impuesto, Total)
    VALUES
        (@NumeroFactura, @SucursalId, @ClienteId, @UsuarioId, @SesionCajaId, @EsCredito, N'Completada', 0, 0, 0, 0);

    SET @VentaId = SCOPE_IDENTITY();

    -- Insertar Detalle con generaciÃ³n dinÃ¡mica de la descripciÃ³n para auditorÃ­a/facturaciÃ³n
    INSERT INTO dbo.VentaDetalle
        (VentaId, ImeiId, VarianteId, Descripcion, Cantidad, PrecioUnitario, Descuento, Impuesto, Total)
    SELECT
        @VentaId, d.ImeiId, d.VarianteId,
        CONCAT(p.Nombre, N' ', v.Color, N' ', v.Almacenamiento), -- SoluciÃ³n a la descripciÃ³n NULL
        d.Cantidad, d.PrecioUnitario, d.Descuento,
        ROUND((d.PrecioUnitario * d.Cantidad - d.Descuento) * @Itbis / 100.0, 2),
        (d.PrecioUnitario * d.Cantidad - d.Descuento) + ROUND((d.PrecioUnitario * d.Cantidad - d.Descuento) * @Itbis / 100.0, 2)
    FROM @Detalles d
    JOIN dbo.ProductoVariante v ON v.VarianteId = d.VarianteId
    JOIN dbo.Producto p ON p.ProductoId = v.ProductoId;

    -- Actualizaciones de Inventario
    UPDATE i SET i.Estado = N'Vendido'
    FROM dbo.InventarioImei i
    JOIN @Detalles d ON d.ImeiId = i.ImeiId;

    INSERT INTO dbo.MovimientoInventario (ImeiId, Tipo, SucursalOrigen, Referencia, UsuarioId)
    SELECT d.ImeiId, N'Venta', @SucursalId, CONCAT(N'Venta #', @VentaId), @UsuarioId
    FROM @Detalles d
    WHERE d.ImeiId IS NOT NULL;

    -- DeducciÃ³n controlada de Stock No Serializado
    UPDATE v SET v.StockNoSerial = v.StockNoSerial - d.Cantidad
    FROM dbo.ProductoVariante v
    JOIN @Detalles d ON d.VarianteId = v.VarianteId
    WHERE d.ImeiId IS NULL;

    -- RecÃ¡lculo de Totales
    UPDATE vta
       SET vta.Subtotal  = x.Sub, vta.Descuento = x.Des, vta.Impuesto  = x.Imp, vta.Total     = x.Tot
    FROM dbo.Venta vta
    CROSS APPLY (
        SELECT SUM(PrecioUnitario * Cantidad) AS Sub, SUM(Descuento) AS Des, SUM(Impuesto) AS Imp, SUM(Total) AS Tot
        FROM dbo.VentaDetalle WHERE VentaId = @VentaId
    ) x
    WHERE vta.VentaId = @VentaId;

    IF @EsCredito = 0
    BEGIN
        IF @MetodoPagoId IS NULL
        BEGIN
            ROLLBACK TRAN;
            RAISERROR(N'Debe indicar el mÃ©todo de pago para una venta de contado.', 16, 1);
            RETURN;
        END
        INSERT INTO dbo.VentaPago (VentaId, MetodoPagoId, Monto)
        SELECT @VentaId, @MetodoPagoId, Total FROM dbo.Venta WHERE VentaId = @VentaId;
    END

    COMMIT TRAN;
END;

GO


/* ============================================================================ */
/*  SEMILLAS DE CONFIGURACIÓN  (equivalentes a v5, v8, v11, v26)                */
/* ============================================================================ */

-- v5) Secuencia de facturas
IF NOT EXISTS (SELECT 1 FROM dbo.Secuencia WHERE Nombre = N'Factura')
    INSERT INTO dbo.Secuencia (Nombre, Prefijo, Valor, Longitud)
    VALUES (N'Factura', N'FAC-', 0, 6);
GO

-- v8) Plantillas NCF (inactivas hasta configurar el rango autorizado por la DGII)
IF NOT EXISTS (SELECT 1 FROM dbo.SecuenciaNcf WHERE TipoComprobante = N'01')
    INSERT INTO dbo.SecuenciaNcf (TipoComprobante, Serie, Secuencia, Hasta, Activo) VALUES (N'01', N'B', 1, 0, 0);
IF NOT EXISTS (SELECT 1 FROM dbo.SecuenciaNcf WHERE TipoComprobante = N'02')
    INSERT INTO dbo.SecuenciaNcf (TipoComprobante, Serie, Secuencia, Hasta, Activo) VALUES (N'02', N'B', 1, 0, 0);
IF NOT EXISTS (SELECT 1 FROM dbo.SecuenciaNcf WHERE TipoComprobante = N'04')
    INSERT INTO dbo.SecuenciaNcf (TipoComprobante, Serie, Secuencia, Hasta, Activo) VALUES (N'04', N'B', 1, 0, 0);
GO

-- v11) Catálogo de cuentas (plan contable base). EsSistema=1 => no eliminable.
IF NOT EXISTS (SELECT 1 FROM dbo.CuentaContable)
BEGIN
    INSERT INTO dbo.CuentaContable (Codigo, Nombre, Tipo, CuentaPadreId, Naturaleza, PermiteMovimiento, EsSistema, Activo) VALUES
        ('1', 'ACTIVO',   'Activo',  NULL, 'Deudora',   0, 1, 1),
        ('2', 'PASIVO',   'Pasivo',  NULL, 'Acreedora', 0, 1, 1),
        ('3', 'CAPITAL',  'Capital', NULL, 'Acreedora', 0, 1, 1),
        ('4', 'INGRESOS', 'Ingreso', NULL, 'Acreedora', 0, 1, 1),
        ('5', 'COSTOS',   'Costo',   NULL, 'Deudora',   0, 1, 1),
        ('6', 'GASTOS',   'Gasto',   NULL, 'Deudora',   0, 1, 1);

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

-- v26) Categoría "Repuestos" (el selector de repuestos del taller filtra por ella)
IF NOT EXISTS (SELECT 1 FROM dbo.Categoria WHERE Nombre = N'Repuestos')
    INSERT INTO dbo.Categoria (Nombre) VALUES (N'Repuestos');
GO

PRINT 'GestionCelulares: esquema completo instalado.';
GO
