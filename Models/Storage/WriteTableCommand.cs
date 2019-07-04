namespace Models.Storage
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;

    /// <summary>Encapsulates a row to write to an SQL database.</summary>
    class WriteTableCommand : IRunnable
    {
        /// <summary>The datastore connection.</summary>
        private IDatabaseConnection connection;

        /// <summary>The data to write to the database.</summary>
        private DataTable dataToWrite;
        
        /// <summary>The details of tables in the database.</summary>
        private Dictionary<string, DatabaseTableDetails> tables = new Dictionary<string, DatabaseTableDetails>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Constructor</summary>
        /// <param name="databaseConnection">The database connection to write to.</param>
        /// <param name="dataToWrite">Data to write to table.</param>
        public WriteTableCommand(IDatabaseConnection databaseConnection, DataTable dataToWrite)
        {
            this.connection = databaseConnection;
            this.dataToWrite = dataToWrite;
        }

        /// <summary>Called to run the command. Can throw on error.</summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            if (dataToWrite.Rows.Count > 0)
            {
                if (!tables.TryGetValue(dataToWrite.TableName, out var table))
                {
                    table = new DatabaseTableDetails(connection, dataToWrite.TableName);
                    tables.Add(dataToWrite.TableName, table);
                }

                var query = new InsertQuery(dataToWrite);

                // Make sure the table has the correct columns.
                table.EnsureTableExistsAndHasRequiredColumns(dataToWrite);

                // Get a list of column names.
                var columnNames = dataToWrite.Columns.Cast<DataColumn>().Select(col => col.ColumnName);

                try
                {
                    connection.BeginTransaction();

                    // Write all rows.
                    foreach (DataRow row in dataToWrite.Rows)
                        query.ExecuteQuery(connection, columnNames, row.ItemArray);
                }
                finally
                {
                    connection.EndTransaction();
                    query.Close(connection);
                }
            }
        }
    }
}