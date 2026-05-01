import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';

@Component({
  selector: 'app-not-found-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, FontAwesomeModule, UiButton],
  templateUrl: './not-found-page.component.html',
  styleUrl: './not-found-page.component.scss'
})
export default class NotFoundPage {}
