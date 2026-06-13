import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface OrdenResumen {
  ordenTallerId: number;
  numeroOrden: string | null;
  cliente: string | null;
  equipo: string | null;
  tecnico: string | null;
  estado: string;
  costoEstimado: number;
  fechaRecepcion: string;
}

export interface Repuesto {
  id: number;
  varianteId: number | null;
  descripcion: string;
  cantidad: number;
  costo: number;
}

export interface Foto {
  id: number;
  url: string;
  fecha: string;
}

export interface Orden {
  ordenTallerId: number;
  numeroOrden: string | null;
  sucursalId: number;
  clienteId: number | null;
  cliente: string | null;
  imeiId: number | null;
  imei: string | null;
  equipoDescripcion: string | null;
  diagnostico: string | null;
  tecnicoId: number | null;
  tecnico: string | null;
  estado: string;
  anticipo: number;
  costoEstimado: number;
  costoFinal: number | null;
  comisionTecnico: number;
  totalRepuestos: number;
  fechaRecepcion: string;
  fechaEntrega: string | null;
  repuestos: Repuesto[];
  fotos: Foto[];
}

export interface OrdenCrear {
  sucursalId: number;
  clienteId?: number | null;
  imeiId?: number | null;
  equipoDescripcion?: string | null;
  diagnostico?: string | null;
  tecnicoId?: number | null;
  numeroOrden?: string | null;
  anticipo: number;
  costoEstimado: number;
}

export interface CambioEstado {
  estado: string;
  costoFinal?: number | null;
  comisionTecnico?: number | null;
}

export interface RepuestoAgregar {
  varianteId?: number | null;
  descripcion: string;
  cantidad: number;
  costo: number;
}

@Injectable({ providedIn: 'root' })
export class TallerService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/taller`;

  buscar(estado?: string, sucursalId?: number, tecnicoId?: number, clienteId?: number) {
    let params = new HttpParams();
    if (estado) params = params.set('estado', estado);
    if (sucursalId) params = params.set('sucursalId', sucursalId);
    if (tecnicoId) params = params.set('tecnicoId', tecnicoId);
    if (clienteId) params = params.set('clienteId', clienteId);
    return this.http.get<OrdenResumen[]>(this.base, { params });
  }

  porId(id: number) {
    return this.http.get<Orden>(`${this.base}/${id}`);
  }

  crear(dto: OrdenCrear) {
    return this.http.post<Orden>(this.base, dto);
  }

  actualizar(id: number, dto: Partial<OrdenCrear>) {
    return this.http.put<Orden>(`${this.base}/${id}`, dto);
  }

  cambiarEstado(id: number, dto: CambioEstado) {
    return this.http.post<Orden>(`${this.base}/${id}/estado`, dto);
  }

  agregarRepuesto(id: number, dto: RepuestoAgregar) {
    return this.http.post<Orden>(`${this.base}/${id}/repuestos`, dto);
  }

  agregarFoto(id: number, url: string) {
    return this.http.post<Orden>(`${this.base}/${id}/fotos`, { url });
  }
}
