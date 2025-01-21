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
    using Gtk.Sheet;
    
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

            Assert.That(provider.ColumnCount, Is.EqualTo(3));
            Assert.That(provider.GetColumnName(0), Is.EqualTo("SimulationName"));
            Assert.That(provider.GetColumnName(1), Is.EqualTo("Col1"));
            Assert.That(provider.GetColumnName(2), Is.EqualTo("Col2"));
            Assert.That(provider.RowCount, Is.EqualTo(7));
            Assert.That(provider.GetColumnUnits(1), Is.EqualTo("g"));
            Assert.That(provider.GetCellContents(1, 0), Is.EqualTo("1"));
            Assert.That(provider.GetCellContents(1, 1), Is.EqualTo("2"));
            Assert.That(provider.GetCellContents(1, 2), Is.EqualTo("3"));
            Assert.That(provider.GetCellContents(1, 3), Is.EqualTo("4"));
            Assert.That(provider.GetCellContents(1, 4), Is.EqualTo("5"));
            Assert.That(provider.GetCellContents(1, 5), Is.EqualTo("6"));
            Assert.That(provider.GetCellContents(1, 6), Is.EqualTo("7"));
        }

        /// <summary>Ensure paging with a column filter works.</summary>
        [Test]
        public void PagingWithColumnFilterWorks()
        {
            CreateTable(database);

            var reader = new DataStoreReader(database);
            var provider = new PagedDataProvider(reader, "Current", "Report", null, "Col2", null, "", 2);

            Assert.That(provider.ColumnCount, Is.EqualTo(2));
            Assert.That(provider.GetColumnName(0), Is.EqualTo("SimulationName"));
            Assert.That(provider.GetColumnName(1), Is.EqualTo("Col2"));
            Assert.That(provider.RowCount, Is.EqualTo(7));
            Assert.That(provider.GetCellContents(1, 0), Is.EqualTo("8"));
            Assert.That(provider.GetCellContents(1, 1), Is.EqualTo("9"));
            Assert.That(provider.GetCellContents(1, 2), Is.EqualTo("10"));
            Assert.That(provider.GetCellContents(1, 3), Is.EqualTo("11"));
            Assert.That(provider.GetCellContents(1, 4), Is.EqualTo("12"));
            Assert.That(provider.GetCellContents(1, 5), Is.EqualTo("13"));
            Assert.That(provider.GetCellContents(1, 6), Is.EqualTo("14"));
        }

        /// <summary>Ensure paging with a simulation, column and row filter works.</summary>
        [Test]
        public void PagingWithSimulationColumnandRowFilterWorks()
        {
            CreateTable(database);

            var reader = new DataStoreReader(database);
            var provider = new PagedDataProvider(reader, "Current", "Report", new string[] { "Sim1" }, "Col1,Col2", "Col1 > 2", "", 2);

            Assert.That(provider.ColumnCount, Is.EqualTo(3));
            Assert.That(provider.GetColumnName(0), Is.EqualTo("SimulationName"));
            Assert.That(provider.GetColumnName(1), Is.EqualTo("Col1"));
            Assert.That(provider.GetColumnName(2), Is.EqualTo("Col2"));
            Assert.That(provider.GetColumnUnits(1), Is.EqualTo("g"));
            Assert.That(provider.RowCount, Is.EqualTo(1));
            Assert.That(provider.GetCellContents(2, 0), Is.EqualTo("10"));
        }

        /// <summary>Ensure paging with a simulation, column and row filter works.</summary>
        /// <remarks>https://github.com/APSIMInitiative/ApsimX/issues/9209</remarks>
        [Test]
        public void PagingWithSimulationColumnandRowFilter2()
        {
            CreateTable(database);

            var reader = new DataStoreReader(database);
            var provider = new PagedDataProvider(reader, "Current", "Report", new string[] { "Sim1" }, "Col1,Col2", "Col1 = '2'", "", 2);

            Assert.That(provider.ColumnCount, Is.EqualTo(3));
            Assert.That(provider.GetColumnName(0), Is.EqualTo("SimulationName"));
            Assert.That(provider.GetColumnName(1), Is.EqualTo("Col1"));
            Assert.That(provider.GetColumnName(2), Is.EqualTo("Col2"));
            Assert.That(provider.GetColumnUnits(1), Is.EqualTo("g"));
            Assert.That(provider.RowCount, Is.EqualTo(1));
            Assert.That(provider.GetCellContents(2, 0), Is.EqualTo("9"));
        }        

        /// <summary>Ensure a row filter with SimulationName = 'aaaa' works.</summary>
        [Test]
        public void PagingWithRowFilterUsingSimulationName()
        {
            CreateTable(database);

            var reader = new DataStoreReader(database);
            var provider = new PagedDataProvider(reader, "Current", "Report", new string[] { "Sim1" }, "Col1,Col2", "[SimulationName] = 'Sim1'", "", 2);

            Assert.That(provider.ColumnCount, Is.EqualTo(3));
            Assert.That(provider.GetColumnName(0), Is.EqualTo("SimulationName"));
            Assert.That(provider.GetColumnName(1), Is.EqualTo("Col1"));
            Assert.That(provider.GetColumnName(2), Is.EqualTo("Col2"));
            Assert.That(provider.GetColumnUnits(1), Is.EqualTo("g"));
            Assert.That(provider.RowCount, Is.EqualTo(3));
            Assert.That(provider.GetCellContents(2, 0), Is.EqualTo("8"));
            Assert.That(provider.GetCellContents(2, 1), Is.EqualTo("9"));
            Assert.That(provider.GetCellContents(2, 2), Is.EqualTo("10"));
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
