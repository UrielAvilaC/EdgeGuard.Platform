# 🎯 Scripts de Prueba HL7 Listener

Scripts de PowerShell para probar el servicio HL7 Listener de EdgeGuard.Platform.

## 📋 Scripts Disponibles

### 1. `Test-Hl7Listener.ps1`
Envía un mensaje HL7 individual para probar la conectividad básica.

**Uso:**
```powershell
.\scripts\Test-Hl7Listener.ps1
```

**Con parámetros personalizados:**
```powershell
.\scripts\Test-Hl7Listener.ps1 -Server "192.168.1.100" -Port 2575
```

**Salida esperada:**
```
========================================
   HL7 Listener Test Script
========================================

Target: localhost:2575

Mensaje HL7 a enviar:
MSH|^~\&|TestApp|TestFacility|EdgeGuard|EdgeGuard|20250115103000||ADT^A01|ABC123|P|2.5
...

✓ Conectado exitosamente
✓ Mensaje enviado (256 bytes)
✓ ACK recibido:
MSH|^~\&|EdgeGuardHub|EdgeGuard|TestApp|TestFacility|...||ACK|...
MSA|AA|...

========================================
Prueba completada exitosamente
========================================
```

---

### 2. `Test-Hl7ConcurrentLoad.ps1`
Envía múltiples mensajes HL7 de forma concurrente para probar el manejo de concurrencia.

**Uso básico:**
```powershell
.\scripts\Test-Hl7ConcurrentLoad.ps1
```

**Prueba de carga alta:**
```powershell
.\scripts\Test-Hl7ConcurrentLoad.ps1 -MessageCount 100 -ConcurrentConnections 20
```

**Parámetros:**
- `-Server`: Dirección del servidor (default: localhost)
- `-Port`: Puerto del listener (default: 2575)
- `-MessageCount`: Número total de mensajes a enviar (default: 10)
- `-ConcurrentConnections`: Conexiones simultáneas máximas (default: 5)

**Salida esperada:**
```
========================================
   HL7 Concurrent Load Test
========================================

Target: localhost:2575
Messages: 100
Concurrent: 20

Enviando 100 mensajes...

[1/100] Mensaje TEST-001 enviado...
[2/100] Mensaje TEST-002 enviado...
...

========================================
Resultados:
========================================
✓ TEST-001 - ACK recibido
✓ TEST-002 - ACK recibido
...

========================================
Total: 100 | Exitosos: 100 | Fallidos: 0
========================================
```

---

## 🚀 Antes de Ejecutar

### 1. **Iniciar la API**
```powershell
cd src\backend\Dicom.Edge.Hub.Api
dotnet run
```

### 2. **Verificar que el listener esté habilitado**
En `appsettings.Development.json`:
```json
{
  "Hl7Listener": {
    "Enabled": true,
    "Port": 2575
  }
}
```

### 3. **Verificar el estado del listener**
```powershell
curl http://localhost:5000/api/hl7status/status
```

Respuesta esperada:
```json
{
  "isRunning": true,
  "port": 2575,
  "activeConnections": 0
}
```

---

## 📊 Verificar Mensajes Procesados

### **API Endpoints**

#### Estado del Listener
```bash
GET http://localhost:5000/api/hl7status/status
```

#### Mensajes Recientes
```bash
GET http://localhost:5000/api/hl7status/recent-messages?count=10
```

#### Detalle de un Mensaje
```bash
GET http://localhost:5000/api/hl7status/messages/{id}
```

### **Con PowerShell**
```powershell
# Estado
Invoke-RestMethod -Uri "http://localhost:5000/api/hl7status/status"

# Mensajes recientes
Invoke-RestMethod -Uri "http://localhost:5000/api/hl7status/recent-messages?count=10"
```

### **Con curl**
```bash
curl http://localhost:5000/api/hl7status/recent-messages?count=10
```

---

## 🧪 Escenarios de Prueba

### **1. Prueba Básica de Conectividad**
```powershell
.\scripts\Test-Hl7Listener.ps1
```
Verifica que el listener acepta conexiones y responde con ACK.

### **2. Prueba de Múltiples Mensajes**
```powershell
.\scripts\Test-Hl7ConcurrentLoad.ps1 -MessageCount 50
```
Verifica que el listener procesa mensajes secuencialmente.

### **3. Prueba de Concurrencia**
```powershell
.\scripts\Test-Hl7ConcurrentLoad.ps1 -MessageCount 100 -ConcurrentConnections 20
```
Verifica que el listener maneja conexiones concurrentes.

### **4. Prueba de Carga Pesada**
```powershell
.\scripts\Test-Hl7ConcurrentLoad.ps1 -MessageCount 1000 -ConcurrentConnections 50
```
Verifica el rendimiento bajo carga alta.

### **5. Prueba de Reconexión**
Ejecutar el script varias veces seguidas para verificar que el listener acepta nuevas conexiones correctamente.

---

## 🔍 Monitoreo Durante las Pruebas

### **Ver Logs en Tiempo Real**
En la consola donde ejecutaste `dotnet run`, verás logs como:
```
info: Dicom.Edge.Hub.Infrastructure.Services.Hl7TcpListener[0]
      HL7 Listener started successfully on port 2575
info: Dicom.Edge.Hub.Infrastructure.Services.Hl7TcpListener[0]
      Client connected: 127.0.0.1:54321. Active connections: 1
info: Dicom.Edge.Hub.Infrastructure.Services.Hl7TcpListener[0]
      Message abc123 received from 127.0.0.1:54321, Type: ADT^A01
```

### **Ver Conexiones Activas**
```powershell
while ($true) {
    $status = Invoke-RestMethod -Uri "http://localhost:5000/api/hl7status/status"
    Write-Host "Active Connections: $($status.activeConnections)" -ForegroundColor Yellow
    Start-Sleep -Seconds 1
}
```

---

## ❌ Solución de Problemas

### **Error: "No se puede conectar al servidor"**
- ✅ Verifica que la API esté ejecutándose
- ✅ Verifica el puerto en `appsettings.json`
- ✅ Verifica el firewall de Windows

### **Error: "Connection refused"**
- ✅ Verifica que `Hl7Listener.Enabled = true` en la configuración
- ✅ Revisa los logs de la API para ver si el listener se inició

### **No se recibe ACK**
- ✅ Revisa los logs para ver si hay errores de procesamiento
- ✅ Verifica que el mensaje HL7 tenga el formato correcto

### **Los mensajes no aparecen en `/recent-messages`**
- ✅ Verifica que el mensaje se haya enviado correctamente
- ✅ Revisa los logs del procesador de mensajes
- ✅ Verifica que no haya errores en `Hl7MessageProcessor`

---

## 📚 Referencias

- [HL7 v2.x Message Structure](http://www.hl7.org/)
- [PowerShell TCP Client](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient)
- Documentación completa: [`docs/HL7_LISTENER_ARCHITECTURE.md`](../docs/HL7_LISTENER_ARCHITECTURE.md)
