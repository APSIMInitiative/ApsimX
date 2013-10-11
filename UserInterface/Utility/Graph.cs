namespace Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Data;
    using System.Xml;
    using Models.Core;

    /// <summary>
    /// Graph utility functions.
    /// </summary>
    public class Graph
    {
        public static Models.Graph.Graph CreateGraphFromResource(string resourceName, Model parent)
        {
            string graphXmL = UserInterface.Properties.Resources.ResourceManager.GetString(resourceName);

            if (graphXmL != null)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(graphXmL);

                Models.Graph.Graph graph = Utility.Xml.Deserialise(doc.DocumentElement) as Models.Graph.Graph;
                if (graph != null)
                {
                    graph.Parent = parent;
                    graph.ResolveLinks();
                }
                return graph;
            }
            return null;
        }
    }
}
