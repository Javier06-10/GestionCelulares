import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Cuenta, CuentaService } from '../../core/cuenta.service';

const TIPOS = ['Activo', 'Pasivo', 'Capital', 'Ingreso', 'Costo', 'Gasto'];

/** Metadatos visuales y contables de cada familia de cuentas. */
interface TemaFamilia {
  icono: string;
  naturaleza: 'Deudora' | 'Acreedora';
  grupo: 'Balance' | 'Resultado';
  chip: string;     // chip del ícono (bg + texto)
  texto: string;    // color de texto/acento
  stripe: string;   // franja lateral de acento
  punto: string;    // punto/indicador
}

const FAMILIAS: Record<string, TemaFamilia> = {
  Activo:  { icono: 'wallet',       naturaleza: 'Deudora',   grupo: 'Balance',   chip: 'bg-emerald-500/10 text-emerald-600', texto: 'text-emerald-600', stripe: 'bg-emerald-500', punto: 'bg-emerald-500' },
  Pasivo:  { icono: 'credit-card',  naturaleza: 'Acreedora', grupo: 'Balance',   chip: 'bg-rose-500/10 text-rose-500',       texto: 'text-rose-500',    stripe: 'bg-rose-500',    punto: 'bg-rose-500' },
  Capital: { icono: 'hand-coins',   naturaleza: 'Acreedora', grupo: 'Balance',   chip: 'bg-violet-500/10 text-violet-600',   texto: 'text-violet-600',  stripe: 'bg-violet-500',  punto: 'bg-violet-500' },
  Ingreso: { icono: 'trending-up',  naturaleza: 'Acreedora', grupo: 'Resultado', chip: 'bg-teal-500/10 text-teal-600',       texto: 'text-teal-600',    stripe: 'bg-teal-500',    punto: 'bg-teal-500' },
  Costo:   { icono: 'package',      naturaleza: 'Deudora',   grupo: 'Resultado', chip: 'bg-amber-500/10 text-amber-600',     texto: 'text-amber-600',   stripe: 'bg-amber-500',   punto: 'bg-amber-500' },
  Gasto:   { icono: 'receipt',      naturaleza: 'Deudora',   grupo: 'Resultado', chip: 'bg-orange-500/10 text-orange-600',   texto: 'text-orange-600',  stripe: 'bg-orange-500',  punto: 'bg-orange-500' }
};

interface FamiliaVista {
  tipo: string;
  tema: TemaFamilia;
  raiz: Cuenta | null;
  hijas: Cuenta[];
  total: number;
  conMovimiento: number;
}

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
  buscar = signal('');
  colapsados = signal<Set<string>>(new Set());

  tema(tipo: string): TemaFamilia { return FAMILIAS[tipo] ?? FAMILIAS['Activo']; }

  // Agrupación por familia, ordenada y filtrada por el buscador
  familias = computed<FamiliaVista[]>(() => {
    const q = this.buscar().trim().toLowerCase();
    return TIPOS.map(tipo => {
      let cuentas = this.cuentas().filter(c => c.tipo === tipo);
      const raiz = cuentas.find(c => c.nivel === 0) ?? null;
      let hijas = cuentas.filter(c => c.nivel >= 1);
      if (q) hijas = hijas.filter(c => c.codigo.toLowerCase().includes(q) || c.nombre.toLowerCase().includes(q));
      hijas = [...hijas].sort((a, b) => a.codigo.localeCompare(b.codigo, undefined, { numeric: true }));
      return {
        tipo,
        tema: this.tema(tipo),
        raiz,
        hijas,
        total: hijas.length,
        conMovimiento: hijas.filter(c => c.permiteMovimiento && c.activo).length
      };
    }).filter(f => f.raiz && (!q || f.hijas.length > 0));
  });

  totalCuentas = computed(() => this.cuentas().filter(c => c.nivel >= 1).length);

  buscando = computed(() => this.buscar().trim().length > 0);

  abierta(tipo: string): boolean {
    return this.buscando() || !this.colapsados().has(tipo);
  }

  alternarFamilia(tipo: string): void {
    if (this.buscando()) return;
    this.colapsados.update(s => {
      const n = new Set(s);
      n.has(tipo) ? n.delete(tipo) : n.add(tipo);
      return n;
    });
  }

  colapsarTodo(colapsar: boolean): void {
    this.colapsados.set(colapsar ? new Set(TIPOS) : new Set());
  }

  // ---- Modal alta / edición ----
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

  padres = computed(() => this.cuentas());

  constructor() { this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar().subscribe({
      next: c => { this.cuentas.set(c); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
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
