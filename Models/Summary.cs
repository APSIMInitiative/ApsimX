using Models.Core;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Data;
using System;
using System.Collections.Generic;

namespace Models
{
    [ViewName("UserInterface.Views.SummaryView")]
    [PresenterName("UserInterface.Presenters.SummaryPresenter")]
    public class Summary : Model, ISummary
    {
        // Privates
        private const string divider = "------------------------------------------------------------------------------";

        // Links
        [Link] private DataStore DataStore = null;
        [Link] private Simulation Simulation = null;
        [Link] private Simulations Simulations = null;
        [Link] private Clock Clock = null;

        // Parameters
        public bool html { get; set; }
        public bool AutoCreate { get; set; }
        public bool StateVariables { get; set; }

        /// <summary>
        /// All simulations have been completed. 
        /// </summary>
        [EventSubscribe("AllCompleted")]
        private void OnAllCompleted(object sender, EventArgs e)
        {
            if (AutoCreate)
                CreateReportFile(false);
        }

        /// <summary>
        /// Write a message to the summary
        /// </summary>
        public void WriteMessage(string FullPath, string Message)
        {
            DataStore.WriteMessage(FullPath, Simulation.Name, Clock.Today, Message, DataStore.ErrorLevel.Information);
        }

        /// <summary>
        /// Write a warning message to the summary
        /// </summary>
        public void WriteWarning(string FullPath, string Message)
        {
            DataStore.WriteMessage(FullPath, Simulation.Name, Clock.Today, Message, DataStore.ErrorLevel.Warning);
        }

        /// <summary>
        /// Write an error message to the summary
        /// </summary>
        public void WriteError(string Message)
        {
            DataStore.WriteMessage(FullPath, Simulation.Name, Clock.Today, Message, DataStore.ErrorLevel.Error);
        }

        /// <summary>
        /// Write a property to the summary.
        /// </summary>
        public void WriteProperty(string Name, string Value)
        {
            DataStore.WriteProperty(Simulation.Name, Name, Value);
        }

        /// <summary>
        /// A property that the presenter will use to get the summary.
        /// </summary>
        public string GetSummary(string apsimSummaryImageFileName)
        {
            StringWriter st = new StringWriter();
            WriteSummary(st, Simulation.Name, apsimSummaryImageFileName);
            return st.ToString();
        }

        /// <summary>
        /// Create a report file in text format.
        /// </summary>
        public void CreateReportFile(bool baseline)
        {
            StreamWriter report;
            if (baseline)
                report = new StreamWriter(Path.ChangeExtension(Simulations.FileName, ".baseline.csv"));
            else
                report = new StreamWriter(Path.ChangeExtension(Simulations.FileName, ".csv"));
            WriteSummary(report, Simulation.Name, null);
        }

        #region Summary report generation

        /// <summary>
        /// Write out summary information
        /// </summary>
        public void WriteSummary(TextWriter report, string simulationName, string apsimSummaryImageFileName)
        {
            if (html)
            {
                report.WriteLine("<!DOCTYPE html>");
                report.WriteLine("<html>");
                report.WriteLine("<body>");
               
                report.WriteLine("<style type=\"text/css\">");
                report.WriteLine("table.ApsimTable {font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#333333;border-width: 1px;border-color: #729ea5;border-collapse: collapse;}");
                report.WriteLine("table.ApsimTable th {font-family:Arial,Helvetica,sans-serif;font-size:14px;background-color:#acc8cc;border-width: 1px;padding: 8px;border-style: solid;border-color: #729ea5;text-align:left;}");
                report.WriteLine("table.ApsimTable tr font-family:Arial,Helvetica,sans-serif;vertical-align:top;{background-color:#d4e3e5;}");
                report.WriteLine("table.ApsimTable td {font-family:Arial,Helvetica,sans-serif;font-size:14px;border-width: 1px;padding: 8px;border-style: solid;border-color: #729ea5;}");

                report.WriteLine("table.PropertyTable {font-family:Arial,Helvetica,sans-serif;font-size:14px;border-width: 0px;}");
                report.WriteLine("table.PropertyTable th {font-family:Arial,Helvetica,sans-serif;font-size:14px;border-width: 0px;}");
                report.WriteLine("table.PropertyTable tr {font-family:Arial,Helvetica,sans-serif;vertical-align:top;}");
                report.WriteLine("table.PropertyTable td {font-family:Arial,Helvetica,sans-serif;font-size:14px;border-width: 0px;}");

                report.WriteLine("p.Warning {font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#FF6600;}");
                report.WriteLine("p.Error {font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#FF0000;}");
                
                report.WriteLine("</style>");

                report.WriteLine("<img src=\"" + apsimSummaryImageFileName + "\">");
            }

            // Write out all properties.
            WriteProperties(report, simulationName);
            
            // Write out all messages.
            if (html)
                report.WriteLine("<hr>");
            WriteHeading(report, "Simulation log:", html);
            DataTable messageTable = GetMessageTable(simulationName);
            WriteTable(report, messageTable, html, false, "PropertyTable");

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
            DataTable messages = DataStore.GetData(simulationName, "Messages");
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
                    Models.DataStore.ErrorLevel errorLevel = (Models.DataStore.ErrorLevel)Enum.Parse(typeof(Models.DataStore.ErrorLevel), row[4].ToString());

                    if (errorLevel == DataStore.ErrorLevel.Error)
                        message = "FATAL ERROR: " + message;
                    else if (errorLevel == DataStore.ErrorLevel.Warning)
                        message = "WARNING: " + message;

                    messageTable.Rows.Add(new object[] { date.ToString("yyyy-MM-dd"), modelName, message });
                }
            }
            return messageTable;
        }

        /// <summary>
        /// Get a table of all properties for all models in the specified simulation.
        /// </summary>
        private void WriteProperties(TextWriter report, string simulationName)
        {
            DataTable propertyTable = new DataTable();

            Simulation simulation = Simulations.Get(simulationName) as Simulation;
            if (simulation != null)
            {
                Model[] models = simulation.FindAll();
                foreach (Model model in models)
                    WriteModelProperties(report, model, html, StateVariables);
            }
        }

        /// <summary>
        /// Write all properties of the specified model to the specified TextWriter. Retruns true if something
        /// was written.
        /// </summary>
        public static bool WriteModelProperties(TextWriter report, Model model, bool html, bool stateVariables)
        {
            string modelName = model.FullPath;

            DataTable propertyTable = new DataTable();
            propertyTable.Columns.Add("Property name", typeof(string));
            propertyTable.Columns.Add("Value", typeof(object));

            DataTable table = new DataTable();

            List<IVariable> properties = new List<IVariable>();
            properties.AddRange(Utility.ModelFunctions.Parameters(model));

            if (stateVariables)
                properties.AddRange(Utility.ModelFunctions.States(model));

            foreach (IVariable property in properties)
            {
                if (property.Name != "Name" && property.Name != "Parent")
                {
                    object value = property.Value;
                    if (value != null)
                    {
                        string propertyName = property.Name;

                        // look for a description attribute.
                        if (property.Description != null)
                            propertyName = property.Description;

                        // look for units
                        if (property.Units != null)
                            propertyName += property.Units;
                        propertyName += ":";

                        // If an array was found then put values into table.
                        if (value.GetType().IsArray)
                        {
                            Array array = value as Array;
                            if (array != null && array.Length > 0)
                            {
                                List<string> tableValues = new List<string>();

                                foreach (object Value in array)
                                    tableValues.Add(FormatValue(Value));

                                if (table.Rows.Count == 0 || table.Rows.Count == tableValues.Count)
                                    Utility.DataTable.AddColumn(table, propertyName, tableValues.ToArray());
                            }
                        }

                        // Write out a code block
                        else if (value.ToString().Contains("\n"))
                            WriteCodeBlock(report, html, value, propertyName);

                        // Write out a normal property.
                        else
                            propertyTable.Rows.Add(new object[] { propertyName, value });
                    }
                }
            }

            bool somethingWritten = false;
            if (propertyTable.Rows.Count > 0 || table.Rows.Count > 0)
            {
                WriteHeading(report, modelName, html);
                somethingWritten = true;
            }

            // write out properties
            if (propertyTable.Rows.Count > 0)
            {
                WriteTable(report, propertyTable, html, false, "PropertyTable");
                somethingWritten = true;
            }

            // write out table.
            if (table.Rows.Count > 0)
            {
                WriteTable(report, table, html, true, "ApsimTable");
                somethingWritten = true;
            }

            if (somethingWritten)
                report.WriteLine(divider);

            return somethingWritten;
        }

        /// <summary>
        /// Write the specified value as a code block to the specified TextWriter.
        /// </summary>
        private static void WriteCodeBlock(TextWriter report, bool html, object value, string propertyName)
        {
            // the value has <cr><lf> - write out manually 
            if (html)
            {
                report.WriteLine("<p>" + propertyName + "</p>");
                report.WriteLine("<code><pre>" + value.ToString().Replace("\n", "<br/>") + "</pre></code>");
            }
            else
            {
                report.WriteLine(propertyName);
                report.WriteLine(value.ToString());
            }
        }

        /// <summary>
        /// Format the specified value into a string and return the string.
        /// </summary>
        private static string FormatValue(object value)
        {
            if (value is double || value is float)
                return String.Format("{0:F3}", value);
            else if (value is DateTime)
                return ((DateTime)value).ToString("yyyy-mm-dd");
            else
                return value.ToString();
        }

        /// <summary>
        /// Write the specified heading to the TextWriter.
        /// </summary>
        private static void WriteHeading(TextWriter writer, string heading, bool html)
        {
            if (html)
                writer.WriteLine("<h2>" + heading + "</h2>");
            else
                writer.WriteLine(heading.ToUpper());
        }

        /// <summary>
        /// Write the specfieid table to the TextWriter.
        /// </summary>
        private static void WriteTable(TextWriter report, DataTable table, bool html, bool includeHeadings, string className)
        {
            if (html)
            {
                report.WriteLine("<p><table class=\"" + className + "\">");
                if (includeHeadings)
                {
                    report.WriteLine("<tr>");
                    foreach (DataColumn col in table.Columns)
                    {
                        report.Write("<th>");
                        report.Write(col.ColumnName);
                        report.WriteLine("</th>");
                    }
                    report.WriteLine("</tr>");
                }

                foreach (DataRow row in table.Rows)
                {
                    report.WriteLine("<tr>");
                    foreach (DataColumn col in table.Columns)
                    {
                        report.WriteLine("<td>");
                        string st = FormatValue(row[col]);
                        if (st.Contains("\n"))
                            st = st.Replace("\n", "<br/>");
                        if (st.Contains("WARNING:"))
                            report.WriteLine("<p class=\"Warning\">");
                        else if (st.Contains("ERROR:"))
                            report.WriteLine("<p class=\"Error\">");
                        else
                            report.WriteLine("<p>");
                        report.Write(st);
                        
                        report.WriteLine("</td>");
                    }
                    report.WriteLine("</tr>");
                }
                report.WriteLine("</table></p>");
            }
            else
            {
                report.WriteLine(Utility.DataTable.DataTableToCSV(table, 0));
            }

        }

        #endregion

    }
}
