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
    using System.Linq;

    [TestFixture]
    public class TestDataStore
    {
        private string fileName;
        private string sqliteFileName;

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
                string[] directories = Directory.GetDirectories(directory, "Bin");
                if (directories.Length == 1)
                {
                    string[] files = Directory.GetFiles(directories[0], "sqlite3.dll");
                    if (files.Length == 1)
                    {
                        return files[0];
                    }
                }

                directory = Path.GetDirectoryName(directory); // parent directory
            }

            throw new Exception("Cannot find apsimx bin directory");
        }

        /// <summary>Initialisation code for all unit tests in this class</summary>
        [SetUp]
        public void Initialise()
        {
            Directory.SetCurrentDirectory(Path.GetTempPath());

            fileName = Path.Combine(Path.GetTempPath(), "TestDataStore.db");
            File.Delete(fileName);

            string sqliteSourceFileName = FindSqlite3DLL();

            sqliteFileName = Path.Combine(Directory.GetCurrentDirectory(), "sqlite3.dll");
            if (!File.Exists(sqliteFileName))
            {
                File.Copy(sqliteSourceFileName, sqliteFileName);
            }
        }

        /// <summary>Clean up after the unit tests</summary>
        [TearDown]
        public void Cleanup()
        {
            File.Delete(fileName);
        }

        /// <summary>Write 2 columns of data for 2 simulations into one table. Ensure data was written correctly</summary>
        [Test]
        public void WriteRow_2Sims1TableSameCols_WriteCorrectly()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                storage.BeginWriting(knownSimulationNames: new string[] { "Sim1", "Sim2" },
                                     simulationNamesBeingRun: new string[] { "Sim1", "Sim2" });

                string[] columnNames1 = new string[] { "Col1", "Col2" };
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 1.0, 11.0 });
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 2.0, 12.0 });
                storage.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 3.0, 13.0 });
                storage.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 4.0, 14.0 });
                storage.EndWriting();
            }

            Assert.AreEqual(TableToString(fileName, "Report1"),
                           "SimulationID, Col1,  Col2\r\n" +
                           "           1,1.000,11.000\r\n" +
                           "           1,2.000,12.000\r\n" +
                           "           2,3.000,13.000\r\n" +
                           "           2,4.000,14.000\r\n");
        }

        /// <summary>Write data for 2 simulations, each with different columns, into one table. Ensure data was written correctly</summary>
        [Test]
        public void WriteRow_2Sims1TableDifferentCols_WriteCorrectly()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                storage.BeginWriting(knownSimulationNames: new string[] { "Sim1", "Sim2" },
                                     simulationNamesBeingRun: new string[] { "Sim1", "Sim2" });

                string[] columnNames1 = new string[] { "Col1", "Col2" };
                string[] columnNames2 = new string[] { "Col3", "Col4" };
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 1.0, 11.0 });
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "(g)" }, new object[] { 2.0, 12.0 });
                storage.WriteRow("Sim2", "Report1", columnNames2, new string[] { null, "(g)" }, new object[] { 3.0, 13.0 });
                storage.WriteRow("Sim2", "Report1", columnNames2, new string[] { null, "(g)" }, new object[] { 4.0, 14.0 });
                storage.EndWriting();
            }

            Assert.AreEqual(TableToString(fileName, "Report1"),
                           "SimulationID, Col1,  Col2, Col3,  Col4\r\n" +
                           "           1,1.000,11.000,     ,      \r\n" +
                           "           1,2.000,12.000,     ,      \r\n" +
                           "           2,     ,      ,3.000,13.000\r\n" +
                           "           2,     ,      ,4.000,14.000\r\n");
        }

        /// <summary>Write array data that changes size into a table. Ensure data was written correctly</summary>
        [Test]
        public void WriteRow_ArrayData_WriteCorrectly()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                storage.BeginWriting(knownSimulationNames: new string[] { "Sim1" },
                                     simulationNamesBeingRun: new string[] { "Sim1" });

                string[] columnNames1 = new string[] { "Col" };
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { "(g)" }, new object[] { new double[] { 1.0 } });
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { "(g)" }, new object[] { new double[] { 2.0, 2.1} });
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { "(g)" }, new object[] { new double[] { 3.0, 3.1, 3.2} });
                storage.EndWriting();
            }

            Assert.AreEqual(TableToString(fileName, "Report1"),
                           "SimulationID,Col(1),Col(2),Col(3)\r\n" +
                           "           1, 1.000,      ,      \r\n" +
                           "           1, 2.000, 2.100,      \r\n" +
                           "           1, 3.000, 3.100, 3.200\r\n");
        }

        /// <summary>Write array of structure data into a table. Ensure data was written correctly</summary>
        [Test]
        public void WriteRow_StructureData_WriteCorrectly()
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

            using (DataStore storage = new DataStore(fileName))
            {
                storage.BeginWriting(knownSimulationNames: new string[] { "Sim1" },
                                     simulationNamesBeingRun: new string[] { "Sim1" });

                string[] columnNames1 = new string[] { "Col" };
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null }, new object[] { record1 });
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null }, new object[] { record2 });
                storage.EndWriting();
            }

            Assert.AreEqual(TableToString(fileName, "Report1"),
                            "SimulationID,     Col.c,Col.d(1),Col.d(2),Col.e(1).a,Col.e(1).b,Col.e(2).a,Col.e(2).b,Col.e(3).a,Col.e(3).b\r\n" +
                            "           1,2017-01-01, 100.000, 101.000,        10,        11,        12,        13,          ,          \r\n" +
                            "           1,2017-01-02, 102.000, 103.000,        16,        17,        18,        19,        20,        21\r\n");
        }

        /// <summary>Ensure that begin writing method does cleanup</summary>
        [Test]
        public void BeginWriting_Cleanup()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                // Create a database with 3 sims.
                storage.BeginWriting(knownSimulationNames: new string[] { "Sim1", "Sim2", "Sim3" },
                                     simulationNamesBeingRun: new string[] { "Sim1" });

                string[] columnNames1 = new string[] { "Col" };
                storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null }, new object[] { 1 });
                storage.WriteRow("Sim2", "Report1", columnNames1, new string[] { null }, new object[] { 2 });
                storage.WriteRow("Sim3", "Report1", columnNames1, new string[] { null }, new object[] { 3 });
                storage.EndWriting();

                // Now do it again this time with another sim.
                storage.BeginWriting(knownSimulationNames: new string[] { "Sim1", "Sim4" },
                                     simulationNamesBeingRun: new string[] { "Sim4" });
                storage.WriteRow("Sim4", "Report1", columnNames1, new string[] { null }, new object[] { 4 });
                storage.EndWriting();
            }

            Assert.AreEqual(TableToString(fileName, "Report1"),
                           "SimulationID,Col\r\n" +
                           "           1,  1\r\n" +
                           "           4,  4\r\n");
        }

        /// <summary>Ensure that GetData when passed a table name returns data for the correct table</summary>
        [Test]
        public void GetData_TableName_ReturnsAllData()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                CreateTable(storage);
                Assert.AreEqual(TableToString(storage.GetData("Report2")),
                                "SimulationName,SimulationID,      Col1,  Col2\r\n" +
                                "          Sim1,           1,2017-01-01,21.000\r\n" +
                                "          Sim1,           1,2017-01-02,22.000\r\n" +
                                "          Sim2,           2,2017-01-01,31.000\r\n" +
                                "          Sim2,           2,2017-01-02,32.000\r\n");
            }
        }

        /// <summary>Ensure that GetData when passed a table name and simulation name returns the correct data</summary>
        [Test]
        public void GetData_TableNameSimName_ReturnsCorrectData()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                CreateTable(storage);
                DataTable data = storage.GetData(tableName:"Report2",
                                                 simulationName: "Sim1");
                Assert.AreEqual(TableToString(data),
                                "SimulationName,SimulationID,      Col1,  Col2\r\n" +
                                "          Sim1,           1,2017-01-01,21.000\r\n" +
                                "          Sim1,           1,2017-01-02,22.000\r\n");
            }
        }

        /// <summary>Ensure that GetData when passed a table name, simulation name and field name returns the correct data</summary>
        [Test]
        public void GetData_TableNameSimNameFieldName_ReturnsCorrectData()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                CreateTable(storage);
                DataTable data = storage.GetData(tableName: "Report2",
                                                 simulationName: "Sim1",
                                                 fieldNames: new string[] { "Col2" });
                Assert.AreEqual(TableToString(data),
                                "SimulationName,SimulationID,  Col2\r\n" +
                                "          Sim1,           1,21.000\r\n" +
                                "          Sim1,           1,22.000\r\n");
            }
        }

        /// <summary>
        /// Ensure that GetData when passed a table name, simulation name, field name
        /// and filter returns the correct data
        /// </summary>
        [Test]
        public void GetData_TableNameSimNameFieldNameFilter_ReturnsCorrectData()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                CreateTable(storage);
                DataTable data = storage.GetData(tableName: "Report2",
                                                 simulationName: "Sim1",
                                                 fieldNames: new string[] { "Col2" },
                                                 filter: "Col2=22");
                Assert.AreEqual(TableToString(data),
                                "SimulationName,SimulationID,  Col2\r\n" +
                                "          Sim1,           1,22.000\r\n");
            }
        }

        /// <summary>
        /// Ensure that GetUnits works
        /// </summary>
        [Test]
        public void GetUnits_TableNameColumnName_ReturnsUnits()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                CreateTable(storage);
                string units = storage.GetUnits(tableName: "Report2",
                                                columnHeading: "Col2");
                Assert.AreEqual(units, "(g/m2)");
            }
        }

        /// <summary>Call WriteTable with no simulation name. Test that it writes correctly.</summary>
        [Test]
        public void WriteTable_Data_CorrectWriting()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                DataTable data = new DataTable("Test");
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
                storage.WriteTable(data);
                Assert.AreEqual(TableToString(fileName, "Test"),
                                           "Col1,Col2\r\n" +
                                           "  10,  20\r\n" +
                                           "  11,  21\r\n");
            }
        }

        /// <summary>Call WriteTable with unknown simulation name. Test that it writes correctly.</summary>
        [Test]
        public void WriteTable_DataWithUnknownSimulationName_CorrectWriting()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                DataTable data = new DataTable("Test");
                data.Columns.Add("SimulationName", typeof(string));
                data.Columns.Add("Col1", typeof(int));
                data.Columns.Add("Col2", typeof(int));
                DataRow row = data.NewRow();
                row["SimulationName"] = "??";
                row["Col1"] = 10;
                row["Col2"] = 20;
                data.Rows.Add(row);
                row = data.NewRow();
                row["SimulationName"] = null;
                row["Col1"] = 11;
                row["Col2"] = 21;
                data.Rows.Add(row);
                storage.WriteTable(data);
                Assert.AreEqual(TableToString(fileName, "Test"),
                                           "SimulationID,SimulationName,Col1,Col2\r\n" +
                                           "            ,            ??,  10,  20\r\n" +
                                           "            ,              ,  11,  21\r\n");
            }
        }

        /// <summary>Call WriteTable with known simulation name. Test that it writes correctly.</summary>
        [Test]
        public void WriteTable_DataWithKnownSimulationName_CorrectWriting()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                // Tell storage about our sim.
                storage.BeginWriting(knownSimulationNames: new string[] { "Sim1" },
                                     simulationNamesBeingRun: new string[] { "Sim1" });
                storage.EndWriting();

                DataTable data = new DataTable("Test");
                data.Columns.Add("SimulationName", typeof(string));
                data.Columns.Add("Col1", typeof(int));
                data.Columns.Add("Col2", typeof(int));
                DataRow row = data.NewRow();
                row["SimulationName"] = "Sim1";
                row["Col1"] = 10;
                row["Col2"] = 20;
                data.Rows.Add(row);
                row = data.NewRow();
                row["SimulationName"] = null;
                row["Col1"] = 11;
                row["Col2"] = 21;
                data.Rows.Add(row);
                storage.WriteTable(data);
                Assert.AreEqual(TableToString(fileName, "Test"),
                                           "SimulationID,SimulationName,Col1,Col2\r\n" +
                                           "           1,          Sim1,  10,  20\r\n" +
                                           "            ,              ,  11,  21\r\n");
            }
        }
        
        /// <summary>Call DeleteTable with a valid table name. Ensure table is deleted</summary>
        [Test]
        public void DeleteTable_TableName_TableDeleted()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                CreateTable(storage);
                storage.DeleteTable("Report1");
                Assert.IsFalse(storage.TableNames.Contains("Report1"));
            }

            SQLite database = new SQLite();
            database.OpenDatabase(fileName, true);
            DataTable tableData = database.ExecuteQuery("SELECT * FROM sqlite_master");
            database.CloseDatabase();
            string[] tableNames = DataTableUtilities.GetColumnAsStrings(tableData, "Name");
            Assert.AreEqual(Array.IndexOf(tableNames, "Report1"), -1);
        }

        /// <summary>Call DeleteTable with a valid table name. Ensure table is deleted</summary>
        [Test]
        public void SimulationNames()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                CreateTable(storage);
                Assert.AreEqual(storage.SimulationNames, new string[] { "Sim1", "Sim2" });
            }
        }

        /// <summary>Call DeleteTable with a valid table name. Ensure table is deleted</summary>
        [Test]
        public void ColumnNames()
        {
            using (DataStore storage = new DataStore(fileName))
            {
                CreateTable(storage);
                Assert.AreEqual(storage.SimulationNames, new string[] { "Col1", "Col2" });
            }
        }

        /// <summary>Call RunQuery with valid sql and ensure correct data is returned.</summary>
        [Test]
        public void RunQuery_sql_DataReturned()
        {
            DataTable data = null;
            using (DataStore storage = new DataStore(fileName))
            {
                CreateTable(storage);
                data = storage.RunQuery("SELECT Col1 FROM Report1");
            }

            Assert.AreEqual(TableToString(data),
                                           "      Col1\r\n" +
                                           "2017-01-01\r\n" +
                                           "2017-01-02\r\n" +
                                           "2017-01-01\r\n" +
                                           "2017-01-02\r\n");
        }

        /// <summary>Convert a SQLite table to a string.</summary>
        private static string TableToString(string fileName, string tableName)
        {
            SQLite database = new SQLite();
            database.OpenDatabase(fileName, true);
            DataTable data = database.ExecuteQuery("SELECT * FROM " + tableName);
            database.CloseDatabase();
            return TableToString(data);
        }

        /// <summary>Convert a DataTable to a string.</summary>
        private static string TableToString(DataTable data)
        {
            StringWriter writer = new StringWriter();
            DataTableUtilities.DataTableToText(data, 0, ",", true, writer);
            return writer.ToString();
        }

        /// <summary>Create a table that we can test</summary>
        private static void CreateTable(DataStore storage)
        {
            storage.BeginWriting(knownSimulationNames: new string[] { "Sim1", "Sim2" },
                                 simulationNamesBeingRun: new string[] { "Sim1", "Sim2" });

            string[] columnNames1 = new string[] { "Col1", "Col2" };
            storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 1), 1.0 });
            storage.WriteRow("Sim1", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 2), 2.0 });
            storage.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 1), 11.0 });
            storage.WriteRow("Sim2", "Report1", columnNames1, new string[] { null, "g" }, new object[] { new DateTime(2017, 1, 2), 12.0 });
            storage.WriteRow("Sim1", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 1), 21.0 });
            storage.WriteRow("Sim1", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 2), 22.0 });
            storage.WriteRow("Sim2", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 1), 31.0 });
            storage.WriteRow("Sim2", "Report2", columnNames1, new string[] { null, "g/m2" }, new object[] { new DateTime(2017, 1, 2), 32.0 });
            storage.EndWriting();
        }
        
    }
}