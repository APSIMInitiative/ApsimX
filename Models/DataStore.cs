using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Models.Core;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using APSIM.Shared.Utilities;

namespace Models
{
    /// <summary>
    /// A data storage model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.DataStoreView")]
    [PresenterName("UserInterface.Presenters.DataStorePresenter")]
    public class DataStore : Model
    {
        /// <summary>A SQLite connection shared between all instances of this DataStore.</summary>
        [NonSerialized]
        private SQLite Connection = null;

        /// <summary>The filename of the SQLite .db</summary>
        /// <value>The filename.</value>
        [XmlIgnore]
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data store should export to text files
        /// automatically when all simulations finish.
        /// </summary>
        /// <value><c>true</c> if [automatic export]; otherwise, <c>false</c>.</value>
        public bool AutoExport { get; set; }

        /// <summary>A flag that when true indicates that the DataStore is in post processing model.</summary>
        private bool DoingPostProcessing = false;

        /// <summary>A sub class for holding information about a table write.</summary>
        private class TableToWrite
        {
            /// <summary>The file name</summary>
            public string FileName;
            /// <summary>The simulation name</summary>
            public string SimulationName;
            /// <summary>The simulation identifier</summary>
            public int SimulationID = int.MaxValue;
            /// <summary>The table name</summary>
            public string TableName;
            /// <summary>The data</summary>
            public DataTable Data;
        }

        /// <summary>A collection of datatables that need writing.</summary>
        private static List<TableToWrite> TablesToWrite = new List<TableToWrite>();


        /// <summary>
        /// This class encapsulates a simple lock mechanism. It is used by DataStore to
        /// apply file level locking.
        /// </summary>
        private class DbMutex
        {
            /// <summary>The locked</summary>
            private bool Locked = false;

            /// <summary>Aquire a lock. If already locked then wait a bit.</summary>
            public void Aquire()
            {
                lock (this)
                {
                    while (Locked)
                        Thread.Sleep(100);
                    Locked = true;
                }
            }

            /// <summary>Release a lock.</summary>
            public void Release()
            {
                Locked = false;
            }
        }

        /// <summary>A static dictionary of locks, one for each filename.</summary>
        private static Dictionary<string, DbMutex> Locks = new Dictionary<string, DbMutex>();

        /// <summary>Is the .db file open for writing?</summary>
        private bool ForWriting = false;

        /// <summary>
        /// An enum that is used to indicate message severity when writing messages to the .db
        /// </summary>
        public enum ErrorLevel 
        {
            /// <summary>Information</summary>
            Information,

            /// <summary>Warning</summary>
            Warning,

            /// <summary>Error</summary>
            Error 
        };

        /// <summary>
        /// A parameterless constructor purely for the XML serialiser. Other models
        /// shouldn't use this contructor.
        /// </summary>
        public DataStore()
        {
        }

        /// <summary>A constructor that needs to know the calling model.</summary>
        /// <param name="ownerModel">The owner model.</param>
        /// <param name="baseline">if set to <c>true</c> [baseline].</param>
        public DataStore(Model ownerModel, bool baseline = false)
        {
            Simulation simulation = Apsim.Parent(ownerModel, typeof(Simulation)) as Simulation;
            if (simulation == null)
            {
                Simulations simulations = Apsim.Parent(ownerModel, typeof(Simulations)) as Simulations;
                if (simulations != null)
                    Filename = Path.ChangeExtension(simulations.FileName, ".db");
            }
            else
                Filename = Path.ChangeExtension(simulation.FileName, ".db");

            if (Filename != null && baseline)
                Filename += ".baseline";
        }

        /// <summary>Destructor. Close our DB connection.</summary>
        ~DataStore()
        {
            Disconnect();
        }

        /// <summary>Disconnect from the SQLite database.</summary>
        public void Disconnect()
        {
            if (Connection != null)
            {
                Connection.CloseDatabase();
                Connection = null;
            }
        }

        /// <summary>All simulations have run - write all tables</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("AllCompleted")]
        private void OnAllSimulationsCompleted(object sender, EventArgs e)
        {
            // Open the .db for writing.
            Open(forWriting: true);

            // Get a list of tables for our .db file.
            List<TableToWrite> tablesForOurFile = new List<TableToWrite>();
            lock (TablesToWrite)
            {
                foreach (TableToWrite table in TablesToWrite)
                    if (table.FileName == Filename)
                        tablesForOurFile.Add(table);
            }

            IEnumerable<string> distinctTableNames = tablesForOurFile.Select(t => t.TableName).Distinct();

            // loop through all our tables and write them to the .db
            foreach (string tableName in distinctTableNames)
            {
                // Get a list of tables that have the same name as 'tableName'
                List<TableToWrite> tables = new List<TableToWrite>();
                foreach (TableToWrite table in tablesForOurFile)
                    if (table.TableName == tableName)
                        tables.Add(table);

                WriteTable(tables.ToArray());

            }

            lock (TablesToWrite)
            {
                foreach (TableToWrite table in tablesForOurFile)
                    TablesToWrite.Remove(table);
            }

            // Call each of the child post simulation tools allowing them to run
            RunPostProcessingTools();

            if (AutoExport)
            {
                WriteToTextFiles();
            }

            // Disconnect.
            Disconnect();
        }

        /// <summary>Remove all simulations from the database that don't exist in 'simulationsToKeep'</summary>
        /// <param name="simulationsToKeep">The simulations to keep.</param>
        public void RemoveUnwantedSimulations(Simulations simulationsToKeep)
        {
            Open(forWriting: true);

            string[] simulationNamesToKeep = simulationsToKeep.FindAllSimulationNames();

            // Make sure that the list of simulations in 'simulationsToKeep' are in the 
            // Simulations table.
            string[] simulationNames = this.SimulationNames;
            foreach (string simulationNameToKeep in simulationNamesToKeep)
            {
                if (!StringUtilities.Contains(simulationNames, simulationNameToKeep))
                {
                    RunQueryWithNoReturnData("INSERT INTO [Simulations] (Name) VALUES ('" + simulationNameToKeep + "')");
                }
            }

            // Get a list of simulation IDs that we are to delete.
            List<int> idsToDelete = new List<int>();
            foreach (string simulationNameInDB in SimulationNames)
                if (!simulationNamesToKeep.Contains(simulationNameInDB))
                {
                    idsToDelete.Add(GetSimulationID(simulationNameInDB));
                }

            if (idsToDelete.Count == 0)
                return;

            // create an SQL WHERE clause with all IDs
            string idString = "";
            for (int i = 0; i < idsToDelete.Count; i++)
            {
                if (i > 0)
                    idString += " OR ";
                idString += "ID = " + idsToDelete[i].ToString();
            }

            RunQueryWithNoReturnData("DELETE FROM Simulations WHERE " + idString);

            idString = "";
            for (int i = 0; i < idsToDelete.Count; i++)
            {
                if (i > 0)
                    idString += " OR ";
                idString += "SimulationID = " + idsToDelete[i].ToString();
            }
            foreach (string tableName in TableNames)
            {
                // delete this simulation
                RunQueryWithNoReturnData("DELETE FROM " + tableName + " WHERE " + idString);
            }
        }

        /// <summary>Delete all tables</summary>
        public void DeleteAllTables()
        {
            foreach (string tableName in this.TableNames)
                if (tableName != "Simulations" && tableName != "Messages")
                    DeleteTable(tableName);
        }

        /// <summary>Determine whether a table exists in the database</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public bool TableExists(string tableName)
        {
            Open(forWriting: false);
            
            return (Connection != null) && Connection.ExecuteQueryReturnInt("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='" + 
                                                    tableName + "'", 0) > 0;
        }

        /// <summary>Delete the specified table.</summary>
        /// <param name="tableName">Name of the table.</param>
        public void DeleteTable(string tableName)
        {
            Open(forWriting: true);
            if (TableExists(tableName))
            {
                string cmd = "DROP TABLE " + tableName;
                RunQueryWithNoReturnData(cmd);
            }
        }

        /// <summary>Create a table in the database based on the specified one.</summary>
        /// <param name="simulationName">Name of the simulation.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="table">The table.</param>
        public void WriteTable(string simulationName, string tableName, DataTable table)
        {
            if (DoingPostProcessing)
            {
                TableToWrite tableToWrite = new TableToWrite();
                tableToWrite.SimulationName = simulationName;
                tableToWrite.TableName = tableName;
                tableToWrite.Data = table;

                WriteTable(new TableToWrite[1] { tableToWrite });
            }
            else
            {
                lock (TablesToWrite)
                    TablesToWrite.Add(new TableToWrite()
                    {
                        FileName = Filename,
                        SimulationName = simulationName,
                        TableName = tableName,
                        Data = table
                    });
            }

        }

        /// <summary>Write a message to the DataStore.</summary>
        /// <param name="simulationName">Name of the simulation.</param>
        /// <param name="date">The date.</param>
        /// <param name="componentName">Name of the component.</param>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        public void WriteMessage(string simulationName, DateTime date, string componentName, string message, ErrorLevel type)
        {
            Open(forWriting: true);
            string[] names = new string[] { "ComponentName", "Date", "Message", "MessageType" };

            string sql = string.Format("INSERT INTO Messages (SimulationID, ComponentName, Date, Message, MessageType) " +
                                       "VALUES ({0}, {1}, {2}, {3}, {4})",
                                       new object[] { GetSimulationID(simulationName),
                                                      "\"" + componentName + "\"",
                                                      date.ToString("yyyy-MM-dd"),
                                                      "\"" + message + "\"",
                                                      Convert.ToInt32(type, System.Globalization.CultureInfo.InvariantCulture)});

            RunQueryWithNoReturnData(sql);
        }

        /// <summary>Return a list of simulations names or empty string[]. Never returns null.</summary>
        /// <value>The simulation names.</value>
        public string[] SimulationNames
        {
            get
            {
                if (!TableExists("Simulations"))
                    return new string[0];

                try
                {
                    DataTable table = Connection.ExecuteQuery("SELECT Name FROM Simulations ORDER BY Name");
                    if (table == null)
                        return new string[0];
                    return DataTableUtilities.GetColumnAsStrings(table, "Name");
                }
                catch (SQLiteException )
                {
                    return new string[0];
                }
            }
        }

        /// <summary>Return a list of table names or empty string[]. Never returns null.</summary>
        /// <value>The table names.</value>
        public string[] TableNames
        {
            get
            {
                try
                {
                    Open(forWriting: false);
                    if (Connection != null)
                    {
                        DataTable table = Connection.ExecuteQuery("SELECT * FROM sqlite_master");
                        List<string> tables = new List<string>();
                        if (table != null)
                        {
                            tables.AddRange(DataTableUtilities.GetColumnAsStrings(table, "Name"));

                            // remove the simulations table
                            int simulationsI = tables.IndexOf("Simulations");
                            if (simulationsI != -1)
                                tables.RemoveAt(simulationsI);
                        }
                        return tables.ToArray();
                    }
                    return new string[0];
                }
                catch (SQLiteException )
                {
                    return new string[0];
                }

            }
        }

        /// <summary>
        /// Return all data from the specified simulation and table name. If simulationName = "*"
        /// the all simulation data will be returned.
        /// </summary>
        /// <param name="simulationName">Name of the simulation.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="includeSimulationName">if set to <c>true</c> [include simulation name].</param>
        /// <returns></returns>
        public DataTable GetData(string simulationName, string tableName, bool includeSimulationName = false)
        {
            Open(forWriting: false);
            if (Connection == null || !TableExists("Simulations") || tableName == null || !TableExists(tableName))
                return null;
            try
            {
                string sql;

                if (simulationName == null || simulationName == "*")
                {
                    sql = "SELECT S.Name as SimName, T.* FROM " + tableName + " T" + ", Simulations S " +
                          "WHERE SimulationID = ID";
                }
                else
                {
                    sql = "SELECT * FROM " + tableName;
                    int simulationID = GetSimulationID(simulationName);
                    sql += " WHERE SimulationID = " + simulationID.ToString();
                }

                return Connection.ExecuteQuery(sql);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Return all data from the specified simulation and table name. If simulationName = "*"
        /// the all simulation data will be returned.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public DataTable GetFilteredData(string tableName, string filter)
        {
            return GetFilteredData(tableName, new string[] { "*" }, filter);
        }

        /// <summary>
        /// Return all data from the specified simulation and table name. If simulationName = "*"
        /// the all simulation data will be returned.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldNames">Field names to get data for.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public DataTable GetFilteredData(string tableName, string[] fieldNames, string filter)
        {
            Open(forWriting: false);
            if (Connection == null || !TableExists("Simulations") || tableName == null)
                return null;
            try
            {
                string sql;

                // Create a string of comma separated field names.
                string fieldNameString = string.Empty;
                foreach (string fieldName in fieldNames)
                {
                    if (fieldNameString != string.Empty)
                        fieldNameString += ",";
                    fieldNameString += "[" + fieldName + "]";
                }

                sql = "SELECT S.Name as SimulationName, " + fieldNameString + " FROM " + tableName + " T" + ", Simulations S ";
                sql += "WHERE ID = SimulationID";
                if (filter != null)
                {
                    sql += " AND " + filter;
                }

                return Connection.ExecuteQuery(sql);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Return all data from the specified simulation and table name.</summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        public DataTable RunQuery(string sql)
        {
            Open(forWriting: false);

            return Connection.ExecuteQuery(sql);
        }

        /// <summary>Return all data from the specified simulation and table name.</summary>
        /// <param name="sql">The SQL.</param>
        public void RunQueryWithNoReturnData(string sql)
        {
            Open(forWriting: true);

            Locks[Filename].Aquire();
            try
            {

                Connection.ExecuteNonQuery(sql);

            }
            finally
            {
                Locks[Filename].Release();
            }
        }

        /// <summary>Remove all rows from the specified table for the specified simulation</summary>
        /// <param name="simulationName">Name of the simulation.</param>
        /// <param name="tableName">Name of the table.</param>
        public void DeleteOldContentInTable(string simulationName, string tableName)
        {
            if (TableExists(tableName))
            {
                Open(forWriting: true);
                int id = GetSimulationID(simulationName);
                string sql = "DELETE FROM " + tableName + " WHERE SimulationID = " + id.ToString();
                RunQueryWithNoReturnData(sql);
            }
        }

        /// <summary>Write all outputs to a text file (.csv)</summary>
        public void WriteToTextFiles()
        {
            string originalFileName = Filename;

            try
            {
                // Write the output CSV file.
                Open(forWriting: false);
                WriteAllTables(this, Filename + ".csv");

                // Write the summary file.
                WriteSummaryFile(this, Filename + ".sum");

                // If the baseline file exists then write the .CSV and .SUM files
                string baselineFileName = Filename + ".baseline";
                if (File.Exists(baselineFileName))
                {
                    DataStore baselineDataStore = new DataStore(this, baseline: true);

                    // Write the CSV output file.
                    WriteAllTables(baselineDataStore, baselineFileName + ".csv");

                    // Write the SUM file.
                    WriteSummaryFile(baselineDataStore, baselineFileName + ".sum");

                    baselineDataStore.Disconnect();
                }
            }
            finally
            {
                Filename = originalFileName;
                Disconnect();
            }
        }

        /// <summary>Write a single summary file.</summary>
        /// <param name="dataStore">The data store containing the data</param>
        /// <param name="fileName">The file name to create</param>
        private static void WriteSummaryFile(DataStore dataStore, string fileName)
        {
            StreamWriter report = report = new StreamWriter(fileName);
            foreach (string simulationName in dataStore.SimulationNames)
            {
                Summary.WriteReport(dataStore, simulationName, report, null, outtype: Summary.OutputType.html);
                report.WriteLine();
                report.WriteLine();
                report.WriteLine("############################################################################");
            }
            report.Close();
        }

        /// <summary>Run all post processing tools.</summary>
        public void RunPostProcessingTools()
        {
            try
            {
                // Open the .db for writing.
                Open(forWriting: true);

                DoingPostProcessing = true;
                foreach (IPostSimulationTool tool in Apsim.Children(this, typeof(IPostSimulationTool)))
                    tool.Run(this);
            }
            finally
            {
                DoingPostProcessing = false;
            }
        }

        #region Privates

        /// <summary>Connect to the SQLite database.</summary>
        /// <param name="forWriting">if set to <c>true</c> [for writing].</param>
        /// <exception cref="Models.Core.ApsimXException">Cannot find name of .db file</exception>
        private void Open(bool forWriting)
        {
            lock (Locks)
            {
                if (Filename == null)
                {
                    Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                    if (simulations != null)
                        Filename = Path.ChangeExtension(simulations.FileName, ".db");
                }

                if (Filename != null && 
                    (Connection == null || 
                    (ForWriting == false && forWriting == true)))
                {
                    if (Filename == null)
                        throw new ApsimXException(this, "Cannot find name of .db file");

                    Disconnect();

                    ForWriting = forWriting;

                    if (!Locks.ContainsKey(Filename))
                        Locks.Add(Filename, new DbMutex());

                    Locks[Filename].Aquire();
                    try
                    {
                        if (!File.Exists(Filename))
                        {
                            Connection = new SQLite();
                            Connection.OpenDatabase(Filename, readOnly: false);
                            Connection.ExecuteNonQuery("CREATE TABLE Simulations (ID INTEGER PRIMARY KEY ASC, Name TEXT COLLATE NOCASE)");
                            Connection.ExecuteNonQuery("CREATE TABLE Messages (SimulationID INTEGER, ComponentName TEXT, Date TEXT, Message TEXT, MessageType INTEGER)");

                            if (!forWriting)
                            {
                                Connection.CloseDatabase();
                                Connection.OpenDatabase(Filename, readOnly: !forWriting);
                            }
                        }
                        else
                        {
                            Connection = new SQLite();
                            Connection.OpenDatabase(Filename, readOnly: !forWriting);
                        }

                    }
                    finally
                    {
                        Locks[Filename].Release();
                    }


                }
            }
        }

        /// <summary>Go create a table in the DataStore with the specified field names and types.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="names">The names.</param>
        /// <param name="types">The types.</param>
        private void CreateTable(string tableName, string[] names, Type[] types)
        {
            Open(forWriting: true);

            string cmd = "CREATE TABLE " + tableName + "(";

            for (int i = 0; i < names.Length; i++)
            {
                string columnType = null;
                columnType = GetSQLColumnType(types[i]);

                if (i != 0)
                    cmd += ",";
                cmd += "[" + names[i] + "] " + columnType;
            }
            cmd += ")";
            Locks[Filename].Aquire();

            try
            {
                if (!TableExists(tableName))
                    Connection.ExecuteNonQuery(cmd);
                else
                    AddMissingColumnsToTable(Connection, tableName, names, types);
            }
            finally
            {
                Locks[Filename].Release();
            }
        }

        /// <summary>
        /// Write the specified tables to a single table in the DB. i.e. merge
        /// all columns and rows in all specified tables into a single table.
        /// </summary>
        /// <param name="tables">The tables.</param>
        private void WriteTable(TableToWrite[] tables)
        {
            // Open the .db for writing.
            Open(forWriting: true);

            // What table are we writing?
            string tableName = tables[0].TableName;

            // Get a list of all names and datatypes for each field in this table.
            List<string> names = new List<string>();
            List<Type> types = new List<Type>();
            names.Add("SimulationID");
            types.Add(typeof(int));
            foreach (TableToWrite table in tables)
            {
                if (table.Data != null)
                {
                    // If the table has a simulationname then go find its ID for later
                    if (table.Data.Columns.Contains("SimulationID"))
                    {
                        // do nothing.
                    }
                    else if (table.SimulationName != null)
                        table.SimulationID = GetSimulationID(table.SimulationName);
                    else
                        AddSimulationIDColumnToTable(table.Data);

                    // Go through all columns for this table and add to 'names' and 'types'
                    foreach (DataColumn column in table.Data.Columns)
                    {
                        if (!names.Contains(column.ColumnName) && column.ColumnName != "SimulationName")
                        {
                            names.Add(column.ColumnName);
                            types.Add(column.DataType);
                        }
                    }
                }
            }

            // Create the table.
            CreateTable(tableName, names.ToArray(), types.ToArray());

            // Prepare the insert query sql
            IntPtr query = PrepareInsertIntoTable(Connection, tableName, names.ToArray());

            // Tell SQLite that we're beginning a transaction.
            Connection.ExecuteNonQuery("BEGIN");

            // Go through all tables and write the data.
            foreach (TableToWrite table in tables)
            {
                // Write each row to the .db
                if (table.Data != null)
                {
                    object[] values = new object[names.Count];
                    foreach (DataRow row in table.Data.Rows)
                    {
                        for (int i = 0; i < names.Count; i++)
                        {
                            if (names[i] == "SimulationID" && table.SimulationID != int.MaxValue)
                                values[i] = table.SimulationID;
                            else if (table.Data.Columns.Contains(names[i]))
                                values[i] = row[names[i]];
                        }

                        // Write the row to the .db
                        Connection.BindParametersAndRunQuery(query, values);
                    }
                }
            }

            // tell SQLite we're ending our transaction.
            Connection.ExecuteNonQuery("END");

            // finalise our query.
            Connection.Finalize(query);
        }

        /// <summary>
        /// Return the simulation id (from the simulations table) for the specified name.
        /// If this name doesn't exist in the table then append a new row to the table and
        /// returns its id.
        /// </summary>
        /// <param name="simulationName">Name of the simulation.</param>
        /// <returns></returns>
        private int GetSimulationID(string simulationName)
        {
            if (!TableExists("Simulations"))
                return -1;

            string selectSQL = "SELECT ID FROM Simulations WHERE Name = '" + simulationName + "'";
            int ID = Connection.ExecuteQueryReturnInt(selectSQL, 0);
            if (ID == -1)
            {
                Locks[Filename].Aquire();
                ID = Connection.ExecuteQueryReturnInt(selectSQL, 0);
                if (ID == -1)
                {
                    if (ForWriting == false)
                    {
                        Disconnect();
                        Connection = new SQLite();
                        Connection.OpenDatabase(Filename, readOnly: false);
                        ForWriting = true;
                    }
                    Connection.ExecuteNonQuery("INSERT INTO [Simulations] (Name) VALUES ('" + simulationName + "')");
                    ID = Connection.ExecuteQueryReturnInt("SELECT ID FROM Simulations WHERE Name = '" + simulationName + "'", 0);
                }

                Locks[Filename].Release();
            }
            return ID;
        }

        /// <summary>Create a text report from tables in this data store.</summary>
        /// <param name="dataStore">The data store.</param>
        /// <param name="fileName">Name of the file.</param>
        private static void WriteAllTables(DataStore dataStore, string fileName)
        {
            
            // Write out each table for this simulation.
            foreach (string tableName in dataStore.TableNames)
            {
                if (tableName != "Messages" && tableName != "InitialConditions")
                {
                    DataTable firstRowOfTable = dataStore.RunQuery("SELECT * FROM " + tableName + " LIMIT 1");
                    if (firstRowOfTable != null)
                    {
                        string fieldNamesString = "";
                        for (int i = 1; i < firstRowOfTable.Columns.Count; i++)
                        {
                            if (i > 1)
                                fieldNamesString += ", ";
                            fieldNamesString += "[" + firstRowOfTable.Columns[i].ColumnName + "]";
                        }

                        string sql = String.Format("SELECT Name, {0} FROM Simulations, {1} " +
                                                   "WHERE Simulations.ID = {1}.SimulationID " +
                                                   "ORDER BY Name",
                                                   fieldNamesString, tableName);
                        DataTable data = dataStore.RunQuery(sql);
                        if (data != null && data.Rows.Count > 0)
                        {
                            StreamWriter report = new StreamWriter(Path.ChangeExtension(fileName, "." + tableName + ".csv"));
                            report.Write(DataTableUtilities.DataTableToText(data, 0, ",", true));
                            report.Close();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Go through the specified names and add them to the specified table if they are not
        /// already there.
        /// </summary>
        /// <param name="Connection">The connection.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="names">The names.</param>
        /// <param name="types">The types.</param>
        private static void AddMissingColumnsToTable(SQLite Connection, string tableName, string[] names, Type[] types)
        {
            List<string> columnNames = Connection.GetColumnNames(tableName);

            for (int i = 0; i < names.Length; i++)
            {
                if (!columnNames.Contains(names[i], StringComparer.OrdinalIgnoreCase))
                {
                    string sql = "ALTER TABLE " + tableName + " ADD COLUMN [";
                    sql += names[i] + "] " + GetSQLColumnType(types[i]);
                    Connection.ExecuteNonQuery(sql);    
                }
            }
        }

        /// <summary>Go prepare an insert into query and return the query.</summary>
        /// <param name="Connection">The connection.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="names">The names.</param>
        /// <returns></returns>
        private static IntPtr PrepareInsertIntoTable(SQLite Connection, string tableName, string[] names)
        {
            string Cmd = "INSERT INTO " + tableName + "(";

            for (int i = 0; i < names.Length; i++)
            {
                if (i > 0)
                    Cmd += ",";
                Cmd += "[" + names[i] + "]";
            }
            Cmd += ") VALUES (";

            for (int i = 0; i < names.Length; i++)
            {
                if (i > 0)
                    Cmd += ",";
                Cmd += "?";
            }
            Cmd += ")";
            return Connection.Prepare(Cmd);
        }

        /// <summary>
        /// Using the SimulationName column in the specified 'table', add a
        /// SimulationID column.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <exception cref="Models.Core.ApsimXException">Cannot find Simulations table</exception>
        private void AddSimulationIDColumnToTable(DataTable table)
        {
            DataTable idTable = Connection.ExecuteQuery("SELECT * FROM Simulations");
            if (idTable == null)
                throw new ApsimXException(this, "Cannot find Simulations table");
            List<double> ids = new List<double>();
            ids.AddRange(DataTableUtilities.GetColumnAsDoubles(idTable, "ID"));

            List<string> simulationNames = new List<string>();
            simulationNames.AddRange(DataTableUtilities.GetColumnAsStrings(idTable, "Name"));

            table.Columns.Add("SimulationID", typeof(int)).SetOrdinal(0);
            foreach (DataRow row in table.Rows)
            {
                string simulationName = row["SimulationName"].ToString();
                if (simulationName != null)
                {
                    int index = StringUtilities.IndexOfCaseInsensitive(simulationNames, simulationName);
                    if (index != -1)
                        row["SimulationID"] = ids[index];
                    else
                    {
                        int id = GetSimulationID(simulationName);
                        ids.Add(id);
                        simulationNames.Add(simulationName);
                        row["SimulationID"] = id;
                    }
                }
            }
        }

        /// <summary>Convert the specified type to a SQL type.</summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private static string GetSQLColumnType(Type type)
        {
            if (type == null)
                return "integer";
            else if (type.ToString() == "System.DateTime")
                return "date";
            else if (type.ToString() == "System.Int32")
                return "integer";
            else if (type.ToString() == "System.Single")
                return "real";
            else if (type.ToString() == "System.Double")
                return "real";
            else
                return "char(50)";
        }
        #endregion
    }
}

