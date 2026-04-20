import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faMicrochip,
  faMemory,
  faHardDrive,
  faListOl,
} from '@fortawesome/free-solid-svg-icons';

import { Node } from '../../models/node.models';

@Component({
  selector: 'app-node-health-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FontAwesomeModule, DecimalPipe],
  templateUrl: './node-health-card.component.html',
  styleUrl: './node-health-card.component.scss'
})
export class NodeHealthCard {
  readonly node = input.required<Node>();

  protected readonly faHardDrive = faHardDrive;
  protected readonly faMicrochip = faMicrochip;
  protected readonly faMemory = faMemory;
  protected readonly faListOl = faListOl;

  protected readonly storagePercent = computed(() => {
    const n = this.node();
    if (n.maxStorageMb === 0) return 0;
    return Math.round(((n.maxStorageMb - n.availableStorageMb) / n.maxStorageMb) * 100);
  });

  protected readonly storageUsedFormatted = computed(() =>
    this.formatMb(this.node().maxStorageMb - this.node().availableStorageMb),
  );

  protected readonly storageMaxFormatted = computed(() =>
    this.formatMb(this.node().maxStorageMb),
  );

  protected readonly availableFormatted = computed(() =>
    this.formatMb(this.node().availableStorageMb),
  );

  private formatMb(mb: number): string {
    if (mb >= 1024 * 1024) return `${(mb / (1024 * 1024)).toFixed(1)} TB`;
    if (mb >= 1024) return `${(mb / 1024).toFixed(1)} GB`;
    return `${mb} MB`;
  }
}
