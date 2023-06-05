using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using APSIM.Shared.JobRunning;

namespace Models.Storage
{

    /// <summary>Encapsulates a command to empty the database as much as possible.</summary>
    class AddCheckpointCommand : IRunnable
    {
        private DataStoreWriter writer;
        private string newCheckpointName;
        private IEnumerable<string> namesOfFilesToStore;

        /// <summary>
        /// Name of the job.
        /// </summary>
        public string Name { get { return "Add Checkpoint"; } }

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress { get { return 0; } }

        /// <summary>Constructor</summary>
        /// <param name="dataStoreWriter">The datastore writer that called this constructor.</param>
        /// <param name="checkpointName">The new checkpoint name to create.</param>
        /// <param name="fileNamesToStore">Names of files to store in checkpoint.</param>
        public AddCheckpointCommand(DataStoreWriter dataStoreWriter,
                                       string checkpointName,
                                       IEnumerable<string> fileNamesToStore)
        {
            writer = dataStoreWriter;
            newCheckpointName = checkpointName;
            namesOfFilesToStore = fileNamesToStore;
        }

        /// <summary>
        /// Prepare the job for running.
        /// </summary>
        public void Prepare()
        {
            // Do nothing.
        }

        /// <summary>Called to run the command. Can throw on error.</summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            var checkpointData = new DataView(writer.Connection.ExecuteQuery("SELECT * FROM [_Checkpoints]"));
            checkpointData.RowFilter = "Name='Current'";
            if (checkpointData.Count == 1)
            {
                int currentCheckId = Convert.ToInt32(checkpointData[0]["ID"], CultureInfo.InvariantCulture);

                // Get the current version and the date time now to write to the checkpoint table.
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string now = writer.Connection.AsSQLString(DateTime.Now);

                // If checkpoint already exists then delete old one.
                int newCheckId;
                checkpointData.RowFilter = string.Format("[Name]='{0}'", newCheckpointName);
                if (checkpointData.Count == 1)
                {
                    // Yes checkpoint already exists - delete old data.
                    newCheckId = Convert.ToInt32(checkpointData[0]["ID"], CultureInfo.InvariantCulture);
                    foreach (var tableName in writer.Connection.GetTableNames())
                    {
                        List<string> columnNames = writer.Connection.GetColumnNames(tableName);
                        if (columnNames.Contains("CheckpointID"))
                        {
                            var deleteSql = string.Format("DELETE FROM [{0}] WHERE [CheckpointID]={1}",
                                                    tableName, newCheckId);
                            writer.Connection.ExecuteNonQuery(deleteSql);
                        }
                    }

                    // Update row in checkpoints table.
                    var sql = string.Format("UPDATE [_Checkpoints] " +
                                            "SET [Version]='{0}' " +
                                            "SET [Date]='{1}') " +
                                            "WHERE [ID]={2}",
                                            version, now, newCheckId);
                }
                else
                {
                    // checkpoint doesn't already exist find a unique ID we can use.
                    checkpointData.RowFilter = null;
                    newCheckId = writer.GetCheckpointID(newCheckpointName);
                }

                // Go through all tables and copy the current data rows to new rows with
                // our new checkpoint id.
                foreach (var tableName in writer.Connection.GetTableNames())
                {
                    List<string> columnNames = writer.Connection.GetColumnNames(tableName);
                    if (tableName != "_CheckpointFiles" && columnNames.Contains("CheckpointID"))
                    {
                        columnNames.Remove("CheckpointID");

                        string csvFieldNames = null;
                        foreach (string columnName in columnNames)
                        {
                            if (csvFieldNames != null)
                                csvFieldNames += ",";
                            csvFieldNames += "[" + columnName + "]";
                        }

                        var sql = string.Format("INSERT INTO [{0}] ([CheckpointID],{1})" +
                                                " SELECT {2},{1}" +
                                                " FROM [{0}]" +
                                                " WHERE [CheckpointID] = {3}",
                                                tableName, csvFieldNames, newCheckId, currentCheckId);
                        writer.Connection.ExecuteNonQuery(sql);
                    }
                }

                // Add in all referenced files.
                if (namesOfFilesToStore != null)
                {
                    DataTable checkpointFiles = new DataTable("_CheckpointFiles");
                    checkpointFiles.Columns.Add("CheckpointID", typeof(int));
                    checkpointFiles.Columns.Add("FileName", typeof(string));
                    checkpointFiles.Columns.Add("Contents", typeof(object));

                    foreach (string fileName in namesOfFilesToStore)
                    {
                        if (File.Exists(fileName))
                        {
                            var row = checkpointFiles.NewRow();
                            row[0] = newCheckId;
                            row[1] = fileName;
                            row[2] = File.ReadAllBytes(fileName);
                            checkpointFiles.Rows.Add(row);
                        }
                    }

                    if (checkpointFiles.Rows.Count > 0)
                        writer.WriteTable(checkpointFiles, deleteAllData: true);
                }
            }
        }

        /// <summary>
        /// Cleanup the job after running it.
        /// </summary>
        public void Cleanup()
        {
            // Do nothing.
        }
    }
}
