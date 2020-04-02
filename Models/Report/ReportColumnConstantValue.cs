namespace Models
{
    using System;

    /// <summary>A class for outputting a constant value in a report column.</summary>
    [Serializable]
    public class ReportColumnConstantValue : IReportColumn
    {
        /// <summary>The column name for the constant</summary>
        public string Name { get; private set; }

        /// <summary>The column name for the constant</summary>
        public string Units { get; private set; }

        /// <summary>The constant value</summary>
        private object value;

        /// <summary>
        /// Constructor for a plain report variable.
        /// </summary>
        /// <param name="columnName">The column name to write to the output</param>
        /// <param name="units">Units of measurement</param>
        /// <param name="constantValue">The constant value</param>
        public ReportColumnConstantValue(string columnName, object constantValue, string units = null)
        {
            Name = columnName;
            Units = units;
            value = constantValue;
        }

        /// <summary>Retrieve the current value</summary>
        public object GetValue(int groupNumber)
        {
            return value;
        }

        /// <summary>Retrieve the current value for the specified group number to be stored in the report.</summary>
        public int NumberOfGroups {  get { return 1; } }
    }
}
