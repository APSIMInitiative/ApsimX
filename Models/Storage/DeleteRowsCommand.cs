namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using System.Threading;

    /// <summary>Encapsulates a command to delete rows from a table for a given checkpoint / simulation.</summary>
    class DeleteRowsCommand : IRunnable
    {
        private IDatabaseConnection database;
        private string table;
        private int simId;
        private int checkId;

        /// <summary>Constructor</summary>
        /// <param name="databaseConnection">The database to cleanup.</param>
        /// <param name="tableName">The table to cleanup.</param>
        /// <param name="checkpointId">The checkpoint ID to use to match rows to remove.</param>
        /// <param name="simulationId">The simulation ID to use to match rows to remove.</param>
        public DeleteRowsCommand(IDatabaseConnection databaseConnection, string tableName, int checkpointId, int simulationId)
        {
            database = databaseConnection;
            table = tableName;
            checkId = checkpointId;
            simId = simulationId;
        }

        /// <summary>Called to run the command. Can throw on error.</summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            string sql;

            if (checkId == 0 && simId == 0)
                sql = string.Format("DROP TABLE {0}", table);
            else if (checkId != 0 && simId == 0)
                sql = string.Format("DELETE FROM {0} WHERE CheckpointID={1}", table, checkId);
            else
                sql = string.Format("DELETE FROM {0} WHERE CheckpointID={1} AND SimulationID={2}",
                                    table, checkId, simId);
            database.ExecuteNonQuery(sql);
        }
    }
}
