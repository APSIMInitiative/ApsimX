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

                bool tableWasDropped = false;
                if (simIds == null)
                {
                    if (checkId == 0)
                    {
                        sql = string.Format("DROP TABLE {0}", table);
                        tableWasDropped = true;
                    }
                    else
                        sql = string.Format("DELETE FROM {0} WHERE CheckpointID={1}", table, checkId);
                }
                else
                {
                    List<string> columns = database.GetTableColumns(table);
                    string ids = StringUtilities.BuildString(simIds, ",");
                    if (columns.Contains("SimulationID"))
                        sql = string.Format("DELETE FROM {0} WHERE CheckpointID={1} AND SimulationID IN ({2})", table, checkId, ids);
                    else if (columns.Contains("SimulationName"))
                    {
                        sql = string.Format("DELETE T FROM {0} T INNER JOIN _Simulations S ON SimulationName=SimulationName WHERE SimulationID IN ({1})", table, ids);
                        if (checkId != 0)
                            sql += string.Format(" AND CheckpointID={0}", checkId);
                    }
                    else
                    {
                        sql = string.Format("DROP TABLE {0}", table);
                        tableWasDropped = true;
                    }
                }
                database.ExecuteNonQuery(sql);

                // If there are no rows left in table then drop the table.
                if (!tableWasDropped)
                {
                    var data = database.ExecuteQuery(string.Format("SELECT * FROM {0} LIMIT 1", table));
                    if (data.Rows.Count == 0)
                    {
                        database.ExecuteNonQuery(string.Format("DROP TABLE {0}", table));
                    }
                }
            }
        }
    }
}
