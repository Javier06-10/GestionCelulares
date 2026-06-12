import { CurrencyPipe, DecimalPipe, NgClass } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { DashboardService } from '../../core/dashboard.service';
import { DashboardDto } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DecimalPipe, NgClass],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
  private servicio = inject(DashboardService);

  datos = signal<DashboardDto | null>(null);
  error = signal<string | null>(null);

  constructor() {
    this.servicio.obtener().subscribe({
      next: d => this.datos.set(d),
      error: () => this.error.set('No se pudieron cargar los indicadores. ¿Está corriendo la API?')
    });
  }
}
