import { animate, style, transition, trigger } from '@angular/animations';

/**
 * Transición sutil entre rutas: el contenido entra con un fade + leve
 * desplazamiento hacia arriba. Se aplica a un contenedor cuyo valor de
 * binding cambia con cada navegación.
 */
export const routeFade = trigger('routeFade', [
  transition('* => *', [
    style({ opacity: 0 }),
    animate('160ms cubic-bezier(0.4, 0, 0.2, 1)', style({ opacity: 1 }))
  ])
]);
