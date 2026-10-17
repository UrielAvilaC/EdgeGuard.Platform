#
# EdgeGuard Node — archivo de configuración del instalador
#
# Copiar como 'node-install.psd1' junto a install.ps1 y rellenar.
#
# Uso:
#   .\install.ps1                  Detecta este archivo, valida y pide confirmación
#   .\install.ps1 -NonInteractive  Valida y ejecuta sin confirmar
#   .\install.ps1 -DryRun          Simula el recorrido completo, no escribe nada
#
# Este archivo es la ÚNICA fuente de verdad del despliegue. El instalador genera
# appsettings.Production.json a partir de él en cada ejecución: no edites ese
# archivo a mano, porque la siguiente actualización lo reescribe.
#
@{

    # ── Paquete y servicio ───────────────────────────────────────────────────
    # PackagePath $null = autodetecta el .zip de mayor versión dentro de data\
    PackagePath      = $null

    # Dónde viven los BINARIOS. Cada actualización reemplaza este directorio
    # completo, así que aquí no puede quedar nada que deba sobrevivir.
    InstallPath      = 'C:\EdgeGuard\Node'

    ServiceName      = 'EdgeGuardNode'
    DisplayName      = 'EdgeGuard DICOM Node'
    StartupType      = 'Automatic'

    # LocalSystem evita todo el trabajo de ACL. Con una cuenta dedicada el paso
    # 08 concede escritura sobre los tres directorios de datos.
    ServiceAccount   = 'LocalSystem'

    # ── Rutas de datos ───────────────────────────────────────────────────────
    # Fuera de InstallPath a propósito: sobreviven a las actualizaciones.
    #
    # La base y los estudios DEBEN ser directorios separados. El nodo lo verifica
    # al arrancar y en Producción se niega a iniciar si se solapan: la medición
    # del espacio recorre la carpeta de estudios y contaría el archivo SQLite
    # como carga DICOM purgable, de modo que la limpieza de emergencia borraría
    # estudios para aliviar una presión que no puede aliviar. El instalador
    # aplica la misma regla antes de escribir nada.
    #
    # Rutas absolutas siempre. Una ruta relativa se resolvería contra el
    # directorio del ejecutable, no contra donde estén los datos.
    DataRootPath     = 'C:\ProgramData\EdgeGuard\Node'
    DatabasePath     = 'C:\ProgramData\EdgeGuard\Node\persistence\edge-node.db'
    StorageRootPath  = 'C:\ProgramData\EdgeGuard\Node\data'
    LogPath          = 'C:\ProgramData\EdgeGuard\Node\logs'

    # ── DICOM ────────────────────────────────────────────────────────────────
    # AeTitle: máximo 16 caracteres, sin espacios. Es el AE con el que el nodo
    # se anuncia al Hub y con el que sale como SCU hacia el PACS.
    AeTitle          = 'EDGE_NODE'

    # Puerto donde el nodo RECIBE C-STORE, C-ECHO y MWL. El paso 01 aborta si ya
    # lo escucha otro proceso, y el paso 09 le abre la regla de firewall.
    DicomPort        = 11112

    # Puerto HTTP de la API del nodo: por ahí entra el Hub para empujar
    # configuración y por ahí responde /health/ready.
    NodeApiPort      = 5120

    # ── Conexión con el Hub ──────────────────────────────────────────────────
    # HubBaseUrl es OBLIGATORIA y debe ser absoluta http/https. No hay default:
    # un nodo sin dirección de Hub falla al arrancar en vez de marcar un
    # endpoint que nadie eligió. El paso 04 comprueba que responda ANTES de
    # instalar nada.
    #
    # Con https y certificado autofirmado, el paso 04 lo reporta: el nodo no
    # confía en certificados que el servidor no valide, y el síntoma sin esta
    # verificación sería un nodo instalado que nunca aparece en el Hub.
    HubBaseUrl       = 'http://hub.miclinica.com'

    # Identidad con la que este nodo se registra. NodeName debe ser único entre
    # todos los nodos del Hub; el paso 10 lo confirma tras el registro.
    NodeName         = 'RIS_HOSPITAL_NORTE'
    FacilityName     = 'Hospital Norte'
    Location         = 'Sala de RX - Piso 2'

    # IP con la que el nodo se anuncia: es la dirección que el Hub y el PACS
    # usarán para alcanzarlo, no necesariamente la del adaptador principal.
    IpAddress        = '192.168.1.50'

    # Opcional. Vacío = el nodo pide su token al arrancar contra
    # POST /api/edge/token (anónimo, válido 5 minutos, de un solo uso).
    # Rellénalo solo si el Hub tiene el auto-servicio cerrado y te emitieron uno
    # con POST /api/nodes/bootstrap-tokens.
    BootstrapToken   = $null

    # Cadencias. Los defaults sirven para una sede normal; súbelos en enlaces
    # lentos o con muchos nodos contra el mismo Hub.
    TimeoutSeconds            = 30
    HeartbeatIntervalSeconds  = 60
    ConfigPullIntervalSeconds = 900
    RegisterOnStartup         = $true
    MaxReconnectAttempts      = 10
    ReconnectDelaySeconds     = 30

    # ── Diagnóstico ──────────────────────────────────────────────────────────
    # InstanceId identifica a este nodo en los logs y en la telemetría. Con
    # varios nodos mandando al mismo destino, repetirlo hace ilegible el log.
    InstanceId       = 'NODE-001'
    RetainDays       = 90
    LogMaxFileSizeMb = 200

    # ── Firewall ─────────────────────────────────────────────────────────────
    # Origen permitido para el puerto DICOM. 'Any' lo deja abierto a toda la
    # red: DICOM es TCP sin TLS ni autenticación fuerte y transporta PHI, así
    # que conviene acotarlo a las modalidades y al PACS.
    #   Ejemplo: @('192.168.1.0/24', '10.0.5.17')
    DicomRemoteAddress   = 'Any'

    # La API del nodo solo la consume el Hub. Acotarla a su IP es lo razonable.
    NodeApiRemoteAddress = 'Any'
}
