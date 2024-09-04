namespace UnitTests.APSIMShared
{
    using APSIM.Shared.Utilities;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    class DBMergerTests
    {
        /// <summary>Ensure two .db files, which have the same tables, can be merged.</summary>
        [Test]
        public void DBsThatHaveTheSameTablesMergeCorrectly()
        {
            var database1 = new SQLite();
            database1.OpenDatabase(":memory:", readOnly: false);
            CreateTable(database1, "_Simulations",
                        columnNames: new List<string> { "ID", "Name", "FolderName" },
                        columnTypes: new List<string> { "int", "string", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, "Sim1", "Folder1" },
                                new object[] { 2, "Sim2", "Folder2" }
                            });

            CreateTable(database1, "Report",
                        columnNames: new List<string> { "SimulationID", "A", "B" },
                        columnTypes: new List<string> { "int", "float", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, 10.0, "str1" },
                                new object[] { 2, 11.0, "str2" }
                            });

            var database2 = new SQLite();
            database2.OpenDatabase(":memory:", readOnly: false);
            CreateTable(database2, "_Simulations",
                        columnNames: new List<string> { "ID", "Name", "FolderName" },
                        columnTypes: new List<string> { "int", "string", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, "Sim3", "Folder3" },
                                new object[] { 2, "Sim4", "Folder4" }
                            });

            CreateTable(database2, "Report",
                        columnNames: new List<string> { "SimulationID", "A", "B" },
                        columnTypes: new List<string> { "int", "float", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, 20.0, "str3" },
                                new object[] { 2, 21.0, "str4" }
                            });

            DBMerger.Merge(database2, database1);

            Assert.That(
                Utilities.CreateTable(new string[] {                       "ID", "Name", "FolderName" },
                                      new List<object[]> { new object[] {     1, "Sim1",    "Folder1" },
                                                           new object[] {     2, "Sim2",    "Folder2" },
                                                           new object[] {     3, "Sim3",    "Folder3" },
                                                           new object[] {     4, "Sim4",    "Folder4" }})
               .IsSame(Utilities.GetTableFromDatabase(database1, "_Simulations")), Is.True);

            Assert.That(
                Utilities.CreateTable(new string[]                      { "SimulationID",  "A",    "B" },
                                      new List<object[]> { new object[] {              1,  10, "str1" },
                                                           new object[] {              2,  11, "str2" },
                                                           new object[] {              3,  20, "str3" },
                                                           new object[] {              4,  21, "str4" }})
               .IsSame(Utilities.GetTableFromDatabase(database1, "Report")), Is.True);
        }

        /// <summary>Ensure two .db files, which have the same simulation names, can be merged.</summary>
        [Test]
        public void DBsThatHaveTheSameSimulationsMergeCorrectly()
        {
            var database1 = new SQLite();
            database1.OpenDatabase(":memory:", readOnly: false);
            CreateTable(database1, "_Simulations",
                        columnNames: new List<string> { "ID", "Name", "FolderName" },
                        columnTypes: new List<string> { "int", "string", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, "Sim1", "Folder1" },
                                new object[] { 2, "Sim2", "Folder2" }
                            });

            CreateTable(database1, "Report",
                        columnNames: new List<string> { "SimulationID", "A", "B" },
                        columnTypes: new List<string> { "int", "float", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, 10.0, "str1" },
                                new object[] { 2, 11.0, "str2" }
                            });

            var database2 = new SQLite();
            database2.OpenDatabase(":memory:", readOnly: false);
            CreateTable(database2, "_Simulations",
                        columnNames: new List<string> { "ID", "Name", "FolderName" },
                        columnTypes: new List<string> { "int", "string", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, "Sim1", "Folder3" },
                                new object[] { 2, "Sim1", "Folder4" }
                            });

            CreateTable(database2, "Report",
                        columnNames: new List<string> { "SimulationID", "A", "B" },
                        columnTypes: new List<string> { "int", "float", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, 20.0, "str3" },
                                new object[] { 2, 21.0, "str4" }
                            });

            DBMerger.Merge(database2, database1);

            Assert.That(
                Utilities.CreateTable(new string[] {                       "ID", "Name", "FolderName" },
                                      new List<object[]> { new object[] {     1, "Sim1",    "Folder1" },
                                                           new object[] {     2, "Sim2",    "Folder2" }})
               .IsSame(Utilities.GetTableFromDatabase(database1, "_Simulations")), Is.True);

            Assert.That(
                Utilities.CreateTable(new string[]                      { "SimulationID",  "A",    "B" },
                                      new List<object[]> { new object[] {              1,  10, "str1" },
                                                           new object[] {              1,  20, "str3" },
                                                           new object[] {              2,  11, "str2" },
                                                           new object[] {              2,  21, "str4" }})
               .IsSame(Utilities.GetTableFromDatabase(database1, "Report")), Is.True);
        }

        /// <summary>Ensure two .db files, which have different tables, can be merged.</summary>
        [Test]
        public void DBThatHaveTheDifferentTablesMergeCorrectly()
        {
            var database1 = new SQLite();
            database1.OpenDatabase(":memory:", readOnly: false);
            CreateTable(database1, "_Simulations",
                        columnNames: new List<string> { "ID", "Name", "FolderName" },
                        columnTypes: new List<string> { "int", "string", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, "Sim1", "Folder1" },
                                new object[] { 2, "Sim2", "Folder2" }
                            });

            CreateTable(database1, "Report1",
                        columnNames: new List<string> { "SimulationID", "A", "B" },
                        columnTypes: new List<string> { "int", "float", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, 10.0, "str1" },
                                new object[] { 2, 11.0, "str2" }
                            });

            var database2 = new SQLite();
            database2.OpenDatabase(":memory:", readOnly: false);
            CreateTable(database2, "_Simulations",
                        columnNames: new List<string> { "ID", "Name", "FolderName" },
                        columnTypes: new List<string> { "int", "string", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, "Sim3", "Folder3" },
                                new object[] { 2, "Sim4", "Folder4" }
                            });

            CreateTable(database2, "Report2",
                        columnNames: new List<string> { "SimulationID", "A", "B" },
                        columnTypes: new List<string> { "int", "float", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, 20.0, "str3" },
                                new object[] { 2, 21.0, "str4" }
                            });

            DBMerger.Merge(database2, database1);

            Assert.That(
                Utilities.CreateTable(new string[] { "ID", "Name", "FolderName" },
                                      new List<object[]> { new object[] {     1, "Sim1",    "Folder1" },
                                                           new object[] {     2, "Sim2",    "Folder2" },
                                                           new object[] {     3, "Sim3",    "Folder3" },
                                                           new object[] {     4, "Sim4",    "Folder4" }})
               .IsSame(Utilities.GetTableFromDatabase(database1, "_Simulations")), Is.True);

            Assert.That(
                Utilities.CreateTable(new string[]                      { "SimulationID", "A", "B" },
                                      new List<object[]> { new object[] {              1,  10, "str1" },
                                                           new object[] {              2,  11, "str2" }})
               .IsSame(Utilities.GetTableFromDatabase(database1, "Report1")), Is.True);

            Assert.That(
                Utilities.CreateTable(new string[]                      { "SimulationID", "A", "B" },
                                      new List<object[]> { new object[] {              3,  20, "str3" },
                                                           new object[] {              4,  21, "str4" }})
               .IsSame(Utilities.GetTableFromDatabase(database1, "Report2")), Is.True);
        }

        /// <summary>Ensure two .db files, which have tables that don't have SimulationID, can be merged.</summary>
        [Test]
        public void DBsThatHaveTablesWithNoSimulationIDMergeCorrectly()
        {
            var database1 = new SQLite();
            database1.OpenDatabase(":memory:", readOnly: false);
            CreateTable(database1, "_Simulations",
                        columnNames: new List<string> { "ID", "Name", "FolderName" },
                        columnTypes: new List<string> { "int", "string", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, "Sim1", "Folder1" },
                                new object[] { 2, "Sim2", "Folder2" }
                            });

            CreateTable(database1, "_Units",
                        columnNames: new List<string> { "TableName", "ColumnHeading", "Units" },
                        columnTypes: new List<string> { "string", "string", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { "Report", "A", "g/m2" }
                            });

            var database2 = new SQLite();
            database2.OpenDatabase(":memory:", readOnly: false);
            CreateTable(database2, "_Simulations",
                        columnNames: new List<string> { "ID", "Name", "FolderName" },
                        columnTypes: new List<string> { "int", "string", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { 1, "Sim1", "Folder3" },
                                new object[] { 2, "Sim1", "Folder4" }
                            });

            CreateTable(database2, "_Units",
                        columnNames: new List<string> { "TableName", "ColumnHeading", "Units" },
                        columnTypes: new List<string> { "string", "string", "string" },
                        rowValues: new List<object[]>
                            {
                                new object[] { "Report", "B", "kg/ha" }
                            });

            DBMerger.Merge(database2, database1);

            Assert.That(
                Utilities.CreateTable(new string[] { "ID", "Name", "FolderName" },
                                      new List<object[]> { new object[] {     1, "Sim1",    "Folder1" },
                                                           new object[] {     2, "Sim2",    "Folder2" }})
               .IsSame(Utilities.GetTableFromDatabase(database1, "_Simulations")), Is.True);

            Assert.That(
                Utilities.CreateTable(new string[] { "TableName", "ColumnHeading", "Units" },
                                      new List<object[]> { new object[] {  "Report",          "A",  "g/m2" },
                                                           new object[] {  "Report",          "B", "kg/ha" }})
               .IsSame(Utilities.GetTableFromDatabase(database1, "_Units")), Is.True);
        }

        /// <summary>Create a table</summary>
        /// <param name="database">The db to create the table in.</param>
        /// <param name="tableName">The name of the table to create.</param>
        /// <param name="columnNames">The column names.</param>
        /// <param name="columnTypes">The column types.</param>
        /// <param name="rowValues">The row values to insert into the table.</param>
        private void CreateTable(IDatabaseConnection database, string tableName, List<string> columnNames, List<string> columnTypes, List<object[]> rowValues)
        { 
            database.CreateTable(tableName, columnNames, columnTypes);
            database.InsertRows(tableName, columnNames, rowValues);
        }
    }
}
