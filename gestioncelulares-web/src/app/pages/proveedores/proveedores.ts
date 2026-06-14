import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { Compra, Pago, Proveedor, ProveedorService } from '../../core/proveedor.service';

@Component({
  selector: 'app-proveedores',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe],
  templateUrl: './proveedores.html'
})
export class Proveedores {
  private fb = inject(FormBuilder);
  private servicio = inject(ProveedorService);
  auth = inject(AuthService);

  proveedores = signal<Proveedor[]>([]);
  cargando = signal(false);
  termino = signal('');

  totalAdeudado = computed(() => this.proveedores().reduce((a, p) => a + p.balance, 0));

  // Alta / edición
  modal = signal(false);
  editando = signal<Proveedor | null>(null);
  guardando = signal(false);
  errorForm = signal<string | null>(null);
  form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    rnc: [''],
    telefono: [''],
    email: ['', Validators.email],
    direccion: [''],
    activo: [true]
  });

  // Detalle (compras y pagos)
  detalle = signal<Proveedor | null>(null);
  compras = signal<Compra[]>([]);
  pagos = signal<Pago[]>([]);
  tabDetalle = signal<'compras' | 'pagos'>('compras');

  // Modal compra
  modalCompra = signal(false);
  errorCompra = signal<string | null>(null);
  formCompra = this.fb.nonNullable.group({
    tipo: ['contado' as 'contado' | 'credito'],
    numeroFactura: [''],
    total: [0, [Validators.required, Validators.min(0.01)]],
    notas: ['']
  });

  // Modal pago
  modalPago = signal(false);
  errorPago = signal<string | null>(null);
  formPago = this.fb.nonNullable.group({
    monto: [0, [Validators.required, Validators.min(0.01)]],
    referencia: ['']
  });

  constructor() { this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.buscar(this.termino() || undefined).subscribe({
      next: d => { this.proveedores.set(d); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  buscar(v: string): void { this.termino.set(v); this.cargar(); }

  // ----- Alta / edición -----
  abrirNuevo(): void {
    this.editando.set(null);
    this.errorForm.set(null);
    this.form.reset({ nombre: '', rnc: '', telefono: '', email: '', direccion: '', activo: true });
    this.modal.set(true);
  }
  abrirEditar(p: Proveedor): void {
    this.editando.set(p);
    this.errorForm.set(null);
    this.form.reset({ nombre: p.nombre, rnc: p.rnc ?? '', telefono: p.telefono ?? '', email: p.email ?? '', direccion: p.direccion ?? '', activo: p.activo });
    this.modal.set(true);
  }
  guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();
    const dto = { nombre: v.nombre.trim(), rnc: v.rnc?.trim() || null, telefono: v.telefono?.trim() || null, email: v.email?.trim() || null, direccion: v.direccion?.trim() || null, activo: v.activo };
    const accion = this.editando() ? this.servicio.actualizar(this.editando()!.proveedorId, dto) : this.servicio.crear(dto);
    accion.subscribe({
      next: () => { this.guardando.set(false); this.modal.set(false); this.cargar(); },
      error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo guardar.'); }
    });
  }

  // ----- Detalle -----
  abrirDetalle(p: Proveedor): void {
    this.detalle.set(p);
    this.tabDetalle.set('compras');
    this.servicio.compras(p.proveedorId).subscribe(c => this.compras.set(c));
    this.servicio.pagos(p.proveedorId).subscribe(pg => this.pagos.set(pg));
  }
  cerrarDetalle(): void { this.detalle.set(null); }

  // ----- Compra -----
  abrirCompra(): void { this.errorCompra.set(null); this.formCompra.reset({ tipo: 'contado', numeroFactura: '', total: 0, notas: '' }); this.modalCompra.set(true); }
  registrarCompra(): void {
    const p = this.detalle();
    if (this.formCompra.invalid || !p) { this.formCompra.markAllAsTouched(); return; }
    const suc = this.auth.usuario()?.sucursalId;
    if (!suc) { this.errorCompra.set('Tu usuario no tiene sucursal asignada.'); return; }
    const v = this.formCompra.getRawValue();
    this.servicio.registrarCompra(p.proveedorId, { sucursalId: suc, numeroFactura: v.numeroFactura?.trim() || null, total: v.total, notas: v.notas?.trim() || null, contado: v.tipo === 'contado' }).subscribe({
      next: () => { this.modalCompra.set(false); this.refrescarDetalle(p.proveedorId); },
      error: err => this.errorCompra.set(err.error?.error ?? 'No se pudo registrar la compra.')
    });
  }

  // ----- Pago -----
  abrirPago(): void { this.errorPago.set(null); this.formPago.reset({ monto: this.detalle()?.balance ?? 0, referencia: '' }); this.modalPago.set(true); }
  registrarPago(): void {
    const p = this.detalle();
    if (this.formPago.invalid || !p) { this.formPago.markAllAsTouched(); return; }
    const v = this.formPago.getRawValue();
    this.servicio.registrarPago(p.proveedorId, { monto: v.monto, referencia: v.referencia?.trim() || null }).subscribe({
      next: () => { this.modalPago.set(false); this.refrescarDetalle(p.proveedorId); },
      error: err => this.errorPago.set(err.error?.error ?? 'No se pudo registrar el pago.')
    });
  }

  private refrescarDetalle(id: number): void {
    this.servicio.porId(id).subscribe(p => this.detalle.set(p));
    this.servicio.compras(id).subscribe(c => this.compras.set(c));
    this.servicio.pagos(id).subscribe(pg => this.pagos.set(pg));
    this.cargar();
  }
}
