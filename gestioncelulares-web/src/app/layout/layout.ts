import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../core/auth.service';

interface ItemMenu {
  etiqueta: string;
  icono: string;
  ruta?: string;       // si está definida, el link funciona; si no, "próximamente"
  soloAdmin?: boolean;
}

const CLAVE_COLAPSADO = 'gc_sidebar_colapsado';

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideAngularModule],
  templateUrl: './layout.html',
  styleUrl: './layout.scss'
})
export class Layout {
  auth = inject(AuthService);

  colapsado = signal(localStorage.getItem(CLAVE_COLAPSADO) === '1');

  constructor() {
    // Persistir la preferencia del usuario
    effect(() => localStorage.setItem(CLAVE_COLAPSADO, this.colapsado() ? '1' : '0'));
  }

  alternar(): void {
    this.colapsado.update(v => !v);
  }

  menu: ItemMenu[] = [
    { etiqueta: 'Dashboard', icono: 'layout-dashboard', ruta: '/' },
    { etiqueta: 'POS / Ventas', icono: 'shopping-cart', ruta: '/pos' },
    { etiqueta: 'Inventario', icono: 'package', ruta: '/inventario' },
    { etiqueta: 'Faltantes', icono: 'package-x', ruta: '/faltantes' },
    { etiqueta: 'Catálogo', icono: 'layers', ruta: '/catalogo' },
    { etiqueta: 'Proveedores', icono: 'truck', ruta: '/proveedores', soloAdmin: true },
    { etiqueta: 'Clientes', icono: 'users', ruta: '/clientes' },
    { etiqueta: 'Créditos', icono: 'credit-card', ruta: '/creditos' },
    { etiqueta: 'Taller', icono: 'wrench', ruta: '/taller' },
    { etiqueta: 'Garantías', icono: 'shield-alert', ruta: '/garantias' },
    { etiqueta: 'Caja', icono: 'wallet', ruta: '/caja' },
    { etiqueta: 'Reportes', icono: 'bar-chart-3', ruta: '/reportes', soloAdmin: true },
    { etiqueta: 'Usuarios', icono: 'settings', ruta: '/usuarios', soloAdmin: true }
  ];

  get items(): ItemMenu[] {
    return this.menu.filter(i => !i.soloAdmin || this.auth.esAdmin());
  }

  iniciales(nombre: string | undefined): string {
    if (!nombre) return '?';
    return nombre.split(' ').filter(Boolean).slice(0, 2).map(p => p[0]).join('').toUpperCase();
  }
}
