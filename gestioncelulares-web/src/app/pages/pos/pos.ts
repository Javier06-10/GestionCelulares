import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { CajaService } from '../../core/caja.service';
import { CatalogoService, Producto } from '../../core/catalogo.service';
import { Cliente, ClienteService } from '../../core/cliente.service';
import { InventarioService } from '../../core/inventario.service';
import { MetodoPago, VentaService } from '../../core/venta.service';

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
  imports: [FormsModule, RouterLink, LucideAngularModule, CurrencyPipe],
  templateUrl: './pos.html'
})
export class Pos {
  private inventario = inject(InventarioService);
  private catalogo = inject(CatalogoService);
  private caja = inject(CajaService);
  private clientesSrv = inject(ClienteService);
  private ventas = inject(VentaService);
  auth = inject(AuthService);

  // Estado de caja
  cajaAbierta = signal<boolean | null>(null); // null = cargando

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
  busquedaCliente = signal('');
  resultadosCliente = signal<Cliente[]>([]);
  clienteSel = signal<Cliente | null>(null);

  // Resultado
  procesando = signal(false);
  errorVenta = signal<string | null>(null);
  ventaOk = signal<{ factura: string; total: number } | null>(null);

  private get sucursalId(): number | null {
    return this.auth.usuario()?.sucursalId ?? null;
  }

  constructor() {
    this.verificarCaja();
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

  // ----- Cliente -----
  buscarCliente(v: string): void {
    this.busquedaCliente.set(v);
    if (v.trim().length < 2) { this.resultadosCliente.set([]); return; }
    this.clientesSrv.buscar(v.trim()).subscribe(r => this.resultadosCliente.set(r.slice(0, 6)));
  }

  elegirCliente(c: Cliente): void {
    this.clienteSel.set(c);
    this.resultadosCliente.set([]);
    this.busquedaCliente.set('');
  }

  quitarCliente(): void {
    this.clienteSel.set(null);
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
        this.ventaOk.set({ factura: v.numeroFactura ?? ('Venta #' + v.ventaId), total: v.total });
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

  nuevaVenta(): void {
    this.ventaOk.set(null);
    this.catalogo.productos().subscribe(p => this.productos.set(p)); // refrescar stock/disponibles
  }
}
