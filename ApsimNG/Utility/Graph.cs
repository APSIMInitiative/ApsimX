namespace Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using APSIM.Core;

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
                Models.Graph graph = FileFormat.ReadFromString<Models.Graph>(graphXmL).Model as Models.Graph;
                if (errors != null && errors.Any())
                    throw errors.First();
                graph.ParentAllDescendants();
                return graph;
            }
            return null;
        }
    }
}
