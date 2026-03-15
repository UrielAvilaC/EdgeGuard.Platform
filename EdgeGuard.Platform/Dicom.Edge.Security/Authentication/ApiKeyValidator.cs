using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Authentication
{
    public class ApiKeyValidator
    {
        private readonly HashSet<string> _validKeys = [];

        public bool Validate(string apiKey)
        {
            return _validKeys.Contains(apiKey);
        }
    }
}
