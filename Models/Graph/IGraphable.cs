namespace Models
{
    using Models.Storage;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Xml.Serialization;

    /// <summary>
    /// An interface for a model that can graph itself.
    /// </summary>
    public interface IGraphable
    {
        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="definitions">A list of definitions to add to.</param>
        /// <param name="storage">Storage service</param>
        /// <param name="simulationFilter">(Optional) only show data for these simulations.</param>
        void GetSeriesToPutOnGraph(IStorageReader storage, List<SeriesDefinition> definitions, List<string> simulationFilter = null);

        /// <summary>Called by the graph presenter to get a list of all annotations to put on the graph.</summary>
        /// <param name="annotations">A list of annotations to add to.</param>
        void GetAnnotationsToPutOnGraph(List<Annotation> annotations);

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

    /// <summary>An enumeration for the different types of markers</summary>
    public enum MarkerType 
    {
        /// <summary>A filled circle marker</summary>
        FilledCircle,

        /// <summary>A filled diamond marker</summary>
        FilledDiamond,

        /// <summary>A filled square marker</summary>
        FilledSquare,

        /// <summary>A filled triangle marker</summary>
        FilledTriangle,

        /// <summary>A circle marker</summary>
        Circle,

        /// <summary>A diamond marker</summary>
        Diamond,

        /// <summary>A square marker</summary>
        Square,

        /// <summary>A triangle marker</summary>
        Triangle,

        /// <summary>A cross marker</summary>
        Cross,

        /// <summary>A plus marker</summary>
        Plus,

        /// <summary>A star marker</summary>
        Star,

        /// <summary>No marker should be display</summary>
        None
    }

    /// <summary>An enumeration for the different sizes of markers</summary>
    public enum MarkerSizeType
    {
        /// <summary>Normal size markers.</summary>
        Normal,

        /// <summary>Small markers</summary>
        Small,

        /// <summary>Very small markers</summary>
        VerySmall,

        /// <summary>Large size markers.</summary>
        Large,

    }

    /// <summary>An enumeration representing the different types of lines</summary>
    public enum LineType 
    {
        /// <summary>A solid line</summary>
        Solid,

        /// <summary>A dashed line</summary>
        Dash,

        /// <summary>A dotted line</summary>
        Dot,

        /// <summary>A dash dot line</summary>
        DashDot,

        /// <summary>No line</summary>
        None 
    }

    /// <summary>An enumeration for the different thicknesses of lines.</summary>
    public enum LineThicknessType
    {
        /// <summary>Normal line thickness</summary>
        Normal,

        /// <summary>Thin line thickess</summary>
        Thin
    }

    /// <summary>Base interface for all annotations</summary>
    public interface Annotation
    {
    }

    /// <summary>
    /// A class for defining a text annotation
    /// </summary>
    public class TextAnnotation : Annotation
    {
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

        /// <summary>Text rotation angle</summary>
        public double textRotation;
    }

    /// <summary>
    /// A class for defining a line annotation
    /// </summary>
    public class LineAnnotation : Annotation
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
        public LineThicknessType thickness;

        /// <summary>Draw the annotation in front of series?</summary>
        public bool InFrontOfSeries { get; set; } = true;

        /// <summary>Annotation tooltip</summary>
        public string ToolTip { get; set; }
    }
}
