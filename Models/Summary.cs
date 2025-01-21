using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Documentation.Extensions;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Logging;
using Models.Storage;

namespace Models
{

    /// <summary>
    /// This model collects the simulation initial conditions and stores into the DataStore.
    /// It also provides an API for writing messages to the DataStore.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.SummaryView")]
    [PresenterName("UserInterface.Presenters.SummaryPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    public class Summary : Model, ISummary
    {
        [NonSerialized]
        private DataTable messages;

        [NonSerialized]
        private bool afterCompleted = false;

        /// <summary>A link to a storage service</summary>
        [Link]
        private IDataStore storage = null;

        /// <summary>A link to the clock in the simulation</summary>
        [Link]
        private IClock clock = null;

        /// <summary>A link to the parent simulation</summary>
        [Link]
        private Simulation simulation = null;

        /// <summary>
        /// Enumeration used to indicate the format of the output string
        /// </summary>
        public enum OutputType
        {
            /// <summary>
            /// Plain ASCII text
            /// </summary>
            plain,

            /// <summary>
            /// HTML format
            /// </summary>
            html,

            /// <summary>
            /// Markdown format
            /// </summary>
            Markdown
        }

        /// <summary>This setting controls what type of messages will be captured by the summary.</summary>
        public MessageType Verbosity { get; set; } = MessageType.All;

        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs args)
        {
            messages = null;
            afterCompleted = false;
        }

        /// <summary>When the simulation is completed, we need to write all the messages to the datastore.</summary>
        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs args)
        {
            WriteMessagesToDataStore();
            afterCompleted = true;
        }

        /// <summary>Event handler to create initialise</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("DoInitialSummary")]
        private void OnDoInitialSummary(object sender, EventArgs e)
        {
            CreateInitialConditionsTable();
        }

        /// <summary>Write a message to the summary</summary>
        /// <param name="author">The model writing the message</param>
        /// <param name="message">The message to write</param>
        /// <param name="messageType">Message output/verbosity level.</param>
        public void WriteMessage(IModel author, string message, MessageType messageType)
        {
            if (Verbosity >= messageType)
            {
                if (storage == null)
                {
                    if (author == null)
                        throw new Exception("No datastore is available!");
                    else
                        throw new ApsimXException(author, "No datastore is available!");
                }

                //set up our messages table if it is null. It will always be null at the start of the simulation.
                if (messages == null)
                {
                    messages = new DataTable("_Messages");
                    messages.Columns.Add("SimulationName", typeof(string));
                    messages.Columns.Add("ComponentName", typeof(string));
                    messages.Columns.Add("Date", typeof(DateTime));
                    messages.Columns.Add("Message", typeof(string));
                    messages.Columns.Add("MessageType", typeof(int));
                }

                // Remove the path of the simulation within the .apsimx file.
                string relativeModelPath = null;
                if (author != null)
                    relativeModelPath = author.FullPath.Replace($"{simulation.FullPath}.", string.Empty);

                DataRow row = messages.NewRow();
                row[0] = simulation.Name;
                row[1] = relativeModelPath;
                row[2] = clock.Today;
                row[3] = message;
                row[4] = (int)messageType;
                messages.Rows.Add(row);
                
                //This message has come in after the simulation has completed, potentially due to a late event or mis-ordered event
                if (afterCompleted)
                    WriteMessagesToDataStore();
            }
        }

        /// <summary>Writes all the stored messages to the datastore. At the end of simulation to fill up the datastore.</summary>
        public void WriteMessagesToDataStore() {
            if (messages != null) {
                storage?.Writer?.WriteTable(messages, false);
                messages = null;
            }
        }

        /// <summary>
        /// Create an initial conditions table in the DataStore.
        /// </summary>
        private void CreateInitialConditionsTable()
        {
            var initConditions = new DataTable("_InitialConditions");
            initConditions.Columns.Add("SimulationName", typeof(string));
            initConditions.Columns.Add("ModelPath", typeof(string));
            initConditions.Columns.Add("Name", typeof(string));
            initConditions.Columns.Add("Description", typeof(string));
            initConditions.Columns.Add("DataType", typeof(string));
            initConditions.Columns.Add("Units", typeof(string));
            initConditions.Columns.Add("DisplayFormat", typeof(string));
            initConditions.Columns.Add("Total", typeof(int));
            initConditions.Columns.Add("Value", typeof(string));

            string simulationPath = simulation.FullPath;

            var row = initConditions.NewRow();
            row.ItemArray = new object[] { simulation.Name, simulationPath, "Simulation name", "Simulation name", "String", string.Empty, string.Empty, 0, simulation.Name };
            initConditions.Rows.Add(row);

            row = initConditions.NewRow();
            row.ItemArray = new object[] { simulation.Name, simulationPath, "APSIM version", "APSIM version", "String", string.Empty, string.Empty, 0, Simulations.GetApsimVersion() };
            initConditions.Rows.Add(row);

            row = initConditions.NewRow();
            row.ItemArray = new object[] { simulation.Name, simulationPath, "Run on", "Run on", "String", string.Empty, string.Empty, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            initConditions.Rows.Add(row);

            // Get all model properties and store in 'initialConditionsTable'
            foreach (Model model in simulation.FindAllInScope())
            {
                string thisRelativeModelPath = model.FullPath.Replace(simulationPath + ".", string.Empty);

                var properties = new List<Tuple<string, VariableProperty>>();
                FindAllProperties(model, properties);
                foreach (var tuple in properties)
                {
                    string propertyValue = tuple.Item2.ValueAsString();
                    if (propertyValue != string.Empty)
                    {
                        if (propertyValue != null && tuple.Item2.DataType == typeof(DateTime))
                            propertyValue = ((DateTime)tuple.Item2.Value).ToString("yyyy-MM-dd HH:mm:ss");

                        int total;
                        if (double.IsNaN(tuple.Item2.Total))
                            total = 0;
                        else
                            total = 1;

                        if (tuple.Item2.Units == null)
                            tuple.Item2.Units = string.Empty;

                        row = initConditions.NewRow();
                        row.ItemArray = new object[] { simulation.Name, thisRelativeModelPath, tuple.Item1, tuple.Item2.Description, tuple.Item2.DataType.Name, tuple.Item2.Units, tuple.Item2.Format, total, propertyValue };
                        initConditions.Rows.Add(row);
                    }
                }
            }

            // The initial conditions table will be automatically cleaned prior to a simulation
            // run, so we don't need to delete existing data in this call to WriteTable().
            storage.Writer.WriteTable(initConditions, false);
        }

        #region Static summary report generation

        /// <summary>
        /// Write a single summary file for all simulations.
        /// </summary>
        /// <param name="storage">The storage where the summary data is stored</param>
        /// <param name="fileName">The file name to write</param>
        /// <param name="darkTheme">Whether or not the dark theme should be used.</param>
        public static void WriteSummaryToTextFiles(IDataStore storage, string fileName, bool darkTheme)
        {
            using (StreamWriter report = new StreamWriter(fileName))
            {
                //get list of simulations in alphabetical order
                List<string> names = storage.Reader.SimulationNames;
                names.Sort();

                foreach (string simulationName in names)
                {
                    Summary.WriteReport(storage, simulationName, report, null, outtype: Summary.OutputType.html, darkTheme: darkTheme, true, true, true, true);
                    report.WriteLine();
                    report.WriteLine();
                    report.WriteLine("############################################################################");
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="simulationName"></param>
        public IEnumerable<Message> GetMessages(string simulationName)
        {
            IDataStore storage = this.storage ?? FindInScope<IDataStore>();
            if (storage == null)
                yield break;
            DataTable messages = storage.Reader.GetData("_Messages", simulationNames: simulationName.ToEnumerable());
            if (messages == null)
                yield break;

            string simulationPath = FindInScope<Simulation>(simulationName)?.FullPath;
            foreach (DataRow row in messages.Rows)
            {
                DateTime date = (DateTime)row["Date"];
                string text = row["Message"]?.ToString();
                string relativePath = row["ComponentName"]?.ToString();
                IModel model = simulationPath == null ? FindInScope(relativePath) : FindByPath(simulationPath + "." + relativePath)?.Value as IModel;
                if (!Enum.TryParse<MessageType>(row["MessageType"]?.ToString(), out MessageType severity))
                    severity = MessageType.Information;
                yield return new Message(date, text, model, severity, simulationName, relativePath);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="simulationName"></param>
        public IEnumerable<InitialConditionsTable> GetInitialConditions(string simulationName)
        {
            IDataStore storage = this.storage ?? FindInScope<IDataStore>();
            if (storage == null)
                yield break;
            DataTable table = storage.Reader.GetData("_InitialConditions", simulationNames: simulationName.ToEnumerable());
            if (table == null)
                yield break;

            string simulationPath = FindInScope<Simulation>(simulationName)?.FullPath;
            foreach (IGrouping<string, DataRow> group in table.AsEnumerable().GroupBy(r => r["ModelPath"]?.ToString()))
            {
                string relativePath = group.Key;
                IModel model = simulationPath == null ? FindInScope(relativePath) : FindByPath(simulationPath + "." + relativePath)?.Value as IModel;
                yield return new InitialConditionsTable(model, group.Select(r => new InitialCondition()
                {
                    Name = r["Name"]?.ToString(),
                    Description = r["Description"]?.ToString(),
                    TypeName = r["DataType"]?.ToString(),
                    Units = r["Units"]?.ToString(),
                    DisplayFormat = r["DisplayFormat"]?.ToString(),
                    Value = r["Value"]?.ToString()
                }), relativePath);
            }
        }

        /// <summary>
        /// Write the summary report to the specified writer.
        /// </summary>
        /// <param name="storage">The data store to query</param>
        /// <param name="simulationName">The simulation name to produce a summary report for</param>
        /// <param name="writer">Text writer to write to</param>
        /// <param name="apsimSummaryImageFileName">The file name for the logo. Can be null</param>
        /// <param name="outtype">Indicates the format to be produced</param>
        /// <param name="darkTheme">Whether or not the dark theme should be used.</param>
        /// <param name="showInfo"></param>
        /// <param name="showWarnings"></param>
        /// <param name="showErrors"></param>
        /// <param name="showInitialConditions"></param>
        public static void WriteReport(
            IDataStore storage,
            string simulationName,
            TextWriter writer,
            string apsimSummaryImageFileName,
            OutputType outtype,
            bool darkTheme,
            bool showInfo,
            bool showWarnings,
            bool showErrors,
            bool showInitialConditions)
        {
            if (outtype == OutputType.html)
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine("<meta content='text/html; charset=UTF-8; http-equiv='content-type'>");
                writer.WriteLine("<style>");
                if (darkTheme)
                {
                    writer.WriteLine("h2 { color:white; } ");
                    writer.WriteLine("h3 { color:white; } ");
                    writer.WriteLine("table { border:1px solid white; }");
                }
                else
                {
                    writer.WriteLine("h2 { color:darkblue; } ");
                    writer.WriteLine("h3 { color:darkblue; } ");
                    writer.WriteLine("table { border:1px solid black; }");
                    writer.WriteLine("th { background-color: palegoldenrod}");
                    writer.WriteLine("tr.total { color:darkorange; }");
                }
                writer.WriteLine("table { border-collapse:collapse; width:100%; table-layout:fixed; text-align:left; }");
                writer.WriteLine("table.headered {text-align:right; }");
                writer.WriteLine("tr.total { font-weight:bold; }");
                writer.WriteLine("table.headered td.col1 { text-align:left; font-weight:bold; }");
                writer.WriteLine("td { border:1px solid; }");
                writer.WriteLine("th { border:1px solid; text-align:right; }");
                writer.WriteLine("th.col1 { text-align:left; }");
                writer.WriteLine("</style>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");
                writer.WriteLine("<a href=\"#log\">Simulation log</a>");

            }

            // Get the initial conditions table.
            if (showInitialConditions)
            {
                DataTable initialConditionsTable = storage.Reader.GetData(simulationNames: simulationName.ToEnumerable(), tableName: "_InitialConditions");
                if (initialConditionsTable != null)
                {
                    // Convert the '_InitialConditions' table in the DataStore to a series of
                    // DataTables for each model.
                    List<DataTable> tables = new List<DataTable>();
                    ConvertInitialConditionsToTables(initialConditionsTable, tables);

                    // Now write all tables to our report.
                    for (int i = 0; i < tables.Count; i += 2)
                    {
                        // Only write something to the summary file if we have something to write.
                        if (tables[i].Rows.Count > 0 || tables[i + 1].Rows.Count > 0)
                        {
                            string heading = tables[i].TableName;
                            WriteHeading(writer, heading, outtype);

                            // Write the manager script.
                            if (tables[i].Rows.Count == 1 && tables[i].Rows[0][0].ToString() == "Script code: ")
                            {
                                WriteScript(writer, tables[i].Rows[0], outtype);
                            }
                            else
                            {
                                // Write the properties table if we have any properties.
                                if (tables[i].Rows.Count > 0)
                                {
                                    WriteTable(writer, tables[i], outtype, "PropertyTable");
                                }

                                // Write the general data table if we have any data.
                                if (tables[i + 1].Rows.Count > 0)
                                {
                                    WriteTable(writer, tables[i + 1], outtype, "ApsimTable");
                                }
                            }

                            if (outtype == OutputType.html)
                                writer.WriteLine("<br/>");
                        }
                    }
                }
            }

            // Write out all messages.
            WriteHeading(writer, "Simulation log:", outtype, "log");
            DataTable messageTable = GetMessageTable(storage, simulationName);
            WriteMessageTable(writer, messageTable, outtype, false, "MessageTable", showInfo, showWarnings, showErrors);

            if (outtype == OutputType.html)
            {
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }
        }

        /// <summary>
        /// Create a message table ready for writing.
        /// </summary>
        /// <param name="storage">The data store</param>
        /// <param name="simulationName">The simulation name to get messages for</param>
        /// <returns>The filled message table</returns>
        private static DataTable GetMessageTable(IDataStore storage, string simulationName)
        {
            DataTable messageTable = new DataTable();
            DataTable messages = storage.Reader.GetData(simulationNames: new string[] { simulationName }, tableName: "_Messages", orderByFieldNames: new string[] { "Date" });
            if (messages != null && messages.Rows.Count > 0)
            {
                messageTable.Columns.Add("Date", typeof(string));
                messageTable.Columns.Add("Message", typeof(string));
                messageTable.Columns.Add("MessageType", typeof(MessageType));
                string previousCol1Text = null;
                string previousMessage = null;
                foreach (DataRow row in messages.Rows)
                {
                    // Work out the column 1 text.
                    string modelName = (string)row["ComponentName"];
                    MessageType errorLevel = (MessageType)Enum.Parse(typeof(MessageType), row["MessageType"].ToString());

                    string col1Text;
                    if (row["Date"].GetType() == typeof(DateTime))
                    {
                        DateTime date = (DateTime)row["Date"];
                        col1Text = date.ToString("yyyy-MM-dd") + " " + modelName;
                    }
                    else
                        col1Text = row["Date"].ToString();

                    // If the date and model name have changed then write a row.
                    if (col1Text != previousCol1Text)
                    {
                        if (previousCol1Text != null)
                        {
                            messageTable.Rows.Add(new object[] { previousCol1Text, previousMessage, errorLevel });
                        }

                        previousMessage = string.Empty;
                        previousCol1Text = col1Text;
                    }
                    else
                    {
                        col1Text = null;
                    }

                    string message = (string)row["Message"];

                    if (errorLevel == MessageType.Error)
                    {
                        previousMessage += "FATAL ERROR: " + message;
                    }
                    else if (errorLevel == MessageType.Warning)
                    {
                        previousMessage += "WARNING: " + message;
                    }
                    else
                    {
                        previousMessage += message;
                    }

                    previousMessage += "\r\n";
                }
                if (previousMessage != null)
                {
                    messageTable.Rows.Add(new object[] { previousCol1Text, previousMessage, MessageType.Information });
                }
            }

            return messageTable;
        }

        /// <summary>
        /// Write the specified heading to the TextWriter.
        /// </summary>
        /// <param name="writer">Text writer to write to</param>
        /// <param name="heading">The heading to write</param>
        /// <param name="outtype">Indicates the format to be produced</param>
        /// <param name="id">Provides an id tag for the heading (html only; optional)</param>
        private static void WriteHeading(TextWriter writer, string heading, OutputType outtype, string id = null)
        {
            if (outtype == OutputType.html)
            {
                writer.Write("<h2");
                if (!String.IsNullOrEmpty(id))
                    writer.Write(" id='" + id + "'");
                writer.WriteLine(">" + heading + "</h2>");
            }
            else if (outtype == OutputType.Markdown)
            {
                writer.WriteLine($"## {heading}");
                writer.WriteLine();
            }
            else
            {
                writer.WriteLine(heading.ToUpper());
                writer.WriteLine(new string('-', heading.Length));
            }
        }

        /// <summary>
        /// Write out manager script
        /// </summary>
        /// <param name="writer">Text writer to write to</param>
        /// <param name="row">The data table row containing the script</param>
        /// <param name="outtype">Indicates the format to be produced</param>
        private static void WriteScript(TextWriter writer, DataRow row, OutputType outtype)
        {
            string st = row[1].ToString();
            st = st.Replace("\t", "    ");
            if (outtype == OutputType.html)
            {
                writer.WriteLine("<pre>");
                st = st.Replace("&", "&amp;");
                st = st.Replace("<", "&lt;");
                st = st.Replace(">", "&gt;");
                writer.WriteLine(st);
                writer.WriteLine("</pre>");
            }
            else if (outtype == OutputType.Markdown)
            {
                writer.WriteLine("```");
                writer.WriteLine(st);
                writer.WriteLine("```");
                writer.WriteLine();
            }
            else
            {
                writer.WriteLine(st);
            }
        }

        /// <summary>
        /// Write the specified table to the TextWriter.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="table">The table to write</param>
        /// <param name="outtype">Indicates the format to be produced</param>
        /// <param name="className">The class name of the generated html table</param>
        private static void WriteTable(TextWriter writer, DataTable table, OutputType outtype, string className)
        {
            bool showHeadings = className != "PropertyTable";
            if (outtype == OutputType.html)
            {
                if (showHeadings)
                {
                    writer.WriteLine("<table class='headered'>");
                    writer.Write("<tr>");
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        writer.Write("<th");
                        if (i == 0)
                            writer.Write(" class='col1'");
                        writer.Write(">" + table.Columns[i].ColumnName + "</th>");
                    }
                    writer.WriteLine();
                }
                else
                    writer.WriteLine("<table>");

                foreach (DataRow row in table.Rows)
                {
                    bool titleRow = Convert.IsDBNull(row[0]);
                    if (titleRow)
                        writer.Write("<tr class='total'>");
                    else
                        writer.Write("<tr>");

                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        string st;
                        if (titleRow && i == 0)
                            st = "Total";
                        else
                            st = row[i].ToString();

                        writer.Write("<td");
                        if (i == 0)
                            writer.Write(" class='col1'");
                        writer.Write(">");
                        writer.Write(st);
                        writer.Write("</td>");
                    }
                    writer.WriteLine("</tr>");
                }
                writer.WriteLine("</table><br/>");
            }
            else if (outtype == OutputType.Markdown)
            {
                writer.WriteLine(DataTableUtilities.ToMarkdown(table, true));
                writer.WriteLine();
            }
            else
            {
                DataTableUtilities.DataTableToText(table, 0, "  ", showHeadings, writer);
            }
        }

        /// <summary>
        /// Write the specified table to the TextWriter.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="table">The table to write</param>
        /// <param name="outtype">Indicates the format to be produced</param>
        /// <param name="includeHeadings">Include headings in the html table produced?</param>
        /// <param name="className">The class name of the generated html table</param>
        /// <param name="showInfo"></param>
        /// <param name="showWarnings"></param>
        /// <param name="showErrors"></param>
        private static void WriteMessageTable(TextWriter writer, DataTable table, OutputType outtype, bool includeHeadings, string className, bool showInfo, bool showWarnings, bool showErrors)
        {
            foreach (DataRow row in table.Rows)
            {
                var messageType = (MessageType)row["MessageType"];
                if (messageType == MessageType.Information && !showInfo)
                    continue;
                if (messageType == MessageType.Warning && !showWarnings)
                    continue;
                if (messageType == MessageType.Error && !showErrors)
                    continue;

                if (outtype == OutputType.html)
                {
                    writer.WriteLine("<h3>" + row[0] + "</h3>");
                }
                else if (outtype == OutputType.Markdown)
                    writer.WriteLine($"### {row[0]}");
                else
                {
                    writer.WriteLine();
                    writer.WriteLine();
                    writer.WriteLine(row[0].ToString());
                }

                string st = row[1].ToString();
                st = st.Replace("\t", "    ");
                if (outtype == OutputType.html)
                {
                    writer.WriteLine("<pre>");
                    st = st.Replace("&", "&amp;");
                    st = st.Replace("<", "&lt;");
                    st = st.Replace(">", "&gt;");
                    writer.WriteLine(st);
                    writer.WriteLine("</pre>");
                }
                else if (outtype == OutputType.Markdown)
                {
                    writer.WriteLine("```");
                    writer.WriteLine(st);
                    writer.WriteLine("```");
                    writer.WriteLine();
                }
                else
                {
                    st = StringUtilities.IndentText(st, 4);
                    writer.WriteLine(st);
                }
            }
        }

        /// <summary>
        /// Find all properties from the model and fill this.properties.
        /// </summary>
        /// <param name="model">The model to search for properties</param>
        /// <param name="properties">The list of properties to fill</param>
        private static void FindAllProperties(Model model, List<Tuple<string, VariableProperty>> properties)
        {
            if (model != null)
            {
                foreach (PropertyInfo property in model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    // Properties must have a [Summary] attribute
                    bool includeProperty = property.IsDefined(typeof(SummaryAttribute), false);

                    if (includeProperty)
                    {
                        string name = property.Name;
                        VariableProperty prop = null;
                        prop = new VariableProperty(model, property);

                        if (prop != null)
                            properties.Add(new Tuple<string, VariableProperty>(name, prop));
                    }
                }
            }
        }

        /// <summary>
        /// Converts a flat 'InitialConditions' table (from the data store) to a series of data tables.
        /// </summary>
        /// <param name="initialConditionsTable">The table to read the rows from</param>
        /// <param name="tables">The list of tables to create</param>
        private static void ConvertInitialConditionsToTables(DataTable initialConditionsTable, List<DataTable> tables)
        {
            DataTable propertyDataTable = null;
            DataTable generalDataTable = null;
            string previousModel = null;
            foreach (DataRow row in initialConditionsTable.Rows)
            {
                string modelPath = row["ModelPath"].ToString();

                // If this is a new model then write a new section for it.
                if (modelPath != previousModel)
                {
                    // Add a new properties table for this model.
                    propertyDataTable = new DataTable(modelPath);
                    propertyDataTable.Columns.Add("Name", typeof(string));
                    propertyDataTable.Columns.Add("Value", typeof(string));
                    tables.Add(propertyDataTable);

                    // Add a new data table for this model.
                    generalDataTable = new DataTable("General " + modelPath);
                    tables.Add(generalDataTable);

                    previousModel = modelPath;
                }

                // Work out the property name.
                string propertyName = row["Description"].ToString();
                if (propertyName == string.Empty)
                    propertyName = row["Name"].ToString();
                string units = row["Units"].ToString();
                string displayFormat = row["DisplayFormat"].ToString();

                // If the data type is an array then write the general datatable.
                if (row["DataType"].ToString().Contains("[]"))
                {
                    if (units != null && units != string.Empty)
                    {
                        propertyName += " (" + units + ")";
                    }

                    bool showTotal = Convert.ToInt32(row["Total"], CultureInfo.InvariantCulture) == 1;
                    AddArrayToTable(propertyName, row["DataType"].ToString(), displayFormat, showTotal, row["Value"], generalDataTable);
                }
                else
                {
                    string value = FormatPropertyValue(row["DataType"].ToString(), row["Value"], displayFormat);
                    if (units != null && units != string.Empty)
                    {
                        value += " (" + units + ")";
                    }

                    propertyDataTable.Rows.Add(new object[]
                    {
                        propertyName + ": ",
                        value
                    });
                }
            }
        }

        /// <summary>
        /// Add a column to the specified table based on values in the 'value'
        /// </summary>
        /// <param name="heading">The new column heading</param>
        /// <param name="dataTypeName">The data type of the value</param>
        /// <param name="displayFormat">The display format to use when writing the column</param>
        /// <param name="showTotal">A value indicating whether a total should be added.</param>
        /// <param name="value">The values containing the array</param>
        /// <param name="table">The table where a column should be added to</param>
        private static void AddArrayToTable(string heading, string dataTypeName, string displayFormat, bool showTotal, object value, DataTable table)
        {
            if (displayFormat == null)
            {
                displayFormat = "N3";
            }

            string[] stringValues = value.ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (dataTypeName == "Double[]")
            {
                List<double> values = new List<double>();
                values.AddRange(MathUtilities.StringsToDoubles(stringValues));
                if (showTotal)
                {
                    values.Add(MathUtilities.Sum(values));
                }

                stringValues = MathUtilities.DoublesToStrings(values, displayFormat);
            }
            else if (dataTypeName == "Int32[]")
            {
                List<double> values = new List<double>();
                values.AddRange(MathUtilities.StringsToDoubles(stringValues));
                if (showTotal)
                {
                    values.Add(MathUtilities.Sum(values));
                }

                stringValues = MathUtilities.DoublesToStrings(values, "N0");
            }
            else if (dataTypeName != "String[]")
            {
                // throw new ApsimXException(null, "Invalid property type: " + dataTypeName);
            }

            DataTableUtilities.AddColumn(table, heading, stringValues);
        }

        /// <summary>
        /// Format the specified value into a string and return the string.
        /// </summary>
        /// <param name="dataTypeName">The name of the data type</param>
        /// <param name="value">The value to format</param>
        /// <param name="format">The format to use for the value</param>
        /// <returns>The formatted value as a string</returns>
        private static string FormatPropertyValue(string dataTypeName, object value, string format)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (dataTypeName == "Double" || dataTypeName == "Single")
            {
                double doubleValue = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                if (format == null || format == string.Empty)
                {
                    return string.Format("{0:F3}", doubleValue);
                }
                else
                {
                    return string.Format("{0:" + format + "}", doubleValue);
                }
            }
            else if (dataTypeName == "DateTime")
            {
                DateTime date = DateTime.ParseExact(value.ToString(), "yyyy-MM-dd hh:mm:ss", null);
                return date.ToString("yyyy-MM-dd");
            }
            else
            {
                return value.ToString();
            }
        }

        #endregion

    }
}
