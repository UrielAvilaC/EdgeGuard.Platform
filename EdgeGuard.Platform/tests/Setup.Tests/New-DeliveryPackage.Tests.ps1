#Requires -Version 5.1
# Pruebas de setup\common\New-DeliveryPackage.ps1 (Pester 3.4+, el que trae Windows).
#   Invoke-Pester .\tests\Setup.Tests

. (Join-Path $PSScriptRoot 'Setup.TestHelpers.ps1')

function Invoke-Entrega {
    param([string]$Raiz, [hashtable]$Extra = @{})

    $parametros = @{ Target = 'Node'; OutputPath = (Join-Path $Raiz 'salida') }
    foreach ($k in $Extra.Keys) { $parametros[$k] = $Extra[$k] }

    return Invoke-ScriptCapturandoError -Script (Join-Path $Raiz 'setup\common\New-DeliveryPackage.ps1') -Parametros $parametros
}

Describe 'New-DeliveryPackage: entrega válida' {

    $raiz = New-RepoConNodoSellado
    $error1 = Invoke-Entrega -Raiz $raiz
    $zip = Join-Path $raiz 'salida\edgeguard-node-9.9.9-instalador.zip'

    It 'termina sin error' {
        $error1 | Should BeNullOrEmpty
    }

    It 'escribe el zip de entrega y su .sha256' {
        Test-Path -LiteralPath $zip | Should Be $true
        Test-Path -LiteralPath "$zip.sha256" | Should Be $true
    }

    It 'el .sha256 lateral es el hash real del zip' {
        $linea = (Get-Content -LiteralPath "$zip.sha256" -Raw).Trim()
        $linea | Should Be ("{0}  edgeguard-node-9.9.9-instalador.zip" -f (Get-Sha256Archivo $zip))
    }

    It 'lleva el instalador, el paquete de esta versión, los manuales y el rastro del sellado' {
        $entradas = Get-EntradasZip $zip
        foreach ($esperada in @('install.ps1', 'README.md', 'node-install.example.psd1', 'bin/01-Paso.ps1',
                                'bin/EdgeGuard.Setup.psm1', 'data/edgeguard-node-9.9.9.zip',
                                'data/checksums.sha256', 'manuales/Nodo-Manual.pdf', 'version.json')) {
            ($entradas -contains $esperada) | Should Be $true
        }
    }

    It 'NO lleva la configuración real de la sede' {
        $entradas = Get-EntradasZip $zip
        ($entradas -contains 'node-install.psd1') | Should Be $false
        (($entradas | Where-Object { $_ -like '*-install.psd1' -and $_ -notlike '*.example.psd1' }).Count) | Should Be 0
    }

    It 'NO lleva versiones anteriores del paquete ni archivos ajenos de bin\' {
        $entradas = Get-EntradasZip $zip
        ($entradas -contains 'data/edgeguard-node-0.0.1.zip') | Should Be $false
        ($entradas -contains 'bin/notas.txt') | Should Be $false
    }

    It 'su checksums.sha256 declara solo lo que viaja, con el hash correcto' {
        $texto = Read-EntradaZip -Zip $zip -Entrada 'data/checksums.sha256'
        $hashPaquete = Get-Sha256Archivo (Join-Path $raiz 'setup\node\data\edgeguard-node-9.9.9.zip')
        $texto | Should Match ([regex]::Escape("$hashPaquete  edgeguard-node-9.9.9.zip"))
        $texto | Should Not Match '0\.0\.1'
    }

    Remove-DirectorioTemporal $raiz
}

Describe 'New-DeliveryPackage: DryRun' {

    $raiz = New-RepoConNodoSellado
    $mensaje = Invoke-Entrega -Raiz $raiz -Extra @{ DryRun = $true }

    It 'valida sin error' {
        $mensaje | Should BeNullOrEmpty
    }

    It 'no escribe el zip de entrega' {
        Test-Path -LiteralPath (Join-Path $raiz 'salida\edgeguard-node-9.9.9-instalador.zip') | Should Be $false
    }

    Remove-DirectorioTemporal $raiz
}

Describe 'New-DeliveryPackage: se niega cuando lo sellado no es de fiar' {

    It 'si el paquete cambió después del sellado' {
        $raiz = New-RepoConNodoSellado
        $ruta = Join-Path $raiz 'setup\node\data\edgeguard-node-9.9.9.zip'
        [System.IO.File]::WriteAllBytes($ruta, ([System.IO.File]::ReadAllBytes($ruta) + [byte[]](1, 2, 3)))
        (Invoke-Entrega -Raiz $raiz) | Should Match 'no coincide'
        Remove-DirectorioTemporal $raiz
    }

    It 'si la versión pedida no está sellada' {
        $raiz = New-RepoConNodoSellado
        (Invoke-Entrega -Raiz $raiz -Extra @{ Version = '1.2.3' }) | Should Match 'No existe edgeguard-node-1.2.3.zip'
        Remove-DirectorioTemporal $raiz
    }

    It 'si version.json salió de un árbol sucio' {
        $raiz = New-RepoConNodoSellado
        Set-Manifiesto -Raiz $raiz -Cambio { param($m) $m.arbolLimpio = $false }
        $mensaje = Invoke-Entrega -Raiz $raiz
        $mensaje | Should Match 'no es de fiar'
        $mensaje | Should Match 'cambios sin confirmar'
        Remove-DirectorioTemporal $raiz
    }

    It 'si version.json es de otra versión' {
        $raiz = New-RepoConNodoSellado
        Set-Manifiesto -Raiz $raiz -Cambio { param($m) $m.version = '1.0.0' }
        (Invoke-Entrega -Raiz $raiz) | Should Match 'es de la versión 1.0.0'
        Remove-DirectorioTemporal $raiz
    }

    It 'si version.json no incluye el paquete (sellado con otro -Target)' {
        $raiz = New-RepoConNodoSellado
        Set-Manifiesto -Raiz $raiz -Cambio { param($m) $m.paquetes = [pscustomobject]@{} }
        (Invoke-Entrega -Raiz $raiz) | Should Match 'no incluye el paquete Node'
        Remove-DirectorioTemporal $raiz
    }

    It 'si version.json registró otro hash para el paquete' {
        $raiz = New-RepoConNodoSellado
        Set-Manifiesto -Raiz $raiz -Cambio { param($m) $m.paquetes.Node.hashes.'edgeguard-node-9.9.9.zip' = ('0' * 64) }
        (Invoke-Entrega -Raiz $raiz) | Should Match 'no es el que version.json registró'
        Remove-DirectorioTemporal $raiz
    }

    It 'si la copia del módulo común en bin\ está desfasada' {
        $raiz = New-RepoConNodoSellado
        Write-ArchivoBom (Join-Path $raiz 'setup\node\bin\EdgeGuard.Setup.psm1') "# otra version`n"
        (Invoke-Entrega -Raiz $raiz) | Should Match 'idéntico'
        Remove-DirectorioTemporal $raiz
    }

    It 'si un script que viaja no tiene BOM' {
        $raiz = New-RepoConNodoSellado
        Write-ArchivoSinBom (Join-Path $raiz 'setup\node\bin\01-Paso.ps1') "# sin bom`n"
        (Invoke-Entrega -Raiz $raiz) | Should Match 'BOM'
        Remove-DirectorioTemporal $raiz
    }

    It 'si un script que viaja no parsea' {
        $raiz = New-RepoConNodoSellado
        Write-ArchivoBom (Join-Path $raiz 'setup\node\bin\01-Paso.ps1') "function { `n"
        (Invoke-Entrega -Raiz $raiz) | Should Match 'no parsea'
        Remove-DirectorioTemporal $raiz
    }
}

Describe 'New-DeliveryPackage: -AllowUnsealed' {

    $raiz = New-RepoConNodoSellado
    Set-Manifiesto -Raiz $raiz -Cambio { param($m) $m.arbolLimpio = $false }
    $mensaje = Invoke-Entrega -Raiz $raiz -Extra @{ AllowUnsealed = $true }
    $zip = Join-Path $raiz 'salida\edgeguard-node-9.9.9-instalador.zip'

    It 'permite ensayar con un sellado sucio' {
        $mensaje | Should BeNullOrEmpty
        Test-Path -LiteralPath $zip | Should Be $true
    }

    It 'pero no mete version.json: un rastro falso es peor que ninguno' {
        ((Get-EntradasZip $zip) -contains 'version.json') | Should Be $false
    }

    Remove-DirectorioTemporal $raiz
}

Describe 'New-DeliveryPackage: auditoría del contenido' {

    It 'rechaza un certificado dentro del paquete sellado' {
        $raiz = New-RepoConNodoSellado -ExtraEnPaquete 'cliente.pfx'
        # El paquete se regenera con el extra: hay que volver a firmarlo
        $zip = Join-Path $raiz 'setup\node\data\edgeguard-node-9.9.9.zip'
        $hash = Get-Sha256Archivo $zip
        Write-ArchivoSinBom (Join-Path $raiz 'setup\node\data\checksums.sha256') "$hash  edgeguard-node-9.9.9.zip`n"
        Set-Manifiesto -Raiz $raiz -Cambio { param($m) $m.paquetes.Node.hashes.'edgeguard-node-9.9.9.zip' = $hash }

        $mensaje = Invoke-Entrega -Raiz $raiz
        $mensaje | Should Match 'no deben llegar a un cliente'
        $mensaje | Should Match 'cliente\.pfx'
        Remove-DirectorioTemporal $raiz
    }
}
