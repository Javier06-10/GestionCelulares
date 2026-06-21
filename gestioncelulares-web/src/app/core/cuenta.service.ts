import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface Cuenta {
  cuentaContableId: number;
  codigo: string;
  nombre: string;
  tipo: string;
  cuentaPadreId: number | null;
  naturaleza: string;
  permiteMovimiento: boolean;
  esSistema: boolean;
  activo: boolean;
  nivel: number;
}

@Injectable({ providedIn: 'root' })
export class CuentaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/cuentas`;

  listar() { return this.http.get<Cuenta[]>(this.base); }

  crear(dto: { codigo: string; nombre: string; cuentaPadreId?: number | null; tipo?: string | null; naturaleza?: string | null; permiteMovimiento: boolean }) {
    return this.http.post<Cuenta>(this.base, dto);
  }
  actualizar(id: number, dto: { nombre: string; permiteMovimiento: boolean; activo: boolean }) {
    return this.http.put<Cuenta>(`${this.base}/${id}`, dto);
  }
  eliminar(id: number) { return this.http.delete<void>(`${this.base}/${id}`); }
}
