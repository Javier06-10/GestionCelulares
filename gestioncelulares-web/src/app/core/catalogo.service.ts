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
  fechaCreacion: string;
  variantes: Variante[];
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

  productos(buscar?: string) {
    const url = buscar ? `${this.base}/productos?buscar=${encodeURIComponent(buscar)}` : `${this.base}/productos`;
    return this.http.get<Producto[]>(url);
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
