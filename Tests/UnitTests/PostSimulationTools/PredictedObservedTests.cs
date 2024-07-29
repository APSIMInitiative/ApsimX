namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.PostSimulationTools;
    using Models.Soils;
    using Models.Soils.Nutrients;
    using Models.Storage;
    using Models.Surface;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using UnitTests.Soils;

    public class PredictedObservedTests
    {
        private IDatabaseConnection database;

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

        [Test]
        public void PredictedObservedUsingSimulationID()
        {
            CreateTable(database);

            var columnNames = new List<string>() { "CheckpointID", "SimulationID", "Col1", "Col2" };

            var predictedRows = new List<object[]>
            {
                new object[] { 1, 1, new DateTime(2017, 1, 1), 1.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 2), 2.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 3), 3.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 4), 4.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 1), 21.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 2), 22.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 3), 23.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 4), 24.0 }
            };
            database.InsertRows("Report", columnNames, predictedRows);

            var observedRows = new List<object[]>
            {
                new object[] { 1, 1, new DateTime(2017, 1, 1), 100.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 2), 200.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 3), 300.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 4), 400.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 1), 210.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 2), 220.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 3), 230.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 4), 240.0 }
            };
            database.InsertRows("Observed", columnNames, observedRows);

            var dataStore = new DataStore(database);
            dataStore.Writer.TablesModified.Add("Observed");

            PredictedObserved PO = new PredictedObserved();
            PO.PredictedTableName = "Report";
            PO.ObservedTableName = "Observed";
            PO.FieldNameUsedForMatch = "SimulationName";
            PO.FieldName2UsedForMatch = "Col1";
            Utilities.InjectLink(PO, "dataStore", dataStore);
            PO.Run();
            dataStore.Writer.Stop();
            dataStore.Reader.Refresh();
            var data = dataStore.Reader.GetData("PredictedObserved");

            Assert.That(
                Utilities.CreateTable(new string[] { "CheckpointName", "CheckpointID", "SimulationName", "SimulationID",                     "Col1", "Observed.Col2", "Predicted.Col2", "Pred-Obs.Col2" },
                   new List<object[]> { new object[] {      "Current",              1,           "Sim1",              1, new DateTime(2017, 01, 01),            100.0,             1.0,          -99.0},    
                                        new object[] {      "Current",              1,           "Sim1",              1, new DateTime(2017, 01, 02),            200.0,             2.0,         -198.0},    
                                        new object[] {      "Current",              1,           "Sim1",              1, new DateTime(2017, 01, 03),            300.0,             3.0,         -297.0},    
                                        new object[] {      "Current",              1,           "Sim1",              1, new DateTime(2017, 01, 04),            400.0,             4.0,         -396.0},    
                                        new object[] {      "Current",              1,           "Sim2",              2, new DateTime(2017, 01, 01),            210.0,            21.0,         -189.0},    
                                        new object[] {      "Current",              1,           "Sim2",              2, new DateTime(2017, 01, 02),            220.0,            22.0,         -198.0},    
                                        new object[] {      "Current",              1,           "Sim2",              2, new DateTime(2017, 01, 03),            230.0,            23.0,         -207.0},    
                                        new object[] {      "Current",              1,           "Sim2",              2, new DateTime(2017, 01, 04),            240.0,            24.0,         -216.0},    
                   })
               .IsSame(data), Is.True);
        }

        [Test]
        public void PredictedObservedNoSimulationID()
        {
            CreateTable(database);

            var columnNames = new List<string>() { "CheckpointID", "SimulationID", "Col1", "Col2" };

            var predictedRows = new List<object[]>
            {
                new object[] { 1, 1, new DateTime(2017, 1, 1), 1.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 2), 2.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 3), 3.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 4), 4.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 5), 5.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 6), 6.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 7), 7.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 8), 8.0 }
            };
            database.InsertRows("Report", columnNames, predictedRows);

            var observedRows = new List<object[]>
            {
                new object[] { 1, 1, new DateTime(2017, 1, 1), 0.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 2), 1.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 3), 2.0 },
                new object[] { 1, 1, new DateTime(2017, 1, 4), 3.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 5), 4.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 6), 5.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 7), 6.0 },
                new object[] { 1, 2, new DateTime(2017, 1, 8), 7.0 }
            };
            database.InsertRows("Observed", columnNames, observedRows);

            var dataStore = new DataStore(database);
            dataStore.Writer.TablesModified.Add("Observed");

            PredictedObserved PO = new PredictedObserved();
            PO.PredictedTableName = "Report";
            PO.ObservedTableName = "Observed";
            PO.FieldNameUsedForMatch = "Col1";
            Utilities.InjectLink(PO, "dataStore", dataStore);
            PO.Run();
            dataStore.Writer.Stop();
            dataStore.Reader.Refresh();
            var data = dataStore.Reader.GetData("PredictedObserved");

            Assert.That(
                Utilities.CreateTable(new string[] { "CheckpointName", "CheckpointID", "SimulationName", "SimulationID",                     "Col1",  "Observed.Col2", "Predicted.Col2", "Pred-Obs.Col2" },
                   new List<object[]> { new object[] {      "Current",              1,           "Sim1",              1, new DateTime(2017, 01, 01),              0.0,              1.0,            1.0},
                                        new object[] {      "Current",              1,           "Sim1",              1, new DateTime(2017, 01, 02),              1.0,              2.0,            1.0},
                                        new object[] {      "Current",              1,           "Sim1",              1, new DateTime(2017, 01, 03),              2.0,              3.0,            1.0},
                                        new object[] {      "Current",              1,           "Sim1",              1, new DateTime(2017, 01, 04),              3.0,              4.0,            1.0},
                                        new object[] {      "Current",              1,           "Sim2",              2, new DateTime(2017, 01, 05),              4.0,              5.0,            1.0},
                                        new object[] {      "Current",              1,           "Sim2",              2, new DateTime(2017, 01, 06),              5.0,              6.0,            1.0},
                                        new object[] {      "Current",              1,           "Sim2",              2, new DateTime(2017, 01, 07),              6.0,              7.0,            1.0},
                                        new object[] {      "Current",              1,           "Sim2",              2, new DateTime(2017, 01, 08),              7.0,              8.0,            1.0},
                   })
               .IsSame(data), Is.True);
        }

        /// <summary>Create a table that we can test</summary>
        private static void CreateTable(IDatabaseConnection database)
        {
            // Create a _Checkpoints table.
            List<string> columnNames = new List<string>() { "ID", "Name", "Version", "Date", "OnGraphs" };
            List<string> columnTypes = new List<string>() { "integer", "char(50)", "char(50)", "date", "integer" };
            database.CreateTable("_Checkpoints", columnNames, columnTypes);
            List<object[]> rows = new List<object[]>
            {
                new object[] { 1, "Current", string.Empty, string.Empty }
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


            // Create a Observed table.
            columnNames = new List<string>() { "CheckpointID", "SimulationID", "Col1", "Col2" };
            columnTypes = new List<string>() { "integer", "integer", "date", "real" };
            database.CreateTable("Observed", columnNames, columnTypes);
        }

    }
}
