import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface Cliente {
  clienteId: number;
  cedula: string | null;
  nombre: string;
  telefono: string | null;
  email: string | null;
  direccion: string | null;
  esMoroso: boolean;
  bloqueado: boolean;
  fechaCreacion: string;
}

export interface ClienteGuardar {
  cedula?: string | null;
  nombre: string;
  telefono?: string | null;
  email?: string | null;
  direccion?: string | null;
}

@Injectable({ providedIn: 'root' })
export class ClienteService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/clientes`;

  buscar(termino?: string, morosos?: boolean, bloqueados?: boolean) {
    let params = new HttpParams();
    if (termino) params = params.set('buscar', termino);
    if (morosos !== undefined) params = params.set('morosos', morosos);
    if (bloqueados !== undefined) params = params.set('bloqueados', bloqueados);
    return this.http.get<Cliente[]>(this.base, { params });
  }

  porId(id: number) {
    return this.http.get<Cliente>(`${this.base}/${id}`);
  }

  crear(dto: ClienteGuardar) {
    return this.http.post<Cliente>(this.base, dto);
  }

  actualizar(id: number, dto: ClienteGuardar) {
    return this.http.put<Cliente>(`${this.base}/${id}`, dto);
  }

  bloquear(id: number) {
    return this.http.post<void>(`${this.base}/${id}/bloquear`, {});
  }

  desbloquear(id: number) {
    return this.http.post<void>(`${this.base}/${id}/desbloquear`, {});
  }
}
