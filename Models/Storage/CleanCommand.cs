using System.Collections.Generic;
using System.Linq;
using System.Threading;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;

namespace Models.Storage
{
    internal class CleanCommand : IRunnable
    {
        private static readonly string[] otherTablesToClean = new string[2]
        {
            "_Messages",
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

            foreach (var tableName in tableNames.Where(t => !t.StartsWith("_")))
                CleanTable(tableName, simulationIDsCSV, simulationNamesCSV, currentID);
            foreach (string tableName in otherTablesToClean)
                if (tableNames.Contains(tableName))
                    CleanTable(tableName, simulationIDsCSV, simulationNamesCSV, currentID);
        }

        /// <summary>
        /// Cleanup the job after running it.
        /// </summary>
        public void Cleanup()
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
            if (fieldNames.Contains("SimulationID") && fieldNames.Contains("CheckpointID"))
            {
                string sql = $"DELETE FROM [{tableName}] " +
                             $"WHERE SimulationID in ({simulationIDs}) ";
                if (currentID != -1)
                    sql += $"AND CheckpointID = {currentID}";
                writer.Connection.ExecuteNonQuery(sql);
            }
            else if (fieldNames.Contains("SimulationName") && fieldNames.Contains("CheckpointID"))
            {
                string sql = $"DELETE FROM [{tableName}] " +
                             $"WHERE SimulationName in ({simulationNames}) ";
                if (currentID != -1)
                    sql += $"AND CheckpointID = {currentID}";
                writer.Connection.ExecuteNonQuery(sql);
            }
        }
    }
}