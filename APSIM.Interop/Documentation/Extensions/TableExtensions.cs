using System;

using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;


namespace APSIM.Interop.Documentation.Extensions
{
    /// <summary>
    /// Extension methods for MigraDoc tables.
    /// </summary>
    internal static class TableExtensions
    {
        /// <summary>
        /// Get the last row in the table. Throws if no rows exist in the table.
        /// </summary>
        /// <param name="table">A table.</param>
        internal static Row GetLastRow(this Table table)
        {
            if (table.Rows == null || table.Rows.Count < 1)
                throw new InvalidOperationException("Table contains no rows");
            return table.Rows[table.Rows.Count - 1];
        }
    }
}
