import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { DevolucionService, NotaCredito } from '../../core/devolucion.service';
import { VentaResumen, VentaService } from '../../core/venta.service';
import { CelebracionComponent } from '../../shared/celebracion.component';
import { CountUpDirective } from '../../shared/count-up.directive';
import { PhoneLoaderComponent } from '../../shared/phone-loader.component';

@Component({
  selector: 'app-devoluciones',
  imports: [FormsModule, LucideAngularModule, CurrencyPipe, DatePipe, CountUpDirective, PhoneLoaderComponent, CelebracionComponent],
  templateUrl: './devoluciones.html'
})
export class Devoluciones {
  private servicio = inject(DevolucionService);
  private ventaSrv = inject(VentaService);

  notas = signal<NotaCredito[]>([]);
  cargando = signal(false);

  // KPIs
  kpiCantidad = computed(() => this.notas().length);
  kpiTotal = computed(() => this.notas().reduce((a, n) => a + n.total, 0));

  // Modal nueva devolución
  modal = signal(false);
  candidatas = signal<VentaResumen[]>([]);
  cargandoVentas = signal(false);
  buscarVenta = signal('');
  ventaSel = signal<VentaResumen | null>(null);
  motivo = signal('');
  guardando = signal(false);
  error = signal<string | null>(null);

  celebrando = signal(false);

  // Ventas candidatas: completadas, con NCF y no seleccionada, filtradas por el término
  ventasVista = computed<VentaResumen[]>(() => {
    const q = this.buscarVenta().toLowerCase().trim();
    return this.candidatas()
      .filter(v => v.estado === 'Completada' && !!v.ncf)
      .filter(v => !q || `${v.numeroFactura ?? ''} ${v.ncf ?? ''} ${v.cliente ?? ''}`.toLowerCase().includes(q))
      .slice(0, 30);
  });

  constructor() { this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar().subscribe({
      next: d => { this.notas.set(d); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  abrirModal(): void {
    this.ventaSel.set(null);
    this.motivo.set('');
    this.buscarVenta.set('');
    this.error.set(null);
    this.modal.set(true);
    // Cargar ventas de los últimos 90 días como candidatas
    this.cargandoVentas.set(true);
    const hasta = new Date();
    const desde = new Date(); desde.setDate(desde.getDate() - 90);
    this.ventaSrv.buscar(this.fecha(desde), this.fecha(hasta)).subscribe({
      next: v => { this.candidatas.set(v); this.cargandoVentas.set(false); },
      error: () => this.cargandoVentas.set(false)
    });
  }
  cerrarModal(): void { this.modal.set(false); }

  private fecha(d: Date): string { return d.toISOString().slice(0, 10); }

  seleccionar(v: VentaResumen): void { this.ventaSel.set(v); }

  confirmar(): void {
    const v = this.ventaSel();
    if (!v) return;
    this.guardando.set(true);
    this.error.set(null);
    this.servicio.crear({ ventaId: v.ventaId, motivo: this.motivo().trim() || null }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.modal.set(false);
        this.cargar();
        this.celebrando.set(true);
        setTimeout(() => this.celebrando.set(false), 2400);
      },
      error: err => { this.guardando.set(false); this.error.set(err.error?.error ?? 'No se pudo registrar la devolución.'); }
    });
  }
}
