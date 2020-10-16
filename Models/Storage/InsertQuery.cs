namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    /// <summary>Encapsulates an insert query for a table.</summary>
    class InsertQuery
    {
        /// <summary>Cache of queries.</summary>
        private List<Tuple<int, object>> queryCache = new List<Tuple<int, object>>();

        /// <summary>
        /// The datatable associated with this query.
        /// </summary>
        private DataTable dataTable;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="table">A DataTable object.</param>
        public InsertQuery(DataTable table)
        {
            this.dataTable = table;
        }

        /// <summary>
        /// Execute the query.
        /// </summary>
        /// <param name="database">The database to write to.</param>
        /// <param name="columnNames">The column names relating to the values.</param>
        /// <param name="rowValues">The values making up the row to write.</param>
        public void ExecuteQuery(IDatabaseConnection database,
                                 IEnumerable<string> columnNames,
                                 IEnumerable<object> rowValues)
        {
            database.InsertRows(dataTable.TableName, columnNames.ToList(), new List<object[]>() { rowValues.ToArray() });
        }
    }
}
