import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, LucideAngularModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  cargando = signal(false);
  error = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    nombreUsuario: ['', Validators.required],
    contrasena: ['', Validators.required]
  });

  entrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.cargando.set(true);
    this.error.set(null);

    const { nombreUsuario, contrasena } = this.form.getRawValue();
    this.auth.login(nombreUsuario, contrasena).subscribe({
      next: () => this.router.navigate(['/']),
      error: err => {
        this.cargando.set(false);
        this.error.set(err.error?.error ?? 'No se pudo conectar con el servidor.');
      }
    });
  }
}
