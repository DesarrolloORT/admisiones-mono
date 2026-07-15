import { computed, Signal, signal } from '@angular/core';

export interface PasswordVisibility {
  /** Si el valor está visible como texto plano. Útil para `aria-pressed`. */
  readonly visible: Signal<boolean>;
  /** Valor para el atributo `type` del input. */
  readonly inputType: Signal<'text' | 'password'>;
  /** Ícono del botón de alternancia. */
  readonly icon: Signal<'visibility' | 'visibility_off'>;
  /** Etiqueta accesible del botón de alternancia. */
  readonly toggleLabel: Signal<string>;
  toggle(): void;
}

/**
 * Estado de mostrar/ocultar un campo de contraseña, listo para bindear al
 * patrón de toggle con `ortSuffix`. Crear una instancia por campo.
 */
export function createPasswordVisibility(subject = 'contraseña'): PasswordVisibility {
  const visible = signal(false);

  return {
    visible: visible.asReadonly(),
    inputType: computed(() => (visible() ? 'text' : 'password')),
    icon: computed(() => (visible() ? 'visibility_off' : 'visibility')),
    toggleLabel: computed(() => (visible() ? `Ocultar ${subject}` : `Mostrar ${subject}`)),
    toggle: () => visible.update(value => !value),
  };
}
