using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Cryptography
{
    public interface IAesEnryptionService
    {
        public byte[] Encrypt(string plainText, byte[] key, byte[]? iv);
    }
}
