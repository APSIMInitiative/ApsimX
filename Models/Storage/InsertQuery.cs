using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;

namespace Models.Storage
{

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
            var queryHandle = GetPreparedQuery(database);
            database.RunBindableQuery(queryHandle, rowValues);
        }

        /// <summary>Get a prepared query for the specified column names.</summary>
        /// <param name="database">The database to write to.</param>
        public object GetPreparedQuery(IDatabaseConnection database)
        {
            // Get a list of column names.
            var columnNames = dataTable.Columns.Cast<DataColumn>().Select(col => col.ColumnName);

            int key = columnNames.Aggregate(0, (current, item) => current + item.GetHashCode());

            var foundQuery = queryCache.Find(q => q.Item1 == key);
            if (foundQuery == null)
            {
                object queryHandle = database.PrepareBindableInsertQuery(dataTable);
                queryCache.Add(new Tuple<int, object>(key, queryHandle));

                // Ensure the number of prepared queries doesn't exceed 5.
                if (queryCache.Count > 5)
                {
                    database.FinalizeBindableQuery(queryCache[0].Item2);
                    queryCache.RemoveAt(0);
                }
                return queryHandle;
            }
            else
                return foundQuery.Item2;
        }

        internal void Close(IDatabaseConnection database)
        {
            foreach (var query in queryCache)
                database.FinalizeBindableQuery(query.Item2);
            queryCache.Clear();
        }
    }
}
