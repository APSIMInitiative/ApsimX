using Models.DCAPST.Canopy;

namespace Models.DCAPST
{
    /// <summary>
    /// 
    /// </summary>
    public class IntervalValues
    {
        /// <summary>
        /// The time of the interval
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double AirTemperature { get; set; }

        /// <summary>
        /// Leaf area index of the sunlit canopy during the interval.
        /// </summary>
        public double SunlitLAI { get; set; }

        /// <summary>
        /// Leaf area index of the shaded canopy during the interval.
        /// </summary>
        public double ShadedLAI { get; set; }

        /// <summary>
        /// Area values for the sunlit canopy
        /// </summary>
        public AreaValues Sunlit { get; set; }

        /// <summary>
        /// Area values for the shaded canopy
        /// </summary>
        public AreaValues Shaded { get; set; }
    }
}
