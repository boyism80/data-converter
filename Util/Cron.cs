using System.Text.RegularExpressions;

namespace ExcelTableConverter.Util
{
    /// <summary>
    /// Validates 6-field cron expressions compatible with croncpp (standard traits):
    /// second minute hour day-of-month month day-of-week
    /// </summary>
    public static class Cron
    {
        private static readonly Regex _whitespace = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly HashSet<string> _months = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
            "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
        };
        private static readonly HashSet<string> _weekdays = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"
        };

        public static bool IsCronType(string type)
        {
            return Type.Nake(type) == "cron";
        }

        public static bool IsValid(string expression)
        {
            return IsValid(expression, out _);
        }

        public static bool IsValid(string expression, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(expression))
            {
                error = "empty expression";
                return false;
            }

            var fields = _whitespace.Split(expression.Trim());
            if (fields.Length != 6)
            {
                error = "expected 6 fields (second minute hour day-of-month month day-of-week)";
                return false;
            }

            if (TryValidateField(fields[0], 0, 59, null, allowQuestion: false, out error) == false)
            {
                error = $"second: {error}";
                return false;
            }

            if (TryValidateField(fields[1], 0, 59, null, allowQuestion: false, out error) == false)
            {
                error = $"minute: {error}";
                return false;
            }

            if (TryValidateField(fields[2], 0, 23, null, allowQuestion: false, out error) == false)
            {
                error = $"hour: {error}";
                return false;
            }

            if (TryValidateField(fields[3], 1, 31, null, allowQuestion: true, out error) == false)
            {
                error = $"day-of-month: {error}";
                return false;
            }

            if (TryValidateField(fields[4], 1, 12, _months, allowQuestion: false, out error) == false)
            {
                error = $"month: {error}";
                return false;
            }

            if (TryValidateField(fields[5], 0, 7, _weekdays, allowQuestion: true, out error) == false)
            {
                error = $"day-of-week: {error}";
                return false;
            }

            return true;
        }

        private static bool TryValidateField(string field, int min, int max, HashSet<string> names, bool allowQuestion, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(field))
            {
                error = "empty field";
                return false;
            }

            if (allowQuestion && field == "?")
                return true;

            foreach (var part in field.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (TryValidatePart(part, min, max, names, allowQuestion, out error) == false)
                    return false;
            }

            return true;
        }

        private static bool TryValidatePart(string part, int min, int max, HashSet<string> names, bool allowQuestion, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(part))
            {
                error = "empty part";
                return false;
            }

            if (allowQuestion && part == "?")
                return true;

            var stepSplit = part.Split('/');
            if (stepSplit.Length > 2)
            {
                error = $"invalid step syntax '{part}'";
                return false;
            }

            var rangePart = stepSplit[0];
            if (stepSplit.Length == 2)
            {
                if (int.TryParse(stepSplit[1], out var step) == false || step <= 0)
                {
                    error = $"invalid step '{stepSplit[1]}'";
                    return false;
                }
            }

            if (rangePart == "*")
                return true;

            var rangeSplit = rangePart.Split('-');
            if (rangeSplit.Length > 2)
            {
                error = $"invalid range syntax '{rangePart}'";
                return false;
            }

            if (TryParseCronValue(rangeSplit[0], min, max, names, out var begin, out error) == false)
                return false;

            if (rangeSplit.Length == 1)
                return true;

            if (TryParseCronValue(rangeSplit[1], min, max, names, out var end, out error) == false)
                return false;

            if (begin > end)
            {
                error = $"range start {begin} is greater than end {end}";
                return false;
            }

            return true;
        }

        private static bool TryParseCronValue(string token, int min, int max, HashSet<string> names, out int value, out string error)
        {
            error = null;
            value = 0;

            if (string.IsNullOrWhiteSpace(token))
            {
                error = "empty value";
                return false;
            }

            if (names != null && names.Contains(token))
            {
                // Named tokens are accepted; numeric bounds are not applied.
                value = min;
                return true;
            }

            if (int.TryParse(token, out value) == false)
            {
                error = $"invalid value '{token}'";
                return false;
            }

            if (value < min || value > max)
            {
                error = $"value {value} out of range [{min}, {max}]";
                return false;
            }

            return true;
        }
    }
}
