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

        /// <summary>Delete the existing rows first?</summary>
        private bool deleteExistingRows;

        public string Name { get { return "Write table"; } }

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress { get { return 0; } }

        /// <summary>Constructor</summary>
        /// <param name="databaseConnection">The database connection to write to.</param>
        /// <param name="dataToWrite">Data to write to table.</param>
        /// <param name="deleteOldData">Delete the existing rows first?</param>
        public WriteTableCommand(IDatabaseConnection databaseConnection, DataTable dataToWrite, bool deleteOldData)
        {
            this.connection = databaseConnection;
            this.dataToWrite = dataToWrite;
            this.deleteExistingRows = deleteOldData;
        }

        /// <summary>
        /// Prepare the job for running.
        /// </summary>
        public void Prepare()
        {
            // Do nothing.
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

                    if (deleteExistingRows)
                    {
                        // fixme - this assumes that "Current" checkpoint ID is always 1.
                        // This should always be correct afaik, but it would be better to
                        // verify this at runtime.
                        bool tableHasCheckpointID = connection.GetColumns(dataToWrite.TableName).Any(c => c.Item1 == "CheckpointID");
                        connection.ExecuteNonQuery($"DELETE FROM [{dataToWrite.TableName}] {(tableHasCheckpointID ? "WHERE CheckpointID = 1" : "")}");
                    }
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

        /// <summary>
        /// Cleanup the job after running it.
        /// </summary>
        public void Cleanup()
        {
            // Do nothing.
        }
    }
}