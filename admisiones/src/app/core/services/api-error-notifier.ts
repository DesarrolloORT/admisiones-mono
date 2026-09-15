import { inject, Injectable } from '@angular/core';
import { ApiErrorNotifier, NormalizedApiError } from '@desarrolloort/ngx-utils';

import { SnackbarHandler } from '../../shared/ui/snackbar/snackbar-handler';

const IGNORED_GLOBAL_ERROR_STATUSES = new Set([401, 404]);

@Injectable({
  providedIn: 'root',
})
export class AppApiErrorNotifier extends ApiErrorNotifier {
  private readonly snackbar = inject(SnackbarHandler);

  public override notify(error: NormalizedApiError): void {
    if (error.action === 'ignore' || IGNORED_GLOBAL_ERROR_STATUSES.has(error.status)) return;
    this.snackbar.error(error.message);
  }
}
