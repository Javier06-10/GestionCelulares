import { CurrencyPipe, DecimalPipe, NgClass } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardService } from '../../core/dashboard.service';
import { DashboardDto } from '../../core/models';
import { CountUpDirective } from '../../shared/count-up.directive';

interface PuntoGrafica { x: number; y: number; fecha: string; total: number; }

// Dimensiones del lienzo SVG de la gráfica
const ANCHO = 720;
const ALTO = 200;
const PAD = { top: 16, right: 16, bottom: 28, left: 16 };

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DecimalPipe, NgClass, LucideAngularModule, CountUpDirective],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
  private servicio = inject(DashboardService);

  readonly ancho = ANCHO;
  readonly alto = ALTO;

  datos = signal<DashboardDto | null>(null);
  error = signal<string | null>(null);

  // Máximo de la serie (para escalar el eje Y)
  maxVenta = computed(() => {
    const serie = this.datos()?.ventasUltimos14Dias ?? [];
    return Math.max(1, ...serie.map(d => d.total));
  });

  totalPeriodo = computed(() =>
    (this.datos()?.ventasUltimos14Dias ?? []).reduce((a, d) => a + d.total, 0)
  );

  // Puntos (x,y) escalados al lienzo
  puntos = computed<PuntoGrafica[]>(() => {
    const serie = this.datos()?.ventasUltimos14Dias ?? [];
    if (serie.length === 0) return [];
    const max = this.maxVenta();
    const w = ANCHO - PAD.left - PAD.right;
    const h = ALTO - PAD.top - PAD.bottom;
    const paso = serie.length > 1 ? w / (serie.length - 1) : 0;
    return serie.map((d, i) => ({
      x: PAD.left + i * paso,
      y: PAD.top + h - (d.total / max) * h,
      fecha: d.fecha,
      total: d.total
    }));
  });

  // Path de la línea
  linea = computed(() => this.puntos().map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' '));

  // Path del área bajo la curva (relleno)
  area = computed(() => {
    const pts = this.puntos();
    if (pts.length === 0) return '';
    const base = ALTO - PAD.bottom;
    const linea = pts.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
    return `${linea} L ${pts[pts.length - 1].x.toFixed(1)} ${base} L ${pts[0].x.toFixed(1)} ${base} Z`;
  });

  // Sparkline de Ventas (14 días)
  sparklineVentas = computed(() => {
    const serie = this.datos()?.ventasUltimos14Dias ?? [];
    if (serie.length === 0) return '';
    const max = Math.max(1, ...serie.map(d => d.total));
    const min = Math.min(...serie.map(d => d.total));
    const range = max - min || 1;
    const w = 120;
    const h = 40;
    const padding = 2;
    const step = w / (serie.length - 1);
    
    return serie.map((d, i) => {
      const x = i * step;
      const y = padding + h - 2 * padding - ((d.total - min) / range) * (h - 2 * padding);
      return `${i === 0 ? 'M' : 'L'} ${x.toFixed(1)} ${y.toFixed(1)}`;
    }).join(' ');
  });

  // Sparkline de Ganancias (14 días)
  sparklineGanancias = computed(() => {
    const serie = this.datos()?.ventasUltimos14Dias ?? [];
    if (serie.length === 0) return '';
    const max = Math.max(1, ...serie.map(d => d.ganancia));
    const min = Math.min(...serie.map(d => d.ganancia));
    const range = max - min || 1;
    const w = 120;
    const h = 40;
    const padding = 2;
    const step = w / (serie.length - 1);
    
    return serie.map((d, i) => {
      const x = i * step;
      const y = padding + h - 2 * padding - ((d.ganancia - min) / range) * (h - 2 * padding);
      return `${i === 0 ? 'M' : 'L'} ${x.toFixed(1)} ${y.toFixed(1)}`;
    }).join(' ');
  });

  constructor() {
    this.servicio.obtener().subscribe({
      next: d => this.datos.set(d),
      error: () => this.error.set('No se pudieron cargar los indicadores. ¿Está corriendo la API?')
    });
  }

  diaCorto(fecha: string): string {
    return new Date(fecha).toLocaleDateString('es-DO', { day: '2-digit', month: '2-digit' });
  }
}
