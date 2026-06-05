import { DestroyRef, effect, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthStore } from '../auth/store/auth.store';
import { SignalRService } from './signalr.service';
import { ToastService } from './toast.service';

/**
 * Result of an asynchronous Hub→Node configuration push, broadcast by the backend
 * over the SignalR `NodePushStatus` event after PR P1-1 made pushes non-blocking.
 */
export interface NodePushStatus {
  nodeId: string;
  kind: 'Config' | 'Rules' | 'Pacs';
  success: boolean;
  error?: string | null;
}

/**
 * Global listener for `NodePushStatus`. Since configuration pushes (saving routing
 * rules, changing PACS, etc.) are now dispatched off the request path, an HTTP 200
 * only means "accepted" — the real per-node result arrives later via SignalR.
 *
 * This service surfaces that result as a toast from any screen. To avoid toast spam
 * on fan-out (a PACS change pushes to every node), only failures are toasted;
 * successes are logged. Initialized once from the app shell.
 */
@Injectable({ providedIn: 'root' })
export class NodePushNotifier {
  private readonly signalR = inject(SignalRService);
  private readonly toast = inject(ToastService);
  private readonly authStore = inject(AuthStore);
  private readonly destroyRef = inject(DestroyRef);

  private started = false;

  /** Idempotent. Call once from the authenticated app shell. */
  init(): void {
    // Start the connection as soon as a token is available. start() assigns the
    // connection synchronously (before its first await), so registering the handler
    // immediately after binds it correctly. The `started` guard prevents re-entry.
    effect(() => {
      if (this.authStore.accessToken() && !this.started) {
        this.started = true;
        this.signalR.start(this.destroyRef).catch(() => undefined);
        this.signalR
          .on<NodePushStatus>('NodePushStatus')
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe((status) => this.handle(status));
      }
    });

    // (Re)join the dashboard group on every (re)connection so the event reaches the
    // client regardless of which screen is active and survives reconnects.
    effect(() => {
      if (this.signalR.connected()) {
        this.signalR.joinDashboard().catch(() => undefined);
      }
    });
  }

  private handle(status: NodePushStatus): void {
    if (status.success) {
      // Avoid toast spam on fan-out; a successful save is already confirmed locally.
      console.debug('[NodePushStatus] %s push to node %s succeeded', status.kind, status.nodeId);
      return;
    }

    const what = this.kindLabel(status.kind);
    const reason = status.error?.trim() ? `: ${status.error}` : '';
    this.toast.error(`No se pudo sincronizar ${what} con el nodo ${status.nodeId}${reason}`);
  }

  private kindLabel(kind: NodePushStatus['kind']): string {
    switch (kind) {
      case 'Config': return 'la configuración';
      case 'Rules': return 'las reglas de ruteo';
      case 'Pacs': return 'los destinos PACS';
      default: return 'la configuración';
    }
  }
}
