import { expect, Page } from '@playwright/test';

export interface LoginCredentials {
  documentNumber: string;
  documentType?: string;
  password: string;
}

export class LoginPage {
  public constructor(private readonly page: Page) {}

  public async goto(): Promise<void> {
    await this.page.goto('/iniciar-sesion');
    await expect(
      this.page.getByRole('heading', { name: 'Comenzá tu camino en ORT' })
    ).toBeVisible();
  }

  public async submitEmpty(): Promise<void> {
    await this.page.getByRole('button', { name: 'Iniciar sesión' }).click();
  }

  public async login(credentials: LoginCredentials): Promise<void> {
    await this.page
      .getByRole('textbox', { name: 'Nro. de documento' })
      .fill(credentials.documentNumber);
    await this.page.getByRole('textbox', { name: 'Contraseña' }).fill(credentials.password);
    await this.page.getByRole('button', { name: 'Iniciar sesión' }).click();
  }
}
