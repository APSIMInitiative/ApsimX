namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// Graph legend configuration options.
    /// </summary>
    public interface ILegendConfiguration
    {
        /// <summary>
        /// Legend orientation.
        /// </summary>
        LegendOrientation Orientation { get; }

        /// <summary>
        /// Legend position.
        /// </summary>
        LegendPosition Position { get; }

        /// <summary>
        /// Should the legend be displayed inside the graph area?
        /// </summary>
        bool InsideGraphArea { get; }
    }
}
