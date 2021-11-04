namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// Graph legend configuration options.
    /// </summary>
    public class LegendConfiguration : ILegendConfiguration
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
        /// Should the legend be displayed inside the graph area?
        /// </summary>
        public bool InsideGraphArea { get; private set; }

        /// <summary>
        /// Create a Legend instance.
        /// </summary>
        /// <param name="orientation">Legend orientation.</param>
        /// <param name="position">Legend position.</param>
        /// <param name="insideGraph">Should the legend be displayed inside the graph area?</param>
        public LegendConfiguration(LegendOrientation orientation, LegendPosition position, bool insideGraph)
        {
            Orientation = orientation;
            Position = position;
            InsideGraphArea = insideGraph;
        }
    }
}
