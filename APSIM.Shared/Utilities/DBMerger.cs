namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Merges two .db files.
    /// </summary>
    public class DBMerger
    {
        /// <summary>
        /// Merge multiple .db files into a single .db file.
        /// </summary>
        /// <param name="fileSpec">The file specification for the .db files to merge.</param>
        /// <param name="recurse">Recursively search for matching .db files in child directories?</param>
        /// <param name="outFileName">The name of the new output file.</param>
        public static void MergeFiles(string fileSpec, bool recurse, string outFileName)
        {
            File.Delete(outFileName);

            string[] filesToMerge = DirectoryUtilities.FindFiles(fileSpec, recurse);
            if (filesToMerge.Length > 1)
            {
                File.Copy(filesToMerge[0], outFileName, overwrite:true);

                var destination = new SQLite();
                destination.OpenDatabase(outFileName, readOnly: false);
                try
                {

                    foreach (var sourceFileName in filesToMerge.Where(file => file != filesToMerge[0]))
                    {
                        var source = new SQLite();
                        source.OpenDatabase(sourceFileName, readOnly: true);
                        try
                        {
                            Merge(source, destination);
                        }
                        finally
                        {
                            source.CloseDatabase();
                        }
                    }
                }
                finally
                {
                    destination.CloseDatabase();
                }
            }
        }

        /// <summary>
        /// Merge a source .db file into a destination .db file.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void Merge(IDatabaseConnection source, SQLite destination)
        {
            destination.BeginTransaction();

            if (source.GetTableNames().Contains("_Simulations"))
            {
                var sourceData = source.ExecuteQuery("SELECT * FROM _Simulations");
                var destinationData = destination.ExecuteQuery("SELECT * FROM _Simulations");
                var simulationNameIDMapping = destinationData
                                                  .AsEnumerable()
                                                  .ToDictionary(row => row.Field<string>(1),
                                                                row => row.Field<int>(0));

                var oldIDNewIDMapping = new Dictionary<int, int>();
                var columnNames = DataTableUtilities.GetColumnNames(destinationData).ToList();
                foreach (DataRow simulationRow in sourceData.Rows)
                {
                    string name = simulationRow["Name"].ToString();
                    if (!simulationNameIDMapping.TryGetValue(name, out int id))
                    {
                        // Add a new row to destination.
                        var newID = simulationNameIDMapping.Values.Max() + 1;
                        simulationNameIDMapping.Add(name, newID);
                        var oldID = Convert.ToInt32(simulationRow["ID"]);
                        oldIDNewIDMapping.Add(oldID, newID);
                        simulationRow["ID"] = newID;
                        destination.InsertRows("_Simulations", columnNames, new List<object[]>() { simulationRow.ItemArray });
                    }
                }

                foreach (var tableName in source.GetTableNames().Where(t => t != "_Simulations" && t != "_Checkpoints"))
                    MergeTable(source, destination, tableName, oldIDNewIDMapping);
            }

            destination.EndTransaction();
        }

        /// <summary>
        /// Move all data from the specified table in destination to source.
        /// </summary>
        /// <param name="source">The source database.</param>
        /// <param name="destination">The destination database.</param>
        /// <param name="tableName">The name of the table to merge.</param>
        /// <param name="oldIDNewIDMapping">A mapping from source IDs to destination IDs.</param>
        private static void MergeTable(IDatabaseConnection source, IDatabaseConnection destination, string tableName, Dictionary<int, int> oldIDNewIDMapping)
        {
            var sourceData = source.ExecuteQuery("SELECT * FROM " + tableName);

            DataTable destinationData;
            if (destination.GetTableNames().Contains(tableName))
                destinationData = destination.ExecuteQuery("SELECT * FROM " + tableName);
            else
            {
                // Need to create the table.
                var colNames = sourceData.Columns.Cast<DataColumn>().Select(col => col.ColumnName).ToList();
                var colTypes = sourceData.Columns.Cast<DataColumn>().Select(col => source.GetDBDataTypeName(col.DataType)).ToList();
                destination.CreateTable(tableName, colNames, colTypes);
            }

            var columnNames = DataTableUtilities.GetColumnNames(sourceData).ToList();
            foreach (DataRow simulationRow in sourceData.Rows)
            {
                if (columnNames.Contains("SimulationID"))
                {
                    var oldID = Convert.ToInt32(simulationRow["SimulationID"]);
                    if (oldIDNewIDMapping.TryGetValue(oldID, out int newID))
                    {
                        // Change the ID to new ID
                        simulationRow["SimulationID"] = newID;
                    }
                }
                destination.InsertRows(tableName, columnNames, new List<object[]>() { simulationRow.ItemArray });
            }

        }
    }
}
