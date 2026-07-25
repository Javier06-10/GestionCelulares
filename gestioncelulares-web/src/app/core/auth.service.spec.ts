import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { UsuarioSesion } from './models';

describe('AuthService (sesión por cookies HttpOnly)', () => {
  const base = environment.apiUrl;
  const admin: UsuarioSesion = { usuarioId: 1, nombreUsuario: 'admin', nombreCompleto: 'Administrador', rol: 'Admin', sucursalId: 1 };

  let service: AuthService;
  let http: HttpTestingController;
  let router: { navigate: jasmine.Spy };

  beforeEach(() => {
    localStorage.clear();
    router = { navigate: jasmine.createSpy('navigate') };
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: router }
      ]
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('login guarda el usuario, marca autenticado y envía credenciales', () => {
    let ok = false;
    service.login('admin', 'clave').subscribe(() => (ok = true));

    const req = http.expectOne(`${base}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();   // para que el navegador acepte las cookies
    req.flush({ usuario: admin, expiraEn: '2026-01-01T00:00:00Z' });

    expect(ok).toBeTrue();
    expect(service.estaAutenticado()).toBeTrue();
    expect(service.esAdmin()).toBeTrue();
    expect(service.usuario()?.nombreUsuario).toBe('admin');
  });

  it('no persiste tokens en localStorage (solo el perfil del usuario)', () => {
    service.login('admin', 'clave').subscribe();
    http.expectOne(`${base}/auth/login`).flush({ usuario: admin, expiraEn: 'x' });

    expect(localStorage.getItem('gc_usuario')).toContain('admin');
    expect(localStorage.getItem('gc_token')).toBeNull();     // ya no se guarda el access token
    expect(localStorage.getItem('gc_refresh')).toBeNull();   // ni el refresh token
  });

  it('refrescar comparte una sola llamada entre 401 simultáneos (dedup)', () => {
    service.refrescar().subscribe();
    service.refrescar().subscribe();

    const reqs = http.match(`${base}/auth/refresh`);
    expect(reqs.length).toBe(1);                     // shareReplay: no dispara N refrescos
    expect(reqs[0].request.withCredentials).toBeTrue();
    reqs[0].flush({ usuario: admin, expiraEn: 'x' });
  });

  it('refrescar actualiza el perfil del usuario', () => {
    service.refrescar().subscribe();
    http.expectOne(`${base}/auth/refresh`).flush({ usuario: { ...admin, rol: 'Vendedor' }, expiraEn: 'x' });

    expect(service.esAdmin()).toBeFalse();
    expect(service.usuario()?.rol).toBe('Vendedor');
  });

  it('logout llama al endpoint, limpia la sesión y navega a /login', () => {
    service.login('admin', 'clave').subscribe();
    http.expectOne(`${base}/auth/login`).flush({ usuario: admin, expiraEn: 'x' });
    expect(service.estaAutenticado()).toBeTrue();

    service.logout();
    const req = http.expectOne(`${base}/auth/logout`);
    expect(req.request.method).toBe('POST');
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(service.usuario()).toBeNull();
    expect(localStorage.getItem('gc_usuario')).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('logout limpia la sesión aunque el endpoint falle', () => {
    service.login('admin', 'clave').subscribe();
    http.expectOne(`${base}/auth/login`).flush({ usuario: admin, expiraEn: 'x' });

    service.logout();
    http.expectOne(`${base}/auth/logout`).flush('err', { status: 500, statusText: 'Server Error' });

    expect(service.usuario()).toBeNull();                       // no deja al usuario "atascado" logueado
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});
