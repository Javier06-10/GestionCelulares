import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { filter, map } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { routeFade } from '../shared/animations';

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
  styleUrl: './layout.scss',
  animations: [routeFade]
})
export class Layout {
  auth = inject(AuthService);
  private router = inject(Router);

  colapsado = signal(localStorage.getItem(CLAVE_COLAPSADO) === '1');

  // URL actual, reactiva ante cada navegación
  private urlActual = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(() => this.router.url)
    ),
    { initialValue: this.router.url }
  );

  // Sección activa (para mostrar su nombre/ícono en el header)
  seccionActual = computed<ItemMenu>(() => {
    const url = this.urlActual().split('?')[0];
    return this.menu.find(i =>
      i.ruta === url || (i.ruta && i.ruta !== '/' && url.startsWith(i.ruta))
    ) ?? this.menu[0];
  });

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
