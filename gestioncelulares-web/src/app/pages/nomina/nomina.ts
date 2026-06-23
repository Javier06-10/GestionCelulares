import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Empleado, NominaService, PagoEmpleado } from '../../core/nomina.service';

const TIPOS = ['Salario', 'Adelanto', 'Bono', 'Comisión'] as const;

@Component({
  selector: 'app-nomina',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe],
  templateUrl: './nomina.html'
})
export class Nomina {
  private fb = inject(FormBuilder);
  private servicio = inject(NominaService);

  readonly tipos = TIPOS;

  pagos = signal<PagoEmpleado[]>([]);
  total = signal(0);
  empleados = signal<Empleado[]>([]);
  cargando = signal(false);
  filtroEmpleado = signal<number | null>(null);

  modal = signal(false);
  guardando = signal(false);
  errorForm = signal<string | null>(null);
  form = this.fb.nonNullable.group({
    empleadoId: [0, [Validators.required, Validators.min(1)]],
    tipo: ['Salario', Validators.required],
    monto: [0, [Validators.required, Validators.min(0.01)]],
    periodo: [''],
    notas: ['']
  });

  constructor() {
    this.servicio.empleados().subscribe(e => this.empleados.set(e));
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar(this.filtroEmpleado()).subscribe({
      next: r => { this.pagos.set(r.pagos); this.total.set(r.totalPagado); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  filtrar(valor: string): void {
    this.filtroEmpleado.set(valor ? Number(valor) : null);
    this.cargar();
  }

  tipoClase(t: string): string {
    switch (t) {
      case 'Salario': return 'bg-tech-accent/10 text-tech-accent';
      case 'Adelanto': return 'bg-amber-500/10 text-amber-600';
      case 'Bono': return 'bg-violet-500/10 text-violet-600';
      case 'Comisión': return 'bg-teal-500/10 text-teal-600';
      default: return 'bg-slate-400/15 text-slate-500';
    }
  }

  abrirNuevo(): void {
    this.errorForm.set(null);
    this.form.reset({ empleadoId: 0, tipo: 'Salario', monto: 0, periodo: '', notas: '' });
    this.modal.set(true);
  }

  guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();
    this.servicio.registrar({
      empleadoId: v.empleadoId,
      tipo: v.tipo,
      monto: v.monto,
      periodo: v.periodo?.trim() || null,
      notas: v.notas?.trim() || null
    }).subscribe({
      next: () => { this.guardando.set(false); this.modal.set(false); this.cargar(); },
      error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo registrar el pago.'); }
    });
  }
}
