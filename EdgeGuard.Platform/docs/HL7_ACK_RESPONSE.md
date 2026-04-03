# HL7 ACK Response - Implementación Correcta

## Resumen
Este documento explica cómo se implementa la respuesta ACK (acknowledgment) en el HL7 Listener siguiendo el estándar HL7 v2.x.

## Problemas Previos

La implementación anterior tenía varios problemas:

1. **Falta de delimitadores de envelope**: No se incluían los caracteres de control `\x0B` (VT) y `\x1C` (FS)
2. **MSA con ID incorrecto**: Se usaba el ID del ACK en lugar del ID del mensaje original
3. **Formato básico**: No seguía completamente el estándar HL7

## Implementación Actual

### Estructura del ACK

Un ACK válido según HL7 v2.x tiene la siguiente estructura:

```
<VT>MSH|^~\&|...|<CR>MSA|...|<CR><FS><CR>
```

Donde:
- `<VT>` = `\x0B` (Vertical Tab) - Inicio del mensaje
- `<CR>` = `\r` (Carriage Return) - Fin de segmento
- `<FS>` = `\x1C` (File Separator) - Fin del mensaje

### Segmentos

#### MSH (Message Header)
```
MSH|^~\&|EdgeGuardHub|EdgeGuard|{SendingApp}|{SendingFacility}|{Timestamp}||ACK|{ACK_ID}|P|2.5
```

Campos:
- **Field 1**: Separador de campos (`|`)
- **Field 2**: Caracteres de codificación (`^~\&`)
- **Field 3**: Aplicación que envía (EdgeGuardHub)
- **Field 4**: Institución que envía (EdgeGuard)
- **Field 5**: Aplicación que recibe (del mensaje original)
- **Field 6**: Institución que recibe (del mensaje original)
- **Field 7**: Timestamp (`yyyyMMddHHmmss`)
- **Field 9**: Tipo de mensaje (ACK)
- **Field 10**: ID único del ACK
- **Field 11**: ID de procesamiento (P = Production)
- **Field 12**: Versión HL7 (2.5)

#### MSA (Message Acknowledgment)
```
MSA|AA|{ORIGINAL_MESSAGE_ID}
```

Campos:
- **Field 1**: Código de ACK
  - `AA` = Application Accept (éxito)
  - `AE` = Application Error (error)
  - `AR` = Application Reject (rechazo)
- **Field 2**: Message Control ID del mensaje **original** (¡importante!)

## Código Implementado

```csharp
private string BuildHl7Ack(Hl7Message originalMessage)
{
    // Timestamp actual
    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
    
    // ID único para este ACK
    var ackMessageControlId = Guid.NewGuid().ToString("N")[..10].ToUpper();
    
    // Extraer el Message Control ID del mensaje original
    var originalMessageControlId = ExtractMessageControlId(originalMessage.Content) 
        ?? ackMessageControlId;

    // Construir segmentos del ACK
    var ackSegments = 
        $"MSH|^~\\&|EdgeGuardHub|EdgeGuard|{originalMessage.SendingApplication}|{originalMessage.SendingFacility}|{timestamp}||ACK|{ackMessageControlId}|P|2.5\r" +
        $"MSA|AA|{originalMessageControlId}\r";

    // Envolver con delimitadores HL7
    return $"\x0B{ackSegments}\x1C\r";
}

private string? ExtractMessageControlId(string hl7Message)
{
    try
    {
        // Limpiar caracteres de control
        var cleanMessage = hl7Message
            .Replace("\x0B", "")
            .Replace("\x1C", "")
            .Replace("\r", "")
            .Replace("\n", "");
        
        // El Message Control ID está en el campo 10 del MSH (índice 9)
        var mshSegment = cleanMessage.Split('|');
        if (mshSegment.Length > 9)
        {
            return mshSegment[9];
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to extract Message Control ID");
    }
    
    return null;
}
```

## Validación del ACK

Para validar que el ACK es correcto, verifica:

1. ✅ **Inicia con `\x0B` (VT)**
2. ✅ **Contiene segmento MSH**
3. ✅ **Contiene segmento MSA**
4. ✅ **Termina con `\x1C\r` (FS + CR)**
5. ✅ **MSA contiene el Message Control ID original**

## Script de Prueba

Usa el script actualizado:

```powershell
.\scripts\Test-Hl7Listener.ps1 -Server localhost -Port 2575
```

Este script ahora:
- Envía un mensaje HL7 con delimitadores correctos
- Valida el ACK recibido
- Muestra los caracteres de control visibles
- Verifica que el Message Control ID coincida

## Ejemplo de Salida

### Mensaje de Prueba
```
<VT>MSH|^~\&|TestApp|TestFacility|EdgeGuard|EdgeGuard|20240115120000||ADT^A01|ABC123DEF|P|2.5<CR>
EVN||20240115120000|||<CR>
PID|1||12345678||DOE^JOHN^A||19800101|M|||123 MAIN ST^^ANYTOWN^CA^12345||555-1234|||S||999999999|<CR>
PV1||I|ICU^101^1||||1234^SMITH^JOHN^MD|||||||||<CR><FS><CR>
```

### ACK Esperado
```
<VT>MSH|^~\&|EdgeGuardHub|EdgeGuard|TestApp|TestFacility|20240115120001||ACK|XYZ789ABC|P|2.5<CR>
MSA|AA|ABC123DEF<CR><FS><CR>
```

## Referencias

- [HL7 Version 2.5 Standard](http://www.hl7.org/implement/standards/product_brief.cfm?product_id=144)
- [HL7 ACK Message Documentation](http://www.hl7.org/documentcenter/public/wg/conf/HL7MSH.htm)
- [MLLP (Minimal Lower Layer Protocol)](https://www.hl7.org/documentcenter/public/wg/inm/mllp_transport_specification.PDF)

## Notas Importantes

1. **Message Control ID**: Siempre debe ser el del mensaje original en el segmento MSA
2. **Delimitadores**: Son obligatorios según el estándar MLLP
3. **Timing**: El ACK debe enviarse inmediatamente después de recibir el mensaje
4. **Character Encoding**: UTF-8 es el más común, pero algunos sistemas usan ASCII

## Soporte para Errores

Para enviar un ACK de error:

```csharp
// En caso de error de aplicación
$"MSA|AE|{originalMessageControlId}|Error: {errorDescription}\r"

// En caso de rechazo
$"MSA|AR|{originalMessageControlId}|Rejected: {rejectReason}\r"
```

Actualmente solo se implementa `AA` (Accept), pero se puede extender según necesidades.
