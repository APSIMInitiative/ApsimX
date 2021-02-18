namespace Models
{
    /// <summary>An interface for a column in a report table.</summary>
    public interface IReportColumn
    {
        /// <summary>Name of column.</summary>
        string Name { get; }

        /// <summary>Units of measurement</summary>
        string Units { get; }

        /// <summary>Retrieve the current value.</summary>
        /// <param name="groupNumber">The group number to retrieve the value for.</param>
        object GetValue(int groupNumber);

        /// <summary>Gets the number of groups.</summary>
        int NumberOfGroups { get; }
    }
}