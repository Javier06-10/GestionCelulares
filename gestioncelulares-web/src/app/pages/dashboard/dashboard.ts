import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardService } from '../../core/dashboard.service';
import { DashboardDto } from '../../core/models';
import { CountUpDirective } from '../../shared/count-up.directive';
import { PhoneLoaderComponent } from '../../shared/phone-loader.component';

interface DonutSeg { dash: string; offset: string; color: string; label: string; pct: number; }
interface Alerta { tipo: 'critico' | 'info' | 'ok'; icono: string; titulo: string; texto: string; }

interface PuntoGrafica { x: number; y: number; fecha: string; total: number; }

type Metrica = 'total' | 'ganancia';

// Dimensiones del lienzo SVG de la gráfica
const ANCHO = 720;
const ALTO = 200;
const PAD = { top: 16, right: 16, bottom: 28, left: 16 };

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DecimalPipe, DatePipe, RouterLink, LucideAngularModule, CountUpDirective, PhoneLoaderComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnDestroy {
  private servicio = inject(DashboardService);

  readonly ancho = ANCHO;
  readonly alto = ALTO;

  datos = signal<DashboardDto | null>(null);
  error = signal<string | null>(null);
  refrescando = signal(false);
  ultima = signal<Date>(new Date());
  reloj = signal<Date>(new Date());   // reloj en vivo (segundos corriendo)

  // Métrica de la gráfica: monto vendido o ganancia
  metrica = signal<Metrica>('total');
  cambiarMetrica(m: Metrica): void { this.metrica.set(m); }

  private valor(d: { total: number; ganancia: number }): number {
    return this.metrica() === 'ganancia' ? d.ganancia : d.total;
  }

  private timer?: ReturnType<typeof setInterval>;
  private relojTimer?: ReturnType<typeof setInterval>;

  constructor() {
    this.cargar();
    // Auto-refresco en vivo cada 30 s
    this.timer = setInterval(() => this.cargar(true), 30000);
    // Reloj en tiempo real: avanza cada segundo
    this.relojTimer = setInterval(() => this.reloj.set(new Date()), 1000);
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
    if (this.relojTimer) clearInterval(this.relojTimer);
  }

  cargar(silencioso = false): void {
    this.refrescando.set(true);
    this.servicio.obtener().subscribe({
      next: d => {
        this.datos.set(d);
        this.ultima.set(new Date());
        this.error.set(null);
        this.refrescando.set(false);
      },
      error: () => {
        this.refrescando.set(false);
        if (!silencioso && !this.datos()) this.error.set('No se pudieron cargar los indicadores. ¿Está corriendo la API?');
      }
    });
  }

  // Máximo de la serie (para escalar el eje Y)
  maxVenta = computed(() => {
    const serie = this.datos()?.ventasUltimos14Dias ?? [];
    return Math.max(1, ...serie.map(d => this.valor(d)));
  });

  totalPeriodo = computed(() =>
    (this.datos()?.ventasUltimos14Dias ?? []).reduce((a, d) => a + this.valor(d), 0)
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
      y: PAD.top + h - (this.valor(d) / max) * h,
      fecha: d.fecha,
      total: this.valor(d)
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

  // ----- Hover interactivo de la gráfica -----
  hoverIdx = signal<number | null>(null);
  hoverPunto = computed<PuntoGrafica | null>(() => {
    const i = this.hoverIdx();
    const pts = this.puntos();
    return i !== null && pts[i] ? pts[i] : null;
  });
  hoverRatio = computed(() => {
    const i = this.hoverIdx();
    const n = this.puntos().length;
    return i !== null && n > 1 ? i / (n - 1) : 0;
  });

  onMover(ev: MouseEvent): void {
    const el = ev.currentTarget as Element;
    const r = el.getBoundingClientRect();
    const n = this.puntos().length;
    if (n === 0 || r.width === 0) return;
    const ratio = (ev.clientX - r.left) / r.width;
    const idx = Math.max(0, Math.min(n - 1, Math.round(ratio * (n - 1))));
    this.hoverIdx.set(idx);
  }
  salirHover(): void { this.hoverIdx.set(null); }

  // ----- Barras de ranking -----
  maxProducto = computed(() => Math.max(1, ...(this.datos()?.topProductosMes ?? []).map(p => p.total)));
  maxVendedor = computed(() => Math.max(1, ...(this.datos()?.topVendedoresMes ?? []).map(v => v.total)));
  pct(valor: number, max: number): number { return Math.max(3, Math.round((valor / max) * 100)); }

  // ----- Métricas derivadas -----
  avgTicket = computed(() => {
    const m = this.datos()?.ventasMes;
    return m && m.cantidad > 0 ? m.total / m.cantidad : 0;
  });
  margenMes = computed(() => {
    const m = this.datos()?.ventasMes;
    return m && m.total > 0 ? Math.round((m.ganancia / m.total) * 100) : 0;
  });
  inventarioTotal = computed(() => {
    const i = this.datos()?.inventario;
    return i ? i.equiposDisponibles + i.accesoriosDisponibles : 0;
  });

  // ----- Donut: ventas por producto (top del mes) -----
  private readonly DONUT_C = 2 * Math.PI * 40; // circunferencia (r=40)
  private readonly DONUT_COLORS = ['#00E676', '#6C5CE7', '#22d3ee', '#f59e0b', '#8D9097'];
  donut = computed<DonutSeg[]>(() => {
    const top = (this.datos()?.topProductosMes ?? []).slice(0, 5);
    const total = top.reduce((a, p) => a + p.total, 0);
    if (total <= 0) return [];
    let acc = 0;
    return top.map((p, i) => {
      const frac = p.total / total;
      const seg: DonutSeg = {
        dash: `${(frac * this.DONUT_C).toFixed(2)} ${(this.DONUT_C - frac * this.DONUT_C).toFixed(2)}`,
        offset: `${(-acc * this.DONUT_C).toFixed(2)}`,
        color: this.DONUT_COLORS[i % this.DONUT_COLORS.length],
        label: p.producto,
        pct: Math.round(frac * 100)
      };
      acc += frac;
      return seg;
    });
  });
  donutPrincipal = computed(() => this.donut()[0] ?? null);

  // ----- Panel de inteligencia: alertas operativas reales -----
  alertas = computed<Alerta[]>(() => {
    const d = this.datos();
    if (!d) return [];
    const a: Alerta[] = [];
    const dop = (n: number) => 'RD$ ' + Math.round(n).toLocaleString('es-DO');
    if (d.creditos.cuotasVencidas > 0)
      a.push({ tipo: 'critico', icono: 'triangle-alert', titulo: 'Cuotas vencidas', texto: `${d.creditos.cuotasVencidas} cuota(s) en mora por ${dop(d.creditos.montoVencido)}.` });
    if (!d.caja.sesionAbierta)
      a.push({ tipo: 'info', icono: 'door-open', titulo: 'Caja cerrada', texto: 'Abre la caja para registrar ventas del turno.' });
    if (d.taller.reparados > 0)
      a.push({ tipo: 'ok', icono: 'wrench', titulo: 'Listos para entregar', texto: `${d.taller.reparados} equipo(s) reparado(s) esperando entrega.` });
    if (a.length === 0)
      a.push({ tipo: 'ok', icono: 'circle-check', titulo: 'Todo en orden', texto: 'Sin alertas operativas en este momento.' });
    return a;
  });

  // Sparkline de Ventas (14 días) para el KPI
  sparklineVentas = computed(() => this.sparkline(d => d.total));
  // Sparkline de Ganancias (14 días) para el KPI
  sparklineGanancias = computed(() => this.sparkline(d => d.ganancia));

  private sparkline(sel: (d: { total: number; ganancia: number }) => number): string {
    const serie = this.datos()?.ventasUltimos14Dias ?? [];
    if (serie.length === 0) return '';
    const vals = serie.map(sel);
    const max = Math.max(1, ...vals);
    const min = Math.min(...vals);
    const range = max - min || 1;
    const w = 120, h = 40, padding = 2;
    const step = w / (serie.length - 1);
    return serie.map((d, i) => {
      const x = i * step;
      const y = padding + h - 2 * padding - ((sel(d) - min) / range) * (h - 2 * padding);
      return `${i === 0 ? 'M' : 'L'} ${x.toFixed(1)} ${y.toFixed(1)}`;
    }).join(' ');
  }

  diaCorto(fecha: string): string {
    return new Date(fecha).toLocaleDateString('es-DO', { day: '2-digit', month: '2-digit' });
  }
}
