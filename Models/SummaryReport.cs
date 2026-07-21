using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using APSIM.Numerics;
using APSIM.Shared.Documentation.Extensions;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Storage;

namespace Models
{
    /// <summary>
    /// Provides static methods for generating and formatting summary reports
    /// from APSIM simulation data. Supports HTML, Markdown, and plain-text output.
    /// </summary>
    public static class SummaryReport
    {
        /// <summary>
        /// Enumeration used to indicate the format of the output string.
        /// </summary>
        public enum OutputType
        {
            /// <summary>Plain ASCII text.</summary>
            plain,

            /// <summary>HTML format.</summary>
            html,

            /// <summary>Markdown format.</summary>
            Markdown
        }

        /// <summary>
        /// Write a single summary file for all simulations.
        /// </summary>
        /// <param name="storage">The storage where the summary data is stored.</param>
        /// <param name="fileName">The file name to write.</param>
        /// <param name="darkTheme">Whether or not the dark theme should be used.</param>
        public static void WriteSummaryToTextFiles(IDataStore storage, string fileName, bool darkTheme)
        {
            using (StreamWriter report = new StreamWriter(fileName))
            {
                List<string> names = storage.Reader.SimulationNames;
                names.Sort();

                foreach (string simulationName in names)
                {
                    WriteReport(storage, simulationName, report, null, outtype: OutputType.html, darkTheme: darkTheme, true, true, true, true);
                    report.WriteLine();
                    report.WriteLine();
                    report.WriteLine("############################################################################");
                }
            }
        }

        /// <summary>
        /// Write the summary report to the specified writer.
        /// </summary>
        /// <param name="storage">The data store to query.</param>
        /// <param name="simulationName">The simulation name to produce a summary report for.</param>
        /// <param name="writer">Text writer to write to.</param>
        /// <param name="apsimSummaryImageFileName">The file name for the logo. Can be null.</param>
        /// <param name="outtype">Indicates the format to be produced.</param>
        /// <param name="darkTheme">Whether or not the dark theme should be used.</param>
        /// <param name="showInfo">Whether to show informational messages.</param>
        /// <param name="showWarnings">Whether to show warning messages.</param>
        /// <param name="showErrors">Whether to show error messages.</param>
        /// <param name="showInitialConditions">Whether to show initial conditions tables.</param>
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

            if (showInitialConditions)
            {
                DataTable initialConditionsTable = storage.Reader.GetData(simulationNames: simulationName.ToEnumerable(), tableName: "_InitialConditions");
                if (initialConditionsTable != null)
                {
                    List<DataTable> tables = new List<DataTable>();
                    ConvertInitialConditionsToTables(initialConditionsTable, tables);

                    for (int i = 0; i < tables.Count; i += 2)
                    {
                        if (tables[i].Rows.Count > 0 || tables[i + 1].Rows.Count > 0)
                        {
                            string heading = tables[i].TableName;
                            WriteHeading(writer, heading, outtype);

                            if (tables[i].Rows.Count == 1 && tables[i].Rows[0][0].ToString() == "Script code: ")
                            {
                                WriteScript(writer, tables[i].Rows[0], outtype);
                            }
                            else
                            {
                                if (tables[i].Rows.Count > 0)
                                    WriteTable(writer, tables[i], outtype, "PropertyTable");

                                if (tables[i + 1].Rows.Count > 0)
                                    WriteTable(writer, tables[i + 1], outtype, "ApsimTable");
                            }

                            if (outtype == OutputType.html)
                                writer.WriteLine("<br/>");
                        }
                    }
                }
            }

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
        /// <param name="storage">The data store.</param>
        /// <param name="simulationName">The simulation name to get messages for.</param>
        /// <returns>The filled message table.</returns>
        public static DataTable GetMessageTable(IDataStore storage, string simulationName)
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

                    if (col1Text != previousCol1Text)
                    {
                        if (previousCol1Text != null)
                            messageTable.Rows.Add(new object[] { previousCol1Text, previousMessage, errorLevel });

                        previousMessage = string.Empty;
                        previousCol1Text = col1Text;
                    }
                    else
                    {
                        col1Text = null;
                    }

                    string message = (string)row["Message"];

                    if (errorLevel == MessageType.Error)
                        previousMessage += "FATAL ERROR: " + message;
                    else if (errorLevel == MessageType.Warning)
                        previousMessage += "WARNING: " + message;
                    else
                        previousMessage += message;

                    previousMessage += "\r\n";
                }
                if (previousMessage != null)
                    messageTable.Rows.Add(new object[] { previousCol1Text, previousMessage, MessageType.Information });
            }

            return messageTable;
        }

        /// <summary>
        /// Converts a flat '_InitialConditions' table from the data store into
        /// a series of paired DataTables (one for properties, one for array data)
        /// grouped by model path.
        /// </summary>
        /// <param name="initialConditionsTable">The flat table to convert.</param>
        /// <param name="tables">The list of tables to populate (pairs: properties + general data).</param>
        public static void ConvertInitialConditionsToTables(DataTable initialConditionsTable, List<DataTable> tables)
        {
            DataTable propertyDataTable = null;
            DataTable generalDataTable = null;
            string previousModel = null;
            foreach (DataRow row in initialConditionsTable.Rows)
            {
                string modelPath = row["ModelPath"].ToString();

                if (modelPath != previousModel)
                {
                    propertyDataTable = new DataTable(modelPath);
                    propertyDataTable.Columns.Add("Name", typeof(string));
                    propertyDataTable.Columns.Add("Value", typeof(string));
                    tables.Add(propertyDataTable);

                    generalDataTable = new DataTable("General " + modelPath);
                    tables.Add(generalDataTable);

                    previousModel = modelPath;
                }

                string propertyName = row["Description"].ToString();
                if (propertyName == string.Empty)
                    propertyName = row["Name"].ToString();
                string units = row["Units"].ToString();
                string displayFormat = row["DisplayFormat"].ToString();

                if (row["DataType"].ToString().Contains("[]"))
                {
                    if (units != null && units != string.Empty)
                        propertyName += " (" + units + ")";

                    bool showTotal = System.Convert.ToInt32(row["Total"], CultureInfo.InvariantCulture) == 1;
                    AddArrayToTable(propertyName, row["DataType"].ToString(), displayFormat, showTotal, row["Value"], generalDataTable);
                }
                else
                {
                    string value = FormatPropertyValue(row["DataType"].ToString(), row["Value"], displayFormat);
                    if (units != null && units != string.Empty)
                        value += " (" + units + ")";

                    propertyDataTable.Rows.Add(new object[]
                    {
                        propertyName + ": ",
                        value
                    });
                }
            }
        }

        /// <summary>
        /// Write the specified heading to the TextWriter.
        /// </summary>
        /// <param name="writer">Text writer to write to.</param>
        /// <param name="heading">The heading to write.</param>
        /// <param name="outtype">Indicates the format to be produced.</param>
        /// <param name="id">Provides an id tag for the heading (html only; optional).</param>
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
        /// Write out a manager script block.
        /// </summary>
        /// <param name="writer">Text writer to write to.</param>
        /// <param name="row">The data table row containing the script.</param>
        /// <param name="outtype">Indicates the format to be produced.</param>
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
        /// <param name="writer">The writer to write to.</param>
        /// <param name="table">The table to write.</param>
        /// <param name="outtype">Indicates the format to be produced.</param>
        /// <param name="className">The class name of the generated html table.</param>
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
                    bool titleRow = System.Convert.IsDBNull(row[0]);
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
        /// Write a message table to the TextWriter, filtered by severity.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="table">The table to write.</param>
        /// <param name="outtype">Indicates the format to be produced.</param>
        /// <param name="includeHeadings">Include headings in the html table produced?</param>
        /// <param name="className">The class name of the generated html table.</param>
        /// <param name="showInfo">Whether to show informational messages.</param>
        /// <param name="showWarnings">Whether to show warning messages.</param>
        /// <param name="showErrors">Whether to show error messages.</param>
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
                    writer.WriteLine("<h3>" + row[0] + "</h3>");
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
        /// Add a column to the specified table based on array values stored as a string.
        /// </summary>
        /// <param name="heading">The new column heading.</param>
        /// <param name="dataTypeName">The data type of the array elements.</param>
        /// <param name="displayFormat">The display format to use when writing the column.</param>
        /// <param name="showTotal">Whether a total row should be added.</param>
        /// <param name="value">The comma-separated string of values.</param>
        /// <param name="table">The table where a column should be added.</param>
        private static void AddArrayToTable(string heading, string dataTypeName, string displayFormat, bool showTotal, object value, DataTable table)
        {
            if (displayFormat == null)
                displayFormat = "N3";

            string[] stringValues = value.ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (dataTypeName == "Double[]")
            {
                List<double> values = new List<double>();
                values.AddRange(MathUtilities.StringsToDoubles(stringValues));
                if (showTotal)
                    values.Add(MathUtilities.Sum(values));

                stringValues = MathUtilities.DoublesToStrings(values, displayFormat);
            }
            else if (dataTypeName == "Int32[]")
            {
                List<double> values = new List<double>();
                values.AddRange(MathUtilities.StringsToDoubles(stringValues));
                if (showTotal)
                    values.Add(MathUtilities.Sum(values));

                stringValues = MathUtilities.DoublesToStrings(values, "N0");
            }

            DataTableUtilities.AddColumn(table, heading, stringValues);
        }

        /// <summary>
        /// Format a property value into a display string.
        /// </summary>
        /// <param name="dataTypeName">The name of the data type.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="format">The format string to apply.</param>
        /// <returns>The formatted value as a string.</returns>
        private static string FormatPropertyValue(string dataTypeName, object value, string format)
        {
            if (value == null)
                return string.Empty;

            if (dataTypeName == "Double" || dataTypeName == "Single")
            {
                double doubleValue = System.Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                if (format == null || format == string.Empty)
                    return string.Format("{0:F3}", doubleValue);
                else
                    return string.Format("{0:" + format + "}", doubleValue);
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
    }
}
