import { ChangeDetectionStrategy, Component, signal, ViewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatSidenavModule, MatSidenav } from '@angular/material/sidenav';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { Sidebar } from '../sidebar/sidebar';
import { Header } from '../header/header';

@Component({
  selector: 'app-main-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, MatSidenavModule, Sidebar, Header],
  template: `
    <div class="layout-shell">
      <app-header
        [sidenavOpen]="sidenavOpen()"
        (toggleSidenav)="toggleSidenav()"
      ></app-header>

      <mat-sidenav-container class="layout-body">
        <mat-sidenav
          #sidenav
          [mode]="isMobile() ? 'over' : 'side'"
          [opened]="sidenavOpen()"
          (openedChange)="sidenavOpen.set($event)"
          class="layout-sidenav"
          [attr.role]="'navigation'"
        >
          <app-sidebar (navigated)="onNavigated()"></app-sidebar>
        </mat-sidenav>

        <mat-sidenav-content class="layout-content">
          <main class="layout-main eg-animate-in">
            <router-outlet />
          </main>
        </mat-sidenav-content>
      </mat-sidenav-container>
    </div>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }

    .layout-shell {
      display: flex;
      flex-direction: column;
      height: 100vh;
      background: var(--eg-surface-dim);
    }

    .layout-body {
      flex: 1;
      overflow: hidden;
    }

    .layout-sidenav {
      width: var(--eg-sidebar-width);
      border-right: 1px solid var(--eg-border) !important;
      background: var(--eg-surface) !important;
      box-shadow: none !important;
    }

    .layout-content {
      background: var(--eg-surface-dim);
    }

    .layout-main {
      padding: 24px;
      max-width: 1600px;

      @media (min-width: 1280px) {
        padding: 28px 32px;
      }
    }
  `,
})
export class MainLayout {
  @ViewChild('sidenav') sidenav!: MatSidenav;

  private readonly breakpointObserver = inject(BreakpointObserver);

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
