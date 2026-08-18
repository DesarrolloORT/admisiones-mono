import { expect, Locator, Page } from '@playwright/test';

import { selectOrtOption } from './ort-controls';

type PaymentMethod =
  'bank-account' | 'geopay' | 'personal-account' | 'banred' | 'abitab' | 'paganza';

const paymentLabels: Record<PaymentMethod, string> = {
  'bank-account': 'Cuenta bancaria',
  geopay: 'Geopay',
  'personal-account': 'Cuenta personal',
  banred: 'Banred',
  abitab: 'Abitab',
  paganza: 'Paganza',
};

const dynamicRadioGroupLabels: Record<string, string> = {
  isCorporate: '¿A título de quién deseás realizar la inscripción?',
  advisingMeeting: '¿Tuviste una reunión de asesoramiento?',
  visitedWebsite: '¿Visitaste el sitio web de ORT?',
  visitedCampus: '¿Visitaste las instalaciones de ORT?',
  recallsAdvertising: '¿Recordás haber visto publicidad de ORT?',
};

export class EnrollmentPage {
  constructor(private readonly page: Page) {}

  public async goto(
    scenario: 'first-time' | 'partial' | 'survey-complete' = 'first-time',
    forceInProgress = false
  ): Promise<void> {
    const scenarioQueryValue = {
      'first-time': 'primera-vez',
      partial: 'parcial',
      'survey-complete': 'encuesta-completa',
    }[scenario];
    const params = new URLSearchParams({ escenario: scenarioQueryValue });
    if (scenario !== 'first-time') {
      params.set('idProducto', '20');
      params.set('idProceso', '200');
      params.append('idOferta', '300');
      params.set('estado', 'En proceso');
      params.set('nivel', '1');
    }
    if (forceInProgress) params.set('resultado', 'en-proceso');

    await this.page.goto(`/inscripciones?${params.toString()}`);
    await expect(
      this.page
        .getByRole('heading', {
          name: /Propuesta académica|Información personal|Confirmación|Inscripción en proceso/,
        })
        .first()
    ).toBeVisible();
  }

  public async fillAcademicProposal(): Promise<void> {
    await this.chooseRadio('proposalType', 'Carrera universitaria');
    await this.select('degreeProgram', 'Licenciatura en Diseño Gráfico');
    await this.select('intake', 'Marzo 2027');
    await this.select('shift', 'Matutino');
    await this.continue();

    await expect(
      this.page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toBeVisible();
  }

  // Actualización profesional: Programa + multi-select de seminarios; el selector
  // de seminarios permanece oculto hasta elegir un programa y no hay Comienzo/Turno.
  public async fillProfessionalUpdateProposal(): Promise<void> {
    await this.chooseRadio('proposalType', 'Actualización profesional');
    await expect(this.responsiveSelect('degreeProgram')).toContainText('Programa');
    await expect(this.responsiveSelect('seminars')).toHaveCount(0);
    await expect(this.responsiveSelect('intake')).toHaveCount(0);
    await expect(this.responsiveSelect('shift')).toHaveCount(0);

    await this.select('degreeProgram', 'Programa de Asesoramiento Financiero');
    await expect(this.responsiveSelect('seminars')).toContainText('Seminario');
    await this.select('seminars', 'Marco legal y tributario');
    await this.continue();

    await expect(
      this.page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toBeVisible();
  }

  public async fillEducation(): Promise<void> {
    await this.chooseRadio('studiesHighSchool', 'Sí, estoy cursando');
    await this.chooseRadio('highSchoolYear', 'Durante secundaria');
    await this.select('orientation', 'Matemática');
    await this.chooseRadio('repeatsHighSchoolYear', 'No');
    await this.chooseRadio('highSchoolLocation', 'Uruguay');
    await this.select('state', 'Montevideo');
    await this.select('educationalInstitution', 'Liceo Nº 1');
    await this.chooseRadio('higherEducationStatus', 'No cursé estudios superiores');
    await this.select('motherEducation', 'Universitaria completa');
    await this.chooseRadio('motherOrtDegree', 'No');
    await this.select('fatherEducation', 'Universitaria completa');
    await this.chooseRadio('fatherOrtDegree', 'No');
    await this.continue();

    await expect(this.radioGroup('degreeProgramDecisionYear')).toBeVisible();
  }

  public async fillAcademicDecision(): Promise<void> {
    await this.chooseRadio('degreeProgramDecisionYear', 'Durante secundaria');
    await this.select('decisionSupport', 'Familia');
    await this.chooseRadio('ortDecisionYear', '2º EMS (5º año)');
    await this.chooseRadio('otherUniversities', 'Sí');
    await this.chooseRadio('decisionCertainty', 'Decidido/a');
    await this.select('ortReasons', 'Propuesta académica');
    await this.continue();

    await expect(this.radioGroup('advisingMeeting')).toBeVisible();
  }

  public async fillOrtExperience(): Promise<void> {
    await this.chooseRadio('advisingMeeting', 'No');
    await this.chooseRadio('visitedWebsite', 'No');
    await this.chooseRadio('visitedCampus', 'No');
    await this.chooseRadio('recallsAdvertising', 'No recuerdo');
    await this.continue();

    await expect(this.page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();
  }

  public async fillEnrollmentOwnership(isCorporate: boolean): Promise<void> {
    await this.chooseRadio(
      'isCorporate',
      isCorporate ? 'Inscripción corporativa' : 'Inscripción a título personal'
    );
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

    await fileInputs.nth(0).setInputFiles({ ...image, name: 'front.png' });
    await fileInputs.nth(1).setInputFiles({ ...image, name: 'back.png' });
    await fileInputs.nth(2).setInputFiles({ ...image, name: 'rostro.png' });
    await expect(this.page.getByText('front.png', { exact: true })).toBeVisible();
    await expect(this.page.getByText('back.png', { exact: true })).toBeVisible();
    await expect(this.page.getByText('rostro.png', { exact: true })).toBeVisible();

    const expiration = this.page.getByRole('textbox', { name: 'Vencimiento' });
    await expiration.focus();
    await this.setExpirationDate(expiration);
    await this.continue();

    await expect(this.regulationReaderButton()).toBeVisible();
  }

  public async continueWithPreloadedIdentity(): Promise<void> {
    await expect(this.page.getByText('identity-document-front.png', { exact: true })).toBeVisible();
    await expect(this.page.getByText('identity-document-back.png', { exact: true })).toBeVisible();
    await expect(this.page.getByText('identity-photo.png', { exact: true })).toBeVisible();
    await expect(this.page.getByRole('textbox', { name: 'Vencimiento' })).toHaveValue('04/02/2030');

    await this.page
      .getByRole('checkbox', { name: 'Verifico que la identidad es correcta' })
      .check();
    await expect(this.regulationReaderButton()).toBeVisible();
  }
  public async acceptRegulation(destination: 'payment' | 'corporate' = 'payment'): Promise<void> {
    await this.regulationReaderButton().click();
    await expect(
      this.page.getByRole('heading', { name: 'Reglamento estudiantil', level: 1 })
    ).toBeVisible();
    await this.page.getByRole('button', { name: 'Aceptar reglamento' }).click();
    await this.continue();

    await expect(
      destination === 'corporate'
        ? this.page.getByRole('heading', { name: 'Inscripción corporativa pendiente' })
        : this.page.getByRole('heading', { name: 'Confirmación', exact: true, level: 2 })
    ).toBeVisible();
  }

  public async selectPayment(method: PaymentMethod): Promise<void> {
    await this.chooseRadio('paymentMethod', paymentLabels[method]);
    await this.pay();
  }

  public async completeInitialEnrollmentWithKeyboard(): Promise<void> {
    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('proposalType', 'Carrera universitaria');
    await this.selectWithKeyboard('degreeProgram', 'Licenciatura en Diseño Gráfico');
    await this.selectWithKeyboard('intake', 'Marzo 2027');
    await this.selectWithKeyboard('shift', 'Matutino');
    await this.continueWithKeyboard();

    await expect(
      this.page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toBeVisible();
    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('studiesHighSchool', 'Sí, estoy cursando');
    await this.chooseRadioWithKeyboard('highSchoolYear', 'Durante secundaria');
    await this.selectWithKeyboard('orientation', 'Matemática');
    await this.chooseRadioWithKeyboard('repeatsHighSchoolYear', 'No');
    await this.chooseRadioWithKeyboard('highSchoolLocation', 'Uruguay');
    await this.selectWithKeyboard('state', 'Montevideo');
    await this.selectWithKeyboard('educationalInstitution', 'Liceo Nº 1');
    await this.chooseRadioWithKeyboard('higherEducationStatus', 'No cursé estudios superiores');
    await this.selectWithKeyboard('motherEducation', 'Universitaria completa');
    await this.chooseRadioWithKeyboard('motherOrtDegree', 'No');
    await this.selectWithKeyboard('fatherEducation', 'Universitaria completa');
    await this.chooseRadioWithKeyboard('fatherOrtDegree', 'No');
    await this.continueWithKeyboard();

    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('degreeProgramDecisionYear', 'Durante secundaria');
    await this.selectWithKeyboard('decisionSupport', 'Familia');
    await this.chooseRadioWithKeyboard('ortDecisionYear', '2º EMS (5º año)');
    await this.chooseRadioWithKeyboard('otherUniversities', 'Sí');
    await this.chooseRadioWithKeyboard('decisionCertainty', 'Decidido/a');
    await this.selectWithKeyboard('ortReasons', 'Propuesta académica');
    await this.continueWithKeyboard();

    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('advisingMeeting', 'No');
    await this.chooseRadioWithKeyboard('visitedWebsite', 'No');
    await this.chooseRadioWithKeyboard('visitedCampus', 'No');
    await this.chooseRadioWithKeyboard('recallsAdvertising', 'No recuerdo');
    await this.continueWithKeyboard();

    await this.expectMainFocus();
    await this.uploadIdentityFileWithKeyboard(0, 'front.png');
    await this.uploadIdentityFileWithKeyboard(1, 'back.png');
    const expiration = this.page.getByRole('textbox', { name: 'Vencimiento' });
    await this.tabTo(expiration);
    await this.setExpirationDate(expiration);
    await this.uploadIdentityFileWithKeyboard(2, 'rostro.png');
    await this.continueWithKeyboard();

    await this.expectMainFocus();
    const regulationButton = this.regulationReaderButton();
    await this.tabTo(regulationButton);
    await this.page.keyboard.press('Enter');

    await expect(
      this.page.getByRole('heading', { name: 'Reglamento estudiantil', level: 1 })
    ).toBeVisible();
    await this.expectMainFocus();
    const acceptRegulationButton = this.page.getByRole('button', {
      name: 'Aceptar reglamento',
    });
    await this.tabTo(acceptRegulationButton);
    await this.page.keyboard.press('Enter');

    await this.expectMainFocus();
    await expect(
      this.page.getByRole('checkbox', {
        name: 'He leído y acepto el reglamento estudiantil de la universidad.',
      })
    ).toBeChecked();
    await this.continueWithKeyboard();

    await expect(
      this.page.getByRole('heading', { name: 'Confirmación', exact: true, level: 2 })
    ).toBeVisible();
    await this.expectMainFocus();
    await this.chooseRadioWithKeyboard('paymentMethod', paymentLabels['personal-account']);
    await this.payWithKeyboard();
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
      this.page.getByRole('heading', { name: 'Propuesta académica', exact: true })
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
    const button = this.paymentSubmitButton();
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
    const button = this.paymentSubmitButton();
    await this.tabTo(button);
    await this.page.keyboard.press('Enter');
  }

  private paymentSubmitButton(): Locator {
    return this.page.locator('.enrollment-payment-submit');
  }

  private regulationReaderButton(): Locator {
    return this.page
      .getByRole('button', { name: /^(Ver reglamento|reglamento estudiantil)$/i })
      .last();
  }

  private async select(controlName: string, option: string): Promise<void> {
    const responsiveSelect = this.responsiveSelect(controlName);
    const combobox = this.page.locator(`ort-select[formcontrolname="${controlName}"]`);
    await expect(responsiveSelect.or(combobox).first()).toBeAttached();

    if ((await responsiveSelect.count()) > 0) {
      const mobileTrigger = responsiveSelect.locator('.responsive-select__mobile-trigger');
      if (await mobileTrigger.isVisible()) {
        await this.selectFromResponsiveDrawer(mobileTrigger, option);
        return;
      }

      const combobox = responsiveSelect.locator('ort-select');
      await this.expectOrtSelectEnabled(combobox);
      await selectOrtOption(this.page, combobox, option);
      return;
    }

    await this.expectOrtSelectEnabled(combobox);
    await selectOrtOption(this.page, combobox, option);
  }

  // toBeEnabled no contempla aria-disabled en elementos custom como ort-select.
  private async expectOrtSelectEnabled(combobox: Locator): Promise<void> {
    await expect(combobox).toBeEnabled();
    await expect(combobox).toHaveAttribute('aria-disabled', 'false');
  }

  private async selectWithKeyboard(controlName: string, option: string): Promise<void> {
    const responsiveSelect = this.responsiveSelect(controlName);
    const combobox = this.page.locator(`ort-select[formcontrolname="${controlName}"]`);
    await expect(responsiveSelect.or(combobox).first()).toBeAttached();

    if ((await responsiveSelect.count()) > 0) {
      const mobileTrigger = responsiveSelect.locator('.responsive-select__mobile-trigger');
      if (await mobileTrigger.isVisible()) {
        await this.selectFromResponsiveDrawerWithKeyboard(mobileTrigger, option);
        return;
      }

      await this.selectOrtWithKeyboard(responsiveSelect.locator('ort-select'), option);
      return;
    }

    await this.selectOrtWithKeyboard(combobox, option);
  }

  private async selectOrtWithKeyboard(combobox: Locator, option: string): Promise<void> {
    await this.expectOrtSelectEnabled(combobox);
    await this.tabTo(combobox);
    await this.page.keyboard.press('Enter');

    await expect(combobox).toHaveAttribute('aria-controls', /.+/);
    const listboxId = await combobox.getAttribute('aria-controls');
    if (!listboxId) throw new Error('El select no expuso el listbox activo.');

    const listbox = this.page.locator(`#${listboxId}`);
    const targetOption = listbox.getByRole('option', { name: option });
    await expect(targetOption).toBeVisible();

    await targetOption.evaluate(
      () =>
        new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))
    );
    await this.page.keyboard.press('Home');
    for (let index = 0; index < 30; index += 1) {
      const activeOption = listbox.locator('ort-option.ort-option-active');
      if ((await activeOption.textContent())?.includes(option)) {
        await this.page.keyboard.press('Enter');
        await expect(combobox).toContainText(option);
        if ((await combobox.getAttribute('aria-expanded')) === 'true') {
          await this.page.keyboard.press('Escape');
        }
        await expect(combobox).toHaveAttribute('aria-expanded', 'false');
        await expect(combobox).toBeFocused();
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
