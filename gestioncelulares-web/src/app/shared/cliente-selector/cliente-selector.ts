import { Component, inject, input, model, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Cliente, ClienteService } from '../../core/cliente.service';
import { RncService } from '../../core/rnc.service';

/**
 * Selector de cliente reutilizable: búsqueda desplegable + creación rápida.
 * Uso: <app-cliente-selector [cliente]="sel()" (clienteChange)="sel.set($event)" [requerido]="true" />
 */
@Component({
  selector: 'app-cliente-selector',
  imports: [ReactiveFormsModule, LucideAngularModule],
  templateUrl: './cliente-selector.html'
})
export class ClienteSelector {
  private fb = inject(FormBuilder);
  private clientesSrv = inject(ClienteService);
  private rncSrv = inject(RncService);

  cliente = model<Cliente | null>(null);
  requerido = input(false);

  busqueda = signal('');
  resultados = signal<Cliente[]>([]);

  // Consulta al padrón DGII
  consultandoRnc = signal(false);
  rncMsg = signal<string | null>(null);

  // Modal de creación rápida
  modalCrear = signal(false);
  guardando = signal(false);
  errorForm = signal<string | null>(null);
  form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    cedula: [''],
    telefono: ['']
  });

  buscar(v: string): void {
    this.busqueda.set(v);
    if (v.trim().length < 2) { this.resultados.set([]); return; }
    this.clientesSrv.buscar(v.trim()).subscribe(r => this.resultados.set(r.slice(0, 6)));
  }

  elegir(c: Cliente): void {
    this.cliente.set(c);
    this.resultados.set([]);
    this.busqueda.set('');
  }

  limpiar(): void {
    this.cliente.set(null);
  }

  abrirCrear(): void {
    this.errorForm.set(null);
    this.rncMsg.set(null);
    this.form.reset({ nombre: this.busqueda().trim(), cedula: '', telefono: '' });
    this.modalCrear.set(true);
  }

  // Busca el RNC/cédula en el padrón de la DGII y autocompleta la razón social
  consultarRnc(): void {
    const ced = this.form.controls.cedula.value?.trim();
    if (!ced) return;
    this.consultandoRnc.set(true);
    this.rncMsg.set(null);
    this.rncSrv.consultar(ced).subscribe({
      next: r => {
        this.consultandoRnc.set(false);
        this.form.controls.nombre.setValue(r.nombre);
        this.rncMsg.set(r.estado && r.estado !== 'ACTIVO' ? `Encontrado (estado: ${r.estado})` : 'Encontrado en el padrón DGII');
      },
      error: () => {
        this.consultandoRnc.set(false);
        this.rncMsg.set('No encontrado en el padrón DGII. Puedes capturarlo manualmente.');
      }
    });
  }

  crear(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();
    this.clientesSrv.crear({
      nombre: v.nombre.trim(),
      cedula: v.cedula?.trim() || null,
      telefono: v.telefono?.trim() || null
    }).subscribe({
      next: c => {
        this.guardando.set(false);
        this.modalCrear.set(false);
        this.busqueda.set('');
        this.resultados.set([]);
        this.cliente.set(c);
      },
      error: err => { this.guardando.set(false); this.errorForm.set(err.error?.error ?? 'No se pudo crear el cliente.'); }
    });
  }
}
