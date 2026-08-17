#
# EdgeGuard Hub — archivo de configuración del instalador
#
# Copiar como 'hub-install.psd1' junto a install.ps1 y rellenar.
#
# Uso:
#   .\install.ps1                  Detecta este archivo, valida y pide confirmación
#   .\install.ps1 -NonInteractive  Valida y ejecuta sin confirmar
#   .\install.ps1 -DryRun          Simula el recorrido completo, no escribe nada
#
# ATENCIÓN: este archivo contiene la contraseña de PostgreSQL en texto plano.
# Trátalo como un secreto: no lo subas al repositorio (ya está en .gitignore)
# ni lo dejes en recursos compartidos. Bórralo del servidor al terminar.
#
@{

    # ── Paquete ──────────────────────────────────────────────────────────────
    # $null = autodetecta el .zip de mayor versión dentro de data\
    PackagePath               = $null
    InstallPath               = 'C:\inetpub\EdgeGuard\Hub'

    # ── IIS ──────────────────────────────────────────────────────────────────
    # Solo HTTP. Si se requiere HTTPS se añade el binding a mano en IIS Manager
    # después de instalar.
    SiteName                  = 'EdgeGuard.Hub'
    AppPoolName               = 'EdgeGuardHub'
    HostHeader                = 'hub.local'
    Port                      = 80
    AppPoolIdentity           = 'LocalSystem'

    # ── PostgreSQL ───────────────────────────────────────────────────────────
    # El rol y la base ya deben existir; el instalador solo verifica que sean
    # alcanzables y que el rol tenga CREATE en el esquema (el Hub aplica las
    # migraciones de EF al arrancar).
    DbHost                    = 'localhost'
    DbPort                    = 5432
    DbName                    = 'edgeguard_hub'
    DbUser                    = 'edgeguard'
    DbPassword                = 'CAMBIAR_ANTES_DE_INSTALAR'

    # ── HL7 MLLP ─────────────────────────────────────────────────────────────
    # TCP crudo, sin TLS ni autenticación, transportando PHI. 'Any' expone el
    # puerto a toda la red que alcance al servidor; acotarlo a la subred del
    # HIS/RIS (p. ej. '10.20.30.0/24') en cuanto sea posible.
    Hl7Enabled                = $true
    Hl7Port                   = 8001
    Hl7RemoteAddress          = 'Any'

    # ── Data Protection ──────────────────────────────────────────────────────
    # DEBE quedar fuera de InstallPath. Cada actualización reemplaza esa
    # carpeta, y perder el key ring vuelve indescifrables los SigningSecret de
    # todos los nodos. El instalador aborta si esta ruta cae bajo InstallPath.
    DataProtectionKeyPath     = 'C:\inetpub\edgeguard\dp-keys'

    # ── Diagnóstico ──────────────────────────────────────────────────────────
    InstanceId                = 'HUB-001'
    RedactionMode             = 'Strict'   # Strict = PHI redactado | Relaxed

    # ── CORS ─────────────────────────────────────────────────────────────────
    # El SPA se sirve desde el mismo sitio, así que normalmente va vacío.
    # Añadir solo orígenes que difieran en esquema, host o puerto.
    CorsAllowedOrigins        = @()

    # ── Banderas de despliegue coordinado ────────────────────────────────────
    # Activar recién cuando todos los nodos hayan salido con la versión nueva.
    NodeAuthEnforce           = $false
    Hl7ValidateBeforeAck      = $false

    # ── Hosting bundle ───────────────────────────────────────────────────────
    # $false = si falta y no está en data\, aborta en vez de salir a internet.
    AllowHostingBundleDownload = $false

}
