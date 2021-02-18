namespace Models
{
    /// <summary>A painter interface for setting visual elements of a simulation/zone pair</summary>
    public interface ISeriesDefinitionPainter
    {
        /// <summary>Set visual aspects (colour, line type, marker type) of the series definition.</summary>
        /// <param name="seriesDefinition">The definition to paint.</param>
        void Paint(SeriesDefinition seriesDefinition);
    }

    /// <summary>A delegate setter function.</summary>
    /// <param name="definition">The series definition to change.</param>
    /// <param name="index">The index</param>
    public delegate void SetFunction(SeriesDefinition definition, int index);


}
