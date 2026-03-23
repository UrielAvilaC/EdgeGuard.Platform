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
    
    $hl7Message = "MSH|^~\&|TestApp|TestFacility|EdgeGuard|EdgeGuard|$timestamp||ADT^A01|$messageId|P|2.5`r"
    $hl7Message += "EVN||$timestamp|||`r"
    $hl7Message += "PID|1||12345678||DOE^JOHN^A||19800101|M|||123 MAIN ST^^ANYTOWN^CA^12345||555-1234|||S||999999999|`r"
    $hl7Message += "PV1||I|ICU^101^1||||1234^SMITH^JOHN^MD|||||||||`r"
    
    Write-Host "Mensaje HL7 a enviar:" -ForegroundColor Green
    Write-Host $hl7Message.Replace("`r", "`r`n") -ForegroundColor Gray
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
        Write-Host "✓ ACK recibido:" -ForegroundColor Green
        Write-Host $ack.Replace("`r", "`r`n") -ForegroundColor Gray
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
