# Envía múltiples mensajes HL7 de forma concurrente para probar el manejo de concurrencia

param(
    [string]$Server = "localhost",
    [int]$Port = 2575,
    [int]$MessageCount = 10,
    [int]$ConcurrentConnections = 5
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   HL7 Concurrent Load Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Target: $Server`:$Port" -ForegroundColor Yellow
Write-Host "Messages: $MessageCount" -ForegroundColor Yellow
Write-Host "Concurrent: $ConcurrentConnections" -ForegroundColor Yellow
Write-Host ""

$jobs = @()
$successCount = 0
$failCount = 0

$scriptBlock = {
    param($Server, $Port, $MessageId)
    
    try {
        $timestamp = Get-Date -Format "yyyyMMddHHmmss"
        $controlId = [guid]::NewGuid().ToString("N").Substring(0, 10).ToUpper()
        
        $hl7Message = "MSH|^~\&|TestApp|TestFacility|EdgeGuard|EdgeGuard|$timestamp||ADT^A01|$controlId|P|2.5`r"
        $hl7Message += "PID|1||$MessageId||DOE^JOHN^A||19800101|M|||123 MAIN ST^^ANYTOWN^CA^12345||555-1234|||S||999999999|`r"
        
        $client = New-Object System.Net.Sockets.TcpClient($Server, $Port)
        $stream = $client.GetStream()
        
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($hl7Message)
        $stream.Write($bytes, 0, $bytes.Length)
        
        $buffer = New-Object byte[] 1024
        $bytesRead = $stream.Read($buffer, 0, $buffer.Length)
        $ack = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $bytesRead)
        
        $stream.Close()
        $client.Close()
        
        return @{
            Success = $true
            MessageId = $MessageId
            ControlId = $controlId
            Ack = $ack
        }
    }
    catch {
        return @{
            Success = $false
            MessageId = $MessageId
            Error = $_.Exception.Message
        }
    }
}

Write-Host "Enviando $MessageCount mensajes..." -ForegroundColor Yellow
Write-Host ""

for ($i = 1; $i -le $MessageCount; $i++) {
    $messageId = "TEST-$($i.ToString('000'))"
    
    $job = Start-Job -ScriptBlock $scriptBlock -ArgumentList $Server, $Port, $messageId
    $jobs += $job
    
    Write-Host "[$i/$MessageCount] Mensaje $messageId enviado..." -ForegroundColor Gray
    
    # Limitar conexiones concurrentes
    if (($jobs | Where-Object { $_.State -eq 'Running' }).Count -ge $ConcurrentConnections) {
        $null = Wait-Job -Job $jobs -Any
    }
    
    Start-Sleep -Milliseconds 100
}

Write-Host ""
Write-Host "Esperando respuestas..." -ForegroundColor Yellow

$null = Wait-Job -Job $jobs

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Resultados:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

foreach ($job in $jobs) {
    $result = Receive-Job -Job $job
    
    if ($result.Success) {
        $successCount++
        Write-Host "✓ $($result.MessageId) - ACK recibido" -ForegroundColor Green
    }
    else {
        $failCount++
        Write-Host "✗ $($result.MessageId) - Error: $($result.Error)" -ForegroundColor Red
    }
    
    Remove-Job -Job $job
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total: $MessageCount | Exitosos: $successCount | Fallidos: $failCount" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Yellow" })
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para ver los mensajes procesados:" -ForegroundColor Yellow
Write-Host "  GET http://localhost:5000/api/hl7status/recent-messages" -ForegroundColor White
