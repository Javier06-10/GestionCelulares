import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface SecuenciaNcf {
  secuenciaNcfId: number;
  tipoComprobante: string;
  descripcion: string;
  serie: string;
  secuencia: number;
  hasta: number;
  disponibles: number;
  ejemplo: string | null;
  vencimiento: string | null;
  activo: boolean;
}

@Injectable({ providedIn: 'root' })
export class NcfService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/ncf`;

  listar() { return this.http.get<SecuenciaNcf[]>(this.base); }

  configurar(id: number, dto: { serie: string; secuencia: number; hasta: number; vencimiento?: string | null; activo: boolean }) {
    return this.http.put<SecuenciaNcf>(`${this.base}/${id}`, dto);
  }
}
