import { expect, Page } from '@playwright/test';

export class HomePage {
  public constructor(private readonly page: Page) {}

  public async goto(): Promise<void> {
    await this.page.goto('/inicio');
    await expect(this.page.getByRole('heading', { name: /Hola/ })).toBeVisible();
  }

  public profileMenuButton() {
    return this.page.getByRole('button', { name: 'Abrir menú de usuario' });
  }

  public profileMenuDialog() {
    return this.page.getByRole('dialog', { name: 'Menú de usuario' });
  }
}
