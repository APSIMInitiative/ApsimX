using System;

namespace Models.CLEM.Extras
{
    /// <summary>
    /// Double extensions
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Double equals accouting for floating point rounding errors
        /// </summary>
        /// <param name="value1">double being compared</param>
        /// <param name="value2">double compare to</param>
        /// <param name="tolerance">tolerance level</param>
        /// <returns>true if equal</returns>
        public static bool EqualsSafe(this double value1, double value2, double tolerance = 1e-8)
        {
            if (tolerance < 0)
                throw new ArgumentException("Tolerance must be greater than 0");
            return Math.Abs(value1 - value2) <= tolerance;
        }

        /// <summary>
        /// Double greater that accouting for floating point rounding errors
        /// </summary>
        /// <param name="value1">double being compared</param>
        /// <param name="value2">double compare to</param>
        /// <param name="tolerance">tolerance level determines number of decimal places to round to</param>
        /// <returns>true if greater than</returns>
        public static bool GreaterThanSafe(this double value1, double value2, double tolerance = 1e-8)
        {
            if (tolerance < 0)
                throw new ArgumentException("Tolerance must be greater than 0");
            int digits = Math.Abs(Convert.ToInt32(Math.Log10(tolerance)));
            return Math.Round(value1, digits) > Math.Round(value1, digits);
        }

        /// <summary>
        /// Double greater than or equals accouting for floating point rounding errors
        /// </summary>
        /// <param name="value1">double being compared</param>
        /// <param name="value2">double compare to</param>
        /// <param name="tolerance">tolerance level determines number of decimal places to round to, and equals tolerance check</param>
        /// <returns>true if greater than or equal to</returns>
        public static bool GreaterThanEqualSafe(this double value1, double value2, double tolerance = 1e-8)
        {
            if (tolerance < 0)
                throw new ArgumentException("Tolerance must be greater than 0");
            int digits = Math.Abs(Convert.ToInt32(Math.Log10(tolerance)));
            return Math.Round(value1, digits) > Math.Round(value1, digits) || value1.EqualsSafe(value2, tolerance);
        }

        /// <summary>
        /// Double less than accouting for floating point rounding errors
        /// </summary>
        /// <param name="value1">double being compared</param>
        /// <param name="value2">double compare to</param>
        /// <param name="tolerance">tolerance level determines number of decimal places to round to</param>
        /// <returns>true if less than</returns>
        public static bool LessThanSafe(this double value1, double value2, double tolerance = 1e-8)
        {
            if (tolerance < 0)
                throw new ArgumentException("Tolerance must be greater than 0");
            int digits = Math.Abs(Convert.ToInt32(Math.Log10(tolerance)));
            return Math.Round(value1, digits) < Math.Round(value1, digits);
        }

        /// <summary>
        /// Double less than or equals than accouting for floating point rounding errors
        /// </summary>
        /// <param name="value1">double being compared</param>
        /// <param name="value2">double compare to</param>
        /// <param name="tolerance">tolerance level determines number of decimal places to round to, and equals tolerance check</param>
        /// <returns>true if less than</returns>
        public static bool LessThanEqualSafe(this double value1, double value2, double tolerance = 1e-8)
        {
            if (tolerance < 0)
                throw new ArgumentException("Tolerance must be greater than 0");
            int digits = Math.Abs(Convert.ToInt32(Math.Log10(tolerance)));
            return Math.Round(value1, digits) < Math.Round(value1, digits) || value1.EqualsSafe(value2, tolerance);
        }
    }
}
