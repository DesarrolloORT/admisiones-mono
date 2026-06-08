import { HttpContextToken } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ReCaptchaV3Service } from 'ng-recaptcha-2';
import { Observable, throwError } from 'rxjs';
import { catchError, map, take, timeout } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export const CAPTCHA_HEADER = 'X-Captcha-Token';
export const CAPTCHA_ACTION = new HttpContextToken<string | null>(() => null);

@Injectable({
  providedIn: 'root',
})
export class CaptchaTokenService {
  private readonly recaptcha = inject(ReCaptchaV3Service);

  public execute(action: string): Observable<string> {
    const normalizedAction = action.trim();

    if (!normalizedAction) {
      return throwError(() => new Error('Captcha action is required.'));
    }

    this.debug('execute:start', { action: normalizedAction });

    return this.recaptcha.execute(normalizedAction).pipe(
      take(1),
      map(token => {
        if (!token?.trim()) {
          throw new Error('Captcha token is empty.');
        }

        this.debug('execute:token', { action: normalizedAction, tokenLength: token.length });

        return token;
      }),
      timeout({
        first: 10000,
        with: () => throwError(() => new Error('Captcha token could not be generated.')),
      }),
      catchError(error => {
        const message = this.errorMessage(error);
        this.error('execute:error', { action: normalizedAction, message, error });

        return throwError(() => new Error(`Captcha failed before API request: ${message}`));
      })
    );
  }

  private debug(event: string, data: Record<string, unknown>): void {
    if (!environment.production) {
      console.debug(`[captcha] ${event}`, data);
    }
  }

  private error(event: string, data: Record<string, unknown>): void {
    if (!environment.production) {
      console.error(`[captcha] ${event}`, data);
    }
  }

  private errorMessage(error: unknown): string {
    if (error instanceof Error && error.message) {
      return error.message;
    }

    if (typeof error === 'string' && error.trim()) {
      return error;
    }

    return 'Unknown reCAPTCHA error.';
  }
}
