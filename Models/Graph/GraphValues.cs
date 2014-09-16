// -----------------------------------------------------------------------
// <copyright file="GraphValues.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Graph
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Models.Core;

    /// <summary>
    /// GraphValues encapsulates all ways that data (a series of values) can be supplied to 
    /// a graph view. Currently there are two ways. The first is from a DataTable coming
    /// from a DataStore. The second uses reflection to pull values from a property in an object.
    /// If TableName == null then it is assumed that FieldName points to a property of an object.
    /// </summary>
    [Serializable]
    public class GraphValues
    {
        /// <summary>
        /// Gets or sets the table name containing the data
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the field name
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// Return a list of valid fieldnames.
        /// </summary>
        /// <param name="graph">The parent graph</param>
        /// <returns>The array of field names</returns>
        public string[] ValidFieldNames(Graph graph)
        {
            if (graph.DataStore != null && this.TableName != null && this.TableName != string.Empty)
            {
                List<string> names = new List<string>();
                names.AddRange(Utility.DataTable.GetColumnNames(graph.DataStore.GetData("*", this.TableName)));
                return names.ToArray();
            }
            return null;
        }

        /// <summary>
        /// Return data using reflection
        /// </summary>
        /// <param name="graph">The parent graph</param>
        /// <returns>The return data or null if not found</returns>
        public IEnumerable GetData(Graph graph)
        {
            if (this.TableName == null && this.FieldName != null)
            {
                if (this.FieldName.StartsWith("["))
                {
                    int posCloseBracket = this.FieldName.IndexOf(']');
                    if (posCloseBracket == -1)
                        throw new Exception("Invalid graph field name: " + this.FieldName);

                    string modelName = this.FieldName.Substring(1, posCloseBracket - 1);
                    string namePath = this.FieldName.Remove(0, posCloseBracket + 2);
                    IModel modelWithData = Apsim.Find(graph, modelName) as IModel;
                    if (modelWithData == null)
                    {
                        // Try by assuming the name is a type.
                        Type t = Utility.Reflection.GetTypeFromUnqualifiedName(modelName);
                        if (t != null)
                        {
                            modelWithData = Apsim.Find(graph, t) as IModel;
                        }
                    }

                    if (modelWithData != null)
                    {
                        // Use reflection to access a property.
                        object obj = Apsim.Get(modelWithData, namePath);
                        if (obj != null && obj.GetType().IsArray)
                            return obj as Array;
                    }
                }
            }

            return null;
        }
    }
}
