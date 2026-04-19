import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'ui-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatProgressSpinnerModule, FaIconComponent],
  template: `
    @if (variant() === 'ghost') {
      <button
        mat-button
        [class]="buttonClasses()"
        [disabled]="disabled() || loading()"
        [type]="type()"
        (click)="clicked.emit($event)"
      >
        @if (loading()) {
          <mat-spinner diameter="18" class="mr-2" />
        } @else if (icon()) {
          <fa-icon [icon]="icon()!" class="mr-2" size="sm" />
        }
        <ng-content />
      </button>
    } @else {
      <button
        mat-flat-button
        [class]="buttonClasses()"
        [disabled]="disabled() || loading()"
        [type]="type()"
        (click)="clicked.emit($event)"
      >
        @if (loading()) {
          <mat-spinner diameter="18" class="mr-2" />
        } @else if (icon()) {
          <fa-icon [icon]="icon()!" class="mr-2" size="sm" />
        }
        <ng-content />
      </button>
    }
  `,
  styles: `
    :host { display: inline-block; }
    .btn-primary {
      --mdc-filled-button-container-color: #0078d4;
      --mdc-filled-button-label-text-color: #fff;
      --mdc-filled-button-hover-state-layer-color: rgba(255 255 255 / 0.08);
    }
    .btn-danger {
      --mdc-filled-button-container-color: #d13438;
      --mdc-filled-button-label-text-color: #fff;
      --mdc-filled-button-hover-state-layer-color: rgba(255 255 255 / 0.08);
    }
    .btn-secondary {
      --mdc-filled-button-container-color: var(--eg-surface-container, #edebe9);
      --mdc-filled-button-label-text-color: var(--eg-text-primary, #323130);
    }
    .btn-sm {
      --mdc-filled-button-container-height: 32px;
      font-size: 0.8125rem;
    }
    .btn-lg {
      --mdc-filled-button-container-height: 44px;
      font-size: 0.9375rem;
      letter-spacing: -0.01em;
    }
    button {
      border-radius: var(--eg-radius-md, 8px) !important;
      font-weight: 600;
      transition: box-shadow var(--eg-transition-fast, 150ms);

      &:hover:not(:disabled) {
        box-shadow: var(--eg-shadow-sm);
      }

      &:active:not(:disabled) {
        transform: translateY(0.5px);
      }
    }
  `,
})
export class UiButton {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly icon = input<IconProp | null>(null);
  readonly type = input<'button' | 'submit'>('button');
  readonly clicked = output<MouseEvent>();

  protected buttonClasses(): string {
    return `btn-${this.variant()} btn-${this.size()}`;
  }
}
