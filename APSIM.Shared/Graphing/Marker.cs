namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// Represents a marker on a graph.
    /// </summary>
    public struct Marker
    {
        /// <summary>
        /// Marker type.
        /// </summary>
        public MarkerType Type { get; private set; }

        /// <summary>
        /// Marker size.
        /// </summary>
        /// <value></value>
        public MarkerSize Size { get; private set; }

        /// <summary>
        /// This is a modifier on marker size as a proportion of the original
        /// size. E.g. 0.5 for half size.
        /// </summary>
        public double SizeModifier { get; private set; }

        /// <summary>
        /// Creates a marker instance.
        /// </summary>
        /// <param name="type">Marker type.</param>
        /// <param name="size">Marker size.</param>
        /// <param name="modifier">Modifier on marker size, as a proportion of the original size.</param>
        public Marker(MarkerType type, MarkerSize size, double modifier)
        {
            Type = type;
            Size = size;
            SizeModifier = modifier;
        }

        /// <summary>
        /// Is this a "filled" marker type? (filled as in filled with colour)
        /// </summary>
        public bool IsFilled()
        {
            switch (Type)
            {
                case MarkerType.FilledCircle:
                case MarkerType.FilledDiamond:
                case MarkerType.FilledSquare:
                case MarkerType.FilledTriangle:
                    return true;
                default:
                    return false;
            }
        }
    }    
}
