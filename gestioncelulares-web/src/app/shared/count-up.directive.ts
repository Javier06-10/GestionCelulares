import { Directive, ElementRef, Input, OnChanges, inject } from '@angular/core';

/**
 * Anima un número desde 0 hasta su valor cuando aparece o cambia.
 * Uso: <span [gcCountUp]="total" countPrefix="RD$" [countDecimals]="2"></span>
 * Respeta `prefers-reduced-motion`.
 */
@Directive({ selector: '[gcCountUp]', standalone: true })
export class CountUpDirective implements OnChanges {
  private el = inject<ElementRef<HTMLElement>>(ElementRef);

  @Input('gcCountUp') value = 0;
  @Input() countDuration = 750;
  @Input() countDecimals = 0;
  @Input() countPrefix = '';
  @Input() countSuffix = '';

  ngOnChanges(): void {
    const fin = Number(this.value) || 0;

    if (typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches) {
      this.render(fin);
      return;
    }

    const inicio = performance.now();
    const animar = (ahora: number) => {
      const p = Math.min(1, (ahora - inicio) / this.countDuration);
      const eased = 1 - Math.pow(1 - p, 3); // easeOutCubic
      this.render(fin * eased);
      if (p < 1) requestAnimationFrame(animar);
    };
    requestAnimationFrame(animar);
  }

  private render(v: number): void {
    const fmt = new Intl.NumberFormat('es-DO', {
      minimumFractionDigits: this.countDecimals,
      maximumFractionDigits: this.countDecimals
    });
    this.el.nativeElement.textContent = `${this.countPrefix}${fmt.format(v)}${this.countSuffix}`;
  }
}
