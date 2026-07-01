import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Ícono-mascota de plataforma: robot verde (Android) o manzana mordida (iOS).
 * Son íconos reconocibles de plataforma, dibujados como SVG propios, con una
 * animación de reposo sutil (flotan/se balancean) para "sentarse" en los KPIs.
 */
@Component({
  selector: 'app-os-mascots',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (tipo() === 'android') {
      <svg class="m-bot" [attr.width]="size()" viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg" fill="none" aria-label="Android">
        <g class="m-antenna">
          <line x1="35" y1="24" x2="29" y2="10" stroke="#A4C639" stroke-width="3.2" stroke-linecap="round"/>
          <line x1="65" y1="24" x2="71" y2="10" stroke="#A4C639" stroke-width="3.2" stroke-linecap="round"/>
        </g>
        <path d="M22 44 a28 28 0 0 1 56 0 Z" fill="#A4C639"/>
        <circle cx="40" cy="33" r="3.1" fill="#fff"/>
        <circle cx="60" cy="33" r="3.1" fill="#fff"/>
        <rect x="22" y="48" width="56" height="40" rx="7" fill="#A4C639"/>
        <rect class="m-arm" x="9" y="50" width="9.5" height="30" rx="4.75" fill="#A4C639"/>
        <rect x="81.5" y="50" width="9.5" height="30" rx="4.75" fill="#A4C639"/>
        <rect x="33" y="84" width="10" height="15" rx="5" fill="#A4C639"/>
        <rect x="57" y="84" width="10" height="15" rx="5" fill="#A4C639"/>
      </svg>
    } @else {
      <svg class="m-apple" [attr.width]="size()" viewBox="0 0 100 112" xmlns="http://www.w3.org/2000/svg" fill="none" aria-label="iOS">
        <path class="m-leaf" d="M53 22 C56 10 66 5 72 4 C72 15 63 23 54 23 C53.4 23 53 22.6 53 22 Z" fill="#141416"/>
        <path fill-rule="evenodd" clip-rule="evenodd" fill="#141416"
          d="M50 33 C45 28 37 26 31 30 C22 35 18 47 18 60 C18 79 28 101 40 101 C45 103 48 101 50 100 C52 101 55 103 60 101 C72 101 82 79 82 60 C82 48 78 37 69 33 C64 30.5 55 30 50 33 Z
             M70 50 a13 13 0 1 0 26 0 a13 13 0 1 0 -26 0 Z"/>
      </svg>
    }
  `,
  styles: [`
    :host { display: inline-block; line-height: 0; }
    .m-bot, .m-apple { display: block; transform-box: fill-box; transform-origin: bottom center; }
    .m-bot { animation: m-bob 3s ease-in-out infinite; filter: drop-shadow(0 4px 8px rgba(0,0,0,0.18)); }
    .m-apple { animation: m-sway 3.4s ease-in-out infinite; filter: drop-shadow(0 4px 8px rgba(0,0,0,0.22)); }
    .m-antenna { transform-box: fill-box; transform-origin: bottom center; animation: m-tilt 2.6s ease-in-out infinite; }
    .m-arm { transform-box: fill-box; transform-origin: top center; animation: m-wave 2s ease-in-out infinite; }
    .m-leaf { transform-box: fill-box; transform-origin: bottom left; animation: m-tilt 3s ease-in-out infinite; }

    @keyframes m-bob { 0%,100% { transform: translateY(0); } 50% { transform: translateY(-5px); } }
    @keyframes m-sway { 0%,100% { transform: translateY(0) rotate(-3deg); } 50% { transform: translateY(-4px) rotate(3deg); } }
    @keyframes m-tilt { 0%,100% { transform: rotate(-7deg); } 50% { transform: rotate(7deg); } }
    @keyframes m-wave { 0%,100% { transform: rotate(4deg); } 50% { transform: rotate(-20deg); } }
    @media (prefers-reduced-motion: reduce) {
      .m-bot, .m-apple, .m-antenna, .m-arm, .m-leaf { animation: none; }
    }
  `]
})
export class OsMascotsComponent {
  /** Plataforma a dibujar. */
  tipo = input<'android' | 'apple'>('android');
  /** Ancho en px. */
  size = input<number>(48);
}
