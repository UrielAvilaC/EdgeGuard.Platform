import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { UiDialog } from './ui-dialog.component';

/**
 * Guardarraíl del fix de scroll en diálogos.
 *
 * El bug original: los diálogos crecían sin límite, el overlay los centraba y
 * lo que sobresalía del viewport quedaba recortado sin barra de scroll, así que
 * los botones Cancelar/Guardar eran inalcanzables sin hacer zoom out.
 *
 * Estas pruebas revisan los fuentes de *todos* los diálogos, no solo los que
 * alguien recuerde mirar.
 */

// Vite reemplaza import.meta.glob en build time: tanto el patrón como las
// opciones tienen que ser literales en el sitio de la llamada.
// Los .scss de componente quedan fuera a propósito: el plugin de Angular los
// procesa aparte y devuelve vacío al pedirlos como texto, así que el layout
// que este spec verifica tiene que vivir en el markup.
type Fuentes = Record<string, string>;

const htmlFiles = import.meta.glob('/src/app/**/*-dialog.component.html', { query: '?raw', import: 'default', eager: true }) as Fuentes;
const tsFiles = import.meta.glob('/src/app/**/*-dialog.component.ts', { query: '?raw', import: 'default', eager: true }) as Fuentes;

/**
 * Diálogos que legítimamente no usan <ui-dialog>. Cualquier otro debe hacerlo.
 * Agregar algo aquí requiere justificarlo: no es una vía de escape.
 */
const SIN_SHELL = new Map<string, string>([
  [
    'ui-confirm-dialog',
    'Tarjeta de alerta centrada, sin barra de header; no encaja en los tres ' +
      'slots del shell. Acota su altura y hace scroll del mensaje por su cuenta.',
  ],
]);

interface DialogSource {
  /** Nombre del componente, p. ej. `template-form-dialog`. */
  name: string;
  /** Markup: el .html si existe, o el template inline del .ts. */
  markup: string;
}

function nameOf(path: string): string {
  return path.split('/').pop()!.replace(/\.component\.(ts|html)$/, '');
}

function collectDialogs(): DialogSource[] {
  return Object.entries(tsFiles)
    // El shell mismo no es un diálogo.
    .filter(([path]) => nameOf(path) !== 'ui-dialog')
    .map(([tsPath, ts]) => {
      let markup = htmlFiles[tsPath.replace(/\.ts$/, '.html')];
      if (markup === undefined) {
        const inline = ts.match(/template:\s*`([\s\S]*?)`,?\s*\n\}\)/);
        if (!inline) throw new Error(`Sin template ni .html: ${tsPath}`);
        markup = inline[1];
      }
      return { name: nameOf(tsPath), markup };
    });
}

const dialogs = collectDialogs();

describe('diálogos', () => {
  it('encuentra los diálogos del proyecto', () => {
    // Si esto baja de golpe, el recolector dejó de encontrarlos y el resto de
    // las pruebas estaría pasando en vacío.
    expect(dialogs.length).toBeGreaterThanOrEqual(15);
  });

  describe.each(dialogs.map(d => [d.name, d] as const))('%s', (_name, dialog) => {
    it('acota su altura y hace scroll del contenido', () => {
      const usaShell = dialog.markup.includes('<ui-dialog');
      const acotaAltura = /max-h-\[/.test(dialog.markup);
      const scrollea = /overflow-y-auto|overflow-auto/.test(dialog.markup);

      expect(
        usaShell || (acotaAltura && scrollea),
        'Un diálogo debe usar <ui-dialog>, o bien acotar su altura y dar scroll ' +
          'a su cuerpo. Si no, al superar la pantalla sus botones quedan fuera ' +
          'de alcance.',
      ).toBe(true);
    });

    it('usa el shell compartido <ui-dialog>', () => {
      if (SIN_SHELL.has(dialog.name)) {
        expect(SIN_SHELL.get(dialog.name)).toBeTruthy();
        return;
      }
      expect(
        dialog.markup.includes('<ui-dialog'),
        `${dialog.name} no usa <ui-dialog>. Los diálogos nuevos deben usarlo ` +
          'para heredar header/footer fijos y cuerpo scrollable.',
      ).toBe(true);
    });

    it('no deja <button> sin type dentro de un <form>', () => {
      // Dentro de un <form>, un <button> sin atributo type es submit por
      // defecto en HTML. Envolver el diálogo entero en el <form> metió los
      // botones de ícono del header y del cuerpo dentro de ese alcance: un
      // clic en "quitar variable" llegaba a guardar el registro completo.
      if (!dialog.markup.includes('<form')) return;

      const sinType = [...dialog.markup.matchAll(/<button[^>]*>/g)]
        .map(m => m[0])
        .filter(tag => !/stypes*=|[type]/.test(tag));

      expect(
        sinType,
        `${dialog.name} tiene <button> sin type dentro de un <form>: ` +
          'sería submit por defecto. Poné type="button" explícito.',
      ).toEqual([]);
    });

    it('deja los botones de envío dentro del <form>', () => {
      // Al partir el diálogo en tres secciones es fácil dejar el footer fuera
      // del <form>: los type="submit" dejan de disparar ngSubmit y guardar
      // falla en silencio. Un type="submit" sin <form> es igual de sospechoso:
      // no envía nada y el botón depende de un (clicked) que puede no estar.
      if (!dialog.markup.includes('type="submit"')) return;

      const apertura = dialog.markup.indexOf('<form');
      const submit = dialog.markup.indexOf('type="submit"');
      const cierre = dialog.markup.indexOf('</form>');

      expect(apertura, 'hay un type="submit" pero no hay <form>').toBeGreaterThanOrEqual(0);
      expect(submit).toBeGreaterThan(apertura);
      expect(submit).toBeLessThan(cierre);
    });
  });
});

describe('UiDialog', () => {
  @Component({
    imports: [UiDialog],
    template: `
      <ui-dialog width="480px">
        <div uiDialogHeader>Título</div>
        <div uiDialogBody>Contenido</div>
        <div uiDialogFooter>Acciones</div>
      </ui-dialog>
    `,
  })
  class Host {}

  function render() {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('proyecta las tres secciones', () => {
    const el = render();
    expect(el.querySelector('[uiDialogHeader]')?.textContent).toContain('Título');
    expect(el.querySelector('[uiDialogBody]')?.textContent).toContain('Contenido');
    expect(el.querySelector('[uiDialogFooter]')?.textContent).toContain('Acciones');
  });

  it('acota la altura y solo hace scroll el cuerpo', () => {
    const el = render();
    const raiz = el.querySelector('ui-dialog > div') as HTMLElement;
    expect(raiz.style.maxHeight).toBe('85dvh');
    expect(raiz.style.width).toBe('480px');

    const cuerpo = el.querySelector('[uiDialogBody]')!.parentElement!;
    // min-h-0 es lo que permite que overflow-y-auto llegue a activarse.
    expect(cuerpo.className).toContain('overflow-y-auto');
    expect(cuerpo.className).toContain('min-h-0');

    for (const fijo of ['[uiDialogHeader]', '[uiDialogFooter]']) {
      expect(el.querySelector(fijo)!.parentElement!.className).toContain('shrink-0');
    }
  });
});

describe('UiDialog footerAlign', () => {
  @Component({
    imports: [UiDialog],
    template: `
      <ui-dialog footerAlign="between">
        <div uiDialogHeader>H</div>
        <div uiDialogBody>B</div>
        <p uiDialogFooter>Izquierda</p>
        <div uiDialogFooter>Derecha</div>
      </ui-dialog>
    `,
  })
  class HostBetween {}

  it('separa los extremos y proyecta varios elementos al footer', () => {
    const fixture = TestBed.createComponent(HostBetween);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;

    const proyectados = el.querySelectorAll('[uiDialogFooter]');
    expect(proyectados.length).toBe(2);

    const footer = proyectados[0].parentElement!;
    expect(footer.className).toContain('justify-between');
    expect(footer.className).not.toContain('justify-end');
    // Ambos comparten contenedor: es el justify-between el que los separa.
    expect(proyectados[1].parentElement).toBe(footer);
  });
});

describe('UiDialog bodyLayout', () => {
  @Component({
    imports: [UiDialog],
    template: `
      <ui-dialog bodyLayout="flex">
        <div uiDialogHeader>H</div>
        <div uiDialogBody class="flex-1 min-h-0 flex flex-col">
          <div class="shrink-0">Fijo</div>
          <pre class="flex-1 min-h-[8rem] overflow-auto">Scrollea</pre>
        </div>
        <div uiDialogFooter>F</div>
      </ui-dialog>
    `,
  })
  class HostFlex {}

  it('vuelve el cuerpo una columna flex sin perder su overflow de respaldo', () => {
    const fixture = TestBed.createComponent(HostFlex);
    fixture.detectChanges();
    const cuerpo = (fixture.nativeElement as HTMLElement)
      .querySelector('[uiDialogBody]')!.parentElement!;

    expect(cuerpo.className).toContain('flex-col');
    // El overflow se mantiene: si los min-height de los hijos no caben, el
    // cuerpo scrollea en vez de recortarlos.
    expect(cuerpo.className).toContain('overflow-y-auto');
    expect(cuerpo.className).toContain('min-h-0');
  });

  it('no aplica la columna flex en el modo por defecto', () => {
    @Component({
      imports: [UiDialog],
      template: `
        <ui-dialog>
          <div uiDialogHeader>H</div>
          <div uiDialogBody>B</div>
          <div uiDialogFooter>F</div>
        </ui-dialog>
      `,
    })
    class HostScroll {}

    const fixture = TestBed.createComponent(HostScroll);
    fixture.detectChanges();
    const cuerpo = (fixture.nativeElement as HTMLElement)
      .querySelector('[uiDialogBody]')!.parentElement!;

    expect(cuerpo.className).not.toContain('flex-col');
  });
});

/**
 * El bug que motivó esto no estaba en los diálogos sino en un componente
 * compartido: `ui-icon-button` renderizaba un `<button>` sin `type`. Fuera de
 * un formulario da igual, pero al envolver los diálogos en `<form>` esos
 * botones pasaron a ser submit por defecto, y quitar una variable guardaba el
 * registro entero sin que nadie tocara Guardar.
 */
describe('botones de los componentes compartidos', () => {
  const plantillas = import.meta.glob('/src/app/shared/components/**/*.component.html', { query: '?raw', import: 'default', eager: true }) as Fuentes;

  const conBotones = Object.entries(plantillas)
    .map(([ruta, html]) => [ruta.split('/').pop()!, html] as const)
    .filter(([, html]) => html.includes('<button'));

  it('encuentra los componentes con botones', () => {
    expect(conBotones.length).toBeGreaterThanOrEqual(3);
  });

  it.each(conBotones)('%s declara el type de cada <button>', (_nombre, html) => {
    const sinType = [...html.matchAll(/<button\b[^>]*>/g)]
      .map(m => m[0])
      .filter(tag => !/\stype\s*=|\[type\]/.test(tag));

    expect(
      sinType.length,
      'Un <button> sin type es submit por defecto dentro de un <form>. ' +
        'Estos componentes se usan dentro de diálogos con formulario, así que ' +
        'el type tiene que ser explícito.',
    ).toBe(0);
  });
});
