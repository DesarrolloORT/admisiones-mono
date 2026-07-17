import AxeBuilder from '@axe-core/playwright';
import { expect, Page } from '@playwright/test';

interface KnownAxeIssue {
  id: string;
  targetIncludes: string;
}

interface AxeOptions {
  knownIssues?: readonly KnownAxeIssue[];
}

export const ORT_FILE_UPLOADER_KNOWN_AXE_ISSUES: readonly KnownAxeIssue[] = [
  {
    id: 'color-contrast',
    targetIncludes: 'ort-file-uploader',
  },
];

export async function expectNoAxeViolations(page: Page, options: AxeOptions = {}): Promise<void> {
  await waitForFiniteAnimations(page);

  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();

  const violations = results.violations
    .map(violation => ({
      id: violation.id,
      impact: violation.impact,
      help: violation.help,
      nodes: violation.nodes
        .filter(node => !isKnownAxeIssue(violation.id, node.target, options.knownIssues ?? []))
        .map(node => node.target),
    }))
    .filter(violation => violation.nodes.length > 0);

  expect(violations).toEqual([]);
}

// Los colores medidos durante un fade-in producen falsos positivos de contraste.
// Se esperan solo las animaciones finitas: las infinitas (spinners) no bloquean.
async function waitForFiniteAnimations(page: Page): Promise<void> {
  await page.evaluate(() =>
    Promise.all(
      document
        .getAnimations()
        .filter(animation => animation.effect?.getTiming().iterations !== Infinity)
        .map(animation => animation.finished.catch(() => undefined))
    )
  );
}

function isKnownAxeIssue(
  id: string,
  target: readonly unknown[],
  knownIssues: readonly KnownAxeIssue[]
): boolean {
  const targetText = target.map(targetPartToText).join(' ');

  return knownIssues.some(
    knownIssue => knownIssue.id === id && targetText.includes(knownIssue.targetIncludes)
  );
}

function targetPartToText(targetPart: unknown): string {
  if (Array.isArray(targetPart)) {
    return targetPart.join(' ');
  }

  return String(targetPart);
}
