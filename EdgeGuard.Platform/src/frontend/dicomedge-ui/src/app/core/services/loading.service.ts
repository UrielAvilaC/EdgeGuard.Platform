import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly activeRequests = signal(0);

  readonly isLoading = signal(false);

  start(): void {
    this.activeRequests.update((count) => count + 1);
    this.isLoading.set(true);
  }

  stop(): void {
    this.activeRequests.update((count) => Math.max(0, count - 1));
    if (this.activeRequests() === 0) {
      this.isLoading.set(false);
    }
  }
}
