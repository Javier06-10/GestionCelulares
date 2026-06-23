import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { NcfService, SecuenciaNcf } from '../../core/ncf.service';
import { CountUpDirective } from '../../shared/count-up.directive';

@Component({
  selector: 'app-ncf',
  imports: [ReactiveFormsModule, LucideAngularModule, DecimalPipe, DatePipe, CountUpDirective],
  templateUrl: './ncf.html'
})
export class Ncf {
  private fb = inject(FormBuilder);
  private servicio = inject(NcfService);

  secuencias = signal<SecuenciaNcf[]>([]);
  cargando = signal(false);

  rangosActivos = computed(() => this.secuencias().filter(s => s.activo).length);
  totalDisponibles = computed(() => this.secuencias().filter(s => s.activo).reduce((a, s) => a + Math.max(0, s.disponibles), 0));
  porAgotarse = computed(() => this.secuencias().filter(s => s.activo && s.disponibles > 0 && s.disponibles < 20).length);

  modal = signal(false);
  editando = signal<SecuenciaNcf | null>(null);
  guardando = signal(false);
  errorForm = signal<string | null>(null);
  form = this.fb.nonNullable.group({
    serie: ['B', [Validators.required, Validators.maxLength(2)]],
    secuencia: [1, [Validators.required, Validators.min(1)]],
    hasta: [0, [Validators.required, Validators.min(0)]],
    vencimiento: [''],
    activo: [false]
  });

  constructor() { this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar().subscribe({
      next: s => { this.secuencias.set(s); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  abrir(s: SecuenciaNcf): void {
    this.editando.set(s);
    this.errorForm.set(null);
    this.form.reset({
      serie: s.serie,
      secuencia: s.secuencia,
      hasta: s.hasta,
      vencimiento: s.vencimiento ? s.vencimiento.slice(0, 10) : '',
      activo: s.activo
    });
    this.modal.set(true);
  }

  guardar(): void {
    const s = this.editando();
    if (this.form.invalid || !s) { this.form.markAllAsTouched(); return; }
    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();
    this.servicio.configurar(s.secuenciaNcfId, {
      serie: v.serie.trim().toUpperCase(),
      secuencia: v.secuencia,
      hasta: v.hasta,
      vencimiento: v.vencimiento || null,
      activo: v.activo
    }).subscribe({
      next: () => { this.guardando.set(false); this.modal.set(false); this.cargar(); },
      error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo guardar.'); }
    });
  }
}
