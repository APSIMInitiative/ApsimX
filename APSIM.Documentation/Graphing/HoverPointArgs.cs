using System;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// When the user hovers over a point on a graph, this structure will be 
    /// passed from GraphView to the presenter. The presenter then needs to
    /// fill in the hover text.
    /// </summary>
    public class HoverPointArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the name of the series being hovered over
        /// </summary>
        public string SeriesName { get; set; }

        /// <summary>
        /// Gets or sets the X point
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y point
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// The presenter needs to set the hover text.
        /// </summary>
        public string HoverText { get; set; }
    }  
}
