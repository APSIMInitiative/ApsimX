using System.Collections.Generic;
using System.Threading;
using APSIM.Shared.JobRunning;

namespace Models.Storage
{

    /// <summary>Encapsulates a command to delete a checkpoint.</summary>
    class DeleteCheckpointCommand : IRunnable
    {
        private DataStoreWriter writer;
        private int checkpointIDToDelete;

        /// <summary>
        /// Name of the job.
        /// </summary>
        public string Name { get { return "Delete Checkpoint"; } }

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress { get { return 0; } }

        /// <summary>Constructor</summary>
        /// <param name="dataStoreWriter">The datastore writer that called this constructor.</param>
        /// <param name="checkpointID">The new checkpoint name to create.</param>
        public DeleteCheckpointCommand(DataStoreWriter dataStoreWriter,
                                       int checkpointID)
        {
            writer = dataStoreWriter;
            checkpointIDToDelete = checkpointID;
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
            // Delete data from all tables.
            foreach (var tableName in writer.Connection.GetTableNames())
            {
                List<string> columnNames = writer.Connection.GetTableColumns(tableName);
                if (columnNames.Contains("CheckpointID"))
                    writer.Connection.ExecuteNonQuery("DELETE FROM [" + tableName +
                                                        "] WHERE [CheckpointID] = " + checkpointIDToDelete);
            }
            writer.Connection.ExecuteNonQuery("DELETE FROM [_Checkpoints]" +
                                                " WHERE [ID] = " + checkpointIDToDelete);
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
