import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Mascotas "pet" de plataforma: un robot Android sentado que balancea una
 * pierna y un zorro plateado (estilo Apple) que menea la cola. Dibujadas como
 * SVG propios y cute; no reproducen los logotipos de marca.
 */
@Component({
  selector: 'app-os-mascots',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (tipo() === 'android') {
      <svg class="m-float" [attr.width]="size()" viewBox="0 0 120 134" xmlns="http://www.w3.org/2000/svg" fill="none" aria-label="Mascota Android">
        <defs>
          <linearGradient id="botBody" x1="0" y1="0" x2="0" y2="1">
            <stop stop-color="#63d585"/><stop offset="1" stop-color="#3bb35f"/>
          </linearGradient>
        </defs>
        <ellipse cx="60" cy="128" rx="34" ry="6" fill="#000" opacity="0.08"/>
        <!-- piernas colgando (detrás del cuerpo); una se balancea -->
        <rect x="46" y="94" width="12" height="30" rx="6" fill="#39ac5b"/>
        <ellipse cx="52" cy="123" rx="7.5" ry="4.5" fill="#39ac5b"/>
        <g class="m-leg">
          <rect x="64" y="94" width="12" height="30" rx="6" fill="#39ac5b"/>
          <ellipse cx="70" cy="123" rx="7.5" ry="4.5" fill="#39ac5b"/>
        </g>
        <!-- brazos en reposo -->
        <rect x="18" y="56" width="12" height="30" rx="6" fill="#4cc06e"/>
        <rect x="90" y="56" width="12" height="30" rx="6" fill="#4cc06e"/>
        <!-- cuerpo -->
        <rect x="28" y="50" width="64" height="52" rx="24" fill="url(#botBody)" stroke="#2f9e4d" stroke-width="2"/>
        <ellipse cx="60" cy="80" rx="17" ry="19" fill="#8ee7a6" opacity="0.55"/>
        <circle cx="60" cy="80" r="10" fill="#eafff0" stroke="#2f9e4d" stroke-width="1.5"/>
        <circle cx="60" cy="80" r="3.4" fill="#3bb35f"/>
        <!-- cabeza -->
        <g class="m-head">
          <g class="m-antenna">
            <line x1="47" y1="30" x2="41" y2="13" stroke="#39ac5b" stroke-width="3.4" stroke-linecap="round"/>
            <line x1="73" y1="30" x2="79" y2="13" stroke="#39ac5b" stroke-width="3.4" stroke-linecap="round"/>
            <circle cx="41" cy="12" r="3" fill="#63d585"/>
            <circle cx="79" cy="12" r="3" fill="#63d585"/>
          </g>
          <rect x="30" y="24" width="60" height="42" rx="21" fill="url(#botBody)" stroke="#2f9e4d" stroke-width="2"/>
          <ellipse cx="48" cy="44" rx="6.2" ry="7" fill="#fff"/>
          <ellipse cx="72" cy="44" rx="6.2" ry="7" fill="#fff"/>
          <circle cx="49" cy="45" r="3.1" fill="#1e2530"/>
          <circle cx="73" cy="45" r="3.1" fill="#1e2530"/>
          <circle cx="50.2" cy="43.6" r="1" fill="#fff"/>
          <circle cx="74.2" cy="43.6" r="1" fill="#fff"/>
          <path d="M51 54 Q60 61 69 54" stroke="#1e2530" stroke-width="2.6" stroke-linecap="round" fill="none"/>
          <circle cx="41" cy="52" r="3" fill="#ff8fae" opacity="0.4"/>
          <circle cx="79" cy="52" r="3" fill="#ff8fae" opacity="0.4"/>
        </g>
      </svg>
    } @else {
      <svg class="m-float" [attr.width]="size()" viewBox="0 0 132 132" xmlns="http://www.w3.org/2000/svg" fill="none" aria-label="Mascota Apple">
        <defs>
          <linearGradient id="foxBody" x1="0" y1="0" x2="0" y2="1">
            <stop stop-color="#eef0f4"/><stop offset="1" stop-color="#c4cad3"/>
          </linearGradient>
        </defs>
        <ellipse cx="68" cy="124" rx="44" ry="6" fill="#000" opacity="0.08"/>
        <!-- cola que se menea -->
        <g class="m-tail">
          <path d="M88 84 C120 72 132 102 116 118 C107 126 90 123 82 110 C76 100 78 92 88 84 Z" fill="url(#foxBody)" stroke="#aab2bd" stroke-width="1.5"/>
          <path d="M104 110 C112 116 116 108 114 117 C109 123 100 121 96 116 C99 112 101 111 104 110 Z" fill="#fbfcfd"/>
        </g>
        <!-- cuerpo sentado -->
        <path d="M52 64 C39 72 39 104 58 114 C70 120 84 118 90 105 C97 90 91 72 79 66 C70 61 60 60 52 64 Z" fill="url(#foxBody)" stroke="#aab2bd" stroke-width="1.5"/>
        <path d="M58 88 C58 103 65 114 68 114 C71 114 79 103 78 90 C72 96 63 95 58 88 Z" fill="#fbfcfd"/>
        <rect x="55" y="104" width="9" height="16" rx="4.5" fill="url(#foxBody)"/>
        <rect x="72" y="104" width="9" height="16" rx="4.5" fill="url(#foxBody)"/>
        <ellipse cx="59.5" cy="119" rx="5" ry="3" fill="#fbfcfd"/>
        <ellipse cx="76.5" cy="119" rx="5" ry="3" fill="#fbfcfd"/>
        <!-- manzanita compañera -->
        <g>
          <path d="M27 96 C24 93 19 93 16 96 C12 100 12 108 16 114 C18 117 21 117 23 115 C25 117 28 117 30 114 C34 108 34 100 30 96 C29 95 28 95 27 96 Z" fill="#5a616b"/>
          <path d="M24 92 C25 88 29 86 32 86 C32 90 28 93 25 93 Z" fill="#7a8592"/>
        </g>
        <!-- cabeza -->
        <g class="m-head">
          <path d="M47 42 L39 13 L63 34 Z" fill="url(#foxBody)" stroke="#aab2bd" stroke-width="1.5"/>
          <path d="M87 42 L95 13 L71 34 Z" fill="url(#foxBody)" stroke="#aab2bd" stroke-width="1.5"/>
          <path d="M50 39 L46 22 L60 34 Z" fill="#d9b6c1"/>
          <path d="M84 39 L88 22 L74 34 Z" fill="#d9b6c1"/>
          <path d="M67 31 C50 31 40 46 44 62 C47 75 59 85 67 85 C75 85 87 75 90 62 C94 46 84 31 67 31 Z" fill="url(#foxBody)" stroke="#aab2bd" stroke-width="1.5"/>
          <path d="M55 60 C55 74 63 85 67 85 C71 85 79 74 79 60 C73 66 61 66 55 60 Z" fill="#fbfcfd"/>
          <ellipse cx="57" cy="56" rx="4" ry="5" fill="#2b2f36"/>
          <ellipse cx="77" cy="56" rx="4" ry="5" fill="#2b2f36"/>
          <circle cx="58.3" cy="54.2" r="1.3" fill="#fff"/>
          <circle cx="78.3" cy="54.2" r="1.3" fill="#fff"/>
          <path d="M63 66 L71 66 L67 71.5 Z" fill="#3a3f47"/>
          <path d="M67 71.5 C64 76 60 75 58 73" stroke="#3a3f47" stroke-width="1.8" stroke-linecap="round" fill="none"/>
          <circle cx="50" cy="65" r="3.2" fill="#ff9db4" opacity="0.35"/>
          <circle cx="84" cy="65" r="3.2" fill="#ff9db4" opacity="0.35"/>
        </g>
      </svg>
    }
  `,
  styles: [`
    :host { display: inline-block; line-height: 0; }
    .m-float { display: block; transform-box: fill-box; transform-origin: bottom center; animation: m-bob 3.4s ease-in-out infinite; filter: drop-shadow(0 5px 9px rgba(0,0,0,0.18)); }
    .m-head { transform-box: fill-box; transform-origin: bottom center; animation: m-nod 3.8s ease-in-out infinite; }
    .m-antenna { transform-box: fill-box; transform-origin: bottom center; animation: m-tilt 2.8s ease-in-out infinite; }
    .m-leg { transform-box: fill-box; transform-origin: top center; animation: m-swing 1.5s ease-in-out infinite; }
    .m-tail { transform-box: fill-box; transform-origin: bottom left; animation: m-wag 1.3s ease-in-out infinite; }

    @keyframes m-bob { 0%,100% { transform: translateY(0); } 50% { transform: translateY(-5px); } }
    @keyframes m-nod { 0%,100% { transform: rotate(-2.5deg); } 50% { transform: rotate(2.5deg); } }
    @keyframes m-tilt { 0%,100% { transform: rotate(-8deg); } 50% { transform: rotate(8deg); } }
    @keyframes m-swing { 0%,100% { transform: rotate(14deg); } 50% { transform: rotate(-16deg); } }
    @keyframes m-wag { 0%,100% { transform: rotate(-15deg); } 50% { transform: rotate(15deg); } }
    @media (prefers-reduced-motion: reduce) {
      .m-float, .m-head, .m-antenna, .m-leg, .m-tail { animation: none; }
    }
  `]
})
export class OsMascotsComponent {
  /** Plataforma a dibujar. */
  tipo = input<'android' | 'apple'>('android');
  /** Ancho en px. */
  size = input<number>(72);
}
