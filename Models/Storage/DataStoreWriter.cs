using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using Models.Core.Run;

namespace Models.Storage
{

    /// <summary>
    /// This class encapsulates all writing to a DataStore
    /// </summary>
    public class DataStoreWriter : IJobManager, IStorageWriter
    {
        /// <summary>Lock object.</summary>
        private object lockObject = new object();

        /// <summary>A list of all write commands.</summary>
        /// <remarks>NEVER modify this without first acquiring a lock on <see cref="lockObject" />.</remarks>
        private Queue<IRunnable> commands = new();

        /// <summary>A sleep job to stop the job runner from exiting.</summary>
        private IRunnable sleepJob = new JobRunnerSleepJob(10);

        /// <summary>The runner used to run commands on a worker thread.</summary>
        private JobRunner commandRunner;

        /// <summary>Are we idle i.e. not writing to database?</summary>
        private bool idle = true;

        /// <summary>Has something been written to the db?</summary>
        private bool somethingHasBeenWriten = false;

        /// <summary>The IDS for all simulations</summary>
        private Dictionary<string, SimulationDetails> simulationIDs = new Dictionary<string, SimulationDetails>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The IDs for all checkpoints</summary>
        private Dictionary<string, Checkpoint> checkpointIDs = new Dictionary<string, Checkpoint>(StringComparer.OrdinalIgnoreCase);

        /// <summary>A list of simulation names that have been cleaned up for each table.</summary>
        private Dictionary<string, List<string>> simulationNamesThatHaveBeenCleanedUp = new Dictionary<string, List<string>>();

        /// <summary>A list of units for each table.</summary>
        private Dictionary<string, List<ColumnUnits>> units = new Dictionary<string, List<ColumnUnits>>();

        /// <summary>A list of names of tables that don't have checkpointid or simulatoinid columns.</summary>
        private static string[] tablesNotNeedingIndexColumns = new string[] { "_Simulations", "_Checkpoints", "_Units" };

        /// <summary>Are we stopping writing to the db?</summary>
        private bool stopping;

        /// <summary>The details of tables that have been written to.</summary>
        private Dictionary<string, DatabaseTableDetails> tables = new Dictionary<string, DatabaseTableDetails>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Default constructor.</summary>
        public DataStoreWriter()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dbConnection">Data database connection to write to.</param>
        public DataStoreWriter(IDatabaseConnection dbConnection)
        {
            SetConnection(dbConnection);
        }

        /// <summary>Set the database connection.Constructor</summary>
        /// <param name="dbConnection">The database connection to write to.</param>
        public void SetConnection(IDatabaseConnection dbConnection)
        {
            Connection = dbConnection;
            ReadExistingDatabase(dbConnection);
            if (dbConnection is SQLite && !(dbConnection as SQLite).IsInMemory)
            {
                // For disk-based databases, these pragmas greatly improve performance
                dbConnection.ExecuteQuery("PRAGMA journal_mode=WAL");
                dbConnection.ExecuteQuery("PRAGMA synchronous=NORMAL");
            }
        }

        /// <summary>The database connection to write to.</summary>
        public IDatabaseConnection Connection { get; private set; }

        /// <summary>Has something been written to the database since the last call to this property?</summary>
        public bool SomethingBeenWritten
        {
            get
            {
                bool returnValue = somethingHasBeenWriten;
                somethingHasBeenWriten = false;
                return returnValue;
            }
        }

        /// <summary>
        /// A list of table names which have been modified in the most recent simulations run.
        /// </summary>
        public List<string> TablesModified { get; private set; } = new List<string>();

        /// <summary>
        /// 
        /// </summary>
        public int NumJobs { get { return 0; } }

        /// <summary>Call JobHasCompleted when job is complete?</summary>
        public bool NotifyWhenJobComplete => false;

        /// <summary>
        /// Add rows to a table in the db file. Note that the data isn't written immediately.
        /// </summary>
        /// <param name="data">Name of simulation the values correspond to.</param>
        public void WriteTable(ReportData data)
        {
            // NOTE: This can be called from many threads. Don't actually
            // write to the database on these threads. We have a single worker
            // thread to do that.

            Start();
            var table = data.ToTable();
            AddIndexColumns(table, "Current", data.SimulationName, data.FolderName);

            // Add units
            AddUnits(data.TableName, data.ColumnNames, data.ColumnUnits);

            lock (lockObject)
            {
                if (!tables.TryGetValue(table.TableName, out var tableDetails))
                {
                    tableDetails = new DatabaseTableDetails(Connection, table.TableName);
                    tables.Add(table.TableName, tableDetails);
                }

                commands.Enqueue(new WriteTableCommand(Connection, table, tableDetails, deleteOldData: false));
                if (!TablesModified.Contains(table.TableName))
                    TablesModified.Add(table.TableName);
            }
        }

        /// <summary>
        /// Write a table of data. Uses the TableName property of the specified DataTable.
        /// </summary>
        /// <param name="table">The data to write.</param>
        /// <param name="deleteAllData">Delete all existing data from this table in the DB before writing the table to the DB?</param>
        /// <remarks>
        /// Before simulations are run, all tables whose names don't start with an underscore
        /// (ie any table generated by Report,ExcelInput, etc) will be cleaned; that is, all
        /// data associated with the simulation(s) about to be run will be removed from these
        /// tables. Additionally, the messages and initial conditions tables will also be cleaned.
        /// 
        /// That being said, any model which can be run *without* running simulations (e.g.
        /// any post-simulation tool) should always set the second argument to true, to ensure
        /// that data is deleted. This is necessary because if the user only wants to run
        /// post simulation-tools, we cannot clean the datastore on a per-simulation basis,
        /// so no automatic cleaning occurs at all.
        /// 
        /// Seting the second argument to true when no data exists is not an error.
        /// </remarks>
        public void WriteTable(DataTable table, bool deleteAllData = true)
        {
            if (table == null)
                return;
            // NOTE: This can be called from many threads. Don't actually
            // write to the database on these threads. We have a single worker
            // thread to do that.

            Start();

            AddIndexColumns(table, "Current", null, null);

            lock (lockObject)
            {
                if (!tables.TryGetValue(table.TableName, out var tableDetails))
                {
                    tableDetails = new DatabaseTableDetails(Connection, table.TableName);
                    tables.Add(table.TableName, tableDetails);
                }
                commands.Enqueue(new WriteTableCommand(Connection, table, tableDetails, deleteAllData));
                if (!TablesModified.Contains(table.TableName))
                    TablesModified.Add(table.TableName);
            }
        }

        /// <summary>
        /// Deletes a table from the database.
        /// </summary>
        /// <param name="tableName">Name of the table to be deleted.</param>
        public void DeleteTable(string tableName)
        {
            string sql;
            // If there is only 1 checkpointID in the database, we can just drop the table.
            // If this table doesn't have a CheckpointID column, we also just drop the table.
            // Otherwise, we delete all data corresponding to the "Current" checkpoint ID.
            bool tableHasCheckpointID = Connection.GetColumns(tableName).Any(c => c.Item1 == "CheckpointID");
            if (checkpointIDs.Count <= 1 || !tableHasCheckpointID)
                Connection.DropTable(tableName);
            else
            {
                int currentCheckpointID = checkpointIDs["Current"].ID;
                sql = $"DELETE FROM \"{tableName}\" WHERE \"CheckpointID\" = {currentCheckpointID}";

                Connection.ExecuteNonQuery(sql);
                lock (lockObject)
                {
                    if (!TablesModified.Contains(tableName))
                        TablesModified.Add(tableName);
                }
            }
        }

        /// <summary>Wait for all records to be written.</summary>
        public void WaitForIdle()
        {
            if (commandRunner != null)
            {
                // Make sure all existing writing has completed.
                SpinWait.SpinUntil(() => commands.Count < 1 && idle);
            }
        }

        /// <summary>Immediately stop all writing to database.</summary>
        public void Cancel()
        {
            if (commandRunner != null)
            {
                stopping = true;
                commandRunner.Stop();
                idle = true;
                commandRunner = null;
                commands.Clear();
                lock (lockObject)
                    simulationIDs.Clear();
                checkpointIDs.Clear();
                simulationNamesThatHaveBeenCleanedUp.Clear();
                units.Clear();
            }
        }

        /// <summary>Finish all writing to database.</summary>
        public void Stop()
        {
            if (commandRunner != null)
            {
                try
                {
                    WaitForIdle();
                    WriteSimulationIDs();
                    WriteCheckpointIDs();
                    WriteAllUnits();
                    WaitForIdle();
                    // Make sure all existing writing has completed.
                    SpinWait.SpinUntil(() => commandRunner.SimsRunning.IsEmpty);
                    Connection.EndWriting();
                }
                catch
                {
                    // Swallow exceptions
                }
                finally
                {
                    WaitForIdle();
                    stopping = true;
                    commandRunner = null;
                    commands.Clear();
                    lock (lockObject)
                        simulationIDs.Clear();
                    checkpointIDs.Clear();
                    simulationNamesThatHaveBeenCleanedUp.Clear();
                    units.Clear();
                }
            }
        }

        /// <summary>Called by the job runner when all jobs completed</summary>
        public void AllCompleted() { }

        /// <summary>Return an enumeration of jobs that need running.</summary>
        public IEnumerable<IRunnable> GetJobs()
        {
            // NOTE: This is called from the job runner worker thread.

            while (!stopping)
            {
                IRunnable command = null;
                lock (lockObject)
                {
                    if (commands.Count > 0)
                    {
                        command = commands.Dequeue();
                        // The WaitForIdle() function will wait until there are no jobs
                        // and idle is set to true. Therefore, we should update the value
                        // of idle *before* removing this command from the commands list.
                        // Otherwise, we could end up in a situation where idle is (briefly)
                        // set to true (because a command was recently added), and the commands
                        // list is empty, which would cause WaitForIdle() to return, even
                        // though the job runner actually hasn't finished running this command.
                        idle = command == null;
                    }
                    else
                        idle = true;
                }

                // If nothing was found to run then return a sleep job 
                // so that the job runner doesn't exit.
                if (command == null)
                {
                    yield return sleepJob;
                }
                else
                {
                    somethingHasBeenWriten = true;
                    yield return command;
                }
            }
        }

        /// <summary>Delete all data in datastore, except for checkpointed data.</summary>
        public void Empty()
        {
            try
            {
                Start();
            }
            catch
            {
                // The call to Start may fail if the database is corrupt, which may well be why we want to empty it.
                // For that reason, catch any exceptions and proceed.
            }
            lock (lockObject)
                commands.Enqueue(new EmptyCommand(Connection));
            Stop();
        }

        /// <summary>Save the current data to a checkpoint.</summary>
        /// <param name="name">Name of checkpoint.</param>
        /// <param name="filesToStore">Files to store the contents of.</param>
        public void AddCheckpoint(string name, IEnumerable<string> filesToStore = null)
        {
            Start();
            lock (lockObject)
                commands.Enqueue(new AddCheckpointCommand(this, name, filesToStore));
            Stop();
        }

        /// <summary>Delete a checkpoint.</summary>
        /// <param name="name">Name of checkpoint to delete.</param>
        public void DeleteCheckpoint(string name)
        {
            Start();
            lock (lockObject)
            {
                commands.Enqueue(new DeleteCheckpointCommand(this, GetCheckpointID(name)));
                checkpointIDs.Remove(name);
            }
            Stop();
        }

        /// <summary>Revert a checkpoint.</summary>
        /// <param name="name">Name of checkpoint to revert to.</param>
        public void RevertCheckpoint(string name)
        {
            Start();
            lock (lockObject)
                commands.Enqueue(new RevertCheckpointCommand(this, GetCheckpointID(name)));
            Stop();
        }

        /// <summary>Set a checkpoint show on graphs flag.</summary>
        /// <param name="name">Name of checkpoint.</param>
        /// <param name="showGraphs">Show graphs?</param>
        public void SetCheckpointShowGraphs(string name, bool showGraphs)
        {
            Start();
            if (checkpointIDs.ContainsKey(name))
                checkpointIDs[name].ShowOnGraphs = showGraphs;
            Stop();
        }

        /// <summary>
        /// Add a list of column units for the specified table.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="columnNames">A collection of column names.</param>
        /// <param name="columnUnits">A corresponding collection of column units.</param>
        public void AddUnits(string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits)
        {
            // Can be called by many threads simultaneously.
            lock (lockObject)
            {
                for (int i = 0; i < columnNames.Count(); i++)
                {
                    var columnName = columnNames.ElementAt(i);
                    var columnUnit = columnUnits.ElementAt(i);
                    if (columnUnit != null && columnUnit != string.Empty)
                    {
                        if (!units.TryGetValue(tableName, out var tableUnits))
                        {
                            tableUnits = new List<ColumnUnits>();
                            units.Add(tableName, tableUnits);
                        }
                        if (tableUnits.Find(unit => unit.Name == columnName) == null)
                            tableUnits.Add(new ColumnUnits() { Name = columnName, Units = columnUnits.ElementAt(i) });
                    }
                }
            }
        }

        /// <summary>
        /// Get a simulation ID for the specified simulation name. Will
        /// create an ID if the simulationName is unknown.
        /// </summary>
        /// <param name="simulationName">The name of the simulation to look for.</param>
        /// <param name="folderName">The name of the folder the simulation belongs in.</param>
        /// <returns>Always returns a number.</returns>
        public int GetSimulationID(string simulationName, string folderName)
        {
            if (simulationName == null)
                return 0;

            lock (lockObject)
            {
                if (!simulationIDs.TryGetValue(simulationName, out SimulationDetails details))
                {
                    // Not found so create a new ID, add it to our collection of ids
                    int id;
                    if (simulationIDs.Count > 0)
                        id = simulationIDs.Values.Select(v => v.ID).Max() + 1;
                    else
                        id = 1;
                    simulationIDs.Add(simulationName, new SimulationDetails() { ID = id, FolderName = folderName });
                    return id;
                }
                else if (folderName != null)
                    details.FolderName = folderName;
                return details.ID;
            }
        }

        /// <summary>
        /// Get a checkpoint ID for the specified name. Will
        /// create an ID if the Name is unknown.
        /// </summary>
        /// <param name="checkpointName">The name of the checkpoint to look for.</param>
        /// <returns>Always returns a number.</returns>
        public int GetCheckpointID(string checkpointName)
        {
            if (checkpointName == null)
                return 0;

            lock (lockObject)
            {
                if (!checkpointIDs.TryGetValue(checkpointName, out Checkpoint checkpoint))
                {
                    checkpoint = new Checkpoint();
                    // Not found so create a new ID, add it to our collection of ids
                    if (checkpointIDs.Count > 0)
                        checkpoint.ID = checkpointIDs.Select(c => c.Value.ID).Max() + 1;
                    else
                        checkpoint.ID = 1;
                    checkpointIDs.Add(checkpointName, checkpoint);
                }
                return checkpoint.ID;
            }
        }

        /// <summary>
        /// Read the database connection for simulation and checkpoint ids.
        /// </summary>
        /// <param name="dbConnection">The database connection to read from.</param>
        private void ReadExistingDatabase(IDatabaseConnection dbConnection)
        {
            if (dbConnection == null)
                return;

            lock (lockObject)
                simulationIDs.Clear();
            if (dbConnection.TableExists("_Simulations"))
            {
                var data = dbConnection.ExecuteQuery("SELECT * FROM [_Simulations]");
                foreach (DataRow row in data.Rows)
                {
                    int id = Convert.ToInt32(row["ID"], CultureInfo.InvariantCulture);
                    string folderName = null;
                    if (data.Columns.Contains("FolderName"))
                        folderName = row["FolderName"].ToString();
                    lock (lockObject)
                        simulationIDs.Add(row["Name"].ToString(),
                                      new SimulationDetails() { ID = id, FolderName = folderName });
                }
            }

            checkpointIDs.Clear();
            if (dbConnection.TableExists("_Checkpoints"))
            {
                var data = dbConnection.ExecuteQuery("SELECT * FROM [_Checkpoints]");
                foreach (DataRow row in data.Rows)
                    checkpointIDs.Add(row["Name"].ToString(), new Checkpoint()
                    {
                        ID = Convert.ToInt32(row["ID"], CultureInfo.InvariantCulture),
                        ShowOnGraphs = data.Columns["OnGraphs"] != null &&
                                       !Convert.IsDBNull(row["OnGraphs"]) &&
                                       Convert.ToInt32(row["OnGraphs"], CultureInfo.InvariantCulture) == 1
                    });
            }
        }

        /// <summary>
        /// Create a db clean command.
        /// </summary>
        /// <param name="names">A list of simulation names that are about to run.</param>
        public IRunnable Clean(IEnumerable<string> names)
        {
            var ids = new List<int>();
            if (simulationIDs.Count == 0)
                ReadExistingDatabase(Connection);
            foreach (var name in names)
                if (simulationIDs.TryGetValue(name, out SimulationDetails details))
                    ids.Add(details.ID);
            return new CleanCommand(this, names, ids);
        }

        /// <summary>
        /// Returns the number of entries in the command queue
        /// </summary>
        /// <returns></returns>
        public int CommandCount()
        {
            return commands.Count;
        }
        /// <summary>
        /// Initiate a clean of the database.
        /// </summary>
        /// <param name="names">Simulation names to be cleaned.</param>
        /// <param name="wait">Wait for the clean operation to finish?</param>
        public void Clean(IEnumerable<string> names, bool wait)
        {
            if (wait)
                Start();
            lock (lockObject)
                commands.Enqueue(Clean(names));
            if (wait)
                Stop();
        }

        /// <summary>Create a command runner one hasn't already been created.</summary>
        private void Start()
        {
            if (commandRunner == null)
            {
                lock (lockObject)
                {
                    if (commandRunner == null)
                    {
                        stopping = false;
                        commandRunner = new JobRunner(numProcessors: (Connection is Firebird && (Connection as Firebird).fbDBServerType == FirebirdSql.Data.FirebirdClient.FbServerType.Default) ? -1 : 1);
                        commandRunner.Add(this);
                        commandRunner.Run();
                        ReadExistingDatabase(Connection);
                        tables.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Add in checkpoint and simulation ID columns.
        /// </summary>
        /// <param name="table">The table to add the columns to.</param>
        /// <param name="checkpointName">The name of the checkpoint.</param>
        /// <param name="simulationName">The simulation name.</param>
        /// <param name="folderName">The name of the folder the simulation sits in.</param>
        private void AddIndexColumns(DataTable table, string checkpointName, string simulationName, string folderName)
        {
            if (!tablesNotNeedingIndexColumns.Contains(table.TableName))
            {
                if (!table.Columns.Contains("CheckpointID"))
                {
                    var checkpointColumn = table.Columns.Add("CheckpointID", typeof(int));
                    int checkpointNameColumnIndex = table.Columns.IndexOf("CheckpointName");
                    if (checkpointNameColumnIndex != -1)
                    {
                        // A checkpoint name column exists.
                        foreach (DataRow row in table.Rows)
                        {
                            checkpointName = row[checkpointNameColumnIndex].ToString();
                            row[checkpointColumn] = GetCheckpointID(checkpointName); ;
                        }
                        table.Columns.RemoveAt(checkpointNameColumnIndex);
                    }
                    else
                    {
                        var id = GetCheckpointID(checkpointName);
                        foreach (DataRow row in table.Rows)
                            row[checkpointColumn] = id;
                    }
                    checkpointColumn.SetOrdinal(0);
                }

                if (!table.Columns.Contains("SimulationID"))
                {
                    DataColumn simulationColumn = null;
                    int simulationNameColumnIndex = table.Columns.IndexOf("SimulationName");
                    if (simulationNameColumnIndex != -1)
                    {
                        simulationColumn = table.Columns.Add("SimulationID", typeof(int));
                        foreach (DataRow row in table.Rows)
                        {
                            simulationName = row[simulationNameColumnIndex].ToString();
                            row[simulationColumn] = GetSimulationID(simulationName, folderName); ;
                        }
                        table.Columns.RemoveAt(simulationNameColumnIndex);
                    }
                    else if (simulationName != null)
                    {
                        simulationColumn = table.Columns.Add("SimulationID", typeof(int));
                        var id = GetSimulationID(simulationName, folderName);
                        foreach (DataRow row in table.Rows)
                            row[simulationColumn] = id;
                    }
                    if (simulationColumn != null)
                        simulationColumn.SetOrdinal(1);
                }
            }
        }

        /// <summary>
        /// Write all units to our list of units for later writing to the database.
        /// </summary>
        private void WriteAllUnits()
        {
            if (units.Any())
            {
                var unitTable = new DataTable("_Units");
                unitTable.Columns.Add("TableName", typeof(string));
                unitTable.Columns.Add("ColumnHeading", typeof(string));
                unitTable.Columns.Add("Units", typeof(string));

                foreach (var tableUnit in units)
                {
                    foreach (var unit in tableUnit.Value)
                    {
                        var unitRow = unitTable.NewRow();
                        unitRow[0] = tableUnit.Key;
                        unitRow[1] = unit.Name;
                        unitRow[2] = unit.Units;
                        unitTable.Rows.Add(unitRow);
                    }
                }
                WriteTable(unitTable, deleteAllData: true);
            }
        }

        /// <summary>Write the simulations table.</summary>
        private void WriteSimulationIDs()
        {
            // If there are no tables in the database then they must have been emptied.
            // Don't write a simulations table in this case.
            if (simulationIDs.Count > 0 && Connection.GetTableNames().Count > 0)
            {
                var simulationsTable = new DataTable("_Simulations");
                simulationsTable.Columns.Add("ID", typeof(int));
                simulationsTable.Columns.Add("Name", typeof(string));
                simulationsTable.Columns.Add("FolderName", typeof(string));

                foreach (var simulation in simulationIDs)
                {
                    var row = simulationsTable.NewRow();
                    row[0] = simulation.Value.ID;
                    row[1] = simulation.Key;
                    row[2] = simulation.Value.FolderName;
                    simulationsTable.Rows.Add(row);
                }
                WriteTable(simulationsTable, deleteAllData: true);
            }
        }

        /// <summary>Write the checkpoints table.</summary>
        private void WriteCheckpointIDs()
        {
            // If there are no tables in the database then they must have been emptied.
            // Don't write a checkpoints table in this case.
            if (checkpointIDs.Count > 0 && Connection.GetTableNames().Count > 0)
            {
                var checkpointsTable = new DataTable("_Checkpoints");
                checkpointsTable.Columns.Add("ID", typeof(int));
                checkpointsTable.Columns.Add("Name", typeof(string));
                checkpointsTable.Columns.Add("Version", typeof(string));
                checkpointsTable.Columns.Add("OnGraphs", typeof(int));

                foreach (var checkpoint in checkpointIDs)
                {
                    var row = checkpointsTable.NewRow();
                    row[0] = checkpoint.Value.ID;
                    row[1] = checkpoint.Key;
                    if (checkpoint.Value.ShowOnGraphs)
                        row[3] = 1;
                    checkpointsTable.Rows.Add(row);
                }
                WriteTable(checkpointsTable, deleteAllData: true);
            }
        }

        void IJobManager.JobHasCompleted(JobCompleteArguments args)
        {
        }

        /// <summary>
        /// A class for encapsulating column units.
        /// </summary>
        private class ColumnUnits
        {
            /// <summary>Name of column.</summary>
            public string Name { get; set; }

            /// <summary>Units of column.</summary>
            public string Units { get; set; }
        }

        private class SimulationDetails
        {
            public int ID { get; set; }
            public string FolderName { get; set; }
        }
    }
}
