import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

/** Nota de crédito (devolución) emitida. */
export interface NotaCredito {
  notaCreditoId: number;
  ncf: string | null;
  ncfModificado: string | null;
  ventaId: number;
  numeroFactura: string | null;
  cliente: string | null;
  monto: number;
  itbis: number;
  total: number;
  motivo: string | null;
  fechaRegistro: string;
}

@Injectable({ providedIn: 'root' })
export class DevolucionService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/devoluciones`;

  /** Lista todas las notas de crédito emitidas. */
  listar() {
    return this.http.get<NotaCredito[]>(this.base);
  }

  /** Registra una devolución: emite la NC (04) y revierte el inventario. */
  crear(dto: { ventaId: number; motivo?: string | null }) {
    return this.http.post<NotaCredito>(this.base, dto);
  }
}
