import { Injectable, inject } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { filter, startWith } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TitleService {
  private readonly appTitle = 'EdgeGuard DICOM';
  private readonly router = inject(Router);
  private readonly title = inject(Title);

  private initialized = false;

  init(): void {
    if (this.initialized) {
      return;
    }

    this.initialized = true;

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        startWith(new NavigationEnd(0, this.router.url, this.router.url)),
      )
      .subscribe(() => {
        const routeTitle = this.resolveRouteTitle(this.router.routerState.snapshot.root);
        this.setPageTitle(routeTitle);
      });
  }

  setPageTitle(pageTitle?: string | null): void {
    const normalized = pageTitle?.trim();

    if (!normalized) {
      this.title.setTitle(this.appTitle);
      return;
    }

    this.title.setTitle(`${normalized} | ${this.appTitle}`);
  }

  private resolveRouteTitle(snapshot: ActivatedRouteSnapshot): string | null {
    let current: ActivatedRouteSnapshot | null = snapshot;
    let resolvedTitle: string | null = null;

    while (current) {
      if (typeof current.title === 'string' && current.title.trim().length > 0) {
        resolvedTitle = current.title;
      }

      current = current.firstChild;
    }

    return resolvedTitle;
  }
}
