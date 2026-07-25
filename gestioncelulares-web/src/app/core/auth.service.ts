import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, of, shareReplay, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { UsuarioSesion } from './models';

const USUARIO_KEY = 'gc_usuario';

/** La sesión vive en cookies HttpOnly (no accesibles desde JS). Aquí solo se guarda el
 *  perfil del usuario para pintar la UI; la autenticación real la llevan las cookies. */
interface SesionResponse { usuario: UsuarioSesion; expiraEn: string; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  readonly usuario = signal<UsuarioSesion | null>(this.leerUsuario());
  readonly estaAutenticado = computed(() => this.usuario() !== null);
  readonly esAdmin = computed(() => this.usuario()?.rol === 'Admin');

  // Refresh en curso compartido: varios 401 simultáneos comparten una sola llamada.
  private refresh$: Observable<SesionResponse> | null = null;

  login(nombreUsuario: string, contrasena: string) {
    // withCredentials para que el navegador acepte/envíe las cookies de sesión.
    return this.http
      .post<SesionResponse>(`${environment.apiUrl}/auth/login`, { nombreUsuario, contrasena }, { withCredentials: true })
      .pipe(tap(resp => this.guardarUsuario(resp.usuario)));
  }

  /** Renueva la sesión usando la cookie de refresh. Deduplica llamadas concurrentes. */
  refrescar(): Observable<SesionResponse> {
    if (this.refresh$) return this.refresh$;

    this.refresh$ = this.http
      .post<SesionResponse>(`${environment.apiUrl}/auth/refresh`, {}, { withCredentials: true })
      .pipe(
        tap(resp => this.guardarUsuario(resp.usuario)),
        shareReplay(1),
        finalize(() => (this.refresh$ = null))
      );
    return this.refresh$;
  }

  logout(): void {
    // Revoca el refresh token y borra las cookies del lado servidor; luego limpia la UI.
    this.http.post(`${environment.apiUrl}/auth/logout`, {}, { withCredentials: true })
      .pipe(catchError(() => of(null)))
      .subscribe(() => this.limpiarSesion());
  }

  private limpiarSesion(): void {
    localStorage.removeItem(USUARIO_KEY);
    this.usuario.set(null);
    this.router.navigate(['/login']);
  }

  private guardarUsuario(usuario: UsuarioSesion): void {
    localStorage.setItem(USUARIO_KEY, JSON.stringify(usuario));
    this.usuario.set(usuario);
  }

  private leerUsuario(): UsuarioSesion | null {
    const json = localStorage.getItem(USUARIO_KEY);
    return json ? (JSON.parse(json) as UsuarioSesion) : null;
  }
}
