import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface ContabilizacionResultado {
  ventas: number;
  nomina: number;
  caja: number;
  total: number;
  mensajes: string[];
}

export interface EstadoResultadosLinea {
  codigo: string;
  nombre: string;
  monto: number;
}

export interface EstadoResultados {
  desde: string;
  hasta: string;
  ingresos: EstadoResultadosLinea[];
  costos: EstadoResultadosLinea[];
  gastos: EstadoResultadosLinea[];
  totalIngresos: number;
  totalCostos: number;
  utilidadBruta: number;
  totalGastos: number;
  utilidadNeta: number;
}

export interface BalanceGeneralLinea {
  codigo: string;
  nombre: string;
  monto: number;
}

export interface BalanceGeneral {
  hasta: string;
  activos: BalanceGeneralLinea[];
  pasivos: BalanceGeneralLinea[];
  capital: BalanceGeneralLinea[];
  totalActivos: number;
  totalPasivos: number;
  capitalAportado: number;
  resultadoEjercicio: number;
  totalCapital: number;
  totalPasivoCapital: number;
  cuadrado: boolean;
}

@Injectable({ providedIn: 'root' })
export class ContabilidadService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/contabilidad`;

  contabilizar(desde: string, hasta: string) {
    const p = new HttpParams().set('desde', desde).set('hasta', hasta);
    return this.http.post<ContabilizacionResultado>(`${this.base}/contabilizar`, {}, { params: p });
  }

  estadoResultados(desde: string, hasta: string) {
    const p = new HttpParams().set('desde', desde).set('hasta', hasta);
    return this.http.get<EstadoResultados>(`${this.base}/estado-resultados`, { params: p });
  }

  balanceGeneral(hasta: string) {
    const p = new HttpParams().set('hasta', hasta);
    return this.http.get<BalanceGeneral>(`${this.base}/balance-general`, { params: p });
  }
}
