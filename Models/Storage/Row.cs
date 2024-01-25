using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Newtonsoft.Json;

namespace Models.Storage
{
    /// <summary>Encapsulates a row that needs writing to the database.</summary>
    class Row
    {
        /// <summary>Simulation name for this row</summary>
        public string SimulationName { get; private set; }

        /// <summary>A collection of column names for this row</summary>
        public IEnumerable<string> ColumnNames { get; private set; }

        /// <summary>A collection of column units for this row</summary>
        public IEnumerable<string> ColumnUnits { get; private set; }

        /// <summary>A collection of column values for this row</summary>
        public IEnumerable<object> Values { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="simulationName">Simulation name</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="valuesToWrite">A row of values to write</param>
        public Row(string simulationName,
                   IEnumerable<string> columnNames,
                   IEnumerable<string> columnUnits,
                   IEnumerable<object> valuesToWrite)
        {
            this.SimulationName = simulationName;
            this.ColumnNames = columnNames;
            this.ColumnUnits = columnUnits;
            this.Values = valuesToWrite;
        }

        /// <summary>Write row to .db</summary>
        /// <param name="returnColumnNames">Column names for each value</param>
        /// <param name="returnValues">An write to store our values in</param>
        /// <param name="simulationIDs">A dictionary of simulation IDs</param>
        public void GetValues(List<string> returnColumnNames, ref object[] returnValues, Dictionary<string, int> simulationIDs)
        {
            //   Flatten();
            int id;
            if (SimulationName != null && simulationIDs.TryGetValue(SimulationName, out id))
                returnValues[0] = simulationIDs[SimulationName];

            for (int i = 0; i < Values.Count(); i++)
            {
                int returnIndex = returnColumnNames.IndexOf(ColumnNames.ElementAt(i));
                if (returnIndex != -1)
                    returnValues[returnIndex] = Values.ElementAt(i);
            }
        }

        /// <summary>
        /// 'Flatten' the row passed in, into a list of columns ready to be added
        /// to a data table.
        /// </summary>
        public void Flatten()
        {
            List<string> newColumnNames = new List<string>();
            List<string> newColumnUnits = new List<string>();
            List<object> newValues = new List<object>();

            for (int i = 0; i < Values.Count(); i++)
            {
                string units = null;
                if (ColumnUnits != null)
                    units = ColumnUnits.ElementAt(i);
                FlattenValue(ColumnNames.ElementAt(i),
                             units,
                             Values.ElementAt(i),
                             newColumnNames, newColumnUnits, newValues);
            }

            ColumnNames = newColumnNames;
            ColumnUnits = newColumnUnits;
            Values = newValues;
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
                        if (attr is JsonIgnoreAttribute)
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
