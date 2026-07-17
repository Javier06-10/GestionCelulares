import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface Variante {
  varianteId: number;
  productoId: number;
  color: string | null;
  almacenamiento: string | null;
  condicion: string | null;
  codigoBarras: string | null;
  precioVenta: number;
  precioCosto: number;
  stockNoSerial: number;
  stockMinimo: number;
  activo: boolean;
}

export interface Producto {
  productoId: number;
  nombre: string;
  descripcion: string | null;
  marcaId: number | null;
  marca: string | null;
  categoriaId: number | null;
  categoria: string | null;
  serializado: boolean;
  activo: boolean;
  provisional: boolean;
  fechaCreacion: string;
  variantes: Variante[];
}

/** Resultado de la creación rápida (venta rápida del POS). */
export interface ProductoRapidoResultado {
  productoId: number;
  varianteId: number;
  nombre: string;
  precioVenta: number;
}

export interface Marca { marcaId: number; nombre: string; }
export interface Categoria { categoriaId: number; nombre: string; }

export interface VarianteGuardar {
  color?: string | null;
  almacenamiento?: string | null;
  condicion?: string | null;
  codigoBarras?: string | null;
  precioVenta: number;
  precioCosto: number;
  stockNoSerial: number;
  stockMinimo: number;
  activo?: boolean;
}

export interface ProductoCrear {
  nombre: string;
  descripcion?: string | null;
  marcaId?: number | null;
  categoriaId?: number | null;
  serializado: boolean;
  variantes: VarianteGuardar[];
}

export interface ProductoActualizar {
  nombre: string;
  descripcion?: string | null;
  marcaId?: number | null;
  categoriaId?: number | null;
  serializado: boolean;
  activo: boolean;
}

/** Variante aplanada con el nombre del producto, para selectores. */
export interface VarianteOpcion {
  varianteId: number;
  etiqueta: string;
  precioVenta: number;
  precioCosto: number;
  serializado: boolean;
}

@Injectable({ providedIn: 'root' })
export class CatalogoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/catalogo`;

  // Marcas
  marcas() { return this.http.get<Marca[]>(`${this.base}/marcas`); }
  crearMarca(nombre: string) { return this.http.post<Marca>(`${this.base}/marcas`, { nombre }); }

  // Categorías
  categorias() { return this.http.get<Categoria[]>(`${this.base}/categorias`); }
  crearCategoria(nombre: string) { return this.http.post<Categoria>(`${this.base}/categorias`, { nombre }); }

  // Productos
  productos(buscar?: string) {
    const url = buscar ? `${this.base}/productos?buscar=${encodeURIComponent(buscar)}` : `${this.base}/productos`;
    return this.http.get<Producto[]>(url);
  }
  productoPorId(id: number) { return this.http.get<Producto>(`${this.base}/productos/${id}`); }
  crearProducto(dto: ProductoCrear) { return this.http.post<Producto>(`${this.base}/productos`, dto); }
  actualizarProducto(id: number, dto: ProductoActualizar) { return this.http.put<Producto>(`${this.base}/productos/${id}`, dto); }
  /** Venta rápida: crea un accesorio provisional con nombre y precio. */
  crearRapido(dto: { nombre: string; precioVenta: number; codigoBarras?: string | null; cantidad: number }) {
    return this.http.post<ProductoRapidoResultado>(`${this.base}/productos/rapido`, dto);
  }

  // Variantes
  agregarVariante(productoId: number, dto: VarianteGuardar) {
    return this.http.post<Variante>(`${this.base}/productos/${productoId}/variantes`, dto);
  }
  actualizarVariante(varianteId: number, dto: VarianteGuardar) {
    return this.http.put<Variante>(`${this.base}/variantes/${varianteId}`, dto);
  }

  /** Genera y asigna un código de barras EAN-13 único a una variante sin código. */
  generarCodigo(varianteId: number) {
    return this.http.post<Variante>(`${this.base}/variantes/${varianteId}/generar-codigo`, {});
  }

  /** Genera código de barras para todas las variantes que no tengan uno. */
  generarCodigosFaltantes() {
    return this.http.post<{ generados: number }>(`${this.base}/variantes/generar-codigos`, {});
  }

  /** Aplana todas las variantes activas en opciones para un <select>. */
  variantesOpciones(productos: Producto[]): VarianteOpcion[] {
    const ops: VarianteOpcion[] = [];
    for (const p of productos) {
      for (const v of p.variantes) {
        if (!v.activo) continue;
        const detalle = [v.color, v.almacenamiento, v.condicion].filter(Boolean).join(' · ');
        ops.push({
          varianteId: v.varianteId,
          etiqueta: detalle ? `${p.nombre} — ${detalle}` : p.nombre,
          precioVenta: v.precioVenta,
          precioCosto: v.precioCosto,
          serializado: p.serializado
        });
      }
    }
    return ops;
  }
}
