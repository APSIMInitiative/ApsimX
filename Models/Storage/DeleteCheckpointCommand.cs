namespace Models.Storage
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Reflection;
    using System.Threading;

    /// <summary>Encapsulates a command to delete a checkpoint.</summary>
    class DeleteCheckpointCommand : IRunnable
    {
        private DataStoreWriter writer;
        private int checkpointIDToDelete;

        /// <summary>Constructor</summary>
        /// <param name="dataStoreWriter">The datastore writer that called this constructor.</param>
        /// <param name="checkpointID">The new checkpoint name to create.</param>
        public DeleteCheckpointCommand(DataStoreWriter dataStoreWriter, 
                                       int checkpointID)
        {
            writer = dataStoreWriter;
            checkpointIDToDelete = checkpointID;
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
    }
}
