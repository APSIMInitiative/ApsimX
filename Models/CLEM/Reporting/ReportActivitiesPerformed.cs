using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using APSIM.Shared.Utilities;
using System.Data;
using System.IO;
using Models.CLEM.Resources;
using Models.Core.Attributes;
using Models.Core.Run;
using Models.Storage;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.CLEMView")]
    [PresenterName("UserInterface.Presenters.ActivityLedgerGridPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Zones.CircularZone))]
    [ValidParent(ParentType = typeof(Zones.RectangularZone))]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [Description("This report automatically generates an activity performed ledger and provides a table of activity success.")]
    [Version(1, 0, 4, "Automatically create HTML version")]
    [Version(1, 0, 3, "Rotate option for HTML version")]
    [Version(1, 0, 2, "HTML version created")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/ActivitiesPerformed.htm")]
    public class ReportActivitiesPerformed : Models.Report, ICLEMDescriptiveSummary, ICLEMUI, ISpecificOutputFilename
    {
        /// <summary>
        /// Create html version of summary
        /// </summary>
        [Description("Create HTML version")]
        public bool CreateHTML { get; set; }

        /// <summary>
        /// Rotate report so months are columns
        /// </summary>
        [Description("Rotate report for HTML version")]
        public bool RotateReport { get; set; }

        /// <summary>
        /// Automatically create html version of summary at end of simulation
        /// </summary>
        [Description("Automatically create HTML report at end of simulation")]
        public bool AutoCreateHTML { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SelectedTab { get; set; }

        /// <summary>The columns to write to the data store.</summary>
        [NonSerialized]
        private List<IReportColumn> columns = null;
        [NonSerialized]
        private ReportData dataToWriteToDb = null;

        /// <summary>Link to a simulation</summary>
        [Link]
        private Simulation simulation = null;

        /// <summary>Link to a clock model.</summary>
        [Link]
        private IClock clock = null;

        /// <summary>Link to a storage service.</summary>
        [Link]
        private IDataStore storage = null;

        /// <summary>Link to a locator service.</summary>
        [Link]
        private ILocator locator = null;

        /// <summary>Link to an event service.</summary>
        [Link]
        private IEvent events = null;

        /// <summary>
        /// Name of filename to save labour report
        /// </summary>
        public string HtmlOutputFilename { get { return "ActivitiesPerformedSummary.html"; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReportActivitiesPerformed()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Default;
        }

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            dataToWriteToDb = null;

            VariableNames = new string[]
            {
                "[Clock].Today as Date",
                "[Activities].LastActivityPerformed.Name as Name",
                "[Activities].LastActivityPerformed.Status as Status",
                "[Activities].LastActivityPerformed.UniqueID as UniqueID"
            };

            EventNames = new string[] { "[Activities].ActivityPerformed" };

            // Tidy up variable/event names.
            VariableNames = TidyUpVariableNames();
            EventNames = TidyUpEventNames();
            this.FindVariableMembers();

            // Subscribe to events.
            foreach (string eventName in EventNames)
            {
                if (eventName != string.Empty)
                {
                    events.Subscribe(eventName.Trim(), DoOutputEvent);
                }
            }
        }

        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e)
        {
            if (dataToWriteToDb != null)
            {
                storage.Writer.WriteTable(dataToWriteToDb);
            }
            dataToWriteToDb = null;

            // if auto create
            if(AutoCreateHTML)
            {
                this.CreateDataTable(storage, Path.GetDirectoryName((sender as Simulation).FileName), false);
            }
        }

        /// <summary>A method that can be called by other models to perform a line of output.</summary>
        public new void DoOutput()
        {
            if (dataToWriteToDb == null)
            {
                string folderName = null;
                var folderDescriptor = simulation.Descriptors.Find(d => d.Name == "FolderName");
                if (folderDescriptor != null)
                {
                    folderName = folderDescriptor.Value;
                }

                dataToWriteToDb = new ReportData()
                {
                    FolderName = folderName,
                    SimulationName = simulation.Name,
                    TableName = Name,
                    ColumnNames = columns.Select(c => c.Name).ToList(),
                    ColumnUnits = columns.Select(c => c.Units).ToList()
                };
            }

            // Get number of groups.
            var numGroups = Math.Max(1, columns.Max(c => c.NumberOfGroups));

            for (int groupIndex = 0; groupIndex < numGroups; groupIndex++)
            {
                // Create a row ready for writing.
                List<object> valuesToWrite = new List<object>();
                List<string> invalidVariables = new List<string>();
                for (int i = 0; i < columns.Count; i++)
                {
                    try
                    {
                        valuesToWrite.Add(columns[i].GetValue(groupIndex));
                    }
                    catch// (Exception err)
                    {
                        // Should we include exception message?
                        invalidVariables.Add(columns[i].Name);
                    }
                }
                if (invalidVariables != null && invalidVariables.Count > 0)
                {
                    throw new Exception($"Error in report {Name}: Invalid report variables found:\r\n{string.Join("\r\n", invalidVariables)}");
                }

                // Add row to our table that will be written to the db file
                dataToWriteToDb.Rows.Add(valuesToWrite);
            }

            // Write the table if we reach our threshold number of rows.
            if (dataToWriteToDb.Rows.Count >= 100)
            {
                storage.Writer.WriteTable(dataToWriteToDb);
                dataToWriteToDb = null;
            }

            DayAfterLastOutput = clock.Today.AddDays(1);
        }

        /// <summary>Create a text report from tables in this data store.</summary>
        /// <param name="storage">The data store.</param>
        /// <param name="fileName">Name of the file.</param>
        public static new void WriteAllTables(IDataStore storage, string fileName)
        {
            // Write out each table for this simulation.
            foreach (string tableName in storage.Reader.TableNames)
            {
                DataTable data = storage.Reader.GetData(tableName);
                if (data != null && data.Rows.Count > 0)
                {
                    SortColumnsOfDataTable(data);
                    StreamWriter report = new StreamWriter(Path.ChangeExtension(fileName, "." + tableName + ".csv"));
                    DataTableUtilities.DataTableToText(data, 0, ",", true, report);
                    report.Close();
                }
            }
        }

        /// <summary>Sort the columns alphabetically</summary>
        /// <param name="table">The table to sort</param>
        private static void SortColumnsOfDataTable(DataTable table)
        {
            var columnArray = new DataColumn[table.Columns.Count];
            table.Columns.CopyTo(columnArray, 0);
            var ordinal = -1;
            foreach (var orderedColumn in columnArray.OrderBy(c => c.ColumnName))
            {
                orderedColumn.SetOrdinal(++ordinal);
            }

            ordinal = -1;
            int i = table.Columns.IndexOf("SimulationName");
            if (i != -1)
            {
                table.Columns[i].SetOrdinal(++ordinal);
            }

            i = table.Columns.IndexOf("SimulationID");
            if (i != -1)
            {
                table.Columns[i].SetOrdinal(++ordinal);
            }
        }


        /// <summary>Called when one of our 'EventNames' events are invoked</summary>
        public new void DoOutputEvent(object sender, EventArgs e)
        {
            DoOutput();
        }

        /// <summary>
        /// Fill the Members list with VariableMember objects for each variable.
        /// </summary>
        private new void FindVariableMembers()
        {
            this.columns = new List<IReportColumn>();

            AddExperimentFactorLevels();

            // If a group by variable was specified then all columns need to be aggregated
            // columns. Find the first aggregated column so that we can, later, use its from and to
            // variables to create an agregated column that doesn't have them.
            string from = null;
            string to = null;
            if (!string.IsNullOrEmpty(GroupByVariableName))
            {
                FindFromTo(out from, out to);
            }

            foreach (string fullVariableName in this.VariableNames)
            {
                try
                {
                    if (!string.IsNullOrEmpty(fullVariableName))
                    {
                        columns.Add(new ReportColumn(fullVariableName, clock, locator, events, GroupByVariableName, from, to));
                    }
                }
                catch (Exception err)
                {
                    throw new Exception($"Error while creating report column '{fullVariableName}'", err);
                }
            }
        }

        /// <summary>Add the experiment factor levels as columns.</summary>
        private void AddExperimentFactorLevels()
        {
            if (simulation.Descriptors != null)
            {
                foreach (var descriptor in simulation.Descriptors)
                {
                    if (descriptor.Name != "Zone" && descriptor.Name != "SimulationName")
                    {
                        this.columns.Add(new ReportColumnConstantValue(descriptor.Name, descriptor.Value));
                    }
                }
            }
        }


        #region create html report

        private DataTable GetData(IDataStore dataStore)
        {
            DataTable data = null;
            if (dataStore != null)
            {
                try
                {
                    // get all rows from table
                    data = dataStore.Reader.GetData(
                                            tableName: this.Name,
                                            count: 0);
                    if (data != null)
                    {
                        // need to filter by current simulation
                        string simName = this.FindAllAncestors<Simulation>().First().Name;
                        string zoneName = this.FindAllAncestors<ZoneCLEM>().First().Name;
                        var filteredData = data.AsEnumerable()
                            .Where(row => row.Field<String>("SimulationName") == simName & row.Field<String>("Zone") == zoneName);
                        if (filteredData.Any())
                        {
                            data = filteredData.CopyToDataTable();
                        }
                        else
                        {
                            data = new DataTable();
                        }
                    }

                }
                catch (Exception)
                {
                }
            }
            else
            {
                data = new DataTable();
            }
            return data;
        }

        private DataTable Transpose(DataTable dt)
        {
            DataTable dtNew = new DataTable();

            //adding columns    
            for (int i = 0; i <= dt.Rows.Count; i++)
            {
                dtNew.Columns.Add(i.ToString());
            }

            //Changing Column Captions: 
            dtNew.Columns[0].ColumnName = "Month";

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string columnname = dt.Rows[i].ItemArray[0].ToString();
                int cnt = 0;
                while (dtNew.Columns.Contains(columnname + (cnt == 0 ? "" : $"_{cnt}")))
                {
                    cnt++;
                }
                dtNew.Columns[i + 1].ColumnName = columnname + (cnt == 0 ? "" : $"_{cnt}");
            }

            //Adding Row Data
            for (int k = 1; k < dt.Columns.Count; k++)
            {
                DataRow r = dtNew.NewRow();
                r[0] = dt.Columns[k].ToString();
                for (int j = 1; j <= dt.Rows.Count; j++)
                {
                    r[j] = dt.Rows[j - 1][k];
                }
                dtNew.Rows.Add(r);
            }

            return dtNew;
        }

        /// <summary>
        /// 
        /// </summary>
        public DataTable CreateDataTable(IDataStore dataStore, string directoryPath, bool darkTheme)
        {
            using (DataTable data = GetData(dataStore))
            {
                DataTable tbl = null;
                if (data != null && data.Rows.Count > 0)
                {
                    // get unique rows
                    List<string> activities = data.AsEnumerable().Select(a => a.Field<string>("UniqueID")).Distinct().ToList<string>();
                    string timeStepUID = data.AsEnumerable().Where(a => a.Field<string>("Name") == "TimeStep").FirstOrDefault().Field<string>("UniqueID");

                    // get unique columns
                    List<DateTime> dates = data.AsEnumerable().Select(a => a.Field<DateTime>("Date")).Distinct().ToList<DateTime>();

                    // create table
                    tbl = new DataTable();
                    tbl.Columns.Add("Activity");
                    foreach (var item in dates)
                    {
                        tbl.Columns.Add(item.Month.ToString("00") + "\r\n" + item.ToString("yy"));
                    }
                    // add blank column for resize row height of pixelbuf with font size change
                    tbl.Columns.Add(" ");

                    foreach (var item in activities)
                    {
                        if (item != timeStepUID)
                        {
                            DataRow dr = tbl.NewRow();
                            string name = data.AsEnumerable().Where(a => a.Field<string>("UniqueID") == item).FirstOrDefault()["Name"].ToString();
                            dr["Activity"] = name;

                            foreach (var activityTick in data.AsEnumerable().Where(a => a.Field<string>("UniqueID") == item))
                            {
                                DateTime dte = (DateTime)activityTick["Date"];
                                string status = activityTick["Status"].ToString();
                                dr[dte.Month.ToString("00") + "\r\n" + dte.ToString("yy")] = status;
                            }
                            dr[" "] = " ";
                            tbl.Rows.Add(dr);
                        }
                    }
                }
                CreateHTMLVersion(tbl, directoryPath, darkTheme);
                return tbl;
            }
        }

        /// <summary>
        /// Create a html rendered version of this report
        /// </summary>
        public void CreateHTMLVersion(DataTable data, string directoryPath, bool darkTheme = false)
        {
            string html = "<!DOCTYPE html>\r\n" +
                "<html>\r\n<head>\r\n<script type=\"text / javascript\" src=\"https://livejs.com/live.js\"></script>\r\n" +
                "<meta http-equiv=\"Cache-Control\" content=\"no-cache, no-store, must-revalidate\" />\r\n" +
                "<meta http-equiv = \"Pragma\" content = \"no-cache\" />\r\n" +
                "<meta http-equiv = \"Expires\" content = \"0\" />\r\n" +
                "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />\r\n" +
                "<style>\r\n" +
                "body {color: [FontColor]; font-size:1em; font-family: Segoe UI, Arial, sans-serif}" +
                "table {border-collapse: collapse; font-size:0.8em; }" +
                "table,th,td {border: 1px solid #aaaaaa; }" +
                "table th {padding:3px; color:[HeaderFontColor]; vertical-align: bottom; text-align: center;}" +
                "th span {-ms-writing-mode: tb-rl;-webkit-writing-mode: vertical-rl;writing-mode: vertical-rl;transform: rotate(180deg);white-space: nowrap;}" +
                "table td {padding:3px; }" +
                "td:nth-child(n+2) {text-align:center;}" +
                "td:first-child {background: white; position: -webkit-sticky; /* for Safari */ position: sticky; left: 0; z-index: 9998;}" +
                "th:nth-child(1) {text-align:left;}" +
                "th:first-child {left:0; z-index: 9999; }" +
                "th {background-color: Black !important; position:-webkit-sticky; /* for Safari */ position: sticky; top: 0;}" +
                "tr:nth-child(2n+3) {background-color:[ResRowBack] !important;}" +
                "tr:nth-child(2n+2) {background-color:[ResRowBack2] !important;}" +
                "td.fill {background-color: #c1946c !important;}" +
                ".topspacing { margin-top:10px; }" +
                ".disabled { color:#CCC; }" +
                ".clearfix { overflow: auto; }" +
                ".namediv { float:left; vertical-align:middle; }" +
                ".typediv { float:right; vertical-align:middle; font-size:0.6em; }" +
                ".warningbanner {background-color:Orange !important; border-radius:5px 5px 5px 5px; color:Black; padding:5px; font-weight:bold; margin-bottom:10px;margin-top:10px; }" +
                ".errorbanner {background-color:Red !important; border-radius:5px 5px 5px 5px; color:Black; padding:5px; font-weight:bold; margin-bottom:10px;margin-top:10px; }" +
                ".defaultbanner { background-color:[ContDefaultBanner] !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".defaultcontent {background-color:[ContDefaultBack] !important; border-radius:0px 0px 5px 5px; border-color:[ContDefaultBanner]; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                "@media print { body { -webkit - print - color - adjust: exact; }}" +
                ".rotate {/* FF3.5+ */ -moz - transform: rotate(-90.0deg); /* Opera 10.5 */ -o - transform: rotate(-90.0deg); /* Saf3.1+, Chrome */ -webkit - transform: rotate(-90.0deg); /* IE6,IE7 */ filter: progid: DXImageTransform.Microsoft.BasicImage(rotation = 0.083); /* IE8 */ -ms - filter: \"progid:DXImageTransform.Microsoft.BasicImage(rotation=0.083)\"; /* Standard */ transform: rotate(-90.0deg);} " +
                ".scrollcontainer {overflow: auto; }" +
                ".wrapper {overflow: hidden; display:grid; grid-template-rows: auto, auto, 1fr; position: absolute; width: calc(100vw - 20px); height: calc(100vh -20px); grid-gap: 15px; padding: 10px; top: 0; left: 0; }" +
                ".r1 {grid-row: 1; }" +
                ".r2 {grid-row: 2; }" +
                ".r3 {grid-row: 3; }" +
 //               "*{box-sizing: border-box; padding: 0; margin: 0;}" +
                "html {height 100%;}" +
                "\r\n</style>\r\n<!-- graphscript --></ head>\r\n<body>";

            // apply theme based settings
            if (!darkTheme)
            {
                // light theme
                html = html.Replace("[FontColor]", "black");
                html = html.Replace("[HeaderFontColor]", "white");

                // resources
                html = html.Replace("[ResRowBack]", "#fdfdfd");
                html = html.Replace("[ResRowBack2]", "white");
                html = html.Replace("[ResContBack]", "floralwhite");
                html = html.Replace("[ResContBackLight]", "white");
                html = html.Replace("[ResContBackDark]", "floralwhite");
                html = html.Replace("[ResFontBanner]", "white");
                html = html.Replace("[ResFontContent]", "black");

                html = html.Replace("[ContDefaultBack]", "#FAFAFA");
                html = html.Replace("[ContDefaultBanner]", "#000");
            }
            else
            {
                // dark theme
                html = html.Replace("[FontColor]", "#E5E5E5");
                html = html.Replace("[HeaderFontColor]", "black");

                // resources
                html = html.Replace("[ResRowBack]", "#281A0E");
                html = html.Replace("[ResRowBack2]", "#3F2817");
                html = html.Replace("[ResContBack]", "#281A0E");
                html = html.Replace("[ResContBackLight]", "#3F2817");
                html = html.Replace("[ResContBackDark]", "#281A0E");
                html = html.Replace("[ResFontBanner]", "#ffffff");
                html = html.Replace("[ResFontContent]", "#ffffff");

                html = html.Replace("[ContDefaultBack]", "#282828");
                html = html.Replace("[ContDefaultBanner]", "#686868");
            }

            using (StringWriter htmlString = new StringWriter())
            {
                htmlString.WriteLine(html);
                htmlString.WriteLine("\r\n<div class=\"wrapper\">");
                htmlString.WriteLine("\r\n<div class=\"r1\"><span style=\"font-size:0.8em; font-weight:bold\">You will need to keep refreshing this page after each run of the simulation to see changes relating to the activities performed status</span></div>");

                htmlString.Write("\r\n<div class=\"r2\"><div class=\"clearfix defaultbanner\">");
                htmlString.Write($"<div class=\"namediv\">Report activities performed</div>");
                htmlString.Write($"<div class=\"typediv\">Details</div>");
                htmlString.Write("</div>");
                htmlString.Write("\r\n<div class=\"defaultcontent\">");
                htmlString.Write($"\r\n<div class=\"activityentry\">Summary last created on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()}<br />");
                htmlString.WriteLine("\r\n</div>");
                htmlString.WriteLine("\r\n</div>");
                htmlString.WriteLine("\r\n</div>");

                if (data != null)
                {
                    if (RotateReport)
                    {
                        data = Transpose(data);
                    }
                    htmlString.WriteLine($"<div class=\"r3 scrollcontainer\"><table><tr>");
                    bool first = true;
                    foreach (DataColumn col in data.Columns)
                    {
                        if (col.ColumnName != " ")
                        {
                            string splitter = (RotateReport) ? "\\" : "<br />";
                            htmlString.Write($"<th>{((RotateReport & !first) ? "<span>" : "")}{col.ColumnName.Replace("\r\n", splitter)}{((RotateReport & !first) ? "</span>" : "")}</th>");
                            first = false; 
                        }
                    }
                    htmlString.WriteLine($"</tr>");

                    foreach (DataRow row in data.Rows)
                    {
                        htmlString.WriteLine($"<tr>");
                        foreach (var item in row.ItemArray)
                        {
                            if (item.ToString() != " ")
                            {
                                string splitter = "\\";
                                string image = "";
                                switch (item.ToString())
                                {
                                    case "Success":
                                    case "NoTask":
                                    case "NotNeeded":
                                    case "Timer":
                                    case "Calculation":
                                    case "Critical":
                                    case "Partial":
                                    case "Ignore":
                                        image = $"ActivitiesReport{item.ToString()}Web";
                                        break;
                                    case "Warning":
                                        image = $"ActivitiesReportIgnoreWeb";
                                        break;
                                    case "Ignored":
                                        image = "ActivitiesReportBlankWeb";
                                        break;
                                    default:
                                        image = "";
                                        break;
                                }

                                if (image == "")
                                {
                                    htmlString.Write($"<td>{item.ToString().Replace("\r\n", splitter)}</td>");
                                }
                                else
                                {
                                    htmlString.Write($"<td><img src=\"http:////www.apsim.info/clem/Content/Resources/Images/IconsSVG/{image}.png\"</td>");
                                } 
                            }
                        }
                        htmlString.WriteLine($"</tr>");
                    }
                    htmlString.WriteLine($"</table></div></div>");
                }

                if (CreateHTML | AutoCreateHTML)
                {
                    // System.IO.File.WriteAllText(Path.Combine(directoryPath, this.HtmlOutputFilename), htmlString.ToString());
                }
            }
        }

        #endregion

        #region descriptive summary
        /// <summary>
        /// 
        /// </summary>
        public HTMLSummaryStyle ModelSummaryStyle { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formatForParentControl"></param>
        /// <returns></returns>
        public string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (CreateHTML)
                {
                    htmlWriter.Write($"<div>A HTML version of this report is available. See Summary tab for current link");
                    if(RotateReport)
                    {
                        htmlWriter.Write($" with months as columns and activities as rows.</div>");
                    }
                    else
                    {
                        htmlWriter.Write($" with months as rows and activities as columns.</div>");
                    }
                }
                else
                {
                    htmlWriter.Write($"<div>No HTML version of this report is provided for viewing in browser.</div>");
                }
                if (AutoCreateHTML)
                {
                    htmlWriter.Write($"<div>A HTML version of this report will automatically be created for its parent CLEMZone and named the same as the simulation file with a html extension</div>");
                }
                return htmlWriter.ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="formatForParentControl"></param>
        /// <param name="htmlString"></param>
        /// <returns></returns>
        public string GetFullSummary(object model, bool formatForParentControl, string htmlString)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (model is ICLEMDescriptiveSummary)
                {
                    ICLEMDescriptiveSummary cm = model as ICLEMDescriptiveSummary;
                    htmlWriter.Write(cm.ModelSummaryOpeningTags(formatForParentControl));

                    htmlWriter.Write(cm.ModelSummaryInnerOpeningTagsBeforeSummary());

                    htmlWriter.Write(cm.ModelSummary(formatForParentControl));

                    htmlWriter.Write(cm.ModelSummaryInnerOpeningTags(formatForParentControl));

                    foreach (var item in (model as IModel).Children)
                    {
                        htmlWriter.Write(GetFullSummary(item, true, htmlString));
                    }
                    htmlWriter.Write(cm.ModelSummaryInnerClosingTags(formatForParentControl));

                    htmlWriter.Write(cm.ModelSummaryClosingTags(formatForParentControl));
                }
                return htmlWriter.ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formatForParentControl"></param>
        /// <returns></returns>
        public string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "\r\n</div>\r\n</div>";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formatForParentControl"></param>
        /// <returns></returns>
        public string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            string overall = "default";
            string extra = "";

            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"holder" + ((extra == "") ? "main" : "sub") + " " + overall + "\" style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + ";\">");
                htmlWriter.Write("\r\n<div class=\"clearfix " + overall + "banner" + extra + "\">" + this.ModelSummaryNameTypeHeader() + "</div>");
                htmlWriter.Write("\r\n<div class=\"" + overall + "content" + ((extra != "") ? extra : "") + "\">");

                return htmlWriter.ToString();
            }
        }

        /// <summary>
        /// Returns the opacity value for this component in the summary display
        /// </summary>
        public double SummaryOpacity(bool formatForParent) => ((!this.Enabled & (!formatForParent | (formatForParent & this.Parent.Enabled))) ? 0.4 : 1.0);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formatForParentControl"></param>
        /// <returns></returns>
        public string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formatForParentControl"></param>
        /// <returns></returns>
        public string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ModelSummaryInnerOpeningTagsBeforeSummary()
        {
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ModelSummaryNameTypeHeader()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"namediv\">" + this.Name + ((!this.Enabled) ? " - DISABLED!" : "") + "</div>");
                if (this.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
                {
                    htmlWriter.Write("<div class=\"partialdiv\"");
                    htmlWriter.Write(">");
                     htmlWriter.Write("</div>");
                }
                htmlWriter.Write("<div class=\"typediv\">" + this.GetType().Name + "</div>");
                return htmlWriter.ToString();
            }
        }
        #endregion
    }
}
