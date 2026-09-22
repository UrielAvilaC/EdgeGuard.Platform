# Diccionario

Los términos que aparecen en la consola del Hub, en este manual y en lo que le
diga soporte. En orden alfabético.

**AE Title (nombre AE)** — El nombre con el que un equipo se identifica al hablar
DICOM. El nodo tiene el suyo, el PACS tiene el suyo, y cada modalidad el suyo.
Máximo 16 caracteres. **Si dos equipos no tienen bien el nombre AE del otro, no
se hablan** — y es la causa más común de que un envío falle después de un cambio
de configuración.

**C-ECHO** — El "¿me escuchas?" de DICOM. No manda ningún estudio: solo
comprueba que el otro equipo conteste. El nodo se lo manda al PACS cada dos
minutos, y es lo que alimenta la revisión `pacs`.

**C-STORE** — El envío de un estudio de un equipo a otro. Las modalidades le
hacen C-STORE al nodo, y el nodo le hace C-STORE al PACS.

**Cola** — La lista de estudios que el nodo recibió y todavía no entregó al PACS.
Sobrevive a un reinicio del servicio: nada se pierde por reiniciar.

**Degradado** — Estado de algo que funciona pero no del todo bien. Se atiende
hoy, no ahora mismo.

**DICOM** — El idioma común de los equipos médicos de imagen. Define tanto el
formato de los estudios como la forma de enviarlos.

**Estudio** — El conjunto de imágenes de un paciente en una sesión. Es la unidad
que el nodo recibe, guarda y reenvía.

**Hub** — El servidor central y su consola web. Ahí se configura cada nodo y se
ve el estado de todas las sedes.

**Limpieza** — El borrado automático de estudios que ya cumplieron su plazo. El
nodo la corre cada hora.

**Modalidad** — Un equipo que produce estudios: tomógrafo, rayos X, ultrasonido,
resonancia.

**Nodo** — El programa instalado en su sede. Recibe de las modalidades y reenvía
al PACS.

**PACS** — El sistema donde los estudios se guardan para siempre y donde los
médicos los consultan. El destino final.

**Puerto** — El número por el que un equipo escucha. El nodo usa el **11112**
para DICOM y el **5120** para su revisión de estado.

**Servicio** — Un programa de Windows que corre sin ventana y arranca solo con el
equipo. El del nodo se llama `EdgeGuardNode`.

**StudyInstanceUID** — El identificador único de un estudio: una cadena larga de
números y puntos. Es lo que permite seguir un estudio concreto en el registro.

**UTC** — La hora de referencia mundial, la que usa el registro del nodo. **En
México, la hora local es seis horas menos que UTC** (cinco en horario de verano).

---

# Apéndice A — Dónde está cada cosa

| Qué | Dónde |
|---|---|
| El programa | `C:\EdgeGuard\Node\` |
| La base de datos del nodo | `C:\ProgramData\EdgeGuard\Node\persistence\` |
| Los estudios en tránsito | `C:\ProgramData\EdgeGuard\Node\data\` |
| El registro diario | `C:\ProgramData\EdgeGuard\Node\logs\` |
| La configuración | `C:\EdgeGuard\Node\appsettings.Production.json` |

> **La base de datos y los estudios están en carpetas separadas a propósito**, y
> el nodo se niega a arrancar si alguien las junta. La razón: para decidir cuándo
> hacer espacio, el nodo mide lo que ocupa la carpeta de estudios. Si su propia
> base estuviera ahí dentro, la contaría como estudios y borraría estudios de
> verdad para aliviar un peso que no puede quitar.

> **No edite la configuración a mano.** La reescribe el instalador cada vez que
> se actualiza el nodo. Lo que se edita es el archivo de instalación, y de eso se
> encarga quien instala.

---

# Apéndice B — Qué revisa `health/ready`

```powershell
Invoke-RestMethod http://localhost:5120/health/ready
```

| Revisión | Qué mira | `Degraded` cuando | `Unhealthy` cuando |
|---|---|---|---|
| `storage` | Espacio libre del disco de estudios | Queda menos del mínimo configurado | No puede leer el disco |
| `database` | La base de datos del nodo | — | No abre o no responde |
| `pacs` | Que el PACS conteste al C-ECHO | No hay PACS configurado | El PACS no contesta |

El `status` general es el peor de los tres: si uno está `Unhealthy`, el conjunto
es `Unhealthy`.

Hay también una revisión más simple, que solo dice si el programa está vivo:

```powershell
Invoke-RestMethod http://localhost:5120/health/live
```

Sirve para distinguir *"el nodo no arrancó"* de *"el nodo arrancó pero algo no
está bien"*.

---

# Apéndice C — Los plazos que el nodo respeta

Todos se configuran desde la consola del Hub. Estos son los valores con los que
se entrega:

| Qué | Plazo |
|---|---|
| Estudios ya entregados al PACS | 7 días |
| Estudios que fallaron | 90 días |
| Cualquier estudio | 30 días |
| Límite de espacio antes de limpiar de urgencia | 100 GB |

**El plazo de los fallidos es el más largo a propósito**: son los que alguien
puede necesitar revisar, y borrarlos pronto quitaría la evidencia justo del caso
que hay que investigar.

---

# Apéndice D — Lo que este manual no cubre

**Instalar o reinstalar el nodo.** Es un procedimiento aparte, con su propio
manual, y lo ejecuta personal técnico.

**Configurar las reglas de ruteo** — a qué PACS va cada estudio, según la
modalidad o el equipo de origen. Se hace desde la consola del Hub y está en el
manual del Hub.

**Dar de alta modalidades nuevas.** Requiere tocar la configuración del nodo
desde el Hub y la del equipo nuevo. Pídalo a soporte.

**Cualquier cosa que pase dentro del PACS.** El nodo entrega y registra si la
entrega fue aceptada; lo que el PACS haga después no lo ve ni lo controla.
