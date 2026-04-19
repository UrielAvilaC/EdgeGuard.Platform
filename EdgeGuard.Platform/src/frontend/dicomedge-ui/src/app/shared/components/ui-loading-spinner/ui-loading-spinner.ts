import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'ui-loading-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatProgressSpinnerModule],
  template: `
    @if (overlay()) {
      <div class="spinner-overlay">
        <mat-spinner [diameter]="diameter()" />
      </div>
    } @else {
      <div class="spinner-inline">
        <mat-spinner [diameter]="diameter()" />
      </div>
    }
  `,
  styles: `
    :host { display: block; }

    .spinner-overlay {
      position: absolute;
      inset: 0;
      z-index: 10;
      display: flex;
      align-items: center;
      justify-content: center;
      background: rgba(255 255 255 / 0.7);
      backdrop-filter: blur(2px);
      border-radius: inherit;
    }

    :host-context(.dark) .spinner-overlay {
      background: rgba(15 23 42 / 0.7);
    }

    .spinner-inline {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 24px;
    }
  `,
})
export class UiLoadingSpinner {
  readonly overlay = input(false);
  readonly diameter = input(40);
}
