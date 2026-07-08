import { toBlobFile, toIdentityFile, toIdentityUploadFile } from './inscription-files';

describe('inscription-files', () => {
  it('parses a backend data-url into a browser File', () => {
    const archivo = `data:image/png;base64,${globalThis.btoa('png-bytes')}`;

    const file = toIdentityFile({ nombreArchivo: 'frente.png', archivo }, 'fallback.jpg');

    expect(file?.name).toBe('frente.png');
    expect(file?.type).toBe('image/png');
    expect(file?.size).toBe('png-bytes'.length);
  });

  it('infers the mime type from the file name for raw base64 and falls back on empty content', () => {
    const file = toIdentityFile(
      { nombreArchivo: 'dorso.jpg', archivo: globalThis.btoa('jpg-bytes') },
      'fallback.jpg'
    );

    expect(file?.type).toBe('image/jpeg');
    expect(toIdentityFile({ nombreArchivo: 'x.jpg', archivo: '   ' }, 'fallback.jpg')).toBeNull();
    expect(toIdentityFile(null, 'fallback.jpg')).toBeNull();
  });

  it('returns null for invalid base64 content', () => {
    expect(
      toIdentityFile({ nombreArchivo: 'x.jpg', archivo: '@@no-base64@@' }, 'f.jpg')
    ).toBeNull();
  });

  it('wraps a non-empty blob as File and rejects empty blobs', () => {
    const blob = new Blob(['photo'], { type: 'image/png' });

    const file = toBlobFile(blob, 'foto-persona.jpg');

    expect(file?.name).toBe('foto-persona.jpg');
    expect(file?.type).toBe('image/png');
    expect(toBlobFile(new Blob([]), 'foto.jpg')).toBeNull();
    expect(toBlobFile(null, 'foto.jpg')).toBeNull();
  });

  it('encodes an image File as base64 upload payload', async () => {
    const file = new File(['upload-bytes'], 'frente.jpg', { type: 'image/jpeg' });

    const upload = await toIdentityUploadFile(file);

    expect(upload.nombreArchivo).toBe('frente.jpg');
    expect(globalThis.atob(upload.archivo)).toBe('upload-bytes');
  });

  it('rejects files that are not jpeg or png', async () => {
    const file = new File(['x'], 'documento.pdf', { type: 'application/pdf' });

    await expect(toIdentityUploadFile(file)).rejects.toThrow('Invalid identity image type.');
  });
});
