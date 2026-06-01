import { expect, Page } from '@playwright/test';

import { careerData, personalData, RegisterScenario } from '../test-data/register-scenarios';
import { clickRadioByName, selectOrtOption } from './ort-controls';

type PersonalData = typeof personalData;
type CareerData = typeof careerData;

const documentTypeLabels: Record<RegisterScenario['documentType'], string> = {
  CI: 'Cédula',
  PS: 'Pasaporte',
  DE: 'Documento extranjero',
};

export class RegisterPage {
  public constructor(private readonly page: Page) {}

  public async goto(): Promise<void> {
    await this.page.goto('/registro');
    await expect(this.page.getByRole('heading', { name: 'Crear cuenta' })).toBeVisible();
  }

  public async fillIdentity(scenario: RegisterScenario): Promise<void> {
    if (scenario.documentType !== 'CI') {
      await selectOrtOption(
        this.page,
        this.page.getByRole('combobox', { name: 'Tipo de documento' }),
        documentTypeLabels[scenario.documentType]
      );
    }

    await this.page
      .getByRole('textbox', { name: /Nro\. de (cédula|pasaporte|documento extranjero)/ })
      .fill(scenario.documentNumber);
  }

  public async scanDocument(file: {
    name: string;
    mimeType: string;
    buffer: Buffer;
  }): Promise<void> {
    await this.page.locator('#document-file').setInputFiles(file);
  }

  public async continueFromIdentity(): Promise<void> {
    await this.page.getByRole('button', { name: 'Continuar' }).click();
  }

  public async fillFullPersonalData(data: PersonalData = personalData): Promise<void> {
    await this.page.getByRole('textbox', { name: 'Primer nombre' }).fill(data.firstName);
    await this.page.getByRole('textbox', { name: 'Segundo nombre' }).fill(data.secondName);
    await this.page.getByRole('textbox', { name: 'Primer apellido' }).fill(data.firstLastName);
    await this.page.getByRole('textbox', { name: 'Segundo apellido' }).fill(data.secondLastName);
    await this.page.getByLabel('Fecha de nacimiento').fill(data.birthDate);
    await selectOrtOption(this.page, this.page.getByRole('combobox', { name: 'Sexo' }), data.sex);
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'País de residencia' }),
      data.country
    );
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'Departamento / Estado' }),
      data.state
    );
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'Ciudad' }),
      data.city
    );
    await this.page.getByRole('textbox', { name: 'Dirección' }).fill(data.address);
    await this.page.getByRole('textbox', { name: 'Celular con prefijo +598' }).fill(data.phone);
    await this.page.getByRole('textbox', { exact: true, name: 'E-mail' }).fill(data.email);
    await this.page.getByRole('textbox', { name: 'Confirmar e-mail' }).fill(data.email);
  }

  public async fillVerificationData(data: PersonalData = personalData): Promise<void> {
    await this.page.getByRole('textbox', { name: 'Primer apellido' }).fill(data.firstLastName);
    await this.page.getByRole('textbox', { exact: true, name: 'E-mail' }).fill(data.email);
  }

  public async continueFromPersonalData(): Promise<void> {
    await this.page.getByRole('button', { name: 'Continuar' }).click();
  }

  public async fillCareerSelection(data: CareerData = careerData): Promise<void> {
    await clickRadioByName(this.page, data.academicLevel);
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'Carrera' }),
      data.career
    );
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'Comienzo' }),
      data.start
    );
  }

  public async submitCareerSelection(): Promise<void> {
    await this.page.getByRole('button', { name: 'Crear cuenta' }).click();
  }

  public async expectCreatedAccount(): Promise<void> {
    await expect(this.page.getByRole('status').first()).toContainText(
      'Cuenta creada correctamente. Revisá tu correo para obtener la contraseña.'
    );
  }
}
