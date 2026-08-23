import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type DialogFooterAlign = 'end' | 'between';

/**
 * Envoltura estándar de los diálogos: header y footer fijos, body scrollable.
 *
 * El body es el único que crece y desborda, de modo que los botones de acción
 * siempre quedan alcanzables aunque el contenido supere la altura de pantalla.
 *
 * Los diálogos con formulario deben envolver el componente completo, no el body,
 * para que los botones `type="submit"` del footer sigan dentro del `<form>`:
 *
 * ```html
 * <form (ngSubmit)="onSubmit()">
 *   <ui-dialog>
 *     <div uiDialogHeader>…</div>
 *     <div uiDialogBody>…</div>
 *     <div uiDialogFooter>…</div>
 *   </ui-dialog>
 * </form>
 * ```
 */
@Component({
  selector: 'ui-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ui-dialog.component.html',
  styleUrl: './ui-dialog.component.scss'
})
export class UiDialog {
  /** Ancho deseado del diálogo; se recorta solo si no cabe en el viewport. */
  readonly width = input('560px');

  /** Altura máxima. `dvh` evita que la barra del navegador móvil tape el footer. */
  readonly maxHeight = input('85dvh');

  /** `between` para footers que separan una acción destructiva del resto. */
  readonly footerAlign = input<DialogFooterAlign>('end');

  /**
   * Hace el body enfocable para poder scrollearlo con el teclado.
   * Solo necesario en diálogos de solo lectura: si el body tiene campos de
   * formulario, el tabulado entre ellos ya produce el scroll.
   */
  readonly bodyFocusable = input(false);
}
