using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Enums
{
    public enum StudyStatus
    {
        Scheduled,       // agendado desde HL7 worklist (aún sin imágenes)
        Receiving,       // imágenes llegando
        Completed,       // estudio completo
        QueuedForSend,   // listo para enviar al PACS
        Sending,         // enviando al PACS
        SentToPacs,      // enviado correctamente
        Failed           // error
    }
}
