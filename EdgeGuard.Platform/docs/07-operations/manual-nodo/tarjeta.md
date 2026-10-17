## Kit de comandos

PowerShell en el servidor de la sede, como administrador.

| Qué contesta | Comando | Está bien si… |
|---|---|---|
| ¿Está bien el nodo? | `Invoke-RestMethod http://localhost:5120/health/ready` | `status = Healthy` |
| ¿Está corriendo el servicio? | `Get-Service -Name EdgeGuardNode` | `Running` |
| ¿Recibe estudios? | `Get-NetTCPConnection -LocalPort 11112 -State Listen` | Devuelve al menos una línea |
| ¿Alcanza al Hub? | `Test-NetConnection -ComputerName <hub> -Port 80` | `TcpTestSucceeded : True` |
| ¿Hay espacio? | `Get-PSDrive C \| Select-Object Used, Free` | Más de 10 GB libres |
| ¿Cuánto ocupan los estudios? | `Get-ChildItem C:\ProgramData\EdgeGuard\Node\data -Recurse \| Measure-Object Length -Sum` | No crece día tras día |
| Reiniciar el nodo | `Restart-Service -Name EdgeGuardNode` | Corta de 10 a 30 s; la cola no se pierde |

**Los errores de hoy** — las horas están en UTC, seis más que la hora local:

```powershell
$hoy = "C:\ProgramData\EdgeGuard\Node\logs\node-$(Get-Date -Format yyyyMMdd).log"
Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.'@l' -in 'Error','Fatal','Warning' } |
    Select-Object -Last 20 @{n='Hora';e={$_.'@t'}}, @{n='Mensaje';e={$_.'@mt'}}
```

**Seguir un estudio** — con el `StudyInstanceUID` que muestra la consola del Hub:

```powershell
Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.StudyInstanceUid -eq "<identificador>" } |
    Select-Object @{n='Hora';e={$_.'@t'}}, @{n='Mensaje';e={$_.'@mt'}}
```

**Si el servicio no arranca**, el motivo está aquí:

```powershell
Get-Content C:\EdgeGuard\Node\logs\node-bootstrap-*.log -Tail 30
```

## Triage en cuatro preguntas

**"No están llegando los estudios."** Pregunte desde cuándo, de qué modalidad, y
si es uno o ninguno. Después:

1. **¿Falla en una modalidad o en todas?** Una → revise esa modalidad: dirección,
   puerto 11112 y nombre AE. Todas → siga.
2. **¿El servicio está `Running`?** No → arránquelo y lea el registro de arranque.
   Sí → siga.
3. **¿`health/ready` dice `Healthy`?** No → mire cuál de las tres revisiones falla.
   Sí → siga.
4. **¿El estudio aparece en la consola del Hub?** No aparece → no llegó al nodo:
   es la modalidad o la red de la sede. En *Enviando* → el PACS no lo recibe.
   Completado → el nodo ya hizo su trabajo; el problema está en el PACS o el visor.

**Anote dónde se detuvo el flujo.** Modalidad → nodo → PACS. Es lo más valioso
que puede entregar al escalar.

## Escalamiento

| Prioridad | Cuándo |
|---|---|
| **P1** | La sede no recibe estudios de ninguna modalidad |
| **P2** | Una modalidad o una función detenida; el resto opera |
| **P3** | Molestia sin detener la operación |

Sube un nivel si hay estudios urgentes esperando.

| Nivel | Se acude cuando |
|---|---|
| 1 · Soporte interno | Siempre primero; ejecuta el triage |
| 2 · Plataforma | El nivel 1 agotó el manual |
| 3 · Redes | El nodo no alcanza al Hub, o las modalidades no alcanzan al nodo |
| 3 · PACS | Estudios detenidos en *Enviando*, o `pacs` en `Unhealthy` |
| 3 · Modalidades | Una sola modalidad no envía y las demás sí |
| 4 · Proveedor | Descartado todo lo anterior |

Los contactos de cada nivel están en el procedimiento interno de soporte.

**Antes de llamar, reúna:** qué pasó y desde cuándo · a qué modalidades afecta ·
dónde se detuvo el flujo · la respuesta de `health/ready` · el StudyInstanceUID
de un estudio afectado · qué se intentó ya.

**No se escala:** un nodo fuera de línea con la sede cerrada · el corte de
segundos al reiniciar el servicio · un estudio en *Enviando* por unos minutos ·
que el nodo borre estudios que ya entregó al PACS, porque es su trabajo.
