using System.Data;

namespace Models.Interfaces
{
    /// <summary>An interface for editable tabular data.</summary>
    public interface ITabularData
    {
        /// <summary>Tabular data.</summary>
        public DataTable GetData();
    }
}
