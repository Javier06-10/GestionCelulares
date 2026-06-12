import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';
import { Layout } from './layout/layout';
import { Clientes } from './pages/clientes/clientes';
import { Dashboard } from './pages/dashboard/dashboard';
import { Inventario } from './pages/inventario/inventario';
import { Login } from './pages/login/login';

export const routes: Routes = [
  { path: 'login', component: Login },
  {
    path: '',
    component: Layout,
    canActivate: [authGuard],
    children: [
      { path: '', component: Dashboard },
      { path: 'clientes', component: Clientes },
      { path: 'inventario', component: Inventario }
      // Pendientes: pos, creditos, taller, caja, reportes, usuarios
    ]
  },
  { path: '**', redirectTo: '' }
];
