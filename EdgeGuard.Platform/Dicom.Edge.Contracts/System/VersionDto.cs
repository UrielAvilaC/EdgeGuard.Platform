using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.System
{
    public class VersionDto
    {
        public string Version { get; set; } = default!;

        public string Build { get; set; } = default!;

        public DateTime BuildDate { get; set; }
    }
}
