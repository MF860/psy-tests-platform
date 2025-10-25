using System.Globalization;

namespace PsyApi.Utils
{
    public static class NumericValidator
    {
        public static (bool isValid, double? value, string? error) ValidateNumeric(string? input, bool allowNegative = false)
        {
            if (string.IsNullOrWhiteSpace(input))
                return (false, null, "NUMERIC_EMPTY");

            // Remove any Arabic/Eastern digits and convert to Western
            var normalized = input.Trim()
                .Replace('٠', '0').Replace('١', '1')
                .Replace('٢', '2').Replace('٣', '3')
                .Replace('٤', '4').Replace('٥', '5')
                .Replace('٦', '6').Replace('٧', '7')
                .Replace('٨', '8').Replace('٩', '9')
                // Handle Arabic decimal separator (٫)
                .Replace('٫', '.')
                // Handle Arabic thousands separator (٬)
                .Replace('٬', ',');

            if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                if (!allowNegative && value < 0)
                    return (false, null, "NUMERIC_NEGATIVE_NOT_ALLOWED");

                if (double.IsInfinity(value) || double.IsNaN(value))
                    return (false, null, "NUMERIC_INVALID_RANGE");

                return (true, value, null);
            }

            return (false, null, "NUMERIC_INVALID_FORMAT");
        }

        public static (bool isValid, double? value, string? error) ValidateNumericForItem(
            string? input, 
            string? correctAnswer, 
            int difficulty, 
            bool allowNegative = false)
        {
            // First validate basic numeric format
            var (isValid, value, error) = ValidateNumeric(input, allowNegative);
            if (!isValid || !value.HasValue)
                return (false, null, error);

            // If we have a correct answer, validate within tolerance
            if (!string.IsNullOrWhiteSpace(correctAnswer) && 
                double.TryParse(correctAnswer, NumberStyles.Any, CultureInfo.InvariantCulture, out var correctValue))
            {
                var tolerancePct = difficulty switch
                {
                    1 => 0.10, // Easy - 10% tolerance
                    2 => 0.15, // Medium-Easy - 15% tolerance
                    3 => 0.20, // Medium - 20% tolerance 
                    4 => 0.25, // Medium-Hard - 25% tolerance
                    5 => 0.30, // Hard - 30% tolerance
                    _ => 0.15  // Default to 15% tolerance
                };

                var tolerance = Math.Abs(correctValue) * tolerancePct;
                var difference = Math.Abs(correctValue - value.Value);

                // For values close to zero, use absolute tolerance based on difficulty
                if (Math.Abs(correctValue) < 1.0)
                {
                    tolerance = difficulty switch
                    {
                        1 => 0.3,  // Easy - more forgiving absolute tolerance
                        2 => 0.4,  // Medium-Easy
                        3 => 0.5,  // Medium
                        4 => 0.6,  // Medium-Hard  
                        5 => 0.7,  // Hard - even more forgiving for hard questions
                        _ => 0.4   // Default to 0.4 absolute tolerance
                    };
                }

                if (difference > tolerance)
                    return (false, value, "NUMERIC_INVALID_TOLERANCE");
            }

            return (true, value, null);
        }
    }
}