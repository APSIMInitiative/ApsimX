using System.Collections.Generic;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using System.Linq;
using System.Threading;
using System;
using Models.Core;

namespace Models.Storage
{
    internal class CleanCommand : IRunnable
    {
        private static readonly string[] otherTablesToClean = new string[3]
        {
            "_Messages",
            "_Factors",
            "_InitialConditions"
        };

        private DataStoreWriter writer;
        private IEnumerable<string> names;
        private IEnumerable<int> ids;

        public CleanCommand(DataStoreWriter dataStoreWriter, IEnumerable<string> names, IEnumerable<int> ids)
        {
            writer = dataStoreWriter;
            this.names = names;
            this.ids = ids;
        }

        /// <summary>
        /// Name of the job.
        /// </summary>
        public string Name { get { return "Clean"; } }

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress { get { return 0; } }

        /// <summary>
        /// For Firebird use - indicates a list of simulations of length 1500 or above,
        /// which is too long to use directly in a Firebird IN predicate.
        /// </summary>
        private bool isLongList = false;

        /// <summary>
        /// Prepare the IRunnable instance to be run.
        /// </summary>
        public void Prepare()
        {
            // Nothing to do.
        }

        /// <summary>Called to run the command. Can throw on error.</summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            var tableNames = writer.Connection.GetTableNames();
            var simulationIDsCSV = StringUtilities.Build(ids, ",");
            var simulationNamesCSV = StringUtilities.Build(names, ",");
            int currentID = -1;
            if (tableNames.Contains("_Checkpoints"))
                currentID = writer.Connection.ExecuteQueryReturnInt("SELECT ID FROM [_Checkpoints] WHERE [Name]='Current'");

            // We need to do things a bit differently in Firebird, as it has a limit of 1500 items 
            // in the WHERE IN() clause, and with things like Wheat Validation we exceed that limit.

            if (writer.Connection is Firebird && !String.IsNullOrEmpty(simulationIDsCSV))
            {
                List<object[]> Ids = simulationIDsCSV.Split(',').Select(c => new object[1] { Convert.ToInt32(c) }).ToList();
                isLongList = Ids.Count > 1499;
                if (isLongList)
                {
                    string sql = "RECREATE GLOBAL TEMPORARY TABLE \"_DropIDs\" (\"simID\" integer) ON COMMIT PRESERVE ROWS";
                    writer.Connection.ExecuteNonQuery(sql);
                    writer.Connection.InsertRows("_DropIDs", new List<string> { "simID" }, Ids);
                }
            }

            foreach (var tableName in tableNames.Where(t => !t.StartsWith("_")))
                CleanTable(tableName, simulationIDsCSV, simulationNamesCSV, currentID);
            foreach (string tableName in otherTablesToClean)
                if (tableNames.Contains(tableName))
                    CleanTable(tableName, simulationIDsCSV, simulationNamesCSV, currentID);

            if (writer.Connection is Firebird && isLongList && writer.Connection.TableExists("_DropIDs"))
            {
                writer.Connection.DropTable("_DropIDs");
            }
        }

        /// <summary>
        /// Cleanup the job after running it.
        /// </summary>
        public void Cleanup(System.Threading.CancellationTokenSource cancelToken)
        {
            // Do nothing.
        }

        /// <summary>
        /// Clean all existing data in the given table for the specified simulation names.
        /// </summary>
        /// <param name="tableName">Name of the table to clean.</param>
        /// <param name="simulationIDs">Comma-separated list of simulation IDs for the simulations to be cleaned.</param>
        /// <param name="simulationNames">Comma-separated list of simulation names for the simulations to be cleaned.</param>
        /// <param name="currentID">ID of the "Current" checkpoint.</param>
        private void CleanTable(string tableName, string simulationIDs, string simulationNames, int currentID)
        {
            var fieldNames = writer.Connection.GetColumnNames(tableName);
            if (writer.Connection is Firebird)
            {
                if (fieldNames.Contains("SimulationID") && fieldNames.Contains("CheckpointID"))
                {
                    // We need to do things a bit differently in Firebird, as it has a limit of 1500 items 
                    // in the WHERE IN() clause, and with things like Wheat Validation we exceed that limit.

                    if (isLongList)
                    {
                        // We could use $"DELETE FROM \"{tableName}\" WHERE \"SimulationID\" IN (SELECT \"simID\" FROM \"_DropIDs\")",
                        // which is a bit easier for a human to parse, but the MERGE version seems to be significantly faster
                        // (although still quite slow)        
                        string sql = $"MERGE INTO \"{tableName}\" USING (SELECT \"rowid\" FROM \"{tableName}\", \"_DropIDs\" WHERE " +
                                         $"\"{tableName}\".\"SimulationID\" = \"_DropIDs\".\"simID\"";
                        if (currentID != -1)
                            sql += $" AND \"CheckpointID\" = {currentID}";
                        sql += $") \"temp\" on \"{tableName}\".\"rowid\" = \"temp\".\"rowid\" WHEN MATCHED THEN DELETE";
                        writer.Connection.ExecuteNonQuery(sql);
                    }
                    else if (!String.IsNullOrEmpty(simulationIDs))
                    {
                        string sql = $"DELETE FROM \"{tableName}\" " +
                                     $"WHERE \"SimulationID\" IN ({simulationIDs})";
                        if (currentID != -1)
                            sql += $" AND \"CheckpointID\" = {currentID}";
                        writer.Connection.ExecuteNonQuery(sql);
                    }
                }
                else if (fieldNames.Contains("SimulationName") && fieldNames.Contains("CheckpointID"))
                    throw new FirebirdException("Use of SimulationName field rather than SimulationID not currently supported by Firebird.");
            }

            else
            {
                writer.Connection.BeginTransaction();
                try
                {
                    if (fieldNames.Contains("SimulationID") && fieldNames.Contains("CheckpointID"))
                    {
                        string sql = $"DELETE FROM [{tableName}] " +
                        $"WHERE \"SimulationID\" in ({simulationIDs}) ";
                        if (currentID != -1)
                            sql += $"AND \"CheckpointID\" = {currentID}";
                        writer.Connection.ExecuteNonQuery(sql);
                    }
                    else if (fieldNames.Contains("SimulationName") && fieldNames.Contains("CheckpointID"))
                    {
                        string sql = $"DELETE FROM [{tableName}] " +
                                     $"WHERE \"SimulationName\" in ({simulationNames}) ";
                        if (currentID != -1)
                            sql += $"AND \"CheckpointID\" = {currentID}";
                        writer.Connection.ExecuteNonQuery(sql);
                    }
                }
                finally
                {
                    writer.Connection.EndTransaction();
                }
            }
        }
    }
}