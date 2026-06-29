import { inject } from '@angular/core';
import { CanActivateChildFn, Router } from '@angular/router';
import { puedeAcceder } from './acceso';
import { AuthService } from './auth.service';

/**
 * Bloquea la navegación a una pantalla si el rol del usuario no tiene acceso,
 * tanto desde el menú como escribiendo la URL. Redirige al Dashboard.
 */
export const rolGuard: CanActivateChildFn = (childRoute) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const path = childRoute.routeConfig?.path ?? '';
  const rol = auth.usuario()?.rol;

  return puedeAcceder(rol, path) ? true : router.createUrlTree(['/']);
};
