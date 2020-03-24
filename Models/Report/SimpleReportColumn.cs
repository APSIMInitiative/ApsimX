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
    /// A class for looking after a column of output that has no group by or 
    /// temporal aggregation.
    /// </summary>
    [Serializable]
    public class SimpleReportColumn : IReportColumn
    {
        /// <summary>An instance of a locator service.</summary>
        private readonly ILocator locator;

        /// <summary>The full name of the variable we are retrieving from APSIM.</summary>
        private readonly string variableName;


        /// <summary>
        /// Constructor for a plain report variable.
        /// </summary>
        /// <param name="reportLine">The variable line from report.</param>
        /// <param name="locator">An instance of a locator service</param>
        /// <param name="events">An instance of an events service</param>
        public SimpleReportColumn(string reportLine, ILocator locator, IEvent events)
        {
            variableName = reportLine;
            Name = StringUtilities.SplitOffAfterDelimiter(ref variableName, " as ");
            variableName = variableName.Trim();

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

            this.locator = locator;
            IVariable var = locator.GetObject(variableName);
            if (var != null)
            {
                Units = var.UnitsLabel;
                if (Units != null && Units.StartsWith("(") && Units.EndsWith(")"))
                    Units = Units.Substring(1, Units.Length - 2);
            }
        }

        /// <summary>
        /// Units as specified in the descriptor.
        /// </summary>
        public string Units { get; private set; }

        /// <summary>
        /// The column heading.
        /// </summary>
        public string Name { get; set; }

        /// <summary>Get the number of groups.</summary>
        public int NumberOfGroups { get { return 1; } }

        /// <summary>Retrieve the current value to be stored in the report.</summary>
        /// <param name="groupNumber">Group number - ignored here.</param>
        public object GetValue(int groupNumber)
        {
            return GetValue(variableName, locator);
        }

        /// <summary>Retrieve the current value to be stored in the report.</summary>
        /// <param name="variableName">Variable name to get.</param>
        /// <param name="locator">Locator to use to find variable.</param>
        public static object GetValue(string variableName, ILocator locator)
        {
            object value = locator.Get(variableName);
            if (value == null)
                throw new Exception($"Unable to locate report variable: {variableName}");
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
    }
}