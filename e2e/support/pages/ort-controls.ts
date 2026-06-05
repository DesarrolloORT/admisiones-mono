import { expect, Locator, Page } from '@playwright/test';

export async function selectOrtOption(
  page: Page,
  trigger: Locator,
  optionName: string | RegExp
): Promise<void> {
  await trigger.click();

  const optionByRole = page.getByRole('option', { name: optionName });

  if ((await optionByRole.count()) > 0) {
    await optionByRole.first().click();
    return;
  }

  const optionByText = page.locator('ort-option').filter({ hasText: optionName }).last();

  await expect(optionByText).toBeVisible();
  await optionByText.click();
}

export async function clickRadioByName(page: Page, name: string | RegExp): Promise<void> {
  const radioCard = page.locator('ort-radio-card-button').filter({ hasText: name }).first();

  if ((await radioCard.count()) > 0) {
    await radioCard.click();
    return;
  }

  const radio = page.getByRole('radio', { name });

  if ((await radio.count()) > 0) {
    await radio.first().check({ force: true });
    return;
  }

  await page.getByText(name).click();
}
