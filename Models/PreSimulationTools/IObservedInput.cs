namespace Models.PreSimulationTools
{
    /// <summary>
    /// Interface for common ObservedInput functions.
    /// </summary>
    public interface IObservedInput
    {
        /// <summary>
        /// Returns an array of column names.
        /// </summary>
        /// <returns></returns>
        string[] ColumnNames { get; }

        /// <summary>
        /// Returns an array of sheet names.
        /// </summary>
        string[] SheetNames { get; }

    }
}
