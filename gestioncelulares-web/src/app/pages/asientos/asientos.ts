import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Asiento, AsientoResumen, AsientoService, BalanceComprobacion } from '../../core/asiento.service';
import { Cuenta, CuentaService } from '../../core/cuenta.service';

@Component({
  selector: 'app-asientos',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe],
  templateUrl: './asientos.html'
})
export class Asientos {
  private fb = inject(FormBuilder);
  private servicio = inject(AsientoService);
  private cuentasSrv = inject(CuentaService);

  tab = signal<'diario' | 'balance'>('diario');

  cuentas = signal<Cuenta[]>([]);          // solo cuentas de detalle
  asientos = signal<AsientoResumen[]>([]);
  cargando = signal(false);
  desde = signal(this.inicioMes());
  hasta = signal(this.hoy());

  // Detalle de un asiento
  detalle = signal<Asiento | null>(null);

  // Balance de comprobación
  balance = signal<BalanceComprobacion | null>(null);

  // Modal nuevo asiento
  modal = signal(false);
  guardando = signal(false);
  errorForm = signal<string | null>(null);
  form = this.fb.nonNullable.group({
    fecha: [this.hoy(), Validators.required],
    concepto: ['', [Validators.required, Validators.maxLength(300)]],
    detalles: this.fb.array([this.nuevaLinea(), this.nuevaLinea()])
  });

  get lineas(): FormArray { return this.form.get('detalles') as FormArray; }

  private nuevaLinea() {
    return this.fb.nonNullable.group({
      cuentaContableId: [null as number | null, Validators.required],
      debito: [0, [Validators.min(0)]],
      credito: [0, [Validators.min(0)]],
      descripcion: ['']
    });
  }

  private hoy(): string { return new Date().toISOString().slice(0, 10); }
  private inicioMes(): string { const d = new Date(); return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10); }

  constructor() {
    this.cuentasSrv.listar().subscribe(c => this.cuentas.set(c.filter(x => x.permiteMovimiento && x.activo)));
    this.cargar();
  }

  cambiarTab(t: 'diario' | 'balance'): void {
    this.tab.set(t);
    if (t === 'diario') this.cargar(); else this.cargarBalance();
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar(this.desde(), this.hasta()).subscribe({
      next: a => { this.asientos.set(a); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  cargarBalance(): void {
    this.cargando.set(true);
    this.servicio.balanceComprobacion(this.desde(), this.hasta()).subscribe({
      next: b => { this.balance.set(b); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  generar(): void {
    if (this.tab() === 'diario') this.cargar(); else this.cargarBalance();
  }

  verDetalle(a: AsientoResumen): void {
    this.servicio.porId(a.asientoContableId).subscribe(d => this.detalle.set(d));
  }
  cerrarDetalle(): void { this.detalle.set(null); }

  anular(a: AsientoResumen): void {
    if (a.estado === 'Anulado') return;
    if (!confirm(`¿Anular el asiento #${a.numero}?`)) return;
    this.servicio.anular(a.asientoContableId).subscribe({ next: () => this.cargar() });
  }

  // ----- Nuevo asiento -----
  abrirNuevo(): void {
    this.errorForm.set(null);
    this.lineas.clear();
    this.lineas.push(this.nuevaLinea());
    this.lineas.push(this.nuevaLinea());
    this.form.patchValue({ fecha: this.hoy(), concepto: '' });
    this.modal.set(true);
  }

  agregarLinea(): void { this.lineas.push(this.nuevaLinea()); }
  quitarLinea(i: number): void { if (this.lineas.length > 2) this.lineas.removeAt(i); }

  totalDebito(): number { return this.lineas.controls.reduce((s, l) => s + (+l.get('debito')!.value || 0), 0); }
  totalCredito(): number { return this.lineas.controls.reduce((s, l) => s + (+l.get('credito')!.value || 0), 0); }
  diferencia(): number { return Math.round((this.totalDebito() - this.totalCredito()) * 100) / 100; }

  guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    if (this.diferencia() !== 0) { this.errorForm.set('El asiento no cuadra: débitos ≠ créditos.'); return; }
    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();
    this.servicio.crear({
      fecha: v.fecha,
      concepto: v.concepto.trim(),
      detalles: v.detalles.map(d => ({
        cuentaContableId: Number(d.cuentaContableId),
        debito: +d.debito || 0,
        credito: +d.credito || 0,
        descripcion: d.descripcion?.trim() || null
      }))
    }).subscribe({
      next: () => { this.guardando.set(false); this.modal.set(false); this.cargar(); },
      error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo registrar el asiento.'); }
    });
  }
}
