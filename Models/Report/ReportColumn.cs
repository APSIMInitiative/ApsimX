// -----------------------------------------------------------------------
// <copyright file="ReportColumn.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Models.Core;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A class for looking after a column of output. A column will store a value 
    /// each time it is told to do so (by calling StoreValue method). This value
    /// can be a scalar, an array of scalars, a structure, or an array of structures.
    /// It can handle array sizes changing through a simulation. 
    /// It "flattens" arrays and structures
    /// e.g. if the variable is sw_dep and has 3 elements then
    ///      Names -> sw_dep(1), sw_dep(2), sw_dep(3)
    ///      Types ->    double,    double,    double
    /// e.g. if the variable is a struct {double A; double B; double C;}
    ///      Names -> struct.A, struct.B, struct.C
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    [Serializable]
    public class ReportColumn
    {
        /// <summary>
        /// The from field converted to a date.
        /// </summary>
        private DateTime fromDate;

        /// <summary>
        /// True when from field has no year specified
        /// </summary>
        private bool fromHasNoYear;

        /// <summary>
        /// The to field converted as a date
        /// </summary>
        private DateTime toDate;

        /// <summary>
        /// The to field has no year specified
        /// </summary>
        private bool toHasNoYear;

        /// <summary>
        /// The parent model. Needed so that we can get values of variables.
        /// </summary>
        private IModel parentModel;

        /// <summary>
        /// Reference to the clock model.
        /// </summary>
        private Clock clock;

        /// <summary>
        /// The full name of the variable we are retrieving from APSIM.
        /// </summary>
        private string variableName;

        /// <summary>
        /// The column heading.
        /// </summary>
        private string heading;

        /// <summary>
        /// The values for each report event (e.g. daily)
        /// </summary>
        private List<object> values = new List<object>();

        /// <summary>
        /// The values for each report event (e.g. daily)
        /// </summary>
        private List<object> valuesToAggregate = new List<object>();

        /// <summary>
        /// For array variables, this will be the number of array elements to write to the data table.
        /// </summary>
        private int maximumNumberArrayElements;

        /// <summary>
        /// The data type of this column
        /// </summary>
        private Type valueType;

        /// <summary>
        /// True when the simulation is within the capture window for this variable
        /// </summary>
        private bool inCaptureWindow;

        /// <summary>
        /// The aggregation function if specified. Null if not specified.
        /// </summary>
        private string aggregationFunction;

        ///// <summary>
        ///// Units as specified in the descriptor.
        ///// </summary>
        //private string units;

        /// <summary>
        /// The date when an aggregation value was last stored
        /// </summary>
        private DateTime lastStoreDate;

        /// <summary>
        /// The report model frequencies
        /// </summary>
        private List<string> reportingFrequencies = new List<string>();

        /// <summary>
        /// Constructor for an aggregated column.
        /// </summary>
        /// <param name="aggregationFunction">The aggregation function</param>
        /// <param name="variableName">The name of the APSIM variable to retrieve</param>
        /// <param name="columnName">The column name to write to the output</param>
        /// <param name="from">The beginning of the capture window</param>
        /// <param name="to">The end of the capture window</param>
        /// <param name="frequenciesFromReport">Reporting frequencies</param>
        /// <param name="parentModel">The parent model</param>
        /// <returns>The newly created ReportColumn</returns>
        private ReportColumn(string aggregationFunction, string variableName, string columnName, string from, string to, string[] frequenciesFromReport, IModel parentModel)
        {
            this.aggregationFunction = aggregationFunction;
            this.variableName = variableName;
            this.heading = columnName;
            this.parentModel = parentModel;
            this.inCaptureWindow = false;
            this.reportingFrequencies.AddRange(frequenciesFromReport);
            this.clock = Apsim.Find(parentModel, typeof(Clock)) as Clock;

            Apsim.Subscribe(parentModel, "[Clock].StartOfDay", this.OnStartOfDay);
            Apsim.Subscribe(parentModel, "[Clock].DoReportCalculations", this.OnEndOfDay);

            if (DateTime.TryParse(from, out this.fromDate))
                this.fromHasNoYear = !from.Contains(this.fromDate.Year.ToString());
            else
                Apsim.Subscribe(parentModel, from, this.OnBeginCapture);

            if (DateTime.TryParse(to, out this.toDate))
                this.toHasNoYear = !to.Contains(this.toDate.Year.ToString());
            else
                Apsim.Subscribe(parentModel, to, this.OnEndCapture);

            foreach (string frequency in this.reportingFrequencies)
                Apsim.Subscribe(parentModel, frequency, this.OnReportFrequency);
        }

        /// <summary>
        /// Constructor for a plain report variable.
        /// </summary>
        /// <param name="variableName">The name of the APSIM variable to retrieve</param>
        /// <param name="columnName">The column name to write to the output</param>
        /// <param name="frequenciesFromReport">Reporting frequencies</param>
        /// <param name="parentModel">The parent model</param>
        private ReportColumn(string variableName, string columnName, string[] frequenciesFromReport, IModel parentModel)
        {
            this.variableName = variableName;
            this.heading = columnName;
            this.parentModel = parentModel;
            this.reportingFrequencies.AddRange(frequenciesFromReport);
            this.clock = Apsim.Find(parentModel, typeof(Clock)) as Clock;

            foreach (string frequency in this.reportingFrequencies)
                Apsim.Subscribe(parentModel, frequency, this.OnReportFrequency);
        }

        /// <summary>
        /// Factory create method. Can throw if invalid descriptor found.
        /// </summary>
        /// <remarks>
        /// A descriptor is passed in that describes what the column represents.
        /// The syntax of this descriptor is:
        /// Evaluate TypeOfAggregation of APSIMVariable/Expression [from Event/Date to Event/Date] as OutputLabel [Units]
        /// -    TypeOfAggregation – Sum, Ave, Min, Max, First, Last, Diff, (others?) (see below)
        /// -    APSIMVariable/Expression – APSIM output variable or an expression (see below)
        /// -    Event/Date – optional, an events or dates to begin and end the aggregation 
        /// -    OutputLabel – the label to use in the output file
        /// -    Units – optional, the label to use in the output file
        /// TypeOfAggregation
        /// -    Sum – arithmetic summation over  the aggregation period
        /// -    Ave – arithmetic average over  the aggregation period
        /// -    Min – minimum value during the aggregation period
        /// -    Max – maximum value during the aggregation period
        /// -    First – first or earliest value during the aggregation period
        /// -    Last – last or latest value during the aggregation period
        /// -    Diff – difference in the value of the variable or expression from the beginning to the end
        /// -    Others???? Stdev?, sum pos?
        /// APSIMVariable
        /// -    Any output variable or single array element (e.g. sw_dep(1)) from any APSIM module
        /// Expression
        /// -    Needs lots of explanation so see more below
        /// Event or Date
        /// -    Any APSIM event (e.g. ‘sowing’) or date (e.g. ‘31-Dec’, ’15-Jan-2001’)
        /// -    Events are acted on immediately that they are triggered
        /// -    A ‘from’ date is assumed to be at the beginning of the day and a ‘to’ date is assumed to be at the end of the day
        /// -    These are optional.  If omitted then the aggregation is assumed to coincide with the reporting interval
        /// OutputLabel
        /// -    The label to use in the output file
        /// Units
        /// -    The units (e.g. ‘mm’) to use in the output file
        /// -    This is optional.  If omitted then the units will appear are ‘()’
        /// </remarks>
        /// <param name="descriptor">A column descriptor</param>
        /// <param name="report">The parent report model</param>
        /// <param name="frequenciesFromReport">Reporting frequencies</param>
        /// <returns>The newly created ReportColumn</returns>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        public static ReportColumn Create(string descriptor, IModel report, string[] frequenciesFromReport)
        {
            string pattern = @"((?<func>(sum|avg|min|max|first|last|diff))(\s+of\s+))?" +
                             @"(?<variable>\S+)" +
                             @"(\s+from\s+(?<from>\S+)\s+to\s+(?<to>\S+))?" +
                             @"(\s+as\s+(?<alias>.+))?";

            Regex r = new Regex(pattern);
            Match m = r.Match(descriptor);
            if (!m.Success)
                throw new ApsimXException(report, "Invalid report variable found: " + descriptor);

            GroupCollection c = r.Match(descriptor).Groups;
            string variableName = c["variable"].Value;
            string columnName = c["alias"].Value;
            string aggregationFunction = c["func"].Value;
            string from = c["from"].Value;
            string to = c["to"].Value;

            // specify a column heading if alias was not specified.
            if (columnName == string.Empty)
                columnName = variableName.Replace("[", string.Empty).Replace("]", string.Empty);

            if (aggregationFunction != string.Empty)
                return new ReportColumn(aggregationFunction, variableName, columnName, from, to, frequenciesFromReport, report);
            else
                return new ReportColumn(variableName, columnName, frequenciesFromReport, report);
        }

        /// <summary>
        /// Simulation is terminating. Perform clean up.
        /// </summary>
        public void OnSimulationCompleted()
        {
            if (this.fromDate != DateTime.MinValue)
                Apsim.Unsubscribe(this.parentModel, "[Clock].StartOfDay", this.OnStartOfDay);

            if (this.toDate != DateTime.MinValue)
                Apsim.Unsubscribe(this.parentModel, "[Clock].EndOfDay", this.OnEndOfDay);

            foreach (string frequency in this.reportingFrequencies)
                Apsim.Unsubscribe(this.parentModel, frequency, this.OnReportFrequency);
        }

        #region Capture methods

        /// <summary>
        /// The from property is an event name. This is the event handler for the from event.,
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnBeginCapture(object sender, EventArgs e)
        {
            this.StoreValueForAggregation();
            this.inCaptureWindow = true;
        }

        /// <summary>
        /// The to property is an event name. This is the event handler for the to event.,
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnEndCapture(object sender, EventArgs e)
        {
            this.StoreValueForAggregation();
            this.inCaptureWindow = false;
        }

        /// <summary>
        /// Start of day event handler.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnStartOfDay(object sender, EventArgs e)
        {
            // If we're not currently in the capture window, then see if today is the first
            // day of the window. If so then
            if (!this.inCaptureWindow)
            {
                // Look at the date to see if we are on the 'from' date.
                // If so then set the capture flag to true.
                if (this.fromDate == DateTime.MinValue)
                {
                }
                else if (this.fromHasNoYear)
                {
                    DateTime d1 = new DateTime(this.clock.Today.Year, this.fromDate.Month, this.fromDate.Day);
                    DateTime d2 = new DateTime(this.clock.Today.Year, this.toDate.Month, this.toDate.Day);
                    if (d2.DayOfYear <= d1.DayOfYear)
                        d2 = new DateTime(this.clock.Today.Year + 1, this.toDate.Month, this.toDate.Day);
                    if (this.clock.Today >= d1 && this.clock.Today <= d2)
                        this.inCaptureWindow = true;
                }
                else
                {
                    if (this.clock.Today == this.fromDate)
                        this.inCaptureWindow = true;
                }

                // If we have just turned on capture then store a value now.
                if (this.inCaptureWindow)
                    this.StoreValueForAggregation();
            }
        }

        /// <summary>
        /// End of day event handler.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnEndOfDay(object sender, EventArgs e)
        {
            if (this.inCaptureWindow)
            {
                this.StoreValueForAggregation();

                // Look at the date to see if we are on the 'end' date.
                // If so then set the capture flag to false.
                if (this.toDate == DateTime.MinValue)
                {
                }
                else if (this.toHasNoYear)
                {
                    if (this.clock.Today.Day == this.toDate.Day && this.clock.Today.Month == this.toDate.Month)
                        this.inCaptureWindow = false;
                }
                else
                {
                    if (this.clock.Today == this.toDate)
                        this.inCaptureWindow = false;
                }
            }
        }

        /// <summary>
        /// Event handler for report frequencies
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnReportFrequency(object sender, EventArgs e)
        {
            // If we're at the end of the capture window then apply the aggregation.
            if (this.aggregationFunction != null)
            {
                if (!this.inCaptureWindow)
                    this.ApplyAggregation();
                else
                    this.values.Add(null);
            }
            else
                this.StoreValue();
        }

        /// <summary>
        /// Retrieve the current value and store it in our array of values.
        /// </summary>
        private void StoreValue()
        {
            object value = Apsim.Get(this.parentModel.Parent, this.variableName, true);

            if (value == null)
                this.values.Add(null);
            else
            {
                if (value.GetType().IsArray || value.GetType().IsClass)
                {
                    try
                    {
                        value = ReflectionUtilities.Clone(value);
                    }
                    catch (Exception)
                    {
                        throw new ApsimXException(this.parentModel, "Cannot report variable " + this.variableName +
                                                                    ". Variable is not of a reportable type. Perhaps " +
                                                                    " it is a PMF Function that needs a .Value appended to the name.");
                    }
                }

                this.values.Add(value);
            }
        }

        /// <summary>
        /// Retrieve the current value and store it in our aggregation array of values.
        /// </summary>
        private void StoreValueForAggregation()
        {
            if (this.clock.Today != this.lastStoreDate)
            {
                object value = Apsim.Get(this.parentModel.Parent, this.variableName, true);

                if (value == null)
                    this.valuesToAggregate.Add(null);
                else
                {
                    if (value.GetType().IsArray || value.GetType().IsClass)
                        value = ReflectionUtilities.Clone(value);

                    this.valuesToAggregate.Add(value);
                }
                this.lastStoreDate = this.clock.Today;
            }
        }

        /// <summary>
        /// Apply the aggregation function if necessary to the list of values we
        /// have stored.
        /// </summary>
        private void ApplyAggregation()
        {
            double result = double.NaN;
            if (this.valuesToAggregate.Count > 0 && this.aggregationFunction != null)
            {
                if (this.aggregationFunction == "sum")
                    result = MathUtilities.Sum(this.valuesToAggregate);
                else if (this.aggregationFunction == "avg")
                    result = MathUtilities.Average(this.valuesToAggregate);
                else if (this.aggregationFunction == "min")
                    result = MathUtilities.Min(this.valuesToAggregate);
                else if (this.aggregationFunction == "max")
                    result = MathUtilities.Max(this.valuesToAggregate);
                else if (this.aggregationFunction == "first")
                    result = Convert.ToDouble(this.valuesToAggregate.First());
                else if (this.aggregationFunction == "last")
                    result = Convert.ToDouble(this.valuesToAggregate.Last());
                else if (this.aggregationFunction == "diff")
                    result = Convert.ToDouble(this.valuesToAggregate.Last()) - Convert.ToDouble(this.valuesToAggregate.First());

                if (!double.IsNaN(result))
                {
                    this.valuesToAggregate.Clear();
                    this.values.Add(result);
                }
            }
        }

        #endregion

        #region Output methods

        /// <summary>
        /// Add the required columns to the specified data table.
        /// </summary>
        /// <param name="table">The data table to add columns to</param>
        public void AddColumnsToTable(DataTable table)
        {
            object firstValue = this.FirstNonBlankValue();
            this.valueType = null;
            if (firstValue != null)
            {
                this.valueType = firstValue.GetType();
                if (this.valueType.IsArray)
                    this.maximumNumberArrayElements = this.GetMaximumNumberArrayElements();
            }
            List<FlattenedValue> flattenedValues = this.FlattenValue(firstValue, this.heading, this.valueType);

            foreach (FlattenedValue column in flattenedValues)
                table.Columns.Add(column.Name, column.Type);
        }

        /// <summary>
        /// Add the required number of rows to the table.
        /// </summary>
        /// <param name="table">The data table to add rows to</param>
        public void AddRowsToTable(DataTable table)
        {
            // Ensure there are enough data rows in the table.
            while (table.Rows.Count < this.values.Count)
                table.Rows.Add(table.NewRow());

            for (int rowIndex = 0; rowIndex < this.values.Count; rowIndex++)
            {
                List<FlattenedValue> flattenedValues = this.FlattenValue(this.values[rowIndex], this.heading, this.valueType);

                foreach (FlattenedValue column in flattenedValues)
                {
                    if (column.Value != null)
                        table.Rows[rowIndex][column.Name] = column.Value;
                }
            }
        }

        /// <summary>
        /// Go through values and return the first non blank one. 
        /// </summary>
        /// <returns>Returns first non blank value or null if all are missing</returns>
        private object FirstNonBlankValue()
        {
            foreach (object value in this.values)
                if (value != null)
                    return value;
            return null;
        }

        /// <summary>
        /// 'Flatten' then object passed in, into a list of columns ready to be added
        /// to a data table.
        /// </summary>
        /// <param name="value">The object to be analyzed and flattened</param>
        /// <param name="name">The name of the object</param>
        /// <param name="type">Type type of the object</param>
        /// <returns>The list of values that can be written to a data table</returns>
        private List<FlattenedValue> FlattenValue(object value, string name, Type type)
        {
            List<FlattenedValue> flattenedValues = new List<FlattenedValue>();

            if (type == null)
            {
                // Whole column is null.
                flattenedValues.Add(new FlattenedValue() { Name = name, Type = typeof(int) });
            }
            else if (type.IsArray)
            {
                // Array
                Array array = value as Array;
                for (int columnIndex = 0; columnIndex < this.maximumNumberArrayElements; columnIndex++)
                {
                    string heading = name;
                    heading += "(" + (columnIndex + 1).ToString() + ")";
                    if (array == null || columnIndex >= array.Length)
                        flattenedValues.Add(new FlattenedValue() { Name = heading, Type = type });
                    else
                    {
                        object arrayElement = array.GetValue(columnIndex);
                        flattenedValues.AddRange(this.FlattenValue(arrayElement, heading, array.GetType().GetElementType()));
                    }
                }
            }
            else if (type == typeof(DateTime) || type == typeof(string) || !type.IsClass)
            {
                // Scalar
                flattenedValues.Add(new FlattenedValue() { Name = name, Type = type, Value = value });
            }
            else
            {
                // A struct or class
                foreach (PropertyInfo property in ReflectionUtilities.GetPropertiesSorted(type, BindingFlags.Instance | BindingFlags.Public))
                {
                    string heading = name + "." + property.Name;
                    object classElement = property.GetValue(value, null);
                    flattenedValues.AddRange(this.FlattenValue(classElement, heading, classElement.GetType().GetElementType()));
                }
            }

            return flattenedValues;
        }

        /// <summary>
        /// Calculate the maximum number of array elements.
        /// </summary>
        /// <returns>The maximum number of array elements</returns>
        private int GetMaximumNumberArrayElements()
        {
            int maxNumValues = 0;
            foreach (object value in this.values)
            {
                if (value != null)
                    maxNumValues = Math.Max(maxNumValues, (value as Array).Length);
            }
            return maxNumValues;
        }

        /// <summary>
        /// A structure to hold a name, type and value. Used in the flattening process.
        /// </summary>
        private struct FlattenedValue
        {
            /// <summary>
            /// The name of a column
            /// </summary>
            public string Name;

            /// <summary>
            /// The type of a column
            /// </summary>
            public Type Type;

            /// <summary>
            /// The value of a column
            /// </summary>
            public object Value;
        }
        #endregion
    }
}
