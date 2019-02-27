namespace UnitTests
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
        //private string sqliteFileName;
        private string savedDirectoryName;

        private class Pair
        {
            public int a { get; set; }
            public int b { get; set; }
        }

        private class Record
        {
            public DateTime c { get; set; }
            public List<double> d { get; set; }
            public Pair[] e { get; set; }
        }

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

        /// <summary>Initialisation code for all unit tests in this class</summary>
        [SetUp]
        public void Initialise()
        {
            database = new SQLite();
            database.OpenDatabase(":memory:", readOnly: false);

            string sqliteSourceFileName = FindSqlite3DLL();
            savedDirectoryName = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(sqliteSourceFileName));
            //sqliteFileName = Path.Combine(Directory.GetCurrentDirectory(), "sqlite3.dll");
            //if (!File.Exists(sqliteFileName))
            //{
            //    File.Copy(sqliteSourceFileName, sqliteFileName, overwrite:true);
            //}
        }

        [TearDown]
        public void TearDown()
        {
            Directory.SetCurrentDirectory(savedDirectoryName);
        }

        /// <summary>Write data for 2 simulations into one table. Ensure data was written correctly.</summary>
        [Test]
        public void WriteReportDataForTwoSimulations()
        {
            DataStoreWriter writer = new DataStoreWriter(database);

            string[] columnNames1 = new string[] { "Col1", "Col2" };
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "g" }, new object[] { 1.0, 11.0 });
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "g" }, new object[] { 2.0, 12.0 });
            writer.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "g" }, new object[] { 3.0, 13.0 });
            writer.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "g" }, new object[] { 4.0, 14.0 });
            writer.Stop();

            Assert.AreEqual(Utilities.TableToString(database, "_Simulations"),
               "ID,Name,FolderName\r\n" +
               " 1,Sim1,          \r\n" +
               " 2,Sim2,          \r\n");

            Assert.AreEqual(Utilities.TableToString(database, "_Checkpoints"),
               "ID,   Name,Version,Date\r\n" +
               " 1,Current,       ,    \r\n");

            Assert.AreEqual(Utilities.TableToString(database, "_Units"),
               "TableName,ColumnHeading,Units\r\n" +
               "  Report1,         Col2,    g\r\n");

            Assert.AreEqual(Utilities.TableToString(database, "Report1"),
                           "CheckpointID,SimulationID, Col1,  Col2\r\n" +
                           "           1,           1,1.000,11.000\r\n" +
                           "           1,           1,2.000,12.000\r\n" +
                           "           1,           2,3.000,13.000\r\n" +
                           "           1,           2,4.000,14.000\r\n");
        }

        /// <summary>Write data for 2 simulations, each with different columns, into one table. Ensure data was written correctly.</summary>
        [Test]
        public void WriteReportDataForTwoSimulationsDifferentCols()
        {
            DataStoreWriter writer = new DataStoreWriter(database);

            string[] columnNames1 = new string[] { "Col1", "Col2" };
            string[] columnNames2 = new string[] { "Col3", "Col4" };
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 1.0, 11.0 });
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 2.0, 12.0 });
            writer.WriteRow("Sim2", "Report1", columnNames2, new string[] { null, "(g)" }, new object[] { 3.0, 13.0 });
            writer.WriteRow("Sim2", "Report1", columnNames2, new string[] { null, "(g)" }, new object[] { 4.0, 14.0 });
            writer.Stop();

            Assert.AreEqual(Utilities.TableToString(database, "Report1"),
                           "CheckpointID,SimulationID, Col1,  Col2, Col3,  Col4\r\n" +
                           "           1,           1,1.000,11.000,     ,      \r\n" +
                           "           1,           1,2.000,12.000,     ,      \r\n" +
                           "           1,           2,     ,      ,3.000,13.000\r\n" +
                           "           1,           2,     ,      ,4.000,14.000\r\n");
        }

        /// <summary>Write array data that changes size into a table. Ensure data was written correctly</summary>
        [Test]
        public void WriteArrayDataThatChangesSize()
        {
            DataStoreWriter writer = new DataStoreWriter(database);

            string[] columnNames1 = new string[] { "Col" };
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { "(g)" }, new object[] { new double[] { 1.0 } });
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { "(g)" }, new object[] { new double[] { 2.0, 2.1 } });
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { "(g)" }, new object[] { new double[] { 3.0, 3.1, 3.2 } });
            writer.Stop();

            Assert.AreEqual(Utilities.TableToString(database, "Report1"),
                           "CheckpointID,SimulationID,Col(1),Col(2),Col(3)\r\n" +
                           "           1,           1, 1.000,      ,      \r\n" +
                           "           1,           1, 2.000, 2.100,      \r\n" +
                           "           1,           1, 3.000, 3.100, 3.200\r\n");
        }

        /// <summary>Write array of structure data into a table. Ensure data was written correctly</summary>
        [Test]
        public void WriteStructureData()
        {
            Record record1 = new Record()
            {
                c = new DateTime(2017, 1, 1),
                d = new List<double> { 100, 101 },
                e = new Pair[] { new Pair() { a = 10, b = 11 },
                                     new Pair() { a = 12, b = 13 } }
            };
            Record record2 = new Record()
            {
                c = new DateTime(2017, 1, 2),
                d = new List<double> { 102, 103 },
                e = new Pair[] { new Pair() { a = 16, b = 17 },
                                     new Pair() { a = 18, b = 19 },
                                     new Pair() { a = 20, b = 21 } }
            };

            DataStoreWriter writer = new DataStoreWriter(database);

            string[] columnNames1 = new string[] { "Col" };
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { null }, new object[] { record1 });
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { null }, new object[] { record2 });
            writer.Stop();

            Assert.AreEqual(Utilities.TableToString(database, "Report1"),
                            "CheckpointID,SimulationID,     Col.c,Col.d(1),Col.d(2),Col.e(1).a,Col.e(1).b,Col.e(2).a,Col.e(2).b,Col.e(3).a,Col.e(3).b\r\n" +
                            "           1,           1,2017-01-01, 100.000, 101.000,        10,        11,        12,        13,          ,          \r\n" +
                            "           1,           1,2017-01-02, 102.000, 103.000,        16,        17,        18,        19,        20,        21\r\n");
        }

        /// <summary>When writing simulation data to a table, ensure that old rows are removed.</summary>
        [Test]
        public void RemoveOldRowsWhenWritingSimulationData()
        {
            string[] columnNames1 = new string[] { "Col" };
            DataStoreWriter writer = new DataStoreWriter(database);

            writer.WriteRow("Sim1", "Report1", columnNames1, null, new object[] { 1 });
            writer.WriteRow("Sim2", "Report1", columnNames1, null, new object[] { 2 });
            writer.WriteRow("Sim3", "Report1", columnNames1, null, new object[] { 3 });
            writer.Stop();

            // Now do it again this time deleting sim2 and sim3 and adding sim4
            writer = new DataStoreWriter(database);
            writer.WriteRow("Sim1", "Report1", columnNames1, null, new object[] { 5 });
            writer.WriteRow("Sim4", "Report1", columnNames1, null, new object[] { 4 });
            writer.Stop();

            Assert.AreEqual(Utilities.TableToStringUsingSQL(database, "SELECT * FROM Report1 ORDER BY Col"),
                           "CheckpointID,SimulationID,Col\r\n" +
                           "           1,           2,  2\r\n" +
                           "           1,           3,  3\r\n" +
                           "           1,           4,  4\r\n" +
                           "           1,           1,  5\r\n");
        }

        /// <summary>Write a table of data with no simulation name.</summary>
        [Test]
        public void WriteATableOfData()
        {
            DataTable data = new DataTable("Report1");
            data.Columns.Add("Col1(g)", typeof(int));
            data.Columns.Add("Col2", typeof(int));
            DataRow row = data.NewRow();
            row["Col1(g)"] = 10;
            row["Col2"] = 20;
            data.Rows.Add(row);
            row = data.NewRow();
            row["Col1(g)"] = 11;
            row["Col2"] = 21;
            data.Rows.Add(row);

            DataStoreWriter writer = new DataStoreWriter(database);
            writer.WriteTable(data);
            writer.Stop();

            Assert.AreEqual(Utilities.TableToString(database, "Report1"),
                                        "CheckpointID,Col1,Col2\r\n" +
                                        "           1,  10,  20\r\n" +
                                        "           1,  11,  21\r\n");

            // Make sure units were extracted from the column names and written.
            Assert.AreEqual(Utilities.TableToString(database, "_Units"),
               "TableName,ColumnHeading,Units\r\n" +
               "  Report1,         Col1,    g\r\n");
        }

        /// <summary>Write a table of data twice ensuring the old data is removed.</summary>
        [Test]
        public void CleanupOldTableData()
        {
            DataTable data = new DataTable("Report1");
            data.Columns.Add("Col1(g)", typeof(int));
            data.Columns.Add("Col2(kg)", typeof(int));
            DataRow row = data.NewRow();
            row["Col1(g)"] = 10;
            row["Col2(kg)"] = 20;
            data.Rows.Add(row);
            row = data.NewRow();
            row["Col1(g)"] = 11;
            row["Col2(kg)"] = 21;
            data.Rows.Add(row);

            DataStoreWriter writer = new DataStoreWriter(database);
            writer.WriteTable(data);
            writer.Stop();

            // Change the data.
            data.Rows[0][0] = 100;
            data.Rows[0][1] = 200;
            data.Rows[1][0] = 110;
            data.Rows[1][1] = 210;

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

        /// <summary>Delete all rows in a table.</summary>
        [Test]
        public void DeleteAllRowsInTable()
        {
            DataStoreWriter writer = new DataStoreWriter(database);
            string[] columnNames1 = new string[] { "Col1", "Col2" };
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 1), 1.0 });
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 2), 2.0 });
            writer.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 1), 11.0 });
            writer.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 2), 12.0 });
            writer.WriteRow("Sim1", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 1), 21.0 });
            writer.WriteRow("Sim1", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 2), 22.0 });
            writer.WriteRow("Sim2", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 1), 31.0 });
            writer.WriteRow("Sim2", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 2), 32.0 });
            writer.WaitForIdle();

            writer.DeleteRowsInTable("Report1");
            writer.Stop();

            Assert.IsFalse(database.GetTableNames().Contains("Report1"));
        }

        /// <summary>Delete all data for current checkpoint.</summary>
        [Test]
        public void DeleteCurrentCheckpoint()
        {
            DataStoreWriter writer = new DataStoreWriter(database);
            string[] columnNames1 = new string[] { "Col1", "Col2" };
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 1), 1.0 });
            writer.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 2), 2.0 });
            writer.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 1), 11.0 });
            writer.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 2), 12.0 });
            writer.WriteRow("Sim1", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 1), 21.0 });
            writer.WriteRow("Sim1", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 2), 22.0 });
            writer.WriteRow("Sim2", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 1), 31.0 });
            writer.WriteRow("Sim2", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 2), 32.0 });
            writer.Stop();

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
                "ID,   Name,Version,Date\r\n" +
                " 1,Current,       ,    \r\n");
        }

        /// <summary>Revert a checkpoint</summary>
        [Test]
        public void RevertCheckpoint()
        {
            DataStoreReaderTests.CreateTable(database);

            var writer = new DataStoreWriter(database);

            // Write some new current data.
            // Create a database with 3 sims.
            string[] columnNames1 = new string[] { "Col1", "Col2" };
            writer.WriteRow("Sim1", "Report", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 3), 100.0 });
            writer.WriteRow("Sim1", "Report", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 4), 200.0 });

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

            var writer = new DataStoreWriter(database);

            // Write new rows for sim2. Should get rid of old sim2 data and replace 
            // with these 2 new rows.
            string[] columnNames1 = new string[] { "Col1", "Col2" };
            writer.WriteRow("Sim2", "Report", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 1), 3.0 });
            writer.WriteRow("Sim2", "Report", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 2), 4.0 });

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

            database = new SQLite();
            database.OpenDatabase(path, readOnly: false);

            var writer = new DataStoreWriter(database);
            writer.WriteRow("Sim", "Report",
                            new string[] { "Col1" },
                            null,
                            new object[] { 1 });
            writer.Stop();

            Assert.AreEqual(Utilities.TableToString(database, "Report"),
                "CheckpointID,SimulationID,Col1\r\n" +
                "           1,           1,   1\r\n");

            database.CloseDatabase();
            File.Delete(path);
        }
    }
}