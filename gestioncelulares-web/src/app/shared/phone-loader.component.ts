import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Indicador de carga premium: un teléfono girando en 3D con halo de neón.
 * Reutilizable en cualquier pantalla mientras se cargan datos.
 */
@Component({
  selector: 'app-phone-loader',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pl-wrap" [style.--pl-size.px]="size()">
      <div class="pl-glow"></div>
      <div class="pl-stage">
        <svg class="pl-phone" viewBox="0 0 84 150" xmlns="http://www.w3.org/2000/svg" fill="none">
          <rect x="3" y="3" width="78" height="144" rx="16" fill="#0d0d10" stroke="#00e676" stroke-width="2.5"/>
          <rect x="9" y="14" width="66" height="122" rx="9" fill="#15151b"/>
          <rect x="31" y="7" width="22" height="4" rx="2" fill="#00e676" opacity="0.7"/>
          <circle cx="42" cy="70" r="15" fill="none" stroke="#00e676" stroke-width="2.5" opacity="0.9"/>
          <path d="M42 60 L42 70 L49 74" stroke="#6C5CE7" stroke-width="2.5" stroke-linecap="round"/>
        </svg>
      </div>
      @if (label()) { <p class="pl-label">{{ label() }}</p> }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .pl-wrap {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 14px;
      padding: 24px;
    }
    .pl-stage {
      perspective: 600px;
      width: var(--pl-size, 64px);
      height: calc(var(--pl-size, 64px) * 1.78);
      display: grid;
      place-items: center;
      position: relative;
    }
    .pl-phone {
      width: 100%;
      height: 100%;
      transform-style: preserve-3d;
      animation: pl-spin 2.2s cubic-bezier(0.65, 0, 0.35, 1) infinite;
      filter: drop-shadow(0 8px 20px rgba(0, 230, 118, 0.3));
    }
    .pl-glow {
      position: absolute;
      width: calc(var(--pl-size, 64px) * 2.6);
      height: calc(var(--pl-size, 64px) * 2.6);
      border-radius: 50%;
      background: radial-gradient(circle, rgba(0, 230, 118, 0.16) 0%, transparent 65%);
      animation: pl-pulse 2.2s ease-in-out infinite;
    }
    .pl-label {
      margin: 0;
      font-size: 0.8rem;
      color: #64748b;
      font-family: 'Courier New', monospace;
      letter-spacing: 0.4px;
    }
    @keyframes pl-spin { 0% { transform: rotateY(0); } 100% { transform: rotateY(360deg); } }
    @keyframes pl-pulse { 0%,100% { transform: scale(0.85); opacity: 0.5; } 50% { transform: scale(1.1); opacity: 1; } }
    @media (prefers-reduced-motion: reduce) {
      .pl-phone { animation: pl-pulse 2s ease-in-out infinite; }
    }
  `]
})
export class PhoneLoaderComponent {
  /** Ancho del teléfono en px (el alto se calcula proporcional). */
  size = input<number>(64);
  /** Texto opcional bajo el loader. */
  label = input<string>('');
}
