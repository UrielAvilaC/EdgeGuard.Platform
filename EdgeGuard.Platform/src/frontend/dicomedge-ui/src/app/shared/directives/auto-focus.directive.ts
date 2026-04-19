import { AfterViewInit, Directive, ElementRef, inject, input } from '@angular/core';

@Directive({
  selector: '[uiAutoFocus]',
})
export class AutoFocusDirective implements AfterViewInit {
  readonly uiAutoFocus = input(true);

  private readonly el = inject(ElementRef<HTMLElement>);

  ngAfterViewInit(): void {
    if (this.uiAutoFocus()) {
      this.el.nativeElement.focus();
    }
  }
}
