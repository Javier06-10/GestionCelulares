import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';
import { Layout } from './layout/layout';
import { Caja } from './pages/caja/caja';
import { Catalogo } from './pages/catalogo/catalogo';
import { Clientes } from './pages/clientes/clientes';
import { Creditos } from './pages/creditos/creditos';
import { Dashboard } from './pages/dashboard/dashboard';
import { Inventario } from './pages/inventario/inventario';
import { Login } from './pages/login/login';
import { Pos } from './pages/pos/pos';

export const routes: Routes = [
  { path: 'login', component: Login },
  {
    path: '',
    component: Layout,
    canActivate: [authGuard],
    children: [
      { path: '', component: Dashboard },
      { path: 'pos', component: Pos },
      { path: 'clientes', component: Clientes },
      { path: 'inventario', component: Inventario },
      { path: 'catalogo', component: Catalogo },
      { path: 'caja', component: Caja },
      { path: 'creditos', component: Creditos }
      // Pendientes: taller, reportes, usuarios
    ]
  },
  { path: '**', redirectTo: '' }
];
