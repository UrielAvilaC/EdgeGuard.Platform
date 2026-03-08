using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Common.Guard
{
    public static class GuardClause
    {
        public static void AgainstNull(object? value, string name)
        {
            if (value is null)
                throw new ArgumentNullException(name);
        }

        public static void AgainstNullOrEmpty(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{name} cannot be empty.");
        }
    }
}
