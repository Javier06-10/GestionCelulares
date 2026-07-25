import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';
import { rolGuard } from './core/rol-guard';

// Carga diferida (lazy) de cada pantalla: cada ruta baja su propio chunk bajo demanda,
// en vez de meter las 22 páginas en el bundle inicial.
export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login').then(m => m.Login) },
  {
    path: '',
    loadComponent: () => import('./layout/layout').then(m => m.Layout),
    canActivate: [authGuard],
    canActivateChild: [rolGuard],
    children: [
      { path: '', loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.Dashboard) },
      { path: 'pos', loadComponent: () => import('./pages/pos/pos').then(m => m.Pos) },
      { path: 'ventas', loadComponent: () => import('./pages/ventas/ventas').then(m => m.Ventas) },
      { path: 'devoluciones', loadComponent: () => import('./pages/devoluciones/devoluciones').then(m => m.Devoluciones) },
      { path: 'clientes', loadComponent: () => import('./pages/clientes/clientes').then(m => m.Clientes) },
      { path: 'inventario', loadComponent: () => import('./pages/inventario/inventario').then(m => m.Inventario) },
      { path: 'faltantes', redirectTo: 'inventario', pathMatch: 'full' },
      { path: 'catalogo', loadComponent: () => import('./pages/catalogo/catalogo').then(m => m.Catalogo) },
      { path: 'proveedores', loadComponent: () => import('./pages/proveedores/proveedores').then(m => m.Proveedores) },
      { path: 'caja', loadComponent: () => import('./pages/caja/caja').then(m => m.Caja) },
      { path: 'creditos', loadComponent: () => import('./pages/creditos/creditos').then(m => m.Creditos) },
      { path: 'taller', loadComponent: () => import('./pages/taller/taller').then(m => m.Taller) },
      { path: 'apartados', loadComponent: () => import('./pages/apartados/apartados').then(m => m.Apartados) },
      { path: 'garantias', loadComponent: () => import('./pages/garantias/garantias').then(m => m.Garantias) },
      { path: 'usuarios', loadComponent: () => import('./pages/usuarios/usuarios').then(m => m.Usuarios) },
      { path: 'nomina', loadComponent: () => import('./pages/nomina/nomina').then(m => m.Nomina) },
      { path: 'cuentas', loadComponent: () => import('./pages/cuentas/cuentas').then(m => m.Cuentas) },
      { path: 'asientos', loadComponent: () => import('./pages/asientos/asientos').then(m => m.Asientos) },
      { path: 'estados', loadComponent: () => import('./pages/estados/estados').then(m => m.Estados) },
      { path: 'ncf', loadComponent: () => import('./pages/ncf/ncf').then(m => m.Ncf) },
      { path: 'reportes', loadComponent: () => import('./pages/reportes/reportes').then(m => m.Reportes) }
    ]
  },
  { path: '**', redirectTo: '' }
];
