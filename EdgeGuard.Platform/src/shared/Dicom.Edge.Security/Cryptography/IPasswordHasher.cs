using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Cryptography
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }
}
