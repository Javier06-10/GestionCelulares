import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface Usuario {
  usuarioId: number;
  nombreUsuario: string;
  nombreCompleto: string;
  email: string | null;
  rolId: number;
  rol: string;
  sucursalId: number | null;
  activo: boolean;
  ultimoAcceso: string | null;
  fechaCreacion: string;
}

export interface Rol { rolId: number; nombre: string; descripcion: string | null; }

@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/usuarios`;

  listar(activos?: boolean) {
    let params = new HttpParams();
    if (activos !== undefined) params = params.set('activos', activos);
    return this.http.get<Usuario[]>(this.base, { params });
  }

  porId(id: number) { return this.http.get<Usuario>(`${this.base}/${id}`); }
  roles() { return this.http.get<Rol[]>(`${this.base}/roles`); }

  crear(dto: { nombreUsuario: string; nombreCompleto: string; email?: string | null; contrasena: string; rolId: number; sucursalId?: number | null; }) {
    return this.http.post<Usuario>(this.base, dto);
  }
  actualizar(id: number, dto: { nombreCompleto: string; email?: string | null; rolId: number; sucursalId?: number | null; activo: boolean; }) {
    return this.http.put<Usuario>(`${this.base}/${id}`, dto);
  }
  resetContrasena(id: number, contrasenaNueva: string) {
    return this.http.post<void>(`${this.base}/${id}/reset-contrasena`, { contrasenaNueva });
  }
  cambiarContrasena(contrasenaActual: string, contrasenaNueva: string) {
    return this.http.post<void>(`${this.base}/cambiar-contrasena`, { contrasenaActual, contrasenaNueva });
  }
}
