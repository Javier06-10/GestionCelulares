import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { Caso, Garantia, GarantiaService, IndiceFalla } from '../../core/garantia.service';

type Vista = 'garantias' | 'casos' | 'fallas';

@Component({
  selector: 'app-garantias',
  imports: [ReactiveFormsModule, LucideAngularModule, DatePipe],
  templateUrl: './garantias.html'
})
export class Garantias {
  private fb = inject(FormBuilder);
  private servicio = inject(GarantiaService);
  auth = inject(AuthService);

  vista = signal<Vista>('garantias');
  cargando = signal(false);

  garantias = signal<Garantia[]>([]);
  casos = signal<Caso[]>([]);
  fallas = signal<IndiceFalla[]>([]);

  // Consulta por IMEI
  imeiBuscado = signal<Garantia | null>(null);
  imeiNoEncontrado = signal(false);

  // Detalle de caso
  caso = signal<Caso | null>(null);
  errorCaso = signal<string | null>(null);

  // Abrir caso
  modalCaso = signal(false);
  errorAbrir = signal<string | null>(null);
  formCaso = this.fb.nonNullable.group({
    imeiId: [null as number | null, Validators.required],
    garantiaId: [null as number | null],
    tipoFalla: [''],
    descripcionFalla: ['']
  });

  // Resolver caso
  modalResolver = signal(false);
  errorResolver = signal<string | null>(null);
  formResolver = this.fb.nonNullable.group({
    tipoResolucion: ['Reparacion', Validators.required],
    imeiReemplazoId: [null as number | null],
    notas: ['']
  });

  constructor() { this.cargar(); }

  cambiarVista(v: Vista): void { this.vista.set(v); this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    const fin = () => this.cargando.set(false);
    if (this.vista() === 'garantias') this.servicio.buscar().subscribe({ next: d => { this.garantias.set(d); fin(); }, error: fin });
    else if (this.vista() === 'casos') this.servicio.casos().subscribe({ next: d => { this.casos.set(d); fin(); }, error: fin });
    else this.servicio.indiceFallas().subscribe({ next: d => { this.fallas.set(d); fin(); }, error: fin });
  }

  buscarImei(v: string): void {
    const imei = v.trim();
    this.imeiNoEncontrado.set(false);
    this.imeiBuscado.set(null);
    if (imei.length < 4) return;
    this.servicio.porImei(imei).subscribe({ next: g => this.imeiBuscado.set(g), error: () => this.imeiNoEncontrado.set(true) });
  }

  // ----- Detalle de caso -----
  abrirDetalleCaso(id: number): void {
    this.errorCaso.set(null);
    this.servicio.casoPorId(id).subscribe(c => this.caso.set(c));
  }
  cerrarDetalleCaso(): void { this.caso.set(null); }

  iniciarCaso(): void {
    const c = this.caso(); if (!c) return;
    this.servicio.iniciarCaso(c.casoGarantiaId).subscribe({ next: x => { this.caso.set(x); this.cargar(); }, error: e => this.errorCaso.set(e.error?.error ?? 'No se pudo iniciar.') });
  }
  cerrarCaso(): void {
    const c = this.caso(); if (!c) return;
    this.servicio.cerrarCaso(c.casoGarantiaId).subscribe({ next: x => { this.caso.set(x); this.cargar(); }, error: e => this.errorCaso.set(e.error?.error ?? 'No se pudo cerrar.') });
  }

  // ----- Abrir caso -----
  abrirModalCaso(g?: Garantia): void {
    this.errorAbrir.set(null);
    this.formCaso.reset({ imeiId: g?.imeiId ?? null, garantiaId: g?.garantiaId ?? null, tipoFalla: '', descripcionFalla: '' });
    this.modalCaso.set(true);
  }
  crearCaso(): void {
    if (this.formCaso.invalid) { this.formCaso.markAllAsTouched(); return; }
    const v = this.formCaso.getRawValue();
    this.servicio.abrirCaso({ imeiId: v.imeiId, garantiaId: v.garantiaId, tipoFalla: v.tipoFalla?.trim() || null, descripcionFalla: v.descripcionFalla?.trim() || null }).subscribe({
      next: c => { this.modalCaso.set(false); this.vista.set('casos'); this.cargar(); this.caso.set(c); },
      error: e => this.errorAbrir.set(e.error?.error ?? 'No se pudo abrir el caso.')
    });
  }

  // ----- Resolver caso -----
  abrirResolver(): void {
    this.errorResolver.set(null);
    this.formResolver.reset({ tipoResolucion: 'Reparacion', imeiReemplazoId: null, notas: '' });
    this.modalResolver.set(true);
  }
  resolver(): void {
    const c = this.caso(); if (!c || this.formResolver.invalid) return;
    const v = this.formResolver.getRawValue();
    this.servicio.resolverCaso(c.casoGarantiaId, { tipoResolucion: v.tipoResolucion, imeiReemplazoId: v.imeiReemplazoId, notas: v.notas?.trim() || null }).subscribe({
      next: x => { this.caso.set(x); this.modalResolver.set(false); this.cargar(); },
      error: e => this.errorResolver.set(e.error?.error ?? 'No se pudo resolver.')
    });
  }

  estadoGarClase(e: string, vigente: boolean): string {
    if (e === 'Vigente' && vigente) return 'bg-tech-accent/10 text-tech-accent';
    if (e === 'Anulada') return 'bg-red-500/10 text-red-500';
    return 'bg-slate-400/15 text-slate-500';
  }
  estadoCasoClase(e: string): string {
    switch (e) {
      case 'Abierto': return 'bg-tech-sand/30 text-amber-700';
      case 'EnProceso': return 'bg-tech-purple/10 text-tech-purple';
      case 'Resuelto': return 'bg-tech-accent/10 text-tech-accent';
      case 'Rechazado': return 'bg-red-500/10 text-red-500';
      case 'Cerrado': return 'bg-slate-400/15 text-slate-500';
      default: return 'bg-slate-400/15 text-slate-500';
    }
  }
}
