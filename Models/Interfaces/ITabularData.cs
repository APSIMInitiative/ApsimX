using Models.Soils;

namespace Models.Interfaces
{
    /// <summary>An interface for editable tabular data.</summary>
    public interface ITabularData
    {
        /// <summary>Get tabular data. Called by GUI.</summary>
        TabularData GetTabularData();
    }
}
