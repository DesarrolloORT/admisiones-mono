import { expect, Page } from '@playwright/test';

import { selectOrtOption } from './ort-controls';

type MetodoPago =
  | 'cuenta-bancaria'
  | 'tarjeta-credito'
  | 'cuenta-personal'
  | 'banred'
  | 'abitab'
  | 'paganza';

const paymentLabels: Record<MetodoPago, string> = {
  'cuenta-bancaria': 'Cuenta bancaria',
  'tarjeta-credito': 'Tarjeta de crédito',
  'cuenta-personal': 'Cuenta personal',
  banred: 'Banred',
  abitab: 'Abitab',
  paganza: 'Paganza',
};

const dynamicRadioGroupLabels: Record<string, string> = {
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
    await this.chooseRadio('lugarSecundaria', 'Uruguay');
    await this.chooseRadio('estadoEducacionSuperior', 'No cursé estudios superiores');
    await this.select('formacionMadre', 'Universitaria completa');
    await this.select('formacionPadre', 'Universitaria completa');
    await this.continue();

    await expect(this.radioGroup('anioDecisionCarrera')).toBeVisible();
  }

  public async fillAcademicDecision(): Promise<void> {
    await this.chooseRadio('anioDecisionCarrera', '1º EMS (4º año)');
    await this.select('apoyoDecision', 'Familia');
    await this.chooseRadio('anioDecisionOrt', '2º EMS (5º año)');
    await this.chooseRadio('otrasUniversidades', 'Sí');
    await this.chooseRadio('certezaDecision', 'Decidido/a');
    await this.select('motivosOrt', 'Propuesta académica');
    await this.continue();

    await expect(this.radioGroup('reunionAsesoramiento')).toBeVisible();
  }

  public async fillOrtExperience(): Promise<void> {
    await this.chooseRadio('reunionAsesoramiento', 'Sí');
    await this.chooseRadio('visitoWeb', 'Sí');
    await this.chooseRadio('visitoSede', 'Sí');
    await this.chooseRadio('recuerdaPublicidad', 'Sí');
    await this.continue();

    await expect(this.radioGroup('situacionLaboral')).toBeVisible();
  }

  public async fillWorkStatus(): Promise<void> {
    await this.chooseRadio('situacionLaboral', 'Sí, trabajo actualmente');
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
    await expect(this.page.getByText('frente.png')).toBeVisible();
    await expect(this.page.getByText('dorso.png')).toBeVisible();
    await expect(this.page.getByText('rostro.png')).toBeVisible();

    const expiration = this.page.getByRole('textbox', { name: 'Vencimiento' });
    await expiration.fill('04/02/2030');
    await expiration.blur();
    await expect(expiration).toHaveValue('04/02/2030');
    await this.continue();

    await expect(this.page.getByRole('button', { name: 'Ver reglamento' })).toBeVisible();
  }

  public async acceptRegulation(): Promise<void> {
    await this.page.getByRole('button', { name: 'Ver reglamento' }).click();
    await expect(this.page.getByRole('heading', { name: 'Reglamento estudiantil' })).toBeVisible();
    await this.page.getByRole('button', { name: 'Aceptar reglamento' }).click();
    await this.continue();

    await expect(this.page.getByRole('heading', { name: 'Confirmá tu inscripción' })).toBeVisible();
  }

  public async selectPayment(method: MetodoPago): Promise<void> {
    await this.chooseRadio('metodoPago', paymentLabels[method]);
    await this.continue();

    await expect(this.page.getByText('Confirmar inscripción', { exact: true })).toBeVisible();
  }

  public async confirmPayment(): Promise<void> {
    await this.page.getByRole('button', { name: 'Confirmar', exact: true }).click();
  }

  public async saveAndExit(): Promise<void> {
    await this.page.getByRole('button', { name: 'Cerrar inscripción' }).click();
    await expect(this.page.getByText('¿Querés salir de la inscripción?')).toBeVisible();
    await this.page.getByRole('button', { name: 'Guardar y salir' }).click();
    await expect(this.page).toHaveURL(/\/inicio/);
  }

  private async continue(): Promise<void> {
    const button = this.page.getByRole('button', { name: 'Continuar', exact: true });
    await button.focus();
    await button.press('Enter');
  }

  private async select(controlName: string, option: string): Promise<void> {
    const combobox = this.page.locator(`ort-select[formcontrolname="${controlName}"]`);
    await expect(combobox).toBeEnabled();
    await selectOrtOption(this.page, combobox, option);
  }

  private async chooseRadio(controlName: string, label: string): Promise<void> {
    const group = this.radioGroup(controlName);
    await expect(group).toBeVisible();

    const option = group.locator('ort-radio-button').filter({ hasText: label });
    await expect(option).toBeVisible();
    const radio = option.getByRole('radio');
    await radio.evaluate((element: HTMLInputElement) => element.click());
    await expect(radio).toBeChecked();
  }

  private radioGroup(controlName: string) {
    const accessibleName = dynamicRadioGroupLabels[controlName];
    if (accessibleName) {
      return this.page.getByRole('group', { name: accessibleName });
    }

    return this.page.locator(`ort-radio-group[formcontrolname="${controlName}"]`);
  }
}
