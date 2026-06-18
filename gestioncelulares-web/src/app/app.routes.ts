import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';
import { Layout } from './layout/layout';
import { Apartados } from './pages/apartados/apartados';
import { Caja } from './pages/caja/caja';
import { Catalogo } from './pages/catalogo/catalogo';
import { Clientes } from './pages/clientes/clientes';
import { Creditos } from './pages/creditos/creditos';
import { Dashboard } from './pages/dashboard/dashboard';
import { Faltantes } from './pages/faltantes/faltantes';
import { Garantias } from './pages/garantias/garantias';
import { Inventario } from './pages/inventario/inventario';
import { Login } from './pages/login/login';
import { Ncf } from './pages/ncf/ncf';
import { Nomina } from './pages/nomina/nomina';
import { Pos } from './pages/pos/pos';
import { Proveedores } from './pages/proveedores/proveedores';
import { Reportes } from './pages/reportes/reportes';
import { Taller } from './pages/taller/taller';
import { Usuarios } from './pages/usuarios/usuarios';

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
      { path: 'faltantes', component: Faltantes },
      { path: 'catalogo', component: Catalogo },
      { path: 'proveedores', component: Proveedores },
      { path: 'caja', component: Caja },
      { path: 'creditos', component: Creditos },
      { path: 'taller', component: Taller },
      { path: 'apartados', component: Apartados },
      { path: 'garantias', component: Garantias },
      { path: 'usuarios', component: Usuarios },
      { path: 'nomina', component: Nomina },
      { path: 'ncf', component: Ncf },
      { path: 'reportes', component: Reportes }
    ]
  },
  { path: '**', redirectTo: '' }
];
