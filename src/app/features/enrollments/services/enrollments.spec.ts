import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { EnrollmentsApi } from '../api/enrollments.api';
import { Enrollments } from './enrollments';

describe('Enrollments', () => {
  let service: Enrollments;
  let endpointMock: {
    getIdentityDocument: ReturnType<typeof vi.fn>;
    getIdentityPhoto: ReturnType<typeof vi.fn>;
    uploadIdentityDocument: ReturnType<typeof vi.fn>;
    uploadIdentityPhoto: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      getIdentityDocument: vi.fn().mockReturnValue(of({})),
      getIdentityPhoto: vi.fn().mockReturnValue(of(new Blob())),
      uploadIdentityDocument: vi.fn().mockReturnValue(of(true)),
      uploadIdentityPhoto: vi.fn().mockReturnValue(of(true)),
    };

    TestBed.configureTestingModule({
      providers: [Enrollments, { provide: EnrollmentsApi, useValue: endpointMock }],
    });
    service = TestBed.inject(Enrollments);
  });

  it('maps identity document and photo responses to preload files', async () => {
    endpointMock.getIdentityDocument.mockReturnValueOnce(
      of({
        front: { fileName: 'front.png', content: 'aGVsbG8=' },
        back: {
          fileName: 'folder\\back.jpg',
          content: 'data:image/jpeg;base64,d29ybGQ=',
        },
        expirationDate: '2030-02-04',
      })
    );
    endpointMock.getIdentityPhoto.mockReturnValueOnce(
      of(new Blob(['photo'], { type: 'image/png' }))
    );

    const preload = await firstValueFrom(service.getIdentityPreload());

    expect(preload.front).toEqual(
      expect.objectContaining({ name: 'front.png', size: 5, type: 'image/png' })
    );
    expect(preload.back).toEqual(
      expect.objectContaining({ name: 'back.jpg', size: 5, type: 'image/jpeg' })
    );
    expect(preload.selfie).toEqual(
      expect.objectContaining({ name: 'identity-photo.png', size: 5, type: 'image/png' })
    );
    expect(preload.expirationDate).toBe('2030-02-04');
  });

  it('returns an empty identity preload when person files are unavailable', async () => {
    endpointMock.getIdentityDocument.mockReturnValueOnce(
      throwError(() => new Error('document unavailable'))
    );
    endpointMock.getIdentityPhoto.mockReturnValueOnce(
      throwError(() => new Error('photo unavailable'))
    );

    await expect(firstValueFrom(service.getIdentityPreload())).resolves.toEqual({
      front: null,
      back: null,
      selfie: null,
      expirationDate: null,
    });
  });

  it('uploads identity document files as base64', async () => {
    const front = new File(['front'], 'front.png', { type: 'image/png' });
    const back = new File(['back'], 'back.jpg', { type: 'image/jpeg' });

    await expect(
      firstValueFrom(service.uploadIdentityDocument({ date: '2030-02-04', front, back }))
    ).resolves.toBe(true);

    expect(endpointMock.uploadIdentityDocument).toHaveBeenCalledWith({
      date: '2030-02-04',
      front: { fileName: 'front.png', content: 'ZnJvbnQ=' },
      back: { fileName: 'back.jpg', content: 'YmFjaw==' },
    });
  });

  it('uploads identity photo as base64', async () => {
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    await expect(firstValueFrom(service.uploadIdentityPhoto(selfie))).resolves.toBe(true);

    expect(endpointMock.uploadIdentityPhoto).toHaveBeenCalledWith({
      attachedFile: { fileName: 'selfie.png', content: 'cGhvdG8=' },
    });
  });
});
