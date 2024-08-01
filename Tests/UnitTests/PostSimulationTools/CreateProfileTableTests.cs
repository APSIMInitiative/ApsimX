using APSIM.Shared.Utilities;
using Models.PostSimulationTools;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

namespace UnitTests
{
    public class CreateProfileTableTests
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
        public void TestDepth()
        {
            CreateTable(database);

            var columnNames = new List<string>() { "CheckpointID", "SimulationID", "Year", "Col1(1)", "Col1(2)", "Col2(1)", "Col2(2)" };

            var predictedRows = new List<object[]>
            {
                new object[] { 1, 1, 1970, 10, 11, 12, 13 },
                new object[] { 1, 1, 1971, 14, 15, 16, 17 },
            };
            database.InsertRows("Report", columnNames, predictedRows);

            var dataStore = new DataStore(database);

            CreateProfileTable tool = new()
            {
                SourceTableName = "Report",
            };
            Utilities.InjectLink(tool, "dataStore", dataStore);
            tool.Run();
            dataStore.Writer.Stop();
            dataStore.Reader.Refresh();
            var data = dataStore.Reader.GetData("CreateProfileTable");

            DataTable table = Utilities.CreateTable(new string[] { "CheckpointName", "CheckpointID", "SimulationName", "SimulationID", "Year", "Col1", "Col2" },
                   new List<object[]> { new object[] {                    "Current",             1,            "Sim1",              1,   1970,     10,    12 },
                                        new object[] {                    "Current",             1,            "Sim1",              1,   1970,     11,    13 },
                                        new object[] {                    "Current",             1,            "Sim1",              1,   1971,     14,    16 },
                                        new object[] {                    "Current",             1,            "Sim1",              1,   1971,     15,    17 },
                   });

            Assert.That(table.IsSame(data), Is.True);
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
            columnNames = new List<string>() { "CheckpointID", "SimulationID", "Year", "Col1(1)", "Col1(2)", "Col2(1)", "Col2(2)" };
            columnTypes = new List<string>() { "integer", "integer", "integer", "integer", "integer", "integer", "integer" };
            database.CreateTable("Report", columnNames, columnTypes);
        }
    }
}
