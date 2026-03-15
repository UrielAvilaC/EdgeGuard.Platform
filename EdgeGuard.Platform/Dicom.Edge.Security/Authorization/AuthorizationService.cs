using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Authorization
{
    public class AuthorizationService : IAuthorizationService
    {
        public bool HasPermission(Role role, Permission permission)
        {
            if (role == Role.Admin)
                return true;

            if (role == Role.Operator &&
                permission == Permission.ViewStudies)
                return true;

            return false;
        }
    }
}
