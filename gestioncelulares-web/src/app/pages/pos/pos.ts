import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { CajaService } from '../../core/caja.service';
import { CatalogoService, Producto } from '../../core/catalogo.service';
import { Cliente } from '../../core/cliente.service';
import { FacturaConfig, FacturaConfigService } from '../../core/factura-config.service';
import { InventarioService, StockDisponible } from '../../core/inventario.service';
import { MetodoPago, SecuenciaFactura, VentaService } from '../../core/venta.service';
import { ClienteSelector } from '../../shared/cliente-selector/cliente-selector';

interface FacturaData {
  factura: string;
  fecha: Date;
  cliente: string | null;
  vendedor: string;
  esCredito: boolean;
  metodoPago: string | null;
  lineas: { descripcion: string; imei?: string; cantidad: number; precioUnitario: number; total: number }[];
  subtotal: number;
  descuento: number;
  impuesto: number;
  total: number;
}

interface LineaCarrito {
  key: string;
  imeiId: number | null;
  varianteId: number;
  descripcion: string;
  imei?: string;
  cantidad: number;
  precioUnitario: number;
  descuento: number;
  serializado: boolean;
}

const ITBIS = 0.18; // 18% — coincide con Empresa.PorcentajeItbis; el total final lo recalcula el API

@Component({
  selector: 'app-pos',
  imports: [FormsModule, RouterLink, LucideAngularModule, CurrencyPipe, DatePipe, ClienteSelector],
  templateUrl: './pos.html'
})
export class Pos {
  private inventario = inject(InventarioService);
  private catalogo = inject(CatalogoService);
  private caja = inject(CajaService);
  private ventas = inject(VentaService);
  auth = inject(AuthService);

  // Estado de caja
  cajaAbierta = signal<boolean | null>(null); // null = cargando

  // Dispositivos disponibles (stock por IMEI)
  dispositivos = signal<StockDisponible[]>([]);

  // Catálogo / accesorios
  productos = signal<Producto[]>([]);
  accesorios = computed(() =>
    this.productos()
      .filter(p => !p.serializado && p.activo)
      .flatMap(p => p.variantes.filter(v => v.activo).map(v => ({
        varianteId: v.varianteId,
        nombre: p.nombre,
        detalle: [v.color, v.almacenamiento].filter(Boolean).join(' · '),
        precioVenta: v.precioVenta,
        stock: v.stockNoSerial
      })))
  );

  // Búsqueda IMEI
  imeiInput = signal('');
  buscandoImei = signal(false);
  errorImei = signal<string | null>(null);

  // Carrito
  carrito = signal<LineaCarrito[]>([]);
  subtotal = computed(() => this.carrito().reduce((a, l) => a + l.precioUnitario * l.cantidad - l.descuento, 0));
  impuesto = computed(() => Math.round(this.subtotal() * ITBIS * 100) / 100);
  total = computed(() => this.subtotal() + this.impuesto());

  // Cobro
  esCredito = signal(false);
  metodos = signal<MetodoPago[]>([]);
  metodoPagoId = signal<number | null>(null);

  // Cliente
  clienteSel = signal<Cliente | null>(null);

  // Resultado / Factura
  procesando = signal(false);
  errorVenta = signal<string | null>(null);
  factura = signal<FacturaData | null>(null);

  // Configuración de factura
  facturaCfg = inject(FacturaConfigService);
  modalConfig = signal(false);
  borradorCfg = signal<FacturaConfig>(this.facturaCfg.config());
  // Numeración (secuencia en el servidor)
  secuencia = signal<SecuenciaFactura | null>(null);

  private get sucursalId(): number | null {
    return this.auth.usuario()?.sucursalId ?? null;
  }

  constructor() {
    this.verificarCaja();
    this.cargarDispositivos();
    this.catalogo.productos().subscribe(p => this.productos.set(p));
    this.ventas.metodosPago().subscribe(m => {
      this.metodos.set(m);
      const efectivo = m.find(x => x.nombre.toLowerCase() === 'efectivo') ?? m[0];
      if (efectivo) this.metodoPagoId.set(efectivo.metodoPagoId);
    });
  }

  private verificarCaja(): void {
    const suc = this.sucursalId;
    if (!suc) { this.cajaAbierta.set(false); return; }
    this.caja.actual(suc).subscribe({
      next: () => this.cajaAbierta.set(true),
      error: () => this.cajaAbierta.set(false)
    });
  }

  private cargarDispositivos(): void {
    this.inventario.disponibles(this.sucursalId ?? undefined)
      .subscribe(d => this.dispositivos.set(d));
  }

  errorDispositivo = signal<string | null>(null);

  // Agregar un dispositivo eligiendo automáticamente un IMEI disponible de la variante
  agregarDispositivo(d: StockDisponible): void {
    this.errorDispositivo.set(null);
    this.inventario.imeisDisponibles(d.varianteId, this.sucursalId ?? undefined).subscribe({
      next: imeis => {
        const libre = imeis.find(i => !this.carrito().some(l => l.imeiId === i.imeiId));
        if (!libre) {
          this.errorDispositivo.set('No quedan equipos disponibles de ese modelo.');
          return;
        }
        this.carrito.update(c => [...c, {
          key: 'imei-' + libre.imeiId,
          imeiId: libre.imeiId,
          varianteId: libre.varianteId,
          descripcion: `${libre.producto}${libre.color ? ' · ' + libre.color : ''}${libre.almacenamiento ? ' · ' + libre.almacenamiento : ''}`,
          imei: libre.imei,
          cantidad: 1,
          precioUnitario: d.precioVenta,
          descuento: 0,
          serializado: true
        }]);
      },
      error: () => this.errorDispositivo.set('No se pudieron cargar los equipos disponibles.')
    });
  }

  // ----- Agregar por IMEI -----
  agregarPorImei(): void {
    const imei = this.imeiInput().trim();
    if (imei.length < 4) return;
    this.errorImei.set(null);
    this.buscandoImei.set(true);

    this.inventario.porImei(imei).subscribe({
      next: e => {
        this.buscandoImei.set(false);
        if (e.estado !== 'Disponible') {
          this.errorImei.set(`El equipo está '${e.estado}', no se puede vender.`);
          return;
        }
        if (this.carrito().some(l => l.imeiId === e.imeiId)) {
          this.errorImei.set('Ese equipo ya está en el carrito.');
          return;
        }
        // Precio de venta desde el catálogo (la consulta por IMEI no lo trae)
        const precio = this.precioDeVariante(e.varianteId);
        this.carrito.update(c => [...c, {
          key: 'imei-' + e.imeiId,
          imeiId: e.imeiId,
          varianteId: e.varianteId,
          descripcion: `${e.producto}${e.color ? ' · ' + e.color : ''}${e.almacenamiento ? ' · ' + e.almacenamiento : ''}`,
          imei: e.imei,
          cantidad: 1,
          precioUnitario: precio,
          descuento: 0,
          serializado: true
        }]);
        this.imeiInput.set('');
      },
      error: () => {
        this.buscandoImei.set(false);
        this.errorImei.set('No se encontró ningún equipo con ese IMEI.');
      }
    });
  }

  private precioDeVariante(varianteId: number): number {
    for (const p of this.productos()) {
      const v = p.variantes.find(x => x.varianteId === varianteId);
      if (v) return v.precioVenta;
    }
    return 0;
  }

  // ----- Agregar accesorio -----
  agregarAccesorio(a: { varianteId: number; nombre: string; detalle: string; precioVenta: number }): void {
    const existente = this.carrito().find(l => l.varianteId === a.varianteId && l.imeiId === null);
    if (existente) {
      this.carrito.update(c => c.map(l => l.key === existente.key ? { ...l, cantidad: l.cantidad + 1 } : l));
    } else {
      this.carrito.update(c => [...c, {
        key: 'var-' + a.varianteId,
        imeiId: null,
        varianteId: a.varianteId,
        descripcion: a.detalle ? `${a.nombre} · ${a.detalle}` : a.nombre,
        cantidad: 1,
        precioUnitario: a.precioVenta,
        descuento: 0,
        serializado: false
      }]);
    }
  }

  cambiarCantidad(key: string, delta: number): void {
    this.carrito.update(c => c.map(l => {
      if (l.key !== key || l.serializado) return l;
      return { ...l, cantidad: Math.max(1, l.cantidad + delta) };
    }));
  }

  fijarDescuento(key: string, valor: number): void {
    this.carrito.update(c => c.map(l => l.key === key ? { ...l, descuento: Math.max(0, valor || 0) } : l));
  }

  fijarPrecio(key: string, valor: number): void {
    this.carrito.update(c => c.map(l => l.key === key ? { ...l, precioUnitario: Math.max(0, valor || 0) } : l));
  }

  quitar(key: string): void {
    this.carrito.update(c => c.filter(l => l.key !== key));
  }

  vaciar(): void {
    this.carrito.set([]);
    this.errorVenta.set(null);
  }

  alternarCredito(valor: boolean): void {
    this.esCredito.set(valor);
  }

  // ----- Cobro -----
  puedeCobrar = computed(() => {
    if (this.carrito().length === 0) return false;
    if (this.esCredito() && !this.clienteSel()) return false;
    if (!this.esCredito() && !this.metodoPagoId()) return false;
    return true;
  });

  cobrar(): void {
    const suc = this.sucursalId;
    if (!suc || !this.puedeCobrar()) return;

    this.procesando.set(true);
    this.errorVenta.set(null);

    this.ventas.registrar({
      sucursalId: suc,
      clienteId: this.clienteSel()?.clienteId ?? null,
      esCredito: this.esCredito(),
      metodoPagoId: this.esCredito() ? null : this.metodoPagoId(),
      detalles: this.carrito().map(l => ({
        imeiId: l.imeiId,
        varianteId: l.varianteId,
        cantidad: l.cantidad,
        precioUnitario: l.precioUnitario,
        descuento: l.descuento
      }))
    }).subscribe({
      next: v => {
        this.procesando.set(false);
        // Armar la factura con el detalle del carrito antes de vaciarlo
        const metodo = this.metodos().find(m => m.metodoPagoId === this.metodoPagoId());
        this.factura.set({
          factura: v.numeroFactura ?? ('Venta #' + v.ventaId),
          fecha: new Date(),
          cliente: this.clienteSel()?.nombre ?? null,
          vendedor: this.auth.usuario()?.nombreCompleto ?? '',
          esCredito: this.esCredito(),
          metodoPago: this.esCredito() ? null : (metodo?.nombre ?? null),
          lineas: this.carrito().map(l => ({
            descripcion: l.descripcion,
            imei: l.imei,
            cantidad: l.cantidad,
            precioUnitario: l.precioUnitario,
            total: l.precioUnitario * l.cantidad - l.descuento
          })),
          subtotal: this.subtotal(),
          descuento: this.carrito().reduce((a, l) => a + l.descuento, 0),
          impuesto: this.impuesto(),
          total: v.total
        });
        this.carrito.set([]);
        this.clienteSel.set(null);
        this.esCredito.set(false);
      },
      error: err => {
        this.procesando.set(false);
        this.errorVenta.set(err.error?.error ?? 'No se pudo registrar la venta.');
      }
    });
  }

  imprimirFactura(): void {
    window.print();
  }

  nuevaVenta(): void {
    this.factura.set(null);
    this.cargarDispositivos();
    this.catalogo.productos().subscribe(p => this.productos.set(p)); // refrescar stock/disponibles
  }

  // ----- Configuración de factura -----
  abrirConfig(): void {
    this.borradorCfg.set({ ...this.facturaCfg.config() });
    this.secuencia.set(null);
    this.ventas.obtenerSecuencia().subscribe({ next: s => this.secuencia.set(s) });
    this.modalConfig.set(true);
  }

  setCfg<K extends keyof FacturaConfig>(campo: K, valor: FacturaConfig[K]): void {
    this.borradorCfg.update(c => ({ ...c, [campo]: valor }));
  }

  setSec<K extends keyof SecuenciaFactura>(campo: K, valor: SecuenciaFactura[K]): void {
    this.secuencia.update(s => s ? { ...s, [campo]: valor } : s);
  }

  guardarConfig(): void {
    this.facturaCfg.guardar(this.borradorCfg());
    const s = this.secuencia();
    if (s) {
      this.ventas.guardarSecuencia({ prefijo: s.prefijo, proximo: Number(s.proximo), longitud: Number(s.longitud) })
        .subscribe({ next: () => this.modalConfig.set(false), error: () => this.modalConfig.set(false) });
    } else {
      this.modalConfig.set(false);
    }
  }
}
