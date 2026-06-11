# Contexto del proyecto — Gestión Celulares

> Documento de traspaso (handoff). Resume el estado para retomar el trabajo o pasarlo a otra sesión.
> Última actualización: 2026-06-11.

**PROYECTO:** ERP + POS + Taller + Créditos + Inventario IMEI para un comercio de venta y reparación de
celulares (República Dominicana — ITBIS 18% y, a futuro, facturación e-CF de la DGII). Despliegue para
**una sola empresa** (single-tenant) por ahora, con el modelo preparado para crecer a multi-sucursal/multi-tenant.

**STACK:** .NET 8 (ASP.NET Core Web API) + EF Core + **SQL Server**. Frontend Angular (pendiente).
Se descartó Supabase/PostgreSQL: este proyecto va con SQL Server, administrado desde SSMS.

**UBICACIÓN:** la solución vive en este repo: `C:\Users\Javier Vasquez\source\repos\Javier06-10\GestionCelulares\`
(remoto GitHub `Javier06-10/GestionCelulares`). Ojo: existe una carpeta `Desktop\GestionCelulares` vacía que no es el proyecto.

## 1) Base de datos (YA CREADA en SQL Server local, verificada)
Base `GestionCelulares` en `localhost` (autenticación Windows). 33 tablas, 5 procedimientos, 4 vistas.
Script completo exportado por SSMS: `Downloads\script.ipynb` (es T-SQL plano pese a la extensión).
Scripts originales en orden: `GestionCelulares_v1.sql` (esquema base), `v2_garantias_sp.sql` (Garantías/RMA,
tipo tabla `VentaDetalleTipo`, SPs `usp_Venta_Registrar`/`usp_Credito_Crear`/`usp_Caja_Cerrar` y vistas),
`v3_pagocredito.sql` (`usp_PagoCredito_Registrar`), `v4_mora.sql` (`usp_Creditos_ActualizarMora`, job diario).

Módulos cubiertos: Empresa/Sucursal, Seguridad (Rol, Permiso, RolPermiso, Usuario, RefreshToken), Clientes,
Proveedores (Compra, PagoProveedor), Catálogo (Marca, Categoria, Producto, ProductoVariante), Inventario IMEI
(InventarioImei, MovimientoInventario), Caja (SesionCaja, MovimientoCaja), POS/Ventas (MetodoPago, Venta,
VentaDetalle, VentaPago, Comision), Créditos (Credito, Cuota, PagoCredito), Taller (OrdenTaller,
OrdenTallerRepuesto, OrdenTallerFoto), Garantías/RMA, Auditoría (LogAuditoria).

**Datos semilla cargados (2026-06-10):** Empresa "Celulares 360" (ITBIS 18%), sucursal "Principal",
roles Admin/Vendedor, usuario `admin` (clave `Admin123*` asignada por el seeder de Development),
métodos de pago (Efectivo, Tarjeta, Transferencia) y catálogo de ejemplo (marca Samsung, categoría
Smartphones, producto Galaxy A55 con 3 variantes).

Notas: PKs `INT IDENTITY`, dinero `DECIMAL(18,2)`, tasas/ITBIS `DECIMAL(9,4)`, estados con `CHECK`, scripts
idempotentes, procedimientos críticos transaccionales. Crédito = interés simple mensual.

## 2) Backend .NET (compila 0 errores / 0 warnings; módulos probados de punta a punta)
**Clean Architecture**, 4 proyectos + `GestionCelulares.sln`:
- **GestionCelulares.Domain** — entidades POCO puras (sin EF): Core, Seguridad, Catalogo, Inventario,
  Cliente, Proveedor/Compra/PagoProveedor.
- **GestionCelulares.Application** — casos de uso + contratos. `Common/Interfaces`: `IApplicationDbContext`,
  `ITokenService`, `IPasswordHasher`. `Common/Exceptions` (una excepción por módulo → HTTP 400/404).
  Módulos: `Auth`, `Inventario`, `Clientes`, `Proveedores`, `Catalogo`. `AddApplication()`.
- **GestionCelulares.Infrastructure** — `Persistence/GestionCelularesContext` (DbContext que implementa
  `IApplicationDbContext`; mapeo Fluent). `Identity/TokenService` (JWT) + `BCryptPasswordHasher`.
  `Settings/JwtSettings`. `AddInfrastructure(config)`.
- **GestionCelulares.Api** — composition root. Controllers: Auth, Inventario, Clientes, Proveedores, Catalogo.
  `Program.cs` (JWT + Swagger + CORS `http://localhost:4200` + seeder dev).

Dependencias: `Domain ← Application ← Infrastructure`, `Api → Application + Infrastructure`.

**Implementado y funcionando (Fase 1 completa):**
- **Auth (Épica 1):** `POST /api/auth/login`, `POST /api/auth/refresh` (JWT + refresh con rotación, BCrypt).
- **Inventario IMEI (Épica 6, parcial):** disponibles por sucursal, consulta por IMEI/Id, registro con
  movimiento de entrada, transferencia entre sucursales.
- **Clientes (Épica 3):** búsqueda por nombre/cédula/teléfono, filtros morosos/bloqueados, registro con
  cédula única, edición, bloqueo/desbloqueo.
- **Proveedores (Épica 4):** CRUD con búsqueda, historial y registro de compras (suma balance) y pagos
  (resta balance; rechaza pagos que excedan lo adeudado).
- **Catálogo (Épica 5):** marcas y categorías con nombre único, productos con variantes
  (color/almacenamiento/condición), gestión de precios y stock no serializado.

**Convención importante:** las proyecciones a DTO en los servicios deben ser `Expression<Func<Entidad,Dto>>`
estáticas (no métodos) para que EF Core las traduzca a SQL y cargue las navegaciones. Un método estático en el
`Select` se evalúa en cliente y deja navegaciones en null (bug ya corregido en `CatalogoService`).

**Antes de ejecutar:** ajustar en `GestionCelulares.Api/appsettings.json`: (1) `ConnectionStrings:Default`,
(2) `Jwt:Key` (32+ chars; sigue el placeholder). Luego `dotnet run --project GestionCelulares.Api --launch-profile http`
(puerto 5289) y probar en Swagger.

## 3) Documento de planificación (Word)
`Downloads\planificacion_proyecto.docx` — 19 épicas / 340+ historias en 4 fases. Fase 1 (auth, clientes,
proveedores, catálogo, inventario IMEI) **completada en la API**. Fase 2: POS, caja, créditos, taller,
dashboard, reportes, garantías/RMA.

**Hecho además (2026-06-11) — FASE 2 COMPLETA:** Caja (usp_Caja_Cerrar con OUTPUT), Ventas/POS
(usp_Venta_Registrar con TVP VentaDetalleTipo como DataTable Structured), Créditos (crear/abonos/mora +
vista vw_CuotasVencidas), Gestión de usuarios (CRUD Admin + cambio de clave propio con revocación de
refresh tokens), Taller (flujo Recibido→EnReparacion→Reparado→Entregado/Cancelado, repuestos que
descuentan StockNoSerial, fotos, Kanban), Dashboard (GET /api/dashboard con todos los indicadores),
Reportes (ventas/inventario/morosidad/caja/taller/cobros, solo Admin) y Garantías/RMA (garantía por
venta/IMEI con vigencia, casos con flujo Abierto→EnProceso→Resuelto/Rechazado→Cerrado, reemplazo que
mueve inventario Devuelto/Vendido con kardex, vistas vw_GarantiaVigentePorImei y vw_IndiceFallasPorModelo).
Patrón: puertos ICajaProcedures/IVentaProcedures/ICreditoProcedures en Application, ADO.NET en Infrastructure;
los RAISERROR clase 16 se convierten en excepciones de módulo → HTTP 400.
Nota POS: al cobrar cuotas en efectivo, el frontend debe enviar sesionCajaId en el abono o el arqueo no lo cuenta.

**Decisiones transversales aplicadas:**
- **Fechas en hora local del servidor** (RD, UTC-4 sin DST), igual que la BD (SYSDATETIME). La API usa
  `DateTime.Now`; solo el JWT usa UTC internamente (estándar del token).
- **Autorización por roles**: `[Authorize(Roles = Roles.Admin)]` en Proveedores (todo), escritura de
  Catálogo, registro/transferencia de Inventario, bloquear/desbloquear clientes y actualizar-mora.
  Vendedor opera POS, caja, clientes y consultas. Usuarios: `admin`/Admin123* (Admin) y `vendedor`/Admin123* (Vendedor).
- **Jwt:Key real** en `appsettings.Development.json`; `Program.cs` se niega a arrancar con clave corta o placeholder.

## 4) Pendiente / próximos pasos (en orden acordado)
1. Frontend **Angular** (POS, panel admin, dashboard, Kanban del taller) — CORS ya apunta a localhost:4200.
2. Proyecto de **tests** (lógica financiera primero: cuotas, intereses, mora, arqueo).
3. Programar `usp_Creditos_ActualizarMora` como job diario (SQL Agent o BackgroundService).
4. Para despliegue: cadena de conexión y Jwt:Key por user-secrets/variables de entorno.
5. Fase 3 del plan: multi-sucursal completo, facturación electrónica e-CF (DGII), auditoría, notificaciones.

**Idioma:** español. **SO:** Windows 11, PowerShell. El usuario trabaja con SSMS y Visual Studio
(cerrar VS antes de mover carpetas, porque bloquea `.vs`).
