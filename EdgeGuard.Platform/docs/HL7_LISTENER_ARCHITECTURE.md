# HL7 Listener Service - Clean Architecture

Implementación de un servicio de escucha HL7 siguiendo los principios de **Clean Architecture** para EdgeGuard.Platform.

## 📐 Arquitectura

```
┌─────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                       │
│  ┌────────────────┐          ┌──────────────────────────────┐   │
│  │  Program.cs    │          │  Hl7StatusController.cs      │   │
│  │  (DI Setup)    │          │  (API Endpoints)             │   │
│  └────────────────┘          └──────────────────────────────┘   │
└────────────────────────────────┬────────────────────────────────┘
                                 │
┌────────────────────────────────┼────────────────────────────────┐
│                    Application Layer                            │
│  ┌──────────────────────┐   ┌────────────────────────────────┐ │
│  │ Hl7MessageProcessor  │   │   Hl7ListenerOptions           │ │
│  │ (Use Case)           │   │   (Configuration)              │ │
│  └──────────────────────┘   └────────────────────────────────┘ │
└────────────────────────────────┬────────────────────────────────┘
                                 │
┌────────────────────────────────┼────────────────────────────────┐
│                      Domain Layer                               │
│  ┌──────────────────┐   ┌────────────────────────────────────┐ │
│  │  Hl7Message      │   │  Interfaces:                       │ │
│  │  (Entity)        │   │  - IHl7Listener                    │ │
│  │                  │   │  - IHl7MessageRepository           │ │
│  └──────────────────┘   │  - IHl7MessageProcessor            │ │
│                         └────────────────────────────────────┘ │
└────────────────────────────────┬────────────────────────────────┘
                                 │
┌────────────────────────────────┼────────────────────────────────┐
│                   Infrastructure Layer                          │
│  ┌─────────────────────────┐  ┌──────────────────────────────┐ │
│  │  Hl7TcpListener         │  │  InMemoryHl7MessageRepository│ │
│  │  (TCP Implementation)   │  │  (Repository Implementation) │ │
│  └─────────────────────────┘  └──────────────────────────────┘ │
│  ┌─────────────────────────┐                                   │
│  │ Hl7ListenerHostedService│                                   │
│  │ (BackgroundService)     │                                   │
│  └─────────────────────────┘                                   │
└─────────────────────────────────────────────────────────────────┘
```

## 📂 Estructura de Archivos

### **Domain Layer** (`Dicom.Edge.Hub.Domain`)
```
Domain/
├── Entities/
│   └── Hl7Message.cs              # Entidad de dominio con lógica de negocio
└── Interfaces/
    ├── IHl7Listener.cs            # Contrato del listener
    └── IHl7MessageRepository.cs   # Contrato del repositorio
```

### **Application Layer** (`Dicom.Edge.Hub.Application`)
```
Application/
├── Hl7/
│   ├── IHl7MessageProcessor.cs    # Interfaz del procesador
│   ├── Hl7MessageProcessor.cs     # Caso de uso de procesamiento
│   └── Hl7ListenerOptions.cs      # Configuración
└── Extensions/
    └── Hl7ServiceCollectionExtensions.cs  # DI de aplicación
```

### **Infrastructure Layer** (`Dicom.Edge.Hub.Infrastructure`)
```
Infrastructure/
├── Services/
│   └── Hl7TcpListener.cs          # Implementación TCP del listener
├── Repositories/
│   └── InMemoryHl7MessageRepository.cs  # Repositorio en memoria
├── HostedServices/
│   └── Hl7ListenerHostedService.cs      # BackgroundService
└── Extensions/
    └── Hl7InfrastructureExtensions.cs   # DI de infraestructura
```

### **Presentation Layer** (`Dicom.Edge.Hub.Api`)
```
Api/
├── Controllers/
│   └── Hl7StatusController.cs     # Endpoints de monitoreo
├── Program.cs                      # Configuración de DI
└── appsettings.Development.json    # Configuración
```

## 🚀 Uso

### **1. Configuración** (`appsettings.json`)

```json
{
  "Hl7Listener": {
    "Enabled": true,
    "Port": 2575,
    "MaxConcurrentConnections": 100,
    "MaxQueuedMessages": 1000,
    "ProcessingWorkers": 4,
    "ConnectionTimeoutMs": 300000,
    "BufferSize": 8192
  }
}
```

### **2. Registro en DI** (`Program.cs`)

```csharp
// Ya configurado en tu Program.cs
builder.Services.AddHl7Application(builder.Configuration);
builder.Services.AddHl7Infrastructure();
```

### **3. Ejecutar la API**

```bash
dotnet run --project src/backend/Dicom.Edge.Hub.Api
```

El listener se iniciará automáticamente en el puerto **2575**.

## 🔍 Endpoints de Monitoreo

### **Estado del Listener**
```http
GET /api/hl7status/status
```
**Respuesta:**
```json
{
  "isRunning": true,
  "port": 2575,
  "activeConnections": 3
}
```

### **Mensajes Recientes**
```http
GET /api/hl7status/recent-messages?count=10
```
**Respuesta:**
```json
[
  {
    "id": "guid",
    "messageType": "ADT^A01",
    "sendingApplication": "HIS",
    "sendingFacility": "Hospital",
    "receivedAt": "2025-01-15T10:30:00Z",
    "clientEndpoint": "192.168.1.100:54321",
    "status": "Processed",
    "processedAt": "2025-01-15T10:30:01Z"
  }
]
```

### **Detalle de Mensaje**
```http
GET /api/hl7status/messages/{id}
```

## 🧪 Probar el Listener

### **Con netcat (Linux/Mac)**
```bash
echo -e "MSH|^~\\&|TestApp|Facility|||20250115103000||ADT^A01|12345|P|2.5\r" | nc localhost 2575
```

### **Con PowerShell (Windows)**
```powershell
$client = New-Object System.Net.Sockets.TcpClient("localhost", 2575)
$stream = $client.GetStream()
$message = "MSH|^~\&|TestApp|Facility|||20250115103000||ADT^A01|12345|P|2.5`r"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$stream.Write($bytes, 0, $bytes.Length)
$stream.Close()
$client.Close()
```

## ⚙️ Características Principales

### **✅ Múltiples Conexiones Concurrentes**
- Maneja hasta 100 conexiones simultáneas (configurable)
- Control de concurrencia con `SemaphoreSlim`
- Timeout automático de conexiones inactivas

### **✅ Procesamiento Paralelo**
- 4 workers procesan mensajes en paralelo (configurable)
- Desacoplamiento mediante `System.Threading.Channels`
- Backpressure automático con `BoundedChannel`

### **✅ Persistencia**
- Todos los mensajes se persisten automáticamente
- Tracking de estado (Received → Processing → Processed/Failed)
- Repositorio extensible (actualmente en memoria)

### **✅ ACK Automático**
- Envía ACK HL7 al cliente inmediatamente
- Cumple con el estándar HL7 v2.5

### **✅ Observabilidad**
- Logging estructurado con diferentes niveles
- Métricas de conexiones activas
- Rastreo de mensajes por ID

## 🔄 Flujo de Procesamiento

```
┌──────────┐
│ Cliente  │
│  HL7     │
└─────┬────┘
      │ 1. Envía mensaje HL7
      ▼
┌─────────────────┐
│  Hl7TcpListener │
└────────┬────────┘
         │ 2. Persiste mensaje
         ▼
┌─────────────────────┐
│ IHl7MessageRepository│
└─────────────────────┘
         │ 3. Encola en Channel
         ▼
┌──────────────────┐
│ Processing Worker│
└────────┬─────────┘
         │ 4. Procesa mensaje
         ▼
┌────────────────────┐
│ Hl7MessageProcessor│
└────────┬───────────┘
         │ 5. Lógica de negocio
         ▼
┌─────────────────────┐
│ Actualiza estado    │
│ (Processed/Failed)  │
└─────────────────────┘
```

## 📝 Extender la Funcionalidad

### **1. Agregar Persistencia Real**

Reemplazar `InMemoryHl7MessageRepository` con Entity Framework:

```csharp
// Infrastructure/Repositories/EfHl7MessageRepository.cs
public class EfHl7MessageRepository : IHl7MessageRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<Hl7Message> AddAsync(Hl7Message message, CancellationToken ct)
    {
        _context.Hl7Messages.Add(message);
        await _context.SaveChangesAsync(ct);
        return message;
    }
    // ... implementar otros métodos
}
```

Registrar en DI:
```csharp
services.AddScoped<IHl7MessageRepository, EfHl7MessageRepository>();
```

### **2. Agregar Lógica de Negocio**

Editar `Hl7MessageProcessor.cs`:

```csharp
public async Task ProcessAsync(Hl7Message message, CancellationToken ct)
{
    // Parsear mensaje completo con una librería HL7
    var parsedMessage = _hl7Parser.Parse(message.Content);
    
    // Mapear a entidades del dominio
    var patient = _mapper.MapToPatient(parsedMessage);
    
    // Ejecutar lógica de negocio
    await _patientService.CreateOrUpdateAsync(patient, ct);
    
    // Publicar evento de integración
    await _eventBus.PublishAsync(new PatientCreatedEvent(patient), ct);
}
```

### **3. Agregar Health Check**

```csharp
public class Hl7ListenerHealthCheck : IHealthCheck
{
    private readonly IHl7Listener _listener;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        return Task.FromResult(_listener.IsRunning
            ? HealthCheckResult.Healthy("HL7 Listener is running")
            : HealthCheckResult.Unhealthy("HL7 Listener is not running"));
    }
}

// En Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<Hl7ListenerHealthCheck>("hl7_listener");
```

## 🏗️ Principios de Clean Architecture Aplicados

✅ **Independencia de frameworks**: El dominio no depende de Entity Framework, ASP.NET, etc.  
✅ **Testabilidad**: Todas las dependencias son interfaces que se pueden mockear  
✅ **Independencia de UI**: La lógica no conoce HTTP, gRPC o cualquier otro protocolo  
✅ **Independencia de BD**: El dominio no sabe si usa SQL, NoSQL o memoria  
✅ **Regla de dependencia**: Las capas externas dependen de las internas, nunca al revés

## 📚 Referencias

- [HL7 v2.x Standard](http://www.hl7.org/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Hosted Services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [System.Threading.Channels](https://learn.microsoft.com/en-us/dotnet/api/system.threading.channels)
