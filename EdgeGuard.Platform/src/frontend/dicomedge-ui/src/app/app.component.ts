import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FaIconLibrary, FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { fas } from '@fortawesome/free-solid-svg-icons';
import { far } from '@fortawesome/free-regular-svg-icons';

import { AuthService } from './core/auth/services/auth.service';
import { TitleService } from './core/services/title.service';
import { NodePushNotifier } from './core/services/node-push-notifier.service';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, FontAwesomeModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class App {
  protected readonly title = signal('EdgeGuard');

  private readonly library = inject(FaIconLibrary);
  private readonly authService = inject(AuthService);
  private readonly titleService = inject(TitleService);
  private readonly nodePushNotifier = inject(NodePushNotifier);

  constructor() {
    this.authService.init();
    this.titleService.init();
    // Surface async Hub→Node push results (NodePushStatus) as toasts app-wide.
    this.nodePushNotifier.init();

    this.library.addIconPacks(fas, far);
  }
}
