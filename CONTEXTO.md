# Contexto del proyecto — Gestión Celulares

> Documento de traspaso (handoff). Resume el estado para retomar el trabajo o pasarlo a otra sesión.

**PROYECTO:** ERP + POS + Taller + Créditos + Inventario IMEI para un comercio de venta y reparación de
celulares (República Dominicana — ITBIS 18% y, a futuro, facturación e-CF de la DGII). Despliegue para
**una sola empresa** (single-tenant) por ahora, con el modelo preparado para crecer a multi-sucursal/multi-tenant.

**STACK:** .NET 8 (ASP.NET Core Web API) + EF Core + **SQL Server**. Frontend Angular (pendiente).
Se descartó Supabase/PostgreSQL: este proyecto va con SQL Server, administrado desde SSMS.

## 1) Base de datos (YA CREADA en SQL Server)
Base `GestionCelulares`. Scripts en `Desktop\` (ejecutar en orden en SSMS):
- `GestionCelulares_v1.sql` — esquema base (≈29 tablas) + datos semilla. Single-tenant (sin tabla Tenant;
  hay `Empresa` de 1 fila y `Sucursal` "Principal"). IMEI como instancia única con kardex.
- `GestionCelulares_v2_garantias_sp.sql` — tablas Garantías/RMA (`Garantia`, `CasoGarantia`), tipo tabla
  `VentaDetalleTipo`, procedimientos `usp_Venta_Registrar`, `usp_Credito_Crear`, `usp_Caja_Cerrar`, y vistas
  `vw_CuotasVencidas`, `vw_GarantiaVigentePorImei`, `vw_InventarioDisponible`, `vw_IndiceFallasPorModelo`.
- `GestionCelulares_v3_pagocredito.sql` — `usp_PagoCredito_Registrar` (abonos: distribuye cuota más antigua
  primero, actualiza estados de cuota/crédito).
- `GestionCelulares_v4_mora.sql` — `usp_Creditos_ActualizarMora` (marca cuotas vencidas, créditos/clientes en
  mora; pensado para job diario) + bloque comentado de SQL Server Agent.

Módulos cubiertos: Empresa/Sucursal, Seguridad (Rol, Permiso, RolPermiso, Usuario, RefreshToken), Clientes,
Proveedores (Compra, PagoProveedor), Catálogo (Marca, Categoria, Producto, ProductoVariante), Inventario IMEI
(InventarioImei, MovimientoInventario), Caja (SesionCaja, MovimientoCaja), POS/Ventas (MetodoPago, Venta,
VentaDetalle, VentaPago, Comision), Créditos (Credito, Cuota, PagoCredito), Taller (OrdenTaller,
OrdenTallerRepuesto, OrdenTallerFoto), Garantías/RMA, Auditoría (LogAuditoria).

Notas: PKs `INT IDENTITY`, dinero `DECIMAL(18,2)`, tasas/ITBIS `DECIMAL(9,4)`, estados con `CHECK`, scripts
idempotentes, procedimientos críticos transaccionales. Crédito = interés simple mensual. Usuario `admin`
semilla con `HashContrasena='CAMBIAR_HASH'` (placeholder).

## 2) Backend .NET (YA CREADO, compila 0 errores / 0 warnings)
Ubicación: `Desktop\GestionCelulares\` (todo bajo una sola carpeta). **Clean Architecture**, 4 proyectos +
`GestionCelulares.sln` + `README.md`:
- **GestionCelulares.Domain** — entidades POCO puras (sin EF): Core, Seguridad, Catalogo, Inventario.
- **GestionCelulares.Application** — casos de uso + contratos. `Common/Interfaces`: `IApplicationDbContext`,
  `ITokenService`, `IPasswordHasher`. `Common/Exceptions`. `Auth` (DTOs + AuthService). `Inventario`
  (DTOs, read model `InventarioDisponibleDto`, InventarioService). `AddApplication()`.
- **GestionCelulares.Infrastructure** — `Persistence/GestionCelularesContext` (DbContext que implementa
  `IApplicationDbContext`; mapeo Fluent). `Identity/TokenService` (JWT) + `BCryptPasswordHasher`.
  `Settings/JwtSettings`. `AddInfrastructure(config)`.
- **GestionCelulares.Api** — composition root. `Controllers/AuthController`, `Controllers/InventarioController`.
  `Program.cs` (AddApplication + AddInfrastructure + JWT + Swagger + CORS `http://localhost:4200` + seeder dev).

Dependencias: `Domain ← Application ← Infrastructure`, `Api → Application + Infrastructure`.

**Implementado y funcionando:**
- **Auth (Épica 1):** `POST /api/auth/login`, `POST /api/auth/refresh` (JWT + refresh con rotación, BCrypt vía
  `IPasswordHasher`). En Development un seeder pone la clave de `admin` en `Admin123*` si está el placeholder.
- **Inventario IMEI:** `GET /api/inventario/disponibles?sucursalId=`, `GET /api/inventario/imei/{imei}`,
  `GET /api/inventario/{id}`, `POST /api/inventario` (registra IMEI + movimiento de entrada),
  `POST /api/inventario/{id}/transferir`.

**Antes de ejecutar:** ajustar en `GestionCelulares.Api/appsettings.json`: (1) `ConnectionStrings:Default`,
(2) `Jwt:Key` (32+ chars). Luego `dotnet run --project GestionCelulares.Api` y probar en Swagger.
Abrir en VS desde `Desktop\GestionCelulares\GestionCelulares.sln`.

**Importante:** las entidades/DbContext se escribieron a mano reflejando la BD (no se pudo correr
`dotnet ef scaffold` por no tener acceso al SQL Server del usuario). El scaffold real es opcional (ver README).

## 3) Documento de planificación (Word)
`Downloads\planificacion_proyecto.docx` — actualizado: secciones nuevas (presupuesto económico, migración,
modo offline POS, estrategia de pruebas, Garantías) + sección 17 de implementación de BD; "Próximos pasos"
es la sección 18 con lo ya hecho tachado. Periféricos del POS quedaron fuera a pedido del usuario.

## 4) Pendiente / próximos pasos
- **Módulo Ventas/POS**: invocar el SP `usp_Venta_Registrar` desde Infrastructure (patrón para llamar
  procedimientos con EF Core, incluyendo el TVP `VentaDetalleTipo`).
- Módulos **Clientes** y **Catálogo** (CRUD, replicando la plantilla de Inventario).
- **Autorización por roles/permisos** (`[Authorize(Roles=...)]` o políticas con `RolPermiso`).
- Integración con frontend **Angular**.
- (Opcional) `usp_Credito_Reestructurar`; cerrar presupuesto económico (TCO).

**Idioma:** español. **SO:** Windows 11, PowerShell. El usuario trabaja con SSMS y Visual Studio
(cerrar VS antes de mover carpetas, porque bloquea `.vs`).
