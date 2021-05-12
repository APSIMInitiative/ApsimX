namespace UnitTests.Sheet
{
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using UserInterface.Views;

    [TestFixture]
    class PagedDataTableTests
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


        /// <summary></summary>
        [Test]
        public void PagingWorks()
        {
            CreateTable(database);
            
            var reader = new DataStoreReader(database);
            var provider = new PagedDataTableProvider(reader, "Current", "Report", 
                                                      new string[] { "Sim1" },
                                                      null,
                                                      null,
                                                      2);

            Assert.AreEqual(2, provider.ColumnCount);
            Assert.AreEqual("SimulationName", provider.GetCellContents(0, 0));
            Assert.AreEqual("Col1", provider.GetCellContents(1, 0));
            Assert.AreEqual(9, provider.RowCount);
            Assert.AreEqual(2, provider.NumHeadingRows);
            Assert.AreEqual("Col1", provider.GetCellContents(1, 0));
            Assert.AreEqual("g", provider.GetCellContents(1, 1));
            Assert.AreEqual("1", provider.GetCellContents(1, 2));
            Assert.AreEqual("2", provider.GetCellContents(1, 3));
            Assert.AreEqual("3", provider.GetCellContents(1, 4));
            Assert.AreEqual("4", provider.GetCellContents(1, 5));
            Assert.AreEqual("5", provider.GetCellContents(1, 6));
            Assert.AreEqual("6", provider.GetCellContents(1, 7));
            Assert.AreEqual("7", provider.GetCellContents(1, 8));
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
                new object[] { 1, "Sim1", string.Empty }
            };
            database.InsertRows("_Simulations", columnNames, rows);

            // Create a Report table.
            columnNames = new List<string>() { "CheckpointID", "SimulationID", "Col1" };
            columnTypes = new List<string>() { "integer", "integer", "integer" };
            database.CreateTable("Report", columnNames, columnTypes);
            rows = new List<object[]>
            {
                new object[] { 1, 1, 1 },
                new object[] { 1, 1, 2 },
                new object[] { 1, 1, 3 },
                new object[] { 1, 1, 4 },
                new object[] { 1, 1, 5 },
                new object[] { 1, 1, 6 },
                new object[] { 1, 1, 7 }
            };
            database.InsertRows("Report", columnNames, rows);

            // Create a _Units table.
            columnNames = new List<string>() { "TableName", "ColumnHeading", "Units" };
            columnTypes = new List<string>() { "char(50)", "char(50)", "char(50)" };
            database.CreateTable("_Units", columnNames, columnTypes);
            rows = new List<object[]>
            {
                new object[] { "Report", "Col1", "g" }
            };
            database.InsertRows("_Units", columnNames, rows);
        }
    }
}
