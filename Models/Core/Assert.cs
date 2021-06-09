using System;
using APSIM.Shared.Utilities;

namespace Models.Core
{
    /// <summary>
    /// Assertion code.
    /// </summary>
    internal class Assert
    {
        /// <summary>
        /// Assert that a value is greater than a given lower bound.
        /// </summary>
        /// <param name="expected">The expected value (ie the lower bound).</param>
        /// <param name="actual">The actual value.</param>
        public static void Greater(double expected, double actual)
        {
            if (!MathUtilities.IsGreaterThan(expected, actual))
                throw new AssertionFailedException($"Expected: greater than {expected} but was: {actual}");
        }

        /// <summary>
        /// Assert that a value is less than a given lower bound.
        /// </summary>
        /// <param name="expected">The expected value (ie the upper bound).</param>
        /// <param name="actual">The actual value.</param>
        public static void Less(double expected, double actual)
        {
            if (!MathUtilities.IsLessThan(expected, actual))
                throw new AssertionFailedException($"Expected: less than {expected} but was: {actual}");
        }

        /// <summary>
        /// Assert that the two values are equal.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        public static void Equal(double expected, double actual)
        {
            if (!MathUtilities.FloatsAreEqual(expected, actual))
                throw new AssertionFailedException($"Expected: {expected} but was: {actual}");
        }

        /// <summary>
        /// Assert that the value is less than or equal to an expected value.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        public static void LessThanOrEqual(double expected, double actual)
        {
            if (!MathUtilities.IsLessThanOrEqual(actual, expected))
                throw new AssertionFailedException($"Expected: less than or equal to {expected} but was: {actual}");
        }

        /// <summary>
        /// Assert that the value is greater than or equal to an expected value.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        public static void GreaterThanOrEqual(double expected, double actual)
        {
            if (!MathUtilities.IsGreaterThanOrEqual(actual, expected))
                throw new AssertionFailedException($"Expected: greater than or equal to {expected} but was: {actual}");
        }
    }
}
