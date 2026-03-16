using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Dicom.Edge.Security.Cryptography
{
    public class HashService : IHashService
    {
        public string Hash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
