import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  input,
  OnDestroy,
  output,
  viewChild,
} from '@angular/core';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

let nextDialogId = 0;

@Component({
  selector: 'app-inscripcion-dialog',
  imports: [OrtButtonModule, OrtIconModule],
  templateUrl: './inscripcion-dialog.html',
  styleUrl: './inscripcion-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionDialog implements OnDestroy {
  private readonly document = inject(DOCUMENT);
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);
  private readonly dialog = viewChild<ElementRef<HTMLDialogElement>>('dialog');
  private readonly dialogId = `inscripcion-dialog-${nextDialogId++}`;
  private previouslyFocusedElement: HTMLElement | null = null;
  private previousBodyOverflow: string | null = null;
  private wasOpen = false;

  public readonly open = input(false);
  public readonly title = input.required<string>();
  public readonly helperText = input('');
  public readonly closeLabel = input('Cerrar diálogo');
  public readonly closed = output<void>();

  protected readonly titleId = `${this.dialogId}-title`;
  protected readonly descriptionId = `${this.dialogId}-description`;

  constructor() {
    effect(() => {
      const isOpen = this.open();
      const dialog = this.dialog()?.nativeElement;
      if (!dialog) return;

      if (isOpen && !this.wasOpen && !dialog.open) {
        this.previouslyFocusedElement = this.document.activeElement as HTMLElement | null;
        this.lockPageScroll();
        dialog.showModal();
        setTimeout(() => this.focusFirstControl());
      } else if (!isOpen && this.wasOpen && dialog.open) {
        dialog.close();
        this.unlockPageScroll();
        setTimeout(() => this.previouslyFocusedElement?.focus());
      }

      this.wasOpen = isOpen;
    });
  }

  public ngOnDestroy(): void {
    this.unlockPageScroll();
  }

  protected requestClose(event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    this.closed.emit();
  }

  protected onDialogClick(event: MouseEvent): void {
    if (event.target === this.dialog()?.nativeElement) {
      this.requestClose(event);
    }
  }

  private focusFirstControl(): void {
    const initialControl = this.host.nativeElement.querySelector<HTMLElement>(
      '[data-dialog-initial-focus]'
    );
    const fallbackControl = this.host.nativeElement.querySelector<HTMLElement>(
      'button, [href], input, select'
    );

    (initialControl ?? fallbackControl)?.focus();
  }

  private lockPageScroll(): void {
    if (this.previousBodyOverflow !== null) return;

    this.previousBodyOverflow = this.document.body.style.overflow;
    this.document.body.style.overflow = 'hidden';
  }

  private unlockPageScroll(): void {
    if (this.previousBodyOverflow === null) return;

    this.document.body.style.overflow = this.previousBodyOverflow;
    this.previousBodyOverflow = null;
  }
}
