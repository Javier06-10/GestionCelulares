# GestiónCelulares — Resumen de Pantallas y Tecnologías

Sistema ERP/POS/Taller/Crédito/Fiscal para una tienda de venta y reparación de
celulares en República Dominicana.

---

## 🧱 Stack tecnológico

### Frontend (`gestioncelulares-web/`)
| Área | Tecnología |
|---|---|
| Framework | **Angular 20.3** — componentes **standalone** (sin NgModules) |
| Estado reactivo | **Angular Signals** (`signal` / `computed` / `effect`) |
| Formularios | **Reactive Forms** (`FormGroup`, `FormArray`) |
| Ruteo | **Angular Router** + `authGuard` + interceptor **JWT** |
| HTTP / async | **RxJS 7.8** (`HttpClient`) |
| Estilos | **Tailwind CSS 3.4** (paleta propia *"Stealth Tech"*), PostCSS, Autoprefixer |
| Íconos | **lucide-angular** |
| Animación | **@angular/animations** (transición de ruta) · **Web Animations API** (volar al carrito, count-up) · **keyframes CSS globales** (cascada, sheen, hover-lift, pop de modales) |
| Lenguaje | **TypeScript 5.9** |
| Build | **@angular/build** (esbuild) |
| Tipos del API | **OpenAPI Generator** (dev) |
| Tests | **Karma + Jasmine** |
| Otros | `localStorage` (token y preferencias), Web Audio (`SoundService`), `window.print()`, descarga de archivos vía `Blob`, *deep link* a WhatsApp, `<canvas>` (confeti) |

### Backend (`GestionCelulares.*`)
| Área | Tecnología |
|---|---|
| Plataforma | **.NET 8 / ASP.NET Core Web API** |
| Arquitectura | **Clean Architecture** — `Domain` / `Application` / `Infrastructure` / `Api` |
| ORM | **Entity Framework Core 8** (provider **SQL Server**) + **stored procedures T-SQL** para flujos transaccionales (venta, caja, NCF) |
| Base de datos | **SQL Server** (localhost) · scripts versionados `GestionCelulares_vN_*.sql` aplicados con `sqlcmd` |
| Seguridad | **JWT Bearer** + roles (`[Authorize(Roles=...)]`) · **BCrypt.Net** (hash de contraseñas) |
| Docs API | **Swagger / Swashbuckle (OpenAPI)** |
| Tests | **xUnit** |

---

## 🖥️ Pantallas

> Todas comparten la base: Angular standalone + Signals + Reactive Forms +
> Tailwind "Stealth Tech" + Lucide + las animaciones globales. La columna
> **"Tecnología destacada"** resalta lo particular de cada una.

### Acceso
| Pantalla | Ruta | Propósito | Tecnología destacada |
|---|---|---|---|
| **Login** | `/login` | Autenticación | JWT + interceptor; guarda `accessToken`/`refreshToken` |

### Operación / Ventas
| Pantalla | Ruta | Propósito | Tecnología destacada |
|---|---|---|---|
| **Dashboard** | `/` | KPIs del negocio en tiempo real | **Gráfica SVG** (área + tooltip interactivo), **sparklines**, **count-up**, **auto-refresh 30s**, barras de ranking |
| **POS / Punto de venta** | `/pos` | Vender (IMEI/accesorios) | Carrito con Signals, **atajos de teclado** (F2/F8/F9/F10), **factura imprimible** (`@media print`), **WhatsApp**, **confeti** (`<canvas>`), **"volar al carrito"** (Web Animations API) |
| **Historial de ventas** | `/ventas` | Consulta/anulación de ventas | Filtros por fecha/sucursal/cliente |
| **Caja** | `/caja` | Apertura, movimientos y arqueo del turno | Sesión atada al empleado; resumen de turno; cierre con cuadre |
| **Apartados** | `/apartados` | Layaway de equipos con IMEI | Abonos, método "Apartado", reglas de cancelación |

### Inventario
| Pantalla | Ruta | Propósito | Tecnología destacada |
|---|---|---|---|
| **Inventario** | `/inventario` | **Unifica** Existencias + Agotados + Faltantes en pestañas | KPIs glassmorphism clicables, búsqueda por IMEI, ajuste de stock |
| **Catálogo** | `/catalogo` | Productos, variantes, marcas y categorías | Filtro por tipo, **margen %** por variante, *color swatch* |

### Clientes y crédito
| Pantalla | Ruta | Propósito | Tecnología destacada |
|---|---|---|---|
| **Clientes** | `/clientes` | Cartera de clientes | **Filtrado 100% en cliente** (Signals/computed), KPIs como filtro |
| **Créditos** | `/creditos` | Financiamientos y cobranza | **Barra de progreso** de pago, **cuota estimada en vivo**, mora |
| **Garantías / RMA** | `/garantias` | Cobertura y casos de reclamación | Consulta por IMEI, **días de cobertura** con color, índice de fallas |

### Taller
| Pantalla | Ruta | Propósito | Tecnología destacada |
|---|---|---|---|
| **Taller** | `/taller` | Órdenes de reparación | **Tablero Kanban con HTML5 Drag & Drop**, comisiones por técnico |

### Administración
| Pantalla | Ruta | Propósito | Tecnología destacada |
|---|---|---|---|
| **Proveedores** | `/proveedores` | Suplidores, compras y CxP | Detalle con compras/pagos; registro de compras (tipo DGII) |
| **Nómina** | `/nomina` | Pagos a empleados | Tipos (salario/adelanto/bono/comisión) con color |
| **Usuarios** | `/usuarios` | Gestión de usuarios y roles | Reset de contraseña; roles (Admin/Técnico/Vendedor) |

### Contabilidad
| Pantalla | Ruta | Propósito | Tecnología destacada |
|---|---|---|---|
| **Catálogo de cuentas** | `/cuentas` | Plan contable (6 familias) | Árbol colapsable, sistema de **color por familia**, buscador |
| **Libro Diario** | `/asientos` | Asientos de **partida doble** | Validación débito=crédito, **contabilización automática** (idempotente por `Origen`+`ReferenciaId`), balance de comprobación |
| **Estados financieros** | `/estados` | Estado de Resultados y Balance General | **Cierre del ejercicio**, **export CSV** e **imprimir/PDF** (`window.print()`) |

### Fiscal (DGII)
| Pantalla | Ruta | Propósito | Tecnología destacada |
|---|---|---|---|
| **NCF** | `/ncf` | Rangos de comprobantes fiscales | KPI "por agotarse" con alerta |
| **Reportes** | `/reportes` | Reportes operativos y fiscales | **DGII 606/607/608** (descarga TXT), **export CSV** (`Blob`) por reporte |

---

## ✨ Sistema de animaciones (global)
- **Cascada de entrada** de tarjetas KPI y **armado escalonado de filas** de tabla.
- **Hover-lift + glow** en tarjetas KPI; **sheen** (destello) en botones primarios.
- **Pop** de modales (backdrop fade + panel con rebote).
- **Count-up** en los KPIs de todas las pantallas.
- **"Volar al carrito"** en el POS (producto fantasma en arco + rebote del carrito).
- Todo respeta **`prefers-reduced-motion`** (accesibilidad).

---

## 🔑 Conceptos transversales
- **Multi-rol**: Admin / Técnico / Vendedor (autorización por endpoint).
- **Fiscalidad RD**: NCF automático según cliente (Crédito Fiscal 01 / Consumo 02), ITBIS 18%, formatos 606/607/608.
- **Contabilidad de partida doble** con contabilización automática idempotente y estados financieros.
- **Caja por turno** atada al empleado, con arqueo y cuadre.
