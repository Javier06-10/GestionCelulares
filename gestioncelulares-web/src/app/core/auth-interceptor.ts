import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

/**
 * Adjunta el JWT a las llamadas a la API. Si expira (401), intenta renovar el
 * token con el refresh token y reintenta la petición; si la renovación falla,
 * cierra la sesión.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const conToken = (r: HttpRequest<unknown>) =>
    auth.token && r.url.startsWith(environment.apiUrl)
      ? r.clone({ setHeaders: { Authorization: `Bearer ${auth.token}` } })
      : r;

  return next(conToken(req)).pipe(
    catchError((error: HttpErrorResponse) => {
      const esApi = req.url.startsWith(environment.apiUrl);
      const esAuth = req.url.endsWith('/auth/login') || req.url.endsWith('/auth/refresh');

      // Solo interesa el 401 de la API que no venga del propio login/refresh.
      if (error.status !== 401 || !esApi || esAuth) {
        return throwError(() => error);
      }

      // Sin refresh token no hay nada que renovar: cerrar sesión.
      if (!auth.refreshToken) {
        auth.logout();
        return throwError(() => error);
      }

      // Renovar (compartido entre 401 simultáneos) y reintentar con el token nuevo.
      return auth.refrescar().pipe(
        switchMap(() => next(conToken(req))),
        catchError(err => {
          auth.logout();
          return throwError(() => err);
        })
      );
    })
  );
};
