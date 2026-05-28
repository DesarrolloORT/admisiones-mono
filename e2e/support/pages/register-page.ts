import { expect, Page } from '@playwright/test';

import { careerData, personalData, RegisterScenario } from '../test-data/register-scenarios';
import { clickRadioByName, selectOrtOption } from './ort-controls';

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

  public async continueFromIdentity(): Promise<void> {
    await this.page.getByRole('button', { name: 'Continuar' }).click();
  }

  public async fillFullPersonalData(): Promise<void> {
    await this.page.getByRole('textbox', { name: 'Primer nombre' }).fill(personalData.firstName);
    await this.page.getByRole('textbox', { name: 'Segundo nombre' }).fill(personalData.secondName);
    await this.page
      .getByRole('textbox', { name: 'Primer apellido' })
      .fill(personalData.firstLastName);
    await this.page
      .getByRole('textbox', { name: 'Segundo apellido' })
      .fill(personalData.secondLastName);
    await this.page.getByLabel('Fecha de nacimiento').fill(personalData.birthDate);
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'Sexo' }),
      personalData.sex
    );
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'País de residencia' }),
      personalData.country
    );
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'Departamento / Estado' }),
      personalData.state
    );
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'Ciudad' }),
      personalData.city
    );
    await this.page.getByRole('textbox', { name: 'Dirección' }).fill(personalData.address);
    await this.page
      .getByRole('textbox', { name: 'Celular con prefijo +598' })
      .fill(personalData.phone);
    await this.page.getByRole('textbox', { exact: true, name: 'E-mail' }).fill(personalData.email);
    await this.page.getByRole('textbox', { name: 'Confirmar e-mail' }).fill(personalData.email);
  }

  public async fillVerificationData(): Promise<void> {
    await this.page
      .getByRole('textbox', { name: 'Primer apellido' })
      .fill(personalData.firstLastName);
    await this.page.getByRole('textbox', { exact: true, name: 'E-mail' }).fill(personalData.email);
  }

  public async continueFromPersonalData(): Promise<void> {
    await this.page.getByRole('button', { name: 'Continuar' }).click();
  }

  public async fillCareerSelection(): Promise<void> {
    await clickRadioByName(this.page, careerData.academicLevel);
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'Carrera' }),
      careerData.career
    );
    await selectOrtOption(
      this.page,
      this.page.getByRole('combobox', { name: 'Comienzo' }),
      careerData.start
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
