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

namespace Models
{
    [Serializable]
    [ViewName("UserInterface.Views.DataStoreView")]
    [PresenterName("UserInterface.Presenters.DataStorePresenter")]
    public class DataStore : ModelCollection
    {
        /// <summary>
        /// A SQLite connection shared between all instances of this DataStore.
        /// </summary>
        [NonSerialized]
        private Utility.SQLite Connection = null;

        /// <summary>
        /// The filename of the SQLite .db
        /// </summary>
        [NonSerialized]
        private string Filename;

        class TableToWrite
        {
            public string SimulationName;
            public int SimulationID = int.MaxValue;
            public string TableName;
            public DataTable Data;
        }

        /// <summary>
        /// A collection of datatables that need writing.
        /// </summary>
        private static List<TableToWrite> TablesToWrite = new List<TableToWrite>();


        /// <summary>
        /// This class encapsulates a simple lock mechanism. It is used by DataStore to 
        /// apply file level locking.
        /// </summary>
        class DbMutex
        {
            private bool Locked = false;

            /// <summary>
            /// Aquire a lock. If already locked then wait a bit.
            /// </summary>
            public void Aquire()
            {
                lock (this)
                {
                    while (Locked)
                        Thread.Sleep(100);
                    Locked = true;
                }
            }

            /// <summary>
            /// Release a lock.
            /// </summary>
            public void Release()
            {
                Locked = false;
            }
        }

        /// <summary>
        /// A static dictionary of locks, one for each filename.
        /// </summary>
        private static Dictionary<string, DbMutex> Locks = new Dictionary<string, DbMutex>();

        /// <summary>
        /// True when the database has been opened as readonly.
        /// </summary>
        private bool ReadOnly = false;

        /// <summary>
        /// An enum that is used to indicate message severity when writing messages to the .db
        /// </summary>
        public enum ErrorLevel { Information, Warning, Error };





        /// <summary>
        /// DataStore has been loaded. Open the database.
        /// </summary>
        public override void OnLoaded()
        {
            // Get a reference to our Simulations parent.,
            Simulations simulations = Parent as Simulations;
            if (simulations != null)
            {
                // Make sure that the .db exists and that it has a Simulations table.
                Filename = Path.ChangeExtension(simulations.FileName, ".db");
                if (File.Exists(Filename))
                {
                    Connect(Filename, false);

                    // Get rid of unwanted simulations.
                    RemoveUnwantedSimulations(simulations);

                    // Disconnect.
                    Disconnect();

                    // Now reconnect as readonly. This is so the GUI can display data from the db
                    Connect(Filename, true);
                }
            }
        }

        /// <summary>
        /// Destructor. Close our DB connection.
        /// </summary>
        ~DataStore()
        {
            Disconnect();
        }

        /// <summary>
        /// Connect to the SQLite database.
        /// </summary>
        public void Connect(string fileName, bool readOnly)
        {
            lock (Locks)
            {
                if (Connection == null)
                {
                    ReadOnly = readOnly;
                    Filename = fileName;
                    if (Filename != null)
                    {
                        Connection = new Utility.SQLite();
                        Connection.OpenDatabase(Filename, readOnly);

                        if (!Locks.ContainsKey(Filename))
                            Locks.Add(Filename, new DbMutex());

                        Locks[Filename].Aquire();
                        try
                        {
                            if (!TableExists(Connection, "Simulations"))
                                Connection.ExecuteNonQuery("CREATE TABLE Simulations (ID INTEGER PRIMARY KEY ASC, Name TEXT)");
                            if (!TableExists(Connection, "Messages"))
                                Connection.ExecuteNonQuery("CREATE TABLE Messages (SimulationID INTEGER, ComponentName TEXT, Date TEXT, Message TEXT, MessageType INTEGER)");
                        }
                        finally
                        {
                            Locks[Filename].Release();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Disconnect from the SQLite database.
        /// </summary>
        public void Disconnect()
        {
            if (Connection != null)
            {
                Connection.CloseDatabase();
                Connection = null;
            }
        }

        /// <summary>
        /// All simulations have run - write all tables
        /// </summary>
        public override void OnAllCompleted()
        {
            Utility.SQLite connection = new Utility.SQLite();
            connection.OpenDatabase(Filename, readOnly:false);

            // loop through all 'TablesToWrite' and write them to the .db
            while (TablesToWrite.Count > 0)
            {
                string tableName = TablesToWrite[0].TableName;

                // Get a list of tables that have the same name as 'tableName'
                List<TableToWrite> tables = new List<TableToWrite>();
                foreach (TableToWrite table in TablesToWrite)
                    if (table.TableName == tableName)
                        tables.Add(table);

                // Get a list of all names and datatypes for each field in this table.
                List<string> names = new List<string>();
                List<Type> types = new List<Type>();
                names.Add("SimulationID");
                types.Add(typeof(int));
                foreach (TableToWrite table in tables)
                {
                    // If the table has a simulationname then go find its ID for later
                    if (TablesToWrite[0].SimulationName != null)
                        table.SimulationID = GetSimulationID(connection, table.SimulationName);
                    else
                        AddSimulationIDColumnToTable(TablesToWrite[0].Data);

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

                // Create the table.
                CreateTable(connection, tableName, names.ToArray(), types.ToArray());

                // Prepare the insert query sql
                IntPtr query = PrepareInsertIntoTable(connection, tableName, names.ToArray());

                // Tell SQLite that we're beginning a transaction.
                connection.ExecuteNonQuery("BEGIN");

                // Go through all tables and write the data.
                foreach (TableToWrite table in tables)
                {
                    // Write each row to the .db
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
                        connection.BindParametersAndRunQuery(query, values);
                    }

                    // Remove the table from 'TablesToWrite' 
                    TablesToWrite.Remove(table);
                }

                // tell SQLite we're ending our transaction.
                connection.ExecuteNonQuery("END");

                // finalise our query.
                connection.Finalize(query);
            }

            connection.CloseDatabase();
        }


        /// <summary>
        /// Remove all unwanted simulations from the database.
        /// </summary>
        public void RemoveUnwantedSimulations(Simulations simulations)
        {
            string[] simulationNamesToKeep = simulations.FindAllSimulationNames();
            foreach (string simulationNameInDB in SimulationNames)
            {
                if (!simulationNamesToKeep.Contains(simulationNameInDB))
                {
                    int id = GetSimulationID(Connection, simulationNameInDB);

                    RunQueryWithNoReturnData("DELETE FROM Simulations WHERE ID = " + id.ToString());
                    foreach (string tableName in TableNames)
                    {
                        // delete this simulation
                        RunQueryWithNoReturnData("DELETE FROM " + tableName + " WHERE SimulationID = " + id.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Determine whether a table exists in the database
        /// </summary>
        public bool TableExists(Utility.SQLite connection, string tableName)
        {
            return (connection != null) && connection.ExecuteQueryReturnInt("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='" + 
                                                    tableName + "'", 0) > 0;
        }

        /// <summary>
        ///  Go create a table in the DataStore with the specified field names and types.
        /// </summary>
        public void CreateTable(Utility.SQLite connection, string tableName, string[] names, Type[] types)
        {
            string cmd = "CREATE TABLE " + tableName +"(";

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
                if (!TableExists(connection, tableName))
                    connection.ExecuteNonQuery(cmd);
                else
                    AddMissingColumnsToTable(connection, tableName, names, types);
            }
            finally
            {
                Locks[Filename].Release();
            }
        }

        /// <summary>
        /// Delete the specified table.
        /// </summary>
        public void DeleteTable(string tableName)
        {
            if (TableExists(Connection, tableName))
            {
                string cmd = "DROP TABLE " + tableName;
                RunQueryWithNoReturnData(cmd);
            }
        }

        /// <summary>
        /// Convert the specified type to a SQL type.
        /// </summary>
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

        /// <summary>
        /// Create a table in the database based on the specified one.
        /// </summary>
        public void WriteTable(string simulationName, string tableName, DataTable table)
        {
            lock (TablesToWrite)
                TablesToWrite.Add(new TableToWrite()
                {
                    SimulationName = simulationName,
                    TableName = tableName,
                    Data = table
                });


        }


        /// <summary>
        /// Using the SimulationName column in the specified 'table', add a
        /// SimulationID column.
        /// </summary>
        /// <param name="table"></param>
        private void AddSimulationIDColumnToTable(DataTable table)
        {
            DataTable idTable = Connection.ExecuteQuery("SELECT * FROM Simulations");
            if (idTable == null)
                throw new ApsimXException(FullPath, "Cannot find Simulations table");
            double[] ids = Utility.DataTable.GetColumnAsDoubles(idTable, "ID");
            string[] simulationNames = Utility.DataTable.GetColumnAsStrings(idTable, "Name");

            table.Columns.Add("SimulationID", typeof(int)).SetOrdinal(0);
            foreach (DataRow row in table.Rows)
            {
                string simulationName = row["SimulationName"].ToString();
                if (simulationName != null)
                {
                    int index = Array.IndexOf(simulationNames, simulationName);
                    if (index != -1)
                        row["SimulationID"] = ids[index];
                }
                    
            }
        }

        /// <summary>
        /// Write a message to the DataStore.
        /// </summary>
        public void WriteMessage(string simulationName, DateTime date, string componentName, string message, ErrorLevel type)
        {

            string[] names = new string[] { "ComponentName", "Date", "Message", "MessageType" };

            string sql = string.Format("INSERT INTO Messages (SimulationID, ComponentName, Date, Message, MessageType) " +
                                       "VALUES ({0}, {1}, {2}, {3}, {4})",
                                       new object[] { GetSimulationID(Connection, simulationName),
                                                      "\"" + componentName + "\"",
                                                      date.ToString("yyyy-MM-dd"),
                                                      "\"" + message + "\"",
                                                      Convert.ToInt32(type, System.Globalization.CultureInfo.InvariantCulture)});

            RunQueryWithNoReturnData(sql);
        }
        
        /// <summary>
        /// Return a list of simulations names or empty string[]. Never returns null.
        /// </summary>
        public string[] SimulationNames
        {
            get
            {
                if (!TableExists(Connection, "Simulations"))
                    return new string[0];

                try
                {
                    DataTable table = Connection.ExecuteQuery("SELECT Name FROM Simulations");
                    if (table == null)
                        return new string[0];
                    return Utility.DataTable.GetColumnAsStrings(table, "Name");
                }
                catch (Utility.SQLiteException )
                {
                    return new string[0];
                }
            }
        }

        /// <summary>
        /// Return a list of table names or empty string[]. Never returns null.
        /// </summary>
        public string[] TableNames
        {
            get
            {
                try
                {
                    if (Connection == null)
                    {
                        Connect(Filename, true);
                    }
                    if (Connection != null)
                    {
                        DataTable table = Connection.ExecuteQuery("SELECT * FROM sqlite_master");
                        List<string> tables = new List<string>();
                        if (table != null)
                        {
                            tables.AddRange(Utility.DataTable.GetColumnAsStrings(table, "Name"));

                            // remove the simulations table
                            int simulationsI = tables.IndexOf("Simulations");
                            if (simulationsI != -1)
                                tables.RemoveAt(simulationsI);
                        }
                        return tables.ToArray();
                    }
                    return new string[0];
                }
                catch (Utility.SQLiteException )
                {
                    return new string[0];
                }

            }
        }

        /// <summary>
        /// Return all data from the specified simulation and table name. If simulationName = "*"
        /// the all simulation data will be returned.
        /// </summary>
        public DataTable GetData(string simulationName, string tableName, bool includeSimulationName = false)
        {
            if (Connection == null || !TableExists(Connection, "Simulations") || tableName == null || !TableExists(Connection, tableName))
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
                    int simulationID = GetSimulationID(Connection, simulationName);
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
        /// Return all data from the specified simulation and table name.
        /// </summary>
        public DataTable RunQuery(string sql)
        {
            return Connection.ExecuteQuery(sql);
        }

        /// <summary>
        /// Return all data from the specified simulation and table name.
        /// </summary>
        public void RunQueryWithNoReturnData(string sql)
        {
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

        /// <summary>
        /// Remove all rows from the specified table for the specified simulation
        /// </summary>
        public void DeleteOldContentInTable(string simulationName, string tableName)
        {
            if (TableExists(Connection, tableName))
            {
                int id = GetSimulationID(Connection, simulationName);
                string sql = "DELETE FROM " + tableName + " WHERE SimulationID = " + id.ToString();
                RunQueryWithNoReturnData(sql);
            }
        }

        /// <summary>
        /// Write all outputs to a text file (.csv)
        /// </summary>
        public void WriteOutputFile()
        {
            string originalFileName = Filename;

            Disconnect();
            
            // Write baseline .csv
            string baselineFileName = Path.ChangeExtension(originalFileName, ".db.baseline");
            if (File.Exists(baselineFileName))
            {
                Connect(baselineFileName, readOnly: true);
                StreamWriter report = new StreamWriter(baselineFileName + ".csv");
                WriteAllTables(report);
                report.Close();
            }

            if (File.Exists(originalFileName))
            {
                Disconnect();

                // Write normal .csv
                Connect(originalFileName, readOnly: true);
                StreamWriter report = new StreamWriter(originalFileName + ".csv");
                WriteAllTables(report);
                report.Close();
            }
        }

        #region Privates

        /// <summary>
        /// Return the simulation id (from the simulations table) for the specified name.
        /// If this name doesn't exist in the table then append a new row to the table and 
        /// returns its id.
        /// </summary>
        private int GetSimulationID(Utility.SQLite connection, string simulationName)
        {
            if (!TableExists(connection, "Simulations"))
                return -1;

            int ID = connection.ExecuteQueryReturnInt("SELECT ID FROM Simulations WHERE Name = '" + simulationName + "'", 0);
            if (ID == -1 && !ReadOnly)
            {
                connection.ExecuteNonQuery("INSERT INTO [Simulations] (Name) VALUES ('" + simulationName + "')");
                ID = connection.ExecuteQueryReturnInt("SELECT ID FROM Simulations WHERE Name = '" + simulationName + "'", 0);
            }
            return ID;
        }

        /// <summary>
        /// Create a text report from tables in this data store.
        /// </summary>
        private void WriteAllTables(StreamWriter report)
        {
            // Write out each table for this simulation.
            foreach (string tableName in TableNames)
            {
                if (tableName != "Messages" && tableName != "Properties")
                {
                    DataTable firstRowOfTable = RunQuery("SELECT * FROM " + tableName + " LIMIT 1");
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
                        DataTable data = RunQuery(sql);
                        if (data != null && data.Rows.Count > 0)
                        {

                            report.WriteLine("TABLE: " + tableName);

                            report.Write(Utility.DataTable.DataTableToCSV(data, 0));
                        }
                    }
                }
            }
            report.WriteLine();
        }

        /// <summary>
        /// Go through the specified names and add them to the specified table if they are not 
        /// already there.
        /// </summary>
        private static void AddMissingColumnsToTable(Utility.SQLite connection, string tableName, string[] names, Type[] types)
        {
            List<string> columnNames = connection.GetColumnNames(tableName);

            for (int i = 0; i < names.Length; i++)
            {
                if (!columnNames.Contains(names[i]))
                {
                    string sql = "ALTER TABLE " + tableName + " ADD COLUMN [";
                    sql += names[i] + "] " + GetSQLColumnType(types[i]);
                    connection.ExecuteNonQuery(sql);    
                }
            }
        }

        /// <summary>
        ///  Go prepare an insert into query and return the query.
        /// </summary>
        private static IntPtr PrepareInsertIntoTable(Utility.SQLite Connection, string tableName, string[] names)
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

        #endregion





    }
}

