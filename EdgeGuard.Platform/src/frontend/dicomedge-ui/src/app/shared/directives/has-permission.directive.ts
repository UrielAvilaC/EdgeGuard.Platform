import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';

import { AuthStore } from '../../core/auth/store/auth.store';

@Directive({
  selector: '[hasPermission]',
})
export class HasPermissionDirective {
  readonly hasPermission = input.required<string>();

  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly authStore = inject(AuthStore);

  private hasView = false;

  constructor() {
    effect(() => {
      const requiredPermission = this.hasPermission();
      const userPermissions = this.authStore.permissions();
      const hasAccess = userPermissions.includes(requiredPermission);

      if (hasAccess && !this.hasView) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.hasView = true;
      } else if (!hasAccess && this.hasView) {
        this.viewContainer.clear();
        this.hasView = false;
      }
    });
  }
}
