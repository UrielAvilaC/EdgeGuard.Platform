## Apéndice A — Kit de comandos

Todos se ejecutan en PowerShell, **en el servidor del Hub**, con la consola
abierta como administrador. Están agrupados por lo que contestan, no por lo que
hacen.

Si solo va a memorizar uno, que sea el primero.

### A.1 ¿Está bien el Hub?

```powershell
Invoke-RestMethod http://localhost/health/ready
```

**Responde bien:** `status = Healthy`.
**Responde mal:** `Degraded` o `Unhealthy` — lea el bloque `checks`.
**No responde nada:** el Hub está caído → apartado 4.2.

```powershell
Import-Module WebAdministration
Get-WebAppPoolState -Name EdgeGuardHub
```

**Responde bien:** `Started`.
**Responde mal:** `Stopped` — IIS lo apagó tras fallos repetidos de arranque.

### A.2 ¿Está bien la base de datos?

```powershell
Test-NetConnection -ComputerName localhost -Port 5432
```

**Responde bien:** `TcpTestSucceeded : True`. Cambie el nombre del equipo si la
base está en otro servidor.

### A.3 ¿Está escuchando el receptor de HL7?

```powershell
Get-NetTCPConnection -LocalPort 8001 -State Listen
```

**Responde bien:** una o más líneas en estado `Listen`.
**Responde mal:** nada — el receptor no está activo → apartado 4.6.

### A.4 ¿Quién tiene el puerto 80?

```powershell
Get-Website | Select-Object Name, State, @{n='Enlaces';e={$_.bindings.Collection.bindingInformation}}
```

**Responde bien:** `EdgeGuard.Hub` iniciado con `*:80:`, y ningún otro sitio en
ese puerto.
**Responde mal:** el Default Web Site presente en `*:80:` → apartado 1.4.

### A.5 ¿Hay espacio y están sanos los registros?

```powershell
Get-PSDrive C | Select-Object Used, Free
Get-ChildItem C:\inetpub\EdgeGuard\Hub\logs | Measure-Object Length -Sum
```

**Responde bien:** más de 20 GB libres y un directorio de registros del orden de
cientos de MB.

### A.6 ¿Están las llaves?

```powershell
Get-ChildItem C:\inetpub\edgeguard\dp-keys -Filter 'key-*.xml'
```

**Responde bien:** al menos un archivo.
**Responde mal:** vacío — no registre nodos nuevos y consulte al proveedor
(apartado 2.4).

### A.7 Los últimos errores del día

```powershell
$hoy = "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log"

Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.'@l' -in 'Error','Fatal','Warning' } |
    Select-Object -Last 20 @{n='Hora';e={$_.'@t'}}, @{n='Nivel';e={$_.'@l'}}, @{n='Mensaje';e={$_.'@mt'}}
```

**Recuerde:** las horas están en UTC (apartado 5.2).

### A.8 Seguir una operación por su Correlation ID

```powershell
$id  = "<pegue aquí el identificador>"
$hoy = "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log"

Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.CorrelationId -eq $id } |
    Select-Object @{n='Hora';e={$_.'@t'}}, @{n='Nivel';e={$_.'@l'}}, @{n='Mensaje';e={$_.'@mt'}}, @{n='Detalle';e={$_.'@x'}}
```

### A.9 Reiniciar el Hub

```powershell
Restart-WebAppPool -Name EdgeGuardHub
```

**Interrumpe:** de 10 a 30 segundos de servicio web y de recepción HL7.
**No pierde:** llaves, configuración, sesiones abiertas ni estudios en curso.

### A.10 Preparar el paquete para escalar

```powershell
$hoy = "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log"

Get-Content $hoy | ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object { $_.'@l' -in 'Error','Fatal','Warning' } |
    Select-Object @{n='Hora';e={$_.'@t'}}, @{n='Nivel';e={$_.'@l'}},
                  @{n='Mensaje';e={$_.'@mt'}}, @{n='Detalle';e={$_.'@x'}},
                  CorrelationId |
    Export-Csv "$env:USERPROFILE\Desktop\eventos-hub-$(Get-Date -Format yyyyMMdd).csv" `
               -NoTypeInformation -Encoding UTF8
```

Deja el archivo en el escritorio. Revíselo antes de enviarlo.

---

## Apéndice B — Clasificación y escalamiento

> **Este apéndice no incluye el directorio de contactos, los horarios de
> atención ni los tiempos comprometidos de respuesta.** No es un olvido: esos
> datos cambian con el tiempo y son propios de su organización, así que viven en
> su procedimiento interno de soporte, donde se mantienen actualizados. Un
> manual impreso con teléfonos desactualizados dirige llamadas a la nada.
>
> Lo que sí está aquí es lo que no cambia: cómo se clasifica una falla, a qué
> especialidad corresponde y qué hay que reunir antes de escalarla.

### B.1 Prioridades

| Prioridad | Definición operativa | Ejemplos |
|---|---|---|
| **P1 — Crítica** | La operación de imagenología está detenida. Ningún sitio puede trabajar, o se están perdiendo estudios | El Hub no responde. La base de datos caída. Todos los nodos fuera de línea |
| **P2 — Alta** | Un sitio o una función está detenida; el resto opera | Un nodo fuera de línea. El PACS no recibe. No llegan las órdenes del HIS |
| **P3 — Normal** | Molestia o degradación, sin detener la operación | La pantalla no se actualiza sola. Una modalidad de varias no envía. Espacio en disco a la baja |

**Cómo decidir en 10 segundos:** ¿está detenida la operación de todos? P1. ¿De
uno? P2. ¿De nadie? P3.

**Lo que sube una prioridad un nivel:** que haya estudios urgentes esperando.
Acuérdelo con el área de imagenología antes de que ocurra, no durante.

### B.2 A qué especialidad corresponde

El orden importa: un escalamiento que salta el nivel 1 llega sin triage, y casi
siempre regresa pidiendo justo la información que el nivel 1 habría reunido.

| Nivel | Cuándo se acude | Qué se le pide |
|---|---|---|
| **1 — Soporte interno de TI** | Siempre primero | Ejecutar el triage del apartado 4.1 y agotar este manual |
| **2 — Responsable de la plataforma** | El nivel 1 agotó el manual | Decidir el siguiente paso y coordinar a los demás |
| **3 — Base de datos** | La salud reporta fallo en `database`; restauraciones | Disponibilidad del servidor PostgreSQL, permisos, respaldos |
| **3 — Redes** | Un sitio no comunica; puertos bloqueados | Conectividad entre sitios y Hub, firewall, puertos 80, 8001 y 5432 |
| **3 — PACS** | Estudios detenidos en *Enviando*; el médico no ve el estudio | Recepción del PACS y autorización del Hub como emisor |
| **3 — HIS/RIS** | No llegan órdenes; mensajes rechazados | Envío, formato y contenido de los mensajes HL7 |
| **4 — Proveedor de la plataforma** | Descartado todo lo anterior, o defecto del producto | Diagnóstico de la aplicación |

Los datos de contacto de cada nivel están en el procedimiento interno de soporte
de su organización.

### B.3 Antes de llamar

No levante el teléfono sin esto. Es la misma lista del apartado 5.5, repetida
aquí para que la tarjeta sea autosuficiente.

- [ ] Qué pasó, en una frase, y desde cuándo
- [ ] A quién afecta: un usuario, un sitio, todos
- [ ] Dónde se detuvo el flujo, según el triage del apartado 4.1
- [ ] El Correlation ID, si el sistema mostró un error
- [ ] La respuesta de `Invoke-RestMethod http://localhost/health/ready`
- [ ] Qué se intentó ya de este manual y qué resultado dio
- [ ] Identificador del estudio o del mensaje, si aplica, con la hora aproximada

### B.4 Qué no se escala

Estas situaciones **no** son fallas y no necesitan llamada:

- Una sesión que expiró después de una hora sin uso.
- Un corte de 10 a 30 segundos justo después de reiniciar el app pool.
- Un nodo que aparece fuera de línea mientras su sitio está apagado por horario.
- Mensajes HL7 rechazados por ser de un tipo que el Hub no procesa: es diseño,
  no defecto.
