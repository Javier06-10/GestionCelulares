import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse, UsuarioSesion } from './models';

const TOKEN_KEY = 'gc_token';
const REFRESH_KEY = 'gc_refresh';
const USUARIO_KEY = 'gc_usuario';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  readonly usuario = signal<UsuarioSesion | null>(this.leerUsuario());
  readonly estaAutenticado = computed(() => this.usuario() !== null);
  readonly esAdmin = computed(() => this.usuario()?.rol === 'Admin');

  // Refresh en curso compartido: varios 401 simultáneos comparten una sola llamada.
  private refresh$: Observable<LoginResponse> | null = null;

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  login(nombreUsuario: string, contrasena: string) {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, { nombreUsuario, contrasena })
      .pipe(tap(resp => this.guardarSesion(resp)));
  }

  /**
   * Renueva el access token con el refresh token guardado. El backend rota ambos
   * tokens. Deduplica llamadas concurrentes con shareReplay para no disparar N refrescos.
   */
  refrescar(): Observable<LoginResponse> {
    if (this.refresh$) return this.refresh$;

    const rt = this.refreshToken;
    if (!rt) return throwError(() => new Error('No hay refresh token.'));

    this.refresh$ = this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/refresh`, { refreshToken: rt })
      .pipe(
        tap(resp => this.guardarSesion(resp)),
        shareReplay(1),
        finalize(() => (this.refresh$ = null))
      );
    return this.refresh$;
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USUARIO_KEY);
    this.usuario.set(null);
    this.router.navigate(['/login']);
  }

  private guardarSesion(resp: LoginResponse): void {
    localStorage.setItem(TOKEN_KEY, resp.accessToken);
    localStorage.setItem(REFRESH_KEY, resp.refreshToken);
    localStorage.setItem(USUARIO_KEY, JSON.stringify(resp.usuario));
    this.usuario.set(resp.usuario);
  }

  private leerUsuario(): UsuarioSesion | null {
    const json = localStorage.getItem(USUARIO_KEY);
    return json ? (JSON.parse(json) as UsuarioSesion) : null;
  }
}
