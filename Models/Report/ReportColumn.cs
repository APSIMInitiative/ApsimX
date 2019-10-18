// -----------------------------------------------------------------------
// <copyright file="ReportColumn.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Functions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Models.Storage;

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
    ///      
    /// # Features
    /// - 
    /// 
    /// ## Aggregation
    /// 
    /// Syntax:
    /// 
    /// *function* of *variable* from *date* to *date*
    /// 
    /// *function* can be any of the following:
    /// 
    /// - sum
    /// - avg
    /// - min
    /// - max
    /// - first
    /// - last
    /// - diff
    /// 
    /// *variable* is any valid reporting variable.
    /// 
    /// There are three ways to specify aggregation dates:
    /// 
    /// 1. Fixed/static date
    /// 
    /// Description
    /// 
    /// e.g. 1-Jan, 1/1/2012, etc.
    /// 
    /// 2. Apsim variable
    /// 
    /// The date can point to any Apsim variable which is of type DateTime.
    /// 
    /// e.g. [Clock].StartDate, [Clock].Today, [Report].LastReportDate, etc.
    /// 
    /// 3. Apsim event
    /// 
    /// The date can point to any Apsim event.
    /// 
    /// e.g. [Clock].StartOfYear, [Wheat].Harvesting, etc.
    /// 
    /// </summary>
    /// <remarks>
    /// Need tests for:
    /// 
    /// 1. All permutations of date specification types with all types of aggregation.
    /// 
    /// </remarks>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    [Serializable]
    public class ReportColumn : IReportColumn
    {
        private enum AggregationType
        {
            sum,
            avg,
            min,
            max,
            first,
            last,
            diff
        };

        /// <summary>
        /// An instance of a storage service.
        /// </summary>
        private IStorageWriter storage;

        /// <summary>
        /// An instance of a locator service.
        /// </summary>
        private ILocator locator;

        /// <summary>
        /// An instance of an events service.
        /// </summary>
        private IEvent events;

        /// <summary>
        /// When aggregating values over multiple dates, we need to store the variable value each day, before reporting
        /// occurs. However, which event do we subscribe to? Reporting can potentially occur on any event. We also need
        /// to consider that reporting does not necessarily happen every day so we can't just store the values
        /// immediately before reporting inside the GetValue() method. This solution is to always store aggregation
        /// values in the DoReportCalculations event, but also to store the value immediately before reporting if we
        /// haven't already stored the value for today. This boolean allows us to do this.
        /// </summary>
        private bool haveAggregatedValuesToday;

        /// <summary>
        /// True when from field has no year specified.
        /// </summary>
        private bool fromHasNoYear;

        /// <summary>
        /// The to field has no year specified.
        /// </summary>
        private bool toHasNoYear;

        /// <summary>
        /// True iff 'from' date is an event.
        /// </summary>
        private bool toIsEvent;

        /// <summary>
        /// True iff 'to' date is an event.
        /// </summary>
        private bool fromIsEvent;

        /// <summary>
        /// Reference to the clock model.
        /// </summary>
        private IClock clock;

        /// <summary>
        /// The full name of the variable we are retrieving from APSIM.
        /// </summary>
        private string variableName;

        /// <summary>
        /// The values for each report event (e.g. daily).
        /// </summary>
        private List<object> valuesToAggregate = new List<object>();

        /// <summary>
        /// The aggregation function if specified. Null if not specified.
        /// </summary>
        private string aggregationFunction;

        /// <summary>
        /// Variable containing a reference to the aggregation start date.
        /// </summary>
        private IVariable fromVariable = null;

        /// <summary>
        /// Variable containing a reference to the aggregation end date.
        /// </summary>
        private IVariable toVariable = null;

        /// <summary>
        /// Constructor for an aggregated column.
        /// </summary>
        /// <param name="aggregationFunction">The aggregation function</param>
        /// <param name="variableName">The name of the APSIM variable to retrieve</param>
        /// <param name="columnName">The column name to write to the output</param>
        /// <param name="from">The beginning of the capture window</param>
        /// <param name="to">The end of the capture window</param>
        /// <param name="clock">An instance of a clock model</param>
        /// <param name="storage">An instance of a storage service</param>
        /// <param name="locator">An instance of a locator service</param>
        /// <param name="events">An instance of an events service</param>
        /// <returns>The newly created ReportColumn</returns>
        private ReportColumn(string aggregationFunction, string variableName, string columnName, object from, object to, 
                             IClock clock, IStorageWriter storage, ILocator locator, IEvent events)
        {
            this.aggregationFunction = aggregationFunction;
            this.variableName = variableName;
            this.Name = columnName;
            this.storage = storage;
            this.locator = locator;
            this.events = events;
            this.clock = clock;
            try
            {
                IVariable var = locator.GetObject(variableName);
                if (var != null)
                {
                    Units = var.UnitsLabel;
                    if (Units != null && Units.StartsWith("(") && Units.EndsWith(")"))
                        Units = Units.Substring(1, Units.Length - 2);
                }
            }
            catch (Exception)
            {
            }

            events.Subscribe("[Clock].DoReportCalculations", this.DoReportCalculations);
            events.Subscribe("[Clock].DoDailyInitialisation", this.StartOfDay);

            if (DateTime.TryParse(from.ToString(), out DateTime date))
            {
                // The from date is a static, hardcoded date string. ie 1-Jan, 1/1/2012, etc.
                this.fromVariable = new VariableObject(date);

                // If the date string does not contain a year (ie 1-Jan), we ignore year and
                this.fromHasNoYear = !from.ToString().Contains(date.Year.ToString());
            }
            else if (from is IVariable)
                this.fromVariable = from as IVariable;
            else
            {
                // Assume the string is an event name.
                events.Subscribe(from.ToString(), this.OnBeginCapture);
                fromIsEvent = true;
            }

            if (DateTime.TryParse(to.ToString(), out date))
            {
                // The from date is a static, hardcoded date string. ie 1-Jan, 1/1/2012, etc.
                this.toVariable = new VariableObject(date);

                // If the date string does not contain a year (ie 1-Jan), we ignore year and
                this.toHasNoYear = !to.ToString().Contains(date.Year.ToString());
            }
            else if (to is IVariable)
                this.toVariable = to as IVariable;
            else
            {
                // Assume the string is an event name.
                events.Subscribe(to.ToString(), this.OnEndCapture);
                toIsEvent = true;
            }
        }

        /// <summary>
        /// Constructor for a plain report variable.
        /// </summary>
        /// <param name="variableName">The name of the APSIM variable to retrieve</param>
        /// <param name="columnName">The column name to write to the output</param>
        /// <param name="clock">An instance of a clock model</param>
        /// <param name="storage">An instance of a storage service</param>
        /// <param name="locator">An instance of a locator service</param>
        /// <param name="events">An instance of an events service</param>
        private ReportColumn(string variableName, string columnName, 
                             IClock clock, IStorageWriter storage, ILocator locator, IEvent events)
        {
            this.variableName = variableName.Trim();
            this.Name = columnName;
            this.storage = storage;
            this.locator = locator;
            this.events = events;
            this.clock = clock;
            try
            {
                IVariable var = locator.GetObject(variableName);
                if (var != null)
                {
                    Units = var.UnitsLabel;
                    if (Units != null && Units.StartsWith("(") && Units.EndsWith(")"))
                        Units = Units.Substring(1, Units.Length - 2);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Units as specified in the descriptor.
        /// </summary>
        public string Units { get; private set; }

        /// <summary>
        /// The column heading.
        /// </summary>
        public string Name { get; set; }
        
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
        /// <param name="clock">An instance of a clock model</param>
        /// <param name="storage">An instance of a storage service</param>
        /// <param name="locator">An instance of a locator service</param>
        /// <param name="events">An instance of an event service</param>
        /// <returns>The newly created ReportColumn</returns>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        public static ReportColumn Create(string descriptor, IClock clock, IStorageWriter storage, ILocator locator, IEvent events)
        {
            string columnName = StringUtilities.RemoveWordAfter(ref descriptor, "as");
            object to = StringUtilities.RemoveWordAfter(ref descriptor, "to");
            object from = StringUtilities.RemoveWordAfter(ref descriptor, "from");
            if (clock is IModel)
            {
                if (from != null)
                {
                    IVariable fromValue = Apsim.GetVariableObject(clock as IModel, from.ToString());
                    if (fromValue != null)
                        from = fromValue;
                }
                if (to != null)
                {
                    IVariable toValue = Apsim.GetVariableObject(clock as IModel, to.ToString());
                    if (toValue != null)
                        to = toValue;
                }
            }
            string aggregationFunction = StringUtilities.RemoveWordBefore(ref descriptor, "of");

            string variableName = descriptor;  // variable name is what is left over.

            // specify a column heading if alias was not specified.
            if (columnName == null)
                columnName = variableName.Replace("[", string.Empty).Replace("]", string.Empty);

            if (aggregationFunction != null)
                return new ReportColumn(aggregationFunction, variableName, columnName, from, to, clock, storage, locator, events);
            else
                return new ReportColumn(variableName, columnName, clock, storage, locator, events);
        }

        /// <summary>
        /// Retrieve the current value to be stored in the report.
        /// </summary>
        public virtual object GetValue()
        {
            if (aggregationFunction == null)
                return GetVariableValue();

            return ApplyAggregation();
        }

        private void BeginCapture()
        {
            if (fromIsEvent)
                fromVariable = new VariableObject(ReflectionUtilities.Clone(clock.Today));

            // The 'from' event has fired, so we want to start capturing values, but the 'to' 
            // event may not have fired. Therefore, we set the 'to' variable to DateTime.MaxValue.
            if (toIsEvent)
                toVariable = new VariableObject(DateTime.MaxValue);

            // First day in capture window, we need to remove any aggregated values.
            valuesToAggregate.Clear();
            haveAggregatedValuesToday = false;
            StoreValueForAggregation();
        }

        private DateTime GetFromDate()
        {
            if (fromVariable == null)
                // From date is an event.
                return DateTime.MaxValue;

            return (DateTime)fromVariable.Value;
        }

        private DateTime GetToDate()
        {
            if (toVariable == null)
                return DateTime.MaxValue;

            return (DateTime)toVariable.Value;
        }

        private bool AfterFrom()
        {
            DateTime from = GetFromDate();

            if (fromHasNoYear)
            {
                // From date is hardcoded without a year. e.g. 1-Jan.
                // This is complicated by aggregation over the year boundary - e.g. from 20-Dec to 10-Jan.
                // We can't just check that today's doy > from.doy, because in the above example, this would return
                // false once we enter January in the next year.
                DateTime to = GetToDate();
                if (from.DayOfYear > to.DayOfYear)
                    return clock.Today.DayOfYear >= from.DayOfYear || clock.Today.DayOfYear <= to.DayOfYear;
                return clock.Today.DayOfYear >= from.DayOfYear;
            }

            return clock.Today >= from;
        }

        private bool BeforeTo()
        {
            DateTime to = GetToDate();

            if (toHasNoYear)
            {
                // To date is hardcoded without a year. e.g. 1-Jan.
                // This is complicated by aggregation over the year boundary - e.g. from 20-Dec to 10-Jan.
                // We can't just check that today's doy < to.doy.
                DateTime from = GetFromDate();
                if (from.DayOfYear > to.DayOfYear)
                    return clock.Today.DayOfYear >= from.DayOfYear || clock.Today.DayOfYear <= to.DayOfYear;

                return clock.Today.DayOfYear <= to.DayOfYear;
            }

            return clock.Today <= to;
        }

        /// <summary>
        /// Returns true iff today's date lies inside the aggregation window.
        /// </summary>
        private bool InCaptureWindow()
        {
            bool afterFrom = AfterFrom();
            bool beforeTo = BeforeTo();

            return afterFrom && beforeTo;
        }

        /// <summary>
        /// Gets the value of the variable/expression.
        /// </summary>
        private object GetVariableValue()
        {
            object value = locator.Get(variableName);

            if (value is IFunction function)
                value = function.Value();
            else if (value != null && (value.GetType().IsArray || value.GetType().IsClass))
            {
                try
                {
                    value = ReflectionUtilities.Clone(value);
                }
                catch (Exception err)
                {
                    throw new Exception($"Cannot report variable \"{variableName}\": Variable is a non-reportable type: \"{value?.GetType()?.Name}\".", err);
                }
            }

            return value;
        }

        /// <summary>
        /// Retrieve the current value and store it in our aggregation array of values.
        /// </summary>
        private void StoreValueForAggregation()
        {
            if (!haveAggregatedValuesToday && InCaptureWindow())
            {
                valuesToAggregate.Add(GetVariableValue());
                haveAggregatedValuesToday = true;
            }
        }

        /// <summary>
        /// Apply the aggregation function if necessary to the list of values wehave stored.
        /// </summary>
        private object ApplyAggregation()
        {
            double result = double.NaN;
            if (this.valuesToAggregate.Count > 0 && this.aggregationFunction != null)
            {
                if (this.aggregationFunction.Equals("sum", StringComparison.CurrentCultureIgnoreCase))
                    if (this.valuesToAggregate[0].GetType() == typeof(double))
                        result = MathUtilities.Sum(this.valuesToAggregate.Cast<double>());
                    else if (this.valuesToAggregate[0].GetType() == typeof(int))
                        result = MathUtilities.Sum(this.valuesToAggregate.Cast<int>());
                    else
                        throw new Exception("Unable to use sum function for variable of type " + this.valuesToAggregate[0].GetType().ToString());
                else if (this.aggregationFunction.Equals("avg", StringComparison.CurrentCultureIgnoreCase))
                    result = MathUtilities.Average(this.valuesToAggregate);
                else if (this.aggregationFunction.Equals("min", StringComparison.CurrentCultureIgnoreCase))
                    result = MathUtilities.Min(this.valuesToAggregate);
                else if (this.aggregationFunction.Equals("max", StringComparison.CurrentCultureIgnoreCase))
                    result = MathUtilities.Max(this.valuesToAggregate);
                else if (this.aggregationFunction.Equals("first", StringComparison.CurrentCultureIgnoreCase))
                    result = Convert.ToDouble(this.valuesToAggregate.First(), System.Globalization.CultureInfo.InvariantCulture);
                else if (this.aggregationFunction.Equals("last", StringComparison.CurrentCultureIgnoreCase))
                    result = Convert.ToDouble(this.valuesToAggregate.Last(), System.Globalization.CultureInfo.InvariantCulture);
                else if (this.aggregationFunction.Equals("diff", StringComparison.CurrentCultureIgnoreCase))
                    result = Convert.ToDouble(this.valuesToAggregate.Last(), System.Globalization.CultureInfo.InvariantCulture) -
                                    Convert.ToDouble(this.valuesToAggregate.First(), System.Globalization.CultureInfo.InvariantCulture);
            }
            return result;
        }

        /// <summary>
        /// The from property is an event name. This is the event handler for the from event.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnBeginCapture(object sender, EventArgs e)
        {
            BeginCapture();
        }

        /// <summary>
        /// The to property is an event name. This is the event handler for the to event.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnEndCapture(object sender, EventArgs e)
        {
            toVariable = new VariableObject(ReflectionUtilities.Clone(clock.Today));
        }

        /// <summary>
        /// Called once per day. Stores values for aggregation.
        /// Note: this could be called before or after reporting occurs.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void DoReportCalculations(object sender, EventArgs e)
        {
            StoreValueForAggregation();
        }

        /// <summary>
        /// Called at start of day. Resets daily global variables.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void StartOfDay(object sender, EventArgs e)
        {
            haveAggregatedValuesToday = false;

            // If today is the from date we should initiate variable capturing for this aggregation window.
            DateTime from = GetFromDate();
            if (clock.Today.DayOfYear == from.DayOfYear && (fromHasNoYear || clock.Today.Year == from.Year))
                BeginCapture();

            // This is an edge case for aggregation from Report.DateOfLastOutput, which is only updated at the end of the day.
            // Check if yesterday was last report date.
            if (fromVariable != null && fromVariable.Name == ".DateOfLastOutput" && (from.DayOfYear + 1) == clock.Today.DayOfYear && from.Year == clock.Today.Year)
            {
                if (valuesToAggregate != null && valuesToAggregate.Count > 0)
                    valuesToAggregate.RemoveRange(0, valuesToAggregate.Count - 1);

                if (toIsEvent)
                    toVariable = new VariableObject(DateTime.MaxValue);
            }
        }
    }
}