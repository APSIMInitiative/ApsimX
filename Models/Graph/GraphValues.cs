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
        /// Return values to caller.
        /// </summary>
        public IEnumerable Values(Graph graph)
        {
            IEnumerable Data = null;
            if (SimulationName == null && TableName == null && FieldName != null)
            {
                // Use reflection to access a property.
                object Obj = graph.Get(FieldName);
                if (Obj != null && Obj.GetType().IsArray)
                {
                    Array A = Obj as Array;
                    return A;
                }
            }
            else if (graph.DataStore != null && SimulationName != null && TableName != null && FieldName != null)
            {
                System.Data.DataTable DataSource = graph.DataStore.GetData(SimulationName, TableName);
                if (DataSource != null && FieldName != null && DataSource.Columns[FieldName] != null)
                {
                    if (DataSource.Columns[FieldName].DataType == typeof(DateTime))
                        return Utility.DataTable.GetColumnAsDates(DataSource, FieldName);
                    else if (DataSource.Columns[FieldName].DataType == typeof(string))
                        return Utility.DataTable.GetColumnAsStrings(DataSource, FieldName);
                    else
                        return Utility.DataTable.GetColumnAsDoubles(DataSource, FieldName);
                }
            }
            return Data;
        }

        /// <summary>
        /// Return a list of valid fieldnames.
        /// </summary>
        public string[] ValidFieldNames(Graph graph)
        {
            if (graph.DataStore != null && SimulationName != null && TableName != null && TableName != "")
            {
                List<string> Names = new List<string>();
                Names.AddRange(Utility.DataTable.GetColumnNames(graph.DataStore.GetData(SimulationName, TableName)));
                return Names.ToArray();
            }
            return null;
        }

    }

}
