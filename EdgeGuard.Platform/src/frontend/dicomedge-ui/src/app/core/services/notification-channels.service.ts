import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, catchError, forkJoin, map, of, shareReplay, tap } from 'rxjs';

import { ApiClient } from '../api/api-client';
import { API_ROUTES } from '../api/api-routes';

interface NotificationSettingsDto {
  smtpEnabled: boolean;
}

interface WhatsAppConfigStatusDto {
  enabled: boolean;
}

export interface NotificationChannelsState {
  email: boolean;
  whatsapp: boolean;
}

/**
 * Resolves which result-delivery channels (email / WhatsApp) are actually configured
 * and active on the Hub. The value is loaded once and cached so it can drive UI gating
 * (nav items, route guards, the study-detail delivery dialog) without repeated calls.
 */
@Injectable({ providedIn: 'root' })
export class NotificationChannelsService {
  private readonly api = inject(ApiClient);

  private readonly state = signal<NotificationChannelsState | null>(null);

  readonly emailEnabled = computed(() => this.state()?.email ?? false);
  readonly whatsAppEnabled = computed(() => this.state()?.whatsapp ?? false);
  readonly anyEnabled = computed(() => this.emailEnabled() || this.whatsAppEnabled());
  readonly loaded = computed(() => this.state() !== null);

  private inFlight$?: Observable<NotificationChannelsState>;

  /** Loads channel enablement once and caches it. Safe to call repeatedly. */
  ensureLoaded(): Observable<NotificationChannelsState> {
    const current = this.state();
    if (current) return of(current);
    if (this.inFlight$) return this.inFlight$;

    this.inFlight$ = forkJoin({
      email: this.api.get<NotificationSettingsDto>(API_ROUTES.NOTIFICATION_SETTINGS.GET).pipe(
        map((s) => s.smtpEnabled),
        catchError(() => of(false)),
      ),
      whatsapp: this.api.get<WhatsAppConfigStatusDto>(API_ROUTES.WHATSAPP.CONFIG_STATUS).pipe(
        map((s) => s.enabled),
        catchError(() => of(false)),
      ),
    }).pipe(
      tap((s) => {
        this.state.set(s);
        this.inFlight$ = undefined;
      }),
      shareReplay(1),
    );

    return this.inFlight$;
  }

  /** Fire-and-forget load used to warm the signals at app startup. */
  load(): void {
    this.ensureLoaded().subscribe();
  }

  /**
   * Discards the cached state and re-fetches from the Hub. Use when the channel
   * enablement may have changed server-side (e.g. WhatsApp toggled in settings/DB)
   * and the SPA is still running, since {@link ensureLoaded} otherwise serves the
   * value cached at startup indefinitely.
   */
  refresh(): Observable<NotificationChannelsState> {
    this.state.set(null);
    this.inFlight$ = undefined;
    return this.ensureLoaded();
  }
}
