# Manuales del cliente

Los PDF de esta carpeta son el material que se entrega al cliente junto con el
instalador. **Son producto generado**: se pueden borrar y rehacer en cualquier
momento a partir del Markdown del repositorio.

| Archivo | Qué es | Fuente |
|---|---|---|
| `Instalacion-y-puesta-en-marcha.pdf` | Procedimiento de instalación, de la preparación del servidor a la verificación funcional. Se usa una sola vez. | `docs/07-operations/runbook-instalacion-hub.md` |
| `Operacion-diagnostico-y-soporte.pdf` | Revisión diaria, mantenimiento, respaldos, triage, lectura de registros, diccionario y apéndices. El manual de consulta permanente. | `docs/07-operations/manual-operacion/0*.md` |
| `Tarjeta-de-diagnostico-y-escalamiento.pdf` | Dos hojas para imprimir y pegar junto al servidor. Extracto del manual de operación, sin contenido propio. | `docs/07-operations/manual-operacion/tarjeta.md` |

> **Sin datos de contacto, por decisión.** El apéndice B y la tarjeta clasifican
> la falla y dicen a qué especialidad corresponde, pero no llevan teléfonos,
> horarios ni tiempos comprometidos de respuesta: eso cambia con el tiempo y vive
> en el procedimiento interno de soporte del cliente. Los dos documentos lo dicen
> explícitamente, para que no se lea como un olvido.

**Un manual puede venir de varios archivos.** La clave `Source` del manifiesto
acepta una lista y los concatena en ese orden: los manuales largos se escriben
por capítulos y se arman al generar.

## Regenerar

```powershell
cd setup\manuales\build
.\Build-Manuales.ps1
```

Un solo manual, conservando el HTML intermedio para revisar el maquetado en el
navegador:

```powershell
.\Build-Manuales.ps1 -Only instalacion -KeepHtml
```

**Qué hace falta en la máquina:** Node 22 o superior, y Microsoft Edge o Google
Chrome. Nada más — no hay paquetes que instalar ni red que consultar. El
Markdown se convierte a HTML con `Convert-Markdown.ps1` y el HTML se imprime con
el motor del navegador.

## Cómo está armado

```
manuales\
  *.pdf                      Los entregables
  build\
    Build-Manuales.ps1       Manifiesto y orquestación
    Convert-Markdown.ps1     Markdown → HTML
    plantilla.html           Portada, índice y hoja de estilo de impresión
    plantilla-tarjeta.html   Variante compacta, sin portada ni índice
    print-pdf.mjs            HTML → PDF por el protocolo DevTools
```

`build\` **no forma parte del paquete que recibe el cliente**: se entregan los
PDF, no las herramientas que los producen.

### Decisiones que conviene conocer antes de tocar esto

**El conversor de Markdown es propio y deliberadamente parcial.** Cubre lo que
usan los manuales —encabezados, tablas, citas, listas, casillas, bloques de
código, reglas y formato en línea— y nada más. Un conversor completo significaba
una dependencia, y el paquete tiene que poder regenerarse en el servidor del
cliente sin instalar nada.

**Los identificadores de los encabezados siguen la regla de GitHub.** Los
índices y las referencias cruzadas de los documentos ya están escritos con esas
anclas y siguen funcionando dentro del PDF.

**El índice del PDF se genera; el que traiga el Markdown se retira.** La clave
`SkipSections` del manifiesto es la que lo hace. Dos índices en un documento
terminan siempre igual: uno de los dos se queda atrás.

**No se usa `--print-to-pdf`.** Esa bandera imprime el encabezado y el pie que
Chromium trae de fábrica: la ruta `file:///C:\...` del archivo temporal y la
fecha del equipo. Por el protocolo DevTools sí se controlan las plantillas, y de
ahí sale el pie con el nombre del manual y la numeración.

**La portada lleva pie de página.** Chromium no sabe excluir la primera hoja del
encabezado y el pie. Se optó por prescindir del encabezado —que en la portada se
veía como un defecto de plantilla— y dejar solo el pie, que en una portada
numerada no desentona.

**El PDF sí lleva marcadores.** `generateDocumentOutline` construye el panel de
marcadores del lector a partir de los encabezados del documento, y de paso el
PDF sale etiquetado. Sumado al índice hipervinculado de las primeras páginas y a
la búsqueda de texto, un manual largo se navega sin depender del número de
página.
