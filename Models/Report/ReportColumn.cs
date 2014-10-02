// -----------------------------------------------------------------------
// <copyright file="ReportColumn.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using Models.Core;
using System.Data;

    /// <summary>
    /// A class for looking after a column of output. A column will store a value 
    /// each time it is told to do so (by calling StoreValue method). This value
    /// can be a scalar, an array of scalars, a structure, or an array of structures.
    /// It can handle array sizes changing through a simulation. 
    /// It "flattens" arrays and structures
    /// e.g. if the variable is sw_dep and has 3 elements then
    ///      Names -> sw_dep(1), sw_dep(2), sw_dep(3)
    ///      Types ->    double,    double,    double
    ///      
    /// e.g. if the variable is a struct {double A; double B; double C;}
    ///      Names -> struct.A, struct.B, struct.C
    /// </summary>
    [Serializable]
    public class ReportColumn
    {
        /// <summary>
        /// The parent model. Needed so that we can get values of variables.
        /// </summary>
        private IModel parentModel;

        /// <summary>
        /// The full name of the variable we are retriving from APSIM.
        /// </summary>
        private string fullName;

        /// <summary>
        /// The values for each report event (e.g. daily)
        /// </summary>
        private List<object> values = new List<object>();

        /// <summary>
        /// For array variables, this will be the number of array elements to write to the data table.
        /// </summary>
        private int maximumNumberArrayElements;

        /// <summary>
        /// The data type of this column
        /// </summary>
        private Type valueType;

        /// <summary>
        /// The column heading.
        /// </summary>
        private string heading;

        /// <summary>
        /// Factory create method. Can throw if invalid descriptor found.
        /// </summary>
        /// <remarks>
        /// A descriptor is passed in that describes what the column represents.
        /// The syntax of this descriptor is:
        /// Evaluate TypeOfAggregation of APSIMVariable/Expression [from Event/Date to Event/Date] as OutputLabel [Units]
        /// -	TypeOfAggregation – Sum, Ave, Min, Max, First, Last, Diff, (others?) (see below)
        /// -	APSIMVariable/Expression – APSIM output variable or an expression (see below)
        /// -	Event/Date – optional, an events or dates to begin and end the aggregation 
        /// -	OutputLabel – the label to use in the output file
        /// -	Units – optional, the label to use in the output file
        /// 
        /// TypeOfAggregation
        /// -	Sum – arithmetic summation over  the aggregation period
        /// -	Ave – arithmetic average over  the aggregation period
        /// -	Min – minimum value during the aggregation period
        /// -	Max – maximum value during the aggregation period
        /// -	First – first or earliest value during the aggregation period
        /// -	Last – last or latest value during the aggregation period
        /// -	Diff – difference in the value of the variable or expression from the beginning to the end
        /// -	Others???? Stdev?, sum pos?
        /// APSIMVariable
        /// -	Any output variable or single array element (e.g. sw_dep(1)) from any APSIM module
        /// Expression
        /// -	Needs lots of explanation so see more below
        /// Event or Date
        /// -	Any APSIM event (e.g. ‘sowing’) or date (e.g. ‘31-Dec’, ’15-Jan-2001’)
        /// -	Events are acted on immediately that they are triggered
        /// -	A ‘from’ date is assumed to be at the beginning of the day and a ‘to’ date is assumed to be at the end of the day
        /// -	These are optional.  If omitted then the aggregation is assumed to coincide with the reporting interval
        /// OutputLabel
        /// -	The label to use in the output file
        /// Units
        /// -	The units (e.g. ‘mm’) to use in the output file
        /// -	This is optional.  If omitted then the units will appear are ‘()’
        /// </remarks>
        /// <param name="descriptor">A column descriptor</param>
        /// <param name="report">The parent report model</param>
        public static ReportColumn Create(string descriptor, IModel report)
        {
            ReportColumn column = new ReportColumn();
            column.fullName = descriptor;
            int posAlias = column.fullName.IndexOf(" as ");
            if (posAlias != -1)
            {
                column.heading = column.fullName.Substring(posAlias + 4);
                column.fullName = column.fullName.Substring(0, posAlias);
            }
            else
                column.heading = column.fullName.Replace("[", "").Replace("]", "");

            column.parentModel = report;
            return column;
        }

        /// <summary>
        /// Retrieve the current value and store it in our array of values.
        /// </summary>
        public void StoreValue()
        {
            object Value = Apsim.Get(parentModel.Parent, fullName);

            if (Value == null)
                values.Add(null);
            else
            {
                if (Value.GetType().IsArray || Value.GetType().IsClass)
                    Value = Utility.Reflection.Clone(Value);
                
                values.Add(Value);
            }
        }

        /// <summary>
        /// Add the required columns to the specified data table.
        /// </summary>
        /// <param name="table">The data table to add columns to</param>
        public void AddColumnsToTable(DataTable table)
        {
            object firstValue = FirstNonBlankValue();
            valueType = null;
            if (firstValue != null)
            {
                valueType = firstValue.GetType();
                if (valueType.IsArray)
                    maximumNumberArrayElements = GetMaximumNumberArrayElements();
            }
            List<FlattenedValue> flattenedValues = FlattenValue(firstValue, heading, valueType);

            foreach (FlattenedValue column in flattenedValues)
                table.Columns.Add(column.name, column.type);
        }

        /// <summary>
        /// Add the required number of rows to the table.
        /// </summary>
        /// <param name="table"></param>
        public void AddRowsToTable(DataTable table)
        {
            // Ensure there are enough data rows in the table.
            while (table.Rows.Count < values.Count)
                table.Rows.Add(table.NewRow());

            for (int rowIndex = 0; rowIndex < values.Count; rowIndex++)
            {
                List<FlattenedValue> flattenedValues = FlattenValue(values[rowIndex], heading, valueType);

                foreach (FlattenedValue column in flattenedValues)
                {
                    if (column.value != null)
                        table.Rows[rowIndex][column.name] = column.value;
                }
            }
        }

        /// <summary>
        /// Go through values and return the first non blank one. 
        /// </summary>
        /// <returns>Returns first non blank value or null if all are missing</returns>
        private object FirstNonBlankValue()
        {
            foreach (object value in values)
                if (value != null)
                    return value;
            return null;
        }

        /// <summary>
        /// 'Flatten' then object passed in, into a list of columns ready to be added
        /// to a data table.
        /// </summary>
        /// <param name="value">The object to be analysed and flattened</param>
        /// <param name="name">The name of the object</param>
        /// <param name="type">Type type of the object</param>
        /// <returns></returns>
        private List<FlattenedValue> FlattenValue(object value, string name, Type type)
        {
            List<FlattenedValue> flattenedValues = new List<FlattenedValue>();

            if (type == null)
            {
                // Whole column is null.
                flattenedValues.Add(new FlattenedValue() { name = name, type = typeof(int) });
            }
            else if (type.IsArray)
            {
                // Array
                Array array = value as Array;
                for (int columnIndex = 0; columnIndex < maximumNumberArrayElements; columnIndex++)
                {
                    string heading = name;
                    heading += "(" + (columnIndex + 1).ToString() + ")";
                    if (array == null || columnIndex >= array.Length)
                        flattenedValues.Add(new FlattenedValue() { name = heading, type = type });
                    else
                    {
                        object arrayElement = array.GetValue(columnIndex);
                        flattenedValues.AddRange(FlattenValue(arrayElement, heading, array.GetType().GetElementType()));
                    }
                }
            }
            else if (type == typeof(DateTime) || type == typeof(string) || !type.IsClass)
            {
                // Scalar
                flattenedValues.Add(new FlattenedValue() { name = name, type = type, value = value });
            }
            else
            {
                // A struct or class
                foreach (PropertyInfo Property in Utility.Reflection.GetPropertiesSorted(type, BindingFlags.Instance | BindingFlags.Public))
                {
                    string heading = name + "." + Property.Name;
                    object classElement = Property.GetValue(value, null);
                    flattenedValues.AddRange(FlattenValue(classElement, heading, classElement.GetType().GetElementType()));
                }
            }

            return flattenedValues;
        }

        /// <summary>
        /// Calculate the maximum number of array elements.
        /// </summary>
        private int GetMaximumNumberArrayElements()
        {
            int MaxNumValues = 0;
            foreach (object Value in values)
            {
                if (Value != null)
                    MaxNumValues = Math.Max(MaxNumValues, (Value as Array).Length);
            }
            return MaxNumValues;
        }

        /// <summary>
        /// A structure to hold a name, type and value. Used in the flattening process.
        /// </summary>
        private struct FlattenedValue
        {
            /// <summary>
            /// The name of a column
            /// </summary>
            public string name;

            /// <summary>
            /// The type of a collumn
            /// </summary>
            public Type type;

            /// <summary>
            /// The value of a column
            /// </summary>
            public object value;
        }
    }
}
