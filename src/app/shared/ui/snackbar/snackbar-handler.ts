import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { OrtSnackbarRef, OrtSnackbarService } from '@desarrolloort/components';

export type SnackbarVariant = 'error' | 'information' | 'success' | 'warning';

export interface SnackbarConfig {
  readonly message: string;
  readonly title?: string;
  readonly hint?: string;
  readonly actionLabel?: string;
  readonly action?: () => void;
  readonly duration?: number;
  readonly variant?: SnackbarVariant;
}

const DEFAULT_DURATION_MS = 5000;

@Injectable({
  providedIn: 'root',
})
export class SnackbarHandler {
  private readonly document = inject(DOCUMENT);
  private readonly snackBar = inject(OrtSnackbarService);
  private currentRef: OrtSnackbarRef | null = null;
  private readonly fontsReady = this.document.fonts?.ready ?? Promise.resolve();

  public show(config: SnackbarConfig): void {
    void this.fontsReady.then(() => this.showImmediate(config));
  }

  private showImmediate(config: SnackbarConfig): void {
    const variant = config.variant ?? 'information';
    const supportingText = config.hint ?? config.title;

    const ref = this.snackBar.open({
      actionLabel: config.actionLabel,
      autoDismiss: true,
      durationMs: config.duration ?? DEFAULT_DURATION_MS,
      horizontalPosition: 'center',
      message: config.message,
      supportingText,
      variant,
      verticalPosition: 'bottom',
    });

    this.currentRef = ref;

    ref.afterDismissed().subscribe(() => {
      if (this.currentRef === ref) {
        this.currentRef = null;
      }
    });

    if (config.action) {
      const action = config.action;
      ref.onAction().subscribe(() => action());
    }
  }

  public success(message: string, config: Omit<SnackbarConfig, 'message' | 'variant'> = {}): void {
    this.show({ ...config, message, variant: 'success' });
  }

  public error(message: string, config: Omit<SnackbarConfig, 'message' | 'variant'> = {}): void {
    this.show({ ...config, message, variant: 'error' });
  }

  public warning(message: string, config: Omit<SnackbarConfig, 'message' | 'variant'> = {}): void {
    this.show({ ...config, message, variant: 'warning' });
  }

  public information(
    message: string,
    config: Omit<SnackbarConfig, 'message' | 'variant'> = {}
  ): void {
    this.show({ ...config, message, variant: 'information' });
  }

  public dismiss(): void {
    this.currentRef?.dismiss();
    this.currentRef = null;
  }
}
