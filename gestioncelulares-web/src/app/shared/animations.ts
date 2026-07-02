import { animate, style, transition, trigger } from '@angular/animations';

/**
 * Transición sutil entre rutas: el contenido entra con un fade + leve
 * desplazamiento hacia arriba. Se aplica a un contenedor cuyo valor de
 * binding cambia con cada navegación.
 */
export const routeFade = trigger('routeFade', [
  transition('* => *', [
    style({ opacity: 0, transform: 'translateY(10px) scale(0.995)' }),
    animate('260ms cubic-bezier(0.22, 1, 0.36, 1)', style({ opacity: 1, transform: 'translateY(0) scale(1)' }))
  ])
]);
