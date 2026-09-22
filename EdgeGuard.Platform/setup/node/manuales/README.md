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
| `Nodo-Operacion-diagnostico-y-soporte.pdf` | Revisión diaria, diagnóstico por síntoma, respaldos, diccionario y apéndices. El manual de consulta permanente. | `docs/07-operations/manual-nodo/0*.md` |
| `Nodo-Tarjeta-de-diagnostico-y-escalamiento.pdf` | Dos hojas para imprimir y pegar junto al servidor de la sede. Extracto del manual, sin contenido propio. | `docs/07-operations/manual-nodo/tarjeta.md` |

**Falta el manual de instalación.** Su fuente sería un runbook que describe un
procedimiento que todavía no existe: el instalador del nodo (`setup/node/`) está
en diseño. Documentar hoy el `install-service.ps1` actual sería escribir un
manual que hay que tirar en cuanto el instalador esté.

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
