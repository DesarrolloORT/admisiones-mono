import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { vi } from 'vitest';

import { ResponsiveSelect, type ResponsiveSelectOption } from './responsive-select';

@Component({
  selector: 'app-responsive-select-host',
  imports: [ReactiveFormsModule, ResponsiveSelect],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <form [formGroup]="form">
      <app-responsive-select
        formControlName="option"
        label="Opción"
        [multiple]="multiple"
        [options]="options"
        [searchable]="searchable" />
    </form>
  `,
})
class HostComponent {
  readonly form = new FormGroup({
    option: new FormControl<string | string[]>('', {
      nonNullable: true,
      validators: Validators.required,
    }),
  });
  options: readonly ResponsiveSelectOption[] = [
    { value: 'a', label: 'A' },
    { value: 'b', label: 'B' },
  ];
  multiple = false;
  searchable = true;
}

const breakpoint = signal({
  isXSmall: true,
  isSmall: false,
  isMedium: false,
  isLarge: false,
  currentBreakpoint: 'xs',
  screenWidth: 375,
});

describe('ResponsiveSelect', () => {
  beforeAll(() => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
      configurable: true,
      value: vi.fn(),
    });
  });

  let fixture: ComponentFixture<HostComponent>;

  beforeEach(() => {
    breakpoint.set({
      isXSmall: true,
      isSmall: false,
      isMedium: false,
      isLarge: false,
      currentBreakpoint: 'xs',
      screenWidth: 375,
    });

    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [{ provide: BreakpointService, useValue: { breakpoint } }],
    });
    fixture = TestBed.createComponent(HostComponent);
  });

  it('renders only the mobile drawer trigger on small breakpoints', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.responsive-select__desktop')).toBeNull();
    expect(fixture.nativeElement.querySelector('.responsive-select__mobile')).toBeTruthy();
  });

  it('renders only the desktop select from medium breakpoints', () => {
    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: true,
      isLarge: false,
      currentBreakpoint: 'md',
      screenWidth: 900,
    });

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.responsive-select__desktop')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.responsive-select__mobile')).toBeNull();
  });

  it('commits the active desktop option when the library misses a keyboard selection', async () => {
    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: true,
      isLarge: false,
      currentBreakpoint: 'md',
      screenWidth: 900,
    });
    fixture.detectChanges();
    const trigger = fixture.nativeElement.querySelector('ort-select') as HTMLElement;
    const listbox = document.createElement('ort-menu');
    listbox.id = 'active-listbox';
    const option = document.createElement('ort-option');
    option.id = 'active-option';
    option.textContent = 'B';
    listbox.append(option);
    document.body.append(listbox);
    trigger.setAttribute('aria-expanded', 'true');
    trigger.setAttribute('aria-controls', listbox.id);
    trigger.setAttribute('aria-activedescendant', option.id);
    trigger.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true })
    );
    await new Promise(resolve => setTimeout(resolve));
    listbox.remove();

    expect(fixture.componentInstance.form.controls.option.value).toBe('b');
  });

  it('hides drawer search when there are fewer than 6 options', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('input[type="search"]')).toBeNull();
  });

  it('shows drawer search from 6 options', () => {
    fixture.componentInstance.options = Array.from({ length: 6 }, (_, index) => ({
      value: String(index),
      label: `Opción ${index}`,
    }));

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('input[type="search"]')).toBeTruthy();
  });

  it('writes mobile drawer selection to the form control', () => {
    fixture.detectChanges();
    const select = selectComponent(fixture);

    select.openDrawer();
    select.toggleOption('b');
    select.confirmDrawerValue();

    expect(fixture.componentInstance.form.controls.option.value).toBe('b');
    expect(fixture.componentInstance.form.controls.option.touched).toBe(true);
  });

  it('shows the first value of an array control in single mode', () => {
    fixture.componentInstance.form.controls.option.setValue(['b']);
    fixture.detectChanges();

    expect(
      (
        fixture.nativeElement.querySelector(
          '.responsive-select__mobile-value'
        ) as HTMLElement | null
      )?.textContent
    ).toContain('B');
  });

  it('toggles multiple values from the mobile drawer', () => {
    fixture.componentInstance.multiple = true;
    fixture.componentInstance.form.controls.option.setValue([]);
    fixture.detectChanges();
    const select = selectComponent(fixture);

    select.openDrawer();
    select.toggleOption('a');
    select.toggleOption('b');
    select.confirmDrawerValue();

    expect(fixture.componentInstance.form.controls.option.value).toEqual(['a', 'b']);
    expect(
      fixture.nativeElement.querySelector('.responsive-select__mobile--multiple')
    ).toBeTruthy();
  });

  it('supports drawer option arrows and restores focus after confirm', async () => {
    fixture.detectChanges();
    const select = selectComponent(fixture);
    const trigger = fixture.nativeElement.querySelector(
      '.responsive-select__mobile-trigger'
    ) as HTMLButtonElement;

    trigger.focus();
    select.openDrawer();
    fixture.detectChanges();

    const options = fixture.nativeElement.querySelectorAll(
      '.responsive-select__drawer-option'
    ) as NodeListOf<HTMLButtonElement>;
    options[0].focus();
    options[0].dispatchEvent(
      new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true })
    );

    expect(document.activeElement).toBe(options[1]);

    select.toggleOption('b');
    select.confirmDrawerValue();
    await new Promise(resolve => setTimeout(resolve));

    expect(document.activeElement).toBe(trigger);
  });
});

function selectComponent(fixture: ComponentFixture<HostComponent>): {
  confirmDrawerValue(): void;
  openDrawer(): void;
  toggleOption(value: string): void;
} {
  return fixture.debugElement.query(By.directive(ResponsiveSelect)).componentInstance;
}
