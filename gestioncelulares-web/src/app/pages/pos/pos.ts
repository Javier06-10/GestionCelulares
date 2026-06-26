import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal, HostListener, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { SoundService } from '../../core/sound.service';
import { FlyToCartService } from '../../core/fly-to-cart.service';
import { CajaService } from '../../core/caja.service';
import { CatalogoService, Producto } from '../../core/catalogo.service';
import { Cliente } from '../../core/cliente.service';
import { FacturaConfig, FacturaConfigService } from '../../core/factura-config.service';
import { InventarioService, StockDisponible } from '../../core/inventario.service';
import { UiService } from '../../core/ui.service';
import { MetodoPago, SecuenciaFactura, VentaService } from '../../core/venta.service';
import { ClienteSelector } from '../../shared/cliente-selector/cliente-selector';

interface FacturaData {
  factura: string;
  ncf: string | null;
  fecha: Date;
  cliente: string | null;
  clienteDoc: string | null;   // RNC/cédula — obligatorio en Crédito Fiscal (01)
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
export class Pos implements OnDestroy {
  private inventario = inject(InventarioService);
  private catalogo = inject(CatalogoService);
  private caja = inject(CajaService);
  private ventas = inject(VentaService);
  private sound = inject(SoundService);
  private fly = inject(FlyToCartService);
  ui = inject(UiService);
  auth = inject(AuthService);

  /** Al salir del POS se abandona el modo kiosko para no dejar la app sin menú. */
  ngOnDestroy(): void {
    if (this.ui.kiosko()) this.ui.salir();
  }

  /** Dispara la animación "volar al carrito" desde el botón clicado. */
  volar(ev: Event): void {
    this.fly.fly(ev.currentTarget as Element);
  }

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

  // Filtro rápido de productos (dispositivos + accesorios)
  filtroProd = signal('');
  dispositivosFiltrados = computed(() => {
    const q = this.filtroProd().toLowerCase().trim();
    const list = this.dispositivos();
    if (!q) return list;
    return list.filter(d => `${d.producto} ${d.marca ?? ''} ${d.color ?? ''} ${d.almacenamiento ?? ''}`.toLowerCase().includes(q));
  });
  accesoriosFiltrados = computed(() => {
    const q = this.filtroProd().toLowerCase().trim();
    const list = this.accesorios();
    if (!q) return list;
    return list.filter(a => `${a.nombre} ${a.detalle}`.toLowerCase().includes(q));
  });

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

  // Tipo de comprobante fiscal: '02' Consumo (default) | '01' Crédito Fiscal
  tipoComprobante = signal<'02' | '01'>('02');
  // ¿El cliente seleccionado tiene RNC/cédula? (requisito para Crédito Fiscal)
  clienteTieneRnc = computed(() => {
    const c = this.clienteSel();
    return !!c?.cedula && c.cedula.replace(/\D/g, '').length > 0;
  });

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
          this.sound.playError();
          return;
        }
        this.sound.playScan();
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
      error: () => {
        this.errorDispositivo.set('No se pudieron cargar los equipos disponibles.');
        this.sound.playError();
      }
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
          this.sound.playError();
          return;
        }
        if (this.carrito().some(l => l.imeiId === e.imeiId)) {
          this.errorImei.set('Ese equipo ya está en el carrito.');
          this.sound.playError();
          return;
        }
        this.sound.playScan();
        // Precio de venta desde el catálogo (la consulta por IMEI no lo trae)
        const precio = this.precioDeVariante(e.varianteId);
        this.carrito.update(c => [...c, {
          key: 'imei-' + e.imeiId,
          imeiId: e.imeiId,
          varianteId: e.varianteId,
          descripcion: `${e.producto}${e.color ? ' · ' + e.color : ''}${e.almacenamiento ? ' · ' + e.almacenamiento : ''}`,
          imei: e.imei,
          shadowClass: 'imei-' + e.imeiId,
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
        this.sound.playError();
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
    this.sound.playScan();
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
    // Crédito Fiscal exige cliente con RNC/cédula
    if (this.tipoComprobante() === '01' && !this.clienteTieneRnc()) return false;
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
      tipoComprobante: this.tipoComprobante(),
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
        this.sound.playSuccess();
        this.lanzarConfeti();
        // Armar la factura con el detalle del carrito antes de vaciarlo
        const metodo = this.metodos().find(m => m.metodoPagoId === this.metodoPagoId());
        this.factura.set({
          factura: v.numeroFactura ?? ('Venta #' + v.ventaId),
          ncf: v.ncf ?? null,
          fecha: new Date(),
          cliente: this.clienteSel()?.nombre ?? null,
          clienteDoc: this.clienteSel()?.cedula ?? null,
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
        this.tipoComprobante.set('02');
      },
      error: err => {
        this.procesando.set(false);
        this.sound.playError();
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

  @HostListener('window:keydown', ['$event'])
  manejarAtajos(event: KeyboardEvent): void {
    if (this.modalConfig()) return;

    if (event.key === 'F2') {
      event.preventDefault();
      const inputEl = document.querySelector('input[name="imei"]') as HTMLInputElement;
      if (inputEl) {
        inputEl.focus();
        inputEl.select();
      }
    } else if (event.key === 'F8') {
      event.preventDefault();
      this.alternarCredito(!this.esCredito());
    } else if (event.key === 'F9') {
      event.preventDefault();
      const clienteInput = document.querySelector('input[placeholder*="cliente"]') as HTMLInputElement;
      if (clienteInput) {
        clienteInput.focus();
        clienteInput.select();
      }
    } else if (event.key === 'F10' || (event.ctrlKey && event.key === 'Enter')) {
      if (this.puedeCobrar() && !this.procesando()) {
        event.preventDefault();
        this.cobrar();
      }
    } else if (event.key === 'Escape') {
      if (this.factura()) {
        this.nuevaVenta();
      } else if (this.modalConfig()) {
        this.modalConfig.set(false);
      }
    }
  }

  lanzarConfeti(): void {
    try {
      const canvas = document.createElement('canvas');
      canvas.style.position = 'fixed';
      canvas.style.top = '0';
      canvas.style.left = '0';
      canvas.style.width = '100vw';
      canvas.style.height = '100vh';
      canvas.style.pointerEvents = 'none';
      canvas.style.zIndex = '9999';
      document.body.appendChild(canvas);

      const ctx = canvas.getContext('2d');
      if (!ctx) return;

      let width = (canvas.width = window.innerWidth);
      let height = (canvas.height = window.innerHeight);

      const handleResize = () => {
        width = canvas.width = window.innerWidth;
        height = canvas.height = window.innerHeight;
      };
      window.addEventListener('resize', handleResize);

      const particles: any[] = [];
      const colors = ['#00E676', '#6C5CE7', '#00c853', '#80ee9f', '#a29bfe', '#ffffff'];

      for (let i = 0; i < 100; i++) {
        particles.push({
          x: Math.random() * width,
          y: Math.random() * height - height,
          r: Math.random() * 5 + 3,
          d: Math.random() * height,
          color: colors[Math.floor(Math.random() * colors.length)],
          tilt: Math.random() * 10 - 5,
          tiltAngleIncremental: Math.random() * 0.08 + 0.02,
          tiltAngle: 0
        });
      }

      let animationFrameId: number;
      const draw = () => {
        ctx.clearRect(0, 0, width, height);

        particles.forEach((p, idx) => {
          p.tiltAngle += p.tiltAngleIncremental;
          p.y += (Math.cos(p.d) + 3 + p.r / 2) / 1.5;
          p.x += Math.sin(p.tiltAngle);
          p.tilt = Math.sin(p.tiltAngle - idx / 3) * 12;

          ctx.beginPath();
          ctx.lineWidth = p.r;
          ctx.strokeStyle = p.color;
          ctx.moveTo(p.x + p.tilt + p.r / 2, p.y);
          ctx.lineTo(p.x + p.tilt, p.y + p.tilt + p.r / 2);
          ctx.stroke();
        });

        if (particles.some(p => p.y < height)) {
          animationFrameId = requestAnimationFrame(draw);
        } else {
          cancelAnimationFrame(animationFrameId);
          window.removeEventListener('resize', handleResize);
          canvas.remove();
        }
      };

      draw();
      setTimeout(() => {
        cancelAnimationFrame(animationFrameId);
        window.removeEventListener('resize', handleResize);
        canvas.remove();
      }, 3500);
    } catch (e) {
      console.warn('Confetti error:', e);
    }
  }

  compartirWhatsApp(): void {
    const f = this.factura();
    if (!f) return;

    let texto = `*FACTURA DIGITAL - ${this.facturaCfg.config().nombreComercial}*\n`;
    texto += `=========================\n`;
    texto += `*Factura:* ${f.factura}\n`;
    if (f.ncf) texto += `*NCF:* ${f.ncf}\n`;
    texto += `*Fecha:* ${new Date(f.fecha).toLocaleString('es-DO')}\n`;
    if (f.cliente) texto += `*Cliente:* ${f.cliente}\n`;
    texto += `*Atendió:* ${f.vendedor}\n`;
    texto += `-------------------------\n`;

    f.lineas.forEach(l => {
      texto += `• ${l.cantidad}x ${l.descripcion}`;
      if (l.imei) texto += ` (IMEI: ${l.imei})`;
      texto += ` - RD$${l.total.toLocaleString('es-DO', { minimumFractionDigits: 2 })}\n`;
    });

    texto += `-------------------------\n`;
    texto += `*Subtotal:* RD$${f.subtotal.toLocaleString('es-DO', { minimumFractionDigits: 2 })}\n`;
    if (f.descuento > 0) texto += `*Descuento:* -RD$${f.descuento.toLocaleString('es-DO', { minimumFractionDigits: 2 })}\n`;
    texto += `*ITBIS (18%):* RD$${f.impuesto.toLocaleString('es-DO', { minimumFractionDigits: 2 })}\n`;
    texto += `*TOTAL:* *RD$${f.total.toLocaleString('es-DO', { minimumFractionDigits: 2 })}*\n`;
    texto += `*Pago:* ${f.esCredito ? 'A CRÉDITO' : (f.metodoPago || 'Efectivo')}\n`;
    if (this.facturaCfg.config().mensajePie) {
      texto += `\n_${this.facturaCfg.config().mensajePie}_`;
    }

    const enlace = `https://api.whatsapp.com/send?text=${encodeURIComponent(texto)}`;
    window.open(enlace, '_blank');
  }
}
