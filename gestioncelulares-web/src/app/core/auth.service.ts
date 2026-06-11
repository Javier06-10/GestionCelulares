import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse, UsuarioSesion } from './models';

const TOKEN_KEY = 'gc_token';
const USUARIO_KEY = 'gc_usuario';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  readonly usuario = signal<UsuarioSesion | null>(this.leerUsuario());
  readonly estaAutenticado = computed(() => this.usuario() !== null);
  readonly esAdmin = computed(() => this.usuario()?.rol === 'Admin');

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  login(nombreUsuario: string, contrasena: string) {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, { nombreUsuario, contrasena })
      .pipe(
        tap(resp => {
          localStorage.setItem(TOKEN_KEY, resp.accessToken);
          localStorage.setItem(USUARIO_KEY, JSON.stringify(resp.usuario));
          this.usuario.set(resp.usuario);
        })
      );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USUARIO_KEY);
    this.usuario.set(null);
    this.router.navigate(['/login']);
  }

  private leerUsuario(): UsuarioSesion | null {
    const json = localStorage.getItem(USUARIO_KEY);
    return json ? (JSON.parse(json) as UsuarioSesion) : null;
  }
}
