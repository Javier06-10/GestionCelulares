import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { VentaResumen, VentaService } from '../../core/venta.service';

const TIPOS_ANULACION = [
  { v: '01', n: 'Deterioro de factura' },
  { v: '02', n: 'Errores de impresión' },
  { v: '03', n: 'Impresión defectuosa' },
  { v: '04', n: 'Duplicidad de factura' },
  { v: '05', n: 'Corrección de información' },
  { v: '06', n: 'Cambio de productos' },
  { v: '07', n: 'Devolución de productos' },
  { v: '08', n: 'Omisión de productos' },
  { v: '09', n: 'Errores en secuencia de NCF' }
];

@Component({
  selector: 'app-ventas',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe],
  templateUrl: './ventas.html'
})
export class Ventas {
  private fb = inject(FormBuilder);
  private servicio = inject(VentaService);
  auth = inject(AuthService);

  readonly tipos = TIPOS_ANULACION;

  ventas = signal<VentaResumen[]>([]);
  cargando = signal(false);
  desde = signal(this.inicioMes());
  hasta = signal(this.hoy());

  totalPeriodo = computed(() => this.ventas().filter(v => v.estado !== 'Anulada').reduce((a, v) => a + v.total, 0));

  // Modal anular
  modal = signal(false);
  objetivo = signal<VentaResumen | null>(null);
  anulando = signal(false);
  errorModal = signal<string | null>(null);
  form = this.fb.nonNullable.group({
    tipoAnulacion: ['05', Validators.required],
    motivo: [''],
    emitirNotaCredito: [false]
  });

  constructor() { this.cargar(); }

  private hoy(): string { return new Date().toISOString().slice(0, 10); }
  private inicioMes(): string { const d = new Date(); return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10); }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.buscar(this.desde(), this.hasta()).subscribe({
      next: v => { this.ventas.set(v); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  abrirAnular(v: VentaResumen): void {
    this.objetivo.set(v);
    this.errorModal.set(null);
    this.form.reset({ tipoAnulacion: '05', motivo: '', emitirNotaCredito: false });
    this.modal.set(true);
  }

  confirmarAnular(): void {
    const v = this.objetivo();
    if (this.form.invalid || !v) { this.form.markAllAsTouched(); return; }
    this.anulando.set(true);
    this.errorModal.set(null);
    const f = this.form.getRawValue();
    this.servicio.anular(v.ventaId, { tipoAnulacion: f.tipoAnulacion, motivo: f.motivo?.trim() || null, emitirNotaCredito: f.emitirNotaCredito }).subscribe({
      next: () => { this.anulando.set(false); this.modal.set(false); this.cargar(); },
      error: err => { this.anulando.set(false); this.errorModal.set(err.error?.error ?? 'No se pudo anular la venta.'); }
    });
  }
}
