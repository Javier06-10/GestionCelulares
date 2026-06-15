import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface Empleado {
  usuarioId: number;
  nombreCompleto: string;
  rol: string;
}

export interface PagoEmpleado {
  pagoEmpleadoId: number;
  empleadoId: number;
  empleado: string;
  rol: string;
  tipo: string;
  monto: number;
  periodo: string | null;
  notas: string | null;
  fecha: string;
}

export interface NominaResumen {
  pagos: PagoEmpleado[];
  totalPagado: number;
}

@Injectable({ providedIn: 'root' })
export class NominaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/nomina`;

  empleados() { return this.http.get<Empleado[]>(`${this.base}/empleados`); }

  listar(empleadoId?: number | null, desde?: string | null, hasta?: string | null) {
    let params = new HttpParams();
    if (empleadoId) params = params.set('empleadoId', empleadoId);
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);
    return this.http.get<NominaResumen>(this.base, { params });
  }

  registrar(dto: { empleadoId: number; tipo: string; monto: number; periodo?: string | null; notas?: string | null }) {
    return this.http.post<PagoEmpleado>(this.base, dto);
  }
}
