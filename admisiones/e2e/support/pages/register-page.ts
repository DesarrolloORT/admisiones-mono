import { expect, Page } from '@playwright/test';

import { personalData, RegisterScenario } from '../test-data/register-scenarios';
import { selectOrtOption } from './ort-controls';

type PersonalData = typeof personalData;

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
    await this.page.locator('ort-file-uploader input[type="file"]').setInputFiles(file);
  }

  public async continueFromIdentity(): Promise<void> {
    await this.page.getByRole('button', { name: 'Continuar' }).click();
  }

  public async fillFullPersonalData(data: PersonalData = personalData): Promise<void> {
    await this.page.getByRole('textbox', { name: 'Primer nombre' }).fill(data.firstName);
    await this.page.getByRole('textbox', { name: 'Segundo nombre' }).fill(data.secondName);
    await this.page.getByRole('textbox', { name: 'Primer apellido' }).fill(data.firstLastName);
    await this.page.getByRole('textbox', { name: 'Segundo apellido' }).fill(data.secondLastName);
    await this.page.getByLabel('Fecha de nacimiento').fill(toDisplayDate(data.birthDate));
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
    await this.page
      .getByRole('textbox', { exact: true, name: 'Número de teléfono' })
      .fill(data.phone);
    await this.page.getByRole('textbox', { exact: true, name: 'E-mail' }).fill(data.email);
    await this.page.getByRole('textbox', { name: 'Confirmar e-mail' }).fill(data.email);
  }

  public async fillVerificationData(data: PersonalData = personalData): Promise<void> {
    await this.page.getByRole('textbox', { name: 'Primer apellido' }).fill(data.firstLastName);
    await this.page.getByRole('textbox', { exact: true, name: 'E-mail' }).fill(data.email);
  }

  public async continueFromPersonalData(): Promise<void> {
    await this.page.getByRole('button', { name: /^(Crear cuenta|Confirmar)$/ }).click();
  }

  public async expectCreatedAccount(
    documentType: RegisterScenario['documentType'] = 'CI'
  ): Promise<void> {
    if (documentType === 'CI') {
      await this.expectEmailConfirmation();
      return;
    }

    await this.expectPendingRequest();
  }

  public async expectVerifiedIdentity(): Promise<void> {
    await this.expectEmailConfirmation();
  }

  private async expectPendingRequest(): Promise<void> {
    await expect(this.page).toHaveURL(/\/confirmacion-correo\/solicitud-registro/);
    await expect(this.page.getByRole('heading', { name: 'Procesando tu solicitud' })).toBeVisible();
    await expect(
      this.page.getByText(
        'Recibimos tu solicitud de creación de cuenta. Estamos validando tus datos.'
      )
    ).toBeVisible();
  }

  private async expectEmailConfirmation(): Promise<void> {
    await expect(this.page).toHaveURL(/\/confirmacion-correo\/registro/);
    await expect(
      this.page.getByRole('heading', { name: '¡Cuenta creada con éxito!' })
    ).toBeVisible();
    await expect(
      this.page.getByText(
        'Revisá tu casilla de e-mail. Te enviamos un enlace de activación para crear tu contraseña y finalizar el registro.'
      )
    ).toBeVisible();
  }
}

function toDisplayDate(value: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);

  if (!match) {
    throw new Error(`Fecha de prueba inválida: ${value}`);
  }

  return `${match[3]}/${match[2]}/${match[1]}`;
}
