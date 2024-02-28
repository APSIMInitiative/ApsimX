using System.Data;
using System.Linq;
using System.Threading;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;

namespace Models.Storage
{

    /// <summary>Encapsulates a row to write to an SQL database.</summary>
    class WriteTableCommand : IRunnable
    {
        /// <summary>The datastore connection.</summary>
        private IDatabaseConnection connection;

        /// <summary>The data to write to the database.</summary>
        private DataTable dataToWrite;

        /// <summary>The details of the table to write to.</summary>
        private DatabaseTableDetails tableDetails;

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
        /// <param name="tableDetails">The details of the table to write to.</param>
        /// <param name="deleteOldData">Delete the existing rows first?</param>
        public WriteTableCommand(IDatabaseConnection databaseConnection, DataTable dataToWrite, DatabaseTableDetails tableDetails, bool deleteOldData)
        {
            this.connection = databaseConnection;
            this.dataToWrite = dataToWrite;
            this.tableDetails = tableDetails;
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
                // Make sure the table has the correct columns.
                tableDetails.EnsureTableExistsAndHasRequiredColumns(ref dataToWrite);

                connection.BeginTransaction();
                var query = new InsertQuery(dataToWrite);
                try
                {
                    if (deleteExistingRows && connection.TableExists(dataToWrite.TableName))
                    {
                        // fixme - this assumes that "Current" checkpoint ID is always 1.
                        // This should always be correct afaik, but it would be better to
                        // verify this at runtime.
                        bool tableHasCheckpointID = connection.GetColumns(dataToWrite.TableName).Any(c => c.Item1 == "CheckpointID");
                        connection.ExecuteNonQuery($"DELETE FROM [{dataToWrite.TableName}] {(tableHasCheckpointID ? "WHERE \"CheckpointID\" = 1" : "")}");
                    }

                    if (connection is Firebird)
                    {
                        // Treat messages as a special case
                        // They come in as single-row tables, so writing each
                        // separately is not very efficient.
                        if (dataToWrite.TableName == "_Messages")
                        {
                            (connection as Firebird).InsertMessageRecord(dataToWrite);
                            return;
                        }
                    }

                    // Get a list of column names.
                    var columnNames = dataToWrite.Columns.Cast<DataColumn>().Select(col => col.ColumnName);

                    // Write all rows.
                    foreach (DataRow row in dataToWrite.Rows)
                        query.ExecuteQuery(connection, columnNames, row.ItemArray);
                }
                finally
                {
                    query.Close(connection);
                    connection.EndTransaction();
                }
            }
        }

        /// <summary>
        /// Cleanup the job after running it.
        /// </summary>
        public void Cleanup(System.Threading.CancellationTokenSource cancelToken)
        {
            // Do nothing.
        }
    }
}