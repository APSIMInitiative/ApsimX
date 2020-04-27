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
            while (directory != null)
            {
                string[] directories = Directory.GetDirectories(directory, "Bin", SearchOption.AllDirectories);
                foreach (string dir in directories)
                {
                    string[] files = Directory.GetFiles(dir, "sqlite3.dll");
                    if (files.Length == 1)
                    {
                        return files[0];
                    }
                }

                directory = Path.GetDirectoryName(directory); // parent directory
            }

            throw new Exception("Cannot find sqlite3 dll directory");
        }

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            string sqliteSourceFileName = FindSqlite3DLL();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(sqliteSourceFileName));
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

            Assert.AreEqual(Utilities.TableToString(database, "_Simulations"),
               "ID,Name,FolderName\r\n" +
               " 1,Sim1,   Folder1\r\n" +
               " 2,Sim2,   Folder2\r\n");

            Assert.AreEqual(Utilities.TableToString(database, "_Checkpoints"),
               "ID,   Name,Version,Date,OnGraphs\r\n" +
               " 1,Current,       ,    ,        \r\n");

            Assert.AreEqual(Utilities.TableToString(database, "_Units"),
               "TableName,ColumnHeading,Units\r\n" +
               "   Report,         Col2,    g\r\n" +
               "   Report,         Col3,kg/ha\r\n");

            Assert.AreEqual(Utilities.TableToString(database, "Report"),
                           "CheckpointID,SimulationID, Col1,Col2,Col3\r\n" +
                           "           1,           1,1.000,  11,    \r\n" +
                           "           1,           1,2.000,  12,    \r\n" +
                           "           1,           2,3.000,    ,  13\r\n" +
                           "           1,           2,4.000,    ,  14\r\n");
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
            writer.WriteTable(data2);
            writer.Stop();

            Assert.AreEqual(Utilities.TableToStringUsingSQL(database, "SELECT * FROM [Report] ORDER BY [Col]"),
                           "CheckpointID,SimulationID,Col\r\n" +
                           "           1,           1,  3\r\n");
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

            Assert.AreEqual(Utilities.TableToString(database, "Report"),
                                        "CheckpointID,Col1,Col2\r\n" +
                                        "           1,  10,  20\r\n" +
                                        "           1,  11,  21\r\n");

            // Make sure units were extracted from the column names and written.
            Assert.AreEqual(Utilities.TableToString(database, "_Units"),
               "TableName,ColumnHeading,Units\r\n" +
               "   Report,         Col1,    g\r\n");
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

            Assert.AreEqual(Utilities.TableToString(database, "Report"),
                           "CheckpointID,SimulationID, Col1\r\n" +
                           "           1,           1,1.000\r\n" +
                           "           1,           1,2.000\r\n" +
                           "           1,           1,3.000\r\n" +
                           "           1,           1,4.000\r\n");
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
            writer.WriteTable(data);
            writer.Stop();

            // Make sure the old data was removed and we only have new data.
            Assert.AreEqual(Utilities.TableToString(database, "Report1"),
                                        "CheckpointID,Col1,Col2\r\n" +
                                        "           1, 100, 200\r\n" +
                                        "           1, 110, 210\r\n");
        }

        /// <summary>Write a table of data twice ensuring the old data is removed.</summary>
        [Test]
        public void CleanupOldColumns()
        {
            DataTable data = new DataTable("Report1");
            data.Columns.Add("Col1", typeof(int));
            data.Columns.Add("Col2", typeof(int));
            DataRow row = data.NewRow();
            row["Col1"] = 10;
            row["Col2"] = 20;
            data.Rows.Add(row);

            DataStoreWriter writer = new DataStoreWriter(database);
            writer.WriteTable(data);
            writer.Stop();

            // Change the structure of the data.
            data = new DataTable("Report1");
            data.Columns.Add("Col1", typeof(int));
            data.Columns.Add("Col3", typeof(int));
            row = data.NewRow();
            row["Col1"] = 100;
            row["Col3"] = 200;
            data.Rows.Add(row);
            
            // Write the table a second time.
            writer = new DataStoreWriter(database);
            writer.WriteTable(data);
            writer.Stop();

            // Make sure the old data was removed and we only have new data.
            Assert.AreEqual(Utilities.TableToString(database, "Report1"),
                                        "CheckpointID,Col1,Col3\r\n" +
                                        "           1, 100, 200\r\n");
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
            writer = new DataStoreWriter(database);
            writer.Empty();
            writer.Stop();

            Assert.AreEqual(database.GetTableNames().Count, 0);
        }

        /// <summary>Add a checkpoint</summary>
        [Test]
        public void AddCheckpoint()
        {
            DataStoreReaderTests.CreateTable(database);

            var writer = new DataStoreWriter(database);

            // Add a checkpoint
            File.WriteAllText("Dummy.txt", "abcde");
            writer.AddCheckpoint("Saved2", new string[] { "Dummy.txt" });

            writer.Stop();

            Assert.AreEqual(Utilities.TableToString(database, "Report"),
                           "CheckpointID,SimulationID,      Col1,  Col2\r\n" +
                           "           1,           1,2017-01-01, 1.000\r\n" +
                           "           1,           1,2017-01-02, 2.000\r\n" +
                           "           1,           2,2017-01-01,21.000\r\n" +
                           "           1,           2,2017-01-02,22.000\r\n" +
                           "           2,           1,2017-01-01,11.000\r\n" +
                           "           2,           1,2017-01-02,12.000\r\n" +
                           "           2,           2,2017-01-01,31.000\r\n" +
                           "           2,           2,2017-01-02,32.000\r\n" +
                           "           3,           1,2017-01-01, 1.000\r\n" +
                           "           3,           1,2017-01-02, 2.000\r\n" +
                           "           3,           2,2017-01-01,21.000\r\n" +
                           "           3,           2,2017-01-02,22.000\r\n");

            Assert.AreEqual(Utilities.TableToString(database, "_Checkpoints", new string[] { "ID", "Name" }),
                                        "ID,   Name\r\n" +
                                        " 1,Current\r\n" +
                                        " 2, Saved1\r\n" +
                                        " 3, Saved2\r\n");

            Assert.AreEqual(Utilities.TableToString(database, "_CheckpointFiles", new string[] { "CheckpointID", "FileName", "Contents" }),
                                        "CheckpointID, FileName,     Contents\r\n" +
                                        "           3,Dummy.txt,System.Byte[]\r\n");
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

            Assert.AreEqual(Utilities.TableToString(database, "Report"),
                           "CheckpointID,SimulationID,      Col1,  Col2\r\n" +
                           "           1,           1,2017-01-01, 1.000\r\n" +
                           "           1,           1,2017-01-02, 2.000\r\n" +
                           "           1,           2,2017-01-01,21.000\r\n" +
                           "           1,           2,2017-01-02,22.000\r\n");
            Assert.AreEqual(Utilities.TableToString(database, "_Checkpoints"),
                "ID,   Name,Version,Date,OnGraphs\r\n" +
                " 1,Current,       ,    ,        \r\n");
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

            Assert.AreEqual(Utilities.TableToString(database, "Report"),
                           "CheckpointID,SimulationID,      Col1,  Col2\r\n" +
                           "           1,           1,2017-01-01,11.000\r\n" +
                           "           1,           1,2017-01-02,12.000\r\n" +
                           "           1,           2,2017-01-01,31.000\r\n" +
                           "           1,           2,2017-01-02,32.000\r\n" +
                           "           2,           1,2017-01-01,11.000\r\n" +
                           "           2,           1,2017-01-02,12.000\r\n" +
                           "           2,           2,2017-01-01,31.000\r\n" +
                           "           2,           2,2017-01-02,32.000\r\n");

            Assert.AreEqual(Utilities.TableToString(database, "_Checkpoints", new string[] { "ID", "Name" }),
                            "ID,   Name\r\n" +
                            " 1,Current\r\n" +
                            " 2, Saved1\r\n");
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
            writer.WriteTable(data1);

            // Add a checkpoint - overwrite existing one.
            writer.AddCheckpoint("Saved1");
            writer.Stop();

            Assert.AreEqual(Utilities.TableToString(database, "Report"),
                            "CheckpointID,SimulationID,      Col1, Col2\r\n" +
                            "           1,           1,2017-01-01,1.000\r\n" +
                            "           1,           1,2017-01-02,2.000\r\n" +
                            "           1,           2,2017-01-01,3.000\r\n" +
                            "           1,           2,2017-01-02,4.000\r\n" +
                            "           2,           1,2017-01-01,1.000\r\n" +
                            "           2,           1,2017-01-02,2.000\r\n" +
                            "           2,           2,2017-01-01,3.000\r\n" +
                            "           2,           2,2017-01-02,4.000\r\n");
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

            Assert.AreEqual(Utilities.TableToString(database, "Report"),
                "CheckpointID,SimulationID,Col1\r\n" +
                "           1,           1,  10\r\n");

            database.CloseDatabase();
            File.Delete(path);
        }
    }
}