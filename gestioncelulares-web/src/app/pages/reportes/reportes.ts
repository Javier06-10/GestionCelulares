import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import {
  exportarCsv, ReporteCaja, ReporteCobros, ReporteInventario,
  ReporteMorosidad, ReporteService, ReporteTaller, ReporteVentas
} from '../../core/reporte.service';

type Tab = 'ventas' | 'inventario' | 'morosidad' | 'caja' | 'taller' | 'cobros';

@Component({
  selector: 'app-reportes',
  imports: [LucideAngularModule, CurrencyPipe, DecimalPipe, DatePipe],
  templateUrl: './reportes.html',
  styleUrl: './reportes.scss'
})
export class Reportes {
  private servicio = inject(ReporteService);

  tab = signal<Tab>('ventas');
  cargando = signal(false);

  // Rango de fechas (por defecto: mes actual)
  desde = signal(this.inicioMes());
  hasta = signal(this.hoy());

  ventas = signal<ReporteVentas | null>(null);
  inventario = signal<ReporteInventario | null>(null);
  morosidad = signal<ReporteMorosidad | null>(null);
  caja = signal<ReporteCaja | null>(null);
  taller = signal<ReporteTaller | null>(null);
  cobros = signal<ReporteCobros | null>(null);

  tabs: { id: Tab; label: string; icono: string; conFecha: boolean }[] = [
    { id: 'ventas', label: 'Ventas', icono: 'trending-up', conFecha: true },
    { id: 'inventario', label: 'Inventario', icono: 'boxes', conFecha: false },
    { id: 'morosidad', label: 'Morosidad', icono: 'circle-alert', conFecha: false },
    { id: 'caja', label: 'Caja', icono: 'wallet', conFecha: true },
    { id: 'taller', label: 'Taller', icono: 'wrench', conFecha: true },
    { id: 'cobros', label: 'Cobros', icono: 'coins', conFecha: true }
  ];

  private hoy(): string { return new Date().toISOString().slice(0, 10); }
  private inicioMes(): string { const d = new Date(); return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10); }

  get tabActual() { return this.tabs.find(t => t.id === this.tab())!; }

  constructor() { this.generar(); }

  cambiarTab(t: Tab): void {
    this.tab.set(t);
    this.generar();
  }

  generar(): void {
    this.cargando.set(true);
    const d = this.desde(), h = this.hasta();
    const fin = () => this.cargando.set(false);
    switch (this.tab()) {
      case 'ventas': this.servicio.ventas(d, h).subscribe({ next: r => { this.ventas.set(r); fin(); }, error: fin }); break;
      case 'inventario': this.servicio.inventario().subscribe({ next: r => { this.inventario.set(r); fin(); }, error: fin }); break;
      case 'morosidad': this.servicio.morosidad().subscribe({ next: r => { this.morosidad.set(r); fin(); }, error: fin }); break;
      case 'caja': this.servicio.caja(d, h).subscribe({ next: r => { this.caja.set(r); fin(); }, error: fin }); break;
      case 'taller': this.servicio.taller(d, h).subscribe({ next: r => { this.taller.set(r); fin(); }, error: fin }); break;
      case 'cobros': this.servicio.cobros(d, h).subscribe({ next: r => { this.cobros.set(r); fin(); }, error: fin }); break;
    }
  }

  // ----- Exportaciones CSV -----
  expVentas(): void {
    const r = this.ventas(); if (!r) return;
    exportarCsv('reporte-ventas', ['Fecha', 'Cantidad', 'Total', 'Ganancia'],
      r.porDia.map(d => [d.fecha.slice(0, 10), d.cantidad, d.total, d.ganancia]));
  }
  expInventario(): void {
    const r = this.inventario(); if (!r) return;
    exportarCsv('reporte-inventario', ['Producto', 'Marca', 'Variante', 'Disponibles', 'Precio venta', 'Valor costo'],
      r.lineas.map(l => [l.producto, l.marca ?? '', [l.color, l.almacenamiento].filter(Boolean).join(' '), l.disponibles, l.precioVenta, l.valorCosto]));
  }
  expMorosidad(): void {
    const r = this.morosidad(); if (!r) return;
    exportarCsv('reporte-morosidad', ['Cliente', 'Teléfono', 'Cuotas vencidas', 'Monto vencido', 'Días máx'],
      r.clientes.map(c => [c.cliente, c.telefono ?? '', c.cuotasVencidas, c.montoVencido, c.diasMaxVencido]));
  }
  expCaja(): void {
    const r = this.caja(); if (!r) return;
    exportarCsv('reporte-caja', ['Sesión', 'Apertura', 'Cierre', 'Monto apertura', 'Monto cierre', 'Diferencia', 'Estado'],
      r.detalle.map(s => [s.sesionCajaId, s.fechaApertura, s.fechaCierre ?? '', s.montoApertura, s.montoCierre ?? '', s.diferencia ?? '', s.estado]));
  }
  expTaller(): void {
    const r = this.taller(); if (!r) return;
    exportarCsv('reporte-taller', ['Técnico', 'Órdenes entregadas', 'Ingresos', 'Comisión'],
      r.porTecnico.map(t => [t.tecnico, t.ordenesEntregadas, t.ingresos, t.comision]));
  }
  expCobros(): void {
    const r = this.cobros(); if (!r) return;
    exportarCsv('reporte-cobros', ['Fecha', 'Pagos', 'Total'],
      r.porDia.map(d => [d.fecha.slice(0, 10), d.pagos, d.total]));
  }

  exportar(): void {
    switch (this.tab()) {
      case 'ventas': this.expVentas(); break;
      case 'inventario': this.expInventario(); break;
      case 'morosidad': this.expMorosidad(); break;
      case 'caja': this.expCaja(); break;
      case 'taller': this.expTaller(); break;
      case 'cobros': this.expCobros(); break;
    }
  }
}
