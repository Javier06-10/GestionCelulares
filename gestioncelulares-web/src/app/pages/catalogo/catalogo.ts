import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import {
  CatalogoService, Categoria, Marca, Producto, Variante
} from '../../core/catalogo.service';

@Component({
  selector: 'app-catalogo',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe],
  templateUrl: './catalogo.html'
})
export class Catalogo {
  private fb = inject(FormBuilder);
  private servicio = inject(CatalogoService);

  productos = signal<Producto[]>([]);
  marcas = signal<Marca[]>([]);
  categorias = signal<Categoria[]>([]);
  cargando = signal(false);
  termino = signal('');
  expandido = signal<number | null>(null);

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
    activo: [true]
  });

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
    activo: [true]
  });

  // Modal marcas/categorías
  modalCatalogos = signal(false);
  nuevaMarca = signal('');
  nuevaCategoria = signal('');

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
    this.formProducto.reset({ nombre: '', descripcion: '', marcaId: null, categoriaId: null, serializado: true, activo: true });
    this.modalProducto.set(true);
  }

  abrirEditarProducto(p: Producto): void {
    this.editandoProducto.set(p);
    this.errorProducto.set(null);
    this.formProducto.reset({
      nombre: p.nombre,
      descripcion: p.descripcion ?? '',
      marcaId: p.marcaId,
      categoriaId: p.categoriaId,
      serializado: p.serializado,
      activo: p.activo
    });
    this.modalProducto.set(true);
  }

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
      serializado: v.serializado
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
    this.formVariante.reset({ color: '', almacenamiento: '', condicion: 'Nuevo', codigoBarras: '', precioVenta: 0, precioCosto: 0, stockNoSerial: 0, activo: true });
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
      activo: vr.activo
    });
    this.modalVariante.set(true);
  }

  guardarVariante(): void {
    if (this.formVariante.invalid || !this.productoVariante()) { this.formVariante.markAllAsTouched(); return; }
    this.guardandoVariante.set(true);
    this.errorVariante.set(null);
    const v = this.formVariante.getRawValue();
    const dto = {
      color: v.color?.trim() || null,
      almacenamiento: v.almacenamiento?.trim() || null,
      condicion: v.condicion?.trim() || null,
      codigoBarras: v.codigoBarras?.trim() || null,
      precioVenta: v.precioVenta,
      precioCosto: v.precioCosto,
      stockNoSerial: v.stockNoSerial,
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
}
