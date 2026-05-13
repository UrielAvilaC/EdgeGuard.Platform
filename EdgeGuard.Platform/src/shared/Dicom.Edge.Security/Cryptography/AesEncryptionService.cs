using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Dicom.Edge.Security.Cryptography
{
    public class AesEncryptionService : IAesEnryptionService
    {
        public byte[] Encrypt(string plainText, byte[] key, byte[]? iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            if (iv != null)
            {
                aes.IV = iv;
            }
            else
            {
                aes.GenerateIV();
            }
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return cipherBytes;
        }
    }
}
