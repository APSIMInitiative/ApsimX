using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Models.Core;
using System.Data;
using System.Diagnostics;
using System.Reflection;

namespace Models
{
    [Serializable]
    [ViewName("UserInterface.Views.DataStoreView")]
    [PresenterName("UserInterface.Presenters.DataStorePresenter")]
    public class DataStore : Model
    {
        [NonSerialized]
        private Utility.SQLite Connection = null;
        [NonSerialized]
        private Dictionary<string, IntPtr> TableInsertQueries = new Dictionary<string, IntPtr>();
        [NonSerialized]
        private Dictionary<string, int> SimulationIDs = new Dictionary<string, int>();
        private string Filename;

        public enum ErrorLevel { Information, Warning, Error };

        // Parameters
        public bool AutoCreateReport { get; set; }

        // Links
        [Link]
        private Simulations Simulations = null;

        /// <summary>
        /// Destructor. Close our DB connection.
        /// </summary>
        ~DataStore()
        {
            Disconnect();
        }

        public void Connect()
        {
            if (Connection == null)
            {
                Models.Core.Model RootModel = this;
                while (RootModel.Parent != null)
                    RootModel = RootModel.Parent;

                if (RootModel != null && RootModel is Models.Core.Simulations)
                {
                    Models.Core.Simulations simulations = RootModel as Models.Core.Simulations;
                    Connect(Path.ChangeExtension(simulations.FileName, ".db"));
                }
                else
                    throw new ApsimXException("DataStore", "Cannot determine the filename of the datastore.");
            }
        }


        /// <summary>
        /// Connect to the SQLite database.
        /// </summary>
        public void Connect(string fileName)
        {
            if (Connection == null)
            {
                SimulationIDs = new Dictionary<string, int>();
                TableInsertQueries = new Dictionary<string, IntPtr>();

                Filename = fileName;
                if (Filename == null || Filename.Length == 0)
                    throw new ApsimXException("Filename", "The simulations object doesn't have a filename. Cannot open .db");
                Connection = new Utility.SQLite();
                Connection.OpenDatabase(Filename);

                // Connection.ExecuteNonQuery("PRAGMA synchronous=OFF");
                // Connection.ExecuteNonQuery("BEGIN");

                // Create a simulations table if not present.
                if (!TableExists("Simulations"))
                    Connection.ExecuteNonQuery("CREATE TABLE Simulations (ID INTEGER PRIMARY KEY ASC, Name TEXT)");

                // Create a Messages table if not present.
                // NB: MessageType values:
                //     1 = Information
                //     2 = Warning
                //     3 = Fatal
                string[] Names = new string[] { "ComponentName", "Date", "Message", "MessageType" };
                Type[] Types = new Type[] { typeof(string), typeof(DateTime), typeof(string), typeof(int) };
                CreateTable("Messages", Names, Types);
            }
        }

        /// <summary>
        /// Disconnect from the SQLite database.
        /// </summary>
        public void Disconnect()
        {
            if (Connection != null)
            {
                foreach (KeyValuePair<string, IntPtr> Table in TableInsertQueries)
                    Connection.Finalize(Table.Value);
                Connection = null;
                SimulationIDs = null;
                TableInsertQueries = null;


            }
        }

        /// <summary>
        /// Initialise this data store.
        /// </summary>
        [EventSubscribe("AllCommencing")]
        private void OnAllCommencing(object sender, EventArgs e)
        {
            Connect(Path.ChangeExtension(Simulations.FileName, ".db"));
            RemoveUnwantedSimulations();
            Disconnect();
        }

        /// <summary>
        /// All simulations have been completed. 
        /// </summary>
        [EventSubscribe("AllCompleted")]
        private void OnAllCompleted(object sender, EventArgs e)
        {
            if (AutoCreateReport)
                WriteOutputFile();
        }

        /// <summary>
        /// Remove all unwanted simulations from the database.
        /// </summary>
        public void RemoveUnwantedSimulations()
        {
            string[] simulationNamesToKeep = Simulations.FindAllSimulationNames();
            foreach (string simulationNameInDB in SimulationNames)
            {
                if (!simulationNamesToKeep.Contains(simulationNameInDB))
                {
                    int id = GetSimulationID(simulationNameInDB);

                    Connection.ExecuteNonQuery("DELETE FROM Simulations WHERE ID = " + id.ToString());
                    foreach (string tableName in TableNames)
                    {
                        // delete this simulation
                        Connection.ExecuteNonQuery("DELETE FROM " + tableName + " WHERE SimulationID = " + id.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Determine whether a table exists in the database
        /// </summary>
        /// <param name="table_name">Name of the table</param>
        /// <returns>True if the table is present</returns>
        public bool TableExists(string table_name)
        {
            return Connection.ExecuteQueryReturnInt("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='" + 
                                                    table_name + "'", 0) > 0;
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
                if (types[i] == null)
                    columnType = "integer";
                else if (types[i].ToString() == "System.DateTime")
                    columnType = "date";
                else if (types[i].ToString() == "System.Int32")
                    columnType = "integer";
                else if (types[i].ToString() == "System.Single")
                    columnType = "real";
                else if (types[i].ToString() == "System.Double")
                    columnType = "real";
                else
                    columnType = "char(50)";

                cmd += ",[" + names[i] + "] " + columnType;
            }
            cmd += ")";
            if (!TableExists(tableName))
                Connection.ExecuteNonQuery(cmd);

            List<string> allNames = new List<string>();
            allNames.Add("SimulationID");
            allNames.AddRange(names);
            IntPtr query = PrepareInsertIntoTable(tableName, allNames.ToArray());
            if (!TableInsertQueries.ContainsKey(tableName))
                TableInsertQueries.Add(tableName, query);
        }

        /// <summary>
        /// Create a table in the database based on the specified one.
        /// </summary>
        public void CreateTable(string simulationName, string tableName, DataTable table)
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

            // Add all rows.
            object[] values = new object[table.Columns.Count];
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                    values[i] = row[i];
                WriteToTable(simulationName, tableName, values);
            }
        }

        /// <summary>
        /// Write a message to the DataStore.
        /// </summary>
        public void WriteMessage(string componentFullPath, string simulationName, DateTime date, string message, ErrorLevel type)
        {
            WriteMessage(simulationName, date, componentFullPath, message, type);
        }

        /// <summary>
        /// Write a message to the DataStore.
        /// </summary>
        public void WriteMessage(string simulationName, DateTime date, string componentName, string message, ErrorLevel type)
        {
            Connect();
            WriteToTable("Messages", new object[] { GetSimulationID(simulationName), 
                                                      componentName, date, message, Convert.ToInt32(type, System.Globalization.CultureInfo.InvariantCulture) });
        }

        /// <summary>
        /// Write temporal data to the datastore.
        /// </summary>
        public void WriteToTable(string simulationName, string tableName, object[] values)
        {
            List<object> allValues = new List<object>();
            allValues.Add(GetSimulationID(simulationName));
            allValues.AddRange(values);
            WriteToTable(tableName, allValues.ToArray());
        }
        
        /// <summary>
        /// Write a row to the specified table in the DataStore using the specified field values.
        /// Values should be in the correct field order.
        /// </summary>
        private void WriteToTable(string tableName, object[] values)
        {
            if (!TableInsertQueries.ContainsKey(tableName))
                throw new ApsimXException(FullPath, "Cannot find table: " + tableName + " in the DataStore");
            IntPtr query = TableInsertQueries[tableName];
            Connection.BindParametersAndRunQuery(query, values);
        }

        /// <summary>
        /// Return a list of simulations names or empty string[]. Never returns null.
        /// </summary>
        public string[] SimulationNames
        {
            get
            {
                Connect();
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
                    Connect();
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
            Connect();
            if (!TableExists("Simulations"))
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
            Connect();
            return Connection.ExecuteQuery(sql);
        }

        /// <summary>
        /// Return all data from the specified simulation and table name.
        /// </summary>
        public void RunQueryWithNoReturnData(string sql)
        {
            Connect();
            Connection.ExecuteNonQuery(sql);
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
            Connect(baselineFileName);
            StreamWriter report = new StreamWriter(Filename + ".csv");
            WriteAllTables(report);
            report.Close();
            Disconnect();
            
            // Write normal .csv
            Connect(originalFileName);
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
            if (SimulationIDs.ContainsKey(simulationName))
                return SimulationIDs[simulationName];

            if (!TableExists("Simulations"))
                return -1;

            int ID = Connection.ExecuteQueryReturnInt("SELECT ID FROM Simulations WHERE Name = '" + simulationName + "'", 0);
            if (ID == -1)
            {
                Connection.ExecuteNonQuery("INSERT INTO [Simulations] (Name) VALUES ('" + simulationName + "')");
                ID = Connection.ExecuteQueryReturnInt("SELECT ID FROM Simulations WHERE Name = '" + simulationName + "'", 0);
            }
            SimulationIDs.Add(simulationName, ID);
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
                    if (data.Rows.Count > 0)
                    {

                        report.WriteLine("TABLE: " + tableName);

                        report.Write(Utility.DataTable.DataTableToCSV(data, 0));
                    }
                }
            }
            report.WriteLine();
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

