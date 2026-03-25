# Test HL7 Listener
# Este script envía un mensaje HL7 de prueba al listener

param(
    [string]$Server = "localhost",
    [int]$Port = 2575
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   HL7 Listener Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Target: $Server`:$Port" -ForegroundColor Yellow
Write-Host ""

try {
    # Crear mensaje HL7 de prueba (ADT^A01 - Patient Admission)
    $timestamp = Get-Date -Format "yyyyMMddHHmmss"
    $messageId = [guid]::NewGuid().ToString("N").Substring(0, 10).ToUpper()

    # Mensaje HL7 sin delimitadores de envelope (el listener debe aceptar ambos formatos)
    $hl7MessageContent = "MSH|^~\&|TestApp|TestFacility|EdgeGuard|EdgeGuard|$timestamp||ADT^A01|$messageId|P|2.5`r"
    $hl7MessageContent += "EVN||$timestamp|||`r"
    $hl7MessageContent += "PID|1||12345678||DOE^JOHN^A||19800101|M|||123 MAIN ST^^ANYTOWN^CA^12345||555-1234|||S||999999999|`r"
    $hl7MessageContent += "PV1||I|ICU^101^1||||1234^SMITH^JOHN^MD|||||||||`r"

    # Envolver con delimitadores HL7 estándar: <VT>mensaje<FS><CR>
    $VT = [char]0x0B
    $FS = [char]0x1C
    $hl7Message = "$VT$hl7MessageContent$FS`r"

    Write-Host "Mensaje HL7 a enviar:" -ForegroundColor Green
    Write-Host $hl7MessageContent.Replace("`r", "`r`n") -ForegroundColor Gray
    Write-Host "Control ID: $messageId" -ForegroundColor Cyan
    Write-Host ""
    
    # Conectar al servidor
    Write-Host "Conectando a $Server`:$Port..." -ForegroundColor Yellow
    $client = New-Object System.Net.Sockets.TcpClient($Server, $Port)
    $stream = $client.GetStream()
    
    Write-Host "✓ Conectado exitosamente" -ForegroundColor Green
    Write-Host ""
    
    # Enviar mensaje
    Write-Host "Enviando mensaje HL7..." -ForegroundColor Yellow
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($hl7Message)
    $stream.Write($bytes, 0, $bytes.Length)
    
    Write-Host "✓ Mensaje enviado ($($bytes.Length) bytes)" -ForegroundColor Green
    Write-Host ""
    
    # Esperar ACK
    Write-Host "Esperando ACK..." -ForegroundColor Yellow
    $buffer = New-Object byte[] 1024
    $bytesRead = $stream.Read($buffer, 0, $buffer.Length)

    if ($bytesRead -gt 0) {
        $ack = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $bytesRead)
        Write-Host "✓ ACK recibido ($bytesRead bytes):" -ForegroundColor Green

        # Mostrar ACK con caracteres de control visibles
        $displayAck = $ack.Replace("`r", "[CR]`r`n").Replace("`n", "[LF]").Replace([char]0x0B, "[VT]").Replace([char]0x1C, "[FS]")
        Write-Host $displayAck -ForegroundColor Gray

        # Validar formato HL7
        Write-Host ""
        Write-Host "Validación del ACK:" -ForegroundColor Cyan

        if ($ack.StartsWith([char]0x0B)) {
            Write-Host "  ✓ Inicia con VT (Vertical Tab)" -ForegroundColor Green
        } else {
            Write-Host "  ✗ No inicia con VT" -ForegroundColor Red
        }

        if ($ack.Contains([char]0x1C)) {
            Write-Host "  ✓ Contiene FS (File Separator)" -ForegroundColor Green
        } else {
            Write-Host "  ✗ No contiene FS" -ForegroundColor Red
        }

        if ($ack.Contains("MSH|")) {
            Write-Host "  ✓ Contiene segmento MSH" -ForegroundColor Green
        } else {
            Write-Host "  ✗ No contiene segmento MSH" -ForegroundColor Red
        }

        if ($ack.Contains("MSA|")) {
            Write-Host "  ✓ Contiene segmento MSA" -ForegroundColor Green
        } else {
            Write-Host "  ✗ No contiene segmento MSA" -ForegroundColor Red
        }

        # Verificar que el MSA contiene el Message Control ID original
        if ($ack.Contains($messageId)) {
            Write-Host "  ✓ MSA contiene el Message Control ID original ($messageId)" -ForegroundColor Green
        } else {
            Write-Host "  ⚠ MSA no contiene el Message Control ID original" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "✗ No se recibió ACK" -ForegroundColor Red
    }
    
    # Cerrar conexión
    $stream.Close()
    $client.Close()
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Prueba completada exitosamente" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    
    Write-Host ""
    Write-Host "Para ver el mensaje procesado, consulta:" -ForegroundColor Yellow
    Write-Host "  GET http://localhost:5000/api/hl7status/recent-messages" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "✗ Error:" $_.Exception.Message -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Verifica que:" -ForegroundColor Yellow
    Write-Host "  1. La API esté ejecutándose" -ForegroundColor White
    Write-Host "  2. El listener esté habilitado en appsettings.json" -ForegroundColor White
    Write-Host "  3. El puerto $Port no esté bloqueado por el firewall" -ForegroundColor White
}
