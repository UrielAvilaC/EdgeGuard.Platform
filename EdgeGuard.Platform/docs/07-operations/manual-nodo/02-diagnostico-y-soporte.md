# Cómo usar esta parte

**Busque el síntoma que le reportaron, no el que usted supone.** Cada apartado
empieza por lo que se ve desde fuera y va estrechando.

Antes de empezar, dos reglas que ahorran horas:

**Anote dónde se detuvo el flujo.** El estudio hace un recorrido: modalidad →
nodo → PACS, y el nodo le cuenta al Hub cada paso. Saber en cuál de los tres
tramos se detuvo descarta dos tercios de las causas posibles.

**No reinicie antes de mirar.** Reiniciar borra las pistas y casi nunca arregla
nada. Mire primero el estado y el registro; reinicie cuando este manual se lo
indique.

---

# El nodo aparece fuera de línea

## Primero: ¿el servicio está corriendo?

En el servidor de la sede:

```powershell
Get-Service -Name EdgeGuardNode
```

| Lo que dice | Qué significa |
|---|---|
| `Running` | El servicio está arriba — pase al siguiente apartado |
| `Stopped` | Se detuvo. Siga aquí |
| Error de que no existe | El nodo no está instalado en este equipo, o tiene otro nombre |

### Si dice `Stopped`

Arránquelo:

```powershell
Start-Service -Name EdgeGuardNode
```

**Si arranca y se queda arriba**, revise el registro para ver por qué se había
caído — un servicio no se detiene solo.

**Si arranca y se vuelve a detener en segundos**, hay un problema de
configuración o de disco. Mire el registro de arranque:

```powershell
Get-Content C:\EdgeGuard\Node\logs\node-bootstrap-*.log -Tail 30
```

Tres mensajes que aparecen ahí y qué significan:

| Si dice algo como… | Es que… | Se arregla |
|---|---|---|
| `HubBaseUrl is required` | Al nodo no le dijeron a qué Hub hablarle | Escale: falta configuración del instalador |
| `La base de datos del nodo … está dentro de la raíz de almacenamiento DICOM` | Las carpetas de datos quedaron mal puestas | Escale: es un error de instalación |
| `unable to open database file` | El nodo no puede escribir su base | Revise espacio en disco y permisos de la carpeta |

## Segundo: el servicio corre pero el Hub no lo ve

Entonces el problema está en el camino entre el nodo y el Hub.

```powershell
Invoke-RestMethod http://localhost:5120/health/ready
```

**Si responde `Healthy`**, el nodo está sano y lo que falla es la red hacia el
Hub. Compruebe que el servidor alcance al Hub:

```powershell
Test-NetConnection -ComputerName <dirección-del-hub> -Port 80
```

`TcpTestSucceeded : True` significa que el camino está abierto y el problema es
otro — escale a plataforma. `False` significa que la red bloquea el paso:
**escale a redes.**

**Si no responde nada**, el servicio está arrancado pero no terminó de iniciar.
Espere un minuto y repita. Si sigue igual, revise el registro del día.

> **Antes de escalar, distinga una sede apagada de una falla.** Un nodo fuera de
> línea a las 9 de la noche con la sede cerrada es lo normal. Lo que se escala es
> un nodo fuera de línea **en horario de trabajo**.

---

# No están llegando los estudios

## ¿Es una modalidad o todas?

Esta pregunta parte el problema en dos y vale más que cualquier comando.

**Si es una sola modalidad** → el problema está en esa modalidad o en su
configuración. El nodo está bien; los demás equipos sí envían.

**Si son todas** → el problema está en el nodo o en la red de la sede.

## Si es una sola modalidad

Revise en el propio equipo:

1. ¿Tiene bien la dirección del nodo y el puerto **11112**?
2. ¿Tiene bien el nombre AE del nodo? Un nombre distinto al configurado hace que
   el nodo rechace la conexión.
3. ¿El equipo reporta algún error al enviar? Anótelo textualmente.

Desde el servidor, confirme que el nodo está escuchando:

```powershell
Get-NetTCPConnection -LocalPort 11112 -State Listen
```

Si no devuelve nada, **el nodo no está recibiendo**: vaya al apartado anterior.

## Si son todas

Confirme que el puerto está abierto y que el servicio responde, con los dos
comandos de arriba. Si el nodo escucha y responde `Healthy` pero ninguna
modalidad logra enviar, lo más probable es el **firewall del servidor** o un
cambio de red en la sede. **Escale a redes**, indicando que el puerto 11112 del
servidor no es alcanzable desde las modalidades.

---

# Los estudios llegan pero no salen al PACS

El síntoma es claro en la consola del Hub: los estudios se quedan en **Enviando**
y no pasan a **Completado**. Suele venir acompañado del disco llenándose.

## Primero: ¿el PACS contesta?

```powershell
Invoke-RestMethod http://localhost:5120/health/ready
```

Mire la revisión `pacs`:

| Dice | Qué significa | Qué hacer |
|---|---|---|
| `Healthy` | El PACS contesta. El problema es otro | Siga abajo |
| `Degraded` con *"PACS host is not configured"* | Nadie le dijo al nodo a qué PACS enviar | Configúrelo desde la consola del Hub |
| `Unhealthy` | El PACS no contesta | **Escale a PACS** |

El nodo comprueba el PACS **cada dos minutos**, así que este dato es fresco.

## Si el PACS contesta pero los estudios no avanzan

El PACS está vivo pero rechaza lo que el nodo le manda. Las causas habituales:

- El PACS **no reconoce el nombre AE** del nodo. Es la más común después de un
  cambio de configuración de cualquiera de los dos lados.
- El PACS **está lleno** o en mantenimiento.
- El PACS **rechaza el tipo de estudio** que se le manda.

Ninguna de las tres se arregla desde el nodo. **Escale a PACS**, y lleve el
registro del nodo: ahí está el error exacto que devolvió el PACS.

Para sacarlo:

```powershell
$hoy = "C:\ProgramData\EdgeGuard\Node\logs\node-$(Get-Date -Format yyyyMMdd).log"
Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.'@mt' -like '*PACS*' -or $_.'@mt' -like '*C-STORE*' } |
    Select-Object -Last 20 @{n='Hora';e={$_.'@t'}}, @{n='Mensaje';e={$_.'@mt'}}
```

> El nodo **reintenta solo**, con esperas cada vez más largas, hasta cinco veces.
> Un estudio en *Enviando* durante unos minutos está en ese ciclo normal. Lo que
> no es normal es que siga ahí al día siguiente.

---

# El disco se está llenando

Casi siempre es **consecuencia**, no causa: el disco se llena porque los estudios
no salen. Antes de hacer espacio, revise el apartado anterior — si no, en unas
horas estará igual.

## Cuánto espacio hay y cuánto ocupa el nodo

```powershell
Get-PSDrive C | Select-Object Used, Free
Get-ChildItem C:\ProgramData\EdgeGuard\Node\data -Recurse |
    Measure-Object -Property Length -Sum
```

| Libre | Qué hacer |
|---|---|
| Más de 10 GB | Nada |
| Entre 5 y 10 GB | Revise hoy por qué creció |
| Entre 2 y 5 GB | Atienda hoy. El nodo ya está borrando antes de tiempo |
| Menos de 2 GB | Atienda ahora. Se va a quedar sin recibir |

## Qué NO hacer

**No borre a mano archivos de `data\`.** El nodo lleva su propio registro de qué
estudios tiene; borrar por fuera lo deja creyendo que siguen ahí, y los reporta
al Hub como presentes cuando ya no existen.

**No borre nada de `persistence\`.** Es la base de datos del nodo: ahí está el
historial de qué se entregó y qué no.

Si necesita espacio de inmediato, lo correcto es **bajar el plazo de retención
desde la consola del Hub** y dejar que la limpieza del nodo haga el trabajo.

---

# Leer el registro

El nodo escribe un archivo por día en:

```
C:\ProgramData\EdgeGuard\Node\logs\node-AAAAMMDD.log
```

Está en un formato que se lee mejor con comandos que a ojo. **Las horas están en
UTC**, no en hora local: en México son seis horas más que la hora local.

**Los errores y avisos de hoy:**

```powershell
$hoy = "C:\ProgramData\EdgeGuard\Node\logs\node-$(Get-Date -Format yyyyMMdd).log"
Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.'@l' -in 'Error','Fatal','Warning' } |
    Select-Object -Last 20 @{n='Hora';e={$_.'@t'}}, @{n='Mensaje';e={$_.'@mt'}}
```

**Todo lo de un estudio, por su identificador** — el `StudyInstanceUID` que
aparece en la consola del Hub:

```powershell
Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.StudyInstanceUid -eq "<identificador>" } |
    Select-Object @{n='Hora';e={$_.'@t'}}, @{n='Mensaje';e={$_.'@mt'}}
```

Esto último es lo más útil que puede llevar a soporte: el recorrido completo de
un estudio concreto, con horas.

---

# Escalamiento

## Prioridad

| Prioridad | Cuándo |
|---|---|
| **P1** | La sede no puede trabajar: no se reciben estudios de ninguna modalidad |
| **P2** | Una modalidad o una función detenida; el resto opera |
| **P3** | Molestia sin detener la operación |

Suba un nivel si hay **estudios urgentes** esperando.

## A quién

| Nivel | Se acude cuando |
|---|---|
| 1 · Soporte interno | Siempre primero; ejecuta la revisión de este manual |
| 2 · Plataforma | El nivel 1 agotó el manual |
| 3 · Redes | El nodo no alcanza al Hub, o las modalidades no alcanzan al nodo |
| 3 · PACS | Estudios detenidos en *Enviando*, o `pacs` en `Unhealthy` |
| 3 · Modalidades | Una sola modalidad no envía y las demás sí |
| 4 · Proveedor | Descartado todo lo anterior |

Los contactos de cada nivel están en el procedimiento interno de soporte de su
institución; este manual no los lleva a propósito, porque cambian con el tiempo.

## Qué reunir antes de llamar

1. **Qué pasó y desde cuándo.**
2. **A quién afecta**: una modalidad, toda la sede.
3. **Dónde se detuvo el flujo**: modalidad → nodo → PACS.
4. La respuesta completa de `health/ready`.
5. El `StudyInstanceUID` de un estudio afectado, y su recorrido en el registro.
6. **Qué se intentó ya.**

## Lo que no se escala

- Un nodo fuera de línea con la sede cerrada.
- El corte de 10 a 30 segundos tras reiniciar el servicio.
- Un estudio en *Enviando* durante unos minutos.
- Que el nodo borre estudios ya entregados al PACS: es su trabajo.
