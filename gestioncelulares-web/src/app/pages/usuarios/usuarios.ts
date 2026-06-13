import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { Rol, Usuario, UsuarioService } from '../../core/usuario.service';

@Component({
  selector: 'app-usuarios',
  imports: [ReactiveFormsModule, LucideAngularModule, DatePipe],
  templateUrl: './usuarios.html'
})
export class Usuarios {
  private fb = inject(FormBuilder);
  private servicio = inject(UsuarioService);
  auth = inject(AuthService);

  usuarios = signal<Usuario[]>([]);
  roles = signal<Rol[]>([]);
  cargando = signal(false);

  // Modal crear / editar
  modal = signal(false);
  editando = signal<Usuario | null>(null);
  guardando = signal(false);
  errorForm = signal<string | null>(null);
  form = this.fb.nonNullable.group({
    nombreUsuario: ['', [Validators.required, Validators.minLength(3)]],
    nombreCompleto: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', Validators.email],
    contrasena: ['', [Validators.minLength(8)]],
    rolId: [0, [Validators.required, Validators.min(1)]],
    activo: [true]
  });

  // Modal reset contraseña
  modalReset = signal(false);
  usuarioReset = signal<Usuario | null>(null);
  nuevaClave = signal('');
  errorReset = signal<string | null>(null);
  okReset = signal(false);

  constructor() {
    this.cargar();
    this.servicio.roles().subscribe(r => this.roles.set(r));
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar().subscribe({
      next: u => { this.usuarios.set(u); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  abrirNuevo(): void {
    this.editando.set(null);
    this.errorForm.set(null);
    this.form.reset({ nombreUsuario: '', nombreCompleto: '', email: '', contrasena: '', rolId: 0, activo: true });
    this.form.controls.nombreUsuario.enable();
    this.form.controls.contrasena.addValidators(Validators.required);
    this.form.controls.contrasena.updateValueAndValidity();
    this.modal.set(true);
  }

  abrirEditar(u: Usuario): void {
    this.editando.set(u);
    this.errorForm.set(null);
    this.form.reset({
      nombreUsuario: u.nombreUsuario,
      nombreCompleto: u.nombreCompleto,
      email: u.email ?? '',
      contrasena: '',
      rolId: u.rolId,
      activo: u.activo
    });
    this.form.controls.nombreUsuario.disable();
    this.form.controls.contrasena.removeValidators(Validators.required);
    this.form.controls.contrasena.updateValueAndValidity();
    this.modal.set(true);
  }

  guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();
    const sucursalId = this.auth.usuario()?.sucursalId ?? null;

    if (this.editando()) {
      this.servicio.actualizar(this.editando()!.usuarioId, {
        nombreCompleto: v.nombreCompleto.trim(),
        email: v.email?.trim() || null,
        rolId: Number(v.rolId),
        sucursalId,
        activo: v.activo
      }).subscribe({
        next: () => { this.guardando.set(false); this.modal.set(false); this.cargar(); },
        error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo guardar.'); }
      });
    } else {
      this.servicio.crear({
        nombreUsuario: v.nombreUsuario.trim(),
        nombreCompleto: v.nombreCompleto.trim(),
        email: v.email?.trim() || null,
        contrasena: v.contrasena,
        rolId: Number(v.rolId),
        sucursalId
      }).subscribe({
        next: () => { this.guardando.set(false); this.modal.set(false); this.cargar(); },
        error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo crear el usuario.'); }
      });
    }
  }

  // ----- Reset contraseña -----
  abrirReset(u: Usuario): void {
    this.usuarioReset.set(u);
    this.nuevaClave.set('');
    this.errorReset.set(null);
    this.okReset.set(false);
    this.modalReset.set(true);
  }

  confirmarReset(): void {
    const u = this.usuarioReset();
    if (!u || this.nuevaClave().length < 8) { this.errorReset.set('La contraseña debe tener al menos 8 caracteres.'); return; }
    this.servicio.resetContrasena(u.usuarioId, this.nuevaClave()).subscribe({
      next: () => { this.okReset.set(true); },
      error: err => this.errorReset.set(err.error?.error ?? 'No se pudo restablecer.')
    });
  }

  rolClase(rol: string): string {
    switch (rol) {
      case 'Admin': return 'bg-tech-accent/10 text-tech-accent';
      case 'Tecnico': return 'bg-tech-purple/10 text-tech-purple';
      case 'Vendedor': return 'bg-tech-sand/30 text-amber-700';
      default: return 'bg-slate-400/15 text-slate-500';
    }
  }
}
