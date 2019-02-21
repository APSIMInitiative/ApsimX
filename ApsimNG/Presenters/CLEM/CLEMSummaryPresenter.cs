
namespace UserInterface.Presenters
{
    using System.Collections.Generic;
    using System.Text;
    using Models.CLEM;
    using Models.Core;
    using Views;

    /// <summary>
    /// Presenter to provide HTML description summary for CLEM models
    /// </summary>
    public class CLEMSummaryPresenter : IPresenter
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
            RefreshSummary();
        }

        public void RefreshSummary()
        {
            string htmlString = "<!DOCTYPE html>\n" +
                "<html>\n<head>\n<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />\n<style>\n" +
                "body {font-family: sans-serif, Arial, Helvetica; max-width:1000px; font-size:10pt;}" +
                "table {border-collapse: collapse; font-size:0.8em; }" +
                ".resource table,th,td {border: 1px solid #996633; }" +
                "table th {padding:8px; }" +
                "table td {padding:8px; }" +
                " td:nth-child(n+2) {text-align:center;}" +
                " th:nth-child(1) {text-align:left;}" +
                ".resource th {background-color: #996633 !important; color: white;}" +
                ".resource tr:nth-child(2n+3) {background:floralwhite !important;}" +
                ".resource tr:nth-child(2n+2) {background:white !important;}" +
                ".resource td.fill {background-color: #c1946c !important;color: white;}" +
                ".resourcebanner {background-color:#996633 !important; color:white; padding:5px; font-weight:bold; border-radius:5px 5px 0px 0px; }" +
                ".resourceborder {border-color:#996633; border-width:1px; border-style:solid; padding:0px; background-color:Cornsilk !important; }" +
                ".resourcelink {color:#996633; font-weight:bold; background-color:Cornsilk !important; border-color:#996633; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".activity th,td {padding:5px; }" +
                ".activity table,th,td {border: 1px solid #996633; }" +
                ".activity th {background-color: #996633 !important; color: white;}" +
                ".activity td.fill {background-color: #996633 !important; color: white;}" +
                ".activity table {border-collapse: collapse; font-size:0.8em; }" +
                ".activityarea {padding:10px; }" +
                ".topspacing { margin-top:10px; }" +
                ".disabled { color:#CCC; }" +
                ".clearfix { overflow: auto; }" +
                ".namediv { float:left; vertical-align:middle; }" +
                ".typediv { float:right; vertical-align:middle; font-size:0.6em; }" +
                ".partialdiv { font-size:0.8em; float:right; text-transform: uppercase; color:white; font-weight:bold; vertical-align:middle; border-color:white; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; margin-left: 10px;  border-radius:3px; }" +
                ".filelink {color:green; font-weight:bold; background-color:mintcream !important; border-color:green; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".errorlink {color:white; font-weight:bold; background-color:red !important; border-color:darkred; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".setvalue {font-weight:bold; background-color:#e8fbfc !important; border-color:#697c7c; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px;}" +
                ".folder {color:#666666; font-style: italic; font-size:1.1em; }" +
                ".cropmixedlabel {color:#666666; font-style: italic; font-size:1.1em; padding: 5px 0px 10px 0px; }" +
                ".croprotationlabel {color:#666666; font-style: italic; font-size:1.1em; padding: 5px 0px 10px 0px; }" +
                ".cropmixedborder {border-color:#86b2b1; background-color:white !important; border-width:1px; border-style:solid; padding:0px 10px 0px 10px; margin-bottom:5px; }" +
                ".croprotationborder {border-color:#86b2b1; background-color:white !important; border-width:1px; border-style:solid; padding:0px 10px 0px 10px; margin-bottom:5px; }" +
                ".labourgroupsborder {border-color:#996633; background-color:white !important; border-width:1px; border-style:solid; padding:10px; margin-bottom:5px; margin-top:5px;}" +
                ".activitygroupsborder {border-color:#86b2b1; background-color:white !important; border-width:1px; border-style:solid; padding:10px; margin-bottom:5px; margin-top:5px;}" +
                ".labournote {font-style: italic; color:#666666; padding-top:7px;}" +
                ".filterborder {display: block; width: 100% - 40px; border-color:#cc33cc; background-color:#f2e2f2 !important; border-width:1px; border-style:solid; padding:5px; margin:10px 0px 5px 0px; border-radius:5px; }" +
                ".filter {float: left; border-color:#cc33cc; background-color:#cc33cc !important; color:white; border-width:1px; border-style:solid; padding: 0px 5px 0px 5px; font-weight:bold; margin: 0px 5px 0px 5px;  border-radius:3px;}" +
                ".filtererror {float: left; border-color:red; background-color:red !important; color:white; border-width:1px; border-style:solid; padding: 0px 5px 0px 5px; font-weight:bold; margin: 0px 5px 0px 5px;  border-radius:3px;}" +
                ".activity h1 {color:#009999; } .activity h1,h2,h3 { color:#009999; margin-bottom:5px; }" +
                ".activityborder {border-color:#009999; border-width:2px; border-style:none none none solid; padding:0px 0px 0px 10px; margin-bottom:15px; }" +
                ".activityborderfull {border-color:#009999; border-radius:5px; background-color:#f0f0f0 !important; border-width:1px; border-style:solid; margin-bottom:40px; }" +
                ".activitybanner {background-color:#009999 !important; border-radius:5px 5px 0px 0px; color:#f0f0f0; padding:5px; font-weight:bold }" +
                ".activitybannerlight {background-color:#86b2b1 !important; border-radius:5px 5px 0px 0px; color:white; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".activitybannerdark {background-color:#009999 !important; border-radius:5px 5px 0px 0px; color:white; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".activitybannercontent {background-color:#86b2b1 !important; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:5px; }" +
                ".activitycontent {background-color:#f0f0f0 !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#86b2b1; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".activitycontentlight {background-color:white !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#86b2b1; border-width:0px 1px 1px 1px; border-style:solid; padding:10px;}" +
                ".activitycontentdark {background-color:#f0f0f0 !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#86b2b1; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".activitypadding {padding:10px; }" +
                ".activityentry {padding:5px 0px 5px 0px; }" +
                ".resource h1,h2,h3 {color:#996633; } .activity h1,h2,h3 { color:#009999; margin-bottom:5px; }" +
                ".resourcebannerlight {background-color:#c1946c !important; border-radius:5px 5px 0px 0px; color:white; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".resourcebannerdark {background-color:#996633 !important; color:white; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".resourcecontent {background-color:floralwhite !important; margin-bottom:40px; border-radius:0px 0px 5px 5px; border-color:#996633; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcecontentlight {background-color:white !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#c1946c; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcecontentdark {background-color:floralwhite !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#996633; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".filebanner {background-color:green !important; border-radius:5px 5px 0px 0px; color:mintcream; padding:5px; font-weight:bold }" +
                ".filecontent {background-color:#FCFFFC !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:green; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".defaultbanner {background-color:black !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".defaultcontent {background-color:#FAFAFA !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:black; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".holdermain {margin: 20px 0px 20px 0px}" +
                ".holdersub {margin: 5px 0px 5px}" +
                "@media print { body { -webkit - print - color - adjust: exact; }}"+
                "\n</style>\n</head>\n<body>";
            if (model.GetType() == typeof(ZoneCLEM))
            {
                htmlString += (model as ZoneCLEM).GetFullSummary(model, true, htmlString);
            }
            else
            {
                htmlString += (model as CLEMModel).GetFullSummary(model, false, htmlString);
            }
            htmlString += "\n</body>\n</html>";

            this.genericView.SetContents(htmlString, false, false);
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }
    }
}

