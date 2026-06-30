import { HttpContextToken } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ReCaptchaV3Service } from 'ng-recaptcha-2';
import { Observable, throwError } from 'rxjs';
import { catchError, map, take, timeout } from 'rxjs/operators';

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

    return this.recaptcha.execute(normalizedAction).pipe(
      take(1),
      map(token => {
        if (!token?.trim()) {
          throw new Error('Captcha token is empty.');
        }
        return token;
      }),
      timeout({
        first: 10000,
        with: () => throwError(() => new Error('Captcha token could not be generated.')),
      }),
      catchError(error => {
        const message = this.errorMessage(error);
        return throwError(() => new Error(`Captcha failed before API request: ${message}`));
      })
    );
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
