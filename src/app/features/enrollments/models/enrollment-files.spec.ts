import { toBlobFile, toIdentityFile, toIdentityUploadFile } from './enrollment-files';

describe('enrollment-files', () => {
  it('parses a backend data-url into a browser File', () => {
    const content = `data:image/png;base64,${globalThis.btoa('png-bytes')}`;

    const file = toIdentityFile({ fileName: 'frente.png', content }, 'frente-documento');

    expect(file?.name).toBe('frente.png');
    expect(file?.type).toBe('image/png');
    expect(file?.size).toBe('png-bytes'.length);
  });

  it('derives the name from the real type when the backend sends no file name', () => {
    const content = `data:image/png;base64,${globalThis.btoa('png-bytes')}`;

    const file = toIdentityFile({ fileName: null, content }, 'frente-documento');

    expect(file?.name).toBe('frente-documento.png');
    expect(file?.type).toBe('image/png');
  });

  it('infers the mime type from the file name for raw base64 and falls back on empty content', () => {
    const file = toIdentityFile(
      { fileName: 'dorso.jpg', content: globalThis.btoa('jpg-bytes') },
      'dorso-documento'
    );

    expect(file?.type).toBe('image/jpeg');
    expect(toIdentityFile({ fileName: 'x.jpg', content: '   ' }, 'dorso-documento')).toBeNull();
    expect(toIdentityFile(null, 'dorso-documento')).toBeNull();
  });

  it('returns null for invalid base64 content', () => {
    expect(toIdentityFile({ fileName: 'x.jpg', content: '@@no-base64@@' }, 'documento')).toBeNull();
  });

  it('names a nameless blob from its real type', () => {
    const blob = new Blob(['photo'], { type: 'image/png' });

    const file = toBlobFile(blob, 'foto-persona');

    expect(file?.name).toBe('foto-persona.png');
    expect(file?.type).toBe('image/png');
    expect(toBlobFile(new Blob([]), 'foto-persona')).toBeNull();
    expect(toBlobFile(null, 'foto-persona')).toBeNull();
  });

  it('encodes an image File as base64 upload payload', async () => {
    const file = new File(['upload-bytes'], 'frente.jpg', { type: 'image/jpeg' });

    const upload = await toIdentityUploadFile(file);

    expect(upload.fileName).toBe('frente.jpg');
    expect(globalThis.atob(upload.content)).toBe('upload-bytes');
  });

  it('aligns the upload name extension with the real type', async () => {
    const file = new File(['upload-bytes'], 'documento.jpg', { type: 'image/png' });

    const upload = await toIdentityUploadFile(file);

    expect(upload.fileName).toBe('documento.png');
  });

  it('rejects files that are not jpeg or png', async () => {
    const file = new File(['x'], 'documento.pdf', { type: 'application/pdf' });

    await expect(toIdentityUploadFile(file)).rejects.toThrow('Invalid identity image type.');
  });
});
