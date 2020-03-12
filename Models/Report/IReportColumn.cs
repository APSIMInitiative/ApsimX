namespace Models
{
    /// <summary>An interface for a column in a report table.</summary>
    public interface IReportColumn
    {
        /// <summary>Name of column.</summary>
        string Name { get; }

        /// <summary>Units of measurement</summary>
        string Units { get; }

        /// <summary>Retrieve the current value</summary>
        object GetValue();

    }
}