
namespace Models
{
    using Core;
    using System.Collections.Generic;

    /// <summary>Descibes a page of graphs for the tags system.</summary>
    public class GraphPage : AutoDocumentation.ITag
    {
        /// <summary>The image to put into the doc.</summary>
        public List<Graph> graphs = new List<Graph>();

        /// <summary>Unique name for image. Used to save image to temp folder.</summary>
        public string name;
    }
}
