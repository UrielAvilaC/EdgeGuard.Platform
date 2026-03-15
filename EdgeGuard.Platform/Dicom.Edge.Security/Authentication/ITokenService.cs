using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Authentication
{
    public interface ITokenService
    {
        string GenerateToken(string userId, string role);

        bool ValidateToken(string token);
    }
}
