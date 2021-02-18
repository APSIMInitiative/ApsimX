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

            Assert.AreEqual(Utilities.TableToString(database1, "_Simulations"),
               $"ID,Name,FolderName{Environment.NewLine}" +
               $" 1,Sim1,   Folder1{Environment.NewLine}" +
               $" 2,Sim2,   Folder2{Environment.NewLine}" +
               $" 3,Sim3,   Folder3{Environment.NewLine}" +
               $" 4,Sim4,   Folder4{Environment.NewLine}");

            Assert.AreEqual(Utilities.TableToString(database1, "Report"),
               $"SimulationID,     A,   B{Environment.NewLine}" +
               $"           1,10.000,str1{Environment.NewLine}" +
               $"           2,11.000,str2{Environment.NewLine}" +
               $"           3,20.000,str3{Environment.NewLine}" +
               $"           4,21.000,str4{Environment.NewLine}");

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

            Assert.AreEqual(Utilities.TableToString(database1, "_Simulations"),
               $"ID,Name,FolderName{Environment.NewLine}" +
               $" 1,Sim1,   Folder1{Environment.NewLine}" +
               $" 2,Sim2,   Folder2{Environment.NewLine}");

            Assert.AreEqual(Utilities.TableToString(database1, "Report"),
               $"SimulationID,     A,   B{Environment.NewLine}" +
               $"           1,10.000,str1{Environment.NewLine}" +
               $"           1,20.000,str3{Environment.NewLine}" +
               $"           2,11.000,str2{Environment.NewLine}" +
               $"           2,21.000,str4{Environment.NewLine}");

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

            Assert.AreEqual(Utilities.TableToString(database1, "_Simulations"),
               $"ID,Name,FolderName{Environment.NewLine}" +
               $" 1,Sim1,   Folder1{Environment.NewLine}" +
               $" 2,Sim2,   Folder2{Environment.NewLine}" +
               $" 3,Sim3,   Folder3{Environment.NewLine}" +
               $" 4,Sim4,   Folder4{Environment.NewLine}");

            Assert.AreEqual(Utilities.TableToString(database1, "Report1"),
               $"SimulationID,     A,   B{Environment.NewLine}" +
               $"           1,10.000,str1{Environment.NewLine}" +
               $"           2,11.000,str2{Environment.NewLine}");

            Assert.AreEqual(Utilities.TableToString(database1, "Report2"),
               $"SimulationID,     A,   B{Environment.NewLine}" +
               $"           3,20.000,str3{Environment.NewLine}" +
               $"           4,21.000,str4{Environment.NewLine}");
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

            Assert.AreEqual(Utilities.TableToString(database1, "_Simulations"),
               $"ID,Name,FolderName{Environment.NewLine}" +
               $" 1,Sim1,   Folder1{Environment.NewLine}" +
               $" 2,Sim2,   Folder2{Environment.NewLine}");

            Assert.AreEqual(Utilities.TableToString(database1, "_Units"),
               $"TableName,ColumnHeading,Units{Environment.NewLine}" +
               $"   Report,            A, g/m2{Environment.NewLine}" +
               $"   Report,            B,kg/ha{Environment.NewLine}");

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
