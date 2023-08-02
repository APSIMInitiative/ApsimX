namespace Models.DCAPST.Interfaces
{
    /// <summary>
    /// Represents a model that simulates solar geometry on a given day
    /// </summary>
    public interface ISolarGeometry
    {
        /// <summary>
        /// Day of year.
        /// </summary>
        double DayOfYear { get; }

        /// <summary>
        /// Time of sunrise
        /// </summary>
        double Sunrise { get; }

        /// <summary>
        /// Time of sunset
        /// </summary>
        double Sunset { get; }

        /// <summary>
        /// Total time the sun is up
        /// </summary>
        double DayLength { get; }

        /// <summary>
        /// Mean solar radiation per unit area
        /// </summary>
        double SolarConstant { get; }

        /// <summary>
        /// 
        /// </summary>
        void Initialise();

        /// <summary>
        /// Calculates the angle of the sun in the sky at the given time
        /// </summary>
        double SunAngle(double time);
    }
}
