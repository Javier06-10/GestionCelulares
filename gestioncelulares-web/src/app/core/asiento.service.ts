import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface AsientoResumen {
  asientoContableId: number;
  numero: number;
  fecha: string;
  concepto: string;
  origen: string;
  estado: string;
  total: number;
}

export interface AsientoDetalle {
  asientoDetalleId: number;
  cuentaContableId: number;
  cuentaCodigo: string;
  cuentaNombre: string;
  debito: number;
  credito: number;
  descripcion: string | null;
}

export interface Asiento {
  asientoContableId: number;
  numero: number;
  fecha: string;
  concepto: string;
  origen: string;
  estado: string;
  totalDebito: number;
  totalCredito: number;
  detalles: AsientoDetalle[];
}

export interface BalanceLinea {
  cuentaContableId: number;
  codigo: string;
  nombre: string;
  tipo: string;
  debito: number;
  credito: number;
  saldoDeudor: number;
  saldoAcreedor: number;
}

export interface BalanceComprobacion {
  desde: string;
  hasta: string;
  totalDebito: number;
  totalCredito: number;
  totalSaldoDeudor: number;
  totalSaldoAcreedor: number;
  cuadrado: boolean;
  lineas: BalanceLinea[];
}

@Injectable({ providedIn: 'root' })
export class AsientoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/asientos`;

  listar(desde?: string, hasta?: string) {
    let p = new HttpParams();
    if (desde) p = p.set('desde', desde);
    if (hasta) p = p.set('hasta', hasta);
    return this.http.get<AsientoResumen[]>(this.base, { params: p });
  }
  porId(id: number) { return this.http.get<Asiento>(`${this.base}/${id}`); }
  crear(dto: { fecha: string; concepto: string; detalles: { cuentaContableId: number; debito: number; credito: number; descripcion?: string | null }[] }) {
    return this.http.post<Asiento>(this.base, dto);
  }
  anular(id: number) { return this.http.post<void>(`${this.base}/${id}/anular`, {}); }
  balanceComprobacion(desde: string, hasta: string) {
    const p = new HttpParams().set('desde', desde).set('hasta', hasta);
    return this.http.get<BalanceComprobacion>(`${this.base}/balance-comprobacion`, { params: p });
  }
}
