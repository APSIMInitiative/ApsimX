using Models;
using Models.Core;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter for displaying simulation html formatted messages
    /// </summary>
    public class MessagePresenter : IPresenter
    {
        /// <summary>
        /// The model
        /// </summary>
        private Model model;

        /// <summary>
        /// The view to use
        /// </summary>
        private IHTMLView genericView;

        /// <summary>
        /// The explorer
        /// </summary>
        private ExplorerPresenter explorerPresenter;


        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.genericView = view as IHTMLView;
            this.explorerPresenter = explorerPresenter;
            this.genericView.SetContents(CreateHTML(), false, false);
        }

        private string CreateHTML()
        {
            int maxErrors = 100;
            string htmlString = "<!DOCTYPE html>\n" +
                "<html>\n<head>\n<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />\n<style>\n" +
                "body {color: [FontColor]; max-width:1000px; font-size:10pt;}" + 
                ".errorbanner {background-color:red !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".errorcontent {background-color:[ContError] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:red; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".warningbanner {background-color:orange !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".warningcontent {background-color:[ContWarn] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:orange; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".messagebanner {background-color:CornflowerBlue !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".messagecontent {background-color:[ContMessage] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:CornflowerBlue; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".okbanner {background-color:green !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".okcontent {background-color:[ContOK] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:green; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".holdermain {margin: 20px 0px 20px 0px}" +
                ".resourcelink {color:#996633; font-weight:bold; background-color:Cornsilk !important;border-color:#996633; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".activitylink {color:#009999; font-weight:bold; background-color:floralwhite !important;border-color:#009999; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".filterlink {border-color:#cc33cc; background-color:#f2e2f2 !important; color:#cc33cc; border-width:1px; border-style:solid; padding: 0px 5px 0px 5px; font-weight:bold; border-radius:3px;}" +
                ".filelink {color:green; font-weight:bold; background-color:mintcream !important;border-color:green; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".errorlink {color:white; font-weight:bold; background-color:red !important;border-color:darkred; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".setvalue {font-weight:bold; background-color: [ValueSetBack] !important; Color: [ValueSetFont]; border-color:#697c7c; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px;}" +
                ".otherlink {font-weight:bold; color:#333333; background-color:#eeeeee !important;border-color:#999999; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px;}" +
                ".marketlink {font-weight:bold; color:#1785FF; background-color:#DCEEFF !important;border-color:#1785FF; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px;}" +
                ".messageentry {padding:5px 0px 5px 0px; line-height: 1.7em; }" +
                ".holdermain {margin: 20px 0px 20px 0px}" +
                "@media print { body { -webkit - print - color - adjust: exact; }}" +
                "\n</style>\n</head>\n<body>";

            // apply theme based settings
            if (!Utility.Configuration.Settings.DarkTheme)
            {
                // light theme
                htmlString = htmlString.Replace("[FontColor]", "#000000");

                htmlString = htmlString.Replace("[ContError]", "#FFFAFA");
                htmlString = htmlString.Replace("[ContWarn]", "#FFFFFA");
                htmlString = htmlString.Replace("[ContMessage]", "#FAFAFF");
                htmlString = htmlString.Replace("[ContOK]", "#FAFFFF");
                // values
                htmlString = htmlString.Replace("[ValueSetBack]", "#e8fbfc");
                htmlString = htmlString.Replace("[ValueSetFont]", "#000000");
            }
            else
            {
                // dark theme
                htmlString = htmlString.Replace("[FontColor]", "#E5E5E5");

                htmlString = htmlString.Replace("[ContError]", "#490000");
                htmlString = htmlString.Replace("[ContWarn]", "#A35D00");
                htmlString = htmlString.Replace("[ContMessage]", "#030028");
                htmlString = htmlString.Replace("[ContOK]", "#0C440C");
                // values
                htmlString = htmlString.Replace("[ValueSetBack]", "#49adc4");
                htmlString = htmlString.Replace("[ValueSetFont]", "#0e2023");

            }

            // find IStorageReader of simulation
            IModel simulation = Apsim.Parent(model, typeof(Simulation));
            IModel simulations = Apsim.Parent(simulation, typeof(Simulations));
            IDataStore ds = Apsim.Children(simulations, typeof(IDataStore)).FirstOrDefault() as IDataStore;
            if (ds == null)
            {
                return htmlString;
            }
            if(ds.Reader.GetData(simulationName: simulation.Name, tableName: "_Messages") == null)
            {
                return htmlString;
            }
            DataRow[] dataRows = ds.Reader.GetData(simulationName: simulation.Name, tableName: "_Messages").Select();
            if (dataRows.Count() > 0)
            {
                int errorCol = dataRows[0].Table.Columns["MessageType"].Ordinal;  //7; // 8;
                int msgCol = dataRows[0].Table.Columns["Message"].Ordinal;  //6; // 7;
                dataRows = ds.Reader.GetData(simulationName: simulation.Name, tableName: "_Messages").Select().OrderBy(a => a[errorCol].ToString()).ToArray();

                foreach (DataRow dr in dataRows)
                {
                    // convert invalid parameter warnings to errors
                    if(dr[msgCol].ToString().StartsWith("Invalid parameter value in model"))
                    {
                        dr[errorCol] = "0";
                    }
                }

                foreach (DataRow dr in dataRows.Take(maxErrors))
                {
                    bool ignore = false;
                    string msgStr = dr[msgCol].ToString();
                    if (msgStr.Contains("@i:"))
                    {
                        ignore = true;
                    }

                    if (!ignore)
                    {
                        // trim first two rows of error reporting file and simulation.
                        List<string> parts = new List<string>( msgStr.Split('\n'));
                        if(parts[0].Contains("ERROR in file:"))
                        {
                            parts.RemoveAt(0);
                        }
                        if (parts[0].Contains("ERRORS in file:"))
                        {
                            parts.RemoveAt(0);
                        }
                        if (parts[0].Contains("Simulation name:"))
                        {
                            parts.RemoveAt(0);
                        }
                        msgStr = string.Join("\n", parts.Where(a => a.Trim(' ').StartsWith("at ") == false).ToArray());

                        // remove starter text
                        string[] starters = new string[]
                        {
                            "System.Exception: ",
                            "Models.Core.ApsimXException: "
                        };

                        foreach (string start in starters)
                        {
                            if (msgStr.Contains(start))
                            {
                                msgStr = msgStr.Substring(start.Length);
                            }
                        }

                        string type = "Message";
                        string title = "Message";
                        switch (dr[errorCol].ToString())
                        {
                            case "2":
                                type = "Error";
                                title = "Error";
                                break;
                            case "1":
                                type = "Warning";
                                title = "Warning";
                                break;
                            default:
                                break;
                        }
                        if (msgStr.IndexOf(':') >= 0 && msgStr.StartsWith("@"))
                        {
                            switch (msgStr.Substring(0, msgStr.IndexOf(':')))
                            {
                                case "@error":
                                    type = "Error";
                                    title = "Error";
                                    msgStr = msgStr.Substring(msgStr.IndexOf(':') + 1);
                                    break;
                                case "@validation":
                                    type = "Error";
                                    title = "Validation error";
                                    msgStr = msgStr.Replace("PARAMETER:", "<b>Parameter:</b>");
                                    msgStr = msgStr.Replace("DESCRIPTION:", "<b>Description:</b>");
                                    msgStr = msgStr.Replace("PROBLEM:", "<b>Problem:</b>");
                                    msgStr = msgStr.Substring(msgStr.IndexOf(':') + 1);
                                    break;
                            }
                        }
                        if (msgStr.Contains("terminated normally"))
                        {
                            type = "Ok";
                            title = "Success";
                            DataTable dataRows2 = ds.Reader.GetDataUsingSql("Select * FROM _InitialConditions WHERE Name = 'Run on'"); // (simulationName: simulation.Name, tableName: "_InitialConditions");
                            int clockCol = dataRows2.Columns["Value"].Ordinal;  // 8;
                            DateTime lastrun = DateTime.Parse(dataRows2.Rows[0][clockCol].ToString());
                            msgStr = "Simulation successfully completed at [" + lastrun.ToShortTimeString() + "] on [" + lastrun.ToShortDateString() + "]";
                        }

                        htmlString += "\n<div class=\"holdermain\">";
                        htmlString += "\n<div class=\"" + type.ToLower() + "banner\">" + title + "</div>";
                        htmlString += "\n<div class=\"" + type.ToLower() + "content\">";
                        msgStr = msgStr.Replace("\n", "<br />");
                        msgStr = msgStr.Replace("]", "</span>");
                        msgStr = msgStr.Replace("[r=", "<span class=\"resourcelink\">");
                        msgStr = msgStr.Replace("[a=", "<span class=\"activitylink\">");
                        msgStr = msgStr.Replace("[f=", "<span class=\"filterlink\">");
                        msgStr = msgStr.Replace("[x=", "<span class=\"filelink\">");
                        msgStr = msgStr.Replace("[o=", "<span class=\"otherlink\">");
                        msgStr = msgStr.Replace("[m=", "<span class=\"marketlink\">");
                        msgStr = msgStr.Replace("[", "<span class=\"setvalue\">");
                        htmlString += "\n<div class=\"messageentry\">" + msgStr;
                        htmlString += "\n</div>";
                        htmlString += "\n</div>";
                        htmlString += "\n</div>";
                    }
                }
                if(dataRows.Count() > maxErrors)
                {
                    htmlString += "\n<div class=\"holdermain\">";
                    htmlString += "\n <div class=\"warningbanner\">Warning limit reached</div>";
                    htmlString += "\n <div class=\"warningcontent\">";
                    htmlString += "\n  <div class=\"activityentry\">In excess of "+maxErrors+" errors and warnings were generated. Only the first " + maxErrors + " are displayes here. PLease refer to the SummaryInformation for the full list of issues.";
                    htmlString += "\n  </div>";
                    htmlString += "\n </div>";
                    htmlString += "\n</div>";
                }
            }
            else
            {
                htmlString += "\n<div class=\"holdermain\">";
                htmlString += "\n <div class=\"messagebanner\">Message</div>";
                htmlString += "\n <div class=\"messagecontent\">";
                htmlString += "\n  <div class=\"activityentry\">This simulation has not been performed";
                htmlString += "\n  </div>";
                htmlString += "\n </div>";
                htmlString += "\n</div>";
            }
            htmlString += "\n</body>\n</html>";
            return htmlString;
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }

    }
}
