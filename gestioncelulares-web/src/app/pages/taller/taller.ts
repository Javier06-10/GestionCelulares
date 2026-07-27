import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { Cliente } from '../../core/cliente.service';
import { FacturaConfigService } from '../../core/factura-config.service';
import { ImagenService } from '../../core/imagen.service';
import { SoundService } from '../../core/sound.service';
import { ComisionTecnico, Orden, OrdenResumen, TallerService } from '../../core/taller.service';
import { Usuario, UsuarioService } from '../../core/usuario.service';
import { MetodoPago, VentaService } from '../../core/venta.service';
import { BarcodeComponent } from '../../shared/barcode.component';
import { CelebracionComponent } from '../../shared/celebracion.component';
import { ClienteSelector } from '../../shared/cliente-selector/cliente-selector';
import { CountUpDirective } from '../../shared/count-up.directive';

interface Columna { estado: string; titulo: string; color: string; }

@Component({
  selector: 'app-taller',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe, ClienteSelector, CountUpDirective, BarcodeComponent, CelebracionComponent],
  templateUrl: './taller.html'
})
export class Taller {
  private fb = inject(FormBuilder);
  private servicio = inject(TallerService);
  private usuarios = inject(UsuarioService);
  private ventas = inject(VentaService);
  private sound = inject(SoundService);
  private facturaCfg = inject(FacturaConfigService);
  imagenes = inject(ImagenService);
  auth = inject(AuthService);

  // Datos del negocio para el encabezado del ticket
  cfg = this.facturaCfg.config;

  // Imprime el ticket de recepción de la orden en el detalle abierto
  imprimirTicket(): void {
    setTimeout(() => window.print(), 50);
  }

  numeroTicket(o: Orden): string {
    return o.numeroOrden || ('OT-' + String(o.ordenTallerId).padStart(6, '0'));
  }

  ordenes = signal<OrdenResumen[]>([]);
  cargando = signal(false);
  tecnicos = signal<Usuario[]>([]);
  metodos = signal<MetodoPago[]>([]);

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

  // KPIs operativos
  private activas = computed(() => this.ordenes().filter(o => o.estado === 'Recibido' || o.estado === 'EnReparacion' || o.estado === 'Reparado'));
  kpiActivas = computed(() => this.activas().length);
  kpiEnReparacion = computed(() => this.ordenes().filter(o => o.estado === 'EnReparacion').length);
  kpiListas = computed(() => this.ordenes().filter(o => o.estado === 'Reparado').length);
  valorActivo = computed(() => this.activas().reduce((a, o) => a + (o.costoEstimado || 0), 0));

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
    metodoPagoAnticipoId: [null as number | null],
    costoEstimado: [0, [Validators.min(0)]]
  });

  // Repuesto
  modalRepuesto = signal(false);
  formRepuesto = this.fb.nonNullable.group({
    descripcion: ['', Validators.required],
    cantidad: [1, [Validators.required, Validators.min(1)]],
    costo: [0, [Validators.min(0)]]
  });

  // Fotos del equipo (condición al recibir): hasta 3, subidas a Cloudinary
  readonly maxFotos = 3;
  subiendoFoto = signal(false);
  errorFoto = signal<string | null>(null);

  // Comisiones por técnico (solo Admin)
  modalComisiones = signal(false);
  comisiones = signal<ComisionTecnico[]>([]);
  cargandoComisiones = signal(false);
  desde = signal<string>('');
  hasta = signal<string>('');
  totalComisiones = computed(() => this.comisiones().reduce((a, c) => a + c.totalComision, 0));

  // Entrega
  modalEntrega = signal(false);
  celebrandoEntrega = signal(false);   // celebración "equipo entregado"
  formEntrega = this.fb.nonNullable.group({
    costoFinal: [0, [Validators.min(0)]],
    comisionTecnico: [0, [Validators.min(0)]],
    metodoPagoEntregaId: [null as number | null]
  });

  // Saldo a cobrar al entregar (costo final - anticipo de la orden en detalle)
  saldoEntrega(): number {
    const o = this.detalle();
    return o ? Math.max(0, (this.formEntrega.controls.costoFinal.value || 0) - o.anticipo) : 0;
  }

  constructor() {
    this.cargar();
    this.usuarios.listar(true).subscribe({
      next: u => this.tecnicos.set(u),
      error: () => this.tecnicos.set([]) // no Admin: sin lista de técnicos
    });
    this.ventas.metodosPago().subscribe({ next: m => this.metodos.set(m) });
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
    this.sound.playSlide();
    this.servicio.cambiarEstado(o.ordenTallerId, { estado }).subscribe({
      next: x => { this.detalle.set(x); this.cargar(); },
      error: e => {
        this.sound.playError();
        this.errorDetalle.set(e.error?.error ?? 'No se pudo cambiar el estado.');
      }
    });
  }

  // ----- Entrega -----
  abrirEntrega(): void {
    const o = this.detalle();
    this.formEntrega.reset({ costoFinal: o?.costoEstimado ?? 0, comisionTecnico: 0, metodoPagoEntregaId: null });
    this.modalEntrega.set(true);
  }
  confirmarEntrega(): void {
    const o = this.detalle();
    if (!o) return;
    const v = this.formEntrega.getRawValue();
    if (this.saldoEntrega() > 0 && !v.metodoPagoEntregaId) {
      this.sound.playError();
      this.errorDetalle.set('Indica el método de pago del saldo cobrado al entregar.');
      return;
    }
    this.servicio.cambiarEstado(o.ordenTallerId, {
      estado: 'Entregado', costoFinal: v.costoFinal, comisionTecnico: v.comisionTecnico,
      metodoPagoEntregaId: this.saldoEntrega() > 0 ? v.metodoPagoEntregaId : null
    }).subscribe({
      next: x => {
        this.sound.playSuccess();
        this.detalle.set(x);
        this.modalEntrega.set(false);
        this.cargar();
        this.celebrandoEntrega.set(true);
        setTimeout(() => this.celebrandoEntrega.set(false), 2400);
      },
      error: e => {
        this.sound.playError();
        this.errorDetalle.set(e.error?.error ?? 'No se pudo entregar.');
      }
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

  // ----- Fotos del equipo -----
  // Sube la foto a Cloudinary y la adjunta a la orden (máx. 3).
  subirFoto(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const archivo = input.files?.[0];
    const o = this.detalle();
    if (!archivo || !o) return;
    this.errorFoto.set(null);
    this.subiendoFoto.set(true);
    this.imagenes.subir(archivo).subscribe({
      next: url => {
        this.servicio.agregarFoto(o.ordenTallerId, url).subscribe({
          next: x => { this.detalle.set(x); this.subiendoFoto.set(false); input.value = ''; },
          error: e => { this.subiendoFoto.set(false); this.errorFoto.set(e.error?.error ?? 'No se pudo adjuntar la foto.'); input.value = ''; }
        });
      },
      error: err => {
        this.subiendoFoto.set(false);
        const msg = err?.error?.error?.message as string | undefined;
        this.errorFoto.set(msg ? `No se pudo subir: ${msg}` : 'No se pudo subir la foto.');
        input.value = '';
      }
    });
  }

  eliminarFoto(fotoId: number): void {
    const o = this.detalle();
    if (!o) return;
    this.errorFoto.set(null);
    this.servicio.eliminarFoto(o.ordenTallerId, fotoId).subscribe({
      next: x => this.detalle.set(x),
      error: e => this.errorFoto.set(e.error?.error ?? 'No se pudo eliminar la foto.')
    });
  }

  // ----- Nueva orden -----
  abrirNueva(): void {
    this.errorNueva.set(null);
    this.clienteSel.set(null);
    this.formNueva.reset({ equipoDescripcion: '', diagnostico: '', tecnicoId: null, anticipo: 0, metodoPagoAnticipoId: null, costoEstimado: 0 });
    this.modalNueva.set(true);
  }
  crearOrden(): void {
    if (this.formNueva.invalid) { this.formNueva.markAllAsTouched(); return; }
    const suc = this.auth.usuario()?.sucursalId;
    if (!suc) { this.errorNueva.set('Tu usuario no tiene sucursal asignada.'); return; }
    const v = this.formNueva.getRawValue();
    if (v.anticipo > 0 && !v.metodoPagoAnticipoId) {
      this.errorNueva.set('Indica el método de pago del anticipo.');
      return;
    }
    this.guardandoNueva.set(true);
    this.errorNueva.set(null);
    this.servicio.crear({
      sucursalId: suc,
      clienteId: this.clienteSel()?.clienteId ?? null,
      equipoDescripcion: v.equipoDescripcion.trim(),
      diagnostico: v.diagnostico?.trim() || null,
      tecnicoId: v.tecnicoId ? Number(v.tecnicoId) : null,
      anticipo: v.anticipo,
      metodoPagoAnticipoId: v.anticipo > 0 ? (v.metodoPagoAnticipoId ? Number(v.metodoPagoAnticipoId) : null) : null,
      costoEstimado: v.costoEstimado
    }).subscribe({
      next: o => { this.guardandoNueva.set(false); this.modalNueva.set(false); this.cargar(); this.detalle.set(o); },
      error: e => { this.guardandoNueva.set(false); this.errorNueva.set(e.error?.error ?? 'No se pudo crear la orden.'); }
    });
  }

  // ----- Comisiones -----
  abrirComisiones(): void {
    this.modalComisiones.set(true);
    this.cargarComisiones();
  }
  cargarComisiones(): void {
    this.cargandoComisiones.set(true);
    this.servicio.comisiones(this.desde() || null, this.hasta() || null).subscribe({
      next: c => { this.comisiones.set(c); this.cargandoComisiones.set(false); },
      error: () => { this.comisiones.set([]); this.cargandoComisiones.set(false); }
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

  onDragStart(event: DragEvent, id: number): void {
    if (event.dataTransfer) {
      event.dataTransfer.setData('text/plain', id.toString());
      event.dataTransfer.effectAllowed = 'move';
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  onDrop(event: DragEvent, nuevoEstado: string): void {
    event.preventDefault();
    const idStr = event.dataTransfer?.getData('text/plain');
    if (!idStr) return;

    const id = Number(idStr);
    const orden = this.ordenes().find(o => o.ordenTallerId === id);
    if (!orden) return;

    const estadoActual = orden.estado;
    if (estadoActual === nuevoEstado) return;

    const permitidos = this.transiciones[estadoActual] ?? [];
    if (!permitidos.includes(nuevoEstado)) {
      this.sound.playError();
      return;
    }

    if (nuevoEstado === 'Entregado') {
      this.servicio.porId(id).subscribe({
        next: o => {
          this.detalle.set(o);
          this.abrirEntrega();
        },
        error: () => this.sound.playError()
      });
    } else {
      this.sound.playSlide();
      this.servicio.cambiarEstado(id, { estado: nuevoEstado }).subscribe({
        next: () => this.cargar(),
        error: () => this.sound.playError()
      });
    }
  }
}
