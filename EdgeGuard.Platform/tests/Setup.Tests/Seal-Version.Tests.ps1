#Requires -Version 5.1
# Pruebas de las precondiciones de -Tag en setup\common\Seal-Version.ps1.
#   Invoke-Pester .\tests\Setup.Tests
#
# Todas con -DryRun: ejercitan la validación real sin compilar ni empaquetar. Lo que
# NO cubren: que version.json salga con arbolLimpio = true tras un sellado completo,
# porque eso exige dotnet publish self-contained (descarga el runtime). Se verifica
# a mano en el sellado de una versión.

. (Join-Path $PSScriptRoot 'Setup.TestHelpers.ps1')

function Invoke-Sellado {
    param([string]$Raiz, [hashtable]$Parametros)

    return Invoke-ScriptCapturandoError -Script (Join-Path $Raiz 'setup\common\Seal-Version.ps1') -Parametros $Parametros
}

Describe 'Seal-Version -Tag: precondiciones' {

    It 'rechaza etiquetar con el árbol sucio, ANTES de compilar' {
        $raiz = New-RepoSellado
        Add-Content -LiteralPath (Join-Path $raiz 'Directory.Build.props') -Value '<!-- cambio sin confirmar -->'
        $mensaje = Invoke-Sellado -Raiz $raiz -Parametros @{ Target = 'Node'; Tag = $true; DryRun = $true }
        $mensaje | Should Match 'cambios sin confirmar'
        Remove-DirectorioTemporal $raiz
    }

    It 'rechaza etiquetar un número que no está ya estampado y confirmado' {
        $raiz = New-RepoSellado
        $mensaje = Invoke-Sellado -Raiz $raiz -Parametros @{ Target = 'Node'; Version = '1.0.1'; Tag = $true; DryRun = $true }
        $mensaje | Should Match 'ya esté estampado'
        Remove-DirectorioTemporal $raiz
    }

    It 'rechaza una etiqueta que ya existe' {
        $raiz = New-RepoSellado
        & git -C $raiz tag -a v1.0.0 -m 'previa' | Out-Null
        $mensaje = Invoke-Sellado -Raiz $raiz -Parametros @{ Target = 'Node'; Tag = $true; DryRun = $true }
        $mensaje | Should Match "'v1.0.0' ya existe"
        Remove-DirectorioTemporal $raiz
    }

    It 'acepta árbol limpio, número ya estampado y etiqueta libre' {
        $raiz = New-RepoSellado
        $mensaje = Invoke-Sellado -Raiz $raiz -Parametros @{ Target = 'Node'; Tag = $true; DryRun = $true }
        $mensaje | Should BeNullOrEmpty
        Remove-DirectorioTemporal $raiz
    }

    It 'no exige árbol limpio cuando no se pide etiqueta (queda anotado en version.json)' {
        $raiz = New-RepoSellado
        Add-Content -LiteralPath (Join-Path $raiz 'Directory.Build.props') -Value '<!-- cambio sin confirmar -->'
        $mensaje = Invoke-Sellado -Raiz $raiz -Parametros @{ Target = 'Node'; DryRun = $true }
        $mensaje | Should BeNullOrEmpty
        Remove-DirectorioTemporal $raiz
    }

    It 'con -DryRun no crea la etiqueta' {
        $raiz = New-RepoSellado
        Invoke-Sellado -Raiz $raiz -Parametros @{ Target = 'Node'; Tag = $true; DryRun = $true } | Out-Null
        (& git -C $raiz tag --list 'v1.0.0') | Should BeNullOrEmpty
        Remove-DirectorioTemporal $raiz
    }
}
