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
        /// Area values for the sunlit canopy
        /// </summary>
        public AreaValues Sunlit { get; set; }

        /// <summary>
        /// Area values for the shaded canopy
        /// </summary>
        public AreaValues Shaded { get; set; }
    }
}
