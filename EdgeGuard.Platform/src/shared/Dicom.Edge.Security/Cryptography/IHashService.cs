using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Cryptography
{
    public interface IHashService
    {
        string Hash(string input);
    }
}
