using System.Globalization;

namespace Dicom.Edge.Common.Helpers
{
    /// <summary>
    /// Helper methods for DICOM date/time handling.
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        /// Parses a DICOM date string (yyyyMMdd) to DateTime.
        /// </summary>
        public static DateTime ParseDicomDate(string dicomDate)
        {
            if (string.IsNullOrWhiteSpace(dicomDate))
                throw new ArgumentException("DICOM date cannot be empty", nameof(dicomDate));

            if (DateTime.TryParseExact(dicomDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;

            throw new FormatException($"Invalid DICOM date format: {dicomDate}. Expected: yyyyMMdd");
        }

        /// <summary>
        /// Parses a DICOM time string (HHmmss or HHmmss.ffffff) to TimeSpan.
        /// </summary>
        public static TimeSpan ParseDicomTime(string dicomTime)
        {
            if (string.IsNullOrWhiteSpace(dicomTime))
                throw new ArgumentException("DICOM time cannot be empty", nameof(dicomTime));

            // Try with fractional seconds
            if (TimeSpan.TryParseExact(dicomTime, @"hhmmss\.ffffff", CultureInfo.InvariantCulture, out var time))
                return time;

            // Try without fractional seconds
            if (TimeSpan.TryParseExact(dicomTime, "hhmmss", CultureInfo.InvariantCulture, out time))
                return time;

            throw new FormatException($"Invalid DICOM time format: {dicomTime}. Expected: HHmmss or HHmmss.ffffff");
        }

        /// <summary>
        /// Converts DateTime to DICOM date string (yyyyMMdd).
        /// </summary>
        public static string ToDicomDate(DateTime date)
        {
            return date.ToString("yyyyMMdd");
        }

        /// <summary>
        /// Converts TimeSpan to DICOM time string (HHmmss.ffffff).
        /// </summary>
        public static string ToDicomTime(TimeSpan time)
        {
            return time.ToString(@"hhmmss\.ffffff");
        }

        /// <summary>
        /// Converts DateTime to DICOM time string (HHmmss.ffffff).
        /// </summary>
        public static string ToDicomTime(DateTime dateTime)
        {
            return dateTime.ToString(@"HHmmss\.ffffff");
        }

        /// <summary>
        /// Parses DICOM Age String (nnnD, nnnW, nnnM, nnnY) to years.
        /// </summary>
        public static double ParseDicomAgeToYears(string ageString)
        {
            if (string.IsNullOrWhiteSpace(ageString) || ageString.Length < 4)
                throw new ArgumentException("Invalid DICOM age string", nameof(ageString));

            var value = int.Parse(ageString[..3]);
            var unit = ageString[3];

            return unit switch
            {
                'D' => value / 365.25,
                'W' => value / 52.0,
                'M' => value / 12.0,
                'Y' => value,
                _ => throw new FormatException($"Invalid DICOM age unit: {unit}")
            };
        }

        /// <summary>
        /// Converts age in years to DICOM Age String.
        /// </summary>
        public static string ToDicomAge(double ageYears)
        {
            if (ageYears < 1)
            {
                var days = (int)(ageYears * 365.25);
                return $"{days:D3}D";
            }
            return $"{(int)ageYears:D3}Y";
        }

        /// <summary>
        /// Converts local time to UTC considering timezone.
        /// </summary>
        public static DateTime ConvertToUtc(DateTime localTime, string timeZoneId)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone);
        }

        /// <summary>
        /// Converts UTC time to local time for a specific timezone.
        /// </summary>
        public static DateTime ConvertFromUtc(DateTime utcTime, string timeZoneId)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);
        }
    }
}
