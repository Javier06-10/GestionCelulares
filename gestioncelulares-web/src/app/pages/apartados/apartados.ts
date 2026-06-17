import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Apartado, ApartadoResumen, ApartadoService, EquipoApartable } from '../../core/apartado.service';
import { AuthService } from '../../core/auth.service';
import { Cliente } from '../../core/cliente.service';
import { MetodoPago, VentaService } from '../../core/venta.service';
import { ClienteSelector } from '../../shared/cliente-selector/cliente-selector';

@Component({
  selector: 'app-apartados',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe, ClienteSelector],
  templateUrl: './apartados.html'
})
export class Apartados {
  private fb = inject(FormBuilder);
  private servicio = inject(ApartadoService);
  private ventas = inject(VentaService);
  auth = inject(AuthService);

  apartados = signal<ApartadoResumen[]>([]);
  cargando = signal(false);
  filtroEstado = signal<string>('Activo');
  metodos = signal<MetodoPago[]>([]);

  detalle = signal<Apartado | null>(null);
  errorDetalle = signal<string | null>(null);

  // Selector de equipos (compartido por "nuevo" y "cambiar equipo")
  equipos = signal<EquipoApartable[]>([]);
  buscandoEquipo = signal('');

  // Nuevo apartado
  modalNuevo = signal(false);
  guardando = signal(false);
  errorNuevo = signal<string | null>(null);
  clienteSel = signal<Cliente | null>(null);
  equipoSel = signal<EquipoApartable | null>(null);
  formNuevo = this.fb.nonNullable.group({
    precioTotal: [0, [Validators.required, Validators.min(0.01)]],
    abonoInicial: [0, [Validators.min(0)]],
    metodoPagoId: [null as number | null],
    notas: ['']
  });

  // Abonar
  modalAbono = signal(false);
  errorAbono = signal<string | null>(null);
  formAbono = this.fb.nonNullable.group({
    monto: [0, [Validators.required, Validators.min(0.01)]],
    metodoPagoId: [null as number | null, Validators.required]
  });

  // Cambiar equipo
  modalCambio = signal(false);
  errorCambio = signal<string | null>(null);
  cambioEquipoSel = signal<EquipoApartable | null>(null);
  formCambio = this.fb.nonNullable.group({
    precioTotal: [0, [Validators.required, Validators.min(0.01)]]
  });

  // Cancelar (admin)
  modalCancelar = signal(false);
  errorCancelar = signal<string | null>(null);
  formCancelar = this.fb.nonNullable.group({
    devolverMonto: [0, [Validators.min(0)]],
    motivo: ['']
  });

  private get sucursalId(): number | undefined {
    return this.auth.usuario()?.sucursalId ?? undefined;
  }

  constructor() {
    this.cargar();
    this.ventas.metodosPago().subscribe({ next: m => this.metodos.set(m) });
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar(this.filtroEstado() || undefined).subscribe({
      next: d => { this.apartados.set(d); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }
  filtrar(estado: string): void { this.filtroEstado.set(estado); this.cargar(); }

  buscarEquipos(termino: string): void {
    this.buscandoEquipo.set(termino);
    this.servicio.equipos(this.sucursalId, termino || undefined).subscribe({ next: e => this.equipos.set(e) });
  }

  // ----- Detalle -----
  abrir(id: number): void {
    this.errorDetalle.set(null);
    this.servicio.porId(id).subscribe({ next: a => this.detalle.set(a) });
  }
  cerrar(): void { this.detalle.set(null); }

  // ----- Nuevo -----
  abrirNuevo(): void {
    this.errorNuevo.set(null);
    this.clienteSel.set(null);
    this.equipoSel.set(null);
    this.equipos.set([]);
    this.formNuevo.reset({ precioTotal: 0, abonoInicial: 0, metodoPagoId: null, notas: '' });
    this.modalNuevo.set(true);
    this.buscarEquipos('');
  }
  seleccionarEquipo(e: EquipoApartable): void {
    this.equipoSel.set(e);
    this.formNuevo.controls.precioTotal.setValue(e.precioVenta);
  }
  crear(): void {
    const cli = this.clienteSel();
    const eq = this.equipoSel();
    if (!cli) { this.errorNuevo.set('Selecciona un cliente.'); return; }
    if (!eq) { this.errorNuevo.set('Selecciona un equipo.'); return; }
    if (this.formNuevo.invalid) { this.formNuevo.markAllAsTouched(); return; }
    const v = this.formNuevo.getRawValue();
    if (v.abonoInicial > 0 && !v.metodoPagoId) { this.errorNuevo.set('Indica el método de pago del abono inicial.'); return; }
    this.guardando.set(true);
    this.errorNuevo.set(null);
    this.servicio.crear({
      clienteId: cli.clienteId, imeiId: eq.imeiId, precioTotal: v.precioTotal,
      abonoInicial: v.abonoInicial, metodoPagoId: v.abonoInicial > 0 ? v.metodoPagoId : null,
      notas: v.notas?.trim() || null
    }).subscribe({
      next: a => { this.guardando.set(false); this.modalNuevo.set(false); this.cargar(); this.detalle.set(a); },
      error: e => { this.guardando.set(false); this.errorNuevo.set(e.error?.error ?? 'No se pudo crear el apartado.'); }
    });
  }

  // ----- Abonar -----
  abrirAbono(): void {
    this.errorAbono.set(null);
    this.formAbono.reset({ monto: this.detalle()?.saldo ?? 0, metodoPagoId: null });
    this.modalAbono.set(true);
  }
  abonar(): void {
    const a = this.detalle();
    if (!a || this.formAbono.invalid) { this.formAbono.markAllAsTouched(); return; }
    const v = this.formAbono.getRawValue();
    this.servicio.abonar(a.apartadoId, { monto: v.monto, metodoPagoId: v.metodoPagoId }).subscribe({
      next: x => { this.detalle.set(x); this.modalAbono.set(false); this.cargar(); },
      error: e => this.errorAbono.set(e.error?.error ?? 'No se pudo registrar el abono.')
    });
  }

  // ----- Cambiar equipo -----
  abrirCambio(): void {
    this.errorCambio.set(null);
    this.cambioEquipoSel.set(null);
    this.equipos.set([]);
    this.formCambio.reset({ precioTotal: 0 });
    this.modalCambio.set(true);
    this.buscarEquipos('');
  }
  seleccionarCambio(e: EquipoApartable): void {
    this.cambioEquipoSel.set(e);
    this.formCambio.controls.precioTotal.setValue(e.precioVenta);
  }
  cambiar(): void {
    const a = this.detalle();
    const eq = this.cambioEquipoSel();
    if (!a || !eq) { this.errorCambio.set('Selecciona un equipo.'); return; }
    if (this.formCambio.invalid) { this.formCambio.markAllAsTouched(); return; }
    this.servicio.cambiarEquipo(a.apartadoId, { imeiId: eq.imeiId, precioTotal: this.formCambio.getRawValue().precioTotal }).subscribe({
      next: x => { this.detalle.set(x); this.modalCambio.set(false); this.cargar(); },
      error: e => this.errorCambio.set(e.error?.error ?? 'No se pudo cambiar el equipo.')
    });
  }

  // ----- Completar -----
  completar(): void {
    const a = this.detalle();
    if (!a) return;
    this.servicio.completar(a.apartadoId).subscribe({
      next: x => { this.detalle.set(x); this.cargar(); },
      error: e => this.errorDetalle.set(e.error?.error ?? 'No se pudo entregar el equipo.')
    });
  }

  // ----- Cancelar (admin) -----
  abrirCancelar(): void {
    this.errorCancelar.set(null);
    this.formCancelar.reset({ devolverMonto: 0, motivo: '' });
    this.modalCancelar.set(true);
  }
  cancelar(): void {
    const a = this.detalle();
    if (!a) return;
    const v = this.formCancelar.getRawValue();
    this.servicio.cancelar(a.apartadoId, { devolverMonto: v.devolverMonto, motivo: v.motivo?.trim() || null }).subscribe({
      next: x => { this.detalle.set(x); this.modalCancelar.set(false); this.cargar(); },
      error: e => this.errorCancelar.set(e.error?.error ?? 'No se pudo cancelar.')
    });
  }

  progreso(a: ApartadoResumen): number {
    return a.precioTotal > 0 ? Math.min(100, Math.round((a.totalAbonado / a.precioTotal) * 100)) : 0;
  }
  estadoClase(e: string): string {
    switch (e) {
      case 'Activo': return 'bg-tech-accent/10 text-tech-accent';
      case 'Completado': return 'bg-tech-purple/10 text-tech-purple';
      case 'Cancelado': return 'bg-red-500/10 text-red-500';
      default: return 'bg-slate-400/15 text-slate-500';
    }
  }
}
