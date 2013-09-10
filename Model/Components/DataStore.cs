using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Model.Core;
using System.Data;
using System.Diagnostics;
using System.Reflection;
namespace Model.Components
{
    [ViewName("UserInterface.Views.DataStoreView")]
    [PresenterName("UserInterface.Presenters.DataStorePresenter")]
    public class DataStore
    {
        // make the connection a static so that multiple DataStore models will write
        // to the same database.
        private Utility.SQLite Connection = null;
        private Dictionary<string, IntPtr> TableInsertQueries = new Dictionary<string, IntPtr>();
        private Dictionary<string, int> SimulationIDs = new Dictionary<string, int>();

        // Links
        [Link]
        private Simulations Simulations = null;

        public string Name { get; set; }

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
        private void Connect()
        {
            if (Connection == null)
            {
                string Filename = System.IO.Path.ChangeExtension(Simulations.FileName, ".db");
                if (Filename == null || Filename == "")
                    throw new Exception("The simulations object doesn't have a filename. Cannot open .db");
                Connection = new Utility.SQLite();
                Connection.OpenDatabase(Filename);
            }
        }

        /// <summary>
        /// Disconnect from the SQLite database.
        /// </summary>
        private void Disconnect()
        {
            if (Connection != null)
            {
                foreach (KeyValuePair<string, IntPtr> Table in TableInsertQueries)
                    Connection.Finalize(Table.Value);
                if (Connection.IsOpen)
                {
                    //Connection.ExecuteNonQuery("COMMIT");
                    Connection.CloseDatabase();
                }
                Connection = null;
                TableInsertQueries.Clear();
            }
        }
        
        /// <summary>
        /// Initialise this data store.
        /// </summary>
        public void OnInitialised()
        {
            SimulationIDs.Clear();

            if (Connection != null)
                Disconnect();
            string Filename = System.IO.Path.ChangeExtension(Simulations.FileName, ".db");
            if (File.Exists(Filename))
                File.Delete(Filename);

            Connect();

            Connection.ExecuteNonQuery("PRAGMA synchronous=OFF");
            Connection.ExecuteNonQuery("BEGIN");
            
            // Create a simulations table.
            string[] Names = {"ID", "Name"};
            Type[] Types = { typeof(int), typeof(string) };
            Connection.ExecuteNonQuery("CREATE TABLE Simulations (ID INTEGER PRIMARY KEY ASC, Name TEXT)");

            // Create a properties table.
            Names = new string[] { "ComponentName", "Name", "Value" };
            Types = new Type[] { typeof(string), typeof(string), typeof(string) };
            CreateTable("Properties", Names, Types);

            // Create a Messages table.
            // NB: MessageType values:
            //     1 = Information
            //     2 = Warning
            //     3 = Fatal
            Names = new string[] { "ComponentName", "Date", "Message", "MessageType" };
            Types = new Type[] { typeof(string), typeof(DateTime), typeof(string), typeof(int) };
            CreateTable("Messages", Names, Types);

            Simulations.AllCompleted += OnCompleted;
        }

        /// <summary>
        /// All simulations have been completed. 
        /// </summary>
        private void OnCompleted()
        {
            Connection.ExecuteNonQuery("COMMIT");
            Simulations.AllCompleted -= OnCompleted;
        }

        /// <summary>
        ///  Go create a table in the DataStore with the specified field names and types.
        /// </summary>
        public void CreateTable(string TableName, string[] Names, Type[] Types)
        {
            string Cmd = "CREATE TABLE " + TableName + "([SimulationID] integer";

            for (int i = 0; i < Names.Length; i++)
            {
                string ColumnType = null;
                if (Types[i].ToString() == "System.DateTime")
                    ColumnType = "date";
                else if (Types[i].ToString() == "System.Int32")
                    ColumnType = "integer";
                else if (Types[i].ToString() == "System.Single")
                    ColumnType = "real";
                else if (Types[i].ToString() == "System.Double")
                    ColumnType = "real";
                else
                    ColumnType = "char(50)";

                Cmd += ",[" + Names[i] + "] " + ColumnType;
            }
            Cmd += ")";
            Connection.ExecuteNonQuery(Cmd);

            List<string> AllNames = new List<string>();
            AllNames.Add("SimulationID");
            AllNames.AddRange(Names);
            IntPtr Query = PrepareInsertIntoTable(TableName, AllNames.ToArray());
            TableInsertQueries.Add(TableName, Query);
        }

        /// <summary>
        /// Write a property to the DataStore.
        /// </summary>
        public void WriteProperty(string SimulationName, string Name, string Value)
        {
            StackTrace st = new StackTrace(true);
            MethodInfo CallingMethod = st.GetFrame(1).GetMethod() as MethodInfo;
            string ComponentName = CallingMethod.DeclaringType.FullName;

            WriteToTable("Properties", new object[] { GetSimulationID(SimulationName), 
                                                      ComponentName, Name, Value });
        }

        /// <summary>
        /// Write a message to the DataStore.
        /// </summary>
        public void WriteMessage(string SimulationName, DateTime Date, string Message, CriticalEnum Type = CriticalEnum.Information)
        {
            StackTrace st = new StackTrace(true);
            MethodInfo CallingMethod = st.GetFrame(1).GetMethod() as MethodInfo;
            string ComponentName = CallingMethod.DeclaringType.FullName;

            WriteMessage(SimulationName, Date, ComponentName, Message, Type);
        }

        /// <summary>
        /// Write a message to the DataStore.
        /// </summary>
        public void WriteMessage(string SimulationName, DateTime Date, string ComponentName, string Message, CriticalEnum Type = CriticalEnum.Information)
        {
            WriteToTable("Messages", new object[] { GetSimulationID(SimulationName), 
                                                      ComponentName, Date, Message, Convert.ToInt32(Type) });
        }

        /// <summary>
        /// Write temporal data to the datastore.
        /// </summary>
        public void WriteToTable(string SimulationName, string TableName, object[] Values)
        {
            List<object> AllValues = new List<object>();
            AllValues.Add(GetSimulationID(SimulationName));
            AllValues.AddRange(Values);
            WriteToTable(TableName, AllValues.ToArray());
        }
        
        /// <summary>
        /// Write a row to the specified table in the DataStore using the specified field values.
        /// Values should be in the correct field order.
        /// </summary>
        private void WriteToTable(string TableName, object[] Values)
        {
            if (!TableInsertQueries.ContainsKey(TableName))
                throw new Exception("Cannot find table: " + TableName + " in the DataStore");
            IntPtr Query = TableInsertQueries[TableName];
            Connection.BindParametersAndRunQuery(Query, Values);
        }

        /// <summary>
        /// Return a list of simulations names or empty string[]. Never returns null.
        /// </summary>
        public string[] SimulationNames
        {
            get
            {
                Connect();
                try
                {
                    DataTable Table = Connection.ExecuteQuery("SELECT Name FROM Simulations");
                    return Utility.DataTable.GetColumnAsStrings(Table, "Name");
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
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
                    DataTable Table = Connection.ExecuteQuery("SELECT * FROM sqlite_master");

                    List<string> Tables = new List<string>();
                    Tables.AddRange(Utility.DataTable.GetColumnAsStrings(Table, "Name"));

                    // remove the simulations table
                    int SimulationsI = Tables.IndexOf("Simulations");
                    if (SimulationsI != -1)
                        Tables.RemoveAt(SimulationsI);
                    return Tables.ToArray();
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                    return new string[0];
                }

            }
        }

        /// <summary>
        /// Return all data from the specified simulation and table name.
        /// </summary>
        public DataTable GetData(string SimulationName, string TableName)
        {
            Connect();
            int SimulationID = GetSimulationID(SimulationName);
            string sql = string.Format("SELECT * FROM {0}" +
                                       " WHERE SimulationID = {1}",
                                       TableName, SimulationID);
                       
            return Connection.ExecuteQuery(sql);
        }

        #region Privates


        /// <summary>
        /// Return the simulation id (from the simulations table) for the specified name.
        /// If this name doesn't exist in the table then append a new row to the table and 
        /// returns its id.
        /// </summary>
        private int GetSimulationID(string SimulationName)
        {
            if (SimulationIDs.ContainsKey(SimulationName))
                return SimulationIDs[SimulationName];

            int ID = Connection.ExecuteQueryReturnInt("SELECT ID FROM Simulations WHERE Name = '" + SimulationName + "'", 0);
            if (ID == -1)
            {
                Connection.ExecuteNonQuery("INSERT INTO [Simulations] (Name) VALUES ('" + SimulationName + "')");
                ID = Connection.ExecuteQueryReturnInt("SELECT ID FROM Simulations WHERE Name = '" + SimulationName + "'", 0);
            }
            SimulationIDs.Add(SimulationName, ID);
            return ID;
        }

        /// <summary>
        ///  Go prepare an insert into query and return the query.
        /// </summary>
        private IntPtr PrepareInsertIntoTable(string TableName, string[] Names)
        {
            string Cmd = "INSERT INTO " + TableName + "(";

            for (int i = 0; i < Names.Length; i++)
            {
                if (i > 0)
                    Cmd += ",";
                Cmd += "[" + Names[i] + "]";
            }
            Cmd += ") VALUES (";

            for (int i = 0; i < Names.Length; i++)
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
