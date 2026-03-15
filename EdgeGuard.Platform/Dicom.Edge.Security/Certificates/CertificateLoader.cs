using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Dicom.Edge.Security.Certificates
{
    public class CertificateLoader
    {
        public X509Certificate2 Load(string path, string password)
        {
            return new X509Certificate2(path, password);
        }
    }
}
