
namespace Model.Components.Graph
{
    public class Axis
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
    }
}
