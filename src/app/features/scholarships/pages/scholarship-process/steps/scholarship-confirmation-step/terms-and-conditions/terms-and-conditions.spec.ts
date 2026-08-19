import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { TermsAndConditions } from './terms-and-conditions';

describe('TermsAndConditions', () => {
  let fixture: ComponentFixture<TermsAndConditions>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [TermsAndConditions] });
    fixture = TestBed.createComponent(TermsAndConditions);
    fixture.detectChanges();
  });

  it('renders the terms the person has to accept', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(
      fixture.nativeElement.querySelector('.terms-and-conditions__title').textContent
    ).toContain('Términos y Condiciones');
    expect(text).toContain('Condiciones de mantenimiento de la beca');
  });

  it('emits accepted when the person accepts the conditions', () => {
    const accepted = vi.fn();
    fixture.componentInstance.accepted.subscribe(accepted);

    (
      fixture.nativeElement.querySelector('button[ort-primary-button]') as HTMLButtonElement
    ).click();

    expect(accepted).toHaveBeenCalledOnce();
  });
});
