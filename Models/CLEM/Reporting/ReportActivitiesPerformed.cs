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
using Newtonsoft.Json;

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
    public class ReportActivitiesPerformed : Report, ICLEMDescriptiveSummary, ICLEMUI, ISpecificOutputFilename
    {
        /// <summary>
        /// Includes folders in simulation as placeholders in output
        /// </summary>
        [Description("Include folders in output")]
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludeFolders { get; set; }

        /// <summary>
        /// The style timers are handled in report
        /// </summary>
        [Description("Style of handling timers")]
        [System.ComponentModel.DefaultValue(ReportActivitiesPerformedTimerHandleStyle.InPosition)]
        public ReportActivitiesPerformedTimerHandleStyle HandleTimers { get; set; }

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

        /// <inheritdoc/>
        public string SelectedTab { get; set; }

        /// <summary>
        /// Name of filename to save labour report
        /// </summary>
        public string HtmlOutputFilename { get { return "ActivitiesPerformedSummary.html"; } }

        /// <inheritdoc/>
        [JsonIgnore]
        public DescriptiveSummaryMemoReportingType ReportMemosType { get; set; } = DescriptiveSummaryMemoReportingType.InPlace;

        /// <summary>
        /// Constructor
        /// </summary>
        public ReportActivitiesPerformed()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Default;
            CLEMModel.SetPropertyDefaults(this);
        }

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            base.VariableNames = new string[]
            {
                "[Clock].Today as Date",
                "[Activities].LastActivityPerformed.Name as Name",
                "[Activities].LastActivityPerformed.Status as Status",
                "[Activities].LastActivityPerformed.Id as UniqueID",
                "[Activities].LastActivityPerformed.StatusMessage as Message",
                "[Activities].LastActivityPerformed.ModelType as Type",
            };

            EventNames = new string[] { "[Activities].ActivityPerformed" };
            SubscribeToEvents();
        }

        #region create html report

        /// <summary>
        /// Get the data for display
        /// </summary>
        /// <param name="dataStore">The datastore to use</param>
        /// <returns>Data as a datatable</returns>
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
                            .Where(row => row.Field<String>("SimulationName") == simName & row.Field<String>("Zone") == zoneName
                            & (IncludeFolders || (row.Field<int>("Type") != 1))
                            & (HandleTimers != ReportActivitiesPerformedTimerHandleStyle.Ignore || ((row.Field<string>("Name") == "TimeStep" | row.Field<int>("Type") != 2)) )  );
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

        /// <summary>
        /// Method to transpose columns
        /// </summary>
        /// <param name="dt">Data as DataTable</param>
        /// <returns>Transposed DataTable</returns>
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
        /// Method to create data table
        /// </summary>
        public DataTable CreateDataTable(IDataStore dataStore, string directoryPath, bool darkTheme)
        {
            using (DataTable data = GetData(dataStore))
            {
                DataTable tbl = null;
                if (data != null && data.Rows.Count > 0)
                {
                    // get unique rows
                    List<string> activities;
                    switch (HandleTimers)
                    {
                        case ReportActivitiesPerformedTimerHandleStyle.PlaceAtStart:
                            activities = data.AsEnumerable().OrderByDescending(a => a.Field<int>("Type") == (int)ActivityPerformedType.Timer).ThenBy(a => a.Field<string>("UniqueID")).Select(a => a.Field<string>("UniqueID")).Distinct().ToList<string>();
                            break;
                        case ReportActivitiesPerformedTimerHandleStyle.PlaceAtEnd:
                            activities = data.AsEnumerable().OrderBy(a => a.Field<int>("Type") == (int)ActivityPerformedType.Timer).ThenBy(a => a.Field<string>("UniqueID")).Select(a => a.Field<string>("UniqueID")).Distinct().ToList<string>();
                            break;
                        default:
                            activities = data.AsEnumerable().Select(a => a.Field<string>("UniqueID")).Distinct().OrderBy(a => a).ToList<string>();
                            break;
                    }
                    string timeStepUID = data.AsEnumerable().Where(a => a.Field<string>("Name") == "TimeStep").FirstOrDefault().Field<string>("UniqueID");

                    // get unique columns
                    List<DateTime> dates = data.AsEnumerable().Select(a => a.Field<DateTime>("Date")).Distinct().ToList<DateTime>();

                    // create table
                    tbl = new DataTable();
                    tbl.Columns.Add("Activity");
                    foreach (var item in dates)
                    {
                        if(item.Day != DateTime.DaysInMonth(item.Year, item.Month))
                            tbl.Columns.Add("00\r\n" + item.ToString("yy"));
                        else
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
                                string tooltip = activityTick["Message"].ToString();

                                string monthID = "00";
                                if (dte.Day == DateTime.DaysInMonth(dte.Year, dte.Month))
                                    monthID = dte.Month.ToString("00");

                                if(!(monthID == "00" && status == "Timer"))
                                    dr[monthID + "\r\n" + dte.ToString("yy")] = $"{status}:{tooltip}";
                            }
                            dr[" "] = " ";
                            tbl.Rows.Add(dr);
                        }
                    }
                }
                if(CreateHTML)
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
                "table td {padding:3px; position: relative;}" +
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
                "html {height 100%;}" +
                ".note { position: relative;}" +
                ".note:after { /* Magic Happens Here!!! */ content: \"\"; position: absolute; top: 0; right: 0; width: 0; height: 0; display: block; border-left: 8px solid transparent; border-bottom: 8px solid transparent; border-top: 8px solid #f00;} /* </magic> */" +
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
                htmlString.Write($"<div class=\"namediv\">Report activities performed</div><br />");
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
                                var statusParts = item.ToString().Split(':');

                                string image = "";
                                switch (statusParts[0])
                                {
                                    case "Success":
                                    case "NoTask":
                                    case "NotNeeded":
                                    case "Timer":
                                    case "Calculation":
                                    case "Critical":
                                    case "Partial":
                                    case "Ignore":
                                        image = $"ActivitiesReport{statusParts[0]}Web";
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
                                    htmlString.Write($"<td>{statusParts[0].Replace("\r\n", splitter)}</td>");
                                }
                                else
                                {
                                    htmlString.Write($"<td title=\"{statusParts[1]}\" class=\"{(statusParts[1].Any()? "note":"")}\"><img src=\"http:////www.apsim.info/clem/Content/Resources/Images/IconsSVG/{image}.png\"></td>");
                                }

                                //string image = "";
                                //switch (item.ToString())
                                //{
                                //    case "Success":
                                //    case "NoTask":
                                //    case "NotNeeded":
                                //    case "Timer":
                                //    case "Calculation":
                                //    case "Critical":
                                //    case "Partial":
                                //    case "Ignore":
                                //        image = $"ActivitiesReport{item.ToString()}Web";
                                //        break;
                                //    case "Warning":
                                //        image = $"ActivitiesReportIgnoreWeb";
                                //        break;
                                //    case "Ignored":
                                //        image = "ActivitiesReportBlankWeb";
                                //        break;
                                //    default:
                                //        image = "";
                                //        break;
                                //}

                                //if (image == "")
                                //{
                                //    htmlString.Write($"<td>{item.ToString().Replace("\r\n", splitter)}</td>");
                                //}
                                //else
                                //{
                                //    htmlString.Write($"<td><img src=\"http:////www.apsim.info/clem/Content/Resources/Images/IconsSVG/{image}.png\" tooltip=\"{"None"}\"></td>");
                                //}


                            }
                        }
                        htmlString.WriteLine($"</tr>");
                    }
                    htmlString.WriteLine($"</table></div></div>");
                }

                if (CreateHTML | AutoCreateHTML)
                {
                    System.IO.File.WriteAllText(Path.Combine(directoryPath, this.HtmlOutputFilename), htmlString.ToString());
                }
            }
        }

        #endregion

        #region descriptive summary

        ///<inheritdoc/>
        public HTMLSummaryStyle ModelSummaryStyle { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public List<string> CurrentAncestorList { get; set; } = new List<string>();

        /// <inheritdoc/>
        public bool FormatForParentControl { get { return CurrentAncestorList.Count > 1; } }

        ///<inheritdoc/>
        public string ModelSummary()
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

        ///<inheritdoc/>
        public string GetFullSummary(IModel model, List<string> parentControls, string htmlString, Func<string, string> markdown2Html = null)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (model is ICLEMDescriptiveSummary)
                {
                    ICLEMDescriptiveSummary cm = model as ICLEMDescriptiveSummary;
                    cm.CurrentAncestorList = parentControls.ToList();
                    cm.CurrentAncestorList.Add(model.GetType().Name);

                    htmlWriter.Write(cm.ModelSummaryOpeningTags());

                    htmlWriter.Write(cm.ModelSummaryInnerOpeningTagsBeforeSummary());

                    htmlWriter.Write(cm.ModelSummary());

                    htmlWriter.Write(cm.ModelSummaryInnerOpeningTags());

                    foreach (var item in (model as IModel).Children)
                    {
                        htmlWriter.Write(GetFullSummary(item, cm.CurrentAncestorList, htmlString, markdown2Html));
                    }
                    htmlWriter.Write(cm.ModelSummaryInnerClosingTags());

                    htmlWriter.Write(cm.ModelSummaryClosingTags());
                }
                return htmlWriter.ToString();
            }
        }

        ///<inheritdoc/>
        public string ModelSummaryClosingTags()
        {
            return "\r\n</div>\r\n</div>";
        }

        ///<inheritdoc/>
        public string ModelSummaryOpeningTags()
        {
            string overall = "default";
            string extra = "";

            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"holder" + ((extra == "") ? "main" : "sub") + " " + overall + "\" style=\"opacity: " + SummaryOpacity(FormatForParentControl).ToString() + ";\">");
                htmlWriter.Write("\r\n<div class=\"clearfix " + overall + "banner" + extra + "\">" + this.ModelSummaryNameTypeHeader() + "</div>");
                htmlWriter.Write("\r\n<div class=\"" + overall + "content" + ((extra != "") ? extra : "") + "\">");

                return htmlWriter.ToString();
            }
        }

        ///<inheritdoc/>
        public double SummaryOpacity(bool formatForParent) => ((!this.Enabled & (!formatForParent | (formatForParent & this.Parent.Enabled))) ? 0.4 : 1.0);

        ///<inheritdoc/>
        public string ModelSummaryInnerClosingTags()
        {
            return "";
        }

        ///<inheritdoc/>
        public string ModelSummaryInnerOpeningTags()
        {
            return "";
        }

        ///<inheritdoc/>
        public string ModelSummaryInnerOpeningTagsBeforeSummary()
        {
            return "";
        }

        ///<inheritdoc/>
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

        ///<inheritdoc/>
        public string ModelSummaryNameTypeHeaderText()
        {
            return this.Name;
        }
        #endregion

    }

    /// <summary>
    /// Style of reporting timers
    /// </summary>
    public enum ReportActivitiesPerformedTimerHandleStyle
    {
        /// <summary>
        /// Do not include timers in report
        /// </summary>
        Ignore,
        /// <summary>
        /// Place timers are position in tree
        /// </summary>
        InPosition,
        /// <summary>
        /// Place all timers at start
        /// </summary>
        PlaceAtStart,
        /// <summary>
        /// Place all timers at end
        /// </summary>
        PlaceAtEnd
    }
}
