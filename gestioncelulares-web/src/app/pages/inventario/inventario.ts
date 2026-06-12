import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { CatalogoService, VarianteOpcion } from '../../core/catalogo.service';
import { Imei, InventarioService, StockDisponible } from '../../core/inventario.service';

@Component({
  selector: 'app-inventario',
  imports: [ReactiveFormsModule, LucideAngularModule, CurrencyPipe, DecimalPipe],
  templateUrl: './inventario.html'
})
export class Inventario {
  private fb = inject(FormBuilder);
  private servicio = inject(InventarioService);
  private catalogo = inject(CatalogoService);
  auth = inject(AuthService);

  stock = signal<StockDisponible[]>([]);
  cargando = signal(false);
  variantes = signal<VarianteOpcion[]>([]);

  totalEquipos = computed(() => this.stock().reduce((a, s) => a + s.disponibles, 0));
  valorVenta = computed(() => this.stock().reduce((a, s) => a + s.precioVenta * s.disponibles, 0));

  // Búsqueda por IMEI
  imeiBuscado = signal<Imei | null>(null);
  imeiNoEncontrado = signal(false);

  // Modal de registro
  modalAbierto = signal(false);
  guardando = signal(false);
  errorForm = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    imei: ['', [Validators.required, Validators.maxLength(20)]],
    varianteId: [0, [Validators.required, Validators.min(1)]],
    precioCosto: [0, [Validators.required, Validators.min(0)]]
  });

  constructor() {
    this.cargar();
    this.catalogo.productos().subscribe(ps => this.variantes.set(this.catalogo.variantesOpciones(ps)));
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.disponibles().subscribe({
      next: data => {
        this.stock.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  buscarImei(valor: string): void {
    const imei = valor.trim();
    this.imeiNoEncontrado.set(false);
    this.imeiBuscado.set(null);
    if (imei.length < 4) return;

    this.servicio.porImei(imei).subscribe({
      next: dto => this.imeiBuscado.set(dto),
      error: () => this.imeiNoEncontrado.set(true)
    });
  }

  abrirRegistro(): void {
    this.errorForm.set(null);
    this.form.reset({ imei: '', varianteId: 0, precioCosto: 0 });
    this.modalAbierto.set(true);
  }

  cerrarModal(): void {
    this.modalAbierto.set(false);
  }

  registrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const sucursalId = this.auth.usuario()?.sucursalId;
    if (!sucursalId) {
      this.errorForm.set('Tu usuario no tiene una sucursal asignada.');
      return;
    }

    this.guardando.set(true);
    this.errorForm.set(null);
    const v = this.form.getRawValue();

    this.servicio.registrar({
      imei: v.imei.trim(),
      varianteId: v.varianteId,
      sucursalId,
      precioCosto: v.precioCosto
    }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.modalAbierto.set(false);
        this.cargar();
      },
      error: err => {
        this.guardando.set(false);
        this.errorForm.set(err.error?.error ?? 'No se pudo registrar el equipo.');
      }
    });
  }

  estadoClase(estado: string): string {
    switch (estado) {
      case 'Disponible': return 'bg-tech-accent/10 text-tech-accent';
      case 'Vendido': return 'bg-slate-400/15 text-slate-500';
      case 'EnTaller': return 'bg-tech-purple/10 text-tech-purple';
      case 'Devuelto': return 'bg-tech-sand/30 text-amber-700';
      default: return 'bg-slate-400/15 text-slate-500';
    }
  }
}
