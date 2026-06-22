import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { BalanceGeneral, ContabilidadService, EstadoResultados } from '../../core/contabilidad.service';

@Component({
  selector: 'app-estados',
  imports: [FormsModule, LucideAngularModule, CurrencyPipe, DatePipe],
  templateUrl: './estados.html'
})
export class Estados {
  private servicio = inject(ContabilidadService);

  tab = signal<'resultados' | 'balance'>('resultados');
  cargando = signal(false);

  desde = signal(this.inicioMes());
  hasta = signal(this.hoy());

  resultados = signal<EstadoResultados | null>(null);
  balance = signal<BalanceGeneral | null>(null);

  private hoy(): string { return new Date().toISOString().slice(0, 10); }
  private inicioMes(): string { const d = new Date(); return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10); }

  constructor() { this.cargar(); }

  cambiarTab(t: 'resultados' | 'balance'): void {
    this.tab.set(t);
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    if (this.tab() === 'resultados') {
      this.servicio.estadoResultados(this.desde(), this.hasta()).subscribe({
        next: r => { this.resultados.set(r); this.cargando.set(false); },
        error: () => this.cargando.set(false)
      });
    } else {
      this.servicio.balanceGeneral(this.hasta()).subscribe({
        next: b => { this.balance.set(b); this.cargando.set(false); },
        error: () => this.cargando.set(false)
      });
    }
  }

  cerrando = signal(false);
  cerrarEjercicio(): void {
    if (this.cerrando()) return;
    if (!confirm(`Cerrar el ejercicio al ${this.hasta()}: el resultado (utilidad o pérdida) acumulado hasta esa fecha se trasladará a Resultados Acumulados (Capital). ¿Continuar?`)) return;
    this.cerrando.set(true);
    this.servicio.cerrarEjercicio(this.hasta()).subscribe({
      next: r => {
        this.cerrando.set(false);
        alert(r.mensaje);
        this.cargar();
      },
      error: err => {
        this.cerrando.set(false);
        alert(err.error?.error ?? 'No se pudo cerrar el ejercicio.');
      }
    });
  }
}
