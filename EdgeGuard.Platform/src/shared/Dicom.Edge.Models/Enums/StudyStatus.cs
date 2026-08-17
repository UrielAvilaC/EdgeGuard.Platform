using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Enums
{
    public enum StudyStatus
    {
        Scheduled,            // agendado desde HL7 worklist (aún sin imágenes)
        Receiving,            // imágenes llegando
        Completed,            // imágenes recibidas; en espera de resultados (liga/reporte)
        WaitingForImageLinks, // tiene reporte, falta la liga de imágenes
        WaitingForReport,     // tiene liga de imágenes, falta el reporte
        Finalized,            // tiene liga de imágenes Y reporte
        QueuedForSend,        // listo para enviar al PACS
        Sending,              // enviando al PACS
        SentToPacs,           // enviado correctamente
        Failed                // error
    }
}
