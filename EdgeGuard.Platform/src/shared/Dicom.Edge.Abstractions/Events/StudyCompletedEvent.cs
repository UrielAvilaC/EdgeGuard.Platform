using Dicom.Edge.Abstractions.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Events
{
    public sealed record StudyCompletedEvent(
     StudyContext Study
 ) : EdgeEventBase;
}
