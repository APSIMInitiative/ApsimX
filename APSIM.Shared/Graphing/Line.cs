namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// Represents a line on a graph, and the user-configurable options
    /// for that line.
    /// </summary>
    public struct Line
    {
        /// <summary>
        /// Line type.
        /// </summary>
        public LineType Type { get; private set; }

        /// <summary>
        /// Line thickness.
        /// </summary>
        public LineThickness Thickness { get; private set; }

        /// <summary>
        /// Create a Line instance.
        /// </summary>
        /// <param name="type">Line type.</param>
        /// <param name="thickness">Line thickness.</param>
        public Line(LineType type, LineThickness thickness)
        {
            Type = type;
            Thickness = thickness;
        }
    }    
}
