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

        /// <summary>Ensure basic data paging works.</summary>
        [Test]
        public void PagingWorks()
        {
            CreateTable(database);
            
            var reader = new DataStoreReader(database);
            var provider = new PagedDataProvider(reader, "Current", "Report", null, null, null, "", 2);

            Assert.AreEqual(3, provider.ColumnCount);
            Assert.AreEqual("SimulationName", provider.GetCellContents(0, 0));
            Assert.AreEqual("Col1", provider.GetCellContents(1, 0));
            Assert.AreEqual("Col2", provider.GetCellContents(2, 0));
            Assert.AreEqual(9, provider.RowCount);
            Assert.AreEqual(2, provider.NumHeadingRows);
            Assert.AreEqual("g", provider.GetCellContents(1, 1));
            Assert.AreEqual("1", provider.GetCellContents(1, 2));
            Assert.AreEqual("2", provider.GetCellContents(1, 3));
            Assert.AreEqual("3", provider.GetCellContents(1, 4));
            Assert.AreEqual("4", provider.GetCellContents(1, 5));
            Assert.AreEqual("5", provider.GetCellContents(1, 6));
            Assert.AreEqual("6", provider.GetCellContents(1, 7));
            Assert.AreEqual("7", provider.GetCellContents(1, 8));
        }

        /// <summary>Ensure paging with a column filter works.</summary>
        [Test]
        public void PagingWithColumnFilterWorks()
        {
            CreateTable(database);

            var reader = new DataStoreReader(database);
            var provider = new PagedDataProvider(reader, "Current", "Report", null, "Col2", null, "", 2);

            Assert.AreEqual(2, provider.ColumnCount);
            Assert.AreEqual("SimulationName", provider.GetCellContents(0, 0));
            Assert.AreEqual("Col2", provider.GetCellContents(1, 0));
            Assert.AreEqual(8, provider.RowCount);
            Assert.AreEqual(1, provider.NumHeadingRows);
            Assert.AreEqual("8", provider.GetCellContents(1, 1));
            Assert.AreEqual("9", provider.GetCellContents(1, 2));
            Assert.AreEqual("10", provider.GetCellContents(1, 3));
            Assert.AreEqual("11", provider.GetCellContents(1, 4));
            Assert.AreEqual("12", provider.GetCellContents(1, 5));
            Assert.AreEqual("13", provider.GetCellContents(1, 6));
            Assert.AreEqual("14", provider.GetCellContents(1, 7));
        }

        /// <summary>Ensure paging with a simulation, column and row filter works.</summary>
        [Test]
        public void PagingWithSimulationColumnandRowFilterWorks()
        {
            CreateTable(database);

            var reader = new DataStoreReader(database);
            var provider = new PagedDataProvider(reader, "Current", "Report", new string[] { "Sim1" }, "Col1,Col2", "Col1 > 2", "", 2);

            Assert.AreEqual(3, provider.ColumnCount);
            Assert.AreEqual("SimulationName", provider.GetCellContents(0, 0));
            Assert.AreEqual("Col1", provider.GetCellContents(1, 0));
            Assert.AreEqual("Col2", provider.GetCellContents(2, 0));
            Assert.AreEqual(3, provider.RowCount);
            Assert.AreEqual(2, provider.NumHeadingRows);
            Assert.AreEqual("g", provider.GetCellContents(1, 1));
            Assert.IsNull(provider.GetCellContents(2, 1));
            Assert.AreEqual("10", provider.GetCellContents(2, 2));
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
            columnTypes = new List<string>() { "integer", "integer", "integer", "integer" };
            database.CreateTable("Report", columnNames, columnTypes);
            rows = new List<object[]>
            {
                new object[] { 1, 1, 1,  8 },
                new object[] { 1, 1, 2,  9 },
                new object[] { 1, 1, 3, 10 },
                new object[] { 1, 2, 4, 11 },
                new object[] { 1, 2, 5, 12 },
                new object[] { 1, 2, 6, 13 },
                new object[] { 1, 2, 7, 14 }
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
