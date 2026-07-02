import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { Cliente, ClienteService } from '../../core/cliente.service';
import { CreditoResumen, CreditoService } from '../../core/credito.service';
import { OrdenResumen, TallerService } from '../../core/taller.service';
import { VentaResumen, VentaService } from '../../core/venta.service';
import { CountUpDirective } from '../../shared/count-up.directive';
import { PhoneLoaderComponent } from '../../shared/phone-loader.component';

type Filtro = 'todos' | 'aldia' | 'morosos' | 'bloqueados';

interface Nivel { label: string; clase: string; icono: string; }

@Component({
  selector: 'app-clientes',
  imports: [ReactiveFormsModule, LucideAngularModule, DatePipe, CurrencyPipe, RouterLink, CountUpDirective, PhoneLoaderComponent],
  templateUrl: './clientes.html'
})
export class Clientes {
  private fb = inject(FormBuilder);
  private servicio = inject(ClienteService);
  private creditoSrv = inject(CreditoService);
  private ventaSrv = inject(VentaService);
  private tallerSrv = inject(TallerService);
  auth = inject(AuthService);

  clientes = signal<Cliente[]>([]);
  cargando = signal(false);
  termino = signal('');
  filtro = signal<Filtro>('todos');

  // Filtrado en cliente: búsqueda instantánea + filtro por estado
  clientesVista = computed<Cliente[]>(() => {
    const q = this.termino().toLowerCase().trim();
    const f = this.filtro();
    return this.clientes().filter(c => {
      if (f === 'morosos' && !c.esMoroso) return false;
      if (f === 'bloqueados' && !c.bloqueado) return false;
      if (f === 'aldia' && (c.esMoroso || c.bloqueado)) return false;
      if (!q) return true;
      return `${c.nombre} ${c.cedula ?? ''} ${c.telefono ?? ''} ${c.email ?? ''}`.toLowerCase().includes(q);
    });
  });

  // KPIs (sobre el total, no la vista filtrada)
  kpiTotal = computed(() => this.clientes().length);
  kpiAlDia = computed(() => this.clientes().filter(c => !c.esMoroso && !c.bloqueado).length);
  kpiMorosos = computed(() => this.clientes().filter(c => c.esMoroso).length);
  kpiBloqueados = computed(() => this.clientes().filter(c => c.bloqueado).length);

  // Estado del modal
  modalAbierto = signal(false);
  editando = signal<Cliente | null>(null);
  guardando = signal(false);
  errorForm = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    cedula: ['', Validators.maxLength(20)],
    telefono: ['', Validators.maxLength(30)],
    email: ['', [Validators.email, Validators.maxLength(150)]],
    direccion: ['', Validators.maxLength(250)]
  });

  tituloModal = computed(() => (this.editando() ? 'Editar cliente' : 'Nuevo cliente'));

  // ----- Perfil (panel lateral) -----
  clienteSel = signal<Cliente | null>(null);
  cargandoPerfil = signal(false);
  creditosCli = signal<CreditoResumen[]>([]);
  comprasCli = signal<VentaResumen[]>([]);
  reparacionesCli = signal<OrdenResumen[]>([]);

  estadoTallerClase(e: string): string {
    switch (e) {
      case 'EnReparacion': return 'text-tech-purple';
      case 'Reparado': return 'text-tech-accent';
      case 'Cancelado': return 'text-red-500';
      case 'Entregado': return 'text-tech-steel';
      default: return 'text-amber-600';
    }
  }

  // Métricas derivadas del cliente seleccionado (datos reales)
  totalComprado = computed(() => this.comprasCli().filter(v => v.estado !== 'Anulada').reduce((a, v) => a + v.total, 0));
  saldoCredito = computed(() => this.creditosCli().filter(c => c.estado !== 'Saldado').reduce((a, c) => a + c.saldo, 0));
  creditosActivos = computed(() => this.creditosCli().filter(c => c.estado !== 'Saldado'));

  // Nivel del cliente derivado del total comprado (no es un programa de lealtad real)
  nivel = computed<Nivel>(() => {
    const t = this.totalComprado();
    if (t >= 100000) return { label: 'Oro', clase: 'bg-amber-500/10 text-amber-600', icono: 'medal' };
    if (t >= 25000) return { label: 'Plata', clase: 'bg-slate-400/15 text-slate-500', icono: 'medal' };
    return { label: 'Bronce', clase: 'bg-orange-500/10 text-orange-600', icono: 'medal' };
  });

  verPerfil(c: Cliente): void {
    this.clienteSel.set(c);
    this.cargandoPerfil.set(true);
    this.creditosCli.set([]);
    this.comprasCli.set([]);
    this.reparacionesCli.set([]);
    this.creditoSrv.buscar(c.clienteId).subscribe({ next: d => this.creditosCli.set(d) });
    this.tallerSrv.buscar(undefined, undefined, undefined, c.clienteId).subscribe({ next: r => this.reparacionesCli.set(r) });
    this.ventaSrv.buscar(undefined, undefined, undefined, c.clienteId).subscribe({
      next: v => { this.comprasCli.set(v); this.cargandoPerfil.set(false); },
      error: () => this.cargandoPerfil.set(false)
    });
  }
  cerrarPerfil(): void { this.clienteSel.set(null); }

  progreso(montoTotal: number, saldo: number): number {
    if (montoTotal <= 0) return 0;
    return Math.min(100, Math.max(0, Math.round(((montoTotal - saldo) / montoTotal) * 100)));
  }

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.buscar().subscribe({
      next: data => {
        this.clientes.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  buscar(valor: string): void {
    this.termino.set(valor);
  }

  cambiarFiltro(f: Filtro): void {
    this.filtro.set(f);
  }

  abrirNuevo(): void {
    this.editando.set(null);
    this.errorForm.set(null);
    this.form.reset({ nombre: '', cedula: '', telefono: '', email: '', direccion: '' });
    this.modalAbierto.set(true);
  }

  abrirEditar(c: Cliente): void {
    this.editando.set(c);
    this.errorForm.set(null);
    this.form.reset({
      nombre: c.nombre,
      cedula: c.cedula ?? '',
      telefono: c.telefono ?? '',
      email: c.email ?? '',
      direccion: c.direccion ?? ''
    });
    this.modalAbierto.set(true);
  }

  cerrarModal(): void {
    this.modalAbierto.set(false);
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.guardando.set(true);
    this.errorForm.set(null);

    const v = this.form.getRawValue();
    const dto = {
      nombre: v.nombre.trim(),
      cedula: v.cedula?.trim() || null,
      telefono: v.telefono?.trim() || null,
      email: v.email?.trim() || null,
      direccion: v.direccion?.trim() || null
    };

    const accion = this.editando()
      ? this.servicio.actualizar(this.editando()!.clienteId, dto)
      : this.servicio.crear(dto);

    accion.subscribe({
      next: () => {
        this.guardando.set(false);
        this.modalAbierto.set(false);
        this.cargar();
      },
      error: err => {
        this.guardando.set(false);
        this.errorForm.set(err.error?.error ?? 'No se pudo guardar el cliente.');
      }
    });
  }

  alternarBloqueo(c: Cliente): void {
    const accion = c.bloqueado ? this.servicio.desbloquear(c.clienteId) : this.servicio.bloquear(c.clienteId);
    accion.subscribe({ next: () => this.cargar() });
  }
}
