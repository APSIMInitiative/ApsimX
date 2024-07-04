namespace UnitTests.Storage
{
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Reflection;

    [TestFixture]
    public class DataStoreWriterTests
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

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            if (ProcessUtilities.CurrentOS.IsWindows)
            {
                string sqliteSourceFileName = FindSqlite3DLL();
                Directory.SetCurrentDirectory(Path.GetDirectoryName(sqliteSourceFileName));
            }
        }

        /// <summary>Initialisation code for all unit tests in this class</summary>
        [SetUp]
        public void Initialise()
        {
            database = new SQLite();
            database.OpenDatabase(":memory:", readOnly: false);
        }

        /// <summary>Write data for 2 simulations into one table. Ensure data was written correctly.</summary>
        [Test]
        public void WriteReportDataForTwoSimulations()
        {
            // Setup first report data.
            var data1 = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                FolderName = "Folder1",
                TableName = "Report",
                ColumnNames = new string[] { "Col1", "Col2" },
                ColumnUnits = new string[] {   null,    "g" }
            };
            data1.Rows.Add(new List<object>() { 1.0, 11 });
            data1.Rows.Add(new List<object>() { 2.0, 12 });

            // Setup second report data.
            var data2 = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim2",
                FolderName = "Folder2",
                TableName = "Report",
                ColumnNames = new string[] { "Col1", "Col3" },
                ColumnUnits = new string[] {   null, "kg/ha" }
            };
            data2.Rows.Add(new List<object>() { 3.0, 13 });
            data2.Rows.Add(new List<object>() { 4.0, 14 });

            // Write both data tables to writer.
            DataStoreWriter writer = new DataStoreWriter(database);
            writer.WriteTable(data1);
            writer.WriteTable(data2);
            writer.Stop();


            Assert.That(
                Utilities.CreateTable(new string[] {                       "ID", "Name", "FolderName" },
                                      new List<object[]> { new object[] {     1, "Sim1",    "Folder1" },
                                                           new object[] {     2, "Sim2",    "Folder2" }})
               .IsSame(Utilities.GetTableFromDatabase(database, "_Simulations")), Is.True);

            Assert.That(
                Utilities.CreateTable(new string[] {                       "ID", "Name",         "Version",      "OnGraphs" },
                                      new List<object[]> { new object[] {     1, "Current", Convert.DBNull, Convert.DBNull }})
               .IsSame(Utilities.GetTableFromDatabase(database, "_Checkpoints")), Is.True);

            Assert.That(
                Utilities.CreateTable(new string[] {                    "TableName", "ColumnHeading", "Units" },
                                      new List<object[]> { new object[] {  "Report",          "Col2",     "g" },
                                                           new object[] {  "Report",          "Col3", "kg/ha" }})
               .IsSame(Utilities.GetTableFromDatabase(database, "_Units")), Is.True);

            Assert.That(
                Utilities.CreateTable(new string[] {                 "CheckpointID", "SimulationID", "Col1",         "Col2",          "Col3" },
                                      new List<object[]> { new object[] {         1,              1,      1,             11, Convert.DBNull },
                                                           new object[] {         1,              1,      2,             12, Convert.DBNull },
                                                           new object[] {         1,              2,      3, Convert.DBNull,             13 },
                                                           new object[] {         1,              2,      4, Convert.DBNull,             14 }})
               .IsSame(Utilities.GetTableFromDatabase(database, "Report")), Is.True);
        }

        /// <summary>When writing simulation data to a table, ensure that old rows are removed.</summary>
        [Test]
        public void RemoveOldRowsWhenWritingSimulationData()
        {
            // Write first report data.
            var data1 = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col" },
                ColumnUnits = new string[] {  null }
            };
            data1.Rows.Add(new List<object>() { 1 });
            data1.Rows.Add(new List<object>() { 2 });
            DataStoreWriter writer = new DataStoreWriter(database);
            writer.WriteTable(data1);
            writer.Stop();

            // Now do it again this time writing different data for the same sim.
            var data2 = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col" },
                ColumnUnits = new string[] {  null }
            };
            data2.Rows.Add(new List<object>() { 3 });
            writer = new DataStoreWriter(database);
            var cleanCommand = writer.Clean(new List<string>() { "Sim1" });
            cleanCommand.Run(null);
            writer.WriteTable(data2);
            writer.Stop();


            Assert.That(
                Utilities.CreateTable(new string[]                      { "CheckpointID", "SimulationID", "Col" },
                                      new List<object[]> { new object[] {              1,              1,    3 }})
               .IsSame(database.ExecuteQuery("SELECT * FROM [Report] ORDER BY [Col]")), Is.True);
        }

        /// <summary>Write a table of data with no simulation name.</summary>
        [Test]
        public void WriteATableOfDataWithUnits()
        {
            DataTable data = new DataTable("Report");
            data.Columns.Add("Col1", typeof(int));
            data.Columns.Add("Col2", typeof(int));
            DataRow row = data.NewRow();
            row["Col1"] = 10;
            row["Col2"] = 20;
            data.Rows.Add(row);
            row = data.NewRow();
            row["Col1"] = 11;
            row["Col2"] = 21;
            data.Rows.Add(row);

            DataStoreWriter writer = new DataStoreWriter(database);
            writer.WriteTable(data);
            writer.AddUnits("Report", new string[] { "Col1" }, new string[] { "g" });
            writer.Stop();

            Assert.That(
                Utilities.CreateTable(new string[] {                 "CheckpointID", "Col1",         "Col2" },
                                      new List<object[]> { new object[] {         1,     10,            20 },
                                                           new object[] {         1,     11,            21 }})
               .IsSame(Utilities.GetTableFromDatabase(database, "Report")), Is.True);

            Assert.That(
                Utilities.CreateTable(new string[] {                     "TableName", "ColumnHeading", "Units" },
                                      new List<object[]> { new object[] {   "Report",          "Col1",     "g" }})
               .IsSame(Utilities.GetTableFromDatabase(database, "_Units")), Is.True);
        }


        /// <summary>Write to a table twice. This is what report does.</summary>
        [Test]
        public void WriteToATableTwice()
        {
            // Setup first report data.
            var data1 = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col1" },
                ColumnUnits = new string[] { null, null }
            };
            data1.Rows.Add(new List<object>() { 1.0 });
            data1.Rows.Add(new List<object>() { 2.0 });

            // Setup second report data.
            var data2 = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col1" },
                ColumnUnits = new string[] { null }
            };
            data2.Rows.Add(new List<object>() { 3.0 });
            data2.Rows.Add(new List<object>() { 4.0 });

            // Write both data tables to writer.
            DataStoreWriter writer = new DataStoreWriter(database);
            writer.WriteTable(data1);
            writer.WriteTable(data2);
            writer.Stop();

            Assert.That(
                Utilities.CreateTable(new string[]                      { "CheckpointID", "SimulationID", "Col1"},
                                      new List<object[]> { new object[] {              1,              1,     1 },
                                                           new object[] {              1,              1,     2 },
                                                           new object[] {              1,              1,     3 },
                                                           new object[] {              1,              1,     4 }})
               .IsSame(Utilities.GetTableFromDatabase(database, "Report")), Is.True);
        }

        /// <summary>Write a table of data twice ensuring the old data is removed.</summary>
        [Test]
        public void CleanupOldTableData()
        {
            DataTable data = new DataTable("Report1");
            data.Columns.Add("Col1", typeof(int));
            data.Columns.Add("Col2", typeof(int));
            DataRow row = data.NewRow();
            row["Col1"] = 10;
            row["Col2"] = 20;
            data.Rows.Add(row);
            row = data.NewRow();
            row["Col1"] = 11;
            row["Col2"] = 21;
            data.Rows.Add(row);

            DataStoreWriter writer = new DataStoreWriter(database);
            writer.WriteTable(data);
            writer.Stop();

            // Change the data.
            data.Rows[0]["Col1"] = 100;
            data.Rows[0]["Col2"] = 200;
            data.Rows[1]["Col1"] = 110;
            data.Rows[1]["Col2"] = 210;

            // Write the table a second time.
            writer = new DataStoreWriter(database);
            writer.WriteTable(data, deleteAllData:true);
            writer.Stop();

            // Make sure the old data was removed and we only have new data.
            Assert.That(
                Utilities.CreateTable(new string[] {                      "CheckpointID", "Col1", "Col2" },
                                      new List<object[]> { new object[] {              1,    100,   200 },
                                                           new object[] {              1,    110,   210 }})
               .IsSame(Utilities.GetTableFromDatabase(database, "Report1")), Is.True);
        }

        /// <summary>Empty the datastore.</summary>
        [Test]
        public void EmptyDataStore()
        {
            // Setup first report data.
            var data1 = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col1", "Col2" },
                ColumnUnits = new string[] { null, "g" }
            };
            data1.Rows.Add(new List<object>() { 1.0, 11 });
            data1.Rows.Add(new List<object>() { 2.0, 12 });

            // Setup second report data.
            var data2 = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim2",
                TableName = "Report",
                ColumnNames = new string[] { "Col1", "Col3" },
                ColumnUnits = new string[] { null, "kg/ha" }
            };
            data2.Rows.Add(new List<object>() { 3.0, 13 });
            data2.Rows.Add(new List<object>() { 4.0, 14 });

            // Write two sims of data.
            DataStoreWriter writer = new DataStoreWriter(database);
            writer.WriteTable(data1);
            writer.WriteTable(data2);
            writer.Stop();

            // Now empty the datastore.
            writer.Empty();
            writer.Stop();

            Assert.That(database.GetTableNames().Count, Is.EqualTo(0));
        }

        /// <summary>Add a checkpoint</summary>
        [Test]
        public void AddCheckpoint()
        {
            DataStoreReaderTests.CreateTable(database);

            var writer = new DataStoreWriter(database);

            // Add a checkpoint
            string file = Path.Combine(Path.GetTempPath(), $"dummy-{Guid.NewGuid()}.txt");
            File.WriteAllText(file, "abcde");
            try
            {
                writer.AddCheckpoint("Saved2", new string[] { file });

                writer.Stop();

                Assert.That(
                    Utilities.CreateTable(new string[]                      { "CheckpointID", "SimulationID",                     "Col1", "Col2" },
                                        new List<object[]> { new object[] {              1,              1, new DateTime(2017, 01, 01),     1 },
                                                            new object[] {              1,              1, new DateTime(2017, 01, 02),     2 },
                                                            new object[] {              1,              2, new DateTime(2017, 01, 01),    21 },
                                                            new object[] {              1,              2, new DateTime(2017, 01, 02),    22 },
                                                            new object[] {              2,              1, new DateTime(2017, 01, 01),    11},
                                                            new object[] {              2,              1, new DateTime(2017, 01, 02),    12 },
                                                            new object[] {              2,              2, new DateTime(2017, 01, 01),    31 },
                                                            new object[] {              2,              2, new DateTime(2017, 01, 02),    32 },
                                                            new object[] {              3,              1, new DateTime(2017, 01, 01),     1 },
                                                            new object[] {              3,              1, new DateTime(2017, 01, 02),     2 },
                                                            new object[] {              3,              2, new DateTime(2017, 01, 01),    21 },
                                                            new object[] {              3,              2, new DateTime(2017, 01, 02),    22 }})
                .IsSame(Utilities.GetTableFromDatabase(database, "Report")), Is.True);


                Assert.That(
                    Utilities.CreateTable(new string[]                      { "ID",    "Name" },
                                        new List<object[]> { new object[] {    1, "Current" },
                                                            new object[] {    2,  "Saved1" },
                                                            new object[] {    3,  "Saved2" }})
                .IsSame(Utilities.GetTableFromDatabase(database, "_Checkpoints", new string[] { "ID", "Name" })), Is.True);

                Assert.That(
                    Utilities.CreateTable(new string[]                      { "CheckpointID",  "FileName",      "Contents" },
                                        new List<object[]> { new object[] {              3, file, "System.Byte[]"}})
                .IsSame(Utilities.GetTableFromDatabase(database, "_CheckpointFiles")), Is.True);
            }
            finally
            {
                File.Delete(file);
            }
        }

        /// <summary>Delete a checkpoint</summary>
        [Test]
        public void DeleteCheckpoint()
        {
            DataStoreReaderTests.CreateTable(database);

            var writer = new DataStoreWriter(database);

            // Delete a checkpoint
            writer.DeleteCheckpoint("Saved1");

            writer.Stop();

            Assert.That(
                Utilities.CreateTable(new string[]                      { "CheckpointID", "SimulationID", "Col1", "Col2" },
                                      new List<object[]> { new object[] {              1,              1, new DateTime(2017, 01, 01),     1 },
                                                           new object[] {              1,              1, new DateTime(2017, 01, 02),     2 },
                                                           new object[] {              1,              2, new DateTime(2017, 01, 01),    21 },
                                                           new object[] {              1,              2, new DateTime(2017, 01, 02),    22 }})
               .IsSame(Utilities.GetTableFromDatabase(database, "Report")), Is.True);


            Assert.That(
                Utilities.CreateTable(new string[]                      { "ID", "Name",      "Version",   "Date",  "OnGraphs"},
                                      new List<object[]> { new object[] { 1, "Current", Convert.DBNull, Convert.DBNull, Convert.DBNull } })
               .IsSame(Utilities.GetTableFromDatabase(database, "_Checkpoints")), Is.True);
        }

        /// <summary>Revert a checkpoint</summary>
        [Test]
        public void RevertCheckpoint()
        {
            DataStoreReaderTests.CreateTable(database);

            // Write some new current data.
            var data1 = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col1", "Col2" },
                ColumnUnits = new string[] { null, "g" }
            };
            data1.Rows.Add(new List<object>() { new DateTime(2017, 1, 3), 100.0 });
            data1.Rows.Add(new List<object>() { new DateTime(2017, 1, 4), 200.0 });
            var writer = new DataStoreWriter(database);
            writer.WriteTable(data1);

            // Now revert back to checkpoint1
            writer.RevertCheckpoint("Saved1");
            writer.Stop();

            Assert.That(
                Utilities.CreateTable(new string[] { "CheckpointID", "SimulationID", "Col1", "Col2" },
                                      new List<object[]> { new object[] {              1,              1, new DateTime(2017, 01, 01),    11 },
                                                           new object[] {              1,              1, new DateTime(2017, 01, 02),    12 },
                                                           new object[] {              1,              2, new DateTime(2017, 01, 01),    31 },
                                                           new object[] {              1,              2, new DateTime(2017, 01, 02),    32 },
                                                           new object[] {              2,              1, new DateTime(2017, 01, 01),    11 },
                                                           new object[] {              2,              1, new DateTime(2017, 01, 02),    12 },
                                                           new object[] {              2,              2, new DateTime(2017, 01, 01),    31 },
                                                           new object[] {              2,              2, new DateTime(2017, 01, 02),    32 }})
               .IsSame(Utilities.GetTableFromDatabase(database, "Report")), Is.True);


            Assert.That(
                Utilities.CreateTable(new string[]                      { "ID",    "Name" },
                                      new List<object[]> { new object[] {    1, "Current" },
                                                           new object[] {    2,  "Saved1"  }})
               .IsSame(Utilities.GetTableFromDatabase(database, "_Checkpoints", new string[] { "ID", "Name" })), Is.True);
        }

        /// <summary>Overwrite an existing checkpoint</summary>
        [Test]
        public void OverwriteExistingCheckpoint()
        {
            DataStoreReaderTests.CreateTable(database);

            // Write new rows for sim2. Should get rid of old sim2 data and replace 
            // with these 2 new rows.
            // Write some new current data.
            var data1 = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim2",
                TableName = "Report",
                ColumnNames = new string[] { "Col1", "Col2" },
                ColumnUnits = new string[] { null, "g" }
            };
            data1.Rows.Add(new List<object>() { new DateTime(2017, 1, 1), 3.0 });
            data1.Rows.Add(new List<object>() { new DateTime(2017, 1, 2), 4.0 });
            var writer = new DataStoreWriter(database);
            var cleanCommand = writer.Clean(new List<string>() { "Sim2" });
            cleanCommand.Run(null);
            writer.WriteTable(data1);

            // Add a checkpoint - overwrite existing one.
            writer.AddCheckpoint("Saved1");
            writer.Stop();

            Assert.That(
                Utilities.CreateTable(new string[]                      { "CheckpointID", "SimulationID",                     "Col1", "Col2" },
                                      new List<object[]> { new object[] {              1,              1, new DateTime(2017, 01, 01),     1 },
                                                           new object[] {              1,              1, new DateTime(2017, 01, 02),     2 },
                                                           new object[] {              1,              2, new DateTime(2017, 01, 01),     3 },
                                                           new object[] {              1,              2, new DateTime(2017, 01, 02),     4 },
                                                           new object[] {              2,              1, new DateTime(2017, 01, 01),     1 },
                                                           new object[] {              2,              1, new DateTime(2017, 01, 02),     2 },
                                                           new object[] {              2,              2, new DateTime(2017, 01, 01),     3 },
                                                           new object[] {              2,              2, new DateTime(2017, 01, 02),     4 }})
               .IsSame(Utilities.GetTableFromDatabase(database, "Report")), Is.True);
        }

        /// <summary>Tests if we can open a database with foreign characters in the path.</summary>
        [Test]
        public void ForeignCharacterTest()
        {
            string path = Path.Combine(Path.GetTempPath(), "文档.db");
            File.Delete(path);

            database = new SQLite();
            database.OpenDatabase(path, readOnly: false);

            var writer = new DataStoreWriter(database);

            DataTable data = new DataTable("Report");
            data.Columns.Add("SimulationName", typeof(string));
            data.Columns.Add("Col1", typeof(int));
            DataRow row = data.NewRow();
            row["SimulationName"] = "Sim1";
            row["Col1"] = 10;
            data.Rows.Add(row);

            writer.WriteTable(data);
            writer.Stop();


            Assert.That(
                Utilities.CreateTable(new string[]                      { "CheckpointID", "SimulationID", "Col1" },
                                      new List<object[]> { new object[] {              1,              1,    10 }})
               .IsSame(Utilities.GetTableFromDatabase(database, "Report")), Is.True);

            database.CloseDatabase();
            File.Delete(path);
        }
    }
}