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
    public class GraphValues : Model
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
        /// Return a DataStore in scope
        /// </summary>
        private DataStore DataStore
        {
           get
           {
               return Find(typeof(DataStore)) as DataStore;
           }
        }

        /// <summary>
        /// Return values to caller.
        /// </summary>
        [XmlIgnore]
        public IEnumerable Values
        {
            get
            {
                IEnumerable Data = null;
                if (SimulationName == null && TableName == null && FieldName != null)
                {
                    // Use reflection to access a property.
                    object Obj = Get(FieldName);
                    if (Obj != null && Obj.GetType().IsArray)
                    {
                        Array A = Obj as Array;
                        return A;
                    }
                }
                else if (DataStore != null && SimulationName != null && TableName != null && FieldName != null)
                {
                    System.Data.DataTable DataSource = DataStore.GetData(SimulationName, TableName);
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
        }

        /// <summary>
        /// Return a list of valid fieldnames.
        /// </summary>
        public string[] ValidFieldNames
        {
            get
            {
                if (DataStore != null && SimulationName != null && TableName != null)
                {
                    List<string> Names = new List<string>();
                    Names.AddRange(Utility.DataTable.GetColumnNames(DataStore.GetData(SimulationName, TableName)));
                    return Names.ToArray();
                }
                return null;
            }
        }

    }

}
