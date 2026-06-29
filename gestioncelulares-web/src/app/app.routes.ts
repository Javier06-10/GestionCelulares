import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';
import { rolGuard } from './core/rol-guard';
import { Layout } from './layout/layout';
import { Apartados } from './pages/apartados/apartados';
import { Asientos } from './pages/asientos/asientos';
import { Caja } from './pages/caja/caja';
import { Catalogo } from './pages/catalogo/catalogo';
import { Clientes } from './pages/clientes/clientes';
import { Creditos } from './pages/creditos/creditos';
import { Cuentas } from './pages/cuentas/cuentas';
import { Dashboard } from './pages/dashboard/dashboard';
import { Estados } from './pages/estados/estados';
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
import { Ventas } from './pages/ventas/ventas';

export const routes: Routes = [
  { path: 'login', component: Login },
  {
    path: '',
    component: Layout,
    canActivate: [authGuard],
    canActivateChild: [rolGuard],
    children: [
      { path: '', component: Dashboard },
      { path: 'pos', component: Pos },
      { path: 'ventas', component: Ventas },
      { path: 'clientes', component: Clientes },
      { path: 'inventario', component: Inventario },
      { path: 'faltantes', redirectTo: 'inventario', pathMatch: 'full' },
      { path: 'catalogo', component: Catalogo },
      { path: 'proveedores', component: Proveedores },
      { path: 'caja', component: Caja },
      { path: 'creditos', component: Creditos },
      { path: 'taller', component: Taller },
      { path: 'apartados', component: Apartados },
      { path: 'garantias', component: Garantias },
      { path: 'usuarios', component: Usuarios },
      { path: 'nomina', component: Nomina },
      { path: 'cuentas', component: Cuentas },
      { path: 'asientos', component: Asientos },
      { path: 'estados', component: Estados },
      { path: 'ncf', component: Ncf },
      { path: 'reportes', component: Reportes }
    ]
  },
  { path: '**', redirectTo: '' }
];
