using System;
using Models.Core;
using System.Collections.Generic;
using System.Collections;
using System.Xml.Serialization;

namespace Models.Graph
{

    /// <summary>
    /// GraphValues encapsulates all ways that data (a series of values) can be supplied to 
    /// a Graphview. Currently there are two ways. The first is from a DataTable coming
    /// from a DataStore. The second uses reflection to pull values from a property in an object.
    /// e.g. Soil.sw
    /// </summary>
    [Serializable]
    public class GraphValues
    {
        // These three properties are needed to talk to a DataStore.
        // If SimulationName == null and TableName == null then it is
        // assumed that FieldName points to a property of an object.
        public string SimulationName { get; set; }
        public string TableName { get; set; }
        public string FieldName { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public GraphValues() { }

        /// <summary>
        /// Return a list of valid fieldnames.
        /// </summary>
        public string[] ValidFieldNames(Graph graph)
        {
            if (graph.DataStore != null && TableName != null && TableName != "")
            {
                List<string> Names = new List<string>();
                Names.AddRange(Utility.DataTable.GetColumnNames(graph.DataStore.GetData(SimulationName, TableName)));
                return Names.ToArray();
            }
            return null;
        }

    }

}
