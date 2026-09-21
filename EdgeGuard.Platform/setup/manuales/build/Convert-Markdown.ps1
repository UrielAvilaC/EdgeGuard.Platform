#Requires -Version 5.1
<#
.SYNOPSIS
    Convierte el Markdown de los manuales a HTML de impresión.

.DESCRIPTION
    Cubre exactamente el subconjunto de Markdown que usan los manuales —
    encabezados, párrafos, tablas, citas, listas, casillas, bloques de código,
    reglas y formato en línea— y nada más. No es un conversor de propósito
    general y no pretende serlo: un conversor completo traería dependencias, y
    el paquete de entrega tiene que poder regenerarse en el servidor del cliente
    sin instalar nada.

    Los identificadores de los encabezados se generan con la misma regla que
    GitHub, porque los índices de los documentos ya están escritos con esas
    anclas (`#7-fase-t0--verificación-funcional`) y deben seguir funcionando
    dentro del PDF.
#>

Set-StrictMode -Version Latest

# ─────────────────────────────────────────────────────────────────────────────
# Utilidades
# ─────────────────────────────────────────────────────────────────────────────

function ConvertTo-HtmlEscaped {
    param([string]$Text)
    return ($Text -replace '&', '&amp;' -replace '<', '&lt;' -replace '>', '&gt;')
}

<#
.SYNOPSIS
    Ancla de encabezado con la regla de GitHub.
.DESCRIPTION
    Minúsculas, se elimina la puntuación, los espacios pasan a guiones y los
    acentos se conservan. "7. Fase T+0 — Verificación funcional" produce
    "7-fase-t0--verificación-funcional": el guion doble sale de los espacios que
    rodeaban la raya, y así está escrito en los índices existentes.
#>
function ConvertTo-HeadingSlug {
    param([string]$Text)

    $s = $Text.ToLowerInvariant()
    $s = $s -replace '`', ''
    $s = $s -replace '\*\*|\*|__|_', ''
    # Fuera todo lo que no sea letra, dígito, espacio o guion. \p{L} conserva
    # los acentos, que es justo lo que hace GitHub.
    $s = [regex]::Replace($s, '[^\p{L}\p{Nd} \-]', '')
    $s = $s -replace ' ', '-'
    return $s
}

<#
.SYNOPSIS
    Formato en línea: código, negritas, cursivas y enlaces.
.DESCRIPTION
    El código en línea se extrae antes que nada y se sustituye por un marcador
    inerte, para que un asterisco dentro de `*.zip` no se convierta en cursiva.
#>
function ConvertTo-InlineHtml {
    param([string]$Text)

    $codes = New-Object System.Collections.ArrayList
    $work = [regex]::Replace($Text, '`([^`]+)`', {
        param($m)
        $null = $codes.Add($m.Groups[1].Value)
        "$([char]0x1)$($codes.Count - 1)$([char]0x2)"
    })

    $work = ConvertTo-HtmlEscaped $work

    # Enlaces antes que el resto: el texto del enlace admite formato, la URL no.
    $work = [regex]::Replace($work, '\[([^\]]+)\]\(([^)\s]+)\)', {
        param($m)
        $href = $m.Groups[2].Value
        $label = $m.Groups[1].Value
        "<a href=""$href"">$label</a>"
    })

    $work = [regex]::Replace($work, '\*\*([^*]+)\*\*', '<strong>$1</strong>')
    $work = [regex]::Replace($work, '(?<![\w*])\*([^*\n]+)\*(?![\w*])', '<em>$1</em>')

    # Guion largo y comillas ya vienen en el original; solo se restituye el código.
    for ($i = 0; $i -lt $codes.Count; $i++) {
        $escaped = ConvertTo-HtmlEscaped $codes[$i]
        $work = $work.Replace("$([char]0x1)$i$([char]0x2)", "<code>$escaped</code>")
    }

    return $work
}

# ─────────────────────────────────────────────────────────────────────────────
# Bloques
# ─────────────────────────────────────────────────────────────────────────────

<#
.SYNOPSIS
    Convierte una fila de tabla en celdas, respetando los pipes escapados.
#>
function Split-TableRow {
    param([string]$Line)

    $trimmed = $Line.Trim()
    $trimmed = $trimmed -replace '^\|', '' -replace '\|$', ''
    return @($trimmed -split '(?<!\\)\|' | ForEach-Object { ($_ -replace '\\\|', '|').Trim() })
}

function Test-TableSeparator {
    param([string]$Line)
    return ($Line -match '^\s*\|?[\s:|-]+\|[\s:|-]*$' -and $Line -match '-')
}

<#
.SYNOPSIS
    Motor principal: recibe líneas de Markdown y devuelve HTML.
.DESCRIPTION
    Es recursivo para las citas, que pueden contener párrafos, listas y tablas.
    Los encabezados encontrados se acumulan en $Headings para armar el índice.
#>
function Close-MarkdownList {
    param([Parameter(Mandatory)][hashtable]$State)
    if ($State.ItemOpen) { [void]$State.Html.Append("</li>`n"); $State.ItemOpen = $false }
    if ($State.ListKind) { [void]$State.Html.Append("</$($State.ListKind)>`n"); $State.ListKind = $null }
}

function Convert-MarkdownBlocks {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][AllowEmptyString()][string[]]$Lines,
        [System.Collections.ArrayList]$Headings
    )

    # El estado vive en una tabla local, no en el ámbito del script: esta
    # función se llama a sí misma para el contenido de las citas, y un estado
    # compartido haría que la llamada interna pisara la lista de la externa.
    $st = @{
        Html     = New-Object System.Text.StringBuilder
        ListKind = $null   # 'ul' | 'ol' | $null
        ItemOpen = $false
    }
    $i = 0

    while ($i -lt $Lines.Count) {
        $line = $Lines[$i]

        # ── Línea en blanco ──────────────────────────────────────────────────
        if ($line -match '^\s*$') {
            # Una lista sobrevive a una línea en blanco solo si la siguiente
            # línea sigue siendo un elemento suyo o un bloque de código.
            if ($st.ListKind) {
                $next = if ($i + 1 -lt $Lines.Count) { $Lines[$i + 1] } else { '' }
                if ($next -notmatch '^\s*([-*]|\d+\.)\s' -and $next -notmatch '^```') { Close-MarkdownList -State $st }
            }
            $i++
            continue
        }

        # ── Bloque de código ─────────────────────────────────────────────────
        if ($line -match '^\s*```') {
            $lang = ($line -replace '^\s*```', '').Trim()
            $i++
            $buffer = New-Object System.Collections.ArrayList
            while ($i -lt $Lines.Count -and $Lines[$i] -notmatch '^\s*```') {
                $null = $buffer.Add($Lines[$i]); $i++
            }
            $i++  # cierre
            $code = ConvertTo-HtmlEscaped (($buffer -join "`n"))
            $cls = if ($lang) { " class=""lang-$lang""" } else { '' }
            # Dentro de una lista, el bloque pertenece al elemento en curso.
            [void]$st.Html.Append("<pre$cls><code>$code</code></pre>`n")
            continue
        }

        # ── Regla horizontal ─────────────────────────────────────────────────
        if ($line -match '^\s*(---+|\*\*\*+)\s*$') {
            Close-MarkdownList -State $st
            [void]$st.Html.Append("<hr />`n")
            $i++
            continue
        }

        # ── Encabezado ───────────────────────────────────────────────────────
        if ($line -match '^(#{1,6})\s+(.*)$') {
            Close-MarkdownList -State $st
            $level = $Matches[1].Length
            $text = $Matches[2].Trim()
            $slug = ConvertTo-HeadingSlug $text
            if ($null -ne $Headings) { $null = $Headings.Add([pscustomobject]@{ Level = $level; Text = $text; Slug = $slug }) }
            $inner = ConvertTo-InlineHtml $text
            [void]$st.Html.Append("<h$level id=""$slug"">$inner</h$level>`n")
            $i++
            continue
        }

        # ── Cita ─────────────────────────────────────────────────────────────
        if ($line -match '^>\s?') {
            Close-MarkdownList -State $st
            $buffer = New-Object System.Collections.ArrayList
            while ($i -lt $Lines.Count -and $Lines[$i] -match '^>\s?') {
                $null = $buffer.Add(($Lines[$i] -replace '^>\s?', ''))
                $i++
            }
            $inner = Convert-MarkdownBlocks -Lines @($buffer) -Headings $null
            [void]$st.Html.Append("<blockquote>`n$inner</blockquote>`n")
            continue
        }

        # ── Tabla ────────────────────────────────────────────────────────────
        if ($line -match '^\s*\|' -and ($i + 1) -lt $Lines.Count -and (Test-TableSeparator $Lines[$i + 1])) {
            Close-MarkdownList -State $st
            $header = Split-TableRow $line
            $i += 2
            [void]$st.Html.Append("<table>`n<thead><tr>")
            foreach ($cell in $header) { [void]$st.Html.Append("<th>$(ConvertTo-InlineHtml $cell)</th>") }
            [void]$st.Html.Append("</tr></thead>`n<tbody>`n")
            while ($i -lt $Lines.Count -and $Lines[$i] -match '^\s*\|') {
                $cells = Split-TableRow $Lines[$i]
                [void]$st.Html.Append('<tr>')
                foreach ($cell in $cells) { [void]$st.Html.Append("<td>$(ConvertTo-InlineHtml $cell)</td>") }
                [void]$st.Html.Append("</tr>`n")
                $i++
            }
            [void]$st.Html.Append("</tbody>`n</table>`n")
            continue
        }

        # ── Listas ───────────────────────────────────────────────────────────
        if ($line -match '^\s*[-*]\s+(.*)$') {
            $text = $Matches[1]
            if ($st.ListKind -ne 'ul') { Close-MarkdownList -State $st; [void]$st.Html.Append("<ul>`n"); $st.ListKind = 'ul' }
            elseif ($st.ItemOpen) { [void]$st.Html.Append("</li>`n"); $st.ItemOpen = $false }

            # Casilla de verificación: se dibuja como casilla, no como viñeta.
            if ($text -match '^\[( |x|X)\]\s*(.*)$') {
                $mark = if ($Matches[1] -eq ' ') { '&#9744;' } else { '&#9745;' }
                $body = ConvertTo-InlineHtml $Matches[2]
                [void]$st.Html.Append("<li class=""casilla""><span class=""caja"">$mark</span>$body")
            }
            else {
                [void]$st.Html.Append("<li>$(ConvertTo-InlineHtml $text)")
            }
            $st.ItemOpen = $true
            $i++
            continue
        }

        if ($line -match '^\s*(\d+)\.\s+(.*)$') {
            $start = $Matches[1]
            $text = $Matches[2]
            if ($st.ListKind -ne 'ol') {
                Close-MarkdownList -State $st
                $attr = if ($start -ne '1') { " start=""$start""" } else { '' }
                [void]$st.Html.Append("<ol$attr>`n")
                $st.ListKind = 'ol'
            }
            elseif ($st.ItemOpen) { [void]$st.Html.Append("</li>`n"); $st.ItemOpen = $false }
            [void]$st.Html.Append("<li>$(ConvertTo-InlineHtml $text)")
            $st.ItemOpen = $true
            $i++
            continue
        }

        # ── Párrafo ──────────────────────────────────────────────────────────
        $buffer = New-Object System.Collections.ArrayList
        while ($i -lt $Lines.Count -and
               $Lines[$i] -notmatch '^\s*$' -and
               $Lines[$i] -notmatch '^\s*```' -and
               $Lines[$i] -notmatch '^#{1,6}\s' -and
               $Lines[$i] -notmatch '^>\s?' -and
               $Lines[$i] -notmatch '^\s*\|' -and
               $Lines[$i] -notmatch '^\s*[-*]\s+' -and
               $Lines[$i] -notmatch '^\s*\d+\.\s+' -and
               $Lines[$i] -notmatch '^\s*(---+|\*\*\*+)\s*$') {
            $null = $buffer.Add($Lines[$i].Trim())
            $i++
        }

        if ($buffer.Count -gt 0) {
            $text = ConvertTo-InlineHtml ($buffer -join ' ')
            if ($st.ItemOpen) {
                # Continuación floja de un elemento de lista.
                [void]$st.Html.Append(" $text")
            }
            else {
                Close-MarkdownList -State $st
                [void]$st.Html.Append("<p>$text</p>`n")
            }
        }
    }

    Close-MarkdownList -State $st
    return $st.Html.ToString()
}

<#
.SYNOPSIS
    Arma el documento completo: portada, índice y cuerpo, sobre la plantilla.
#>
function ConvertTo-ManualHtml {
    [CmdletBinding()]
    param(
        # Varios archivos se concatenan en el orden dado: los manuales largos se
        # escriben por partes y se arman aquí.
        [Parameter(Mandatory)][string[]]$MarkdownPath,
        [Parameter(Mandatory)][string]$TemplatePath,
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][string]$Title,
        [string]$Subtitle = '',
        [string]$Version = '',
        [string]$IssueDate = '',
        [string]$CoverNote = '',
        [string[]]$SkipSections = @()
    )

    $partes = foreach ($ruta in $MarkdownPath) {
        if (-not (Test-Path -LiteralPath $ruta)) { throw "No existe el documento fuente: $ruta" }
        Get-Content -LiteralPath $ruta -Raw -Encoding UTF8
    }
    $raw = ($partes -join "`n`n")
    $lines = @($raw -split "`r?`n")

    # El H1 del archivo pasa a ser la portada; el cuerpo empieza después.
    if ($lines.Count -gt 0 -and $lines[0] -match '^#\s+') { $lines = $lines[1..($lines.Count - 1)] }

    # Apartados que no van al PDF. El caso real es el índice escrito a mano:
    # el manual genera el suyo a partir de los encabezados, y dos índices
    # seguidos solo invitan a que uno de los dos se quede atrás.
    if ($SkipSections.Count -gt 0) {
        $filtradas = New-Object System.Collections.ArrayList
        $saltando = $false
        foreach ($l in $lines) {
            if ($l -match '^##\s+(.*)$') {
                $titulo = $Matches[1].Trim()
                $saltando = ($SkipSections -contains $titulo)
            }
            if (-not $saltando) { $null = $filtradas.Add($l) }
        }

        # El apartado retirado deja su regla horizontal huérfana junto a la del
        # siguiente; se colapsan las reglas consecutivas.
        $limpias = New-Object System.Collections.ArrayList
        $ultimaRegla = $false
        foreach ($l in $filtradas) {
            $esRegla = $l -match '^\s*---+\s*$'
            if ($esRegla -and $ultimaRegla) { continue }
            if ($l -notmatch '^\s*$') { $ultimaRegla = $esRegla }
            $null = $limpias.Add($l)
        }

        $lines = @($limpias)
    }

    $headings = New-Object System.Collections.ArrayList
    $body = Convert-MarkdownBlocks -Lines $lines -Headings $headings

    # Índice a dos niveles. El segundo nivel no es adorno: en el manual de
    # diagnóstico, cada `###` es un síntoma, y el índice es la forma en que
    # alguien encuentra el suyo sin leer el manual entero.
    $toc = New-Object System.Text.StringBuilder
    foreach ($h in $headings) {
        if ($h.Level -notin @(2, 3)) { continue }
        $label = ConvertTo-InlineHtml $h.Text
        $clase = if ($h.Level -eq 2) { 'principal' } else { 'secundario' }
        [void]$toc.Append("<li class=""$clase""><a href=""#$($h.Slug)"">$label</a></li>`n")
    }

    $template = Get-Content -LiteralPath $TemplatePath -Raw -Encoding UTF8
    $html = $template.
        Replace('{{TITULO}}', [System.Net.WebUtility]::HtmlEncode($Title)).
        Replace('{{SUBTITULO}}', [System.Net.WebUtility]::HtmlEncode($Subtitle)).
        Replace('{{VERSION}}', [System.Net.WebUtility]::HtmlEncode($Version)).
        Replace('{{FECHA}}', [System.Net.WebUtility]::HtmlEncode($IssueDate)).
        Replace('{{NOTA_PORTADA}}', $CoverNote).
        Replace('{{INDICE}}', $toc.ToString()).
        Replace('{{CUERPO}}', $body)

    $dir = Split-Path -Parent $OutputPath
    if ($dir -and -not (Test-Path -LiteralPath $dir)) { $null = New-Item -ItemType Directory -Path $dir -Force }

    # UTF-8 sin BOM: el BOM en un archivo servido por file:// hace que Chromium
    # ignore el <meta charset> en algunos casos.
    [System.IO.File]::WriteAllText($OutputPath, $html, (New-Object System.Text.UTF8Encoding($false)))

    return [pscustomobject]@{
        Output   = $OutputPath
        Sections = @($headings | Where-Object { $_.Level -eq 2 }).Count
        Headings = $headings.Count
    }
}
