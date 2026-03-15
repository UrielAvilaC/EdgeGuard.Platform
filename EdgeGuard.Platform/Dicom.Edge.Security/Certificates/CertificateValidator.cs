using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Dicom.Edge.Security.Certificates
{
    public class CertificateValidator
    {
        public bool Validate(X509Certificate2 cert)
        {
            return cert.NotAfter > DateTime.UtcNow;
        }
    }
}
