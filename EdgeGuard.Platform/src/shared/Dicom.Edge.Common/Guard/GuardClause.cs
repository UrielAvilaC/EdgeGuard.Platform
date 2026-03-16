using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Dicom.Edge.Common.Guard
{
    /// <summary>
    /// Provides guard clauses for validating method arguments and preventing invalid states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Guard clauses fail fast with descriptive exceptions when preconditions are not met.
    /// Use at the beginning of methods to validate inputs.
    /// </para>
    /// </remarks>
    public static class Guard
    {
        // ==================== Null Checks ====================

        /// <summary>
        /// Throws if value is null.
        /// </summary>
        public static T AgainstNull<T>([NotNull] T? value, string parameterName)
        {
            if (value is null)
                throw new ArgumentNullException(parameterName);
            return value;
        }

        /// <summary>
        /// Throws if string is null or empty.
        /// </summary>
        public static string AgainstNullOrEmpty([NotNull] string? value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"{parameterName} cannot be null or empty.", parameterName);
            return value;
        }

        /// <summary>
        /// Throws if string is null, empty, or whitespace.
        /// </summary>
        public static string AgainstNullOrWhiteSpace([NotNull] string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{parameterName} cannot be null, empty, or whitespace.", parameterName);
            return value;
        }

        // ==================== Numeric Ranges ====================

        /// <summary>
        /// Throws if value is negative.
        /// </summary>
        public static int AgainstNegative(int value, string parameterName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} cannot be negative.");
            return value;
        }

        /// <summary>
        /// Throws if value is negative or zero.
        /// </summary>
        public static int AgainstNegativeOrZero(int value, string parameterName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be positive.");
            return value;
        }

        /// <summary>
        /// Throws if value is zero.
        /// </summary>
        public static int AgainstZero(int value, string parameterName)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} cannot be zero.");
            return value;
        }

        /// <summary>
        /// Throws if value is outside the specified range.
        /// </summary>
        public static T AgainstOutOfRange<T>(T value, T min, T max, string parameterName)
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"{parameterName} must be between {min} and {max}.");
            return value;
        }

        // ==================== String Validation ====================

        /// <summary>
        /// Throws if string length is outside specified range.
        /// </summary>
        public static string AgainstInvalidLength(string value, int minLength, int maxLength, string parameterName)
        {
            AgainstNullOrEmpty(value, parameterName);

            if (value.Length < minLength || value.Length > maxLength)
                throw new ArgumentException(
                    $"{parameterName} length must be between {minLength} and {maxLength} characters. Actual: {value.Length}",
                    parameterName);

            return value;
        }

        /// <summary>
        /// Throws if string doesn't match the specified regex pattern.
        /// </summary>
        public static string AgainstInvalidFormat(string value, string pattern, string parameterName, string? errorMessage = null)
        {
            AgainstNullOrEmpty(value, parameterName);

            if (!Regex.IsMatch(value, pattern))
            {
                var message = errorMessage ?? $"{parameterName} format is invalid.";
                throw new ArgumentException(message, parameterName);
            }

            return value;
        }

        /// <summary>
        /// Validates DICOM UID format.
        /// </summary>
        public static string AgainstInvalidUid(string value, string parameterName)
        {
            // DICOM UID: 0-9 and dots, max 64 chars
            const string uidPattern = @"^[\d\.]+$";
            AgainstInvalidLength(value, 1, 64, parameterName);
            return AgainstInvalidFormat(value, uidPattern, parameterName, $"{parameterName} must be a valid DICOM UID.");
        }

        /// <summary>
        /// Validates DICOM AE Title format (max 16 chars, alphanumeric + underscore).
        /// </summary>
        public static string AgainstInvalidAeTitle(string value, string parameterName)
        {
            const string aeTitlePattern = @"^[A-Z0-9_]+$";
            AgainstInvalidLength(value, 1, 16, parameterName);
            return AgainstInvalidFormat(value, aeTitlePattern, parameterName, $"{parameterName} must be a valid AE Title (1-16 chars, A-Z0-9_).");
        }

        // ==================== Collection Validation ====================

        /// <summary>
        /// Throws if collection is null or empty.
        /// </summary>
        public static IEnumerable<T> AgainstNullOrEmpty<T>([NotNull] IEnumerable<T>? collection, string parameterName)
        {
            if (collection is null)
                throw new ArgumentNullException(parameterName);

            if (!collection.Any())
                throw new ArgumentException($"{parameterName} cannot be empty.", parameterName);

            return collection;
        }

        /// <summary>
        /// Throws if collection exceeds maximum count.
        /// </summary>
        public static ICollection<T> AgainstExceedingLimit<T>(ICollection<T> collection, int maxCount, string parameterName)
        {
            AgainstNull(collection, parameterName);

            if (collection.Count > maxCount)
                throw new ArgumentException(
                    $"{parameterName} cannot contain more than {maxCount} items. Actual: {collection.Count}",
                    parameterName);

            return collection;
        }

        // ==================== Business Rules ====================

        /// <summary>
        /// Throws InvalidOperationException if condition is false.
        /// </summary>
        public static void AgainstInvalidOperation(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Throws ArgumentException if condition is false.
        /// </summary>
        public static void AgainstInvalidArgument(bool condition, string parameterName, string message)
        {
            if (!condition)
                throw new ArgumentException(message, parameterName);
        }

        // ==================== Enum Validation ====================

        /// <summary>
        /// Throws if enum value is not defined.
        /// </summary>
        public static T AgainstInvalidEnum<T>(T value, string parameterName) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentException($"{parameterName} has invalid value: {value}", parameterName);

            return value;
        }

        // ==================== DateTime Validation ====================

        /// <summary>
        /// Throws if date is in the future.
        /// </summary>
        public static DateTime AgainstFutureDate(DateTime value, string parameterName)
        {
            if (value > DateTime.UtcNow)
                throw new ArgumentException($"{parameterName} cannot be a future date.", parameterName);

            return value;
        }

        /// <summary>
        /// Throws if date is in the past.
        /// </summary>
        public static DateTime AgainstPastDate(DateTime value, string parameterName)
        {
            if (value < DateTime.UtcNow)
                throw new ArgumentException($"{parameterName} cannot be a past date.", parameterName);

            return value;
        }
    }
}

