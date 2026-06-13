import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface Garantia {
  garantiaId: number;
  ventaId: number | null;
  imeiId: number | null;
  imei: string | null;
  clienteId: number | null;
  cliente: string | null;
  fechaInicio: string;
  mesesCobertura: number;
  fechaVencimiento: string;
  condiciones: string | null;
  estado: string;
  vigente: boolean;
}

export interface Caso {
  casoGarantiaId: number;
  numeroCaso: string | null;
  garantiaId: number | null;
  imeiId: number | null;
  imei: string | null;
  clienteId: number | null;
  cliente: string | null;
  ordenTallerId: number | null;
  fechaApertura: string;
  tipoFalla: string | null;
  descripcionFalla: string | null;
  tipoResolucion: string | null;
  imeiReemplazoId: number | null;
  imeiReemplazo: string | null;
  estado: string;
  fechaCierre: string | null;
  notas: string | null;
}

export interface IndiceFalla {
  productoId: number;
  producto: string;
  marca: string | null;
  totalCasos: number;
}

@Injectable({ providedIn: 'root' })
export class GarantiaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/garantias`;

  // Garantías
  buscar(clienteId?: number, vigentes?: boolean) {
    let p = new HttpParams();
    if (clienteId) p = p.set('clienteId', clienteId);
    if (vigentes !== undefined) p = p.set('vigentes', vigentes);
    return this.http.get<Garantia[]>(this.base, { params: p });
  }
  porImei(imei: string) { return this.http.get<Garantia>(`${this.base}/imei/${encodeURIComponent(imei)}`); }
  crear(dto: { imeiId?: number | null; ventaId?: number | null; clienteId?: number | null; mesesCobertura: number; condiciones?: string | null; fechaInicio?: string | null }) {
    return this.http.post<Garantia>(this.base, dto);
  }
  anular(id: number) { return this.http.post<void>(`${this.base}/${id}/anular`, {}); }

  // Casos RMA
  casos(estado?: string, clienteId?: number) {
    let p = new HttpParams();
    if (estado) p = p.set('estado', estado);
    if (clienteId) p = p.set('clienteId', clienteId);
    return this.http.get<Caso[]>(`${this.base}/casos`, { params: p });
  }
  casoPorId(id: number) { return this.http.get<Caso>(`${this.base}/casos/${id}`); }
  abrirCaso(dto: { garantiaId?: number | null; imeiId?: number | null; clienteId?: number | null; numeroCaso?: string | null; tipoFalla?: string | null; descripcionFalla?: string | null }) {
    return this.http.post<Caso>(`${this.base}/casos`, dto);
  }
  iniciarCaso(id: number, ordenTallerId?: number) {
    const p = ordenTallerId ? new HttpParams().set('ordenTallerId', ordenTallerId) : undefined;
    return this.http.post<Caso>(`${this.base}/casos/${id}/iniciar`, {}, { params: p });
  }
  resolverCaso(id: number, dto: { tipoResolucion: string; imeiReemplazoId?: number | null; notas?: string | null }) {
    return this.http.post<Caso>(`${this.base}/casos/${id}/resolver`, dto);
  }
  cerrarCaso(id: number) { return this.http.post<Caso>(`${this.base}/casos/${id}/cerrar`, {}); }

  indiceFallas() { return this.http.get<IndiceFalla[]>(`${this.base}/indice-fallas`); }
}
