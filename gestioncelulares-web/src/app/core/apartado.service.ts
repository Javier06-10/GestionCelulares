import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface EquipoApartable {
  imeiId: number;
  imei: string;
  varianteId: number;
  producto: string;
  marca: string | null;
  variante: string | null;
  precioVenta: number;
}

export interface AbonoApartado {
  abonoApartadoId: number;
  monto: number;
  metodoPago: string | null;
  tipo: string;
  fecha: string;
}

export interface ApartadoResumen {
  apartadoId: number;
  cliente: string;
  equipo: string;
  precioTotal: number;
  totalAbonado: number;
  saldo: number;
  estado: string;
  fechaInicio: string;
}

export interface Apartado {
  apartadoId: number;
  clienteId: number;
  cliente: string;
  imeiId: number | null;
  imei: string | null;
  varianteId: number;
  equipo: string;
  precioTotal: number;
  totalAbonado: number;
  saldo: number;
  estado: string;
  ventaId: number | null;
  notas: string | null;
  fechaInicio: string;
  fechaCierre: string | null;
  abonos: AbonoApartado[];
}

@Injectable({ providedIn: 'root' })
export class ApartadoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/apartados`;

  equipos(sucursalId?: number, buscar?: string) {
    let params = new HttpParams();
    if (sucursalId) params = params.set('sucursalId', sucursalId);
    if (buscar) params = params.set('buscar', buscar);
    return this.http.get<EquipoApartable[]>(`${this.base}/equipos`, { params });
  }

  listar(estado?: string, clienteId?: number) {
    let params = new HttpParams();
    if (estado) params = params.set('estado', estado);
    if (clienteId) params = params.set('clienteId', clienteId);
    return this.http.get<ApartadoResumen[]>(this.base, { params });
  }

  porId(id: number) { return this.http.get<Apartado>(`${this.base}/${id}`); }

  crear(dto: { clienteId: number; imeiId: number; precioTotal: number; abonoInicial: number; metodoPagoId?: number | null; notas?: string | null }) {
    return this.http.post<Apartado>(this.base, dto);
  }
  abonar(id: number, dto: { monto: number; metodoPagoId?: number | null }) {
    return this.http.post<Apartado>(`${this.base}/${id}/abonos`, dto);
  }
  cambiarEquipo(id: number, dto: { imeiId: number; precioTotal: number }) {
    return this.http.post<Apartado>(`${this.base}/${id}/cambiar-equipo`, dto);
  }
  completar(id: number) { return this.http.post<Apartado>(`${this.base}/${id}/completar`, {}); }
  cancelar(id: number, dto: { devolverMonto: number; motivo?: string | null }) {
    return this.http.post<Apartado>(`${this.base}/${id}/cancelar`, dto);
  }
}
