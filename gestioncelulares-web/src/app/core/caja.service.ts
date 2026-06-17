import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface SesionCaja {
  sesionCajaId: number;
  sucursalId: number;
  usuarioApertura: number;
  usuarioAperturaNombre: string | null;
  usuarioCierre: number | null;
  montoApertura: number;
  montoCierre: number | null;
  diferencia: number | null;
  estado: string;
  fechaApertura: string;
  fechaCierre: string | null;
  totalIngresos: number;
  totalEgresos: number;
}

export interface MovimientoCaja {
  movimientoCajaId: number;
  sesionCajaId: number;
  tipo: string;
  concepto: string;
  monto: number;
  referencia: string | null;
  fecha: string;
}

export interface CierreResultado {
  montoEsperado: number;
  montoContado: number;
  diferencia: number;
}

export interface ResumenMetodo {
  metodo: string;
  cantidad: number;
  monto: number;
}

export interface ResumenTurno {
  sesionCajaId: number;
  empleado: string | null;
  fechaApertura: string;
  montoApertura: number;
  ventasCantidad: number;
  ventasTotal: number;
  ventasEfectivo: number;
  ventasOtros: number;
  abonosCantidad: number;
  abonosTotal: number;
  abonosEfectivo: number;
  tallerRecepciones: number;
  tallerAnticipos: number;
  tallerEntregas: number;
  tallerEntregasCobrado: number;
  apartadosAbonos: number;
  apartadosTotal: number;
  efectivoApartados: number;
  totalIngresos: number;
  totalEgresos: number;
  efectivoVentas: number;
  efectivoReparaciones: number;
  efectivoEsperado: number;
  ventasPorMetodo: ResumenMetodo[];
}

@Injectable({ providedIn: 'root' })
export class CajaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/caja`;

  actual(sucursalId: number) {
    const params = new HttpParams().set('sucursalId', sucursalId);
    return this.http.get<SesionCaja>(`${this.base}/actual`, { params });
  }

  abrir(sucursalId: number, montoApertura: number) {
    return this.http.post<SesionCaja>(`${this.base}/abrir`, { sucursalId, montoApertura });
  }

  movimientos(sesionCajaId: number) {
    return this.http.get<MovimientoCaja[]>(`${this.base}/${sesionCajaId}/movimientos`);
  }

  registrarMovimiento(sesionCajaId: number, tipo: string, concepto: string, monto: number, referencia?: string | null) {
    return this.http.post<MovimientoCaja>(`${this.base}/${sesionCajaId}/movimientos`, { tipo, concepto, monto, referencia });
  }

  cerrar(sesionCajaId: number, montoContado: number) {
    return this.http.post<CierreResultado>(`${this.base}/${sesionCajaId}/cerrar`, { montoContado });
  }

  resumenTurno(sesionCajaId: number) {
    return this.http.get<ResumenTurno>(`${this.base}/${sesionCajaId}/resumen`);
  }
}
