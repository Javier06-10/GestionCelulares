import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

interface Confeti { left: number; delay: number; dur: number; color: string; rot: number; w: number; h: number; }

/**
 * Overlay de celebración reutilizable: confeti cayendo + un ícono que "explota"
 * con anillo, mensaje y submensaje. Se monta con @if desde el padre (para que
 * arranquen las animaciones) y el padre lo retira tras ~2.4s.
 */
@Component({
  selector: 'app-celebracion',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div class="cb-overlay">
      @if (confeti()) {
        <div class="cb-confetti">
          @for (c of piezas(); track $index) {
            <i [style.left.%]="c.left" [style.animation-delay.ms]="c.delay" [style.animation-duration.ms]="c.dur"
               [style.background]="c.color" [style.width.px]="c.w" [style.height.px]="c.h"
               [style.--rot]="c.rot + 'deg'"></i>
          }
        </div>
      }
      <div class="cb-center">
        <div class="cb-ring" [class]="'cb-' + tono()"></div>
        <div class="cb-badge" [class]="'cb-' + tono()">
          <lucide-icon [name]="icono()" [size]="46" class="cb-icon"></lucide-icon>
        </div>
        <div class="cb-msg">{{ mensaje() }}</div>
        @if (submensaje()) { <div class="cb-sub">{{ submensaje() }}</div> }
      </div>
    </div>
  `,
  styles: [`
    .cb-overlay { position: fixed; inset: 0; z-index: 90; display: grid; place-items: center; overflow: hidden;
      background: rgba(13,13,16,0.5); backdrop-filter: blur(4px); animation: cb-fade 2.4s ease forwards; }
    .cb-confetti { position: absolute; inset: 0; pointer-events: none; }
    .cb-confetti i { position: absolute; top: -24px; border-radius: 2px; opacity: 0; transform: rotate(var(--rot)); animation-name: cb-fall; animation-timing-function: cubic-bezier(0.3,0.7,0.5,1); animation-fill-mode: forwards; }
    .cb-center { position: relative; display: flex; flex-direction: column; align-items: center; gap: 10px; text-align: center; }
    .cb-ring { position: absolute; top: -6px; left: 50%; width: 96px; height: 96px; margin-left: -48px; border-radius: 50%; opacity: 0; animation: cb-ring 1s ease-out 0.05s forwards; }
    .cb-badge { width: 96px; height: 96px; border-radius: 50%; display: grid; place-items: center; transform: scale(0); animation: cb-pop 0.7s cubic-bezier(0.34,1.56,0.64,1) forwards; box-shadow: 0 12px 34px -8px rgba(0,0,0,0.5); }
    .cb-icon { color: #fff; }
    .cb-msg { color: #fff; font-weight: 800; font-size: 24px; letter-spacing: 0.3px; text-shadow: 0 4px 16px rgba(0,0,0,0.4); opacity: 0; animation: cb-up 0.6s ease-out 0.28s forwards; }
    .cb-sub { color: #cbd2dc; font-size: 14px; opacity: 0; animation: cb-up 0.6s ease-out 0.4s forwards; }

    .cb-accent { background: #00c853; } .cb-ring.cb-accent { background: radial-gradient(circle, rgba(0,230,118,0.4), transparent 70%); }
    .cb-purple { background: #6C5CE7; } .cb-ring.cb-purple { background: radial-gradient(circle, rgba(108,92,231,0.4), transparent 70%); }
    .cb-amber  { background: #f0a500; } .cb-ring.cb-amber  { background: radial-gradient(circle, rgba(240,165,0,0.4), transparent 70%); }

    @keyframes cb-fall { 0% { opacity: 0; top: -24px; } 12% { opacity: 1; } 100% { opacity: 0; top: 105%; transform: rotate(calc(var(--rot) + 360deg)); } }
    @keyframes cb-pop { 0% { transform: scale(0); } 60% { transform: scale(1.12); } 100% { transform: scale(1); } }
    @keyframes cb-ring { 0% { opacity: 0.9; transform: scale(0.5); } 100% { opacity: 0; transform: scale(1.9); } }
    @keyframes cb-up { 0% { opacity: 0; transform: translateY(12px); } 100% { opacity: 1; transform: translateY(0); } }
    @keyframes cb-fade { 0% { opacity: 0; } 10% { opacity: 1; } 85% { opacity: 1; } 100% { opacity: 0; } }
    @media (prefers-reduced-motion: reduce) {
      .cb-overlay, .cb-confetti i, .cb-ring, .cb-badge, .cb-msg, .cb-sub { animation-duration: 0.01s; }
    }
  `]
})
export class CelebracionComponent {
  icono = input<string>('check');
  mensaje = input<string>('¡Listo!');
  submensaje = input<string>('');
  tono = input<'accent' | 'purple' | 'amber'>('accent');
  confeti = input<boolean>(true);

  private colores = ['#00e676', '#6C5CE7', '#f0a500', '#ff6b9d', '#22d3ee', '#ffd54a'];

  piezas = computed<Confeti[]>(() =>
    Array.from({ length: 16 }, (_, i) => ({
      left: Math.round((i * 6.1 + (i % 3) * 9) % 100),
      delay: (i % 6) * 90,
      dur: 1500 + (i % 5) * 240,
      color: this.colores[i % this.colores.length],
      rot: (i * 47) % 360,
      w: 7 + (i % 3) * 2,
      h: 11 + (i % 4) * 3
    }))
  );
}
