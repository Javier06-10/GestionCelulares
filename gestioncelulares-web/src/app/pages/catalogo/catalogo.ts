import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import {
  CatalogoService, Categoria, Marca, Producto, Variante
} from '../../core/catalogo.service';
import { ImagenService } from '../../core/imagen.service';
import { BarcodeComponent } from '../../shared/barcode.component';
import { CountUpDirective } from '../../shared/count-up.directive';

type FiltroTipo = 'todos' | 'dispositivo' | 'accesorio';

@Component({
  selector: 'app-catalogo',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, CountUpDirective, BarcodeComponent],
  templateUrl: './catalogo.html'
})
export class Catalogo {
  private fb = inject(FormBuilder);
  private servicio = inject(CatalogoService);
  imagenes = inject(ImagenService);

  productos = signal<Producto[]>([]);
  marcas = signal<Marca[]>([]);
  categorias = signal<Categoria[]>([]);
  cargando = signal(false);
  termino = signal('');
  expandido = signal<number | null>(null);
  filtroTipo = signal<FiltroTipo>('todos');

  // Vista filtrada por tipo (sobre lo que devuelve la búsqueda del servidor)
  productosVista = computed<Producto[]>(() => {
    const f = this.filtroTipo();
    const lista = this.productos();
    if (f === 'dispositivo') return lista.filter(p => p.serializado);
    if (f === 'accesorio') return lista.filter(p => !p.serializado);
    return lista;
  });

  // KPIs
  kpiDispositivos = computed(() => this.productos().filter(p => p.serializado).length);
  kpiAccesorios = computed(() => this.productos().filter(p => !p.serializado).length);
  kpiVariantes = computed(() => this.productos().reduce((a, p) => a + p.variantes.length, 0));

  filtrar(f: FiltroTipo): void { this.filtroTipo.set(f); }

  // Margen de ganancia de una variante (%)
  margen(vr: Variante): number {
    return vr.precioVenta > 0 ? Math.round(((vr.precioVenta - vr.precioCosto) / vr.precioVenta) * 100) : 0;
  }
  margenClase(m: number): string {
    if (m >= 30) return 'bg-emerald-500/10 text-emerald-600';
    if (m >= 10) return 'bg-amber-500/10 text-amber-600';
    return 'bg-rose-500/10 text-rose-500';
  }

  // Modal producto
  modalProducto = signal(false);
  editandoProducto = signal<Producto | null>(null);
  guardandoProducto = signal(false);
  errorProducto = signal<string | null>(null);
  formProducto = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    descripcion: [''],
    marcaId: [null as number | null],
    categoriaId: [null as number | null],
    serializado: [true],
    activo: [true],
    imagenUrl: [null as string | null]
  });
  subiendoImagen = signal(false);
  errorImagen = signal<string | null>(null);

  // Modal variante
  modalVariante = signal(false);
  productoVariante = signal<Producto | null>(null);
  editandoVariante = signal<Variante | null>(null);
  guardandoVariante = signal(false);
  errorVariante = signal<string | null>(null);
  formVariante = this.fb.nonNullable.group({
    color: [''],
    almacenamiento: [''],
    condicion: ['Nuevo'],
    codigoBarras: [''],
    precioVenta: [0, [Validators.required, Validators.min(0)]],
    precioCosto: [0, [Validators.required, Validators.min(0)]],
    stockNoSerial: [0, [Validators.min(0)]],
    stockMinimo: [0, [Validators.min(0)]],
    activo: [true]
  });

  // Modal marcas/categorías
  modalCatalogos = signal(false);
  nuevaMarca = signal('');
  nuevaCategoria = signal('');

  // Generación de códigos de barras
  generandoCodigos = signal(false);
  generandoVarianteId = signal<number | null>(null);
  mensajeCodigos = signal<string | null>(null);

  // Accesorios sin código de barras (solo no serializados)
  accesoriosSinCodigo = computed(() =>
    this.productos().filter(p => !p.serializado)
      .reduce((a, p) => a + p.variantes.filter(v => !v.codigoBarras).length, 0)
  );

  /** Genera el código que falta en todo el catálogo (accesorios). */
  generarCodigosFaltantes(): void {
    if (this.generandoCodigos()) return;
    this.generandoCodigos.set(true);
    this.mensajeCodigos.set(null);
    this.servicio.generarCodigosFaltantes().subscribe({
      next: r => {
        this.generandoCodigos.set(false);
        this.mensajeCodigos.set(r.generados > 0
          ? `Se generaron ${r.generados} código(s) de barras.`
          : 'Todos los accesorios ya tienen código.');
        if (r.generados > 0) this.cargar();
      },
      error: () => { this.generandoCodigos.set(false); this.mensajeCodigos.set('No se pudieron generar los códigos.'); }
    });
  }

  /** Genera el código de una variante (accesorio) puntual. */
  generarCodigoVariante(p: Producto, vr: Variante): void {
    if (this.generandoVarianteId()) return;
    this.generandoVarianteId.set(vr.varianteId);
    this.servicio.generarCodigo(vr.varianteId).subscribe({
      next: () => { this.generandoVarianteId.set(null); const pid = p.productoId; this.cargar(); this.expandido.set(pid); },
      error: () => this.generandoVarianteId.set(null)
    });
  }

  constructor() {
    this.cargar();
    this.cargarCatalogos();
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.productos(this.termino() || undefined).subscribe({
      next: data => { this.productos.set(data); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  cargarCatalogos(): void {
    this.servicio.marcas().subscribe(m => this.marcas.set(m));
    this.servicio.categorias().subscribe(c => this.categorias.set(c));
  }

  buscar(v: string): void { this.termino.set(v); this.cargar(); }

  alternarExpandir(id: number): void {
    this.expandido.set(this.expandido() === id ? null : id);
  }

  // ----- Producto -----
  abrirNuevoProducto(): void {
    this.editandoProducto.set(null);
    this.errorProducto.set(null);
    this.errorImagen.set(null);
    this.formProducto.reset({ nombre: '', descripcion: '', marcaId: null, categoriaId: null, serializado: true, activo: true, imagenUrl: null });
    this.modalProducto.set(true);
  }

  abrirEditarProducto(p: Producto): void {
    this.editandoProducto.set(p);
    this.errorProducto.set(null);
    this.errorImagen.set(null);
    this.formProducto.reset({
      nombre: p.nombre,
      descripcion: p.descripcion ?? '',
      marcaId: p.marcaId,
      categoriaId: p.categoriaId,
      serializado: p.serializado,
      activo: p.activo,
      imagenUrl: p.imagenUrl
    });
    this.modalProducto.set(true);
  }

  // Sube la imagen elegida a Cloudinary y guarda el URL en el form.
  subirImagen(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const archivo = input.files?.[0];
    if (!archivo) return;
    this.errorImagen.set(null);
    this.subiendoImagen.set(true);
    this.imagenes.subir(archivo).subscribe({
      next: url => { this.formProducto.controls.imagenUrl.setValue(url); this.subiendoImagen.set(false); input.value = ''; },
      error: () => { this.subiendoImagen.set(false); this.errorImagen.set('No se pudo subir la imagen.'); input.value = ''; }
    });
  }

  quitarImagen(): void { this.formProducto.controls.imagenUrl.setValue(null); }

  guardarProducto(): void {
    if (this.formProducto.invalid) { this.formProducto.markAllAsTouched(); return; }
    this.guardandoProducto.set(true);
    this.errorProducto.set(null);
    const v = this.formProducto.getRawValue();
    const base = {
      nombre: v.nombre.trim(),
      descripcion: v.descripcion?.trim() || null,
      marcaId: v.marcaId ? Number(v.marcaId) : null,
      categoriaId: v.categoriaId ? Number(v.categoriaId) : null,
      serializado: v.serializado,
      imagenUrl: v.imagenUrl || null
    };

    const accion = this.editandoProducto()
      ? this.servicio.actualizarProducto(this.editandoProducto()!.productoId, { ...base, activo: v.activo })
      : this.servicio.crearProducto({ ...base, variantes: [] });

    accion.subscribe({
      next: () => { this.guardandoProducto.set(false); this.modalProducto.set(false); this.cargar(); },
      error: err => { this.guardandoProducto.set(false); this.errorProducto.set(err.error?.error ?? 'No se pudo guardar el producto.'); }
    });
  }

  // ----- Variante -----
  abrirNuevaVariante(p: Producto): void {
    this.productoVariante.set(p);
    this.editandoVariante.set(null);
    this.errorVariante.set(null);
    this.formVariante.reset({ color: '', almacenamiento: '', condicion: 'Nuevo', codigoBarras: '', precioVenta: 0, precioCosto: 0, stockNoSerial: 0, stockMinimo: 0, activo: true });
    this.modalVariante.set(true);
  }

  abrirEditarVariante(p: Producto, vr: Variante): void {
    this.productoVariante.set(p);
    this.editandoVariante.set(vr);
    this.errorVariante.set(null);
    this.formVariante.reset({
      color: vr.color ?? '',
      almacenamiento: vr.almacenamiento ?? '',
      condicion: vr.condicion ?? 'Nuevo',
      codigoBarras: vr.codigoBarras ?? '',
      precioVenta: vr.precioVenta,
      precioCosto: vr.precioCosto,
      stockNoSerial: vr.stockNoSerial,
      stockMinimo: vr.stockMinimo,
      activo: vr.activo
    });
    this.modalVariante.set(true);
  }

  guardarVariante(): void {
    if (this.formVariante.invalid || !this.productoVariante()) { this.formVariante.markAllAsTouched(); return; }
    this.guardandoVariante.set(true);
    this.errorVariante.set(null);
    const v = this.formVariante.getRawValue();
    const serializado = !!this.productoVariante()?.serializado;
    const dto = {
      color: v.color?.trim() || null,
      almacenamiento: v.almacenamiento?.trim() || null,
      // Condición solo aplica a dispositivos; código de barras y stock solo a accesorios
      condicion: serializado ? (v.condicion?.trim() || null) : null,
      codigoBarras: serializado ? null : (v.codigoBarras?.trim() || null),
      precioVenta: v.precioVenta,
      precioCosto: v.precioCosto,
      stockNoSerial: serializado ? 0 : v.stockNoSerial,
      stockMinimo: v.stockMinimo,
      activo: v.activo
    };

    const accion = this.editandoVariante()
      ? this.servicio.actualizarVariante(this.editandoVariante()!.varianteId, dto)
      : this.servicio.agregarVariante(this.productoVariante()!.productoId, dto);

    accion.subscribe({
      next: () => {
        this.guardandoVariante.set(false);
        this.modalVariante.set(false);
        const pid = this.productoVariante()!.productoId;
        this.cargar();
        this.expandido.set(pid);
      },
      error: err => { this.guardandoVariante.set(false); this.errorVariante.set(err.error?.error ?? 'No se pudo guardar la variante.'); }
    });
  }

  // ----- Marcas / Categorías -----
  agregarMarca(): void {
    const n = this.nuevaMarca().trim();
    if (!n) return;
    this.servicio.crearMarca(n).subscribe({
      next: () => { this.nuevaMarca.set(''); this.cargarCatalogos(); }
    });
  }

  agregarCategoria(): void {
    const n = this.nuevaCategoria().trim();
    if (!n) return;
    this.servicio.crearCategoria(n).subscribe({
      next: () => { this.nuevaCategoria.set(''); this.cargarCatalogos(); }
    });
  }

  obtenerColorHex(colorName: string | undefined): string | null {
    if (!colorName) return null;
    const name = colorName.trim().toLowerCase();
    
    const dict: Record<string, string> = {
      negro: '#121214',
      black: '#121214',
      midnight: '#0b0c10',
      blanco: '#f5f6fa',
      white: '#ffffff',
      gris: '#7f8c8d',
      grey: '#7f8c8d',
      silver: '#bdc3c7',
      plata: '#bdc3c7',
      azul: '#2980b9',
      blue: '#2980b9',
      rojo: '#c0392b',
      red: '#c0392b',
      verde: '#27ae60',
      green: '#27ae60',
      dorado: '#f1c40f',
      gold: '#f1c40f',
      rosa: '#fda7df',
      pink: '#fda7df',
      purpura: '#8e44ad',
      purple: '#8e44ad',
      violeta: '#9b59b6',
      morado: '#8e44ad',
      titanio: '#8a8d91',
      titanium: '#8a8d91',
      amarillo: '#f5cd79',
      yellow: '#f5cd79'
    };

    for (const key in dict) {
      if (name.includes(key)) return dict[key];
    }
    return null;
  }

  condicionClase(cond: string): string {
    switch (cond) {
      case 'Nuevo': return 'bg-tech-accent/15 text-emerald-600';
      case 'Usado': return 'bg-tech-charcoal/10 text-tech-charcoal';
      case 'Reacondicionado': return 'bg-tech-purple/15 text-tech-purple';
      default: return 'bg-slate-100 text-slate-600';
    }
  }
}
