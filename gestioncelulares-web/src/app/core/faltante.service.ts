import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface Agotado {
  tipo: string;
  varianteId: number;
  producto: string;
  marca: string | null;
  variante: string | null;
  stock: number;
}

export interface FaltanteManual {
  faltanteId: number;
  descripcion: string;
  varianteId: number | null;
  cantidadDeseada: number;
  notas: string | null;
  fechaCreacion: string;
}

export interface FaltantesResumen {
  agotados: Agotado[];
  manuales: FaltanteManual[];
}

@Injectable({ providedIn: 'root' })
export class FaltanteService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/faltantes`;

  listar() { return this.http.get<FaltantesResumen>(this.base); }
  agregar(dto: { descripcion: string; varianteId?: number | null; cantidadDeseada: number; notas?: string | null }) {
    return this.http.post<FaltanteManual>(this.base, dto);
  }
  resolver(id: number) { return this.http.post<void>(`${this.base}/${id}/resolver`, {}); }
}
