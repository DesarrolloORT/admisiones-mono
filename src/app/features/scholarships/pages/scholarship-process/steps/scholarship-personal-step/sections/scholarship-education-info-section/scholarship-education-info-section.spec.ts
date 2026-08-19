import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { OrtFileUploaderChange } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipPersonalFacade } from '../../../../../../facades/scholarship-personal';
import { ScholarshipProcessFacade } from '../../../../../../facades/scholarship-process';
import { ScholarshipEducationInfoSection } from './scholarship-education-info-section';

function certificateUpload(): OrtFileUploaderChange {
  return {
    value: [{ file: new File(['x'], 'formula69.pdf', { type: 'application/pdf' }), isValid: true }],
  } as unknown as OrtFileUploaderChange;
}

describe('ScholarshipEducationInfoSection', () => {
  let fixture: ComponentFixture<ScholarshipEducationInfoSection>;
  let facade: ScholarshipPersonalFacade;
  let breakpoint: ReturnType<typeof signal<{ isXSmall: boolean; isSmall: boolean }>>;

  beforeEach(() => {
    breakpoint = signal({ isXSmall: false, isSmall: false });

    TestBed.configureTestingModule({
      imports: [ScholarshipEducationInfoSection],
      providers: [
        ScholarshipProcessFacade,
        ScholarshipPersonalFacade,
        { provide: BreakpointService, useValue: { breakpoint } },
      ],
    });

    facade = TestBed.inject(ScholarshipPersonalFacade);
    fixture = TestBed.createComponent(ScholarshipEducationInfoSection);
    fixture.detectChanges();
  });

  it('asks for both secondary-school averages', () => {
    const labels = Array.from(fixture.nativeElement.querySelectorAll('ort-label')).map(label =>
      (label as Element).textContent?.trim()
    );

    expect(labels).toContain('Promedio de 2° EMS (5to)');
    expect(labels).toContain('Promedio de 3° EMS (6to)');
  });

  it('shows one error per missing average once the person tried to continue', () => {
    facade.setVariant('fexaCon');
    facade.submitted.set(true);
    fixture.detectChanges();

    const errors = Array.from(fixture.nativeElement.querySelectorAll('ort-error')).map(error =>
      (error as Element).textContent?.trim()
    );

    expect(errors).toEqual([
      'Ingresá tu promedio',
      'Ingresá tu promedio',
      'Adjuntá el certificado de secundaria',
    ]);
  });

  // fbc no pide el certificado, asi que el uploader no se muestra.
  it('hides the certificate uploader for fbc', () => {
    facade.setVariant('fexaCon');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('ort-file-uploader')).toBeTruthy();

    facade.setVariant('fbc');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ort-file-uploader')).toBeFalsy();
  });

  it('marks the certificate as attached through the facade', () => {
    facade.setVariant('fexaCon');
    fixture.detectChanges();

    fixture.componentInstance['onCertificateFilesChanged'](certificateUpload());
    fixture.detectChanges();

    expect(facade.educationInfoForm.controls.certificateFile.value).toBe(true);
    expect(fixture.nativeElement.querySelector('ort-error')).toBeFalsy();
  });
});
