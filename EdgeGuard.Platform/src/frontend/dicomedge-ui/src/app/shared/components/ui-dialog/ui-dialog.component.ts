import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type DialogFooterAlign = 'end' | 'between';
export type DialogBodyLayout = 'scroll' | 'flex';

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

  /**
   * `scroll` (por defecto): el body entero scrollea.
   *
   * `flex` lo vuelve una columna flex para que el contenido decida qué parte
   * queda fija y qué parte scrollea — p. ej. metadata fija arriba y un visor
   * de payload que scrollea solo. En ese modo el elemento proyectado debe
   * llevar `flex-1 min-h-0 flex flex-col`, marcar como `shrink-0` lo que va
   * fijo, y dar al scroller un `min-h` para que no colapse a nada en
   * pantallas bajas.
   */
  readonly bodyLayout = input<DialogBodyLayout>('scroll');

  /** `between` para footers que separan una acción destructiva del resto. */
  readonly footerAlign = input<DialogFooterAlign>('end');

  /**
   * Hace el body enfocable para poder scrollearlo con el teclado.
   *
   * Solo aplica en modo `scroll`, y solo hace falta en diálogos de solo
   * lectura: si el body tiene campos de formulario, tabular entre ellos ya
   * produce el scroll. En modo `flex` el que scrollea es un hijo, así que el
   * `tabindex` va en ese hijo, no aquí.
   */
  readonly bodyFocusable = input(false);
}
