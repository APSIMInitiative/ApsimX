namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using System.Collections.Generic;
    using System.Threading;
    using System.Linq;

    /// <summary>Encapsulates a command to delete rows from a table for a given checkpoint / simulation.</summary>
    class DeleteRowsCommand : IRunnable
    {
        private IDatabaseConnection database;
        private string table;
        private IEnumerable<int> simIds;
        private int checkId;

        /// <summary>Constructor</summary>
        /// <param name="databaseConnection">The database to cleanup.</param>
        /// <param name="tableName">The table to cleanup.</param>
        /// <param name="checkpointId">The checkpoint ID to use to match rows to remove.</param>
        /// <param name="simulationIds">The simulation IDs to use to match rows to remove.</param>
        public DeleteRowsCommand(IDatabaseConnection databaseConnection, string tableName, int checkpointId, IList<int> simulationIds)
        {
            database = databaseConnection;
            table = tableName;
            checkId = checkpointId;
            if (simulationIds != null && simulationIds.Any())
                simIds = simulationIds;
        }

        /// <summary>Called to run the command. Can throw on error.</summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            if (database.TableExists(table))
            {
                string sql;

                if (checkId == 0 && simIds == null)
                    sql = string.Format("DROP TABLE {0}", table);
                else if (checkId != 0 && simIds == null)
                    sql = string.Format("DELETE FROM {0} WHERE CheckpointID={1}", table, checkId);
                else
                    sql = string.Format("DELETE FROM {0} WHERE CheckpointID={1} AND SimulationID IN ({2})",
                                        table, checkId, StringUtilities.BuildString(simIds, ","));
                database.ExecuteNonQuery(sql);
            }
        }
    }
}
