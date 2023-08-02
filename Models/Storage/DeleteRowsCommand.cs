using System.Collections.Generic;
using System.Linq;
using System.Threading;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;

namespace Models.Storage
{

    /// <summary>Encapsulates a command to delete rows from a table for a given checkpoint / simulation.</summary>
    class DeleteRowsCommand : IRunnable
    {
        private IDatabaseConnection database;
        private string table;
        private IEnumerable<int> simIds;
        private int checkId;

        public string Name { get { return "Delete rows"; } }

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress { get { return 0; } }

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
            if (database.TableExists(table))
            {
                string sql = string.Empty;
                bool dropEntireTable = false;

                if (simIds == null)
                {
                    if (checkId == 0)
                        dropEntireTable = true;
                    else
                        sql = string.Format("DELETE FROM [{0}] WHERE [CheckpointID]={1}", table, checkId);
                }
                else
                {
                    List<string> columns = database.GetTableColumns(table);
                    string ids = StringUtilities.BuildString(simIds, ",");
                    if (columns.Contains("SimulationID"))
                        sql = string.Format("DELETE FROM [{0}] WHERE [CheckpointID]={1} AND [SimulationID] IN ({2})", table, checkId, ids);
                    else if (columns.Contains("SimulationName"))
                    {
                        sql = string.Format("DELETE T FROM [{0}] T INNER JOIN [_Simulations] S ON T.[SimulationName]=S.[SimulationName] WHERE [SimulationID] IN ({1})", table, ids);
                        if (checkId != 0)
                            sql += string.Format(" AND [CheckpointID]={0}", checkId);
                    }
                    else
                        dropEntireTable = true;
                }

                if (dropEntireTable)
                    database.DropTable(table);
                else
                {
                    database.ExecuteNonQuery(sql);
                    // If there are no rows left in table then drop the table.
                    if (database.TableIsEmpty(table))
                        database.DropTable(table);
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
