namespace APSIM.Services.Graphing
{
    /// <summary>
    /// Graph legend configuration options.
    /// </summary>
    public class LegendConfiguration
    {
        /// <summary>
        /// Legend orientation.
        /// </summary>
        public LegendOrientation Orientation { get; private set; }

        /// <summary>
        /// Legend position.
        /// </summary>
        public LegendPosition Position { get; private set; }

        /// <summary>
        /// Create a Legend instance.
        /// </summary>
        /// <param name="orientation">Legend orientation.</param>
        /// <param name="position">Legend position.</param>
        public LegendConfiguration(LegendOrientation orientation, LegendPosition position)
        {
            Orientation = orientation;
            Position = position;
        }
    }
}
