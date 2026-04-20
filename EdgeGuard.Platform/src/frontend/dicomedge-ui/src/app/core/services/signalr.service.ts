import { DestroyRef, inject, Injectable, NgZone, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  HttpTransportType,
} from '@microsoft/signalr';
import { Observable, Subject, timer } from 'rxjs';

import { ENVIRONMENT } from '../config/environment.config';
import { AuthStore } from '../auth/store/auth.store';

export type SignalREvent =
  | 'StudyReceived'
  | 'StudyStatusChanged'
  | 'NodeStatusChanged'
  | 'NodeHeartbeat'
  | 'Hl7MessageReceived'
  | 'WhatsAppNotificationSent'
  | 'AuditEvent';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly env = inject(ENVIRONMENT);
  private readonly authStore = inject(AuthStore);
  private readonly zone = inject(NgZone);

  private connection: HubConnection | null = null;
  private readonly subjects = new Map<string, Subject<unknown>>();
  private retryCount = 0;
  private readonly maxRetries = 10;

  readonly connected = signal(false);

  async start(destroyRef: DestroyRef): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) return;

    const token = this.authStore.accessToken();
    if (!token) return;

    this.connection = new HubConnectionBuilder()
      .withUrl(this.env.signalRUrl, {
        accessTokenFactory: () => this.authStore.accessToken() ?? '',
        transport: HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (ctx) => {
          if (ctx.previousRetryCount >= this.maxRetries) return null;
          return Math.min(1000 * Math.pow(2, ctx.previousRetryCount), 30_000);
        },
      })
      .configureLogging(this.env.production ? LogLevel.Warning : LogLevel.Information)
      .build();

    this.connection.onreconnecting(() => {
      this.zone.run(() => this.connected.set(false));
    });

    this.connection.onreconnected(() => {
      this.zone.run(() => {
        this.connected.set(true);
        this.retryCount = 0;
      });
    });

    this.connection.onclose(() => {
      this.zone.run(() => this.connected.set(false));
    });

    // Cleanup on destroy
    timer(0).pipe(takeUntilDestroyed(destroyRef)).subscribe({
      complete: () => this.stop(),
    });

    try {
      await this.connection.start();
      this.zone.run(() => {
        this.connected.set(true);
        this.retryCount = 0;
      });
    } catch {
      this.zone.run(() => this.connected.set(false));
    }
  }

  async stop(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
      } catch { /* ignore */ }
      this.connection = null;
      this.connected.set(false);
    }
    this.subjects.forEach(s => s.complete());
    this.subjects.clear();
  }

  on<T = unknown>(event: SignalREvent): Observable<T> {
    let subject = this.subjects.get(event);
    if (!subject) {
      subject = new Subject<unknown>();
      this.subjects.set(event, subject);
      this.connection?.on(event, (data: unknown) => {
        this.zone.run(() => subject!.next(data));
      });
    }
    return subject.asObservable() as Observable<T>;
  }

  async invoke(method: string, ...args: unknown[]): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke(method, ...args);
    }
  }

  async joinDashboard(): Promise<void> {
    await this.invoke('JoinDashboard');
  }

  async leaveDashboard(): Promise<void> {
    await this.invoke('LeaveDashboard');
  }

  async joinNode(nodeId: string): Promise<void> {
    await this.invoke('JoinNode', nodeId);
  }

  async leaveNode(nodeId: string): Promise<void> {
    await this.invoke('LeaveNode', nodeId);
  }
}
