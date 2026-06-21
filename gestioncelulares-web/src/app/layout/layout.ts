import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { filter, map } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { SoundService } from '../core/sound.service';
import { routeFade } from '../shared/animations';

interface ItemMenu {
  etiqueta: string;
  icono: string;
  ruta?: string;       // si está definida, el link funciona; si no, "próximamente"
  soloAdmin?: boolean;
  hijos?: ItemMenu[];  // si tiene hijos, es un grupo desplegable (sin ruta propia)
}

interface GrupoMenu {
  titulo: string;
  items: ItemMenu[];
}

const CLAVE_COLAPSADO = 'gc_sidebar_colapsado';
const CLAVE_EXPANDIDOS = 'gc_sidebar_expandidos';

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideAngularModule],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
  animations: [routeFade]
})
export class Layout {
  auth = inject(AuthService);
  sound = inject(SoundService);
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

  // Menú dividido: lo que ve todo el personal vs. lo exclusivo del administrador
  grupos: GrupoMenu[] = [
    {
      titulo: 'General',
      items: [
        { etiqueta: 'Dashboard', icono: 'layout-dashboard', ruta: '/' },
        { etiqueta: 'POS / Ventas', icono: 'shopping-cart', ruta: '/pos' },
        { etiqueta: 'Ventas', icono: 'clipboard-list', ruta: '/ventas' },
        {
          etiqueta: 'Inventario', icono: 'package', hijos: [
            { etiqueta: 'Existencias', icono: 'boxes', ruta: '/inventario' },
            { etiqueta: 'Faltantes', icono: 'package-x', ruta: '/faltantes' },
            { etiqueta: 'Catálogo', icono: 'layers', ruta: '/catalogo' }
          ]
        },
        { etiqueta: 'Clientes', icono: 'users', ruta: '/clientes' },
        { etiqueta: 'Créditos', icono: 'credit-card', ruta: '/creditos' },
        { etiqueta: 'Apartados', icono: 'bookmark', ruta: '/apartados' },
        { etiqueta: 'Taller', icono: 'wrench', ruta: '/taller' },
        { etiqueta: 'Garantías', icono: 'shield-alert', ruta: '/garantias' },
        { etiqueta: 'Caja', icono: 'wallet', ruta: '/caja' }
      ]
    },
    {
      titulo: 'Administración',
      items: [
        { etiqueta: 'Proveedores', icono: 'truck', ruta: '/proveedores', soloAdmin: true },
        { etiqueta: 'Nómina', icono: 'hand-coins', ruta: '/nomina', soloAdmin: true },
        { etiqueta: 'NCF', icono: 'receipt', ruta: '/ncf', soloAdmin: true },
        { etiqueta: 'Reportes', icono: 'bar-chart-3', ruta: '/reportes', soloAdmin: true },
        { etiqueta: 'Usuarios', icono: 'settings', ruta: '/usuarios', soloAdmin: true }
      ]
    }
  ];

  // Grupos desplegables expandidos manualmente por el usuario (persistido)
  expandidos = signal<Set<string>>(this.leerExpandidos());

  private leerExpandidos(): Set<string> {
    try { return new Set<string>(JSON.parse(localStorage.getItem(CLAVE_EXPANDIDOS) ?? '[]')); }
    catch { return new Set<string>(); }
  }

  alternarGrupo(etiqueta: string): void {
    this.expandidos.update(s => {
      const n = new Set(s);
      n.has(etiqueta) ? n.delete(etiqueta) : n.add(etiqueta);
      localStorage.setItem(CLAVE_EXPANDIDOS, JSON.stringify([...n]));
      return n;
    });
  }

  /// <summary>¿Está abierto el grupo? Abierto si el usuario lo expandió o si contiene la sección activa.</summary>
  grupoAbierto(item: ItemMenu): boolean {
    return this.expandidos().has(item.etiqueta) || this.contieneActivo(item);
  }

  private contieneActivo(item: ItemMenu): boolean {
    const url = this.urlActual().split('?')[0];
    return (item.hijos ?? []).some(h => h.ruta === url || (!!h.ruta && h.ruta !== '/' && url.startsWith(h.ruta)));
  }

  // Lista plana de hojas (para resolver la sección activa desde la URL)
  private get menu(): ItemMenu[] {
    return this.grupos.flatMap(g => g.items.flatMap(i => i.hijos ? i.hijos : [i]));
  }

  // Grupos con sus ítems (e hijos) ya filtrados por permiso; se omiten los vacíos
  get gruposVisibles(): GrupoMenu[] {
    return this.grupos
      .map(g => ({
        titulo: g.titulo,
        items: g.items
          .filter(i => !i.soloAdmin || this.auth.esAdmin())
          .map(i => i.hijos ? { ...i, hijos: i.hijos.filter(h => !h.soloAdmin || this.auth.esAdmin()) } : i)
          .filter(i => !i.hijos || i.hijos.length > 0)
      }))
      .filter(g => g.items.length > 0);
  }

  // Hojas planas de un grupo (para el modo colapsado de la barra)
  hojas(items: ItemMenu[]): ItemMenu[] {
    return items.flatMap(i => i.hijos ? i.hijos : [i]);
  }

  iniciales(nombre: string | undefined): string {
    if (!nombre) return '?';
    return nombre.split(' ').filter(Boolean).slice(0, 2).map(p => p[0]).join('').toUpperCase();
  }
}
