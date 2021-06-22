using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Models.Storage
{
    internal class CleanCommand : IRunnable
    {
        private DataStoreWriter writer;
        private List<string> names;
        private List<int> ids;

        public CleanCommand(DataStoreWriter dataStoreWriter, List<string> names, List<int> ids)
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

        /// <summary>Called to run the command. Can throw on error.</summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            var simulationIDsCSV = StringUtilities.Build(ids, ",");
            var simulationNamesCSV = StringUtilities.Build(names, ",");
            int currentID = writer.Connection.ExecuteQueryReturnInt("SELECT ID FROM [_Checkpoints] WHERE [Name]='Current'");

            foreach (var tableName in writer.Connection.GetTableNames().Where(t => !t.StartsWith("_")))
            {
                var fieldNames = writer.Connection.GetColumnNames(tableName);
                if (fieldNames.Contains("SimulationID") && fieldNames.Contains("CheckpointID"))
                    writer.Connection.ExecuteNonQuery($"DELETE FROM [{tableName}] " +
                                                      $"WHERE SimulationID in ({simulationIDsCSV}) " +
                                                      $"AND CheckpointID = {currentID}");
                else if (fieldNames.Contains("SimulationName") && fieldNames.Contains("CheckpointID"))
                    writer.Connection.ExecuteNonQuery($"DELETE FROM [{tableName}] " +
                                                      $"WHERE SimulationName in ({simulationNamesCSV}) " +
                                                      $"AND CheckpointID = {currentID}");
            }
        }
    }
}