using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Dicom.Edge.Security.Certificates
{
    public class CertificateLoader
    {
        public X509Certificate2 Load(string path, string password) => X509CertificateLoader.LoadPkcs12FromFile(path, password);
    }
}
