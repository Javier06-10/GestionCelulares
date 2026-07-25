import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

/**
 * La sesión viaja en cookies HttpOnly: este interceptor solo asegura que las
 * peticiones a la API las envíen (withCredentials). Si el access token expira (401),
 * intenta renovar la sesión con la cookie de refresh y reintenta; si falla, cierra sesión.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const esApi = req.url.startsWith(environment.apiUrl);

  const conCredenciales = esApi ? req.clone({ withCredentials: true }) : req;

  return next(conCredenciales).pipe(
    catchError((error: HttpErrorResponse) => {
      const esAuth = req.url.endsWith('/auth/login')
        || req.url.endsWith('/auth/refresh')
        || req.url.endsWith('/auth/logout');

      // Solo interesa el 401 de la API que no venga del propio flujo de auth.
      if (error.status !== 401 || !esApi || esAuth) {
        return throwError(() => error);
      }

      // Renovar (compartido entre 401 simultáneos) y reintentar con las cookies nuevas.
      return auth.refrescar().pipe(
        switchMap(() => next(conCredenciales)),
        catchError(err => {
          auth.logout();
          return throwError(() => err);
        })
      );
    })
  );
};
