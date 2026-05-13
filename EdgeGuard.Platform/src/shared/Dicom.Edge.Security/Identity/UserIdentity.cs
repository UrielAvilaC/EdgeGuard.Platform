using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Identity
{
    public class UserIdentity
    {
        public string UserId { get; set; } = default!;

        public string Username { get; set; } = default!;

        public IReadOnlyList<string> Roles { get; set; } = [];
    }
}
