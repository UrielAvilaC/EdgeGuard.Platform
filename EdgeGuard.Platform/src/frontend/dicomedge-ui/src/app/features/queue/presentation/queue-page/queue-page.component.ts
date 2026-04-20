import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-queue-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './queue-page.component.html',
  styleUrl: './queue-page.component.scss'
})
export default class QueuePage {}
