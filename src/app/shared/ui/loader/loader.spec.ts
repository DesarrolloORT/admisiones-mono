import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoaderService } from '@desarrolloort/ngx-utils';

import { Loader } from './loader';

describe('Loader', () => {
  let fixture: ComponentFixture<Loader>;
  let component: Loader;

  /**
   * Create the component with a LoaderService whose isLoading
   * is a signal returning the given boolean.
   */
  function createComponent(isLoadingValue: boolean) {
    TestBed.overrideProvider(LoaderService, {
      useValue: {
        isLoading: signal(isLoadingValue),
      } as unknown as LoaderService,
    });

    fixture = TestBed.createComponent(Loader);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  // Synchronous beforeEach: fast, no hanging zone
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Loader],
    });
  });

  it('should match snapshot when loading = true', () => {
    createComponent(true);
    fixture.nativeElement.removeAttribute('id');
    expect(fixture.nativeElement).toMatchSnapshot();
  });

  it('should initialize with the correct loading state (false)', () => {
    createComponent(false);
    expect(component['loading']()).toBe(false);
  });

  it('should initialize with the correct loading state (true)', () => {
    createComponent(true);
    expect(component['loading']()).toBe(true);
  });
});
