import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-routing-rules-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './routing-rules-page.component.html',
  styleUrl: './routing-rules-page.component.scss'
})
export default class RoutingRulesPage {}
