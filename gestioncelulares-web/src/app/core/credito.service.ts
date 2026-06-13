import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface CreditoResumen {
  creditoId: number;
  clienteId: number;
  cliente: string;
  montoTotal: number;
  saldo: number;
  numeroCuotas: number;
  estado: string;
  fechaInicio: string;
}

export interface Cuota {
  cuotaId: number;
  numeroCuota: number;
  fechaVencimiento: string;
  montoCuota: number;
  montoPagado: number;
  saldo: number;
  estado: string;
}

export interface Credito {
  creditoId: number;
  ventaId: number | null;
  clienteId: number;
  cliente: string;
  montoFinanciado: number;
  inicial: number;
  tasaInteres: number;
  numeroCuotas: number;
  montoTotal: number;
  saldo: number;
  estado: string;
  fechaInicio: string;
  cuotas: Cuota[];
}

export interface PagoCredito {
  pagoCreditoId: number;
  creditoId: number;
  cuotaId: number | null;
  metodoPago: string | null;
  monto: number;
  numeroRecibo: string | null;
  fecha: string;
}

export interface AbonoResultado {
  pagoCreditoId: number;
  creditoId: number;
  montoAbonado: number;
  saldoCredito: number;
  estadoCredito: string;
}

export interface CuotaVencida {
  cuotaId: number;
  creditoId: number;
  clienteId: number;
  cliente: string;
  telefono: string | null;
  numeroCuota: number;
  fechaVencimiento: string;
  montoCuota: number;
  montoPagado: number;
  saldo: number;
  diasVencido: number;
}

export interface CreditoCrear {
  clienteId: number;
  montoFinanciado: number;
  numeroCuotas: number;
  fechaPrimerVencimiento: string;
  ventaId?: number | null;
  inicial: number;
  tasaInteresMensual: number;
}

export interface AbonoRegistro {
  monto: number;
  metodoPagoId?: number | null;
  sesionCajaId?: number | null;
  cuotaId?: number | null;
  numeroRecibo?: string | null;
}

@Injectable({ providedIn: 'root' })
export class CreditoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/creditos`;

  buscar(clienteId?: number, estado?: string) {
    let params = new HttpParams();
    if (clienteId) params = params.set('clienteId', clienteId);
    if (estado) params = params.set('estado', estado);
    return this.http.get<CreditoResumen[]>(this.base, { params });
  }

  porId(id: number) {
    return this.http.get<Credito>(`${this.base}/${id}`);
  }

  crear(dto: CreditoCrear) {
    return this.http.post<Credito>(this.base, dto);
  }

  abonos(creditoId: number) {
    return this.http.get<PagoCredito[]>(`${this.base}/${creditoId}/abonos`);
  }

  registrarAbono(creditoId: number, dto: AbonoRegistro) {
    return this.http.post<AbonoResultado>(`${this.base}/${creditoId}/abonos`, dto);
  }

  cuotasVencidas() {
    return this.http.get<CuotaVencida[]>(`${this.base}/cuotas-vencidas`);
  }

  actualizarMora() {
    return this.http.post<unknown>(`${this.base}/actualizar-mora`, {});
  }
}
