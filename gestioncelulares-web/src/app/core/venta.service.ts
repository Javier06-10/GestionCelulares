import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface MetodoPago { metodoPagoId: number; nombre: string; }

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
  numeroFactura?: string | null;
  detalles: VentaDetalleRegistro[];
}

export interface Venta {
  ventaId: number;
  numeroFactura: string | null;
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
}
