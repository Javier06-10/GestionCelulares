import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Cuenta, CuentaService } from '../../core/cuenta.service';

const TIPOS = ['Activo', 'Pasivo', 'Capital', 'Ingreso', 'Costo', 'Gasto'];

@Component({
  selector: 'app-cuentas',
  imports: [ReactiveFormsModule, LucideAngularModule],
  templateUrl: './cuentas.html'
})
export class Cuentas {
  private fb = inject(FormBuilder);
  private servicio = inject(CuentaService);

  readonly tipos = TIPOS;

  cuentas = signal<Cuenta[]>([]);
  cargando = signal(false);

  // Resumen por tipo (las 6 principales)
  resumen = computed(() => TIPOS.map(t => ({
    tipo: t,
    cantidad: this.cuentas().filter(c => c.tipo === t).length
  })));

  // Modal alta / edición
  modal = signal(false);
  editando = signal<Cuenta | null>(null);
  guardando = signal(false);
  errorForm = signal<string | null>(null);
  form = this.fb.nonNullable.group({
    cuentaPadreId: [null as number | null],
    tipo: ['Activo'],
    codigo: ['', [Validators.required, Validators.maxLength(20)]],
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    permiteMovimiento: [true],
    activo: [true]
  });

  // Posibles padres (todas las cuentas, para anidar)
  padres = computed(() => this.cuentas());

  constructor() { this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar().subscribe({
      next: c => { this.cuentas.set(c); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  tipoClase(tipo: string): string {
    switch (tipo) {
      case 'Activo': return 'bg-blue-500/10 text-blue-600';
      case 'Pasivo': return 'bg-red-500/10 text-red-500';
      case 'Capital': return 'bg-tech-purple/10 text-tech-purple';
      case 'Ingreso': return 'bg-tech-accent/10 text-tech-accent';
      case 'Costo': return 'bg-amber-500/10 text-amber-600';
      case 'Gasto': return 'bg-rose-500/10 text-rose-500';
      default: return 'bg-slate-400/15 text-slate-500';
    }
  }

  abrirNueva(padre?: Cuenta): void {
    this.editando.set(null);
    this.errorForm.set(null);
    this.form.reset({
      cuentaPadreId: padre?.cuentaContableId ?? null,
      tipo: padre?.tipo ?? 'Activo',
      codigo: this.sugerirCodigo(padre ?? null),
      nombre: '',
      permiteMovimiento: true,
      activo: true
    });
    this.modal.set(true);
  }

  abrirEditar(c: Cuenta): void {
    this.editando.set(c);
    this.errorForm.set(null);
    this.form.reset({
      cuentaPadreId: c.cuentaPadreId,
      tipo: c.tipo,
      codigo: c.codigo,
      nombre: c.nombre,
      permiteMovimiento: c.permiteMovimiento,
      activo: c.activo
    });
    this.modal.set(true);
  }

  // Sugiere el próximo código bajo un padre (o raíz)
  private sugerirCodigo(padre: Cuenta | null): string {
    const prefijo = padre ? padre.codigo + '.' : '';
    const hijas = this.cuentas().filter(c => c.cuentaPadreId === (padre?.cuentaContableId ?? null));
    let max = 0;
    for (const h of hijas) {
      const ult = h.codigo.split('.').pop() ?? '';
      const n = parseInt(ult, 10);
      if (!isNaN(n) && n > max) max = n;
    }
    const sig = (max + 1).toString().padStart(2, '0');
    return prefijo + sig;
  }

  // Recalcula el código sugerido cuando cambia el padre (solo en alta)
  onPadreChange(valor: string): void {
    if (this.editando()) return;
    const id = valor ? Number(valor) : null;
    const padre = id ? this.cuentas().find(c => c.cuentaContableId === id) ?? null : null;
    this.form.controls.cuentaPadreId.setValue(id);
    this.form.controls.tipo.setValue(padre?.tipo ?? 'Activo');
    this.form.controls.codigo.setValue(this.sugerirCodigo(padre));
  }

  guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();
    const editando = this.editando();

    const accion = editando
      ? this.servicio.actualizar(editando.cuentaContableId, { nombre: v.nombre.trim(), permiteMovimiento: v.permiteMovimiento, activo: v.activo })
      : this.servicio.crear({
          codigo: v.codigo.trim(),
          nombre: v.nombre.trim(),
          cuentaPadreId: v.cuentaPadreId,
          tipo: v.cuentaPadreId ? null : v.tipo,
          permiteMovimiento: v.permiteMovimiento
        });

    accion.subscribe({
      next: () => { this.guardando.set(false); this.modal.set(false); this.cargar(); },
      error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo guardar la cuenta.'); }
    });
  }

  eliminar(c: Cuenta): void {
    if (c.esSistema) return;
    if (!confirm(`¿Eliminar la cuenta ${c.codigo} ${c.nombre}?`)) return;
    this.servicio.eliminar(c.cuentaContableId).subscribe({
      next: () => this.cargar(),
      error: err => alert(err.error?.error ?? 'No se pudo eliminar.')
    });
  }
}
