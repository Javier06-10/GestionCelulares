import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Mascotas decorativas de las plataformas: un robot verde (estilo Android) y
 * una fruta simpática (estilo iOS). Son diseños ORIGINALES —no reproducen los
 * logotipos de marca— con animación de reposo (flotan, parpadean, saludan).
 */
@Component({
  selector: 'app-os-mascots',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mascots" [style.--m-size.px]="size()">
      <!-- Robot verde (Android-style, original) -->
      <svg class="bot" viewBox="0 0 90 110" xmlns="http://www.w3.org/2000/svg" fill="none" aria-label="Robot Android">
        <!-- antenas -->
        <g class="antenna">
          <line x1="30" y1="22" x2="24" y2="8" stroke="#00c853" stroke-width="3" stroke-linecap="round"/>
          <circle cx="24" cy="7" r="3" fill="#00e676"/>
        </g>
        <g class="antenna antenna--r">
          <line x1="60" y1="22" x2="66" y2="8" stroke="#00c853" stroke-width="3" stroke-linecap="round"/>
          <circle cx="66" cy="7" r="3" fill="#00e676"/>
        </g>
        <!-- brazos -->
        <rect class="arm arm--wave" x="8" y="46" width="9" height="30" rx="4.5" fill="#00c853"/>
        <rect x="73" y="46" width="9" height="30" rx="4.5" fill="#00c853"/>
        <!-- cabeza -->
        <rect x="22" y="20" width="46" height="40" rx="16" fill="#00e676"/>
        <!-- ojos -->
        <g class="eyes">
          <circle cx="37" cy="38" r="4.2" fill="#0d0d10"/>
          <circle cx="53" cy="38" r="4.2" fill="#0d0d10"/>
        </g>
        <!-- cuerpo -->
        <rect x="26" y="58" width="38" height="40" rx="12" fill="#00e676"/>
        <rect x="34" y="96" width="7" height="12" rx="3.5" fill="#00c853"/>
        <rect x="49" y="96" width="7" height="12" rx="3.5" fill="#00c853"/>
      </svg>

      <!-- Fruta simpática (iOS-style, original) -->
      <svg class="fruit" viewBox="0 0 90 110" xmlns="http://www.w3.org/2000/svg" fill="none" aria-label="Fruta iOS">
        <!-- hoja -->
        <path class="leaf" d="M48 18 C58 8 72 12 72 12 C72 12 70 26 58 28 C50 29 46 24 48 18 Z" fill="#6C5CE7"/>
        <!-- tallo -->
        <path d="M45 20 C45 14 47 12 49 10" stroke="#8a7be8" stroke-width="3" stroke-linecap="round"/>
        <!-- cuerpo (dos lóbulos tipo manzana) -->
        <path d="M45 26 C30 24 18 36 18 58 C18 82 32 100 45 100 C58 100 72 82 72 58 C72 36 60 24 45 26 Z"
              fill="url(#fruit-grad)"/>
        <path d="M45 30 C40 30 36 32 34 36" stroke="#ffffff" stroke-width="3" stroke-linecap="round" opacity="0.7"/>
        <!-- ojos -->
        <g class="eyes">
          <circle cx="38" cy="58" r="4" fill="#2b2b33"/>
          <circle cx="54" cy="58" r="4" fill="#2b2b33"/>
        </g>
        <!-- sonrisa -->
        <path d="M38 70 Q46 78 54 70" stroke="#2b2b33" stroke-width="3" stroke-linecap="round" fill="none"/>
        <!-- rubor -->
        <circle cx="31" cy="66" r="3.5" fill="#ff6b9d" opacity="0.4"/>
        <circle cx="61" cy="66" r="3.5" fill="#ff6b9d" opacity="0.4"/>
        <defs>
          <linearGradient id="fruit-grad" x1="18" y1="26" x2="72" y2="100" gradientUnits="userSpaceOnUse">
            <stop stop-color="#f4f5f7"/>
            <stop offset="1" stop-color="#cfd4dc"/>
          </linearGradient>
        </defs>
      </svg>
    </div>
  `,
  styles: [`
    .mascots {
      display: flex;
      align-items: flex-end;
      gap: 10px;
    }
    .bot, .fruit {
      width: var(--m-size, 84px);
      height: auto;
      transform-origin: bottom center;
    }
    .bot { animation: m-bob 3.2s ease-in-out infinite; }
    .fruit { animation: m-bob 3.2s ease-in-out infinite 0.6s; }

    /* Elementos internos: fijamos el origen a su propia caja */
    .antenna, .arm, .eyes, .leaf { transform-box: fill-box; }
    .antenna { transform-origin: bottom center; animation: m-antenna 2.4s ease-in-out infinite; }
    .antenna--r { animation-delay: 0.3s; }
    .arm--wave { transform-origin: top center; animation: m-wave 1.8s ease-in-out infinite; }
    .leaf { transform-origin: bottom left; animation: m-antenna 2.8s ease-in-out infinite; }
    .eyes { transform-box: fill-box; transform-origin: center; animation: m-blink 4s steps(1, end) infinite; }

    @keyframes m-bob { 0%,100% { transform: translateY(0); } 50% { transform: translateY(-7px); } }
    @keyframes m-antenna { 0%,100% { transform: rotate(-6deg); } 50% { transform: rotate(6deg); } }
    @keyframes m-wave { 0%,100% { transform: rotate(6deg); } 50% { transform: rotate(-24deg); } }
    @keyframes m-blink {
      0%, 92%, 100% { transform: scaleY(1); }
      95% { transform: scaleY(0.1); }
    }
    @media (prefers-reduced-motion: reduce) {
      .bot, .fruit, .antenna, .arm, .leaf, .eyes { animation: none; }
    }
  `]
})
export class OsMascotsComponent {
  /** Ancho de cada mascota en px. */
  size = input<number>(84);
}
