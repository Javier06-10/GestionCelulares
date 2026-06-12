import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../core/auth.service';

interface ItemMenu {
  etiqueta: string;
  icono: string;
  ruta?: string;       // si está definida, el link funciona; si no, "próximamente"
  soloAdmin?: boolean;
}

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideAngularModule],
  templateUrl: './layout.html',
  styleUrl: './layout.scss'
})
export class Layout {
  auth = inject(AuthService);

  menu: ItemMenu[] = [
    { etiqueta: 'Dashboard', icono: 'layout-dashboard', ruta: '/' },
    { etiqueta: 'POS / Ventas', icono: 'shopping-cart' },
    { etiqueta: 'Inventario', icono: 'package', ruta: '/inventario' },
    { etiqueta: 'Clientes', icono: 'users', ruta: '/clientes' },
    { etiqueta: 'Créditos', icono: 'credit-card' },
    { etiqueta: 'Taller', icono: 'wrench' },
    { etiqueta: 'Caja', icono: 'wallet' },
    { etiqueta: 'Reportes', icono: 'bar-chart-3', soloAdmin: true },
    { etiqueta: 'Usuarios', icono: 'settings', soloAdmin: true }
  ];

  get items(): ItemMenu[] {
    return this.menu.filter(i => !i.soloAdmin || this.auth.esAdmin());
  }

  iniciales(nombre: string | undefined): string {
    if (!nombre) return '?';
    return nombre.split(' ').filter(Boolean).slice(0, 2).map(p => p[0]).join('').toUpperCase();
  }
}
