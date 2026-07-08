import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export type CondicionPago = 'Contado' | 'Credito' | 'Acuerdo';

export interface Proveedor {
  proveedorId: number;
  nombre: string;
  rnc: string | null;
  telefono: string | null;
  email: string | null;
  direccion: string | null;
  balance: number;
  activo: boolean;
  fechaCreacion: string;
  condicionPago: CondicionPago;
  diasCredito: number;
  notaCondicion: string | null;
}

export interface ProveedorGuardar {
  nombre: string;
  rnc?: string | null;
  telefono?: string | null;
  email?: string | null;
  direccion?: string | null;
  activo?: boolean;
  condicionPago?: CondicionPago;
  diasCredito?: number;
  notaCondicion?: string | null;
}

export interface Compra {
  compraId: number;
  proveedorId: number;
  sucursalId: number;
  numeroFactura: string | null;
  fecha: string;
  total: number;
  itbis: number | null;
  notas: string | null;
  contado: boolean;
}

export interface Pago {
  pagoProveedorId: number;
  proveedorId: number;
  compraId: number | null;
  monto: number;
  fecha: string;
  referencia: string | null;
  balanceProveedor?: number;
}

@Injectable({ providedIn: 'root' })
export class ProveedorService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/proveedores`;

  buscar(termino?: string, activos?: boolean) {
    let params = new HttpParams();
    if (termino) params = params.set('buscar', termino);
    if (activos !== undefined) params = params.set('activos', activos);
    return this.http.get<Proveedor[]>(this.base, { params });
  }
  porId(id: number) { return this.http.get<Proveedor>(`${this.base}/${id}`); }
  crear(dto: ProveedorGuardar) { return this.http.post<Proveedor>(this.base, dto); }
  actualizar(id: number, dto: ProveedorGuardar) { return this.http.put<Proveedor>(`${this.base}/${id}`, dto); }

  compras(id: number) { return this.http.get<Compra[]>(`${this.base}/${id}/compras`); }
  registrarCompra(id: number, dto: { sucursalId: number; numeroFactura?: string | null; total: number; itbis: number; tipoBienServicio?: string | null; metodoPagoId?: number | null; notas?: string | null; contado: boolean }) {
    return this.http.post<Compra>(`${this.base}/${id}/compras`, dto);
  }
  pagos(id: number) { return this.http.get<Pago[]>(`${this.base}/${id}/pagos`); }
  registrarPago(id: number, dto: { monto: number; compraId?: number | null; referencia?: string | null }) {
    return this.http.post<Pago>(`${this.base}/${id}/pagos`, dto);
  }
}
