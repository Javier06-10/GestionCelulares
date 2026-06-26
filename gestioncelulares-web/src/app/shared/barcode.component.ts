import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Render de código de barras Code39 como SVG puro (sin dependencias externas).
 * Code39 codifica dígitos, mayúsculas y algunos símbolos; ideal para los códigos
 * EAN-13 internos y los números de orden del taller. Cualquier lector lo escanea.
 */
@Component({
  selector: 'app-barcode',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (barras().length) {
      <svg [attr.viewBox]="'0 0 ' + ancho() + ' ' + alto()" preserveAspectRatio="none"
           [style.width.px]="ancho()" [style.height.px]="alto()" shape-rendering="crispEdges"
           xmlns="http://www.w3.org/2000/svg" role="img" [attr.aria-label]="'Código ' + value()">
        <rect [attr.width]="ancho()" [attr.height]="alto()" fill="#ffffff"></rect>
        @for (b of barras(); track $index) {
          <rect [attr.x]="b.x" y="0" [attr.width]="b.w" [attr.height]="alto()" fill="#000000"></rect>
        }
      </svg>
    }
  `
})
export class BarcodeComponent {
  /** Valor a codificar (se pasa a mayúsculas; Code39 no distingue minúsculas). */
  value = input<string>('');
  /** Ancho de la barra angosta en px. */
  modulo = input<number>(2);
  /** Alto del código en px. */
  alto = input<number>(56);

  // Patrón Code39: 9 elementos por carácter (barra/espacio alternados), n=angosto w=ancho
  private static readonly TABLA: Record<string, string> = {
    '0': 'nnnwwnwnn', '1': 'wnnwnnnnw', '2': 'nnwwnnnnw', '3': 'wnwwnnnnn', '4': 'nnnwwnnnw',
    '5': 'wnnwwnnnn', '6': 'nnwwwnnnn', '7': 'nnnwnnwnw', '8': 'wnnwnnwnn', '9': 'nnwwnnwnn',
    'A': 'wnnnnwnnw', 'B': 'nnwnnwnnw', 'C': 'wnwnnwnnn', 'D': 'nnnnwwnnw', 'E': 'wnnnwwnnn',
    'F': 'nnwnwwnnn', 'G': 'nnnnnwwnw', 'H': 'wnnnnwwnn', 'I': 'nnwnnwwnn', 'J': 'nnnnwwwnn',
    'K': 'wnnnnnnww', 'L': 'nnwnnnnww', 'M': 'wnwnnnnwn', 'N': 'nnnnwnnww', 'O': 'wnnnwnnwn',
    'P': 'nnwnwnnwn', 'Q': 'nnnnnnwww', 'R': 'wnnnnnwwn', 'S': 'nnwnnnwwn', 'T': 'nnnnwnwwn',
    'U': 'wwnnnnnnw', 'V': 'nwwnnnnnw', 'W': 'wwwnnnnnn', 'X': 'nwnnwnnnw', 'Y': 'wwnnwnnnn',
    'Z': 'nwwnwnnnn', '-': 'nwnnnnwnw', '.': 'wwnnnnwnn', ' ': 'nwwnnnwnn', '$': 'nwnwnwnnn',
    '/': 'nwnwnnnwn', '+': 'nwnnnwnwn', '%': 'nnnwnwnwn', '*': 'nwnnwnwnn'
  };

  private barrasYAncho = computed(() => {
    const texto = (this.value() ?? '').toUpperCase();
    const n = this.modulo();
    const w = n * 3;
    const secuencia = `*${[...texto].filter(c => BarcodeComponent.TABLA[c]).join('')}*`;
    const rects: { x: number; w: number }[] = [];
    let x = 0;
    for (let i = 0; i < secuencia.length; i++) {
      const patron = BarcodeComponent.TABLA[secuencia[i]];
      if (!patron) continue;
      for (let e = 0; e < 9; e++) {
        const ancho = patron[e] === 'w' ? w : n;
        if (e % 2 === 0) rects.push({ x, w: ancho });   // elementos pares = barra (negra)
        x += ancho;
      }
      x += n;   // espacio inter-carácter (angosto)
    }
    return { rects, ancho: x };
  });

  barras = computed(() => this.barrasYAncho().rects);
  ancho = computed(() => this.barrasYAncho().ancho);
}
