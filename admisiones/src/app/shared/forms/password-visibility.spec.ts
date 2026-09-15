import { createPasswordVisibility } from './password-visibility';

describe('createPasswordVisibility', () => {
  it('starts hidden and toggles all derived state together', () => {
    const visibility = createPasswordVisibility();

    expect(visibility.visible()).toBe(false);
    expect(visibility.inputType()).toBe('password');
    expect(visibility.icon()).toBe('visibility');
    expect(visibility.toggleLabel()).toBe('Mostrar contraseña');

    visibility.toggle();

    expect(visibility.visible()).toBe(true);
    expect(visibility.inputType()).toBe('text');
    expect(visibility.icon()).toBe('visibility_off');
    expect(visibility.toggleLabel()).toBe('Ocultar contraseña');
  });

  it('uses the provided subject in the toggle label', () => {
    const visibility = createPasswordVisibility('confirmación de contraseña');

    expect(visibility.toggleLabel()).toBe('Mostrar confirmación de contraseña');
  });
});
