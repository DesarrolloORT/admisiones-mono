import { toBlobFile, toIdentityFile, toIdentityUploadFile } from './enrollment-files';

describe('enrollment-files', () => {
  it('parses a backend data-url into a browser File', () => {
    const content = `data:image/png;base64,${globalThis.btoa('png-bytes')}`;

    const file = toIdentityFile({ fileName: 'front.png', content }, 'identity-document-front');

    expect(file?.name).toBe('front.png');
    expect(file?.type).toBe('image/png');
    expect(file?.size).toBe('png-bytes'.length);
  });

  it('derives the name from the real type when the backend sends no file name', () => {
    const content = `data:image/png;base64,${globalThis.btoa('png-bytes')}`;

    const file = toIdentityFile({ fileName: null, content }, 'identity-document-front');

    expect(file?.name).toBe('identity-document-front.png');
    expect(file?.type).toBe('image/png');
  });

  it('infers the mime type from the file name for raw base64 and falls back on empty content', () => {
    const file = toIdentityFile(
      { fileName: 'back.jpg', content: globalThis.btoa('jpg-bytes') },
      'identity-document-back'
    );

    expect(file?.type).toBe('image/jpeg');
    expect(
      toIdentityFile({ fileName: 'x.jpg', content: '   ' }, 'identity-document-back')
    ).toBeNull();
    expect(toIdentityFile(null, 'identity-document-back')).toBeNull();
  });

  it('returns null for invalid base64 content', () => {
    expect(toIdentityFile({ fileName: 'x.jpg', content: '@@no-base64@@' }, 'document')).toBeNull();
  });

  it('names a nameless blob from its real type', () => {
    const blob = new Blob(['photo'], { type: 'image/png' });

    const file = toBlobFile(blob, 'identity-photo');

    expect(file?.name).toBe('identity-photo.png');
    expect(file?.type).toBe('image/png');
    expect(toBlobFile(new Blob([]), 'identity-photo')).toBeNull();
    expect(toBlobFile(null, 'identity-photo')).toBeNull();
  });

  it('encodes an image File as base64 upload payload', async () => {
    const file = new File(['upload-bytes'], 'front.jpg', { type: 'image/jpeg' });

    const upload = await toIdentityUploadFile(file);

    expect(upload.fileName).toBe('front.jpg');
    expect(globalThis.atob(upload.content)).toBe('upload-bytes');
  });

  it('aligns the upload name extension with the real type', async () => {
    const file = new File(['upload-bytes'], 'identity-document.jpg', { type: 'image/png' });

    const upload = await toIdentityUploadFile(file);

    expect(upload.fileName).toBe('identity-document.png');
  });

  it('rejects files that are not jpeg or png', async () => {
    const file = new File(['x'], 'identity-document.pdf', { type: 'application/pdf' });

    await expect(toIdentityUploadFile(file)).rejects.toThrow('Invalid identity image type.');
  });
});
