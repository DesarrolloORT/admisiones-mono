import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';

export interface ExternalPaymentRequest {
  paymentUrl: string;
  encryptedParameters: string;
  target?: '_self' | '_blank';
}

@Injectable({ providedIn: 'root' })
export class ExternalPaymentSubmitter {
  private readonly document = inject(DOCUMENT);

  public submit(request: ExternalPaymentRequest): boolean {
    const url = toHttpUrl(request.paymentUrl);
    if (!url || !request.encryptedParameters.trim()) return false;

    const form = this.document.createElement('form');
    form.method = 'POST';
    form.action = url;
    form.target = request.target ?? '_blank';

    const input = this.document.createElement('input');
    input.type = 'hidden';
    input.name = 'data';
    // Las páginas Pagos*Gestion.aspx (LogicaORT) parsean el campo con
    // data.Split('=')[1].Split('"')[0]: el blob debe ir entre un '=' y una '"'.
    // Mismo formato que usa Gestion_V2 en producción.
    input.value = JSON.stringify({
      params: `parametrosEncriptados=${request.encryptedParameters}`,
    });

    form.append(input);
    this.document.body.append(form);
    try {
      form.submit();
      return true;
    } catch {
      return false;
    } finally {
      form.remove();
    }
  }
}

function toHttpUrl(value: string): string | null {
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:' ? url.toString() : null;
  } catch {
    return null;
  }
}
