import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { Agotado, FaltanteManual, FaltanteService } from '../../core/faltante.service';

@Component({
  selector: 'app-faltantes',
  imports: [ReactiveFormsModule, LucideAngularModule, DatePipe],
  templateUrl: './faltantes.html'
})
export class Faltantes {
  private fb = inject(FormBuilder);
  private servicio = inject(FaltanteService);
  auth = inject(AuthService);

  agotados = signal<Agotado[]>([]);
  manuales = signal<FaltanteManual[]>([]);
  cargando = signal(false);

  total = computed(() => this.agotados().length + this.manuales().length);

  modal = signal(false);
  guardando = signal(false);
  errorForm = signal<string | null>(null);
  form = this.fb.nonNullable.group({
    descripcion: ['', [Validators.required, Validators.maxLength(200)]],
    cantidadDeseada: [1, [Validators.required, Validators.min(1)]],
    notas: ['']
  });

  constructor() { this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar().subscribe({
      next: r => { this.agotados.set(r.agotados); this.manuales.set(r.manuales); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  abrirNuevo(): void {
    this.errorForm.set(null);
    this.form.reset({ descripcion: '', cantidadDeseada: 1, notas: '' });
    this.modal.set(true);
  }

  guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();
    this.servicio.agregar({ descripcion: v.descripcion.trim(), cantidadDeseada: v.cantidadDeseada, notas: v.notas?.trim() || null }).subscribe({
      next: () => { this.guardando.set(false); this.modal.set(false); this.cargar(); },
      error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo guardar.'); }
    });
  }

  resolver(f: FaltanteManual): void {
    this.servicio.resolver(f.faltanteId).subscribe({ next: () => this.cargar() });
  }
}
