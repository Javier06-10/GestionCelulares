import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

/** Fila de la vista vw_InventarioDisponible (stock agrupado). */
export interface StockDisponible {
  productoId: number;
  producto: string;
  marca: string | null;
  varianteId: number;
  color: string | null;
  almacenamiento: string | null;
  condicion: string | null;
  precioVenta: number;
  sucursalId: number;
  disponibles: number;
}

/** Un equipo individual por IMEI. */
export interface Imei {
  imeiId: number;
  imei: string;
  varianteId: number;
  producto: string;
  marca: string | null;
  color: string | null;
  almacenamiento: string | null;
  sucursalId: number;
  estado: string;
  precioCosto: number;
  fechaIngreso: string;
}

export interface ImeiRegistro {
  imei: string;
  varianteId: number;
  sucursalId: number;
  compraId?: number | null;
  precioCosto: number;
}

export interface LoteItem { imei: string; varianteId: number; }
export interface LoteRegistro { sucursalId: number; precioCosto: number; items: LoteItem[]; }
export interface LoteResultado { registrados: number; duplicados: string[]; invalidos: string[]; }

@Injectable({ providedIn: 'root' })
export class InventarioService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/inventario`;

  disponibles(sucursalId?: number) {
    let params = new HttpParams();
    if (sucursalId) params = params.set('sucursalId', sucursalId);
    return this.http.get<StockDisponible[]>(`${this.base}/disponibles`, { params });
  }

  porImei(imei: string) {
    return this.http.get<Imei>(`${this.base}/imei/${encodeURIComponent(imei)}`);
  }

  imeisDisponibles(varianteId: number, sucursalId?: number) {
    let params = new HttpParams().set('varianteId', varianteId);
    if (sucursalId) params = params.set('sucursalId', sucursalId);
    return this.http.get<Imei[]>(`${this.base}/disponibles-imeis`, { params });
  }

  registrar(dto: ImeiRegistro) {
    return this.http.post<Imei>(this.base, dto);
  }

  registrarLote(dto: LoteRegistro) {
    return this.http.post<LoteResultado>(`${this.base}/lote`, dto);
  }

  transferir(imeiId: number, sucursalDestinoId: number) {
    return this.http.post<void>(`${this.base}/${imeiId}/transferir`, { sucursalDestinoId });
  }
}
