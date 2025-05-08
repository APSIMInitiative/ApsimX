namespace Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Data;
    using System.Xml;
    using Models.Core;
    using APSIM.Shared.Utilities;
    using System.Reflection;

    /// <summary>
    /// Graph utility functions.
    /// </summary>
    public class Graph
    {
        public static Models.Graph CreateGraphFromResource(string resourceName)
        {
            string graphXmL = ReflectionUtilities.GetResourceAsString(resourceName);

            if (graphXmL != null)
            {
                List<Exception> errors = null;
                Models.Graph graph = NodeTreeFactory.CreateFromFile<Graph>(graphXmL, e => throw e, false).Root.Model as Models.Graph;
                if (errors != null && errors.Any())
                    throw errors.First();
                graph.ParentAllDescendants();
                return graph;
            }
            return null;
        }
    }
}
