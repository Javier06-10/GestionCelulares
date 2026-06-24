import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { CatalogoService, Producto, Variante } from '../../core/catalogo.service';
import { Agotado, FaltanteManual, FaltanteService } from '../../core/faltante.service';
import { Imei, InventarioService, StockDisponible } from '../../core/inventario.service';
import { exportarCsv } from '../../core/reporte.service';
import { CountUpDirective } from '../../shared/count-up.directive';

interface OpcionSerializada { varianteId: number; etiqueta: string; }
interface AccesorioFila { producto: Producto; variante: Variante; nombre: string; detalle: string; }

type TabInv = 'existencias' | 'agotados' | 'faltantes';

@Component({
  selector: 'app-inventario',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DatePipe, CountUpDirective],
  templateUrl: './inventario.html'
})
export class Inventario {
  private fb = inject(FormBuilder);
  private servicio = inject(InventarioService);
  private catalogo = inject(CatalogoService);
  private faltantesSrv = inject(FaltanteService);
  auth = inject(AuthService);

  tab = signal<TabInv>('existencias');
  irA(t: TabInv): void { this.tab.set(t); }

  stock = signal<StockDisponible[]>([]);
  cargando = signal(false);
  productos = signal<Producto[]>([]);

  // Faltantes / reposición
  agotados = signal<Agotado[]>([]);
  manuales = signal<FaltanteManual[]>([]);
  cargandoFalt = signal(false);
  agotadosCount = computed(() => this.agotados().length);
  porPedirCount = computed(() => this.manuales().length);

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

  // ----- Nivel de stock (barra relativa al máximo + color por umbral) -----
  private maxDisp = computed(() => Math.max(1, ...this.stock().map(s => s.disponibles)));
  private maxAcc = computed(() => Math.max(1, ...this.accesorios().map(a => a.variante.stockNoSerial)));

  nivelDisp(n: number): number { return Math.max(6, Math.round((n / this.maxDisp()) * 100)); }
  nivelAcc(n: number): number { return Math.max(n > 0 ? 6 : 0, Math.round((n / this.maxAcc()) * 100)); }

  claseDisp(n: number): string {
    if (n <= 2) return 'bg-red-500';
    if (n <= 5) return 'bg-amber-500';
    return 'bg-tech-accent';
  }
  claseAcc(n: number): string {
    if (n <= 0) return 'bg-red-500';
    if (n <= 3) return 'bg-amber-500';
    return 'bg-tech-purple';
  }

  exportar(): void {
    const filas: (string | number)[][] = [];
    this.stock().forEach(s => filas.push([
      'Dispositivo', s.producto, s.marca ?? '',
      [s.color, s.almacenamiento, s.condicion].filter(Boolean).join(' '),
      s.precioVenta, s.disponibles
    ]));
    this.accesorios().forEach(a => filas.push([
      'Accesorio', a.nombre, '', a.detalle, a.variante.precioVenta, a.variante.stockNoSerial
    ]));
    exportarCsv('inventario', ['Tipo', 'Producto', 'Marca', 'Variante', 'Precio venta', 'Stock'], filas);
  }

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

  // Modal faltante manual
  modalFalt = signal(false);
  guardandoFalt = signal(false);
  errorFalt = signal<string | null>(null);
  formFalt = this.fb.nonNullable.group({
    descripcion: ['', [Validators.required, Validators.maxLength(200)]],
    cantidadDeseada: [1, [Validators.required, Validators.min(1)]],
    notas: ['']
  });

  constructor() {
    this.cargar();
    this.cargarCatalogo();
    this.cargarFaltantes();
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.disponibles().subscribe({
      next: data => { this.stock.set(data); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  cargarFaltantes(): void {
    this.cargandoFalt.set(true);
    this.faltantesSrv.listar().subscribe({
      next: r => { this.agotados.set(r.agotados); this.manuales.set(r.manuales); this.cargandoFalt.set(false); },
      error: () => this.cargandoFalt.set(false)
    });
  }

  abrirFaltante(): void {
    this.errorFalt.set(null);
    this.formFalt.reset({ descripcion: '', cantidadDeseada: 1, notas: '' });
    this.modalFalt.set(true);
  }

  guardarFaltante(): void {
    if (this.formFalt.invalid) { this.formFalt.markAllAsTouched(); return; }
    this.guardandoFalt.set(true);
    this.errorFalt.set(null);
    const v = this.formFalt.getRawValue();
    this.faltantesSrv.agregar({ descripcion: v.descripcion.trim(), cantidadDeseada: v.cantidadDeseada, notas: v.notas?.trim() || null }).subscribe({
      next: () => { this.guardandoFalt.set(false); this.modalFalt.set(false); this.cargarFaltantes(); },
      error: err => { this.guardandoFalt.set(false); this.errorFalt.set(err.error?.error ?? 'No se pudo guardar.'); }
    });
  }

  resolver(f: FaltanteManual): void {
    this.faltantesSrv.resolver(f.faltanteId).subscribe({ next: () => this.cargarFaltantes() });
  }

  cargarCatalogo(): void {
    this.catalogo.productos().subscribe(p => this.productos.set(p));
  }

  refrescar(): void {
    this.cargar();
    this.cargarCatalogo();
    this.cargarFaltantes();
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
      next: () => { this.guardando.set(false); this.modalAbierto.set(false); this.cargar(); this.cargarFaltantes(); },
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
      next: () => { this.guardando.set(false); this.modalAbierto.set(false); this.cargarCatalogo(); this.cargarFaltantes(); },
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
      next: () => { this.guardandoStock.set(false); this.modalStock.set(false); this.cargarCatalogo(); this.cargarFaltantes(); },
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
