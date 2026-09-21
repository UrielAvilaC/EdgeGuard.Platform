## 1. La revisión diaria

El Hub no necesita atención constante. Necesita cinco minutos al día de alguien
que sepa qué mirar, y este apartado es esa lista.

La revisión sirve para dos cosas: confirmar que lo de ayer siguió funcionando, y
detectar hoy lo que mañana sería una llamada del área de imagenología. Casi
todas las fallas que este manual describe avisan antes de manifestarse.

### 1.1 Los cinco minutos de la mañana

Hágalo a la misma hora, antes de que empiece la operación del día.

**1. Entre al Hub desde un equipo de la red** — no desde el servidor.

Abra `http://<IP-del-servidor>/` en el navegador e inicie sesión.

- [ ] La pantalla de acceso carga
- [ ] La sesión se abre con su cuenta

Si la pantalla no carga desde la red pero sí desde el servidor, el problema es
de red o de firewall, no del Hub. Vaya al apartado 4.

**2. Revise los nodos.**

En la pantalla de nodos, todos los sitios que deberían estar operando aparecen
**en línea**.

- [ ] Ningún nodo aparece fuera de línea o con estado desconocido
- [ ] La última comunicación de cada nodo es de hace minutos, no de horas

Un nodo fuera de línea significa que ese sitio no está enviando estudios, aunque
el resto del sistema se vea perfecto.

**3. Revise los estudios de las últimas 24 horas.**

- [ ] Llegaron estudios de todos los sitios que operaron ayer
- [ ] Ninguno lleva horas en estado *Enviando*
- [ ] La cantidad se parece a la de un día equivalente

Un sitio que operó y no aparece es la señal más temprana de un problema, y la
más fácil de pasar por alto: el sistema no reporta ningún error por un estudio
que nunca llegó.

**4. Confirme la salud del servidor.**

Desde el propio servidor, en una consola de PowerShell:

```powershell
Invoke-RestMethod http://localhost/health/ready
```

Debe responder con `status` en `Healthy`. Cualquier otro valor se interpreta en
el apartado 1.2.

**5. Revise el espacio en disco.**

```powershell
Get-PSDrive C | Select-Object Used, Free
```

- [ ] Quedan más de 20 GB libres

Por debajo de 20 GB conviene actuar sin prisa; por debajo de 5 GB hay que actuar
el mismo día. El Hub guarda metadatos, auditoría y mensajes HL7 —no imágenes—,
así que un crecimiento súbito es en sí mismo una señal de que algo se está
acumulando.

### 1.2 Cómo se lee la salud del Hub

El Hub publica dos direcciones de salud. Sirven para cosas distintas y conviene
no confundirlas.

| Dirección | Qué contesta | Cuándo se usa |
|---|---|---|
| `http://localhost/health/live` | Si el Hub está vivo y respondiendo | Para saber si el proceso arrancó |
| `http://localhost/health/ready` | Si además puede trabajar: base de datos, disco y sus demás dependencias | Para la revisión diaria |

`health/live` puede contestar correctamente mientras la base de datos está
caída: solo dice que el proceso está en pie. La revisión diaria usa
`health/ready`, que es el que se entera.

**Respuesta normal**

```json
{
  "status": "Healthy",
  "duration": 42.38,
  "timestamp": "2026-09-07T13:02:11.4210000+00:00",
  "checks": [
    { "name": "database", "status": "Healthy", "description": "PostgreSQL connection established" },
    { "name": "storage",  "status": "Healthy", "description": "Free disk space: 82.3 GB" }
  ]
}
```

**Los tres estados posibles**

| `status` | Código HTTP | Qué significa | Qué hacer |
|---|---|---|---|
| `Healthy` | 200 | Todo en orden | Nada |
| `Degraded` | 200 | Algo no crítico está al límite; casi siempre el disco | Atender el mismo día. El servicio sigue funcionando |
| `Unhealthy` | 503 | Algo crítico falló; casi siempre la base de datos | Atender de inmediato. El Hub no puede operar |

Cuando el estado no es `Healthy`, la clave está en el bloque `checks`: cada
comprobación trae su propio estado y una descripción que casi siempre dice
exactamente qué pasa. Cópiela tal cual al reportar.

**Si la dirección no contesta nada** —ni siquiera un error— el Hub no está
arriba. Eso es el apartado 4.1, no un problema de salud.

### 1.3 Qué vigilar y cuándo preocuparse

Esta tabla es el criterio para decidir si algo amerita una llamada. Los valores
son un punto de partida razonable; ajústelos a su operación después de las
primeras semanas.

| Qué se observa | Atención | Urgente | Qué suele significar |
|---|---|---|---|
| Estudios recibidos por hora | Cae más de la mitad respecto a un día normal | Cae casi a cero | Un sitio dejó de enviar, o el nodo perdió contacto |
| Estudios en *Enviando* | Más de 10 minutos | Más de una hora, o se acumulan | El PACS no está recibiendo |
| Último contacto de un nodo | Más de 5 minutos | Más de 15 minutos | El nodo está apagado, sin red, o el servicio se detuvo |
| Espacio libre en disco | Menos de 20 GB | Menos de 5 GB | Crecimiento de auditoría o de registros |
| Memoria del servidor | Arriba del 80 % | Arriba del 95 % | Acumulación de trabajo pendiente |
| Mensajes HL7 sin procesar | Más de 100 | Más de 500 | El HIS envía más rápido de lo que se procesa, o hay mensajes rechazados |

**Lo que no aparece en ninguna alarma.** Un estudio que nunca se envió desde la
modalidad no genera error en ningún lado: para el Hub, ese estudio no existe. Es
el motivo por el que el punto 3 de la revisión diaria —comparar contra un día
equivalente— no se puede sustituir por una alarma automática.

### 1.4 Cinco cosas que no hay que hacer

Cada una de estas ha roto una instalación en producción. No son
recomendaciones.

**No reactive el Default Web Site de IIS.** El Hub tiene el puerto 80 del
servidor. Si ese sitio se inicia, se pelea por el mismo puerto y el Hub deja de
responder en el siguiente reinicio del servidor.

**No borre ni mueva la carpeta de llaves** (`C:\inetpub\edgeguard\dp-keys`).
Sin ella, todos los nodos registrados tienen que volver a autenticarse antes de
poder trabajar. Se explica en el apartado 2.4.

**No cambie el emisor de los tokens** (`Jwt__Issuer`) al activar HTTPS. Es una
etiqueta interna, no una dirección de acceso. Cambiarla cierra todas las
sesiones abiertas y no arregla nada.

**No edite archivos dentro de `C:\inetpub\EdgeGuard\Hub`.** La siguiente
actualización reemplaza ese directorio completo. Lo que haya que cambiar se
cambia en la configuración del app pool o desde la propia interfaz del Hub.

**No deje `hub-install.psd1` en el servidor.** Contiene la contraseña de la base
de datos en texto plano. Se borra al terminar la instalación.

---

## 2. Reinicios y mantenimiento

### 2.1 Reiniciar el Hub

**Cuándo:** después de cambiar una variable de configuración, cuando el soporte
lo indique, o como primer intento ante un comportamiento anómalo que no se
explica de otra forma.

El Hub no es un servicio de Windows: vive dentro de IIS, en lo que IIS llama un
*app pool*. Reiniciarlo es reciclar ese app pool.

```powershell
Import-Module WebAdministration
Restart-WebAppPool -Name EdgeGuardHub
```

**Qué debe ocurrir:** el comando no imprime nada y devuelve el control en uno o
dos segundos. El Hub queda disponible en unos 10 a 30 segundos.

**Confirme que volvió:**

```powershell
Invoke-RestMethod http://localhost/health/ready
```

**Qué ven los usuarios mientras tanto:** la interfaz web muestra un error o se
queda cargando durante esos segundos y luego se recupera sola. Las sesiones
abiertas **no** se cierran.

**Qué se interrumpe de verdad:** el receptor de mensajes HL7 vive dentro del
mismo proceso del Hub. Mientras el app pool reinicia, el puerto 8001 no acepta
conexiones y el HIS recibe un rechazo. Los sistemas HIS/RIS normalmente
reintentan, pero **no reinicie el Hub en el pico de la mañana** si puede
evitarlo.

**Qué NO se pierde en un reinicio:** las llaves, la configuración, la base de
datos, los estudios en curso y el registro de sesiones. Un reinicio del app pool
es una operación segura; se documenta aquí para que se use sin miedo cuando hace
falta.

### 2.2 Reiniciar IIS completo

```powershell
iisreset
```

**Casi nunca es lo correcto.** Detiene todos los sitios del servidor, tarda más
y no resuelve nada que el reinicio del app pool no resuelva. Se usa solo cuando
el propio IIS quedó en mal estado —por ejemplo, si `Restart-WebAppPool` devuelve
un error— o cuando el soporte lo pide expresamente.

### 2.3 Reiniciar el servidor

**Cuándo:** actualizaciones de Windows, mantenimiento programado, o cuando el
instalador avisó que quedaba un reinicio pendiente.

**Antes de reiniciar:**

- [ ] Avise al área de imagenología y al responsable del HIS/RIS
- [ ] Confirme que no hay estudios en tránsito (ninguno en *Enviando*)
- [ ] Confirme que el Default Web Site de IIS sigue detenido y sin inicio automático

**Después de reiniciar:**

```powershell
Get-WebAppPoolState -Name EdgeGuardHub
Invoke-RestMethod http://localhost/health/ready
```

- [ ] El app pool aparece como `Started`
- [ ] La salud responde `Healthy`
- [ ] Los nodos vuelven a aparecer en línea en dos o tres minutos
- [ ] El puerto 8001 vuelve a aceptar conexiones: `Test-NetConnection -ComputerName localhost -Port 8001`

El Hub está configurado para arrancar solo, sin esperar a que alguien abra la
página. Si tras el reinicio no responde, el apartado 4.1 tiene el procedimiento.

### 2.4 El key ring: qué es y por qué no se toca

En `C:\inetpub\edgeguard\dp-keys` hay unos archivos XML. Son las llaves con las
que el Hub cifra los secretos que guarda, entre ellos la credencial con la que
cada nodo registrado se identifica.

**Si esas llaves se pierden**, el Hub sigue arrancando y la interfaz sigue
funcionando —no hay ningún error visible—, pero deja de poder descifrar la
credencial de cada nodo. Cada uno de ellos tiene que volver a autenticarse
contra el Hub para que se le recomponga. En una instalación con varios sitios,
eso es una mañana de trabajo y un servicio interrumpido.

**Reglas:**

- La carpeta se respalda (apartado 3).
- No se borra, no se mueve y no se "limpia" aunque parezca que sobra.
- Está fuera del directorio de la aplicación a propósito: así una actualización
  no la toca.
- Si alguna vez está vacía, **no registre nodos nuevos** y consulte al proveedor
  antes de seguir.

Verificación rápida:

```powershell
Get-ChildItem C:\inetpub\edgeguard\dp-keys -Filter 'key-*.xml'
```

Debe existir al menos un archivo.

### 2.5 Registros: espacio y retención

El Hub escribe un archivo de registro por día en
`C:\inetpub\EdgeGuard\Hub\logs\` y **se encarga solo de borrar los viejos**:
conserva alrededor de un mes, con un tope de 100 MB por archivo.

No hay que hacer limpieza manual. Si alguien la hace, que sea moviendo archivos
a otro lado —nunca borrando el archivo del día en curso, que está abierto.

Si su política de resguardo exige conservar los registros más de un mes,
cópielos periódicamente a otro almacenamiento; el Hub no lo hace por su cuenta.

### 2.6 Ventanas de mantenimiento

| Tarea | Interrupción | Aviso previo recomendado |
|---|---|---|
| Reiniciar el app pool | 10–30 segundos | Ninguno fuera de horas pico; avisar al HIS si es en horario de operación |
| Reiniciar el servidor | 3–10 minutos | Imagenología y responsable del HIS/RIS |
| Actualizar el Hub | 5–15 minutos | Ventana de cambio formal |
| Respaldo de la base | Ninguna | Ninguno |

**La mejor ventana es la que su operación defina, no la que diga este manual.**
Como referencia: el momento de menor actividad suele ser entre el final de la
jornada y el inicio de la primera vuelta de la mañana, y las urgencias no
respetan ninguna de las dos.
