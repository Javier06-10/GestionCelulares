import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { CatalogoService, Producto, Variante } from '../../core/catalogo.service';
import { Imei, InventarioService, StockDisponible } from '../../core/inventario.service';

interface OpcionSerializada { varianteId: number; etiqueta: string; }
interface AccesorioFila { producto: Producto; variante: Variante; nombre: string; detalle: string; }

@Component({
  selector: 'app-inventario',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DecimalPipe],
  templateUrl: './inventario.html'
})
export class Inventario {
  private fb = inject(FormBuilder);
  private servicio = inject(InventarioService);
  private catalogo = inject(CatalogoService);
  auth = inject(AuthService);

  stock = signal<StockDisponible[]>([]);
  cargando = signal(false);
  productos = signal<Producto[]>([]);

  // Solo dispositivos serializados, para registrar IMEI
  variantesSerializadas = computed<OpcionSerializada[]>(() =>
    this.productos()
      .filter(p => p.serializado && p.activo)
      .flatMap(p => p.variantes.filter(v => v.activo).map(v => ({
        varianteId: v.varianteId,
        etiqueta: [p.nombre, v.color, v.almacenamiento, v.condicion].filter(Boolean).join(' · ')
      })))
  );

  // Accesorios (no serializados) con su stock por cantidad
  accesorios = computed<AccesorioFila[]>(() =>
    this.productos()
      .filter(p => !p.serializado && p.activo)
      .flatMap(p => p.variantes.filter(v => v.activo).map(v => ({
        producto: p,
        variante: v,
        nombre: p.nombre,
        detalle: [v.color, v.almacenamiento].filter(Boolean).join(' · ')
      })))
  );

  totalEquipos = computed(() => this.stock().reduce((a, s) => a + s.disponibles, 0));
  valorVenta = computed(() => this.stock().reduce((a, s) => a + s.precioVenta * s.disponibles, 0));

  // Búsqueda por IMEI
  imeiBuscado = signal<Imei | null>(null);
  imeiNoEncontrado = signal(false);

  // Opciones de accesorios para el selector (variantes no serializadas)
  accesoriosOpciones = computed(() =>
    this.accesorios().map(a => ({
      varianteId: a.variante.varianteId,
      etiqueta: a.detalle ? `${a.nombre} · ${a.detalle}` : a.nombre,
      stockActual: a.variante.stockNoSerial
    }))
  );

  // Modal de alta al inventario (dispositivo o accesorio)
  modalAbierto = signal(false);
  tipoAlta = signal<'dispositivo' | 'accesorio'>('dispositivo');
  guardando = signal(false);
  errorForm = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    imei: ['', [Validators.required, Validators.maxLength(20)]],
    varianteId: [0, [Validators.required, Validators.min(1)]],
    precioCosto: [0, [Validators.required, Validators.min(0)]]
  });

  formAcc = this.fb.nonNullable.group({
    varianteId: [0, [Validators.required, Validators.min(1)]],
    cantidad: [1, [Validators.required, Validators.min(1)]]
  });

  // Modal de ajuste de stock (accesorios, solo Admin)
  modalStock = signal(false);
  accesorioSel = signal<AccesorioFila | null>(null);
  guardandoStock = signal(false);
  errorStock = signal<string | null>(null);
  nuevoStock = signal(0);

  constructor() {
    this.cargar();
    this.cargarCatalogo();
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.disponibles().subscribe({
      next: data => { this.stock.set(data); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  cargarCatalogo(): void {
    this.catalogo.productos().subscribe(p => this.productos.set(p));
  }

  buscarImei(valor: string): void {
    const imei = valor.trim();
    this.imeiNoEncontrado.set(false);
    this.imeiBuscado.set(null);
    if (imei.length < 4) return;
    this.servicio.porImei(imei).subscribe({
      next: dto => this.imeiBuscado.set(dto),
      error: () => this.imeiNoEncontrado.set(true)
    });
  }

  // ----- Añadir al inventario -----
  abrirRegistro(): void {
    this.errorForm.set(null);
    this.tipoAlta.set('dispositivo');
    this.form.reset({ imei: '', varianteId: 0, precioCosto: 0 });
    this.formAcc.reset({ varianteId: 0, cantidad: 1 });
    this.modalAbierto.set(true);
  }

  cerrarModal(): void { this.modalAbierto.set(false); }

  cambiarTipoAlta(t: 'dispositivo' | 'accesorio'): void {
    this.tipoAlta.set(t);
    this.errorForm.set(null);
  }

  registrar(): void {
    if (this.tipoAlta() === 'dispositivo') this.registrarDispositivo();
    else this.agregarAccesorio();
  }

  private registrarDispositivo(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const sucursalId = this.auth.usuario()?.sucursalId;
    if (!sucursalId) { this.errorForm.set('Tu usuario no tiene una sucursal asignada.'); return; }

    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();
    this.servicio.registrar({ imei: v.imei.trim(), varianteId: v.varianteId, sucursalId, precioCosto: v.precioCosto }).subscribe({
      next: () => { this.guardando.set(false); this.modalAbierto.set(false); this.cargar(); },
      error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo registrar el equipo.'); }
    });
  }

  private agregarAccesorio(): void {
    if (this.formAcc.invalid) { this.formAcc.markAllAsTouched(); return; }
    const v = this.formAcc.getRawValue();
    const fila = this.accesorios().find(a => a.variante.varianteId === Number(v.varianteId));
    if (!fila) { this.errorForm.set('Selecciona un accesorio.'); return; }

    this.guardando.set(true);
    this.errorForm.set(null);
    const va = fila.variante;
    // Suma la cantidad al stock existente (PUT de la variante)
    this.catalogo.actualizarVariante(va.varianteId, {
      color: va.color,
      almacenamiento: va.almacenamiento,
      condicion: va.condicion,
      codigoBarras: va.codigoBarras,
      precioVenta: va.precioVenta,
      precioCosto: va.precioCosto,
      stockNoSerial: va.stockNoSerial + Number(v.cantidad),
      activo: va.activo
    }).subscribe({
      next: () => { this.guardando.set(false); this.modalAbierto.set(false); this.cargarCatalogo(); },
      error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo agregar el accesorio.'); }
    });
  }

  // ----- Ajustar stock de accesorio (Admin) -----
  abrirAjuste(a: AccesorioFila): void {
    this.accesorioSel.set(a);
    this.nuevoStock.set(a.variante.stockNoSerial);
    this.errorStock.set(null);
    this.modalStock.set(true);
  }

  guardarStock(): void {
    const a = this.accesorioSel();
    if (!a) return;
    this.guardandoStock.set(true);
    this.errorStock.set(null);
    const v = a.variante;
    // PUT de la variante conservando sus datos y actualizando solo el stock
    this.catalogo.actualizarVariante(v.varianteId, {
      color: v.color,
      almacenamiento: v.almacenamiento,
      condicion: v.condicion,
      codigoBarras: v.codigoBarras,
      precioVenta: v.precioVenta,
      precioCosto: v.precioCosto,
      stockNoSerial: Math.max(0, this.nuevoStock()),
      activo: v.activo
    }).subscribe({
      next: () => { this.guardandoStock.set(false); this.modalStock.set(false); this.cargarCatalogo(); },
      error: err => { this.guardandoStock.set(false); this.errorStock.set(err.error?.error ?? 'No se pudo ajustar el stock.'); }
    });
  }

  estadoClase(estado: string): string {
    switch (estado) {
      case 'Disponible': return 'bg-tech-accent/10 text-tech-accent';
      case 'Vendido': return 'bg-slate-400/15 text-slate-500';
      case 'EnTaller': return 'bg-tech-purple/10 text-tech-purple';
      case 'Devuelto': return 'bg-tech-sand/30 text-amber-700';
      default: return 'bg-slate-400/15 text-slate-500';
    }
  }
}
