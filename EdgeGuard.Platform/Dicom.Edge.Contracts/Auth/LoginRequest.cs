using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Auth
{
    public class LoginRequest
    {
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
