import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

export interface ReporteVentas {
  desde: string; hasta: string; cantidad: number;
  subtotal: number; descuento: number; impuesto: number; total: number; ganancia: number;
  porDia: { fecha: string; cantidad: number; total: number; ganancia: number }[];
}

export interface ReporteInventario {
  equipos: number; valorCosto: number; valorVenta: number;
  lineas: { producto: string; marca: string | null; color: string | null; almacenamiento: string | null; sucursalId: number; disponibles: number; precioVenta: number; valorCosto: number }[];
}

export interface ReporteMorosidad {
  clientesEnMora: number; montoVencidoTotal: number;
  clientes: { clienteId: number; cliente: string; telefono: string | null; cuotasVencidas: number; montoVencido: number; diasMaxVencido: number }[];
}

export interface ReporteCaja {
  sesiones: number; totalDiferencias: number;
  detalle: { sesionCajaId: number; sucursalId: number; fechaApertura: string; fechaCierre: string | null; montoApertura: number; montoCierre: number | null; diferencia: number | null; estado: string }[];
}

export interface ReporteTaller {
  entregadas: number; canceladas: number; ingresosPorReparacion: number; costoRepuestos: number; comisionesTecnicos: number;
  porTecnico: { tecnicoId: number; tecnico: string; ordenesEntregadas: number; ingresos: number; comision: number }[];
}

export interface ReporteCobros {
  pagos: number; totalCobrado: number;
  porDia: { fecha: string; pagos: number; total: number }[];
}

export interface Reporte607 {
  periodo: string; rnc: string; cantidad: number;
  totalMontoFacturado: number; totalItbis: number;
  nombreArchivo: string; contenidoTxt: string; sinComprobante: number;
}

export interface Reporte606 {
  periodo: string; rnc: string; cantidad: number;
  totalMontoFacturado: number; totalItbis: number;
  nombreArchivo: string; contenidoTxt: string; sinRnc: number;
}

@Injectable({ providedIn: 'root' })
export class ReporteService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/reportes`;

  private rango(desde: string, hasta: string, sucursalId?: number) {
    let p = new HttpParams().set('desde', desde).set('hasta', hasta);
    if (sucursalId) p = p.set('sucursalId', sucursalId);
    return p;
  }

  ventas(desde: string, hasta: string, sucursalId?: number) {
    return this.http.get<ReporteVentas>(`${this.base}/ventas`, { params: this.rango(desde, hasta, sucursalId) });
  }
  inventario(sucursalId?: number) {
    let p = new HttpParams();
    if (sucursalId) p = p.set('sucursalId', sucursalId);
    return this.http.get<ReporteInventario>(`${this.base}/inventario`, { params: p });
  }
  morosidad() {
    return this.http.get<ReporteMorosidad>(`${this.base}/morosidad`);
  }
  caja(desde: string, hasta: string, sucursalId?: number) {
    return this.http.get<ReporteCaja>(`${this.base}/caja`, { params: this.rango(desde, hasta, sucursalId) });
  }
  taller(desde: string, hasta: string, sucursalId?: number) {
    return this.http.get<ReporteTaller>(`${this.base}/taller`, { params: this.rango(desde, hasta, sucursalId) });
  }
  cobros(desde: string, hasta: string) {
    return this.http.get<ReporteCobros>(`${this.base}/cobros`, { params: this.rango(desde, hasta) });
  }
  reporte607(anio: number, mes: number) {
    const p = new HttpParams().set('anio', anio).set('mes', mes);
    return this.http.get<Reporte607>(`${this.base}/607`, { params: p });
  }
  reporte606(anio: number, mes: number) {
    const p = new HttpParams().set('anio', anio).set('mes', mes);
    return this.http.get<Reporte606>(`${this.base}/606`, { params: p });
  }
}

/** Descarga un archivo de texto plano (ej. el TXT del 607 para la DGII). */
export function descargarTxt(nombre: string, contenido: string): void {
  const blob = new Blob([contenido], { type: 'text/plain;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = nombre;
  a.click();
  URL.revokeObjectURL(url);
}

/** Exporta filas a CSV y dispara la descarga. */
export function exportarCsv(nombre: string, columnas: string[], filas: (string | number)[][]): void {
  const escapar = (v: string | number) => {
    const s = String(v ?? '');
    return /[",\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
  };
  const contenido = [columnas, ...filas].map(f => f.map(escapar).join(',')).join('\n');
  const blob = new Blob(['﻿' + contenido], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${nombre}.csv`;
  a.click();
  URL.revokeObjectURL(url);
}
