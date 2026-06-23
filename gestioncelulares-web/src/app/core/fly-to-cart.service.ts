import { Injectable } from '@angular/core';

/**
 * Anima un "producto fantasma" que vuela en arco desde el elemento de origen
 * (el botón/tarjeta del producto) hasta el destino marcado con
 * `id="gc-cart-target"` (el ícono del carrito). Al llegar, hace rebotar el
 * carrito. Respeta prefers-reduced-motion.
 */
@Injectable({ providedIn: 'root' })
export class FlyToCartService {
  private get reduceMotion(): boolean {
    return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  /** Lanza el vuelo desde el elemento origen hacia el carrito. */
  fly(source: Element | null, targetId = 'gc-cart-target'): void {
    if (!source) return;
    const target = document.getElementById(targetId);
    if (!target) return;
    if (this.reduceMotion) { this.bump(target); return; }

    const s = source.getBoundingClientRect();
    const t = target.getBoundingClientRect();
    const sx = s.left + s.width / 2;
    const sy = s.top + s.height / 2;
    const tx = t.left + t.width / 2;
    const ty = t.top + t.height / 2;
    const dx = tx - sx;
    const dy = ty - sy;

    const ghost = document.createElement('div');
    ghost.setAttribute('aria-hidden', 'true');
    ghost.style.cssText = [
      'position:fixed',
      `left:${sx - 22}px`,
      `top:${sy - 22}px`,
      'width:44px',
      'height:44px',
      'border-radius:14px',
      'display:grid',
      'place-items:center',
      'z-index:9999',
      'pointer-events:none',
      'color:#0b0c10',
      'background:linear-gradient(135deg,#00E676,#34d399)',
      'box-shadow:0 10px 30px -6px rgba(0,230,118,0.6)'
    ].join(';');
    // Ícono de bolsa de compra (SVG inline, sin dependencias)
    ghost.innerHTML =
      '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4Z"/><path d="M3 6h18"/><path d="M16 10a4 4 0 0 1-8 0"/></svg>';
    document.body.appendChild(ghost);

    const anim = ghost.animate(
      [
        { transform: 'translate(0,0) scale(1) rotate(0deg)', opacity: 1, offset: 0 },
        { transform: `translate(${dx * 0.5}px, ${dy * 0.5 - 90}px) scale(0.85) rotate(-12deg)`, opacity: 1, offset: 0.55 },
        { transform: `translate(${dx}px, ${dy}px) scale(0.2) rotate(8deg)`, opacity: 0.35, offset: 1 }
      ],
      { duration: 680, easing: 'cubic-bezier(0.5, 0, 0.55, 1)', fill: 'forwards' }
    );

    anim.onfinish = () => {
      ghost.remove();
      this.bump(target);
    };
  }

  /** Rebote del carrito al recibir el producto. */
  private bump(target: HTMLElement): void {
    if (this.reduceMotion) return;
    target.animate(
      [
        { transform: 'scale(1)' },
        { transform: 'scale(1.45)' },
        { transform: 'scale(0.9)' },
        { transform: 'scale(1)' }
      ],
      { duration: 380, easing: 'cubic-bezier(0.34, 1.56, 0.64, 1)' }
    );
  }
}
