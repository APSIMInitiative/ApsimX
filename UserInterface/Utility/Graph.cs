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
        public static Models.Graph.Graph CreateGraphFromResource(string resourceName)
        {
            string graphXmL = UserInterface.Properties.Resources.ResourceManager.GetString(resourceName);

            if (graphXmL != null)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(graphXmL);

                Models.Graph.Graph graph = XmlUtilities.Deserialise(doc.DocumentElement, Assembly.GetExecutingAssembly()) as Models.Graph.Graph;
                Apsim.ParentAllChildren(graph);
                return graph;
            }
            return null;
        }
    }
}
