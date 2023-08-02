using System.Data;
using System.IO;
using System.Threading;
using APSIM.Shared.JobRunning;

namespace Models.Storage
{

    /// <summary>Encapsulates a command to revert from a checkpoint.</summary>
    class RevertCheckpointCommand : IRunnable
    {
        private DataStoreWriter writer;
        private int checkpointIDToRevertTo;

        public string Name { get { return "Revert Checkpoint"; } }

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress { get { return 0; } }

        /// <summary>Constructor</summary>
        /// <param name="dataStoreWriter">The datastore writer that called this constructor.</param>
        /// <param name="checkpointID">The new checkpoint name to create.</param>
        public RevertCheckpointCommand(DataStoreWriter dataStoreWriter,
                                       int checkpointID)
        {
            writer = dataStoreWriter;
            checkpointIDToRevertTo = checkpointID;
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
            int currentID = writer.Connection.ExecuteQueryReturnInt("SELECT ID FROM [_Checkpoints] WHERE [Name]='Current'");
            if (currentID != -1)
            {
                if (writer.Connection.TableExists("_CheckpointFiles"))
                {
                    var filesData = writer.Connection.ExecuteQuery("SELECT * FROM [_CheckpointFiles] WHERE [CheckpointID]=" + checkpointIDToRevertTo);
                    // Revert all files.
                    foreach (DataRow row in filesData.Rows)
                        File.WriteAllBytes(row["FileName"] as string, row["Contents"] as byte[]);
                }

                // Revert data
                foreach (var tableName in writer.Connection.GetTableNames())
                {
                    var columnNames = writer.Connection.GetColumnNames(tableName);

                    if (tableName != "_CheckpointFiles" && columnNames.Contains("CheckpointID"))
                    {
                        // Get a comma separated list of column names.
                        string csvFieldNames = null;
                        foreach (string columnName in columnNames)
                        {
                            if (csvFieldNames != null)
                                csvFieldNames += ",";
                            csvFieldNames += "[" + columnName + "]";
                        }

                        // Delete old current values.
                        writer.Connection.ExecuteNonQuery("DELETE FROM [" + tableName +
                                                            "] WHERE CheckpointID = " + currentID);

                        // Copy checkpoint values to current values.
                        writer.Connection.ExecuteNonQuery("INSERT INTO [" + tableName + "] (" + "[CheckpointID]," + csvFieldNames + ")" +
                                                            " SELECT " + currentID + "," + csvFieldNames +
                                                            " FROM [" + tableName +
                                                            "] WHERE [CheckpointID] = " + checkpointIDToRevertTo);
                    }
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
