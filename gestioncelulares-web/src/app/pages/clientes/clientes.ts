import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { Cliente, ClienteService } from '../../core/cliente.service';

type Filtro = 'todos' | 'aldia' | 'morosos' | 'bloqueados';

@Component({
  selector: 'app-clientes',
  imports: [ReactiveFormsModule, LucideAngularModule, DatePipe],
  templateUrl: './clientes.html'
})
export class Clientes {
  private fb = inject(FormBuilder);
  private servicio = inject(ClienteService);
  auth = inject(AuthService);

  clientes = signal<Cliente[]>([]);
  cargando = signal(false);
  termino = signal('');
  filtro = signal<Filtro>('todos');

  // Filtrado en cliente: búsqueda instantánea + filtro por estado
  clientesVista = computed<Cliente[]>(() => {
    const q = this.termino().toLowerCase().trim();
    const f = this.filtro();
    return this.clientes().filter(c => {
      if (f === 'morosos' && !c.esMoroso) return false;
      if (f === 'bloqueados' && !c.bloqueado) return false;
      if (f === 'aldia' && (c.esMoroso || c.bloqueado)) return false;
      if (!q) return true;
      return `${c.nombre} ${c.cedula ?? ''} ${c.telefono ?? ''} ${c.email ?? ''}`.toLowerCase().includes(q);
    });
  });

  // KPIs (sobre el total, no la vista filtrada)
  kpiTotal = computed(() => this.clientes().length);
  kpiAlDia = computed(() => this.clientes().filter(c => !c.esMoroso && !c.bloqueado).length);
  kpiMorosos = computed(() => this.clientes().filter(c => c.esMoroso).length);
  kpiBloqueados = computed(() => this.clientes().filter(c => c.bloqueado).length);

  // Estado del modal
  modalAbierto = signal(false);
  editando = signal<Cliente | null>(null);
  guardando = signal(false);
  errorForm = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    cedula: ['', Validators.maxLength(20)],
    telefono: ['', Validators.maxLength(30)],
    email: ['', [Validators.email, Validators.maxLength(150)]],
    direccion: ['', Validators.maxLength(250)]
  });

  tituloModal = computed(() => (this.editando() ? 'Editar cliente' : 'Nuevo cliente'));

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.buscar().subscribe({
      next: data => {
        this.clientes.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  buscar(valor: string): void {
    this.termino.set(valor);
  }

  cambiarFiltro(f: Filtro): void {
    this.filtro.set(f);
  }

  abrirNuevo(): void {
    this.editando.set(null);
    this.errorForm.set(null);
    this.form.reset({ nombre: '', cedula: '', telefono: '', email: '', direccion: '' });
    this.modalAbierto.set(true);
  }

  abrirEditar(c: Cliente): void {
    this.editando.set(c);
    this.errorForm.set(null);
    this.form.reset({
      nombre: c.nombre,
      cedula: c.cedula ?? '',
      telefono: c.telefono ?? '',
      email: c.email ?? '',
      direccion: c.direccion ?? ''
    });
    this.modalAbierto.set(true);
  }

  cerrarModal(): void {
    this.modalAbierto.set(false);
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.guardando.set(true);
    this.errorForm.set(null);

    const v = this.form.getRawValue();
    const dto = {
      nombre: v.nombre.trim(),
      cedula: v.cedula?.trim() || null,
      telefono: v.telefono?.trim() || null,
      email: v.email?.trim() || null,
      direccion: v.direccion?.trim() || null
    };

    const accion = this.editando()
      ? this.servicio.actualizar(this.editando()!.clienteId, dto)
      : this.servicio.crear(dto);

    accion.subscribe({
      next: () => {
        this.guardando.set(false);
        this.modalAbierto.set(false);
        this.cargar();
      },
      error: err => {
        this.guardando.set(false);
        this.errorForm.set(err.error?.error ?? 'No se pudo guardar el cliente.');
      }
    });
  }

  alternarBloqueo(c: Cliente): void {
    const accion = c.bloqueado ? this.servicio.desbloquear(c.clienteId) : this.servicio.bloquear(c.clienteId);
    accion.subscribe({ next: () => this.cargar() });
  }
}
