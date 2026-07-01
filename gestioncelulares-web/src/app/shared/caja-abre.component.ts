import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Escena de celebración al abrir la caja: un cofre de dinero que se abre con
 * monedas saltando y destellos. Se monta con @if desde el padre (así las
 * animaciones arrancan) y el padre lo retira tras ~2.2s.
 */
@Component({
  selector: 'app-caja-abre',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="cx-overlay">
      <div class="cx-scene">
        <div class="cx-glow"></div>
        <svg viewBox="0 0 220 200" xmlns="http://www.w3.org/2000/svg" fill="none">
          <!-- monedas que saltan (detrás de la tapa) -->
          <g class="cx-coins">
            <g class="cx-coin cx-coin--1"><circle cx="110" cy="96" r="12" fill="#ffd54a" stroke="#f0a500" stroke-width="2"/><text x="110" y="101" text-anchor="middle" font-size="13" font-weight="700" fill="#b26a00">$</text></g>
            <g class="cx-coin cx-coin--2"><circle cx="110" cy="96" r="10" fill="#ffd54a" stroke="#f0a500" stroke-width="2"/><text x="110" y="100" text-anchor="middle" font-size="11" font-weight="700" fill="#b26a00">$</text></g>
            <g class="cx-coin cx-coin--3"><circle cx="110" cy="96" r="9" fill="#ffe27a" stroke="#f0a500" stroke-width="2"/><text x="110" y="100" text-anchor="middle" font-size="10" font-weight="700" fill="#b26a00">$</text></g>
            <g class="cx-coin cx-coin--4"><circle cx="110" cy="96" r="11" fill="#ffd54a" stroke="#f0a500" stroke-width="2"/><text x="110" y="100" text-anchor="middle" font-size="12" font-weight="700" fill="#b26a00">$</text></g>
          </g>

          <!-- cuerpo del cofre -->
          <rect x="52" y="104" width="116" height="70" rx="12" fill="#2a2540"/>
          <rect x="52" y="104" width="116" height="70" rx="12" fill="url(#cx-body)" opacity="0.5"/>
          <!-- billetes asomando -->
          <rect x="82" y="92" width="56" height="26" rx="4" fill="#2ecc71"/>
          <rect x="88" y="96" width="44" height="18" rx="3" fill="#27ae60"/>
          <circle cx="110" cy="105" r="6" fill="#2ecc71" stroke="#1e8f4e" stroke-width="1.5"/>
          <!-- cerradura -->
          <rect x="102" y="132" width="16" height="20" rx="4" fill="#ffd54a"/>
          <circle cx="110" cy="140" r="3" fill="#b26a00"/>

          <!-- tapa que se abre -->
          <g class="cx-lid">
            <path d="M52 104 C52 82 74 70 110 70 C146 70 168 82 168 104 Z" fill="#3a3357"/>
            <path d="M52 104 C52 82 74 70 110 70 C146 70 168 82 168 104 Z" fill="url(#cx-lid-g)" opacity="0.6"/>
            <rect x="98" y="94" width="24" height="12" rx="3" fill="#ffd54a"/>
          </g>

          <!-- destellos -->
          <g class="cx-spark" fill="#00e676">
            <path class="cx-spark--a" d="M60 60 l3 8 8 3 -8 3 -3 8 -3 -8 -8 -3 8 -3 z"/>
            <path class="cx-spark--b" d="M164 66 l2.5 7 7 2.5 -7 2.5 -2.5 7 -2.5 -7 -7 -2.5 7 -2.5 z"/>
          </g>

          <defs>
            <linearGradient id="cx-body" x1="52" y1="104" x2="168" y2="174" gradientUnits="userSpaceOnUse">
              <stop stop-color="#6C5CE7" stop-opacity="0.5"/><stop offset="1" stop-color="#2a2540"/>
            </linearGradient>
            <linearGradient id="cx-lid-g" x1="52" y1="70" x2="168" y2="104" gradientUnits="userSpaceOnUse">
              <stop stop-color="#8a7be8"/><stop offset="1" stop-color="#6C5CE7" stop-opacity="0.3"/>
            </linearGradient>
          </defs>
        </svg>
        <div class="cx-label">¡Caja abierta!</div>
      </div>
    </div>
  `,
  styles: [`
    .cx-overlay {
      position: fixed;
      inset: 0;
      z-index: 80;
      display: grid;
      place-items: center;
      background: rgba(13, 13, 16, 0.55);
      backdrop-filter: blur(4px);
      animation: cx-fade 2.2s ease forwards;
    }
    .cx-scene { position: relative; width: min(300px, 80vw); text-align: center; }
    .cx-scene svg { width: 100%; height: auto; overflow: visible; }
    .cx-glow {
      position: absolute; left: 50%; top: 46%; transform: translate(-50%, -50%);
      width: 240px; height: 240px; border-radius: 50%;
      background: radial-gradient(circle, rgba(0,230,118,0.25) 0%, transparent 65%);
      animation: cx-glow 2.2s ease-out forwards;
    }
    .cx-label {
      margin-top: 6px; color: #fff; font-weight: 800; font-size: 22px; letter-spacing: 0.5px;
      text-shadow: 0 4px 18px rgba(0,230,118,0.5);
      animation: cx-label 2.2s cubic-bezier(0.34,1.56,0.64,1) forwards;
    }
    .cx-lid { transform-box: fill-box; transform-origin: left bottom; animation: cx-open 0.7s cubic-bezier(0.34,1.56,0.64,1) forwards; }
    .cx-coin { transform-box: fill-box; transform-origin: center; opacity: 0; }
    .cx-coin--1 { animation: cx-coin-a 1.5s ease-out 0.3s forwards; }
    .cx-coin--2 { animation: cx-coin-b 1.6s ease-out 0.42s forwards; }
    .cx-coin--3 { animation: cx-coin-c 1.5s ease-out 0.54s forwards; }
    .cx-coin--4 { animation: cx-coin-d 1.7s ease-out 0.36s forwards; }
    .cx-spark path { transform-box: fill-box; transform-origin: center; opacity: 0; }
    .cx-spark--a { animation: cx-spark 1.4s ease-out 0.55s forwards; }
    .cx-spark--b { animation: cx-spark 1.4s ease-out 0.75s forwards; }

    @keyframes cx-open { 0% { transform: rotate(0); } 60% { transform: rotate(-124deg); } 100% { transform: rotate(-112deg); } }
    @keyframes cx-coin-a { 0% { transform: translate(0,0) scale(0.4); opacity: 0; } 25% { opacity: 1; } 100% { transform: translate(-46px,-70px) scale(1) rotate(180deg); opacity: 0; } }
    @keyframes cx-coin-b { 0% { transform: translate(0,0) scale(0.4); opacity: 0; } 25% { opacity: 1; } 100% { transform: translate(40px,-84px) scale(1) rotate(-160deg); opacity: 0; } }
    @keyframes cx-coin-c { 0% { transform: translate(0,0) scale(0.4); opacity: 0; } 25% { opacity: 1; } 100% { transform: translate(-14px,-96px) scale(1) rotate(120deg); opacity: 0; } }
    @keyframes cx-coin-d { 0% { transform: translate(0,0) scale(0.4); opacity: 0; } 25% { opacity: 1; } 100% { transform: translate(20px,-64px) scale(1) rotate(200deg); opacity: 0; } }
    @keyframes cx-spark { 0% { transform: scale(0) rotate(0); opacity: 0; } 40% { opacity: 1; transform: scale(1.2) rotate(45deg); } 100% { transform: scale(0.6) rotate(90deg); opacity: 0; } }
    @keyframes cx-glow { 0% { opacity: 0; transform: translate(-50%,-50%) scale(0.5); } 40% { opacity: 1; } 100% { opacity: 0; transform: translate(-50%,-50%) scale(1.2); } }
    @keyframes cx-label { 0%, 20% { opacity: 0; transform: translateY(14px) scale(0.8); } 45% { opacity: 1; transform: translateY(0) scale(1); } 85% { opacity: 1; } 100% { opacity: 0; } }
    @keyframes cx-fade { 0% { opacity: 0; } 12% { opacity: 1; } 85% { opacity: 1; } 100% { opacity: 0; } }

    @media (prefers-reduced-motion: reduce) {
      .cx-overlay, .cx-lid, .cx-coin, .cx-spark path, .cx-glow, .cx-label { animation-duration: 0.01s; }
    }
  `]
})
export class CajaAbreComponent {}
