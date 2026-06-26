import { Injectable, signal } from '@angular/core';

/**
 * Estado de interfaz compartido. El "modo kiosko" oculta la barra lateral y el
 * encabezado para que una pantalla (p. ej. el POS) ocupe todo el monitor, y pide
 * pantalla completa real al navegador.
 */
@Injectable({ providedIn: 'root' })
export class UiService {
  readonly kiosko = signal(false);

  constructor() {
    // Si el usuario sale de pantalla completa (Esc), también salimos del modo kiosko
    document.addEventListener('fullscreenchange', () => {
      if (!document.fullscreenElement && this.kiosko()) this.kiosko.set(false);
    });
  }

  entrar(): void {
    this.kiosko.set(true);
    const el = document.documentElement;
    if (el.requestFullscreen) el.requestFullscreen().catch(() => { /* el navegador puede bloquearlo */ });
  }

  salir(): void {
    this.kiosko.set(false);
    if (document.fullscreenElement && document.exitFullscreen) {
      document.exitFullscreen().catch(() => { /* ignore */ });
    }
  }

  alternar(): void {
    this.kiosko() ? this.salir() : this.entrar();
  }
}
