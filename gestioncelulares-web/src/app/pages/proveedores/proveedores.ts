import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { Compra, CondicionPago, ContactoProveedor, Pago, Proveedor, ProveedorService } from '../../core/proveedor.service';
import { MetodoPago, VentaService } from '../../core/venta.service';
import { CountUpDirective } from '../../shared/count-up.directive';

@Component({
  selector: 'app-proveedores',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe, CountUpDirective],
  templateUrl: './proveedores.html'
})
export class Proveedores {
  private fb = inject(FormBuilder);
  private servicio = inject(ProveedorService);
  private ventas = inject(VentaService);
  auth = inject(AuthService);

  metodos = signal<MetodoPago[]>([]);
  proveedores = signal<Proveedor[]>([]);
  cargando = signal(false);
  termino = signal('');

  totalAdeudado = computed(() => this.proveedores().reduce((a, p) => a + p.balance, 0));
  conDeuda = computed(() => this.proveedores().filter(p => p.balance > 0).length);
  activos = computed(() => this.proveedores().filter(p => p.activo).length);

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
    activo: [true],
    condicionPago: ['Contado' as CondicionPago],
    diasCredito: [0, [Validators.min(0), Validators.max(365)]],
    notaCondicion: ['']
  });

  // Contactos del proveedor (varios)
  contactos = signal<ContactoProveedor[]>([]);
  modalContacto = signal(false);
  editandoContacto = signal<ContactoProveedor | null>(null);
  errorContacto = signal<string | null>(null);
  formContacto = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    cargo: [''],
    telefono: [''],
    email: ['', Validators.email],
    esPrincipal: [false]
  });

  private cargarContactos(proveedorId: number): void {
    this.servicio.contactos(proveedorId).subscribe(c => this.contactos.set(c));
  }
  abrirNuevoContacto(): void {
    this.editandoContacto.set(null);
    this.errorContacto.set(null);
    this.formContacto.reset({ nombre: '', cargo: '', telefono: '', email: '', esPrincipal: this.contactos().length === 0 });
    this.modalContacto.set(true);
  }
  abrirEditarContacto(c: ContactoProveedor): void {
    this.editandoContacto.set(c);
    this.errorContacto.set(null);
    this.formContacto.reset({ nombre: c.nombre, cargo: c.cargo ?? '', telefono: c.telefono ?? '', email: c.email ?? '', esPrincipal: c.esPrincipal });
    this.modalContacto.set(true);
  }
  guardarContacto(): void {
    const p = this.detalle();
    if (this.formContacto.invalid || !p) { this.formContacto.markAllAsTouched(); return; }
    const v = this.formContacto.getRawValue();
    const dto = { nombre: v.nombre.trim(), cargo: v.cargo?.trim() || null, telefono: v.telefono?.trim() || null, email: v.email?.trim() || null, esPrincipal: v.esPrincipal };
    const accion = this.editandoContacto()
      ? this.servicio.actualizarContacto(this.editandoContacto()!.contactoProveedorId, dto)
      : this.servicio.agregarContacto(p.proveedorId, dto);
    accion.subscribe({
      next: () => { this.modalContacto.set(false); this.cargarContactos(p.proveedorId); },
      error: err => this.errorContacto.set(err.error?.error ?? 'No se pudo guardar el contacto.')
    });
  }
  eliminarContacto(c: ContactoProveedor): void {
    const p = this.detalle();
    if (!p) return;
    this.servicio.eliminarContacto(c.contactoProveedorId).subscribe({
      next: () => this.cargarContactos(p.proveedorId)
    });
  }

  /** Etiqueta legible de la condición de pago de un proveedor. */
  condicionLabel(p: Proveedor): string {
    if (p.condicionPago === 'Credito') return `Crédito ${p.diasCredito} día(s)`;
    if (p.condicionPago === 'Acuerdo') return 'Acuerdo de pago';
    return 'Contado';
  }
  condicionClase(c: CondicionPago): string {
    return c === 'Credito' ? 'bg-tech-purple/10 text-tech-purple'
      : c === 'Acuerdo' ? 'bg-amber-500/10 text-amber-600'
      : 'bg-tech-accent/10 text-tech-accent';
  }

  // Detalle: dos apartados — Información (datos) y Cuentas por Pagar (financiero)
  detalle = signal<Proveedor | null>(null);
  apartado = signal<'info' | 'cuentas'>('cuentas');
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
    itbis: [0, [Validators.min(0)]],
    tipoBienServicio: ['09'],
    metodoPagoId: [null as number | null],
    notas: ['']
  });

  // Tipos de bien/servicio DGII (formato 606)
  tiposBienServicio = [
    { v: '09', n: '09 - Compras del costo de venta' },
    { v: '02', n: '02 - Trabajos/suministros/servicios' },
    { v: '03', n: '03 - Arrendamientos' },
    { v: '04', n: '04 - Gastos de activos fijos' },
    { v: '06', n: '06 - Otras deducciones admitidas' },
    { v: '07', n: '07 - Gastos financieros' },
    { v: '10', n: '10 - Adquisiciones de activos' },
    { v: '11', n: '11 - Gastos de seguros' }
  ];

  // Modal pago
  modalPago = signal(false);
  errorPago = signal<string | null>(null);
  formPago = this.fb.nonNullable.group({
    monto: [0, [Validators.required, Validators.min(0.01)]],
    referencia: ['']
  });

  constructor() {
    this.cargar();
    this.ventas.metodosPago().subscribe({ next: m => this.metodos.set(m) });
  }

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
    this.form.reset({ nombre: '', rnc: '', telefono: '', email: '', direccion: '', activo: true, condicionPago: 'Contado', diasCredito: 0, notaCondicion: '' });
    this.modal.set(true);
  }
  abrirEditar(p: Proveedor): void {
    this.editando.set(p);
    this.errorForm.set(null);
    this.form.reset({ nombre: p.nombre, rnc: p.rnc ?? '', telefono: p.telefono ?? '', email: p.email ?? '', direccion: p.direccion ?? '', activo: p.activo, condicionPago: p.condicionPago, diasCredito: p.diasCredito, notaCondicion: p.notaCondicion ?? '' });
    this.modal.set(true);
  }
  guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();
    const dto = {
      nombre: v.nombre.trim(), rnc: v.rnc?.trim() || null, telefono: v.telefono?.trim() || null,
      email: v.email?.trim() || null, direccion: v.direccion?.trim() || null, activo: v.activo,
      condicionPago: v.condicionPago,
      diasCredito: v.condicionPago === 'Credito' ? v.diasCredito : 0,
      notaCondicion: v.condicionPago === 'Acuerdo' ? (v.notaCondicion?.trim() || null) : null
    };
    const accion = this.editando() ? this.servicio.actualizar(this.editando()!.proveedorId, dto) : this.servicio.crear(dto);
    accion.subscribe({
      next: () => { this.guardando.set(false); this.modal.set(false); this.cargar(); },
      error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo guardar.'); }
    });
  }

  // ----- Detalle -----
  abrirDetalle(p: Proveedor): void {
    this.detalle.set(p);
    this.apartado.set('cuentas');
    this.tabDetalle.set('compras');
    this.servicio.compras(p.proveedorId).subscribe(c => this.compras.set(c));
    this.servicio.pagos(p.proveedorId).subscribe(pg => this.pagos.set(pg));
    this.cargarContactos(p.proveedorId);
  }
  cerrarDetalle(): void { this.detalle.set(null); }

  // ----- Compra -----
  abrirCompra(): void {
    this.errorCompra.set(null);
    // Preselecciona el tipo según la condición de pago acordada con el proveedor
    const tipo: 'contado' | 'credito' = this.detalle()?.condicionPago === 'Contado' ? 'contado' : 'credito';
    this.formCompra.reset({ tipo, numeroFactura: '', total: 0, itbis: 0, tipoBienServicio: '09', metodoPagoId: null, notas: '' });
    this.modalCompra.set(true);
  }
  registrarCompra(): void {
    const p = this.detalle();
    if (this.formCompra.invalid || !p) { this.formCompra.markAllAsTouched(); return; }
    const suc = this.auth.usuario()?.sucursalId;
    if (!suc) { this.errorCompra.set('Tu usuario no tiene sucursal asignada.'); return; }
    const v = this.formCompra.getRawValue();
    const contado = v.tipo === 'contado';
    this.servicio.registrarCompra(p.proveedorId, { sucursalId: suc, numeroFactura: v.numeroFactura?.trim() || null, total: v.total, itbis: v.itbis, tipoBienServicio: v.tipoBienServicio, metodoPagoId: contado ? (v.metodoPagoId ? Number(v.metodoPagoId) : null) : null, notas: v.notas?.trim() || null, contado }).subscribe({
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
