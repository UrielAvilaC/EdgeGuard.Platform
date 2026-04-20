import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FaIconLibrary, FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faHome,
  faUser,
  faCog,
  faSearch,
  faBars,
  faPlus,
  faEdit,
  faTrash,
  faCheck,
  faTimes,
  faChevronLeft,
  faChevronRight,
  faSpinner,
  faExclamationTriangle,
  faInfoCircle,
  faSignOutAlt,
  faTachometerAlt,
  faServer,
  faDatabase,
  faNetworkWired,
  faClipboardList,
  faComments,
  faShieldAlt,
  faUsersCog,
  faSlidersH,
  faChartBar,
  faSync,
  faEye,
  faDownload,
  faUpload,
  faFilter,
  faSortUp,
  faSortDown,
  faEllipsisV,
  faExclamationCircle,
  faCheckCircle,
  faTimesCircle,
  faMoon,
  faSun,
} from '@fortawesome/free-solid-svg-icons';
import {
  faBell as farBell,
  faCircle as farCircle,
} from '@fortawesome/free-regular-svg-icons';

import { AuthService } from './core/auth/services/auth.service';

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

  constructor() {
    this.authService.init();

    this.library.addIcons(
      faHome, faUser, faCog, faSearch, faBars,
      faPlus, faEdit, faTrash, faCheck, faTimes,
      faChevronLeft, faChevronRight, faSpinner,
      faExclamationTriangle, faInfoCircle,
      faSignOutAlt, faTachometerAlt, faServer,
      faDatabase, faNetworkWired, faClipboardList,
      faComments, faShieldAlt, faUsersCog, faSlidersH,
      faChartBar, faSync, faEye, faDownload, faUpload,
      faFilter, faSortUp, faSortDown, faEllipsisV,
      faExclamationCircle, faCheckCircle, faTimesCircle,
      faMoon, faSun,
      farBell, farCircle,
    );
  }
}
