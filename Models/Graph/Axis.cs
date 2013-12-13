using Models.Core;
using System;

namespace Models.Graph
{
    [Serializable]
    public class Axis : Model
    {
        public enum AxisType { Left, Top, Right, Bottom };

        /// <summary>
        /// The 'type' of axis - left, top, right or bottom.
        /// </summary>
        public AxisType Type { get; set; }

        /// <summary>
        /// The title of the axis.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Is the axis inverted?
        /// </summary>
        public bool Inverted { get; set; }
    }
}
