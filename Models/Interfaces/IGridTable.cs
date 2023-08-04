using Models.Utilities;

namespace Models.Interfaces
{
    /// <summary>An interface for editable tabular data.</summary>
    public interface IGridTable
    {
        /// <summary>Get tabular data. Called by GUI.</summary>
        GridTable GetGridTable();
    }
}
