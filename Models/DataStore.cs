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
    public class DataStore : Model
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
                string dbFileName = Path.ChangeExtension(simulations.FileName, ".db");
                if (File.Exists(dbFileName))
                {
                    Connect(dbFileName, false);

                    // Get rid of unwanted simulations.
                    RemoveUnwantedSimulations(simulations);

                    // Disconnect.
                    Disconnect();

                    // Now reconnect as readonly. This is so the GUI can display data from the db
                    Connect(dbFileName, true);
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
                    if (Filename == null || Filename.Length == 0)
                        throw new ApsimXException("Filename", "The simulations object doesn't have a filename. Cannot open .db");
                    Connection = new Utility.SQLite();
                    Connection.OpenDatabase(Filename, readOnly);

                    if (!Locks.ContainsKey(Filename))
                        Locks.Add(Filename, new DbMutex());

                    Locks[Filename].Aquire();
                    if (!TableExists("Simulations"))
                        Connection.ExecuteNonQuery("CREATE TABLE Simulations (ID INTEGER PRIMARY KEY ASC, Name TEXT)");
                    Locks[Filename].Release();
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
        /// Remove all unwanted simulations from the database.
        /// </summary>
        public void RemoveUnwantedSimulations(Simulations simulations)
        {
            string[] simulationNamesToKeep = simulations.FindAllSimulationNames();
            foreach (string simulationNameInDB in SimulationNames)
            {
                if (!simulationNamesToKeep.Contains(simulationNameInDB))
                {
                    int id = GetSimulationID(simulationNameInDB);

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
        public bool TableExists(string tableName)
        {
            return (Connection != null) && Connection.ExecuteQueryReturnInt("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='" + 
                                                    tableName + "'", 0) > 0;
        }

        /// <summary>
        ///  Go create a table in the DataStore with the specified field names and types.
        /// </summary>
        public void CreateTable(string tableName, string[] names, Type[] types)
        {
            string cmd = "CREATE TABLE " + tableName + "([SimulationID] integer";

            for (int i = 0; i < names.Length; i++)
            {
                string columnType = null;
                columnType = GetSQLColumnType(types[i]);

                cmd += ",[" + names[i] + "] " + columnType;
            }
            cmd += ")";
            Locks[Filename].Aquire();
            if (!TableExists(tableName))
                Connection.ExecuteNonQuery(cmd);
            else
                AddMissingColumnsToTable(tableName, names, types);
            Locks[Filename].Release(); 
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
            // Add all columns.
            List<string> names = new List<string>();
            List<Type> types = new List<Type>();
            foreach (DataColumn column in table.Columns)
            {
                names.Add(column.ColumnName);
                types.Add(column.DataType);
            }

            // Create the table.
            CreateTable(tableName, names.ToArray(), types.ToArray());

            Locks[Filename].Aquire(); 

            // prepare the sql
            names.Insert(0, "SimulationID");
            IntPtr query = PrepareInsertIntoTable(tableName, names.ToArray());

            // tell SQLite that we're beginning a transaction.
            Connection.ExecuteNonQuery("BEGIN");

            // Add all rows.
            object[] values = new object[table.Columns.Count + 1];
            values[0] = GetSimulationID(simulationName);
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                    values[i + 1] = row[i];
                Connection.BindParametersAndRunQuery(query, values);
            }

            // tell SQLite we're ending our transaction.
            Connection.ExecuteNonQuery("END");

            // finalise our query.
            Connection.Finalize(query);

            Locks[Filename].Release(); 
        }

        /// <summary>
        /// Write a message to the DataStore.
        /// </summary>
        public void WriteMessage(string simulationName, DateTime date, string componentName, string message, ErrorLevel type)
        {

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
        
        /// <summary>
        /// Return a list of simulations names or empty string[]. Never returns null.
        /// </summary>
        public string[] SimulationNames
        {
            get
            {
                if (!TableExists("Simulations"))
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
                catch (Utility.SQLiteException )
                {
                    return new string[0];
                }

            }
        }

        /// <summary>
        /// Return all data from the specified simulation and table name. If simulatinName = "*"
        /// the all simulation data will be returned.
        /// </summary>
        public DataTable GetData(string simulationName, string tableName)
        {
            if (Connection == null || !TableExists("Simulations"))
                return null;
            try
            {
                string sql = "SELECT * FROM " + tableName;
                if (simulationName != "*")
                {
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

            Connection.ExecuteNonQuery(sql);

            Locks[Filename].Release(); 
        }

        /// <summary>
        /// Remove all rows from the specified table for the specified simulation
        /// </summary>
        public void DeleteOldContentInTable(string simulationName, string tableName)
        {
            if (TableExists(tableName))
            {
                int id = GetSimulationID(simulationName);
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
            
            // Write baseline .csv
            string baselineFileName = Path.ChangeExtension(Filename, ".db.baseline");
            Connect(baselineFileName, readOnly: true);
            StreamWriter report = new StreamWriter(Filename + ".csv");
            WriteAllTables(report);
            report.Close();
            Disconnect();
            
            // Write normal .csv
            Connect(originalFileName, readOnly: true);
            report = new StreamWriter(originalFileName + ".csv");
            WriteAllTables(report);
            report.Close();
            Disconnect();
        }

        #region Privates

        /// <summary>
        /// Return the simulation id (from the simulations table) for the specified name.
        /// If this name doesn't exist in the table then append a new row to the table and 
        /// returns its id.
        /// </summary>
        private int GetSimulationID(string simulationName)
        {
            if (!TableExists("Simulations"))
                return -1;

            int ID = Connection.ExecuteQueryReturnInt("SELECT ID FROM Simulations WHERE Name = '" + simulationName + "'", 0);
            if (ID == -1 && !ReadOnly)
            {
                Connection.ExecuteNonQuery("INSERT INTO [Simulations] (Name) VALUES ('" + simulationName + "')");
                ID = Connection.ExecuteQueryReturnInt("SELECT ID FROM Simulations WHERE Name = '" + simulationName + "'", 0);
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
                                                   "WHERE Simulations.ID = {1}.SimulationID",
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
        private void AddMissingColumnsToTable(string tableName, string[] names, Type[] types)
        {
            List<string> columnNames = Connection.GetColumnNames(tableName);

            for (int i = 0; i < names.Length; i++)
            {
                if (!columnNames.Contains(names[i]))
                {
                    string sql = "ALTER TABLE " + tableName + " ADD COLUMN [";
                    sql += names[i] + "] " + GetSQLColumnType(types[i]);
                    Connection.ExecuteNonQuery(sql);    
                }
            }
        }

        /// <summary>
        ///  Go prepare an insert into query and return the query.
        /// </summary>
        private IntPtr PrepareInsertIntoTable(string tableName, string[] names)
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

