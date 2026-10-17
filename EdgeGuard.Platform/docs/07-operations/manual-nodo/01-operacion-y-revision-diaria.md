# Qué es el nodo y qué hace

El **nodo EdgeGuard** es un programa que corre solo, sin ventana, en un equipo de
su sede. Se instala una vez y desde entonces trabaja sin que nadie lo abra.

Hace tres cosas, en este orden:

1. **Recibe** los estudios que le envían las modalidades — el tomógrafo, el
   equipo de rayos X, el ultrasonido.
2. **Los guarda** en el disco del equipo mientras los procesa.
3. **Los reenvía** al PACS, y le informa al Hub qué pasó con cada uno.

Todo lo demás —las reglas de a qué PACS va cada estudio, cuántos días se
conservan, con qué nombre se identifica el nodo— se configura **desde la consola
del Hub**, no en el equipo. Eso es deliberado: significa que casi nunca hay que
tocar el servidor de la sede.

> **El nodo no es el PACS.** El nodo recibe, guarda un momento y reenvía. Quien
> conserva los estudios para siempre es el PACS. Si un estudio ya llegó al PACS,
> el nodo puede borrar su copia sin que se pierda nada — y lo hace, porque su
> disco es de paso, no de archivo.

## Cómo se llama cada cosa

| Lo que usted ve | Cómo se llama aquí |
|---|---|
| El equipo donde está instalado el nodo | El **servidor de la sede** |
| El programa que corre sin ventana | El **servicio** `EdgeGuardNode` |
| El tomógrafo, el rayos X, el ultrasonido | Las **modalidades** |
| El sistema donde los médicos ven los estudios | El **PACS** |
| La consola web donde se ve todo | El **Hub** |

---

# La revisión diaria

Cinco minutos, una vez al día, de preferencia a la misma hora. **La mayoría de
los días no encontrará nada** — ese es el punto: el día que encuentre algo, lo
va a encontrar temprano.

## Paso 1 — Mire el nodo en la consola del Hub

Entre a la consola del Hub y busque su sede en la lista de nodos.

| Lo que ve | Qué significa | Qué hacer |
|---|---|---|
| **En línea** | El nodo está trabajando | Nada |
| **Fuera de línea** | El Hub no ha sabido del nodo en varios minutos | Vaya al paso 2 |
| **Degradado** | El nodo responde pero algo no está bien | Vaya al paso 2 |

El nodo le avisa al Hub que sigue vivo **cada minuto**. Si pasan varios minutos
sin aviso, el Hub lo marca fuera de línea. Un parpadeo de un minuto no es nada;
diez minutos sí lo son.

> **Fuera de línea no siempre significa apagado.** Si la sede cerró y apagaron el
> equipo, es lo esperado. Lo que importa es que esté en línea **cuando la sede
> está trabajando**.

## Paso 2 — Pregúntele al nodo cómo está

En el servidor de la sede, abra **PowerShell** y escriba:

```powershell
Invoke-RestMethod http://localhost:5120/health/ready
```

La respuesta trae un `status` general y tres revisiones:

| Revisión | Qué mira | Está bien si… |
|---|---|---|
| `storage` | Espacio libre en el disco donde se guardan los estudios | `Healthy` |
| `database` | La base de datos interna del nodo | `Healthy` |
| `pacs` | Que el PACS conteste | `Healthy` |

**`status` general:**

- **`Healthy`** — todo bien.
- **`Degraded`** — el nodo funciona, pero algo se está degradando. Se atiende
  hoy, no ahora mismo. Mire cuál de las tres revisiones no está `Healthy`.
- **`Unhealthy`** — algo está roto. Vaya al manual de diagnóstico.

> Si el comando no devuelve nada y se queda esperando, **el servicio no está
> corriendo**. Eso se atiende de inmediato: vaya a *"El nodo está fuera de
> línea"* en el manual de diagnóstico.

## Paso 3 — Revise el espacio en disco

```powershell
Get-PSDrive C | Select-Object Used, Free
```

El nodo borra solo los estudios que ya entregó al PACS, así que el disco no
debería crecer sin parar. **Si crece día tras día, es señal de que los estudios
no están saliendo** — el nodo los retiene porque todavía no los entregó.

Con menos de **5 GB libres**, atiéndalo hoy. Con menos de **2 GB**, atiéndalo
ahora: el nodo empieza a borrar estudios antes de tiempo para hacer espacio.

## Paso 4 — Confirme que los estudios del día llegaron

En la consola del Hub, filtre por su sede y por la fecha de hoy. Compare contra
lo que sabe que se hizo: si se tomaron doce estudios y en la lista hay nueve,
ahí hay algo que revisar aunque todos los indicadores estén en verde.

**Los tres estados que importan:**

| Estado | Qué pasó |
|---|---|
| **Recibido** | El nodo lo tiene, todavía no lo manda |
| **Enviando** | Va camino al PACS |
| **Completado** | El PACS lo recibió |

Un estudio en *Enviando* por unos minutos es normal. **Varios estudios atorados
en *Enviando* durante horas no lo es**: el PACS no los está recibiendo.

---

# Lo que hace el nodo sin que usted lo pida

Para que no le sorprenda verlo en los registros.

| Cada | El nodo hace |
|---|---|
| 1 minuto | Le avisa al Hub que sigue vivo |
| 2 minutos | Comprueba que el PACS conteste |
| 5 minutos | Le reporta al Hub cuánto espacio le queda |
| 15 minutos | Le pregunta al Hub si cambió su configuración |
| 1 hora | Hace limpieza: borra lo que ya cumplió su tiempo |

**La limpieza** borra, de forma predeterminada:

- Estudios **ya entregados al PACS**, después de 7 días.
- Estudios **que fallaron**, después de 90 días — el plazo largo es a propósito:
  son los que alguien puede necesitar revisar.
- Cualquier estudio, después de 30 días.

Estos plazos se cambian desde la consola del Hub, no en el servidor.

> **Cuando el disco se llena, el nodo no espera a la limpieza de la hora.** Borra
> de inmediato lo más viejo que ya entregó. Prefiere perder su copia de paso
> antes que quedarse sin espacio y dejar de recibir estudios nuevos. Si ve
> borrados masivos en el registro, la causa es el disco lleno, no una falla.

---

# Cuándo apagar y encender el nodo

Reiniciar el servicio corta la recepción de **10 a 30 segundos**. Es seguro: los
estudios que ya recibió están en disco y la cola de envío sobrevive al reinicio.

```powershell
Restart-Service -Name EdgeGuardNode
```

Lo que sí se pierde es **un estudio que esté llegando en ese momento**: la
modalidad reportará un error de envío y habrá que reenviarlo desde el equipo.

**Reinicie cuando:** el manual de diagnóstico se lo indique, o soporte se lo
pida. **No reinicie** como primer reflejo ante cualquier síntoma: casi nunca es
la solución, y borra las pistas de lo que estaba pasando.

**El mejor momento** es fuera del horario de estudios. Si tiene que hacerlo en
horario, avise a los técnicos de las modalidades para que no envíen durante ese
minuto.

---

# Los respaldos

El nodo guarda dos cosas que vale la pena respaldar:

| Qué | Dónde | Con qué frecuencia |
|---|---|---|
| **La base de datos del nodo** | `C:\ProgramData\EdgeGuard\Node\persistence\` | Diaria |
| **La configuración** | `C:\EdgeGuard\Node\appsettings.Production.json` | Cuando cambie |

**Los estudios en `data\` no se respaldan.** Son copias de paso: el original
está en el PACS, que es quien tiene el respaldo que importa. Respaldarlos
duplicaría mucho espacio sin agregar seguridad.

Para respaldar la base con el servicio detenido —la forma simple y segura:

```powershell
Stop-Service -Name EdgeGuardNode
Copy-Item "C:\ProgramData\EdgeGuard\Node\persistence\edge-node.db" `
          "D:\Respaldos\edge-node-$(Get-Date -Format yyyyMMdd).db"
Start-Service -Name EdgeGuardNode
```

> **Si se pierde la base de datos**, el nodo vuelve a registrarse con el Hub y
> sigue funcionando, pero pierde el historial de qué estudios ya entregó. Los
> estudios en sí no se pierden: están en el PACS.
