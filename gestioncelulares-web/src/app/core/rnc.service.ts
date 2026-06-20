import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface RncConsulta {
  rnc: string;
  nombre: string;
  estado: string | null;
}

@Injectable({ providedIn: 'root' })
export class RncService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/rnc`;

  consultar(rnc: string) {
    return this.http.get<RncConsulta>(`${this.base}/${encodeURIComponent(rnc)}`);
  }
}
