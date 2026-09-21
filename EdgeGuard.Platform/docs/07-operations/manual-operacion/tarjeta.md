## Kit de comandos

PowerShell en el servidor del Hub, como administrador.

| Qué contesta | Comando | Está bien si… |
|---|---|---|
| ¿Está bien el Hub? | `Invoke-RestMethod http://localhost/health/ready` | `status = Healthy` |
| ¿Está arriba el app pool? | `Get-WebAppPoolState -Name EdgeGuardHub` | `Started` |
| ¿Responde la base? | `Test-NetConnection -ComputerName localhost -Port 5432` | `TcpTestSucceeded : True` |
| ¿Escucha el receptor HL7? | `Get-NetTCPConnection -LocalPort 8001 -State Listen` | Devuelve al menos una línea |
| ¿Quién tiene el puerto 80? | `Get-Website \| Select-Object Name, State` | Solo `EdgeGuard.Hub` |
| ¿Hay espacio? | `Get-PSDrive C \| Select-Object Used, Free` | Más de 20 GB libres |
| ¿Están las llaves? | `Get-ChildItem C:\inetpub\edgeguard\dp-keys -Filter 'key-*.xml'` | Al menos un archivo |
| Reiniciar el Hub | `Restart-WebAppPool -Name EdgeGuardHub` | Corta de 10 a 30 s; no pierde nada |

**Los errores de hoy** — las horas están en UTC:

```powershell
$hoy = "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log"
Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.'@l' -in 'Error','Fatal','Warning' } |
    Select-Object -Last 20 @{n='Hora';e={$_.'@t'}}, @{n='Mensaje';e={$_.'@mt'}}
```

**Seguir una operación por su Correlation ID** — el identificador que el sistema
muestra en pantalla cuando hay un error:

```powershell
Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.CorrelationId -eq "<identificador>" } |
    Select-Object @{n='Hora';e={$_.'@t'}}, @{n='Mensaje';e={$_.'@mt'}}, @{n='Detalle';e={$_.'@x'}}
```

## Triage en cuatro preguntas

**"No están llegando los estudios."** Pregunte desde cuándo, si es un sitio o
varios, y si es un estudio o ninguno. Después:

1. **¿Falla en todos los sitios?** → revise el Hub. **¿En uno?** → revise ese nodo.
2. **¿El Hub responde `Healthy`?** No → el Hub está caído. Sí → siga.
3. **¿El nodo está en línea?** No → ese sitio no comunica. Sí → siga.
4. **¿El estudio aparece en el Hub?** No aparece → no llegó al nodo.
   En *Enviando* → el PACS no recibe. Completo → el problema está en el PACS o el visor.

**Anote dónde se detuvo el flujo.** Es lo más valioso que puede entregar al escalar.

## Escalamiento

| Prioridad | Cuándo |
|---|---|
| **P1** | La operación está detenida en todos los sitios |
| **P2** | Un sitio o una función detenida; el resto opera |
| **P3** | Molestia sin detener la operación |

¿Está detenida la operación de todos? P1. ¿De uno? P2. ¿De nadie? P3. Sube un
nivel si hay estudios urgentes esperando.

| Nivel | Se acude cuando |
|---|---|
| 1 · Soporte interno | Siempre primero; ejecuta el triage |
| 2 · Plataforma | El nivel 1 agotó el manual |
| 3 · Base de datos | Falla en `database`, restauraciones |
| 3 · Redes | Un sitio no comunica, puertos bloqueados |
| 3 · PACS | Estudios detenidos en *Enviando* |
| 3 · HIS/RIS | No llegan órdenes, mensajes rechazados |
| 4 · Proveedor | Descartado todo lo anterior |

Los contactos de cada nivel están en el procedimiento interno de soporte.

**Antes de llamar, reúna:** qué pasó y desde cuándo · a quién afecta · dónde se
detuvo el flujo · el Correlation ID · la respuesta de `health/ready` · qué se
intentó ya.

**No se escala:** una sesión expirada por inactividad · el corte de segundos tras
reiniciar el app pool · un nodo fuera de línea con su sitio apagado · un mensaje
HL7 rechazado por ser de un tipo no soportado.
