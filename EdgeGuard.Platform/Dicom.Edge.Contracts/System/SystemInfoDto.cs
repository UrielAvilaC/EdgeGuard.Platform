using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.System
{
    public class SystemInfoDto
    {
        public string Environment { get; set; } = default!;

        public string MachineName { get; set; } = default!;

        public string OsVersion { get; set; } = default!;

        public int ProcessorCount { get; set; }
    }
}
