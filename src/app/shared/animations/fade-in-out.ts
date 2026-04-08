import { animate, style, transition, trigger } from '@angular/animations';

//TODO: add animations here

//* Example:
export const fadeInOut = trigger('fadeInOut', [
  transition(':enter', [
    style({ opacity: 0, transform: 'translateY(-20%)' }),
    animate('200ms ease-in', style({ opacity: 1, transform: 'translateY(0%)' })),
  ]),
  transition(':leave', [
    animate('200ms ease-out', style({ opacity: 0, transform: 'translateY(20%)' })),
  ]),
]);
