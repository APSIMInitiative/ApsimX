using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;

namespace Models.Storage
{

    /// <summary>Encapsulates a command to empty the database as much as possible.</summary>
    class EmptyCommand : IRunnable
    {
        private IDatabaseConnection database;

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress { get { return 0; } }

        public string Name { get { return "Empty the database"; } }

        /// <summary>Constructor</summary>
        /// <param name="databaseConnection">The database to cleanup.</param>
        public EmptyCommand(IDatabaseConnection databaseConnection)
        {
            database = databaseConnection;
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
            if (database == null)
                return;

            bool deleteEverything = false;
            if (database.TableExists("_Checkpoints"))
            {
                var checkpointData = new DataView(database.ExecuteQuery("SELECT * FROM [_Checkpoints]"));
                if (checkpointData.Table.Rows.Count == 1)
                    deleteEverything = true;
                else
                {
                    checkpointData.RowFilter = "Name='Current'";
                    if (checkpointData.Count == 1)
                    {
                        int checkId = Convert.ToInt32(checkpointData[0]["ID"], CultureInfo.InvariantCulture);

                        // Delete current data from all tables.
                        foreach (string tableName in database.GetTableNames())
                        {
                            if (!tableName.StartsWith("_") && database.TableExists(tableName))
                                database.ExecuteNonQuery(string.Format("DELETE FROM [{0}] WHERE [CheckpointID]={1}", tableName, checkId));
                        }
                    }
                }
            }
            else
                deleteEverything = true;

            if (deleteEverything)
            {
                // No checkpoint information, so get rid of everything
                // Delete current data from all tables.
                foreach (string tableName in database.GetTableNames())
                {
                    if (!tableName.StartsWith("_"))
                        database.ExecuteNonQuery(string.Format("DELETE FROM [{0}]", tableName));
                }
            }

            // Delete empty tables.
            List<string> tableNamesToDelete = new List<string>();
            bool allTablesEmpty = true;
            foreach (string tableName in database.GetTableNames())
            {
                if (!tableName.StartsWith("_"))
                {
                    if (database.TableIsEmpty(tableName))
                        tableNamesToDelete.Add(tableName);
                    else
                        allTablesEmpty = false;
                }
            }

            // If all data tables were emptied then delete all tables.
            if (allTablesEmpty)
            {
                tableNamesToDelete = database.GetTableNames();
                // remove any database Views created if no tables remain
                foreach (string viewName in database.GetViewNames())
                {
                    database.ExecuteNonQuery(string.Format("DROP VIEW IF EXISTS [{0}]", viewName));
                }
            }

            foreach (string tableName in tableNamesToDelete)
                database.DropTable(tableName);
            if (database is SQLite)
            {
                (database as SQLite).Vacuum();
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
