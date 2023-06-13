using System.Collections.Generic;
using System.Drawing;
using APSIM.Shared.Graphing;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models
{

    /// <summary>
    /// An interface for a model that can graph itself.
    /// </summary>
    public interface IGraphable : IModel
    {
        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="storage">Storage service</param>
        /// <param name="simulationDescriptions">A list of simulation descriptions that are in scope.</param>
        /// <param name="simulationFilter">(Optional) only show data for these simulations.</param>
        IEnumerable<SeriesDefinition> CreateSeriesDefinitions(IStorageReader storage,
                                                           List<SimulationDescription> simulationDescriptions,
                                                           List<string> simulationFilter = null);

        /// <summary>Called by the graph presenter to get a list of all annotations to put on the graph.</summary>
        IEnumerable<IAnnotation> GetAnnotations();

        /// <summary>Return a list of extra fields that the definition should read.</summary>
        /// <param name="seriesDefinition">The calling series definition.</param>
        /// <returns>A list of fields - never null.</returns>
        IEnumerable<string> GetExtraFieldsToRead(SeriesDefinition seriesDefinition);
    }

    /// <summary>An enumeration for the different types of graph series</summary>
    public enum SeriesType
    {
        /// <summary>A bar series</summary>
        Bar,

        /// <summary>A scatter series</summary>
        Scatter,

        /// <summary>
        /// A region series - two series with the area between them filled with colour.
        /// </summary>
        Region,

        /// <summary>
        /// An area series - a line series with the area between the line and the x-axis filled with colour.
        /// </summary>
        Area,

        /// <summary>
        /// A stacked area series - a line series with the area between the line and the x-axis filled with colour.
        /// </summary>
        StackedArea,

        /// <summary>
        /// A box and whisker plot
        /// </summary>
        Box
    }

    /// <summary>Base interface for all annotations</summary>
    public interface IAnnotation
    {
    }

    /// <summary>
    /// A class for defining a text annotation
    /// </summary>
    public class TextAnnotation : IAnnotation
    {
        /// <summary>Name of annotation.</summary>
        public string Name;

        /// <summary>X position - can be double.MinValue for autocalculated</summary>
        public object x;

        /// <summary>Y position - can be double.MinValue for autocalculated</summary>
        public object y;

        /// <summary>A text annotation.</summary>
        public string text;

        /// <summary>The colour of the text</summary>
        public Color colour;

        /// <summary>Left align the text?</summary>
        public bool leftAlign;

        /// <summary>Top align the text?</summary>
        public bool topAlign = true;

        /// <summary>Text rotation angle</summary>
        public double textRotation;
    }

    /// <summary>
    /// A class for defining a line annotation
    /// </summary>
    public class LineAnnotation : IAnnotation
    {
        /// <summary>X1 position - can be double.MinValue for autocalculated</summary>
        public object x1;

        /// <summary>Y1 position - can be double.MinValue for autocalculated</summary>
        public object y1;

        /// <summary>X2 position - can be double.MinValue for autocalculated</summary>
        public object x2;

        /// <summary>Y2 position - can be double.MinValue for autocalculated</summary>
        public object y2;

        /// <summary>The colour of the text</summary>
        public Color colour;

        /// <summary>Gets the line type to show</summary>
        public LineType type;

        /// <summary>Gets the line thickness</summary>
        public LineThickness thickness;

        /// <summary>Draw the annotation in front of series?</summary>
        public bool InFrontOfSeries { get; set; } = true;

        /// <summary>Annotation tooltip</summary>
        public string ToolTip { get; set; }
    }
}
