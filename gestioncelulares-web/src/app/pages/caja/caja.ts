import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { CajaService, CierreResultado, MovimientoCaja, SesionCaja } from '../../core/caja.service';

@Component({
  selector: 'app-caja',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe],
  templateUrl: './caja.html'
})
export class Caja {
  private fb = inject(FormBuilder);
  private servicio = inject(CajaService);
  auth = inject(AuthService);

  cargando = signal(true);
  sesion = signal<SesionCaja | null>(null);
  movimientos = signal<MovimientoCaja[]>([]);

  // Saldo esperado en efectivo por movimientos manuales (las ventas se suman al cerrar)
  saldoMovimientos = computed(() => {
    const s = this.sesion();
    return s ? s.montoApertura + s.totalIngresos - s.totalEgresos : 0;
  });

  // Apertura
  abriendo = signal(false);
  errorApertura = signal<string | null>(null);
  formApertura = this.fb.nonNullable.group({
    montoApertura: [0, [Validators.required, Validators.min(0)]]
  });

  // Movimiento
  guardandoMov = signal(false);
  errorMov = signal<string | null>(null);
  formMov = this.fb.nonNullable.group({
    tipo: ['Ingreso', Validators.required],
    concepto: ['', [Validators.required, Validators.maxLength(150)]],
    monto: [0, [Validators.required, Validators.min(0.01)]],
    referencia: ['']
  });

  // Cierre
  modalCierre = signal(false);
  cerrando = signal(false);
  errorCierre = signal<string | null>(null);
  resultado = signal<CierreResultado | null>(null);
  formCierre = this.fb.nonNullable.group({
    montoContado: [0, [Validators.required, Validators.min(0)]]
  });

  private get sucursalId(): number | null {
    return this.auth.usuario()?.sucursalId ?? null;
  }

  constructor() {
    this.cargar();
  }

  cargar(): void {
    const suc = this.sucursalId;
    if (!suc) {
      this.cargando.set(false);
      return;
    }
    this.cargando.set(true);
    this.servicio.actual(suc).subscribe({
      next: s => {
        this.sesion.set(s);
        this.cargarMovimientos(s.sesionCajaId);
        this.cargando.set(false);
      },
      error: () => {
        this.sesion.set(null);
        this.cargando.set(false);
      }
    });
  }

  private cargarMovimientos(id: number): void {
    this.servicio.movimientos(id).subscribe({ next: m => this.movimientos.set(m) });
  }

  abrir(): void {
    if (this.formApertura.invalid || !this.sucursalId) return;
    this.abriendo.set(true);
    this.errorApertura.set(null);
    this.servicio.abrir(this.sucursalId, this.formApertura.getRawValue().montoApertura).subscribe({
      next: s => {
        this.abriendo.set(false);
        this.sesion.set(s);
        this.movimientos.set([]);
      },
      error: err => {
        this.abriendo.set(false);
        this.errorApertura.set(err.error?.error ?? 'No se pudo abrir la caja.');
      }
    });
  }

  registrarMovimiento(): void {
    const s = this.sesion();
    if (this.formMov.invalid || !s) {
      this.formMov.markAllAsTouched();
      return;
    }
    this.guardandoMov.set(true);
    this.errorMov.set(null);
    const v = this.formMov.getRawValue();
    this.servicio.registrarMovimiento(s.sesionCajaId, v.tipo, v.concepto.trim(), v.monto, v.referencia?.trim() || null).subscribe({
      next: () => {
        this.guardandoMov.set(false);
        this.formMov.reset({ tipo: v.tipo, concepto: '', monto: 0, referencia: '' });
        this.recargarSesion();
      },
      error: err => {
        this.guardandoMov.set(false);
        this.errorMov.set(err.error?.error ?? 'No se pudo registrar el movimiento.');
      }
    });
  }

  private recargarSesion(): void {
    const s = this.sesion();
    if (!s) return;
    this.servicio.actual(s.sucursalId).subscribe({ next: x => this.sesion.set(x) });
    this.cargarMovimientos(s.sesionCajaId);
  }

  abrirCierre(): void {
    this.resultado.set(null);
    this.errorCierre.set(null);
    this.formCierre.reset({ montoContado: 0 });
    this.modalCierre.set(true);
  }

  confirmarCierre(): void {
    const s = this.sesion();
    if (this.formCierre.invalid || !s) return;
    this.cerrando.set(true);
    this.errorCierre.set(null);
    this.servicio.cerrar(s.sesionCajaId, this.formCierre.getRawValue().montoContado).subscribe({
      next: r => {
        this.cerrando.set(false);
        this.resultado.set(r);
      },
      error: err => {
        this.cerrando.set(false);
        this.errorCierre.set(err.error?.error ?? 'No se pudo cerrar la caja.');
      }
    });
  }

  terminarCierre(): void {
    this.modalCierre.set(false);
    this.sesion.set(null);
    this.movimientos.set([]);
    this.cargar();
  }
}
