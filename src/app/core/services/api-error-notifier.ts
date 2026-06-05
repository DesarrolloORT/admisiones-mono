import { inject, Injectable } from '@angular/core';
import { ApiErrorNotifier, NormalizedApiError } from '@desarrolloort/ngx-utils';

import { SnackbarHandler } from '../../shared/ui/snackbar/snackbar-handler';

@Injectable({
  providedIn: 'root',
})
export class AppApiErrorNotifier extends ApiErrorNotifier {
  private readonly snackbar = inject(SnackbarHandler);

  public override notify(error: NormalizedApiError): void {
    this.snackbar.error(error.message);
  }
}
