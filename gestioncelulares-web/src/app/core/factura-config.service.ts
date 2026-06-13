import { Injectable, signal } from '@angular/core';

export interface FacturaConfig {
  nombreComercial: string;
  rnc: string;
  direccion: string;
  telefono: string;
  mensajePie: string;
  // Qué mostrar en la factura
  mostrarRnc: boolean;
  mostrarCliente: boolean;
  mostrarVendedor: boolean;
  mostrarItbis: boolean;
  mostrarImei: boolean;
  anchoTicket: '58mm' | '80mm' | 'carta';
}

const CLAVE = 'gc_factura_config';

const POR_DEFECTO: FacturaConfig = {
  nombreComercial: 'Celulares 360',
  rnc: '',
  direccion: '',
  telefono: '',
  mensajePie: '¡Gracias por su compra!',
  mostrarRnc: true,
  mostrarCliente: true,
  mostrarVendedor: true,
  mostrarItbis: true,
  mostrarImei: true,
  anchoTicket: '80mm'
};

@Injectable({ providedIn: 'root' })
export class FacturaConfigService {
  readonly config = signal<FacturaConfig>(this.leer());

  private leer(): FacturaConfig {
    const json = localStorage.getItem(CLAVE);
    return json ? { ...POR_DEFECTO, ...JSON.parse(json) } : { ...POR_DEFECTO };
  }

  guardar(cfg: FacturaConfig): void {
    localStorage.setItem(CLAVE, JSON.stringify(cfg));
    this.config.set(cfg);
  }
}
