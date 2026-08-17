import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';

import { NotificationChannelsService } from '../../services/notification-channels.service';

/** Allows the route only when the WhatsApp channel is active; otherwise redirects to the dashboard. */
export const whatsAppChannelGuard: CanActivateFn = () => {
  const channels = inject(NotificationChannelsService);
  const router = inject(Router);
  return channels.ensureLoaded().pipe(
    map((s) => (s.whatsapp ? true : router.createUrlTree(['/dashboard']))),
  );
};

/** Allows the route only when the email (SMTP) channel is active; otherwise redirects to the dashboard. */
export const emailChannelGuard: CanActivateFn = () => {
  const channels = inject(NotificationChannelsService);
  const router = inject(Router);
  return channels.ensureLoaded().pipe(
    map((s) => (s.email ? true : router.createUrlTree(['/dashboard']))),
  );
};
