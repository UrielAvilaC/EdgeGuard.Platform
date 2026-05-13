using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = default!;

        public DateTime ExpiresAt { get; set; }
    }
}
