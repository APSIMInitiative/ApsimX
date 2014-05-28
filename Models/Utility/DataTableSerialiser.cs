using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utility;
using System.Reflection;
using System.Collections;
using Models.Core;

namespace Utility
{
    /// <summary>
    /// This class serialises an object into a series of DataTables.
    /// The values of fields and properties get serialised to columns / rows in
    /// 1 or more datatables.
    /// </summary>
    public class DataTableSerialiser
    {
        static string[] excludedVariableNames = new string[] { "Name", "Parent", "[Children]", "Children", 
                                                               "Members", 
                                                               "_target", "_methodPtr", "_methodPtrAux" };
        
        /// <summary>
        /// Converts the fields and properties of specified object (obj) to a series of DataTables and returns them.
        /// </summary>
        public static System.Data.DataTable[] Serialise(object obj, bool stateVariables)
        {
            SortedSet<string> AlreadyDone = new SortedSet<string>();
            System.Data.DataTable propertyTable = null;
            return Serialise(null, obj, stateVariables, AlreadyDone, propertyTable);
        }

        /// <summary>
        /// Converts the fields and properties of specified object (obj) to a series of DataTables and returns them.
        /// </summary>
        private static System.Data.DataTable[] Serialise(string name, 
                                                         object obj, 
                                                         bool stateVariables, 
                                                         SortedSet<string> AlreadyDone, 
                                                         System.Data.DataTable propertyTable)
        {
            string Name = Reflection.GetValueOfFieldOrProperty("Name", obj) as string;
            // If we've already serialised this obj then exit.
            if (Name != null && AlreadyDone.Contains(Name))
                return new System.Data.DataTable[0];
            AlreadyDone.Add(Name);

            List<System.Data.DataTable> tables = new List<System.Data.DataTable>();
            System.Data.DataTable previousTable = null;

            List<IVariable> properties = new List<IVariable>();
            properties.AddRange(ModelFunctions.FieldsAndProperties(obj, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy));
            
            foreach (IVariable property in properties)
            {
                bool includeVariable = property.IsParameter;
                if (stateVariables)
                    includeVariable |= property.IsState;
                includeVariable = includeVariable && Array.IndexOf(excludedVariableNames, property.Name) == -1;

                if (includeVariable)
                {
                    object value = property.Value;
                    if (value != null)
                    {
                        string propertyName = property.Name;
                        if (name != null)
                            propertyName = name + "." + propertyName;

                        // look for a description attribute.
                        if (property.Description != null)
                            propertyName = property.Description;

                        // look for units
                        if (property.Units != null)
                            propertyName += property.Units;

                        // Serialise a list of structures.
                        if (property.ArrayType != null && 
                            property.ArrayType.IsClass &&
                            property.ArrayType != typeof(string))
                        {
                            IList list = (IList)value;
                            if (list != null)
                            {
                                for (int i = 0; i < list.Count; i++)
                                {
                                    tables.AddRange(Serialise(property.Name + "[" + i.ToString() + "]",
                                                    list[i], stateVariables, AlreadyDone, propertyTable));
                                }
                            }
                        }

                        // Serialise an array of objects
                        else if (value is Array || value is IList)
                        {
                            IEnumerable array = value as IEnumerable;
                            if (array != null)
                            {
                                List<string> tableValues = new List<string>();

                                foreach (object Value in array)
                                    tableValues.Add(FormatValue(Value));

                                if (tableValues.Count > 0)
                                {
                                    // Do we need to create another table?
                                    if (previousTable == null || previousTable.Rows.Count != tableValues.Count)
                                    {
                                        previousTable = new System.Data.DataTable();
                                        tables.Add(previousTable);
                                    }
                                    Utility.DataTable.AddColumn(previousTable, propertyName, tableValues.ToArray());
                                }
                            }
                        }
                        // Write out a normal property.
                        else if (!value.GetType().IsEnum && stateVariables && value.GetType().FullName.StartsWith("Models."))
                            tables.AddRange(Serialise(property.Name, value, stateVariables, AlreadyDone, propertyTable));

                        else
                        {
                            if (propertyTable == null)
                            {
                                propertyTable = new System.Data.DataTable();
                                propertyTable.Columns.Add("Property name", typeof(string));
                                propertyTable.Columns.Add("Value", typeof(object));
                                tables.Add(propertyTable);
                            }

                            if (value.GetType().IsEnum)
                                value = value.ToString();

                            propertyTable.Rows.Add(new object[] { propertyName + ":", value });
                        }
                    }
                }
            }
            return tables.ToArray();
        }

        /// <summary>
        /// Format the specified value into a string and return the string.
        /// </summary>
        private static string FormatValue(object value)
        {
            if (value == null)
                return "null";
            if (value is double || value is float)
                return System.String.Format("{0:F3}", value);
            else if (value is DateTime)
                return ((DateTime)value).ToString("yyyy-mm-dd");
            else
                return value.ToString();
        }
    }



}
