namespace Models
{
    using APSIM.Shared.Utilities;
    using System.Drawing;

    /// <summary>A default painter for setting a simulation / zone pair to default values.</summary>
    public class DefaultPainter : ISeriesDefinitionPainter
    {
        private Color colour;
        private LineType lineType;
        private MarkerType markerType;

        /// <summary>Constructor</summary>
        /// <param name="c"></param>
        /// <param name="l"></param>
        /// <param name="m"></param>
        public DefaultPainter(Color c, LineType l, MarkerType m)
        {
            colour = c;
            lineType = l;
            markerType = m;
        }
        /// <summary>Set visual aspects (colour, line type, marker type) of the series definition.</summary>
        /// <param name="seriesDefinition">The definition to paint.</param>
        public void Paint(SeriesDefinition seriesDefinition)
        {
            seriesDefinition.Colour = ColourUtilities.ChangeColorBrightness(seriesDefinition.Colour, seriesDefinition.ColourModifier);
            seriesDefinition.Line = lineType;
            seriesDefinition.Marker = markerType;
        }
    }
}