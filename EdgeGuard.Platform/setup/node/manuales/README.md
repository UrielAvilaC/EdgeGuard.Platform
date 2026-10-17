# `manuales/` — material del cliente para el nodo

Los PDF de esta carpeta se entregan junto al instalador del nodo. **Son producto
generado**: se pueden borrar y rehacer en cualquier momento a partir del Markdown
del repositorio.

Van aparte de los del Hub (`setup/manuales/`) porque son otro paquete y otro
destinatario: el del Hub lo recibe quien administra el servidor central; este lo
recibe quien está a cargo del equipo de imagen en la sede, y no tiene por qué
saber de sistemas.

| Archivo | Qué es | Fuente |
|---|---|---|
| `Nodo-Instalacion-y-puesta-en-marcha.pdf` | Procedimiento de instalación, de la preparación del servidor a la verificación funcional. Se usa una sola vez y lo ejecuta personal técnico. | `docs/07-operations/runbook-instalacion-nodo.md` |
| `Nodo-Operacion-diagnostico-y-soporte.pdf` | Revisión diaria, diagnóstico por síntoma, respaldos, diccionario y apéndices. El manual de consulta permanente. | `docs/07-operations/manual-nodo/0*.md` |
| `Nodo-Tarjeta-de-diagnostico-y-escalamiento.pdf` | Dos hojas para imprimir y pegar junto al servidor de la sede. Extracto del manual, sin contenido propio. | `docs/07-operations/manual-nodo/tarjeta.md` |

**Dos destinatarios, dos registros.** El de instalación va dirigido a quien
despliega: asume consola elevada, habla de puertos y de rutas, y cierra con la
evidencia que recibe el aprobador del cambio. Los otros dos van dirigidos a quien
se queda a cargo del equipo en la sede, que no tiene por qué saber de sistemas.
Mezclarlos haría el primero inútil y el segundo ilegible.

> **Sin datos de contacto, por decisión.** Los dos documentos clasifican la falla
> y dicen a qué especialidad corresponde, pero no llevan teléfonos, horarios ni
> tiempos comprometidos de respuesta: eso cambia con el tiempo y vive en el
> procedimiento interno de soporte del cliente. Ambos lo dicen explícitamente,
> para que no se lea como un olvido.

## Regenerar

El generador es el mismo que el del Hub — un solo juego de plantillas para los
dos paquetes, de modo que no puedan divergir de aspecto:

```powershell
cd setup\manuales\build
.\Build-Manuales.ps1
```

Solo los del nodo:

```powershell
.\Build-Manuales.ps1 -Only operacion-nodo
.\Build-Manuales.ps1 -Only tarjeta-nodo
```

Conservando el HTML intermedio para revisar el maquetado en el navegador:

```powershell
.\Build-Manuales.ps1 -Only operacion-nodo -KeepHtml
```

**Qué hace falta en la máquina:** Node 22 o superior, y Microsoft Edge o Google
Chrome. Nada más. Los detalles del generador están en
`setup/manuales/README.md`.

## Qué revisar al editarlos

El manual está escrito para alguien **sin formación técnica**. Dos reglas que
conviene sostener al tocarlo:

**Cada apartado empieza por el síntoma, no por el componente.** El lector no sabe
qué es un SCP ni le interesa; sabe que el tomógrafo marca error al enviar.

**Todo comando va con su "está bien si…".** Un comando cuyo resultado el lector
no puede interpretar no le sirve de nada, y le hace creer que hizo una
verificación cuando no hizo ninguna.
