import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { CajaService } from '../../core/caja.service';
import { Cliente } from '../../core/cliente.service';
import {
  Credito, CreditoResumen, CreditoService, CuotaVencida
} from '../../core/credito.service';
import { MetodoPago, VentaService } from '../../core/venta.service';
import { ClienteSelector } from '../../shared/cliente-selector/cliente-selector';
import { CountUpDirective } from '../../shared/count-up.directive';

type Vista = 'creditos' | 'vencidas';

@Component({
  selector: 'app-creditos',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe, ClienteSelector, CountUpDirective],
  templateUrl: './creditos.html'
})
export class Creditos {
  private fb = inject(FormBuilder);
  private servicio = inject(CreditoService);
  private ventas = inject(VentaService);
  private caja = inject(CajaService);
  auth = inject(AuthService);

  vista = signal<Vista>('creditos');
  cargando = signal(false);
  creditos = signal<CreditoResumen[]>([]);
  vencidas = signal<CuotaVencida[]>([]);
  estado = signal<string>('');

  metodos = signal<MetodoPago[]>([]);

  // Detalle
  detalle = signal<Credito | null>(null);
  cargandoDetalle = signal(false);

  // Abono
  modalAbono = signal(false);
  guardandoAbono = signal(false);
  errorAbono = signal<string | null>(null);
  resultadoAbono = signal<string | null>(null);
  formAbono = this.fb.nonNullable.group({
    monto: [0, [Validators.required, Validators.min(0.01)]],
    metodoPagoId: [null as number | null],
    numeroRecibo: ['']
  });

  // Nuevo crédito
  modalNuevo = signal(false);
  guardandoNuevo = signal(false);
  errorNuevo = signal<string | null>(null);
  clienteSel = signal<Cliente | null>(null);
  formNuevo = this.fb.nonNullable.group({
    montoFinanciado: [0, [Validators.required, Validators.min(0.01)]],
    inicial: [0, [Validators.min(0)]],
    numeroCuotas: [6, [Validators.required, Validators.min(1), Validators.max(120)]],
    tasaInteresMensual: [0, [Validators.min(0), Validators.max(100)]],
    fechaPrimerVencimiento: ['', Validators.required]
  });

  // Mora
  corriendoMora = signal(false);

  // Vista filtrada por estado (en cliente, instantáneo)
  creditosVista = computed<CreditoResumen[]>(() => {
    const e = this.estado();
    return e ? this.creditos().filter(c => c.estado === e) : this.creditos();
  });

  totalCartera = computed(() => this.creditos().reduce((a, c) => a + c.saldo, 0));
  totalVencido = computed(() => this.vencidas().reduce((a, c) => a + c.saldo, 0));

  // KPIs (sobre el total)
  kpiActivos = computed(() => this.creditos().filter(c => c.estado === 'Activo').length);
  kpiMora = computed(() => this.creditos().filter(c => c.estado === 'EnMora').length);

  // Progreso de pago de un crédito (% saldado)
  progreso(montoTotal: number, saldo: number): number {
    if (montoTotal <= 0) return 0;
    return Math.min(100, Math.max(0, Math.round(((montoTotal - saldo) / montoTotal) * 100)));
  }

  // Estimación en vivo para el nuevo crédito
  get netoFinanciar(): number {
    const v = this.formNuevo.getRawValue();
    return Math.max(0, (+v.montoFinanciado || 0) - (+v.inicial || 0));
  }
  get cuotaEstimada(): number {
    const v = this.formNuevo.getRawValue();
    const n = +v.numeroCuotas || 0;
    if (n <= 0) return 0;
    const total = this.netoFinanciar * (1 + ((+v.tasaInteresMensual || 0) / 100) * n);
    return total / n;
  }

  constructor() {
    this.cargar();
    this.ventas.metodosPago().subscribe(m => {
      this.metodos.set(m);
      const efectivo = m.find(x => x.nombre.toLowerCase() === 'efectivo') ?? m[0];
      if (efectivo) this.formAbono.controls.metodoPagoId.setValue(efectivo.metodoPagoId);
    });
  }

  cambiarVista(v: Vista): void {
    this.vista.set(v);
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.buscar().subscribe({
      next: d => { this.creditos.set(d); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
    this.cargarVencidas();
  }

  cargarVencidas(): void {
    this.servicio.cuotasVencidas().subscribe({ next: d => this.vencidas.set(d) });
  }

  filtrarEstado(e: string): void {
    this.estado.set(e);
  }

  // ----- Detalle -----
  abrirDetalle(creditoId: number): void {
    this.cargandoDetalle.set(true);
    this.detalle.set(null);
    this.servicio.porId(creditoId).subscribe({
      next: c => { this.detalle.set(c); this.cargandoDetalle.set(false); },
      error: () => this.cargandoDetalle.set(false)
    });
  }

  cerrarDetalle(): void { this.detalle.set(null); }

  // ----- Abono -----
  abrirAbono(): void {
    this.errorAbono.set(null);
    this.resultadoAbono.set(null);
    const c = this.detalle();
    this.formAbono.patchValue({ monto: c ? c.saldo : 0, numeroRecibo: '' });
    this.modalAbono.set(true);
  }

  registrarAbono(): void {
    const c = this.detalle();
    if (this.formAbono.invalid || !c) { this.formAbono.markAllAsTouched(); return; }
    this.guardandoAbono.set(true);
    this.errorAbono.set(null);
    const v = this.formAbono.getRawValue();
    const suc = this.auth.usuario()?.sucursalId;

    // Si hay caja abierta, vincular el abono para que cuente en el arqueo
    const registrar = (sesionCajaId: number | null) => {
      this.servicio.registrarAbono(c.creditoId, {
        monto: v.monto,
        metodoPagoId: v.metodoPagoId,
        sesionCajaId,
        numeroRecibo: v.numeroRecibo?.trim() || null
      }).subscribe({
        next: r => {
          this.guardandoAbono.set(false);
          this.resultadoAbono.set(`Abono aplicado. Saldo: ${r.saldoCredito.toLocaleString('es-DO', { style: 'currency', currency: 'DOP' })} · ${r.estadoCredito}`);
          this.abrirDetalle(c.creditoId);
          this.cargar();
        },
        error: err => { this.guardandoAbono.set(false); this.errorAbono.set(err.error?.error ?? 'No se pudo registrar el abono.'); }
      });
    };

    if (suc) {
      this.caja.actual(suc).subscribe({
        next: s => registrar(s.sesionCajaId),
        error: () => registrar(null)
      });
    } else {
      registrar(null);
    }
  }

  // ----- Nuevo crédito -----
  abrirNuevo(): void {
    this.errorNuevo.set(null);
    this.clienteSel.set(null);
    const hoy = new Date();
    hoy.setMonth(hoy.getMonth() + 1);
    this.formNuevo.reset({
      montoFinanciado: 0, inicial: 0, numeroCuotas: 6, tasaInteresMensual: 0,
      fechaPrimerVencimiento: hoy.toISOString().slice(0, 10)
    });
    this.modalNuevo.set(true);
  }

  crearCredito(): void {
    if (this.formNuevo.invalid || !this.clienteSel()) {
      this.formNuevo.markAllAsTouched();
      if (!this.clienteSel()) this.errorNuevo.set('Selecciona un cliente.');
      return;
    }
    this.guardandoNuevo.set(true);
    this.errorNuevo.set(null);
    const v = this.formNuevo.getRawValue();
    this.servicio.crear({
      clienteId: this.clienteSel()!.clienteId,
      montoFinanciado: v.montoFinanciado,
      inicial: v.inicial,
      numeroCuotas: v.numeroCuotas,
      tasaInteresMensual: v.tasaInteresMensual,
      fechaPrimerVencimiento: v.fechaPrimerVencimiento
    }).subscribe({
      next: c => {
        this.guardandoNuevo.set(false);
        this.modalNuevo.set(false);
        this.cargar();
        this.abrirDetalle(c.creditoId);
      },
      error: err => { this.guardandoNuevo.set(false); this.errorNuevo.set(err.error?.error ?? 'No se pudo crear el crédito.'); }
    });
  }

  // ----- Mora -----
  correrMora(): void {
    this.corriendoMora.set(true);
    this.servicio.actualizarMora().subscribe({
      next: () => { this.corriendoMora.set(false); this.cargar(); },
      error: () => this.corriendoMora.set(false)
    });
  }

  estadoClase(e: string): string {
    switch (e) {
      case 'Activo': return 'bg-tech-accent/10 text-tech-accent';
      case 'EnMora': return 'bg-red-500/10 text-red-500';
      case 'Saldado': return 'bg-slate-400/15 text-slate-500';
      default: return 'bg-slate-400/15 text-slate-500';
    }
  }

  estadoCuotaClase(e: string): string {
    switch (e) {
      case 'Pagada': return 'bg-tech-accent/10 text-tech-accent';
      case 'Parcial': return 'bg-tech-sand/30 text-amber-700';
      case 'Vencida': return 'bg-red-500/10 text-red-500';
      default: return 'bg-slate-400/15 text-slate-500';
    }
  }
}
