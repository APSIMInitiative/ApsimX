using System;

namespace Models.DCAPST
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Convert an angle from degrees to radians.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        public static double ToRadians(this double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Convert an angle from radians to degrees.
        /// </summary>
        /// <param name="radians">The angle in radians.</param>
        /// <returns>The angle in degrees.</returns>
        public static double ToDegrees(this double radians)
        {
            return radians * 180.0 / Math.PI;
        }
    }
}
