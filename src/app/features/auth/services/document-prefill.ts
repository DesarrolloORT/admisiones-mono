import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { LocationValue } from '../../catalogs/models/location-value';
import { Catalogs } from '../../catalogs/services/catalogs';
import {
  type RecognizedFormPatch,
  resolveStateCodeFromBirthplace,
  toRecognizedFormPatch,
} from '../mappers/document-recognition.mapper';
import { DocumentRecognition } from './document-recognition';

export interface DocumentPrefillResult {
  patch: RecognizedFormPatch | null;
  location: LocationValue | null;
}

@Injectable({
  providedIn: 'root',
})
export class DocumentPrefillService {
  private readonly catalogs = inject(Catalogs);
  private readonly documentRecognition = inject(DocumentRecognition);

  public async preload(file: File): Promise<DocumentPrefillResult> {
    const payload = await this.documentRecognition.createRequestFromFile(file);
    const response = await firstValueFrom(this.documentRecognition.recognizeDocument(payload));
    const patch = toRecognizedFormPatch(response.fields);

    return {
      patch,
      location: patch ? await this.resolveLocation(patch) : null,
    };
  }

  private async resolveLocation(patch: RecognizedFormPatch): Promise<LocationValue | null> {
    if (patch.countryCode === null) {
      return null;
    }

    const stateCode = await this.resolveStateCodeFromBirthplace(
      patch.countryCode,
      patch.birthplace
    );

    return {
      countryCode: patch.countryCode,
      stateCode: stateCode,
      cityCode: null,
    };
  }

  private async resolveStateCodeFromBirthplace(
    countryCode: number,
    birthplace: string | null | undefined
  ): Promise<number | null> {
    try {
      const locations = await firstValueFrom(this.catalogs.getCountryLocations());
      return resolveStateCodeFromBirthplace(locations, countryCode, birthplace);
    } catch {
      return null;
    }
  }
}
