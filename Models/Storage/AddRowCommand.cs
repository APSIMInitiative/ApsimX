namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Xml.Serialization;

    /// <summary>Encapsulates a row to write to an SQL database.</summary>
    class AddRowCommand : IRunnable
    {
        private DataStoreWriter writer;
        private string checkpointName;
        private string simulationName;
        private string tableName;
        private IList<string> columnNames;
        private IList<string> columnUnits;
        private IList<object> rowValues;
        private List<InsertQuery> insertQueries;
        private static string[] simulationAndCheckpointColumnNames = { "CheckpointID", "SimulationID" };
        private static string[] checkpointColumnNames = { "CheckpointID" };

        
        /// <summary>Constructor</summary>
        /// <param name="dataStoreWriter">The datastore writer that called this constructor.</param>
        /// <param name="insertQueries">A collection of insert queries that can be used when writing.</param>
        /// <param name="checkpointName">Name of simulation the values correspond to.</param>
        /// <param name="simulationName">Name of simulation the values correspond to.</param>
        /// <param name="tableName">Name of the table to write to.</param>
        /// <param name="columnNames">The column names relating to the values.</param>
        /// <param name="columnUnits">The units of each of the values.</param>
        /// <param name="rowValues">The values making up the row to write.</param>
        public AddRowCommand(DataStoreWriter dataStoreWriter, List<InsertQuery> insertQueries,
                             string checkpointName, string simulationName, string tableName, 
                             IList<string> columnNames, 
                             IList<string> columnUnits, 
                             IList<object> rowValues)
        {
            this.writer = dataStoreWriter;
            this.insertQueries = insertQueries;
            this.checkpointName = checkpointName;
            this.simulationName = simulationName;
            this.tableName = tableName;
            this.columnNames = columnNames;
            this.columnUnits = columnUnits;
            this.rowValues = rowValues;
        }

        /// <summary>Called to run the command. Can throw on error.</summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            Flatten();

            // Have we already writen to this table?
            var query = insertQueries.Find(pq => pq.TableName == tableName);
            if (query == null)
            {
                // No we haven't so prepare a query.
                query = new InsertQuery(tableName);
                insertQueries.Add(query);
            }

            // Work out what columns, units and rows we're actually going to 
            // write to the database.
            IEnumerable<string> allColumnNames = columnNames;
            IEnumerable<string> allUnitNames = columnUnits;
            IEnumerable<object> allRowValues = rowValues;

            // Do we have to add a checkpointid and/or a simulationid column?
            if (simulationName == null && checkpointName != null)
            {
                // Yes - add checkpointid only.
                object[] checkpointRowValue = { writer.GetCheckpointID(checkpointName) };

                allColumnNames = checkpointColumnNames.Concat(columnNames);
                if (columnUnits != null)
                    allUnitNames = new string[1].Concat(columnUnits);
                allRowValues = checkpointRowValue.Concat(rowValues);
            }
            else if (simulationName != null && checkpointName != null)
            {
                // Yes - add checkpointid and simulationid columns.
                object[] coreRowValues = { writer.GetCheckpointID(checkpointName),
                                           writer.GetSimulationID(simulationName) };
                allColumnNames = simulationAndCheckpointColumnNames.Concat(columnNames);
                if (columnUnits != null)
                    allUnitNames = new string[2].Concat(columnUnits);
                allRowValues = coreRowValues.Concat(rowValues);
            }

            // Make sure the table has the correct columns.
            writer.EnsureTableHasColumnNames(tableName, allColumnNames, allUnitNames, allRowValues);

            // Write the row.
            query.ExecuteQuery(writer.Connection, allColumnNames, allRowValues);
        }


        /// <summary>
        /// 'Flatten' the row into a list of columns ready to be added
        /// to a data table.
        /// </summary>
        private void Flatten()
        {
            List<string> newColumnNames = new List<string>();
            List<string> newColumnUnits = new List<string>();
            List<object> newValues = new List<object>();

            for (int i = 0; i < rowValues.Count(); i++)
            {
                string units = null;
                if (columnUnits != null)
                    units = columnUnits.ElementAt(i);
                FlattenValue(columnNames.ElementAt(i),
                             units,
                             rowValues.ElementAt(i),
                             newColumnNames, newColumnUnits, newValues);
            }

            columnNames = newColumnNames;
            columnUnits = newColumnUnits;
            rowValues = newValues;
        }

        /// <summary>
        /// 'Flatten' a value (if it is an array or structure) into something that can be
        /// stored in a flat database table.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="units"></param>
        /// <param name="value"></param>
        /// <param name="newColumnNames"></param>
        /// <param name="newColumnUnits"></param>
        /// <param name="newValues"></param>
        private static void FlattenValue(string name, string units, object value,
                                         List<string> newColumnNames, List<string> newColumnUnits, List<object> newValues)
        {
            if (value == null || value.GetType() == typeof(DateTime) || value.GetType() == typeof(string) || !value.GetType().IsClass)
            {
                // Scalar
                newColumnNames.Add(name);
                newColumnUnits.Add(units);
                newValues.Add(value);
            }
            else if (value.GetType().IsArray)
            {
                // Array
                Array array = value as Array;

                for (int columnIndex = 0; columnIndex < array.Length; columnIndex++)
                {
                    string heading = name;
                    heading += "(" + (columnIndex + 1).ToString() + ")";

                    object arrayElement = array.GetValue(columnIndex);
                    FlattenValue(heading, units, arrayElement,
                                 newColumnNames, newColumnUnits, newValues);  // recursion
                }
            }
            else if (value.GetType().GetInterface("IList") != null)
            {
                // List
                IList array = value as IList;
                for (int columnIndex = 0; columnIndex < array.Count; columnIndex++)
                {
                    string heading = name;
                    heading += "(" + (columnIndex + 1).ToString() + ")";

                    object arrayElement = array[columnIndex];
                    FlattenValue(heading, units, arrayElement,
                                 newColumnNames, newColumnUnits, newValues);  // recursion                }
                }
            }
            else
            {
                // A struct or class
                foreach (PropertyInfo property in ReflectionUtilities.GetPropertiesSorted(value.GetType(), BindingFlags.Instance | BindingFlags.Public))
                {
                    object[] attrs = property.GetCustomAttributes(true);
                    string propUnits = null;
                    bool ignore = false;
                    foreach (object attr in attrs)
                    {
                        if (attr is XmlIgnoreAttribute)
                        {
                            ignore = true;
                            continue;
                        }
                        Core.UnitsAttribute unitsAttr = attr as Core.UnitsAttribute;
                        if (unitsAttr != null)
                            propUnits = unitsAttr.ToString();
                    }
                    if (ignore)
                        continue;
                    string heading = name + "." + property.Name;
                    object classElement = property.GetValue(value, null);
                    FlattenValue(heading, propUnits, classElement,
                                 newColumnNames, newColumnUnits, newValues);  // recursion
                }
            }
        }

    }
}