import { computed, Injectable, signal } from '@angular/core';

import {
  DocumentRecognitionFields,
  DocumentRecognitionFile,
  DocumentRecognitionResponse,
} from '../models/document-recognition.interface';

export interface RegisterDocumentState {
  selectedFileName: string | null;
  recognitionResponse: DocumentRecognitionResponse | null;
  fields: DocumentRecognitionFields | null;
  documentFace: DocumentRecognitionFile | null;
}

const initialState: RegisterDocumentState = {
  selectedFileName: null,
  recognitionResponse: null,
  fields: null,
  documentFace: null,
};

@Injectable({
  providedIn: 'root',
})
export class RegisterDocumentStore {
  private readonly state = signal<RegisterDocumentState>(initialState);

  public readonly selectedFileName = computed(() => this.state().selectedFileName);
  public readonly recognitionResponse = computed(() => this.state().recognitionResponse);
  public readonly fields = computed(() => this.state().fields);
  public readonly documentFace = computed(() => this.state().documentFace);
  public readonly requiresReview = computed<boolean | null>(() => {
    const value = this.state().recognitionResponse?.data?.requiereRevision;
    return typeof value === 'boolean' ? value : null;
  });

  public setSelectedFile(file: File | null): void {
    this.state.update(state => ({
      ...state,
      selectedFileName: file?.name ?? null,
    }));
  }

  public setRecognitionResponse(response: DocumentRecognitionResponse): void {
    this.state.update(state => ({
      ...state,
      recognitionResponse: response,
      fields: response.data?.campos ?? null,
      documentFace: response.data?.caraPersona ?? null,
    }));
  }

  public clearRecognition(): void {
    this.state.update(state => ({
      ...state,
      recognitionResponse: null,
      fields: null,
      documentFace: null,
    }));
  }

  public clear(): void {
    this.state.set(initialState);
  }
}
