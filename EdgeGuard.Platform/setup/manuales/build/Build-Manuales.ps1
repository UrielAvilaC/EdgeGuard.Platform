#Requires -Version 5.1
<#
.SYNOPSIS
    Genera los manuales del cliente en PDF a partir del Markdown del repositorio.

.DESCRIPTION
    Markdown → HTML de impresión → PDF. El Markdown es la fuente única; los PDF
    de `setup\manuales\` son producto generado y se pueden borrar y rehacer.

    Sin dependencias externas: la conversión a HTML la hace
    `Convert-Markdown.ps1` y la impresión el motor de Edge, que ya está en
    cualquier Windows actual. Lo único que hace falta es Node (para hablar el
    protocolo DevTools) y el propio Edge.

.PARAMETER Only
    Genera un solo manual, por su identificador. Sin este parámetro se generan
    todos los del manifiesto.

.PARAMETER KeepHtml
    Conserva el HTML intermedio junto al PDF. Útil para revisar el maquetado en
    el navegador sin volver a imprimir.

.EXAMPLE
    .\Build-Manuales.ps1
    Regenera el paquete completo.

.EXAMPLE
    .\Build-Manuales.ps1 -Only instalacion -KeepHtml
#>
[CmdletBinding()]
param(
    [string]$Only,
    [switch]$KeepHtml
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$BuildPath   = $PSScriptRoot
$ManualesPath = Split-Path -Parent $BuildPath
$RepoPath    = Split-Path -Parent (Split-Path -Parent $ManualesPath)
$DocsPath    = Join-Path $RepoPath 'docs\07-operations'

. (Join-Path $BuildPath 'Convert-Markdown.ps1')

# ─────────────────────────────────────────────────────────────────────────────
# Manifiesto
#
# Un renglón por manual entregable. La versión y la fecha se escriben aquí y
# aparecen en la portada: son lo que el cliente cita cuando reporta algo.
# ─────────────────────────────────────────────────────────────────────────────
$Manuales = @(
    @{
        Id        = 'instalacion'
        Source    = Join-Path $DocsPath 'runbook-instalacion-hub.md'
        Output    = 'Instalacion-y-puesta-en-marcha.pdf'
        Title     = 'Instalación y puesta en marcha'
        Subtitle  = 'Procedimiento completo de instalación del Hub EdgeGuard sobre Windows Server e IIS, desde la preparación del servidor hasta la verificación funcional.'
        # El pie es lo único que se repite en cada hoja. Chromium no sabe
        # excluir la portada del encabezado y el pie, así que se usa uno solo y
        # se carga en él la orientación: qué manual es y en qué página va.
        Footer    = 'EdgeGuard Platform &middot; Instalación y puesta en marcha'
        Version   = '1.2'
        IssueDate = '7 de septiembre de 2026'
        # El índice escrito a mano en el Markdown se retira: el PDF arma el suyo
        # con los encabezados reales, y así no puede quedarse desfasado.
        SkipSections = @('Índice')
        CoverNote = 'Este manual se usa una sola vez, durante la instalación. Para la operación diaria, el diagnóstico de fallas y los respaldos, consulte <strong>Operación, diagnóstico y soporte</strong>, que se entrega en este mismo paquete.'
    }

    @{
        Id     = 'operacion'
        # Un manual, varias fuentes: se escriben por capítulos y se arman en el
        # orden de esta lista.
        Source = @(
            Join-Path $DocsPath 'manual-operacion\01-operacion-y-monitoreo.md'
            Join-Path $DocsPath 'manual-operacion\02-respaldo-y-recuperacion.md'
            Join-Path $DocsPath 'manual-operacion\03-diagnostico-y-soporte.md'
            Join-Path $DocsPath 'manual-operacion\04-diccionario.md'
            Join-Path $DocsPath 'manual-operacion\05-apendices.md'
        )
        Output    = 'Operacion-diagnostico-y-soporte.pdf'
        Title     = 'Operación, diagnóstico y soporte'
        Subtitle  = 'Revisión diaria, mantenimiento, respaldos y guía de diagnóstico del Hub EdgeGuard. El manual de consulta permanente.'
        Footer    = 'EdgeGuard Platform &middot; Operación, diagnóstico y soporte'
        Version   = '1.0'
        IssueDate = '7 de septiembre de 2026'
        SkipSections = @()
        CoverNote = 'Este es el manual que se consulta durante la operación. Está ordenado por síntoma: busque lo que le reportaron, no lo que cree que falló. Para instalar o reinstalar el Hub, consulte <strong>Instalación y puesta en marcha</strong>.'
    }

    @{
        Id       = 'tarjeta'
        Source   = @( Join-Path $DocsPath 'manual-operacion\tarjeta.md' )
        Output   = 'Tarjeta-de-diagnostico-y-escalamiento.pdf'
        Title    = 'Diagnóstico y escalamiento'
        Subtitle = ''
        Footer   = 'EdgeGuard Platform &middot; extracto de <em>Operación, diagnóstico y soporte</em>'
        Version  = '1.0'
        IssueDate = '7 de septiembre de 2026'
        SkipSections = @()
        CoverNote = ''
        # Dos hojas para imprimir y pegar junto al servidor: plantilla propia,
        # sin portada ni índice, y márgenes estrechos.
        Template = 'plantilla-tarjeta.html'
        Margen   = '0.55'
    }
)

# ─────────────────────────────────────────────────────────────────────────────

function Assert-Herramientas {
    $node = Get-Command node -ErrorAction SilentlyContinue
    if (-not $node) {
        throw "No se encontró Node.js. Es lo único que hace falta instalar para generar los PDF (https://nodejs.org, versión 22 o superior)."
    }

    $version = (& node --version).TrimStart('v')
    $mayor = [int]($version -split '\.')[0]
    if ($mayor -lt 22) {
        throw "Node $version es demasiado antiguo. El impresor usa el WebSocket incorporado, que existe a partir de Node 22."
    }

    $rutas = @(
        'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
        'C:\Program Files\Microsoft\Edge\Application\msedge.exe'
        'C:\Program Files\Google\Chrome\Application\chrome.exe'
        'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe'
    )
    if (-not ($rutas | Where-Object { Test-Path -LiteralPath $_ })) {
        throw "No se encontró Microsoft Edge ni Google Chrome. El PDF se imprime con su motor."
    }
}

function New-Manual {
    param([hashtable]$Manual)

    $htmlPath = Join-Path $BuildPath ("{0}.html" -f $Manual.Id)
    $pdfPath  = Join-Path $ManualesPath $Manual.Output

    $plantilla = if ($Manual.ContainsKey('Template')) { $Manual.Template } else { 'plantilla.html' }

    Write-Host "  → HTML" -ForegroundColor DarkGray
    $resultado = ConvertTo-ManualHtml `
        -MarkdownPath $Manual.Source `
        -TemplatePath (Join-Path $BuildPath $plantilla) `
        -OutputPath   $htmlPath `
        -Title        $Manual.Title `
        -Subtitle     $Manual.Subtitle `
        -Version      $Manual.Version `
        -IssueDate    $Manual.IssueDate `
        -CoverNote    $Manual.CoverNote `
        -SkipSections $Manual.SkipSections

    Write-Host "    $($resultado.Sections) apartados, $($resultado.Headings) encabezados" -ForegroundColor DarkGray

    Write-Host "  → PDF" -ForegroundColor DarkGray
    $argumentos = @(
        (Join-Path $BuildPath 'print-pdf.mjs')
        "--html=$htmlPath"
        "--pdf=$pdfPath"
        "--footer=$($Manual.Footer)"
    )
    if ($Manual.ContainsKey('Margen')) { $argumentos += "--margen=$($Manual.Margen)" }

    $salida = & node @argumentos

    if ($LASTEXITCODE -ne 0) {
        throw "Falló la impresión de '$($Manual.Id)': $salida"
    }

    if (-not $KeepHtml) { Remove-Item -LiteralPath $htmlPath -Force }

    $tamano = [math]::Round((Get-Item -LiteralPath $pdfPath).Length / 1KB)
    Write-Host "  $($Manual.Output) — $tamano KB" -ForegroundColor Green
}

# ─────────────────────────────────────────────────────────────────────────────

Assert-Herramientas

$seleccion = if ($Only) {
    $encontrado = @($Manuales | Where-Object { $_.Id -eq $Only })
    if ($encontrado.Count -eq 0) {
        throw "No hay ningún manual con el identificador '$Only'. Disponibles: $(($Manuales | ForEach-Object { $_.Id }) -join ', ')"
    }
    $encontrado
} else { $Manuales }

foreach ($manual in $seleccion) {
    Write-Host ""
    Write-Host $manual.Title -ForegroundColor Cyan
    New-Manual -Manual $manual
}

Write-Host ""
Write-Host "Manuales en $ManualesPath" -ForegroundColor Green
