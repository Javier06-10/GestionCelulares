import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import { Paginado } from './models';

export interface MetodoPago { metodoPagoId: number; nombre: string; }

export interface VentaResumen {
  ventaId: number;
  numeroFactura: string | null;
  ncf: string | null;
  sucursalId: number;
  cliente: string | null;
  fecha: string;
  total: number;
  esCredito: boolean;
  estado: string;
}

export interface SecuenciaFactura { prefijo: string; proximo: number; longitud: number; }

export interface VentaDetalleRegistro {
  imeiId?: number | null;
  varianteId: number;
  cantidad: number;
  precioUnitario: number;
  descuento: number;
}

export interface VentaRegistro {
  sucursalId: number;
  clienteId?: number | null;
  esCredito: boolean;
  metodoPagoId?: number | null;
  tipoComprobante?: string | null;   // '01' Crédito Fiscal, '02' Consumo
  numeroFactura?: string | null;
  detalles: VentaDetalleRegistro[];
}

export interface Venta {
  ventaId: number;
  numeroFactura: string | null;
  ncf: string | null;
  total: number;
  subtotal: number;
  impuesto: number;
  descuento: number;
  esCredito: boolean;
  estado: string;
}

@Injectable({ providedIn: 'root' })
export class VentaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/ventas`;

  metodosPago() {
    return this.http.get<MetodoPago[]>(`${this.base}/metodos-pago`);
  }

  registrar(dto: VentaRegistro) {
    return this.http.post<Venta>(this.base, dto);
  }

  buscar(desde?: string, hasta?: string, sucursalId?: number, clienteId?: number, pagina = 1, tamano = 50) {
    let p = new HttpParams().set('pagina', pagina).set('tamano', tamano);
    if (desde) p = p.set('desde', desde);
    if (hasta) p = p.set('hasta', hasta);
    if (sucursalId) p = p.set('sucursalId', sucursalId);
    if (clienteId) p = p.set('clienteId', clienteId);
    return this.http.get<Paginado<VentaResumen>>(this.base, { params: p });
  }

  anular(id: number, dto: { tipoAnulacion: string; motivo?: string | null; emitirNotaCredito?: boolean }) {
    return this.http.post<Venta>(`${this.base}/${id}/anular`, dto);
  }

  obtenerSecuencia() {
    return this.http.get<SecuenciaFactura>(`${this.base}/secuencia`);
  }
  guardarSecuencia(dto: SecuenciaFactura) {
    return this.http.put<void>(`${this.base}/secuencia`, dto);
  }
}
