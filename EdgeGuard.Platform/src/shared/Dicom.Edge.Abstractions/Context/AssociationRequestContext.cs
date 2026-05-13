using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Context
{
    public class AssociationRequestContext
    {
        public string CallingAeTitle { get; set; } = default!;
        public string CalledAeTitle { get; set; } = default!;
        public string Host { get; set; } = default!;
    }
}
