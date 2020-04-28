namespace Models
{
    using APSIM.Shared.Utilities;
    using Functions;
    using Models.Core;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;

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
    /// - mean
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
        /// <summary>An instance of a locator service.</summary>
        private readonly ILocator locator;

        /// <summary>Are we in the capture window?</summary>
        private bool inCaptureWindow;

        /// <summary>True when from field has no year specified.</summary>
        private bool fromHasNoYear;

        /// <summary>The to field has no year specified.</summary>
        private bool toHasNoYear;

        /// <summary>Reference to the clock model.</summary>
        private readonly IClock clock;

        /// <summary>Reference to the events model.</summary>
        private readonly IEvent events;

        /// <summary>The full name of the variable we are retrieving from APSIM.</summary>
        private string variableName;

        /// <summary>The aggregation function.</summary>
        private string aggregationFunction;

        /// <summary>The full name of the group by variable.</summary>
        private readonly string groupByName;

        /// <summary>From string.</summary>
        private string fromString = null;

        /// <summary>To string.</summary>
        private string toString = null;

        /// <summary>Variable containing a reference to the aggregation start date.</summary>
        private IVariable fromVariable = null;

        /// <summary>Variable containing a reference to the aggregation end date.</summary>
        private IVariable toVariable = null;

        /// <summary>The variable groups containing the variable values.</summary>
        private readonly List<VariableGroup> groups = new List<VariableGroup>();

        /// <summary>
        /// Constructor for an aggregated column.
        /// </summary>
        /// <param name="reportLine">The entire line directory from report.</param>
        /// <param name="clock">An instance of a clock model</param>
        /// <param name="locator">An instance of a locator service</param>
        /// <param name="events">An instance of an events service</param>
        /// <param name="groupByVariableName">Group by variable name.</param>
        /// <param name="from">From clause to use.</param>
        /// <param name="to">To clause to use.</param>
        /// <returns>The newly created ReportColumn</returns>
        public ReportColumn(string reportLine,
                                      IClock clock, ILocator locator, IEvent events,
                                      string groupByVariableName,
                                      string from, string to)
        {
            this.clock = clock;
            this.locator = locator;
            this.events = events;
            if (!string.IsNullOrEmpty(groupByVariableName))
                this.groupByName = groupByVariableName;
            
            var match = ParseReportLine(reportLine);

            var fromString = match.Groups["from"].Value;
            var toString = match.Groups["to"].Value;
            if (string.IsNullOrEmpty(fromString))
            {
                fromString = from;
                toString = to;
            }

            Initialise(aggFunction: match.Groups["agg"].Value,
                       varName: match.Groups["var"].Value,
                       on: match.Groups["on"].Value,
                       alias: match.Groups["alias"]?.Value,
                       from: fromString,
                       to: toString);
        }

        /// <summary>
        /// Units as specified in the descriptor.
        /// </summary>
        public string Units { get; private set; }

        /// <summary>
        /// The column heading.
        /// </summary>
        public string Name { get; set; }

        /// <summary>Retrieve the current value for the specified group number to be stored in the report.</summary>
        public int NumberOfGroups { get { return groups.Count; } }


        /// <summary>
        /// Retrieve the current value to be stored in the report.
        /// </summary>
        public virtual object GetValue(int groupNumber)
        {
            if (groupNumber >= groups.Count)
                groups.Add(new VariableGroup(locator, null, variableName, aggregationFunction));

            if (string.IsNullOrEmpty(aggregationFunction) && string.IsNullOrEmpty(groupByName))
            {
                // This instance is NOT a temporarily aggregated variable and so hasn't 
                // collected a value yet. Do it now.
                groups[groupNumber].StoreValue();
            }

            return groups[groupNumber].GetValue();
        }

        /// <summary>Store a value.</summary>
        public void StoreValue()
        {
            object value = null;
            VariableGroup group = null;
            if (!string.IsNullOrEmpty(groupByName))
            {
                value = locator.Get(groupByName);
                if (value == null)
                    throw new Exception($"Unable to locate group by variable: {groupByName}");

                group = groups.Find(g => g.GroupByValue != null && g.GroupByValue.Equals(value));
            }
            else if (groups.Count > 0)
                group = groups[0];

            if (group == null)
            {
                group = new VariableGroup(locator, value, variableName, aggregationFunction);
                groups.Add(group);
            }
            group.StoreValue();
        }

        /// <summary>
        /// Parse a report variable line.
        /// </summary>
        /// <remarks>
        /// A descriptor is passed in that describes what the column represents.
        /// The syntax of this descriptor is:
        /// Evaluate TypeOfAggregation of APSIMVariable/Expression [from Event/Date to Event/Date] as OutputLabel [Units]
        /// -    TypeOfAggregation – Sum, Mean, Min, Max, First, Last, Diff, (others?) (see below)
        /// -    APSIMVariable/Expression – APSIM output variable or an expression (see below)
        /// -    Event/Date – optional, an events or dates to begin and end the aggregation 
        /// -    OutputLabel – the label to use in the output file
        /// -    Units – optional, the label to use in the output file
        /// TypeOfAggregation
        /// -    Sum – arithmetic summation over  the aggregation period
        /// -    Mean – arithmetic average over  the aggregation period
        /// -    Min – minimum value during the aggregation period
        /// -    Max – maximum value during the aggregation period
        /// -    First – first or earliest value during the aggregation period
        /// -    Last – last or latest value during the aggregation period
        /// -    Diff – difference in the value of the variable or expression from the beginning to the end
        /// -    StdDev - sample standard deviation
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
        /// <returns>The successful RegEx match instance.</returns>
        private Match ParseReportLine(string descriptor)
        {
            var pattern = @"((?<agg>sum|Sum|mean|Mean|min|Min|max|Max|first|First|last|Last|" + // aggregation
                          @"diff|Diff|stddev|Stddev|prod|Prod)\s+of\s+)?" +                     // more aggregation
                          $@"(?<var>((?!\s+from\s+|\s+as\s+|\s+on\s+).)+)" +                    // APSIM variable or expression
                          $@"(\s+on\s+(?<on>((?!\s+from\s+|\s+as\s+).)+))?" +                   // on keyword
                          $@"(\s+from\s+(?<from>\S+)\s+to\s+(?<to>((?!\s+as)\S)+))?" +          // from and to keywords
                          @"(\s+as\s+(?<alias>[\w.]+))?";                                       // alias

            var regEx = new Regex(pattern);
            var match = regEx.Match(descriptor);
            if (!match.Success)
                throw new Exception($"Invalid format for report aggregation variable {descriptor}");
            return match;
        }

        /// <summary>
        /// Initialise the column instance.
        /// </summary>
        /// <param name="aggFunction">The aggregation function.</param>
        /// <param name="varName">The name of the variable to get from APSIM.</param>
        /// <param name="on">The collection event.</param>
        /// <param name="alias">The alias.</param>
        /// <param name="from">The from variable.</param>
        /// <param name="to">The to variable.</param>
        private void Initialise(string aggFunction, string varName, string on, string alias,
                                string from, string to)
        {
            aggregationFunction = aggFunction;
            variableName = varName;
            fromString = from;
            toString = to;
            Name = alias;

            // specify a column heading if alias was not specified.
            if (string.IsNullOrEmpty(Name))
            {
                // Look for an array specification. The aim is to encode the starting
                // index of the array into the column name. e.g. 
                // for a variableName of [2:4], columnName = [2]
                // for a variableName of [3:], columnName = [3]
                // for a variableName of [:5], columnNamne = [0]

                Regex regex = new Regex("\\[([0-9]):*[0-9]*\\]");

                Name = regex.Replace(variableName.Replace("[:", "[1:"), "($1)");

                // strip off square brackets.
                Name = Name.Replace("[", string.Empty).Replace("]", string.Empty);
            }

            // Try and get units.
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

            if (string.IsNullOrEmpty(fromString))
                inCaptureWindow = true;
            else
            {
                // temporarly aggregated variable
                // subscribe to the capture event
                var collectionEventName = "[Clock].DoReportCalculations";
                if (!string.IsNullOrEmpty(on))
                    collectionEventName = on;
                events.Subscribe(collectionEventName, OnDoReportCalculations);

                // subscribe to the start of day event so that we can determine if we're in the capture window.
                events.Subscribe("[Clock].DoDailyInitialisation", OnStartOfDay);

                fromVariable = Apsim.GetVariableObject(clock as IModel, fromString);
                toVariable = Apsim.GetVariableObject(clock as IModel, toString);
                if (fromVariable != null)
                {
                    // A from variable name  was specified.
                }
                else if (DateTime.TryParse(fromString, out DateTime date))
                {
                    // The from date is a static, hardcoded date string. ie 1-Jan, 1/1/2012, etc.
                    fromVariable = new VariableObject(date);

                    // If the date string does not contain a year (ie 1-Jan), we ignore year and
                    fromHasNoYear = !fromString.Contains(date.Year.ToString());
                }
                else
                {
                    // Assume the string is an event name.
                    events.Subscribe(fromString, OnFromEvent);
                    inCaptureWindow = true;
                }

                if (toVariable != null)
                {
                    // A to variable name  was specified.
                }
                else if (DateTime.TryParse(toString, out DateTime date))
                {
                    // The from date is a static, hardcoded date string. ie 1-Jan, 1/1/2012, etc.
                    toVariable = new VariableObject(date);

                    // If the date string does not contain a year (ie 1-Jan), we ignore year and
                    toHasNoYear = !toString.Contains(date.Year.ToString());
                }
                else
                {
                    // Assume the string is an event name.
                    events.Subscribe(toString, OnToEvent);
                }
            }
        }

        /// <summary>
        /// Invoked at the start of day.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnStartOfDay(object sender, EventArgs e)
        {
            if (fromVariable != null)
            {
                var fromDate = (DateTime)fromVariable.Value;
                if (fromHasNoYear)
                    fromDate = new DateTime(clock.Today.Year, fromDate.Month, fromDate.Day);
                if (clock.Today == fromDate)
                    OnFromEvent();
            }

            if (inCaptureWindow && toVariable != null)
            {
                var toDate = (DateTime)toVariable.Value;
                if (toHasNoYear)
                    toDate = new DateTime(clock.Today.Year, toDate.Month, toDate.Day);

                if (clock.Today == toDate.AddDays(1))
                    OnToEvent();
            }
        }

        /// <summary>
        /// Invoked when the from event is invoked or when today is the from date.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnFromEvent(object sender = null, EventArgs e = null)
        {
            groups.ForEach(g => g.Clear());
            inCaptureWindow = true;
        }

        /// <summary>Invoked when the to event is invoked or when today is the to date.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnToEvent(object sender = null, EventArgs e = null)
        {
            inCaptureWindow = false;
        }

        /// <summary>
        /// Called once per day. Stores values for aggregation.
        /// Note: this could be called before or after reporting occurs.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDoReportCalculations(object sender, EventArgs e)
        {
            if (inCaptureWindow)
                StoreValue();
        }
    }
}