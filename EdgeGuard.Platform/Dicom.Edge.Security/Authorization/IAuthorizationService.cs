using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Authorization
{
    public interface IAuthorizationService
    {
        public bool HasPermission(Role role, Permission permission);
    }
}
