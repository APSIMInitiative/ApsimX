using System.Collections.Generic;
using System.Data;

namespace Models.Interfaces
{
    /// <summary>An interface for editable tabular data.</summary>
    public interface ITabularData
    {
        /// <summary>Get tabular data.</summary>
        DataTable TabularData { get; set; }

        /// <summary>Get possible units for a column.</summary>
        IEnumerable<string> GetUnits(int columnIndex);

        /// <summary>Set units for a column.</summary>
        void SetUnits(int columnIndex, string units);
    }
}
