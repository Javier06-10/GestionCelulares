-- =============================================================================
-- GestionCelulares - Script de instalacion inicial (base de datos completa)
-- =============================================================================
-- Que es esto:
--   Volcado completo (tablas, tipos, vistas, indices, defaults, foreign keys y
--   procedimientos almacenados) generado desde SSMS ("Generate Scripts") sobre
--   una instancia de referencia. Sustituye a la cadena de scripts incrementales
--   v1..v16: ya incluye todo lo que esos scripts fueron aplicando con el tiempo.
--
-- Requisitos:
--   - SQL Server 2019 (v15) o superior (usa COMPATIBILITY_LEVEL = 150 y
--     ACCELERATED_DATABASE_RECOVERY). En SQL Server Express tambien funciona.
--   - Ejecutarlo con una cuenta con privilegios de sysadmin (crea la base de
--     datos y un login de servidor).
--
-- Como ejecutarlo en un PC nuevo:
--   1. SSMS: abre este archivo, conectate a la instancia y presiona Ejecutar (F5).
--   2. sqlcmd (linea de comandos):
--        sqlcmd -S NOMBRE_SERVIDOR -E -i GestionCelulares.sql
--      (usa -U usuario -P clave en vez de -E si no usas autenticacion de Windows)
--
-- Importante:
--   - Este script es SOLO para instalacion inicial sobre un servidor limpio.
--     Si la base de datos GestionCelulares ya existe, el script se detiene solo
--     (ver el bloque de abajo) y no toca nada para evitar borrar datos. El
--     mensaje "SQL Server is terminating this process" que veras al abortar es
--     esperado: es la forma estandar de frenar un script T-SQL a mitad de
--     camino (RAISERROR de severidad 20 cierra la conexion a proposito). No
--     indica dano ni afecta a la base de datos existente.
--   - Crea el login [gc_app] con una contrasena de ejemplo
--     ('CAMBIA_ESTA_CLAVE_Fuerte#2026'). CAMBIALA antes de usar en produccion
--     (con ALTER LOGIN) y guarda la real fuera del repositorio (user-secrets /
--     variable de entorno), nunca en este archivo.
-- =============================================================================

USE [master]
GO

IF DB_ID(N'GestionCelulares') IS NOT NULL
BEGIN
    RAISERROR(N'*** La base de datos GestionCelulares ya existe en este servidor. Instalacion abortada: este script es solo para un servidor limpio. Si necesitas reinstalar, elimina la base existente o usa una instancia nueva. (La conexion se cierra a proposito para frenar el resto del script; tu base de datos actual no fue tocada.) ***', 20, 1) WITH LOG;
END
GO

USE [master]
GO
/****** Object:  Database [GestionCelulares]    Script Date: 7/1/2026 11:15:17 AM ******/
CREATE DATABASE [GestionCelulares]
 
ALTER DATABASE [GestionCelulares] SET COMPATIBILITY_LEVEL = 150
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [GestionCelulares].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [GestionCelulares] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [GestionCelulares] SET ANSI_NULLS ON 
GO
ALTER DATABASE [GestionCelulares] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [GestionCelulares] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [GestionCelulares] SET ARITHABORT OFF 
GO
ALTER DATABASE [GestionCelulares] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [GestionCelulares] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [GestionCelulares] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [GestionCelulares] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [GestionCelulares] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [GestionCelulares] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [GestionCelulares] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [GestionCelulares] SET QUOTED_IDENTIFIER ON 
GO
ALTER DATABASE [GestionCelulares] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [GestionCelulares] SET  ENABLE_BROKER 
GO
ALTER DATABASE [GestionCelulares] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [GestionCelulares] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [GestionCelulares] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [GestionCelulares] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [GestionCelulares] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [GestionCelulares] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [GestionCelulares] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [GestionCelulares] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [GestionCelulares] SET  MULTI_USER 
GO
ALTER DATABASE [GestionCelulares] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [GestionCelulares] SET DB_CHAINING OFF 
GO
ALTER DATABASE [GestionCelulares] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [GestionCelulares] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [GestionCelulares] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [GestionCelulares] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'GestionCelulares', N'ON'
GO
ALTER DATABASE [GestionCelulares] SET QUERY_STORE = OFF
GO
-- Login de servidor + usuario de base de datos para la aplicacion (gc_app).
-- Idempotente: se puede volver a ejecutar sin error si ya existen.
USE [master];
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'gc_app')
    CREATE LOGIN [gc_app] WITH PASSWORD = N'CAMBIA_ESTA_CLAVE_Fuerte#2026', CHECK_POLICY = ON;
GO

USE [GestionCelulares]
GO
/****** Object:  User [gc_app] ******/
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'gc_app')
    CREATE USER [gc_app] FOR LOGIN [gc_app] WITH DEFAULT_SCHEMA=[dbo];
GO
/****** Object:  UserDefinedTableType [dbo].[VentaDetalleTipo]    Script Date: 7/1/2026 11:15:17 AM ******/
CREATE TYPE [dbo].[VentaDetalleTipo] AS TABLE(
	[ImeiId] [int] NULL,
	[VarianteId] [int] NOT NULL,
	[Cantidad] [int] NOT NULL,
	[PrecioUnitario] [decimal](18, 2) NOT NULL,
	[Descuento] [decimal](18, 2) NOT NULL
)
GO
/****** Object:  Table [dbo].[Credito]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Cuota]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Cliente]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  View [dbo].[vw_CuotasVencidas]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =============================================================================
   D) VISTAS DE APOYO
   ============================================================================= */

-- Cuotas vencidas (morosidad)
CREATE   VIEW [dbo].[vw_CuotasVencidas] AS
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
/****** Object:  Table [dbo].[Garantia]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[InventarioImei]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  View [dbo].[vw_GarantiaVigentePorImei]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Garantia vigente por IMEI (consulta rapida)
CREATE   VIEW [dbo].[vw_GarantiaVigentePorImei] AS
SELECT
    g.GarantiaId, g.ImeiId, i.Imei, g.ClienteId, g.VentaId,
    g.FechaInicio, g.FechaVencimiento, g.MesesCobertura, g.Estado,
    CASE WHEN g.Estado = N'Vigente' AND g.FechaVencimiento >= CAST(SYSDATETIME() AS DATE)
         THEN 1 ELSE 0 END AS Vigente
FROM dbo.Garantia g
LEFT JOIN dbo.InventarioImei i ON i.ImeiId = g.ImeiId;
GO
/****** Object:  Table [dbo].[Marca]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Producto]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Producto](
	[ProductoId] [int] IDENTITY(1,1) NOT NULL,
	[MarcaId] [int] NULL,
	[CategoriaId] [int] NULL,
	[Nombre] [nvarchar](150) NOT NULL,
	[Descripcion] [nvarchar](300) NULL,
	[Serializado] [bit] NOT NULL,
	[Activo] [bit] NOT NULL,
	[FechaCreacion] [datetime2](0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ProductoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductoVariante]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  View [dbo].[vw_InventarioDisponible]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE   VIEW [dbo].[vw_InventarioDisponible] AS
-- Inventario disponible por modelo/variante/sucursal
SELECT
    p.ProductoId, p.Nombre AS Producto, m.Nombre AS Marca,
    v.VarianteId, v.Color, v.Almacenamiento, v.Condicion, v.PrecioVenta,
    v.StockMinimo,
    i.SucursalId, COUNT(i.ImeiId) AS Disponibles
FROM dbo.InventarioImei i
JOIN dbo.ProductoVariante v ON v.VarianteId = i.VarianteId
JOIN dbo.Producto p        ON p.ProductoId = v.ProductoId
LEFT JOIN dbo.Marca m      ON m.MarcaId = p.MarcaId
WHERE i.Estado = N'Disponible'
GROUP BY p.ProductoId, p.Nombre, m.Nombre, v.VarianteId, v.Color, v.Almacenamiento, v.Condicion, v.PrecioVenta, v.StockMinimo, i.SucursalId;

GO
/****** Object:  Table [dbo].[CasoGarantia]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  View [dbo].[vw_IndiceFallasPorModelo]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Indice de fallas por modelo (a partir de los casos de garantia/RMA)
CREATE   VIEW [dbo].[vw_IndiceFallasPorModelo] AS
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
/****** Object:  Table [dbo].[AbonoApartado]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Apartado]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[AsientoContable]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[AsientoDetalle]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Categoria]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Comision]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Compra]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
PRIMARY KEY CLUSTERED 
(
	[CompraId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ComprobanteAnulado]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[CuentaContable]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Empresa]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Faltante]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[LogAuditoria]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[MetodoPago]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[MovimientoCaja]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[MovimientoInventario]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[NotaCredito]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[OrdenTaller]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[OrdenTallerFoto]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[OrdenTallerRepuesto]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[PadronRnc]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PadronRnc](
	[Rnc] [nvarchar](11) NOT NULL,
	[Nombre] [nvarchar](200) NOT NULL,
	[Estado] [nvarchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[Rnc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PagoCredito]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[PagoEmpleado]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[PagoProveedor]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Permiso]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Proveedor]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
PRIMARY KEY CLUSTERED 
(
	[ProveedorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RefreshToken]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Rol]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[RolPermiso]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolPermiso](
	[RolId] [int] NOT NULL,
	[PermisoId] [int] NOT NULL,
 CONSTRAINT [PK_RolPermiso] PRIMARY KEY CLUSTERED 
(
	[RolId] ASC,
	[PermisoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Secuencia]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[SecuenciaNcf]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[SesionCaja]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Sucursal]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[Usuario]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
PRIMARY KEY CLUSTERED 
(
	[UsuarioId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Usuario_NombreUsuario] UNIQUE NONCLUSTERED 
(
	[NombreUsuario] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Venta]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[VentaDetalle]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Table [dbo].[VentaPago]    Script Date: 7/1/2026 11:15:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  Index [IX_AsientoContable_Fecha]    Script Date: 7/1/2026 11:15:17 AM ******/
CREATE NONCLUSTERED INDEX [IX_AsientoContable_Fecha] ON [dbo].[AsientoContable]
(
	[Fecha] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_Asiento_Origen_Referencia]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Asiento_Origen_Referencia] ON [dbo].[AsientoContable]
(
	[Origen] ASC,
	[ReferenciaId] ASC
)
WHERE ([ReferenciaId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AsientoDetalle_Cuenta]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_AsientoDetalle_Cuenta] ON [dbo].[AsientoDetalle]
(
	[CuentaContableId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Caso_Estado]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Caso_Estado] ON [dbo].[CasoGarantia]
(
	[Estado] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Caso_Imei]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Caso_Imei] ON [dbo].[CasoGarantia]
(
	[ImeiId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Cliente_Cedula]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Cliente_Cedula] ON [dbo].[Cliente]
(
	[Cedula] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Cliente_Telefono]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Cliente_Telefono] ON [dbo].[Cliente]
(
	[Telefono] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Credito_Cliente]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Credito_Cliente] ON [dbo].[Credito]
(
	[ClienteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Cuota_Credito]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Cuota_Credito] ON [dbo].[Cuota]
(
	[CreditoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Cuota_Vencimiento]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Cuota_Vencimiento] ON [dbo].[Cuota]
(
	[FechaVencimiento] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Garantia_Imei]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Garantia_Imei] ON [dbo].[Garantia]
(
	[ImeiId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Imei_Estado]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Imei_Estado] ON [dbo].[InventarioImei]
(
	[Estado] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Imei_Variante]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Imei_Variante] ON [dbo].[InventarioImei]
(
	[VarianteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Log_Fecha]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Log_Fecha] ON [dbo].[LogAuditoria]
(
	[Fecha] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MovInv_Imei]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_MovInv_Imei] ON [dbo].[MovimientoInventario]
(
	[ImeiId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_NotaCredito_Fecha]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_NotaCredito_Fecha] ON [dbo].[NotaCredito]
(
	[FechaRegistro] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Orden_Estado]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Orden_Estado] ON [dbo].[OrdenTaller]
(
	[Estado] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Variante_Producto]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Variante_Producto] ON [dbo].[ProductoVariante]
(
	[ProductoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Venta_Cliente]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Venta_Cliente] ON [dbo].[Venta]
(
	[ClienteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Venta_Fecha]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_Venta_Fecha] ON [dbo].[Venta]
(
	[Fecha] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_VtaDet_Venta]    Script Date: 7/1/2026 11:15:18 AM ******/
CREATE NONCLUSTERED INDEX [IX_VtaDet_Venta] ON [dbo].[VentaDetalle]
(
	[VentaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AbonoApartado] ADD  DEFAULT ('Abono') FOR [Tipo]
GO
ALTER TABLE [dbo].[AbonoApartado] ADD  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[Apartado] ADD  DEFAULT ((0)) FOR [TotalAbonado]
GO
ALTER TABLE [dbo].[Apartado] ADD  DEFAULT ('Activo') FOR [Estado]
GO
ALTER TABLE [dbo].[Apartado] ADD  DEFAULT (sysdatetime()) FOR [FechaInicio]
GO
ALTER TABLE [dbo].[AsientoContable] ADD  DEFAULT ('Manual') FOR [Origen]
GO
ALTER TABLE [dbo].[AsientoContable] ADD  DEFAULT ('Registrado') FOR [Estado]
GO
ALTER TABLE [dbo].[AsientoContable] ADD  DEFAULT (sysdatetime()) FOR [FechaRegistro]
GO
ALTER TABLE [dbo].[AsientoDetalle] ADD  DEFAULT ((0)) FOR [Debito]
GO
ALTER TABLE [dbo].[AsientoDetalle] ADD  DEFAULT ((0)) FOR [Credito]
GO
ALTER TABLE [dbo].[CasoGarantia] ADD  CONSTRAINT [DF_Caso_Apertura]  DEFAULT (sysdatetime()) FOR [FechaApertura]
GO
ALTER TABLE [dbo].[CasoGarantia] ADD  CONSTRAINT [DF_Caso_Estado]  DEFAULT (N'Abierto') FOR [Estado]
GO
ALTER TABLE [dbo].[Cliente] ADD  CONSTRAINT [DF_Cliente_Moroso]  DEFAULT ((0)) FOR [EsMoroso]
GO
ALTER TABLE [dbo].[Cliente] ADD  CONSTRAINT [DF_Cliente_Bloqueado]  DEFAULT ((0)) FOR [Bloqueado]
GO
ALTER TABLE [dbo].[Cliente] ADD  CONSTRAINT [DF_Cliente_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[Comision] ADD  CONSTRAINT [DF_Comision_Origen]  DEFAULT (N'Venta') FOR [OrigenTipo]
GO
ALTER TABLE [dbo].[Comision] ADD  CONSTRAINT [DF_Comision_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[Compra] ADD  CONSTRAINT [DF_Compra_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[Compra] ADD  CONSTRAINT [DF_Compra_Total]  DEFAULT ((0)) FOR [Total]
GO
ALTER TABLE [dbo].[ComprobanteAnulado] ADD  DEFAULT (sysdatetime()) FOR [FechaRegistro]
GO
ALTER TABLE [dbo].[Credito] ADD  CONSTRAINT [DF_Credito_Inicial]  DEFAULT ((0)) FOR [Inicial]
GO
ALTER TABLE [dbo].[Credito] ADD  CONSTRAINT [DF_Credito_Tasa]  DEFAULT ((0)) FOR [TasaInteres]
GO
ALTER TABLE [dbo].[Credito] ADD  CONSTRAINT [DF_Credito_Estado]  DEFAULT (N'Activo') FOR [Estado]
GO
ALTER TABLE [dbo].[Credito] ADD  CONSTRAINT [DF_Credito_Fecha]  DEFAULT (sysdatetime()) FOR [FechaInicio]
GO
ALTER TABLE [dbo].[CuentaContable] ADD  DEFAULT ((1)) FOR [PermiteMovimiento]
GO
ALTER TABLE [dbo].[CuentaContable] ADD  DEFAULT ((0)) FOR [EsSistema]
GO
ALTER TABLE [dbo].[CuentaContable] ADD  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Cuota] ADD  CONSTRAINT [DF_Cuota_Pagado]  DEFAULT ((0)) FOR [MontoPagado]
GO
ALTER TABLE [dbo].[Cuota] ADD  CONSTRAINT [DF_Cuota_Estado]  DEFAULT (N'Pendiente') FOR [Estado]
GO
ALTER TABLE [dbo].[Empresa] ADD  CONSTRAINT [DF_Empresa_Moneda]  DEFAULT (N'DOP') FOR [Moneda]
GO
ALTER TABLE [dbo].[Empresa] ADD  CONSTRAINT [DF_Empresa_Itbis]  DEFAULT ((18.0000)) FOR [PorcentajeItbis]
GO
ALTER TABLE [dbo].[Empresa] ADD  CONSTRAINT [DF_Empresa_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[Faltante] ADD  DEFAULT ((1)) FOR [CantidadDeseada]
GO
ALTER TABLE [dbo].[Faltante] ADD  DEFAULT ((0)) FOR [Resuelto]
GO
ALTER TABLE [dbo].[Faltante] ADD  DEFAULT (sysdatetime()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[Garantia] ADD  CONSTRAINT [DF_Garantia_Inicio]  DEFAULT (CONVERT([date],sysdatetime())) FOR [FechaInicio]
GO
ALTER TABLE [dbo].[Garantia] ADD  CONSTRAINT [DF_Garantia_Meses]  DEFAULT ((3)) FOR [MesesCobertura]
GO
ALTER TABLE [dbo].[Garantia] ADD  CONSTRAINT [DF_Garantia_Estado]  DEFAULT (N'Vigente') FOR [Estado]
GO
ALTER TABLE [dbo].[Garantia] ADD  CONSTRAINT [DF_Garantia_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[InventarioImei] ADD  CONSTRAINT [DF_Imei_Costo]  DEFAULT ((0)) FOR [PrecioCosto]
GO
ALTER TABLE [dbo].[InventarioImei] ADD  CONSTRAINT [DF_Imei_Estado]  DEFAULT (N'Disponible') FOR [Estado]
GO
ALTER TABLE [dbo].[InventarioImei] ADD  CONSTRAINT [DF_Imei_Fecha]  DEFAULT (sysdatetime()) FOR [FechaIngreso]
GO
ALTER TABLE [dbo].[LogAuditoria] ADD  CONSTRAINT [DF_Log_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[MovimientoCaja] ADD  CONSTRAINT [DF_MovCaja_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[MovimientoInventario] ADD  CONSTRAINT [DF_MovInv_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[NotaCredito] ADD  DEFAULT ((0)) FOR [Monto]
GO
ALTER TABLE [dbo].[NotaCredito] ADD  DEFAULT ((0)) FOR [Itbis]
GO
ALTER TABLE [dbo].[NotaCredito] ADD  DEFAULT ((0)) FOR [Total]
GO
ALTER TABLE [dbo].[NotaCredito] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
GO
ALTER TABLE [dbo].[OrdenTaller] ADD  CONSTRAINT [DF_Orden_Estado]  DEFAULT (N'Recibido') FOR [Estado]
GO
ALTER TABLE [dbo].[OrdenTaller] ADD  CONSTRAINT [DF_Orden_Anticipo]  DEFAULT ((0)) FOR [Anticipo]
GO
ALTER TABLE [dbo].[OrdenTaller] ADD  CONSTRAINT [DF_Orden_CostoEst]  DEFAULT ((0)) FOR [CostoEstimado]
GO
ALTER TABLE [dbo].[OrdenTaller] ADD  CONSTRAINT [DF_Orden_Comision]  DEFAULT ((0)) FOR [ComisionTecnico]
GO
ALTER TABLE [dbo].[OrdenTaller] ADD  CONSTRAINT [DF_Orden_FRecep]  DEFAULT (sysdatetime()) FOR [FechaRecepcion]
GO
ALTER TABLE [dbo].[OrdenTallerFoto] ADD  CONSTRAINT [DF_OrdFoto_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[OrdenTallerRepuesto] ADD  CONSTRAINT [DF_OrdRep_Cant]  DEFAULT ((1)) FOR [Cantidad]
GO
ALTER TABLE [dbo].[OrdenTallerRepuesto] ADD  CONSTRAINT [DF_OrdRep_Costo]  DEFAULT ((0)) FOR [Costo]
GO
ALTER TABLE [dbo].[PagoCredito] ADD  CONSTRAINT [DF_PagoCred_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[PagoEmpleado] ADD  DEFAULT ('Salario') FOR [Tipo]
GO
ALTER TABLE [dbo].[PagoEmpleado] ADD  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[PagoProveedor] ADD  CONSTRAINT [DF_PagoProv_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[Producto] ADD  CONSTRAINT [DF_Producto_Serial]  DEFAULT ((1)) FOR [Serializado]
GO
ALTER TABLE [dbo].[Producto] ADD  CONSTRAINT [DF_Producto_Activo]  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Producto] ADD  CONSTRAINT [DF_Producto_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[ProductoVariante] ADD  CONSTRAINT [DF_Variante_PVenta]  DEFAULT ((0)) FOR [PrecioVenta]
GO
ALTER TABLE [dbo].[ProductoVariante] ADD  CONSTRAINT [DF_Variante_PCosto]  DEFAULT ((0)) FOR [PrecioCosto]
GO
ALTER TABLE [dbo].[ProductoVariante] ADD  CONSTRAINT [DF_Variante_Stock]  DEFAULT ((0)) FOR [StockNoSerial]
GO
ALTER TABLE [dbo].[ProductoVariante] ADD  CONSTRAINT [DF_Variante_Activo]  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[ProductoVariante] ADD  CONSTRAINT [DF_ProductoVariante_StockMinimo]  DEFAULT ((0)) FOR [StockMinimo]
GO
ALTER TABLE [dbo].[Proveedor] ADD  CONSTRAINT [DF_Proveedor_Balance]  DEFAULT ((0)) FOR [Balance]
GO
ALTER TABLE [dbo].[Proveedor] ADD  CONSTRAINT [DF_Proveedor_Activo]  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Proveedor] ADD  CONSTRAINT [DF_Proveedor_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[RefreshToken] ADD  CONSTRAINT [DF_RefreshToken_Rev]  DEFAULT ((0)) FOR [Revocado]
GO
ALTER TABLE [dbo].[RefreshToken] ADD  CONSTRAINT [DF_RefreshToken_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[Secuencia] ADD  DEFAULT (N'') FOR [Prefijo]
GO
ALTER TABLE [dbo].[Secuencia] ADD  DEFAULT ((0)) FOR [Valor]
GO
ALTER TABLE [dbo].[Secuencia] ADD  DEFAULT ((8)) FOR [Longitud]
GO
ALTER TABLE [dbo].[SecuenciaNcf] ADD  DEFAULT ('B') FOR [Serie]
GO
ALTER TABLE [dbo].[SecuenciaNcf] ADD  DEFAULT ((1)) FOR [Secuencia]
GO
ALTER TABLE [dbo].[SecuenciaNcf] ADD  DEFAULT ((0)) FOR [Hasta]
GO
ALTER TABLE [dbo].[SecuenciaNcf] ADD  DEFAULT ((0)) FOR [Activo]
GO
ALTER TABLE [dbo].[SesionCaja] ADD  CONSTRAINT [DF_Sesion_Apertura]  DEFAULT ((0)) FOR [MontoApertura]
GO
ALTER TABLE [dbo].[SesionCaja] ADD  CONSTRAINT [DF_Sesion_Estado]  DEFAULT (N'Abierta') FOR [Estado]
GO
ALTER TABLE [dbo].[SesionCaja] ADD  CONSTRAINT [DF_Sesion_FApertura]  DEFAULT (sysdatetime()) FOR [FechaApertura]
GO
ALTER TABLE [dbo].[Sucursal] ADD  CONSTRAINT [DF_Sucursal_Activa]  DEFAULT ((1)) FOR [Activa]
GO
ALTER TABLE [dbo].[Sucursal] ADD  CONSTRAINT [DF_Sucursal_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[Usuario] ADD  CONSTRAINT [DF_Usuario_Activo]  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Usuario] ADD  CONSTRAINT [DF_Usuario_Fecha]  DEFAULT (sysdatetime()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Fecha]  DEFAULT (sysdatetime()) FOR [Fecha]
GO
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Subtotal]  DEFAULT ((0)) FOR [Subtotal]
GO
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Descuento]  DEFAULT ((0)) FOR [Descuento]
GO
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Impuesto]  DEFAULT ((0)) FOR [Impuesto]
GO
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Total]  DEFAULT ((0)) FOR [Total]
GO
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_EsCredito]  DEFAULT ((0)) FOR [EsCredito]
GO
ALTER TABLE [dbo].[Venta] ADD  CONSTRAINT [DF_Venta_Estado]  DEFAULT (N'Completada') FOR [Estado]
GO
ALTER TABLE [dbo].[VentaDetalle] ADD  CONSTRAINT [DF_VtaDet_Cant]  DEFAULT ((1)) FOR [Cantidad]
GO
ALTER TABLE [dbo].[VentaDetalle] ADD  CONSTRAINT [DF_VtaDet_Desc]  DEFAULT ((0)) FOR [Descuento]
GO
ALTER TABLE [dbo].[VentaDetalle] ADD  CONSTRAINT [DF_VtaDet_Imp]  DEFAULT ((0)) FOR [Impuesto]
GO
ALTER TABLE [dbo].[AbonoApartado]  WITH CHECK ADD  CONSTRAINT [FK_AbonoApartado_Apartado] FOREIGN KEY([ApartadoId])
REFERENCES [dbo].[Apartado] ([ApartadoId])
GO
ALTER TABLE [dbo].[AbonoApartado] CHECK CONSTRAINT [FK_AbonoApartado_Apartado]
GO
ALTER TABLE [dbo].[Apartado]  WITH CHECK ADD  CONSTRAINT [FK_Apartado_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
ALTER TABLE [dbo].[Apartado] CHECK CONSTRAINT [FK_Apartado_Cliente]
GO
ALTER TABLE [dbo].[Apartado]  WITH CHECK ADD  CONSTRAINT [FK_Apartado_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
ALTER TABLE [dbo].[Apartado] CHECK CONSTRAINT [FK_Apartado_Imei]
GO
ALTER TABLE [dbo].[Apartado]  WITH CHECK ADD  CONSTRAINT [FK_Apartado_Sucursal_Fix] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
ALTER TABLE [dbo].[Apartado] CHECK CONSTRAINT [FK_Apartado_Sucursal_Fix]
GO
ALTER TABLE [dbo].[Apartado]  WITH CHECK ADD  CONSTRAINT [FK_Apartado_Usuario_Fix] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[Apartado] CHECK CONSTRAINT [FK_Apartado_Usuario_Fix]
GO
ALTER TABLE [dbo].[Apartado]  WITH CHECK ADD  CONSTRAINT [FK_Apartado_Variante] FOREIGN KEY([VarianteId])
REFERENCES [dbo].[ProductoVariante] ([VarianteId])
GO
ALTER TABLE [dbo].[Apartado] CHECK CONSTRAINT [FK_Apartado_Variante]
GO
ALTER TABLE [dbo].[AsientoDetalle]  WITH CHECK ADD  CONSTRAINT [FK_AsientoDetalle_Asiento] FOREIGN KEY([AsientoContableId])
REFERENCES [dbo].[AsientoContable] ([AsientoContableId])
GO
ALTER TABLE [dbo].[AsientoDetalle] CHECK CONSTRAINT [FK_AsientoDetalle_Asiento]
GO
ALTER TABLE [dbo].[AsientoDetalle]  WITH CHECK ADD  CONSTRAINT [FK_AsientoDetalle_Cuenta] FOREIGN KEY([CuentaContableId])
REFERENCES [dbo].[CuentaContable] ([CuentaContableId])
GO
ALTER TABLE [dbo].[AsientoDetalle] CHECK CONSTRAINT [FK_AsientoDetalle_Cuenta]
GO
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [FK_Caso_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [FK_Caso_Cliente]
GO
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [FK_Caso_Garantia] FOREIGN KEY([GarantiaId])
REFERENCES [dbo].[Garantia] ([GarantiaId])
GO
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [FK_Caso_Garantia]
GO
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [FK_Caso_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [FK_Caso_Imei]
GO
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [FK_Caso_ImeiReemp] FOREIGN KEY([ImeiReemplazoId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [FK_Caso_ImeiReemp]
GO
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [FK_Caso_Orden] FOREIGN KEY([OrdenTallerId])
REFERENCES [dbo].[OrdenTaller] ([OrdenTallerId])
GO
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [FK_Caso_Orden]
GO
ALTER TABLE [dbo].[Comision]  WITH CHECK ADD  CONSTRAINT [FK_Comision_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[Comision] CHECK CONSTRAINT [FK_Comision_Usuario]
GO
ALTER TABLE [dbo].[Comision]  WITH CHECK ADD  CONSTRAINT [FK_Comision_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
ALTER TABLE [dbo].[Comision] CHECK CONSTRAINT [FK_Comision_Venta]
GO
ALTER TABLE [dbo].[Compra]  WITH CHECK ADD  CONSTRAINT [FK_Compra_MetodoPago] FOREIGN KEY([MetodoPagoId])
REFERENCES [dbo].[MetodoPago] ([MetodoPagoId])
GO
ALTER TABLE [dbo].[Compra] CHECK CONSTRAINT [FK_Compra_MetodoPago]
GO
ALTER TABLE [dbo].[Compra]  WITH CHECK ADD  CONSTRAINT [FK_Compra_Proveedor] FOREIGN KEY([ProveedorId])
REFERENCES [dbo].[Proveedor] ([ProveedorId])
GO
ALTER TABLE [dbo].[Compra] CHECK CONSTRAINT [FK_Compra_Proveedor]
GO
ALTER TABLE [dbo].[Compra]  WITH CHECK ADD  CONSTRAINT [FK_Compra_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
ALTER TABLE [dbo].[Compra] CHECK CONSTRAINT [FK_Compra_Sucursal]
GO
ALTER TABLE [dbo].[ComprobanteAnulado]  WITH CHECK ADD  CONSTRAINT [FK_ComprobanteAnulado_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
ALTER TABLE [dbo].[ComprobanteAnulado] CHECK CONSTRAINT [FK_ComprobanteAnulado_Venta]
GO
ALTER TABLE [dbo].[Credito]  WITH CHECK ADD  CONSTRAINT [FK_Credito_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
ALTER TABLE [dbo].[Credito] CHECK CONSTRAINT [FK_Credito_Cliente]
GO
ALTER TABLE [dbo].[Credito]  WITH CHECK ADD  CONSTRAINT [FK_Credito_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
ALTER TABLE [dbo].[Credito] CHECK CONSTRAINT [FK_Credito_Venta]
GO
ALTER TABLE [dbo].[CuentaContable]  WITH CHECK ADD  CONSTRAINT [FK_CuentaContable_Padre] FOREIGN KEY([CuentaPadreId])
REFERENCES [dbo].[CuentaContable] ([CuentaContableId])
GO
ALTER TABLE [dbo].[CuentaContable] CHECK CONSTRAINT [FK_CuentaContable_Padre]
GO
ALTER TABLE [dbo].[Cuota]  WITH CHECK ADD  CONSTRAINT [FK_Cuota_Credito] FOREIGN KEY([CreditoId])
REFERENCES [dbo].[Credito] ([CreditoId])
GO
ALTER TABLE [dbo].[Cuota] CHECK CONSTRAINT [FK_Cuota_Credito]
GO
ALTER TABLE [dbo].[Faltante]  WITH CHECK ADD  CONSTRAINT [FK_Faltante_Variante] FOREIGN KEY([VarianteId])
REFERENCES [dbo].[ProductoVariante] ([VarianteId])
GO
ALTER TABLE [dbo].[Faltante] CHECK CONSTRAINT [FK_Faltante_Variante]
GO
ALTER TABLE [dbo].[Garantia]  WITH CHECK ADD  CONSTRAINT [FK_Garantia_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
ALTER TABLE [dbo].[Garantia] CHECK CONSTRAINT [FK_Garantia_Cliente]
GO
ALTER TABLE [dbo].[Garantia]  WITH CHECK ADD  CONSTRAINT [FK_Garantia_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
ALTER TABLE [dbo].[Garantia] CHECK CONSTRAINT [FK_Garantia_Imei]
GO
ALTER TABLE [dbo].[Garantia]  WITH CHECK ADD  CONSTRAINT [FK_Garantia_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
ALTER TABLE [dbo].[Garantia] CHECK CONSTRAINT [FK_Garantia_Venta]
GO
ALTER TABLE [dbo].[Garantia]  WITH CHECK ADD  CONSTRAINT [FK_Garantia_VtaDet] FOREIGN KEY([VentaDetalleId])
REFERENCES [dbo].[VentaDetalle] ([VentaDetalleId])
GO
ALTER TABLE [dbo].[Garantia] CHECK CONSTRAINT [FK_Garantia_VtaDet]
GO
ALTER TABLE [dbo].[InventarioImei]  WITH CHECK ADD  CONSTRAINT [FK_Imei_Compra] FOREIGN KEY([CompraId])
REFERENCES [dbo].[Compra] ([CompraId])
GO
ALTER TABLE [dbo].[InventarioImei] CHECK CONSTRAINT [FK_Imei_Compra]
GO
ALTER TABLE [dbo].[InventarioImei]  WITH CHECK ADD  CONSTRAINT [FK_Imei_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
ALTER TABLE [dbo].[InventarioImei] CHECK CONSTRAINT [FK_Imei_Sucursal]
GO
ALTER TABLE [dbo].[InventarioImei]  WITH CHECK ADD  CONSTRAINT [FK_Imei_Sucursal_Fix] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
ALTER TABLE [dbo].[InventarioImei] CHECK CONSTRAINT [FK_Imei_Sucursal_Fix]
GO
ALTER TABLE [dbo].[InventarioImei]  WITH CHECK ADD  CONSTRAINT [FK_Imei_Variante] FOREIGN KEY([VarianteId])
REFERENCES [dbo].[ProductoVariante] ([VarianteId])
GO
ALTER TABLE [dbo].[InventarioImei] CHECK CONSTRAINT [FK_Imei_Variante]
GO
ALTER TABLE [dbo].[LogAuditoria]  WITH CHECK ADD  CONSTRAINT [FK_Log_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[LogAuditoria] CHECK CONSTRAINT [FK_Log_Usuario]
GO
ALTER TABLE [dbo].[MovimientoCaja]  WITH CHECK ADD  CONSTRAINT [FK_MovCaja_Sesion] FOREIGN KEY([SesionCajaId])
REFERENCES [dbo].[SesionCaja] ([SesionCajaId])
GO
ALTER TABLE [dbo].[MovimientoCaja] CHECK CONSTRAINT [FK_MovCaja_Sesion]
GO
ALTER TABLE [dbo].[MovimientoInventario]  WITH CHECK ADD  CONSTRAINT [FK_MovInv_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
ALTER TABLE [dbo].[MovimientoInventario] CHECK CONSTRAINT [FK_MovInv_Imei]
GO
ALTER TABLE [dbo].[MovimientoInventario]  WITH CHECK ADD  CONSTRAINT [FK_MovInv_SucDest] FOREIGN KEY([SucursalDestino])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
ALTER TABLE [dbo].[MovimientoInventario] CHECK CONSTRAINT [FK_MovInv_SucDest]
GO
ALTER TABLE [dbo].[MovimientoInventario]  WITH CHECK ADD  CONSTRAINT [FK_MovInv_SucOrig] FOREIGN KEY([SucursalOrigen])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
ALTER TABLE [dbo].[MovimientoInventario] CHECK CONSTRAINT [FK_MovInv_SucOrig]
GO
ALTER TABLE [dbo].[MovimientoInventario]  WITH CHECK ADD  CONSTRAINT [FK_MovInv_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[MovimientoInventario] CHECK CONSTRAINT [FK_MovInv_Usuario]
GO
ALTER TABLE [dbo].[NotaCredito]  WITH CHECK ADD  CONSTRAINT [FK_NotaCredito_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
ALTER TABLE [dbo].[NotaCredito] CHECK CONSTRAINT [FK_NotaCredito_Venta]
GO
ALTER TABLE [dbo].[OrdenTaller]  WITH CHECK ADD  CONSTRAINT [FK_Orden_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
ALTER TABLE [dbo].[OrdenTaller] CHECK CONSTRAINT [FK_Orden_Cliente]
GO
ALTER TABLE [dbo].[OrdenTaller]  WITH CHECK ADD  CONSTRAINT [FK_Orden_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
ALTER TABLE [dbo].[OrdenTaller] CHECK CONSTRAINT [FK_Orden_Imei]
GO
ALTER TABLE [dbo].[OrdenTaller]  WITH CHECK ADD  CONSTRAINT [FK_Orden_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
ALTER TABLE [dbo].[OrdenTaller] CHECK CONSTRAINT [FK_Orden_Sucursal]
GO
ALTER TABLE [dbo].[OrdenTaller]  WITH CHECK ADD  CONSTRAINT [FK_Orden_Tecnico] FOREIGN KEY([TecnicoId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[OrdenTaller] CHECK CONSTRAINT [FK_Orden_Tecnico]
GO
ALTER TABLE [dbo].[OrdenTallerFoto]  WITH CHECK ADD  CONSTRAINT [FK_OrdFoto_Orden] FOREIGN KEY([OrdenTallerId])
REFERENCES [dbo].[OrdenTaller] ([OrdenTallerId])
GO
ALTER TABLE [dbo].[OrdenTallerFoto] CHECK CONSTRAINT [FK_OrdFoto_Orden]
GO
ALTER TABLE [dbo].[OrdenTallerRepuesto]  WITH CHECK ADD  CONSTRAINT [FK_OrdRep_Orden] FOREIGN KEY([OrdenTallerId])
REFERENCES [dbo].[OrdenTaller] ([OrdenTallerId])
GO
ALTER TABLE [dbo].[OrdenTallerRepuesto] CHECK CONSTRAINT [FK_OrdRep_Orden]
GO
ALTER TABLE [dbo].[OrdenTallerRepuesto]  WITH CHECK ADD  CONSTRAINT [FK_OrdRep_Variante] FOREIGN KEY([VarianteId])
REFERENCES [dbo].[ProductoVariante] ([VarianteId])
GO
ALTER TABLE [dbo].[OrdenTallerRepuesto] CHECK CONSTRAINT [FK_OrdRep_Variante]
GO
ALTER TABLE [dbo].[PagoCredito]  WITH CHECK ADD  CONSTRAINT [FK_PagoCred_Credito] FOREIGN KEY([CreditoId])
REFERENCES [dbo].[Credito] ([CreditoId])
GO
ALTER TABLE [dbo].[PagoCredito] CHECK CONSTRAINT [FK_PagoCred_Credito]
GO
ALTER TABLE [dbo].[PagoCredito]  WITH CHECK ADD  CONSTRAINT [FK_PagoCred_Cuota] FOREIGN KEY([CuotaId])
REFERENCES [dbo].[Cuota] ([CuotaId])
GO
ALTER TABLE [dbo].[PagoCredito] CHECK CONSTRAINT [FK_PagoCred_Cuota]
GO
ALTER TABLE [dbo].[PagoCredito]  WITH CHECK ADD  CONSTRAINT [FK_PagoCred_Metodo] FOREIGN KEY([MetodoPagoId])
REFERENCES [dbo].[MetodoPago] ([MetodoPagoId])
GO
ALTER TABLE [dbo].[PagoCredito] CHECK CONSTRAINT [FK_PagoCred_Metodo]
GO
ALTER TABLE [dbo].[PagoCredito]  WITH CHECK ADD  CONSTRAINT [FK_PagoCred_Sesion] FOREIGN KEY([SesionCajaId])
REFERENCES [dbo].[SesionCaja] ([SesionCajaId])
GO
ALTER TABLE [dbo].[PagoCredito] CHECK CONSTRAINT [FK_PagoCred_Sesion]
GO
ALTER TABLE [dbo].[PagoCredito]  WITH CHECK ADD  CONSTRAINT [FK_PagoCred_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[PagoCredito] CHECK CONSTRAINT [FK_PagoCred_Usuario]
GO
ALTER TABLE [dbo].[PagoEmpleado]  WITH CHECK ADD  CONSTRAINT [FK_PagoEmpleado_Empleado] FOREIGN KEY([EmpleadoId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[PagoEmpleado] CHECK CONSTRAINT [FK_PagoEmpleado_Empleado]
GO
ALTER TABLE [dbo].[PagoProveedor]  WITH CHECK ADD  CONSTRAINT [FK_PagoProv_Compra] FOREIGN KEY([CompraId])
REFERENCES [dbo].[Compra] ([CompraId])
GO
ALTER TABLE [dbo].[PagoProveedor] CHECK CONSTRAINT [FK_PagoProv_Compra]
GO
ALTER TABLE [dbo].[PagoProveedor]  WITH CHECK ADD  CONSTRAINT [FK_PagoProv_Proveedor] FOREIGN KEY([ProveedorId])
REFERENCES [dbo].[Proveedor] ([ProveedorId])
GO
ALTER TABLE [dbo].[PagoProveedor] CHECK CONSTRAINT [FK_PagoProv_Proveedor]
GO
ALTER TABLE [dbo].[Producto]  WITH CHECK ADD  CONSTRAINT [FK_Producto_Categoria] FOREIGN KEY([CategoriaId])
REFERENCES [dbo].[Categoria] ([CategoriaId])
GO
ALTER TABLE [dbo].[Producto] CHECK CONSTRAINT [FK_Producto_Categoria]
GO
ALTER TABLE [dbo].[Producto]  WITH CHECK ADD  CONSTRAINT [FK_Producto_Marca] FOREIGN KEY([MarcaId])
REFERENCES [dbo].[Marca] ([MarcaId])
GO
ALTER TABLE [dbo].[Producto] CHECK CONSTRAINT [FK_Producto_Marca]
GO
ALTER TABLE [dbo].[ProductoVariante]  WITH CHECK ADD  CONSTRAINT [FK_Variante_Producto] FOREIGN KEY([ProductoId])
REFERENCES [dbo].[Producto] ([ProductoId])
GO
ALTER TABLE [dbo].[ProductoVariante] CHECK CONSTRAINT [FK_Variante_Producto]
GO
ALTER TABLE [dbo].[RefreshToken]  WITH CHECK ADD  CONSTRAINT [FK_RefreshToken_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[RefreshToken] CHECK CONSTRAINT [FK_RefreshToken_Usuario]
GO
ALTER TABLE [dbo].[RolPermiso]  WITH CHECK ADD  CONSTRAINT [FK_RolPermiso_Permiso] FOREIGN KEY([PermisoId])
REFERENCES [dbo].[Permiso] ([PermisoId])
GO
ALTER TABLE [dbo].[RolPermiso] CHECK CONSTRAINT [FK_RolPermiso_Permiso]
GO
ALTER TABLE [dbo].[RolPermiso]  WITH CHECK ADD  CONSTRAINT [FK_RolPermiso_Rol] FOREIGN KEY([RolId])
REFERENCES [dbo].[Rol] ([RolId])
GO
ALTER TABLE [dbo].[RolPermiso] CHECK CONSTRAINT [FK_RolPermiso_Rol]
GO
ALTER TABLE [dbo].[SesionCaja]  WITH CHECK ADD  CONSTRAINT [FK_Sesion_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
ALTER TABLE [dbo].[SesionCaja] CHECK CONSTRAINT [FK_Sesion_Sucursal]
GO
ALTER TABLE [dbo].[SesionCaja]  WITH CHECK ADD  CONSTRAINT [FK_Sesion_UsrAbre] FOREIGN KEY([UsuarioApertura])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[SesionCaja] CHECK CONSTRAINT [FK_Sesion_UsrAbre]
GO
ALTER TABLE [dbo].[SesionCaja]  WITH CHECK ADD  CONSTRAINT [FK_Sesion_UsrCierra] FOREIGN KEY([UsuarioCierre])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[SesionCaja] CHECK CONSTRAINT [FK_Sesion_UsrCierra]
GO
ALTER TABLE [dbo].[Usuario]  WITH CHECK ADD  CONSTRAINT [FK_Usuario_Rol] FOREIGN KEY([RolId])
REFERENCES [dbo].[Rol] ([RolId])
GO
ALTER TABLE [dbo].[Usuario] CHECK CONSTRAINT [FK_Usuario_Rol]
GO
ALTER TABLE [dbo].[Usuario]  WITH CHECK ADD  CONSTRAINT [FK_Usuario_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
ALTER TABLE [dbo].[Usuario] CHECK CONSTRAINT [FK_Usuario_Sucursal]
GO
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [FK_Venta_Cliente] FOREIGN KEY([ClienteId])
REFERENCES [dbo].[Cliente] ([ClienteId])
GO
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [FK_Venta_Cliente]
GO
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [FK_Venta_Sesion] FOREIGN KEY([SesionCajaId])
REFERENCES [dbo].[SesionCaja] ([SesionCajaId])
GO
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [FK_Venta_Sesion]
GO
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [FK_Venta_SesionCaja_Fix] FOREIGN KEY([SesionCajaId])
REFERENCES [dbo].[SesionCaja] ([SesionCajaId])
GO
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [FK_Venta_SesionCaja_Fix]
GO
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [FK_Venta_Sucursal] FOREIGN KEY([SucursalId])
REFERENCES [dbo].[Sucursal] ([SucursalId])
GO
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [FK_Venta_Sucursal]
GO
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [FK_Venta_Usuario] FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([UsuarioId])
GO
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [FK_Venta_Usuario]
GO
ALTER TABLE [dbo].[VentaDetalle]  WITH CHECK ADD  CONSTRAINT [FK_VtaDet_Imei] FOREIGN KEY([ImeiId])
REFERENCES [dbo].[InventarioImei] ([ImeiId])
GO
ALTER TABLE [dbo].[VentaDetalle] CHECK CONSTRAINT [FK_VtaDet_Imei]
GO
ALTER TABLE [dbo].[VentaDetalle]  WITH CHECK ADD  CONSTRAINT [FK_VtaDet_Variante] FOREIGN KEY([VarianteId])
REFERENCES [dbo].[ProductoVariante] ([VarianteId])
GO
ALTER TABLE [dbo].[VentaDetalle] CHECK CONSTRAINT [FK_VtaDet_Variante]
GO
ALTER TABLE [dbo].[VentaDetalle]  WITH CHECK ADD  CONSTRAINT [FK_VtaDet_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
ALTER TABLE [dbo].[VentaDetalle] CHECK CONSTRAINT [FK_VtaDet_Venta]
GO
ALTER TABLE [dbo].[VentaPago]  WITH CHECK ADD  CONSTRAINT [FK_VtaPago_Metodo] FOREIGN KEY([MetodoPagoId])
REFERENCES [dbo].[MetodoPago] ([MetodoPagoId])
GO
ALTER TABLE [dbo].[VentaPago] CHECK CONSTRAINT [FK_VtaPago_Metodo]
GO
ALTER TABLE [dbo].[VentaPago]  WITH CHECK ADD  CONSTRAINT [FK_VtaPago_Venta] FOREIGN KEY([VentaId])
REFERENCES [dbo].[Venta] ([VentaId])
GO
ALTER TABLE [dbo].[VentaPago] CHECK CONSTRAINT [FK_VtaPago_Venta]
GO
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [CK_Caso_Estado] CHECK  (([Estado]=N'Cerrado' OR [Estado]=N'Rechazado' OR [Estado]=N'Resuelto' OR [Estado]=N'EnProceso' OR [Estado]=N'Abierto'))
GO
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [CK_Caso_Estado]
GO
ALTER TABLE [dbo].[CasoGarantia]  WITH CHECK ADD  CONSTRAINT [CK_Caso_Resolucion] CHECK  (([TipoResolucion] IS NULL OR ([TipoResolucion]=N'Rechazado' OR [TipoResolucion]=N'NotaCredito' OR [TipoResolucion]=N'Reemplazo' OR [TipoResolucion]=N'Reparacion')))
GO
ALTER TABLE [dbo].[CasoGarantia] CHECK CONSTRAINT [CK_Caso_Resolucion]
GO
ALTER TABLE [dbo].[Comision]  WITH CHECK ADD  CONSTRAINT [CK_Comision_Origen] CHECK  (([OrigenTipo]=N'Taller' OR [OrigenTipo]=N'Venta'))
GO
ALTER TABLE [dbo].[Comision] CHECK CONSTRAINT [CK_Comision_Origen]
GO
ALTER TABLE [dbo].[Credito]  WITH CHECK ADD  CONSTRAINT [CK_Credito_Estado] CHECK  (([Estado]=N'Reestructurado' OR [Estado]=N'Saldado' OR [Estado]=N'EnMora' OR [Estado]=N'Activo'))
GO
ALTER TABLE [dbo].[Credito] CHECK CONSTRAINT [CK_Credito_Estado]
GO
ALTER TABLE [dbo].[Cuota]  WITH CHECK ADD  CONSTRAINT [CK_Cuota_Estado] CHECK  (([Estado]=N'Vencida' OR [Estado]=N'Pagada' OR [Estado]=N'Parcial' OR [Estado]=N'Pendiente'))
GO
ALTER TABLE [dbo].[Cuota] CHECK CONSTRAINT [CK_Cuota_Estado]
GO
ALTER TABLE [dbo].[Garantia]  WITH CHECK ADD  CONSTRAINT [CK_Garantia_Estado] CHECK  (([Estado]=N'Anulada' OR [Estado]=N'Vencida' OR [Estado]=N'Vigente'))
GO
ALTER TABLE [dbo].[Garantia] CHECK CONSTRAINT [CK_Garantia_Estado]
GO
ALTER TABLE [dbo].[InventarioImei]  WITH CHECK ADD  CONSTRAINT [CK_Imei_Estado] CHECK  (([Estado]=N'Reservado' OR [Estado]=N'Baja' OR [Estado]=N'Transferido' OR [Estado]=N'Devuelto' OR [Estado]=N'EnTaller' OR [Estado]=N'Vendido' OR [Estado]=N'Disponible'))
GO
ALTER TABLE [dbo].[InventarioImei] CHECK CONSTRAINT [CK_Imei_Estado]
GO
ALTER TABLE [dbo].[MovimientoCaja]  WITH CHECK ADD  CONSTRAINT [CK_MovCaja_Tipo] CHECK  (([Tipo]=N'Egreso' OR [Tipo]=N'Ingreso'))
GO
ALTER TABLE [dbo].[MovimientoCaja] CHECK CONSTRAINT [CK_MovCaja_Tipo]
GO
ALTER TABLE [dbo].[MovimientoInventario]  WITH CHECK ADD  CONSTRAINT [CK_MovInv_Tipo] CHECK  (([Tipo]=N'Baja' OR [Tipo]=N'Taller' OR [Tipo]=N'Ajuste' OR [Tipo]=N'Devolucion' OR [Tipo]=N'Transferencia' OR [Tipo]=N'Venta' OR [Tipo]=N'Entrada'))
GO
ALTER TABLE [dbo].[MovimientoInventario] CHECK CONSTRAINT [CK_MovInv_Tipo]
GO
ALTER TABLE [dbo].[OrdenTaller]  WITH CHECK ADD  CONSTRAINT [CK_Orden_Estado] CHECK  (([Estado]=N'Cancelado' OR [Estado]=N'Entregado' OR [Estado]=N'Reparado' OR [Estado]=N'EnReparacion' OR [Estado]=N'Recibido'))
GO
ALTER TABLE [dbo].[OrdenTaller] CHECK CONSTRAINT [CK_Orden_Estado]
GO
ALTER TABLE [dbo].[SesionCaja]  WITH CHECK ADD  CONSTRAINT [CK_Sesion_Estado] CHECK  (([Estado]=N'Cerrada' OR [Estado]=N'Abierta'))
GO
ALTER TABLE [dbo].[SesionCaja] CHECK CONSTRAINT [CK_Sesion_Estado]
GO
ALTER TABLE [dbo].[Venta]  WITH CHECK ADD  CONSTRAINT [CK_Venta_Estado] CHECK  (([Estado]=N'Devuelta' OR [Estado]=N'Anulada' OR [Estado]=N'Completada'))
GO
ALTER TABLE [dbo].[Venta] CHECK CONSTRAINT [CK_Venta_Estado]
GO
/****** Object:  StoredProcedure [dbo].[usp_Caja_Cerrar]    Script Date: 7/1/2026 11:15:18 AM ******/
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
CREATE   PROCEDURE [dbo].[usp_Caja_Cerrar]
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
/****** Object:  StoredProcedure [dbo].[usp_Credito_Crear]    Script Date: 7/1/2026 11:15:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =============================================================================
   C.2) CREDITO: crear financiamiento y generar su tabla de cuotas
        Interes simple mensual: InteresTotal = Principal * (Tasa%/100) * NumeroCuotas
        La ultima cuota absorbe el redondeo para que la suma cuadre con el total.
   ============================================================================= */
CREATE   PROCEDURE [dbo].[usp_Credito_Crear]
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
/****** Object:  StoredProcedure [dbo].[usp_Creditos_ActualizarMora]    Script Date: 7/1/2026 11:15:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [dbo].[usp_Creditos_ActualizarMora]
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
/****** Object:  StoredProcedure [dbo].[usp_Ncf_Siguiente]    Script Date: 7/1/2026 11:15:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE   PROCEDURE [dbo].[usp_Ncf_Siguiente]
    @Tipo NVARCHAR(2),
    @Ncf  NVARCHAR(19) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Ncf = NULL;

    DECLARE @out TABLE (Ncf NVARCHAR(19));

    -- Incremento atómico: solo si está activo, hay rango disponible y no venció
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
/****** Object:  StoredProcedure [dbo].[usp_PagoCredito_Registrar]    Script Date: 7/1/2026 11:15:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [dbo].[usp_PagoCredito_Registrar]
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
/****** Object:  StoredProcedure [dbo].[usp_Venta_Registrar]    Script Date: 7/1/2026 11:15:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[usp_Venta_Registrar]
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

    -- VALIDACIÓN CRÍTICA: Prevenir Stock Negativo en productos no serializados
    IF EXISTS (
        SELECT 1
        FROM @Detalles d
        JOIN dbo.ProductoVariante pv ON pv.VarianteId = d.VarianteId
        WHERE d.ImeiId IS NULL AND (pv.StockNoSerial - d.Cantidad) < 0
    )
    BEGIN
        RAISERROR(N'Stock insuficiente para uno o más accesorios/productos no serializados.', 16, 1);
        RETURN;
    END

    DECLARE @Itbis DECIMAL(9,4) = ISNULL((SELECT TOP 1 PorcentajeItbis FROM dbo.Empresa ORDER BY EmpresaId), 0);

    BEGIN TRAN;

    -- Validación: IMEIs inexistentes (mensaje claro antes del error de FK)
    IF EXISTS (
        SELECT 1 FROM @Detalles d
        WHERE d.ImeiId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM dbo.InventarioImei i WHERE i.ImeiId = d.ImeiId)
    )
    BEGIN
        ROLLBACK TRAN;
        RAISERROR(N'Uno o más IMEI no existen.', 16, 1);
        RETURN;
    END

    -- Bloqueo y validación de IMEIs concurrentes
    IF EXISTS (
        SELECT 1 FROM @Detalles d
        JOIN dbo.InventarioImei i WITH (UPDLOCK, HOLDLOCK) ON i.ImeiId = d.ImeiId
        WHERE d.ImeiId IS NOT NULL AND i.Estado <> N'Disponible'
    )
    BEGIN
        ROLLBACK TRAN;
        RAISERROR(N'Uno o más IMEI seleccionados ya no están disponibles.', 16, 1);
        RETURN;
    END

    -- Insertar Cabecera
    INSERT INTO dbo.Venta
        (NumeroFactura, SucursalId, ClienteId, UsuarioId, SesionCajaId, EsCredito, Estado, Subtotal, Descuento, Impuesto, Total)
    VALUES
        (@NumeroFactura, @SucursalId, @ClienteId, @UsuarioId, @SesionCajaId, @EsCredito, N'Completada', 0, 0, 0, 0);

    SET @VentaId = SCOPE_IDENTITY();

    -- Insertar Detalle con generación dinámica de la descripción para auditoría/facturación
    INSERT INTO dbo.VentaDetalle
        (VentaId, ImeiId, VarianteId, Descripcion, Cantidad, PrecioUnitario, Descuento, Impuesto, Total)
    SELECT
        @VentaId, d.ImeiId, d.VarianteId,
        CONCAT(p.Nombre, N' ', v.Color, N' ', v.Almacenamiento), -- Solución a la descripción NULL
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

    -- Deducción controlada de Stock No Serializado
    UPDATE v SET v.StockNoSerial = v.StockNoSerial - d.Cantidad
    FROM dbo.ProductoVariante v
    JOIN @Detalles d ON d.VarianteId = v.VarianteId
    WHERE d.ImeiId IS NULL;

    -- Recálculo de Totales
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
            RAISERROR(N'Debe indicar el método de pago para una venta de contado.', 16, 1);
            RETURN;
        END
        INSERT INTO dbo.VentaPago (VentaId, MetodoPagoId, Monto)
        SELECT @VentaId, @MetodoPagoId, Total FROM dbo.Venta WHERE VentaId = @VentaId;
    END

    COMMIT TRAN;
END;

GO
/****** Permisos minimos del usuario de aplicacion [gc_app] ******/
-- La app usa EF Core para CRUD normal y ejecuta procedimientos almacenados
-- (ventas, caja, creditos...). No recibe db_owner ni ALTER: no puede
-- modificar el esquema, reduciendo el radio de impacto ante una SQLi.
USE [GestionCelulares]
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_role_members drm
               JOIN sys.database_principals r ON r.principal_id = drm.role_principal_id
               JOIN sys.database_principals m ON m.principal_id = drm.member_principal_id
               WHERE r.name = N'db_datareader' AND m.name = N'gc_app')
    ALTER ROLE db_datareader ADD MEMBER [gc_app];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_role_members drm
               JOIN sys.database_principals r ON r.principal_id = drm.role_principal_id
               JOIN sys.database_principals m ON m.principal_id = drm.member_principal_id
               WHERE r.name = N'db_datawriter' AND m.name = N'gc_app')
    ALTER ROLE db_datawriter ADD MEMBER [gc_app];
GO
GRANT EXECUTE ON SCHEMA::dbo TO [gc_app];
GO
GRANT EXECUTE ON TYPE::dbo.VentaDetalleTipo TO [gc_app];
GO

USE [master]
GO
ALTER DATABASE [GestionCelulares] SET  READ_WRITE
GO
