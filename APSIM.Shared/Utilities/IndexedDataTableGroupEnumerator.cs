namespace APSIM.Shared.Utilities
{
    using System.Collections.Generic;

    /// <summary>
    /// A group enumerator for the IndexedDataTable class
    /// </summary>
    public class IndexedDataTableGroupEnumerator
    {
        /// <summary>The IndexedDataTable to work with</summary>
        private IndexedDataTable table;

        /// <summary>The index values making up this group</summary>
        public object[] IndexValues { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataTable">The IndexedDataTable to work with</param>
        /// <param name="indexValues">The index values making up this group</param>
        public IndexedDataTableGroupEnumerator(IndexedDataTable dataTable, object[] indexValues)
        {
            table = dataTable;
            IndexValues = indexValues;
        }

        /// <summary>Return the underlying data table</summary>
        public IList<T> Get<T>(string columnName)
        {
            table.SetIndex(IndexValues);
            return table.Get<T>(columnName);
        }
    }
}
