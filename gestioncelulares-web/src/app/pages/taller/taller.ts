import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { Cliente } from '../../core/cliente.service';
import { Orden, OrdenResumen, TallerService } from '../../core/taller.service';
import { Usuario, UsuarioService } from '../../core/usuario.service';
import { ClienteSelector } from '../../shared/cliente-selector/cliente-selector';

interface Columna { estado: string; titulo: string; color: string; }

@Component({
  selector: 'app-taller',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe, ClienteSelector],
  templateUrl: './taller.html'
})
export class Taller {
  private fb = inject(FormBuilder);
  private servicio = inject(TallerService);
  private usuarios = inject(UsuarioService);
  auth = inject(AuthService);

  ordenes = signal<OrdenResumen[]>([]);
  cargando = signal(false);
  tecnicos = signal<Usuario[]>([]);

  columnas: Columna[] = [
    { estado: 'Recibido', titulo: 'Recibido', color: 'border-t-slate-400' },
    { estado: 'EnReparacion', titulo: 'En reparación', color: 'border-t-tech-purple' },
    { estado: 'Reparado', titulo: 'Reparado', color: 'border-t-tech-accent' },
    { estado: 'Entregado', titulo: 'Entregado', color: 'border-t-slate-300' }
  ];

  porColumna = computed(() => {
    const map: Record<string, OrdenResumen[]> = {};
    for (const col of this.columnas) map[col.estado] = [];
    for (const o of this.ordenes()) (map[o.estado] ??= []).push(o);
    return map;
  });

  // Detalle
  detalle = signal<Orden | null>(null);
  errorDetalle = signal<string | null>(null);

  // Transiciones permitidas
  private transiciones: Record<string, string[]> = {
    Recibido: ['EnReparacion', 'Cancelado'],
    EnReparacion: ['Reparado', 'Cancelado'],
    Reparado: ['Entregado', 'EnReparacion'],
    Entregado: [],
    Cancelado: []
  };
  siguientes = computed(() => this.transiciones[this.detalle()?.estado ?? ''] ?? []);

  // Nueva orden
  modalNueva = signal(false);
  guardandoNueva = signal(false);
  errorNueva = signal<string | null>(null);
  clienteSel = signal<Cliente | null>(null);
  formNueva = this.fb.nonNullable.group({
    equipoDescripcion: ['', Validators.required],
    diagnostico: [''],
    tecnicoId: [null as number | null],
    anticipo: [0, [Validators.min(0)]],
    costoEstimado: [0, [Validators.min(0)]]
  });

  // Repuesto
  modalRepuesto = signal(false);
  formRepuesto = this.fb.nonNullable.group({
    descripcion: ['', Validators.required],
    cantidad: [1, [Validators.required, Validators.min(1)]],
    costo: [0, [Validators.min(0)]]
  });

  // Foto
  urlFoto = signal('');

  // Entrega
  modalEntrega = signal(false);
  formEntrega = this.fb.nonNullable.group({
    costoFinal: [0, [Validators.min(0)]],
    comisionTecnico: [0, [Validators.min(0)]]
  });

  constructor() {
    this.cargar();
    this.usuarios.listar(true).subscribe({
      next: u => this.tecnicos.set(u),
      error: () => this.tecnicos.set([]) // no Admin: sin lista de técnicos
    });
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.buscar().subscribe({
      next: d => { this.ordenes.set(d); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  // ----- Detalle -----
  abrirDetalle(id: number): void {
    this.errorDetalle.set(null);
    this.servicio.porId(id).subscribe({ next: o => this.detalle.set(o) });
  }
  cerrarDetalle(): void { this.detalle.set(null); }

  asignarTecnico(tecnicoId: number | null): void {
    const o = this.detalle();
    if (!o) return;
    this.servicio.actualizar(o.ordenTallerId, {
      equipoDescripcion: o.equipoDescripcion,
      diagnostico: o.diagnostico,
      tecnicoId: tecnicoId ? Number(tecnicoId) : null,
      anticipo: o.anticipo,
      costoEstimado: o.costoEstimado
    }).subscribe({ next: x => this.detalle.set(x), error: e => this.errorDetalle.set(e.error?.error ?? 'No se pudo asignar.') });
  }

  mover(estado: string): void {
    const o = this.detalle();
    if (!o) return;
    if (estado === 'Entregado') { this.abrirEntrega(); return; }
    this.servicio.cambiarEstado(o.ordenTallerId, { estado }).subscribe({
      next: x => { this.detalle.set(x); this.cargar(); },
      error: e => this.errorDetalle.set(e.error?.error ?? 'No se pudo cambiar el estado.')
    });
  }

  // ----- Entrega -----
  abrirEntrega(): void {
    const o = this.detalle();
    this.formEntrega.reset({ costoFinal: o?.costoEstimado ?? 0, comisionTecnico: 0 });
    this.modalEntrega.set(true);
  }
  confirmarEntrega(): void {
    const o = this.detalle();
    if (!o) return;
    const v = this.formEntrega.getRawValue();
    this.servicio.cambiarEstado(o.ordenTallerId, { estado: 'Entregado', costoFinal: v.costoFinal, comisionTecnico: v.comisionTecnico }).subscribe({
      next: x => { this.detalle.set(x); this.modalEntrega.set(false); this.cargar(); },
      error: e => this.errorDetalle.set(e.error?.error ?? 'No se pudo entregar.')
    });
  }

  // ----- Repuesto -----
  abrirRepuesto(): void {
    this.formRepuesto.reset({ descripcion: '', cantidad: 1, costo: 0 });
    this.modalRepuesto.set(true);
  }
  agregarRepuesto(): void {
    const o = this.detalle();
    if (this.formRepuesto.invalid || !o) { this.formRepuesto.markAllAsTouched(); return; }
    const v = this.formRepuesto.getRawValue();
    this.servicio.agregarRepuesto(o.ordenTallerId, { descripcion: v.descripcion.trim(), cantidad: v.cantidad, costo: v.costo }).subscribe({
      next: x => { this.detalle.set(x); this.modalRepuesto.set(false); },
      error: e => this.errorDetalle.set(e.error?.error ?? 'No se pudo agregar el repuesto.')
    });
  }

  // ----- Foto -----
  agregarFoto(): void {
    const o = this.detalle();
    const url = this.urlFoto().trim();
    if (!o || !url) return;
    this.servicio.agregarFoto(o.ordenTallerId, url).subscribe({
      next: x => { this.detalle.set(x); this.urlFoto.set(''); },
      error: e => this.errorDetalle.set(e.error?.error ?? 'No se pudo agregar la foto.')
    });
  }

  // ----- Nueva orden -----
  abrirNueva(): void {
    this.errorNueva.set(null);
    this.clienteSel.set(null);
    this.formNueva.reset({ equipoDescripcion: '', diagnostico: '', tecnicoId: null, anticipo: 0, costoEstimado: 0 });
    this.modalNueva.set(true);
  }
  crearOrden(): void {
    if (this.formNueva.invalid) { this.formNueva.markAllAsTouched(); return; }
    const suc = this.auth.usuario()?.sucursalId;
    if (!suc) { this.errorNueva.set('Tu usuario no tiene sucursal asignada.'); return; }
    this.guardandoNueva.set(true);
    this.errorNueva.set(null);
    const v = this.formNueva.getRawValue();
    this.servicio.crear({
      sucursalId: suc,
      clienteId: this.clienteSel()?.clienteId ?? null,
      equipoDescripcion: v.equipoDescripcion.trim(),
      diagnostico: v.diagnostico?.trim() || null,
      tecnicoId: v.tecnicoId ? Number(v.tecnicoId) : null,
      anticipo: v.anticipo,
      costoEstimado: v.costoEstimado
    }).subscribe({
      next: o => { this.guardandoNueva.set(false); this.modalNueva.set(false); this.cargar(); this.detalle.set(o); },
      error: e => { this.guardandoNueva.set(false); this.errorNueva.set(e.error?.error ?? 'No se pudo crear la orden.'); }
    });
  }

  estadoClase(e: string): string {
    switch (e) {
      case 'Recibido': return 'bg-slate-400/15 text-slate-500';
      case 'EnReparacion': return 'bg-tech-purple/10 text-tech-purple';
      case 'Reparado': return 'bg-tech-accent/10 text-tech-accent';
      case 'Entregado': return 'bg-slate-400/15 text-slate-500';
      case 'Cancelado': return 'bg-red-500/10 text-red-500';
      default: return 'bg-slate-400/15 text-slate-500';
    }
  }
}
