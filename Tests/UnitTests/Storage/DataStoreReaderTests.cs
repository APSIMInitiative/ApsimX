namespace UnitTests.Storage
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Reflection;

    [TestFixture]
    public class DataStoreReaderTests
    {
        private IDatabaseConnection database;
        //private string sqliteFileName;

        /// <summary>Find and return the file name of SQLite runtime .dll</summary>
        public static string FindSqlite3DLL()
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] files = Directory.GetFiles(directory, "sqlite3.dll");
            if (files.Length == 1)
                return files[0];

            throw new Exception("Cannot find sqlite3 dll directory");
        }

        /// <summary>Initialisation code for all unit tests in this class</summary>
        [SetUp]
        public void Initialise()
        {
            if (ProcessUtilities.CurrentOS.IsWindows)
            {
                string sqliteSourceFileName = FindSqlite3DLL();
                Directory.SetCurrentDirectory(Path.GetDirectoryName(sqliteSourceFileName));
            }

            database = new SQLite();
            database.OpenDatabase(":memory:", readOnly: false);
        }

        /// <summary>Read all data from a table</summary>
        [Test]
        public void ReadAllDataFromATable()
        {
            CreateTable(database);

            DataStoreReader reader = new DataStoreReader(database);
            var data = reader.GetData("Report");

            Assert.That(
                Utilities.CreateTable(new string[] {                    "CheckpointName", "CheckpointID", "SimulationName", "SimulationID",                     "Col1", "Col2" },
                                      new List<object[]> { new object[] {      "Current",              1,           "Sim1",              1, new DateTime(2017, 01, 01),   1.0 },
                                                           new object[] {      "Current",              1,           "Sim1",              1, new DateTime(2017, 01, 02),   2.0 },
                                                           new object[] {      "Current",              1,           "Sim2",              2, new DateTime(2017, 01, 01),   21.0 },
                                                           new object[] {      "Current",              1,           "Sim2",              2, new DateTime(2017, 01, 02),   22.0 }})
               .IsSame(data), Is.True);
        }

        /// <summary>Ensure that GetData when passed a table name and simulation name returns the correct data</summary>
        [Test]
        public void ReadAllDataForASimulation()
        {
            CreateTable(database);

            DataStoreReader reader = new DataStoreReader(database);
            var data = reader.GetData(tableName: "Report",
                                      simulationNames: new string[] { "Sim1" });

            Assert.That(
                Utilities.CreateTable(new string[] { "CheckpointName", "CheckpointID", "SimulationName", "SimulationID", "Col1", "Col2" },
                                      new List<object[]> { new object[] {      "Current",              1,        "Sim1",      1, new DateTime(2017, 01, 01),   1.0 },
                                                           new object[] {      "Current",              1,        "Sim1",      1, new DateTime(2017, 01, 02),   2.0 } })
               .IsSame(data), Is.True);
        }

        /// <summary>Read a single column for a simulation.</summary>
        [Test]
        public void ReadAColumnForASimulation()
        {
            CreateTable(database);

            DataStoreReader reader = new DataStoreReader(database);
            var data = reader.GetData(tableName: "Report",
                                      simulationNames: new string[] { "Sim1" },
                                      fieldNames: new string[] { "Col2" });

            Assert.That(
                Utilities.CreateTable(new string[]                    { "CheckpointName", "CheckpointID", "SimulationName", "SimulationID", "Col2" },
                                      new List<object[]> { new object[] {      "Current",              1,           "Sim1",              1,     1 },
                                                           new object[] {      "Current",              1,           "Sim1",              1,     2 } })
               .IsSame(data), Is.True);
        }

        /// <summary>Read a single column for a simulation with a filter.</summary>
        [Test]
        public void ReadDataForASimulationWithAFilter()
        {
            CreateTable(database);

            DataStoreReader reader = new DataStoreReader(database);
            var data = reader.GetData(tableName: "Report",
                                      simulationNames: new string[] { "Sim1" },
                                      fieldNames: new string[] { "Col2" },
                                      filter: "Col2=2");

            Assert.That(
                Utilities.CreateTable(
                    new string[] {                    "CheckpointName", "CheckpointID", "SimulationName", "SimulationID", "Col2" },
                    new List<object[]> { new object[] {      "Current",              1,           "Sim1",              1,     2 }})
               .IsSame(data), Is.True);
        }

        /// <summary>Read data using SQL.</summary>
        [Test]
        public void ReadDataUsingSql()
        {
            CreateTable(database);

            DataStoreReader reader = new DataStoreReader(database);
            var data = reader.GetDataUsingSql("SELECT [Col1] FROM [Report]");


            Assert.That(
                Utilities.CreateTable(
                    new string[]                      {                    "Col1" },
                    new List<object[]> { new object[] { new DateTime(2017, 01, 01) },
                                         new object[] { new DateTime(2017, 01, 02) },
                                         new object[] { new DateTime(2017, 01, 01) },
                                         new object[] { new DateTime(2017, 01, 02) },
                                         new object[] { new DateTime(2017, 01, 01) },
                                         new object[] { new DateTime(2017, 01, 02) },
                                         new object[] { new DateTime(2017, 01, 01) },
                                         new object[] { new DateTime(2017, 01, 02) }})
               .IsSame(data), Is.True);
        }

        /// <summary>Get units for a column.</summary>
        [Test]
        public void GetUnitsForAColumn()
        {
            CreateTable(database);

            // Create a _Units table.
            var columnNames = new List<string>() { "TableName", "ColumnHeading", "Units" };
            var columnTypes = new List<string>() { "char(50)", "char(50)", "char(50)" };
            database.CreateTable("_Units", columnNames, columnTypes);
            var rows = new List<object[]>
            {
                new object[] { "Report", "Col1", "g" },
                new object[] { "Report", "Col2", "g/m2" },
                new object[] { "Report2", "Col1", "t/ha" },
                new object[] { "Report2", "Col2", "t" }
            };
            database.InsertRows("_Units", columnNames, rows);

            DataStoreReader reader = new DataStoreReader(database);
            string units = reader.Units(tableName: "Report",
                                           columnHeading: "Col2");
            Assert.That(units, Is.EqualTo("g/m2"));
        }

        /// <summary>Get a list of simulation names.</summary>
        [Test]
        public void GetSimulationNames()
        {
            CreateTable(database);
            DataStoreReader reader = new DataStoreReader(database);
            Assert.That(reader.SimulationNames, Is.EqualTo(new string[] { "Sim1", "Sim2" }));
        }

        /// <summary>Get a list of column names for a table.</summary>
        [Test]
        public void GetColumnNames()
        {
            CreateTable(database);
            DataStoreReader reader = new DataStoreReader(database);
            Assert.That(reader.ColumnNames("Report").ToArray(), Is.EqualTo(new string[] { "CheckpointID", "SimulationID", "Col1", "Col2" }));
        }

        /// <summary>Get a list of checkpoint names.</summary>
        [Test]
        public void GetCheckpointNames()
        {
            CreateTable(database);
            DataStoreReader reader = new DataStoreReader(database);
            Assert.That(reader.CheckpointNames[0], Is.EqualTo("Current"));
            Assert.That(reader.CheckpointNames[1], Is.EqualTo("Saved1"));
        }

        /// <summary>
        /// Ensures that checkpoint names are updated after running a simulation.
        /// Reproduces github bug #3734
        /// https://github.com/APSIMInitiative/ApsimX/issues/3734
        /// </summary>
        [Test]
        public void RefreshCheckpointNames()
        {
            Simulations sims = Utilities.GetRunnableSim();
            sims.FileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".apsimx");
            sims.Write(sims.FileName);

            Simulation sim = sims.FindInScope<Simulation>();
            IDataStore storage = sims.FindInScope<IDataStore>();

            // Record checkpoint names before and after running the simulation,
            // and ensure that they are not the same.
            string[] checkpointNamesBeforeRun = storage.Reader.CheckpointNames.ToArray();

            // Run the simulation
            var runner = new Runner(sims);
            runner.Run();
            string[] checkpointNamesAfterRun = storage.Reader.CheckpointNames.ToArray();

            Assert.That(checkpointNamesAfterRun, Is.Not.EqualTo(checkpointNamesBeforeRun), "Storage reader failed to update checkpoint names after simulation was run.");
        }

        /// <summary>Create a table that we can test</summary>
        public static void CreateTable(IDatabaseConnection database)
        {
            // Create a _Checkpoints table.
            List<string> columnNames = new List<string>() { "ID", "Name", "Version", "Date", "OnGraphs" };
            List<string> columnTypes = new List<string>() { "integer", "char(50)", "char(50)", "date", "integer" };
            database.CreateTable("_Checkpoints", columnNames, columnTypes);
            List<object[]> rows = new List<object[]>
            {
                new object[] { 1, "Current", string.Empty, string.Empty },
                new object[] { 2, "Saved1", string.Empty, string.Empty }
            };
            database.InsertRows("_Checkpoints", columnNames, rows);

            // Create a _Simulations table.
            columnNames = new List<string>() { "ID", "Name", "FolderName" };
            columnTypes = new List<string>() { "integer", "char(50)", "char(50)" };
            database.CreateTable("_Simulations", columnNames, columnTypes);
            rows = new List<object[]>
            {
                new object[] { 1, "Sim1", string.Empty },
                new object[] { 2, "Sim2", string.Empty }
            };
            database.InsertRows("_Simulations", columnNames, rows);

            // Create a Report table.
            columnNames = new List<string>() { "CheckpointID", "SimulationID", "Col1", "Col2" };
            columnTypes = new List<string>() { "integer", "integer", "date", "real" };
            database.CreateTable("Report", columnNames, columnTypes);
            rows = new List<object[]>
            {
                new object[] { 1, 1, new DateTime(2017, 1, 1), 1.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 2), 2.0 },
                new object[] { 2, 1, new DateTime(2017, 1, 1), 11.0 },
                new object[] { 2, 1, new DateTime(2017, 1, 2), 12.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 1), 21.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 2), 22.0 },
                new object[] { 2, 2, new DateTime(2017, 1, 1), 31.0 },
                new object[] { 2, 2, new DateTime(2017, 1, 2), 32.0 }
            };
            database.InsertRows("Report", columnNames, rows);

        }
    }
}