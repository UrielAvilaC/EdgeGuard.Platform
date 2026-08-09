import { ChangeDetectionStrategy, Component, signal, ViewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatSidenavModule, MatSidenav } from '@angular/material/sidenav';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { Sidebar } from '../sidebar/sidebar.component';
import { Header } from '../header/header.component';
import { NotificationChannelsService } from '../../core/services/notification-channels.service';

@Component({
  selector: 'app-main-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, MatSidenavModule, Sidebar, Header],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayout {
  @ViewChild('sidenav') sidenav!: MatSidenav;

  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly channels = inject(NotificationChannelsService);

  protected readonly isMobile = toSignal(
    this.breakpointObserver.observe([Breakpoints.Handset]).pipe(
      map((result) => result.matches),
    ),
    { initialValue: false },
  );

  protected readonly sidenavOpen = signal(true);

  constructor() {
    const bp = this.breakpointObserver.observe([Breakpoints.Handset]);
    bp.subscribe((result) => {
      this.sidenavOpen.set(!result.matches);
    });

    // Warm channel enablement so the sidebar can gate WhatsApp / Email-templates items.
    this.channels.load();
  }

  toggleSidenav(): void {
    this.sidenavOpen.update((open) => !open);
  }

  onNavigated(): void {
    if (this.isMobile()) {
      this.sidenavOpen.set(false);
    }
  }
}
