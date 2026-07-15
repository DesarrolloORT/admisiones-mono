import { expect, Locator, Page } from '@playwright/test';

import { selectOrtOption } from './ort-controls';

type MetodoPago =
  'cuenta-bancaria' | 'tarjeta-credito' | 'cuenta-personal' | 'banred' | 'abitab' | 'paganza';

const paymentLabels: Record<MetodoPago, string> = {
  'cuenta-bancaria': 'Cuenta bancaria',
  'tarjeta-credito': 'Tarjeta de crédito',
  'cuenta-personal': 'Cuenta personal',
  banred: 'Banred',
  abitab: 'Abitab',
  paganza: 'Paganza',
};

const dynamicRadioGroupLabels: Record<string, string> = {
  situacionLaboral: '¿Trabajás actualmente?',
  reunionAsesoramiento: '¿Tuviste una reunión de asesoramiento?',
  visitoWeb: '¿Visitaste el sitio web de ORT?',
  visitoSede: '¿Visitaste las instalaciones de ORT?',
  recuerdaPublicidad: '¿Recordás haber visto publicidad de ORT?',
};

export class InscripcionPage {
  constructor(private readonly page: Page) {}

  public async goto(
    escenario: 'primera-vez' | 'parcial' | 'encuesta-completa' = 'primera-vez',
    resultado?: 'en-proceso'
  ): Promise<void> {
    const params = new URLSearchParams({ escenario });
    if (resultado) params.set('resultado', resultado);

    await this.page.goto(`/inscripciones?${params.toString()}`);
    await expect(
      this.page.getByRole('heading', {
        level: 1,
        name: /Inscripción a carrera|Información personal|Confirmación|Inscripción en proceso/,
      })
    ).toBeVisible();
  }

  public async fillAcademicProposal(): Promise<void> {
    await this.chooseRadio('tipoPropuesta', 'Carrera universitaria');
    await this.select('carrera', 'Licenciatura en Diseño Gráfico');
    await this.select('comienzo', 'Marzo 2027');
    await this.select('turno', 'Matutino');
    await this.continue();

    await expect(
      this.page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toBeVisible();
  }

  public async fillEducation(): Promise<void> {
    await this.chooseRadio('cursaSecundaria', 'Sí, estoy cursando');
    await this.chooseRadio('anioSecundaria', 'Durante secundaria');
    await this.select('tipoBachillerato', 'Científico');
    await this.select('orientacion', 'Matemática');
    await this.chooseRadio('lugarSecundaria', 'Uruguay');
    await this.chooseRadio('estadoEducacionSuperior', 'No cursé estudios superiores');
    await this.select('formacionMadre', 'Universitaria completa');
    await this.chooseRadio('tituloOrtMadre', 'No');
    await this.select('formacionPadre', 'Universitaria completa');
    await this.continue();

    await expect(this.radioGroup('anioDecisionCarrera')).toBeVisible();
  }

  public async fillAcademicDecision(): Promise<void> {
    await this.chooseRadio('anioDecisionCarrera', 'Durante secundaria');
    await this.select('apoyoDecision', 'Familia');
    await this.chooseRadio('anioDecisionOrt', '2º EMS (5º año)');
    await this.chooseRadio('otrasUniversidades', 'Sí');
    await this.chooseRadio('certezaDecision', 'Decidido/a');
    await this.select('motivosOrt', 'Propuesta académica');
    await this.continue();

    await expect(this.radioGroup('reunionAsesoramiento')).toBeVisible();
  }

  public async fillOrtExperience(): Promise<void> {
    await this.chooseRadio('reunionAsesoramiento', 'No');
    await this.chooseRadio('visitoWeb', 'No');
    await this.chooseRadio('visitoSede', 'No');
    await this.chooseRadio('recuerdaPublicidad', 'No recuerdo');
    await this.continue();

    await expect(this.radioGroup('situacionLaboral')).toBeVisible();
  }

  public async fillWorkStatus(): Promise<void> {
    await this.chooseRadio('situacionLaboral', 'No trabajo actualmente');
    await this.continue();

    await expect(this.page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();
  }

  public async fillIdentity(): Promise<void> {
    const fileInputs = this.page.locator('ort-file-uploader input[type="file"]');
    await expect(fileInputs).toHaveCount(3);

    const image = {
      name: 'identidad.png',
      mimeType: 'image/png',
      buffer: Buffer.from(
        'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZQMcAAAAASUVORK5CYII=',
        'base64'
      ),
    };

    await fileInputs.nth(0).setInputFiles({ ...image, name: 'frente.png' });
    await fileInputs.nth(1).setInputFiles({ ...image, name: 'dorso.png' });
    await fileInputs.nth(2).setInputFiles({ ...image, name: 'rostro.png' });
    await expect(this.page.getByText('frente.png', { exact: true })).toBeVisible();
    await expect(this.page.getByText('dorso.png', { exact: true })).toBeVisible();
    await expect(this.page.getByText('rostro.png', { exact: true })).toBeVisible();

    const expiration = this.page.getByRole('textbox', { name: 'Vencimiento' });
    await expiration.focus();
    await this.setExpirationDate(expiration);
    await this.continue();

    await expect(this.page.getByRole('button', { name: 'Ver reglamento' })).toBeVisible();
  }

  public async continueWithPreloadedIdentity(): Promise<void> {
    await expect(this.page.getByText('documento-frente.png', { exact: true })).toBeVisible();
    await expect(this.page.getByText('documento-dorso.png', { exact: true })).toBeVisible();
    await expect(this.page.getByText('foto-persona.jpg', { exact: true })).toBeVisible();
    await expect(this.page.getByRole('textbox', { name: 'Vencimiento' })).toHaveValue('04/02/2030');

    await this.page
      .getByRole('checkbox', { name: 'Verifico que la identidad es correcta' })
      .check();
    await expect(this.page.getByRole('button', { name: 'Ver reglamento' })).toBeVisible();
  }
  public async acceptRegulation(): Promise<void> {
    await this.page.getByRole('button', { name: 'Ver reglamento' }).click();
    await expect(this.page.getByRole('heading', { name: 'Reglamento estudiantil' })).toBeVisible();
    await this.page.getByRole('button', { name: 'Aceptar reglamento' }).click();
    await this.continue();

    await expect(
      this.page.getByRole('heading', { name: 'Confirmación', exact: true, level: 1 })
    ).toBeVisible();
  }

  public async selectPayment(method: MetodoPago): Promise<void> {
    await this.chooseRadio('metodoPago', paymentLabels[method]);
    await this.pay();

    await expect(this.page.getByRole('dialog', { name: 'Confirmar inscripción' })).toBeVisible();
  }

  public async confirmPayment(): Promise<void> {
    await this.page.getByRole('button', { name: 'Confirmar', exact: true }).click();
  }

  public async completeInitialEnrollmentWithKeyboard(): Promise<void> {
    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('tipoPropuesta', 'Carrera universitaria');
    await this.selectWithKeyboard('carrera', 'Licenciatura en Diseño Gráfico');
    await this.selectWithKeyboard('comienzo', 'Marzo 2027');
    await this.selectWithKeyboard('turno', 'Matutino');
    await this.continueWithKeyboard();

    await expect(
      this.page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toBeVisible();
    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('cursaSecundaria', 'Sí, estoy cursando');
    await this.chooseRadioWithKeyboard('anioSecundaria', 'Durante secundaria');
    await this.selectWithKeyboard('tipoBachillerato', 'Científico');
    await this.selectWithKeyboard('orientacion', 'Matemática');
    await this.chooseRadioWithKeyboard('lugarSecundaria', 'Uruguay');
    await this.chooseRadioWithKeyboard('estadoEducacionSuperior', 'No cursé estudios superiores');
    await this.selectWithKeyboard('formacionMadre', 'Universitaria completa');
    await this.chooseRadioWithKeyboard('tituloOrtMadre', 'No');
    await this.selectWithKeyboard('formacionPadre', 'Universitaria completa');
    await this.continueWithKeyboard();

    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('anioDecisionCarrera', 'Durante secundaria');
    await this.selectWithKeyboard('apoyoDecision', 'Familia');
    await this.chooseRadioWithKeyboard('anioDecisionOrt', '2º EMS (5º año)');
    await this.chooseRadioWithKeyboard('otrasUniversidades', 'Sí');
    await this.chooseRadioWithKeyboard('certezaDecision', 'Decidido/a');
    await this.selectWithKeyboard('motivosOrt', 'Propuesta académica');
    await this.continueWithKeyboard();

    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('reunionAsesoramiento', 'No');
    await this.chooseRadioWithKeyboard('visitoWeb', 'No');
    await this.chooseRadioWithKeyboard('visitoSede', 'No');
    await this.chooseRadioWithKeyboard('recuerdaPublicidad', 'No recuerdo');
    await this.continueWithKeyboard();

    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('situacionLaboral', 'No trabajo actualmente');
    await this.continueWithKeyboard();

    await this.expectMainFocus();
    await this.uploadIdentityFileWithKeyboard(0, 'frente.png');
    await this.uploadIdentityFileWithKeyboard(1, 'dorso.png');
    const expiration = this.page.getByRole('textbox', { name: 'Vencimiento' });
    await this.tabTo(expiration);
    await this.setExpirationDate(expiration);
    await this.uploadIdentityFileWithKeyboard(2, 'rostro.png');
    await this.continueWithKeyboard();

    await this.expectMainFocus();
    const regulationButton = this.page.getByRole('button', { name: 'Ver reglamento' });
    await this.tabTo(regulationButton);
    await this.page.keyboard.press('Enter');

    await expect(this.page.getByRole('heading', { name: 'Reglamento estudiantil' })).toBeVisible();
    await this.expectMainFocus();
    const acceptRegulationButton = this.page.getByRole('button', {
      name: 'Aceptar reglamento',
    });
    await this.tabTo(acceptRegulationButton);
    await this.page.keyboard.press('Enter');

    await this.expectMainFocus();
    await expect(
      this.page.getByRole('checkbox', { name: 'Acepto el reglamento estudiantil.' })
    ).toBeChecked();
    await this.continueWithKeyboard();

    await expect(
      this.page.getByRole('heading', { name: 'Confirmación', exact: true, level: 1 })
    ).toBeVisible();
    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('metodoPago', paymentLabels['cuenta-personal']);
    await this.payWithKeyboard();

    const confirmButton = this.page.getByRole('button', { name: 'Confirmar', exact: true });
    await expect(this.page.getByRole('button', { name: 'Volver', exact: true })).toBeFocused();
    await this.page.keyboard.press('Tab');
    await expect(confirmButton).toBeFocused();
    await this.page.keyboard.press('Enter');
  }

  public async expectEnterOnFocusedRadioDoesNotAdvance(
    controlName: string,
    label: string
  ): Promise<void> {
    const group = this.radioGroup(controlName);
    await expect(group).toBeVisible();

    const radio = group.getByRole('radio', {
      name: new RegExp(`^${escapeRegExp(label)}(?:\\s|$)`),
    });
    await this.tabTo(radio);
    await this.page.keyboard.press('Enter');

    await expect(
      this.page.getByRole('heading', { name: 'Inscripción a carrera', exact: true })
    ).toBeVisible();
    await expect(
      this.page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toHaveCount(0);
    await expect(radio).not.toBeChecked();

    await this.page.keyboard.press('Space');
    await expect(radio).toBeChecked();
  }

  public async saveAndExit(): Promise<void> {
    await this.page.getByRole('button', { name: 'Cerrar inscripción' }).click();
    await expect(this.page.getByText('¿Querés salir de la inscripción?')).toBeVisible();
    await this.page.getByRole('button', { name: 'Guardar y salir' }).click();
    await expect(this.page).toHaveURL(/\/inicio/);
  }

  private async continue(): Promise<void> {
    const button = this.page.getByRole('button', { name: 'Continuar', exact: true });
    await button.click();
  }

  private async pay(): Promise<void> {
    const button = this.page.getByRole('button', { name: 'Pagar', exact: true });
    await button.focus();
    await expect(button).toBeFocused();
    await this.page.keyboard.press('Enter');
  }

  private async continueWithKeyboard(): Promise<void> {
    const button = this.page.getByRole('button', { name: 'Continuar', exact: true });
    await this.tabTo(button);
    await this.page.keyboard.press('Enter');
  }

  private async payWithKeyboard(): Promise<void> {
    const button = this.page.getByRole('button', { name: 'Pagar', exact: true });
    await this.tabTo(button);
    await this.page.keyboard.press('Enter');
  }

  private async select(controlName: string, option: string): Promise<void> {
    const responsiveSelect = this.responsiveSelect(controlName);
    if ((await responsiveSelect.count()) > 0) {
      const mobileTrigger = responsiveSelect.locator('.responsive-select__mobile-trigger');
      if (await mobileTrigger.isVisible()) {
        await this.selectFromResponsiveDrawer(mobileTrigger, option);
        return;
      }

      const combobox = responsiveSelect.locator('ort-select');
      await expect(combobox).toBeEnabled();
      await selectOrtOption(this.page, combobox, option);
      return;
    }

    const combobox = this.page.locator(`ort-select[formcontrolname="${controlName}"]`);
    await expect(combobox).toBeEnabled();
    await selectOrtOption(this.page, combobox, option);
  }

  private async selectWithKeyboard(controlName: string, option: string): Promise<void> {
    const responsiveSelect = this.responsiveSelect(controlName);
    if ((await responsiveSelect.count()) > 0) {
      const mobileTrigger = responsiveSelect.locator('.responsive-select__mobile-trigger');
      if (await mobileTrigger.isVisible()) {
        await this.selectFromResponsiveDrawerWithKeyboard(mobileTrigger, option);
        return;
      }

      await this.selectOrtWithKeyboard(responsiveSelect.locator('ort-select'), option);
      return;
    }

    await this.selectOrtWithKeyboard(
      this.page.locator(`ort-select[formcontrolname="${controlName}"]`),
      option
    );
  }

  private async selectOrtWithKeyboard(combobox: Locator, option: string): Promise<void> {
    await expect(combobox).toBeEnabled();
    await this.tabTo(combobox);
    await this.page.keyboard.press('Enter');

    const targetOption = this.page.getByRole('option', { name: option });
    await expect(targetOption).toBeVisible();
    const targetId = await targetOption.getAttribute('id');
    expect(targetId).toBeTruthy();

    await this.page.keyboard.press('Home');
    for (let index = 0; index < 30; index += 1) {
      if ((await combobox.getAttribute('aria-activedescendant')) === targetId) {
        for (let attempt = 0; attempt < 3; attempt += 1) {
          await this.page.keyboard.press('Enter');
          if ((await combobox.textContent())?.includes(option)) {
            await this.page.keyboard.press('Escape');
            return;
          }
        }

        await expect(combobox).toContainText(option);
        await this.page.keyboard.press('Escape');
        return;
      }

      await this.page.keyboard.press('ArrowDown');
    }

    throw new Error(`No se pudo seleccionar "${option}" con teclado.`);
  }

  private async selectFromResponsiveDrawer(trigger: Locator, option: string): Promise<void> {
    await expect(trigger).toBeEnabled();
    await trigger.click();
    await expect(this.page.getByRole('dialog', { name: /Seleccionar/ })).toBeVisible();

    const drawerOption = await this.responsiveDrawerOption(option);
    await expect(drawerOption).toBeVisible();
    await drawerOption.click();
    await this.page.getByRole('button', { name: 'Seleccionar' }).click();
    await expect(trigger).toContainText(option);
  }

  private async selectFromResponsiveDrawerWithKeyboard(
    trigger: Locator,
    option: string
  ): Promise<void> {
    await expect(trigger).toBeEnabled();
    await this.tabTo(trigger);
    await this.page.keyboard.press('Enter');
    await expect(this.page.getByRole('dialog', { name: /Seleccionar/ })).toBeVisible();

    const drawerOption = await this.responsiveDrawerOption(option);
    await expect(drawerOption).toBeVisible();
    await drawerOption.focus();
    await this.page.keyboard.press('Space');
    await expect(drawerOption).toHaveAttribute('aria-checked', 'true');

    const confirmButton = this.page.getByRole('button', { name: 'Seleccionar' });
    await this.tabTo(confirmButton);
    await this.page.keyboard.press('Enter');
    await expect(trigger).toBeFocused();
  }
  private responsiveSelect(controlName: string): Locator {
    return this.page.locator(`app-responsive-select[formcontrolname="${controlName}"]`);
  }

  private async responsiveDrawerOption(option: string): Promise<Locator> {
    const name = new RegExp(`^${escapeRegExp(option)}(?:\\s|$)`);
    return this.page
      .getByRole('radio', { name })
      .or(this.page.getByRole('checkbox', { name }))
      .first();
  }

  private async chooseRadio(controlName: string, label: string): Promise<void> {
    const group = this.radioGroup(controlName);
    await expect(group).toBeVisible();

    const radio = group.getByRole('radio', {
      name: new RegExp(`^${escapeRegExp(label)}(?:\\s|$)`),
    });
    await expect(radio).toBeAttached();
    await expect(radio).toBeEnabled();
    await radio.evaluate((element: HTMLInputElement) => element.click());
    await expect(radio).toBeChecked();
  }

  private async chooseRadioWithKeyboard(controlName: string, label: string): Promise<void> {
    const group = this.radioGroup(controlName);
    await expect(group).toBeVisible();

    const radios = group.getByRole('radio');
    const target = group.getByRole('radio', {
      name: new RegExp(`^${escapeRegExp(label)}(?:\\s|$)`),
    });
    await expect(target).toBeAttached();
    await this.tabTo(radios.first());

    const count = await radios.count();
    for (let index = 0; index < count; index += 1) {
      if (await target.evaluate(element => element === element.ownerDocument.activeElement)) {
        if (!(await target.isChecked())) await this.page.keyboard.press('Space');
        await expect(target).toBeChecked();
        return;
      }

      await this.page.keyboard.press('ArrowDown');
    }

    throw new Error(`No se pudo elegir "${label}" con teclado.`);
  }

  private async uploadIdentityFileWithKeyboard(index: number, name: string): Promise<void> {
    const uploader = this.page.locator('ort-file-uploader').nth(index);
    const button = uploader.getByRole('button').first();
    await this.tabTo(button);

    const fileChooserPromise = this.page.waitForEvent('filechooser');
    await this.page.keyboard.press('Enter');
    const fileChooser = await fileChooserPromise;
    await fileChooser.setFiles({
      name,
      mimeType: 'image/png',
      buffer: Buffer.from(
        'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZQMcAAAAASUVORK5CYII=',
        'base64'
      ),
    });

    await expect(this.page.getByText(name, { exact: true })).toBeVisible();
  }

  private async setExpirationDate(expiration: Locator): Promise<void> {
    await expect(expiration).toBeFocused();
    await expiration.evaluate((input: HTMLInputElement) => {
      input.value = '04/02/2030';
      input.dispatchEvent(new Event('input', { bubbles: true }));
      input.dispatchEvent(new Event('change', { bubbles: true }));
      input.dispatchEvent(new Event('blur', { bubbles: true }));
    });
    await expect(expiration).toHaveValue('04/02/2030');
  }

  private async expectMainFocus(): Promise<void> {
    await expect(this.page.locator('#main-content')).toBeFocused();
  }

  private async tabTo(target: Locator): Promise<void> {
    await expect(target).toBeAttached();

    for (let index = 0; index < 180; index += 1) {
      const containsFocus = await target.evaluate(
        element =>
          element === element.ownerDocument.activeElement ||
          element.contains(element.ownerDocument.activeElement)
      );
      if (containsFocus) return;

      await this.page.keyboard.press('Tab');
    }

    throw new Error('El control esperado no es alcanzable con Tab.');
  }

  private radioGroup(controlName: string) {
    const accessibleName = dynamicRadioGroupLabels[controlName];
    if (accessibleName) {
      return this.page.getByRole('group', { name: accessibleName });
    }

    return this.page.locator(`ort-radio-group[formcontrolname="${controlName}"]`);
  }
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
