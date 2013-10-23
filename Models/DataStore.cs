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
    [ViewName("UserInterface.Views.DataStoreView")]
    [PresenterName("UserInterface.Presenters.DataStorePresenter")]
    public class DataStore : Model
    {
        private const string divider = "------------------------------------------------------------------------------";

        private Utility.SQLite Connection = null;
        private Dictionary<string, IntPtr> TableInsertQueries = new Dictionary<string, IntPtr>();
        private Dictionary<string, int> SimulationIDs = new Dictionary<string, int>();

        public enum ErrorLevel { Information, Warning, Error };

        /// <summary>
        /// A property that controls auto creation of a text report.
        /// </summary>
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

        /// <summary>
        /// Connect to the SQLite database.
        /// </summary>
        private void Connect()
        {
            if (Connection == null)
            {
                string Filename = System.IO.Path.ChangeExtension(Simulations.FileName, ".db");
                if (Filename == null || Filename.Length == 0)
                    throw new ApsimXException("Filename", "The simulations object doesn't have a filename. Cannot open .db");
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
        [EventSubscribe("AllCommencing")]
        private void OnAllCommencing(object sender, EventArgs e)
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
        }

        /// <summary>
        /// All simulations have been completed. 
        /// </summary>
        [EventSubscribe("AllCompleted")]
        private void OnAllCompleted(object sender, EventArgs e)
        {
            Connection.ExecuteNonQuery("COMMIT");
            if (AutoCreateReport)
                CreateReport();
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
                if (types[i].ToString() == "System.DateTime")
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
            Connection.ExecuteNonQuery(cmd);

            List<string> allNames = new List<string>();
            allNames.Add("SimulationID");
            allNames.AddRange(names);
            IntPtr query = PrepareInsertIntoTable(tableName, allNames.ToArray());
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
        /// Write a property to the DataStore.
        /// </summary>
        public void WriteProperty(string simulationName, string name, string value)
        {
            StackTrace st = new StackTrace(true);
            MethodInfo callingMethod = st.GetFrame(1).GetMethod() as MethodInfo;
            string componentName = callingMethod.DeclaringType.FullName;

            WriteToTable("Properties", new object[] { GetSimulationID(simulationName), 
                                                      componentName, name, value });
        }

        /// <summary>
        /// Write a message to the DataStore.
        /// </summary>
        public void WriteMessage(string simulationName, DateTime date, string message, ErrorLevel type)
        {
            StackTrace st = new StackTrace(true);
            MethodInfo callingMethod = st.GetFrame(1).GetMethod() as MethodInfo;
            string componentName = callingMethod.DeclaringType.FullName;

            WriteMessage(simulationName, date, componentName, message, type);
        }

        /// <summary>
        /// Write a message to the DataStore.
        /// </summary>
        public void WriteMessage(string simulationName, DateTime date, string componentName, string message, ErrorLevel type)
        {
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
                try
                {
                    DataTable table = Connection.ExecuteQuery("SELECT Name FROM Simulations");
                    return Utility.DataTable.GetColumnAsStrings(table, "Name");
                }
                catch (Utility.SQLiteException err)
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
                catch (Utility.SQLiteException err)
                {
                    Console.WriteLine(err.Message);
                    return new string[0];
                }

            }
        }

        /// <summary>
        /// Return all data from the specified simulation and table name.
        /// </summary>
        public DataTable GetData(string simulationName, string tableName)
        {
            Connect();
            int simulationID = GetSimulationID(simulationName);
            string sql = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                       "SELECT * FROM {0} WHERE SimulationID = {1}",
                                       new object[] {tableName, simulationID});
                       
            return Connection.ExecuteQuery(sql);
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
        /// Create a text report from tables in this data store.
        /// </summary>
        public void CreateReport()
        {

            StreamWriter report = new StreamWriter(Path.ChangeExtension(Simulations.FileName, ".csv"));

            foreach (string simulationName in SimulationNames)
            {
                report.WriteLine("SIMULATION: " + simulationName);

                // Write out all summary messages for this simulation.
                WriteSummary(report, simulationName, false);
            }

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
            report.WriteLine(divider);
            report.WriteLine();
            report.Close();
        }

        /// <summary>
        /// Write out summary information
        /// </summary>
        public void WriteSummary(TextWriter report, string simulationName, bool html)
        {
            if (html)
            {
                report.WriteLine("<!DOCTYPE html>");
                report.WriteLine("<html>");
                report.WriteLine("<body>");
            }

            // Write out all properties.
            WriteHeading(report, "Properties:", html);
            DataTable propertyTable = GetPropertyTable(simulationName);
            WriteTable(report, propertyTable, html);
            
            // Write out all messages.
            WriteHeading(report, "Simulation log:", html);
            DataTable messageTable = GetMessageTable(simulationName);
            WriteTable(report, messageTable, html);

            if (html)
            {
                report.WriteLine("</body>");
                report.WriteLine("</html>");
            }
        }

        /// <summary>
        /// Create a message table ready for writing.
        /// </summary>
        private DataTable GetMessageTable(string simulationName)
        {
            DataTable messageTable = new DataTable();
            DataTable messages = GetData(simulationName, "Messages");
            if (messages.Rows.Count > 0)
            {
                messageTable.Columns.Add("Date", typeof(string));
                messageTable.Columns.Add("Model", typeof(string));
                messageTable.Columns.Add("Message", typeof(string));
                foreach (DataRow row in messages.Rows)
                {
                    string modelName = (string)row[1];
                    DateTime date = (DateTime)row[2];
                    string message = (string)row[3];
                    ErrorLevel errorLevel = (ErrorLevel)Enum.Parse(typeof(ErrorLevel), row[4].ToString());

                    if (errorLevel == ErrorLevel.Error)
                        message = "FATAL ERROR: " + message;
                    else if (errorLevel == ErrorLevel.Warning)
                        message = "WARNING: " + message;

                    messageTable.Rows.Add(new object[] { date.ToString("yyyy-MM-dd"), modelName, message });
                }
            }
            return messageTable;
        }

        /// <summary>
        /// Get a table of all properties for all models in the specified simulation.
        /// </summary>
        private DataTable GetPropertyTable(string simulationName)
        {
            DataTable propertyTable = new DataTable();

            Simulation simulation = Simulations.Get(simulationName) as Simulation;
            if (simulation != null)
            {
                propertyTable.Columns.Add("Model name", typeof(string));
                propertyTable.Columns.Add("Property name", typeof(string));
                propertyTable.Columns.Add("Value", typeof(object));

                Model[] models = simulation.FindAll();
                foreach (Model model in models)
                {
                    string modelName = model.FullPath;

                    PropertyInfo[] properties = model.Properties();
                    foreach (PropertyInfo property in properties)
                    {
                        if (property.Name != "Name" && property.Name != "Parent")
                        {
                            object value = property.GetValue(model, null);
                            if (value != null)
                            {
                                string units = null;
                                Units unitsAttribute = Utility.Reflection.GetAttribute(property, typeof(Units), false) as Units;
                                if (unitsAttribute != null)
                                    units = "(" + unitsAttribute.UnitsString + ")";

                                string propertyName = property.Name;
                                if (units != null)
                                    propertyName += " " + units;

                                if (value.GetType().IsArray)
                                {
                                    Array array = value as Array;
                                    if (array != null && array.Length > 0)
                                    {
                                        propertyTable.Rows.Add(new object[] { modelName, propertyName, array.GetValue(0) });
                                        for (int arrayIndex = 1; arrayIndex < array.Length; arrayIndex++)
                                        {
                                            propertyTable.Rows.Add(new object[] { "", "", array.GetValue(arrayIndex) });
                                        }
                                    }
                                }
                                else
                                    propertyTable.Rows.Add(new object[] { modelName, propertyName, value });
                            }
                        }
                    }
                }
            }
            return propertyTable;
        }

        private string FormatValue(object value)
        {
            if (value is double || value is float)
                return String.Format("{0:N3}", value);
            else if (value is DateTime)
                return ((DateTime)value).ToString("yyyy-mm-dd");
            else
                return value.ToString();
        }

        private void WriteHeading(TextWriter writer, string heading, bool html)
        {
            if (html)
                writer.WriteLine("<h1>" + heading + "</h1>");
            else
                writer.WriteLine(heading.ToUpper());
        }

        private void WriteTable(TextWriter report, DataTable table, bool html)
        {
            if (html)
            {
                report.WriteLine("<table border = \"0\">");
                foreach (DataRow row in table.Rows)
                {
                    report.WriteLine("<tr>");
                    foreach (DataColumn col in table.Columns)
                    {
                        report.Write("<td>");
                        report.Write(FormatValue(row[col]));
                        report.WriteLine("</td>");
                    }
                    report.WriteLine("</tr>");
                }
                report.WriteLine("</table>");
            }
            else
            {
                report.WriteLine(Utility.DataTable.DataTableToCSV(table, 0));
                report.WriteLine(divider);
            }

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
