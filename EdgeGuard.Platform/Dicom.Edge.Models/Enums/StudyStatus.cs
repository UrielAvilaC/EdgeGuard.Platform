using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Enums
{
    public enum StudyStatus
    {
        Receiving,       // imágenes llegando
        Completed,       // estudio completo
        QueuedForSend,   // listo para enviar al PACS
        Sending,         // enviando al PACS
        SentToPacs,      // enviado correctamente
        Failed           // error
    }
}
