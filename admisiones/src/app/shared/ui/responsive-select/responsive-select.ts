import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { ControlValueAccessor, NgControl, Validators } from '@angular/forms';
import {
  getBankSvg,
  OrtButton,
  OrtDrawer,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtSearchableSelect,
  OrtSearchableSelectModule,
  OrtSelectModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

export interface ResponsiveSelectOption {
  value: string;
  label: string;
  icon?: string;
  description?: string;
}

export interface ResponsiveSelectOptionGroup {
  label: string;
  options: readonly ResponsiveSelectOption[];
}

type ResponsiveSelectValue = string | string[] | null;

let nextResponsiveSelectId = 0;

@Component({
  selector: 'app-responsive-select',
  imports: [
    OrtButton,
    OrtDrawer,
    OrtFormFieldModule,
    OrtIconModule,
    OrtInputModule,
    OrtSearchableSelectModule,
    OrtSelectModule,
  ],
  templateUrl: './responsive-select.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './responsive-select.scss',
  host: {
    // El id identifica al control interno, que es el destino de foco del resumen de errores.
    // Los consumidores lo pasan como atributo estatico (`id="..."`), y Angular lo deja tambien
    // en el host: quedaban dos elementos con el mismo id y `getElementById` devolvia el wrapper.
    '[attr.id]': 'null',
  },
})
export class ResponsiveSelect implements ControlValueAccessor {
  protected readonly getBankSvg = getBankSvg;

  public readonly label = input.required<string>();
  public readonly options = input<readonly ResponsiveSelectOption[]>([]);
  public readonly optionGroups = input<readonly ResponsiveSelectOptionGroup[] | null>(null);
  public readonly id = input(`responsive-select-${nextResponsiveSelectId++}`);
  public readonly placeholder = input('Seleccioná...');
  public readonly errorText = input('Seleccioná una opción');
  public readonly noOptionsText = input('No hay opciones');
  public readonly loadingText = input('Cargando...');
  public readonly ariaLabel = input<string | null>(null, { alias: 'aria-label' });
  public readonly disabled = input(false, { transform: booleanAttribute });
  public readonly loading = input(false, { transform: booleanAttribute });
  public readonly multiple = input(false, { transform: booleanAttribute });
  public readonly searchable = input(false, { transform: booleanAttribute });
  public readonly required = input<boolean | null>(null);

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly breakpointService = inject(BreakpointService);
  private readonly ngControl = inject(NgControl, { optional: true, self: true });
  private readonly disabledFromForms = signal(false);
  private readonly mobileTrigger = viewChild<ElementRef<HTMLButtonElement>>('mobileTrigger');
  private readonly desktopSearchableSelect = viewChild(OrtSearchableSelect);

  protected readonly isMobile = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall;
  });
  protected readonly value = signal<ResponsiveSelectValue>('');
  protected readonly pendingValue = signal<ResponsiveSelectValue>('');
  protected readonly drawerOpen = signal(false);
  protected readonly search = signal('');

  private onChange: (value: ResponsiveSelectValue) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  constructor() {
    if (this.ngControl) this.ngControl.valueAccessor = this;

    // Mismo problema que resuelve el trigger propio de `ort-select`: el valor puede escribirse
    // antes de que lleguen las opciones. `ort-searchable-select` fija el texto de su input al
    // recibir el valor y no lo recalcula cuando aparecen las opciones, asi que lo reaplicamos.
    effect(() => {
      const select = this.desktopSearchableSelect();
      const hasOptions = this.allOptions().length > 0;
      if (!select || !hasOptions) return;

      // Sin `untracked` cada tecleo (que confirma null) volveria a escribir el valor y borraria
      // el texto que el usuario esta escribiendo.
      untracked(() => {
        if (this.hasValue()) select.writeValue(this.value());
      });
    });
  }

  writeValue(value: unknown): void {
    this.value.set(this.normalizeValue(value));
  }

  registerOnChange(fn: (value: ResponsiveSelectValue) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabledFromForms.set(isDisabled);
  }

  protected fieldId(): string {
    return this.id();
  }

  protected mobileId(): string {
    return `${this.id()}-mobile`;
  }

  protected drawerId(): string {
    return `${this.id()}-drawer`;
  }

  protected errorId(): string {
    return `${this.id()}-error`;
  }

  protected drawerTitle(): string {
    return `Seleccionar ${this.label().toLocaleLowerCase('es-UY')}`;
  }

  protected isDisabled(): boolean {
    return this.disabled() || this.disabledFromForms();
  }

  protected isRequired(): boolean {
    return this.required() ?? !!this.ngControl?.control?.hasValidator(Validators.required);
  }

  protected invalid(): boolean {
    const control = this.ngControl?.control;
    return !!control && control.invalid && control.touched;
  }

  protected hasValue(): boolean {
    const value = this.value();
    return Array.isArray(value) ? value.length > 0 : !!value;
  }

  protected groups(): readonly ResponsiveSelectOptionGroup[] {
    const groups = this.optionGroups();
    return groups ?? [{ label: '', options: this.options() }];
  }

  protected filteredGroups(): readonly ResponsiveSelectOptionGroup[] {
    const query = this.search().trim().toLocaleLowerCase('es-UY');
    if (!query) return this.groups();

    return this.groups()
      .map(group => ({
        ...group,
        options: group.options.filter(option =>
          option.label.toLocaleLowerCase('es-UY').includes(query)
        ),
      }))
      .filter(group => group.options.length > 0);
  }

  protected showSearch(): boolean {
    return this.searchable() && this.allOptions().length >= 6;
  }

  protected selectedLabel(): string {
    const selected = this.selectedOptions();
    return selected.length > 0
      ? selected.map(option => option.label).join(', ')
      : this.placeholder();
  }

  protected selectedOptions(): readonly ResponsiveSelectOption[] {
    const value = this.value();
    const values = Array.isArray(value) ? value : value ? [value] : [];
    return this.allOptions().filter(option => values.includes(option.value));
  }

  protected onDesktopValueChange(value: unknown): void {
    this.commitValue(value);
  }

  // Etiqueta para valores sin opcion asociada todavia (el catalogo puede llegar despues).
  protected readonly optionLabel = (value: string): string =>
    this.allOptions().find(option => option.value === value)?.label ?? '';

  // El searchable select confirma null en cuanto el texto se aparta de la seleccion, asi que
  // marcar touched aca mostraria el error mientras se escribe: eso queda para el cierre del panel.
  protected onSearchableValueChange(value: unknown): void {
    const normalizedValue = this.normalizeValue(value);
    this.value.set(normalizedValue);
    this.onChange(normalizedValue);
  }

  protected onDesktopKeydown(event: KeyboardEvent): void {
    if (!['Enter', ' ', 'Tab'].includes(event.key)) return;

    const select = event.target as HTMLElement | null;
    if (select?.getAttribute('aria-expanded') !== 'true') return;

    const activeId = select.getAttribute('aria-activedescendant');
    const listboxId = select.getAttribute('aria-controls');
    const listbox = listboxId ? document.getElementById(listboxId) : null;
    const activeText =
      listbox?.querySelector('.ort-option-active')?.textContent ??
      (activeId ? listbox?.querySelector(`#${activeId}`)?.textContent : '') ??
      '';
    const activeValue =
      this.allOptions().find(option => activeText.includes(option.label))?.value ??
      (activeText.includes(this.placeholder()) ? '' : null);
    if (this.multiple() || activeValue === null || activeValue === undefined) return;

    setTimeout(() => this.commitMissingKeyboardSelection(select, activeValue));
  }

  protected onDesktopOpenedChange(open: boolean): void {
    if (!open) this.onTouched();
  }

  protected openDrawer(): void {
    if (this.isDisabled()) return;

    this.pendingValue.set(this.cloneValue(this.value()));
    this.search.set('');
    this.drawerOpen.set(true);
  }

  protected onDrawerOpenChange(open: boolean): void {
    if (!open) this.closeDrawer(true, true);
  }

  protected onDrawerClosed(): void {
    this.closeDrawer(true, true);
  }

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected toggleOption(value: string): void {
    if (!this.multiple()) {
      this.pendingValue.set(value);
      return;
    }

    const pendingValue = this.pendingValue();
    const current = Array.isArray(pendingValue) ? [...pendingValue] : [];
    this.pendingValue.set(
      current.includes(value) ? current.filter(item => item !== value) : [...current, value]
    );
  }

  protected pendingSelected(value: string): boolean {
    const pendingValue = this.pendingValue();
    return Array.isArray(pendingValue) ? pendingValue.includes(value) : pendingValue === value;
  }

  protected confirmDrawerValue(): void {
    this.commitValue(this.pendingValue());
    this.closeDrawer(false, true);
  }

  protected optionRole(): 'checkbox' | 'radio' {
    return this.multiple() ? 'checkbox' : 'radio';
  }

  protected onDrawerOptionKeydown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowRight':
        this.focusRelativeOption(event, 1);
        break;
      case 'ArrowUp':
      case 'ArrowLeft':
        this.focusRelativeOption(event, -1);
        break;
      case 'Home':
        this.focusOptionAt(event, 0);
        break;
      case 'End':
        this.focusOptionAt(event, this.drawerOptions().length - 1);
        break;
    }
  }

  private focusRelativeOption(event: KeyboardEvent, offset: number): void {
    const options = this.drawerOptions();
    const currentIndex = options.indexOf(event.currentTarget as HTMLButtonElement);
    const nextIndex = (currentIndex + offset + options.length) % options.length;
    this.focusOptionAt(event, nextIndex);
  }

  private focusOptionAt(event: KeyboardEvent, index: number): void {
    const option = this.drawerOptions()[index];
    if (!option) return;

    event.preventDefault();
    option.focus();
  }

  private closeDrawer(markTouched: boolean, restoreFocus = false): void {
    if (markTouched) this.onTouched();
    this.drawerOpen.set(false);
    this.search.set('');
    if (restoreFocus) this.restoreMobileFocus();
  }

  private commitValue(value: unknown): void {
    const normalizedValue = this.normalizeValue(value);
    this.value.set(normalizedValue);
    this.onChange(normalizedValue);
    this.onTouched();
  }

  private commitMissingKeyboardSelection(select: HTMLElement, activeValue: string): void {
    if (this.value() === activeValue) return;

    this.commitValue(activeValue);
    select.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true })
    );
  }

  private normalizeValue(value: unknown): ResponsiveSelectValue {
    if (this.multiple()) {
      return Array.isArray(value)
        ? value.filter((item): item is string => typeof item === 'string')
        : [];
    }

    // Tolera controles que guardan array (p. ej. seminarios) en modo simple.
    if (Array.isArray(value)) return typeof value[0] === 'string' ? value[0] : '';

    return typeof value === 'string' ? value : '';
  }

  private cloneValue(value: ResponsiveSelectValue): ResponsiveSelectValue {
    return Array.isArray(value) ? [...value] : value;
  }

  private allOptions(): readonly ResponsiveSelectOption[] {
    return this.groups().flatMap(group => group.options);
  }

  private drawerOptions(): HTMLButtonElement[] {
    return Array.from(
      this.host.nativeElement.querySelectorAll<HTMLButtonElement>(
        '.responsive-select__drawer-option'
      )
    );
  }

  private restoreMobileFocus(): void {
    setTimeout(() => this.mobileTrigger()?.nativeElement.focus());
  }
}
