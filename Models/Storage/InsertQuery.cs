namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>Encapsulates an insert query for a table.</summary>
    class InsertQuery
    {
        /// <summary>Cache of queries.</summary>
        private List<Tuple<int, IntPtr>> queryCache = new List<Tuple<int, IntPtr>>();

        /// <summary>Name of table that this query belongs to.</summary>
        public string TableName { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tableNameForQuery">Name of table.</param>
        public InsertQuery(string tableNameForQuery)
        {
            TableName = tableNameForQuery;
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
            if (database is SQLite)
            {
                var sqlite = database as SQLite;
                var queryHandle = GetPreparedQuery(sqlite, columnNames);
                sqlite.BindParametersAndRunQuery(queryHandle, rowValues);
            }
            else
            {
                List<object[]> values = new List<object[]>() { rowValues.ToArray() };
                database.InsertRows(TableName, columnNames.ToList(), values);
            }
        }

        /// <summary>Get a prepared query for the specified column names.</summary>
        /// <param name="sqlite">The database to write to.</param>
        /// <param name="columnNames">The column names to get the query for.</param>
        public IntPtr GetPreparedQuery(SQLite sqlite, IEnumerable<string> columnNames)
        {
            int key = columnNames.Aggregate(0, (current, item) => current + item.GetHashCode());

            var foundQuery = queryCache.Find(q => q.Item1 == key);
            if (foundQuery == null)
            {
                IntPtr queryHandle = IntPtr.Zero;
                var sql = sqlite.CreateInsertSQL(TableName, columnNames);
                queryHandle = sqlite.Prepare(sql);
                queryCache.Add(new Tuple<int, IntPtr>(key, queryHandle));

                // Ensure the number of prepared queries doesn't exceed 5.
                if (queryCache.Count > 5)
                {
                    sqlite.Finalize(queryCache[0].Item2);
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
                if (database is SQLite)
                    (database as SQLite).Finalize(query.Item2);
            queryCache.Clear();
        }
    }
}
