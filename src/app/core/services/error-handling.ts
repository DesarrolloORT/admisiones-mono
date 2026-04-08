import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ErrorHandling {
  public handleErrorInUI(error?: unknown): void {
    console.error('Unhandled HTTP error', error);
  }
}
