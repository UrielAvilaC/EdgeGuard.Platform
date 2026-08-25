import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faMicrochip,
  faMemory,
  faHardDrive,
  faListOl,
  faDatabase,
  faServer,
} from '@fortawesome/free-solid-svg-icons';

import { Node } from '../../models/node.models';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { formatMb, readStorage, storageBarClass } from '../../../../shared/utils/storage-usage';

@Component({
  selector: 'app-node-health-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FontAwesomeModule, DecimalPipe, RelativeTimePipe],
  templateUrl: './node-health-card.component.html',
  styleUrl: './node-health-card.component.scss'
})
export class NodeHealthCard {
  readonly node = input.required<Node>();

  protected readonly faHardDrive = faHardDrive;
  protected readonly faMicrochip = faMicrochip;
  protected readonly faMemory = faMemory;
  protected readonly faListOl = faListOl;
  protected readonly faDatabase = faDatabase;
  protected readonly faServer = faServer;

  protected readonly storage = computed(() => readStorage(this.node()));

  protected readonly barClass = storageBarClass;
  protected readonly format = formatMb;

  /**
   * El detalle es la única pantalla que desglosa. En la tabla basta el total,
   * pero aquí importa distinguir qué crece: los estudios se pueden purgar, la
   * base no se reduce purgando, y el volumen puede llenarse por cosas que nada
   * tienen que ver con el nodo.
   */
  protected readonly volumeUsedPercent = computed(() => {
    const n = this.node();
    if (!n.storageVolumeTotalMb || n.storageVolumeFreeMb === null) return null;
    const used = n.storageVolumeTotalMb - n.storageVolumeFreeMb;
    return Math.round((used / n.storageVolumeTotalMb) * 100);
  });
}
