using APSIM.Shared.Utilities;
using Models.CLEM;
using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Interfaces;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter to display HTML report of labour allocation to activities
    /// </summary>
    public class LabourAllocationPresenter : IPresenter, ICLEMPresenter, IRefreshPresenter
    {
        /// <summary>
        /// The model
        /// </summary>
        private Model model;

        /// <summary>
        /// The view to use
        /// </summary>
        private IMarkdownView genericView;

        /// <summary>
        /// The explorer
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        private List<ValidParentAttribute> validpAtt = new List<ValidParentAttribute>();
        private int numberLabourTypes = 0;
        [NonSerialized]
        private Labour labour;
        [NonSerialized]
        private List<LabourType> labourList = new List<LabourType>();

        /// <summary>
        /// Attach inherited class additional presenters is needed
        /// </summary>
        public void AttachExtraPresenters(CLEMPresenter clemPresenter)
        {
            object newView = new MarkdownView(clemPresenter.View as ViewBase);
            IPresenter labourPresenter = new LabourAllocationPresenter();
            if (newView != null && labourPresenter != null)
            {
                clemPresenter.View.AddTabView("Display", newView);
                labourPresenter.Attach(clemPresenter.Model, newView, clemPresenter.ExplorerPresenter);
                clemPresenter.PresenterList.Add("Display", labourPresenter);
            }
        }

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.genericView = view as IMarkdownView;
            this.explorerPresenter = explorerPresenter;
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(this.explorerPresenter.ApsimXFile.FileName), (model as ISpecificOutputFilename).HtmlOutputFilename), CreateHTML());
            this.genericView.Text = CreateMarkdown();
        }

        public void Refresh()
        {
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(this.explorerPresenter.ApsimXFile.FileName), (model as ISpecificOutputFilename).HtmlOutputFilename), CreateHTML());
            this.genericView.Text = CreateMarkdown();
        }

        private string CreateHTML()
        {
            string htmlString = "<!DOCTYPE html>\n" +
                "<html>\n<head>\n<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />\n<style>\n" +
                "body {color: [FontColor]; max-width:1000px; font-size:1em; font-family: Segoe UI, Arial, sans-serif}" +
                "table {border-collapse: collapse; font-size:0.8em; }" +
                "table,th,td {border: 1px solid #aaaaaa; }" +
                "table th {padding:3px; color:[HeaderFontColor]; vertical-align: bottom; text-align: center;}" +
                "th span {-ms-writing-mode: tb-rl;-webkit-writing-mode: vertical-rl;writing-mode: vertical-rl;transform: rotate(180deg);white-space: nowrap;}" +
                "table td {padding:3px; }" +
                "td:nth-child(n+2) {text-align:center;}" +
                "th:nth-child(1) {text-align:left;}" +
                "th {background-color: Black !important; }" +
                "tr:nth-child(2n+3) {background:[ResRowBack] !important;}" +
                "tr:nth-child(2n+2) {background:[ResRowBack2] !important;}" +
                "td.fill {background-color: #c1946c !important;}" +
                "table.main {[TableBackground] }" +
                "table.main tr td.disabled {color: [DisabledColour]; }" +
                ".dot { margin:auto; display:block; height:20px; width:20px; line-height:20px; background-color:black; -moz-border-radius: 10px; border-radius: 10px; }" +
                ".dot1 { background-color:#F5793A; }" +
                ".dot2 { background-color:#A95AA1; }" +
                ".dot3 { background-color:#85C0F9; }" +
                ".dot4 { background-color:#0F2080; }" +
                ".warningbanner {background-color:orange; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".warningcontent {background-color:[WarningBackground]; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:orange; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".messagebanner {background-color:CornflowerBlue; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".messagecontent {background-color:[MessageBackground]; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:CornflowerBlue; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                "li {margin-bottom:10px;}" +
                "table.blank td {border: 0px none [GridColor]; }" +
                "table.blank {border: 0px none #009999; border-collapse: collapse; }" +
                "table th:first-child {text-align:left; }" +
                "table th:nth-child(n+2) { /* Safari */ - webkit - transform: rotate(-90deg); /* Firefox */ -moz - transform: rotate(-90deg); /* IE */ -ms - transform: rotate(-90deg); /* Opera */ -o - transform: rotate(-90deg); /* Internet Explorer */ filter: progid: DXImageTransform.Microsoft.BasicImage(rotation = 3);  }" +
                "table td:nth-child(n+2) { text-align:center; }" +
                ".clearfix { overflow: auto; }" +
                ".namediv { float:left; vertical-align:middle; }" +
                ".typediv { float:right; vertical-align:middle; font-size:0.6em; }" +
                ".holdermain {margin: 20px 0px 20px 0px}" +
                ".defaultbanner {background-color:[ContDefaultBanner] !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".defaultcontent {background-color:[ContDefaultBack] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:[ContDefaultBanner]; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                "@media print { body { -webkit - print - color - adjust: exact; }}" +
                ".rotate {/* FF3.5+ */ -moz - transform: rotate(-90.0deg); /* Opera 10.5 */ -o - transform: rotate(-90.0deg); /* Saf3.1+, Chrome */ -webkit - transform: rotate(-90.0deg); /* IE6,IE7 */ filter: progid: DXImageTransform.Microsoft.BasicImage(rotation = 0.083); /* IE8 */ -ms - filter: \"progid:DXImageTransform.Microsoft.BasicImage(rotation=0.083)\"; /* Standard */ transform: rotate(-90.0deg);} " +
                "\n</style>\n</head>\n<body>";

            // Start building table
            // apply theme based settings
            if (!Utility.Configuration.Settings.ThemeRestartRequired)
            {
                htmlString = !Utility.Configuration.Settings.DarkTheme ? ModifyHTMLStyle(htmlString, false) : ModifyHTMLStyle(htmlString, true);
            }
            else
            {
                htmlString = !Utility.Configuration.Settings.DarkTheme ? ModifyHTMLStyle(htmlString, true) : ModifyHTMLStyle(htmlString, false);         
            }

            // get CLEM Zone
            IModel clem = model as IModel;
            while (!(clem is ZoneCLEM))
                clem = clem.Parent;

            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.WriteLine(htmlString);
                htmlWriter.WriteLine("\n<span style=\"font-size:0.8em; font-weight:bold\">You will need to keep refreshing this page after changing settings and selecting the LabourAllocationsReport to see changes</span><br /><br />");

                htmlWriter.Write("\n<div class=\"clearfix defaultbanner\">");
                htmlWriter.Write($"<div class=\"namediv\">Labour allocation summary</div><br />");
                htmlWriter.Write($"<div class=\"typediv\">Details</div>");
                htmlWriter.Write("</div>");
                htmlWriter.Write("\n<div class=\"defaultcontent\">");
                htmlWriter.Write($"\n<div class=\"activityentry\">Summary last created on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()}<br />");
                htmlWriter.WriteLine("\n</div>");
                htmlWriter.WriteLine("\n</div>");

                // Get Labour resources
                labour = clem.FindAllDescendants<Labour>().FirstOrDefault() as Labour;
                if (labour == null)
                {
                    htmlWriter.Write("No Labour supplied in resources");
                    htmlWriter.Write("\n</body>\n</html>");
                    return htmlWriter.ToString();
                }

                numberLabourTypes = labour.FindAllChildren<LabourType>().Count();
                if (numberLabourTypes == 0)
                {
                    htmlWriter.Write("No Labour types supplied in Labour resource");
                    htmlWriter.Write("\n</body>\n</html>");
                    return htmlWriter.ToString();
                }

                // create labour list
                labourList.Clear();
                foreach (LabourType lt in labour.FindAllChildren<LabourType>())
                {
                    var newLabour = new LabourType()
                    {
                        Parent = labour,
                        Name = lt.Name,
                        AgeInMonths = lt.InitialAge * 12,
                        Sex = lt.Sex
                    };
                    IndividualAttribute att = new IndividualAttribute() { StoredValue = lt.Name };
                    newLabour.Attributes.Add("Group", att);
                    labourList.Add(newLabour);
                }

                // get all parents of LabourRequirement
                validpAtt.AddRange(ReflectionUtilities.GetAttributes(typeof(LabourRequirement), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList());
                validpAtt.AddRange(ReflectionUtilities.GetAttributes(typeof(LabourRequirementNoUnitSize), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList());
                validpAtt.AddRange(ReflectionUtilities.GetAttributes(typeof(LabourRequirementSimple), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList());
                if (validpAtt.Count() == 0)
                {
                    htmlWriter.Write("No components allow Labour Requirements to be added");
                    htmlWriter.Write("\n</body>\n</html>");
                    return htmlWriter.ToString();
                }

                // walk through all activities
                // check if LabourRequirement can be added
                ActivitiesHolder activities = clem.FindDescendant<ActivitiesHolder>();
                if (activities == null)
                {
                    htmlWriter.Write("Could not find an Activities Holder");
                    htmlWriter.Write("\n</body>\n</html>");
                    return htmlWriter.ToString();
                }

                using (StringWriter tableHtml = new StringWriter())
                {
                    tableHtml.WriteLine("<table class=\"main\">");
                    tableHtml.Write("<tr><th>Activity</th>");
                    foreach (LabourType lt in labour.FindAllChildren<LabourType>())
                        tableHtml.Write($"<th><span>{lt.Name}</span></th>");

                    tableHtml.WriteLine("</tr>");
                    tableHtml.WriteLine(TableRowHTML(activities));
                    tableHtml.WriteLine("</table>");

                    htmlWriter.Write(tableHtml.ToString());
                }
                // add notes
                htmlWriter.WriteLine("\n<div class=\"holdermain\">");
                htmlWriter.Write("\n<div class=\"clearfix messagebanner\">");
                htmlWriter.Write("<div class=\"typediv\">" + "Notes" + "</div>");
                htmlWriter.Write("</div>");
                htmlWriter.WriteLine("\n<div class=\"messagecontent\">");
                htmlWriter.WriteLine("\n<ul>");
                htmlWriter.WriteLine("\n<li>Only activities capable of including a labour requirement are displayed.</li>");
                htmlWriter.WriteLine("\n<li>Activities with no labour requirement provided are displayed with grey text.</li>");
                htmlWriter.WriteLine("\n<li>Multiple rows of icons (circles) for a given activity show where more than one individual is required.</li>");
                htmlWriter.WriteLine("\n<li>The preferential allocation of labour is displayed in the following order:" +
                    "<table class=\"blank\">" +
                    "<tr><td><span class=\"dot dot1 \">" + "</span></td><td>1st preference</td></tr>" +
                    "<tr><td><span class=\"dot dot2 \">" + "</span></td><td>2nd preference</td></tr>" +
                    "<tr><td><span class=\"dot dot3 \">" + "</span></td><td>3rd preference</td></tr>" +
                    "<tr><td><span class=\"dot dot4 \">" + "</span></td><td>4th+ preference</td></tr>" +
                    "</table></li>");
                htmlWriter.WriteLine("\n</ul>");
                htmlWriter.Write("\n</div>");

                // aging note
                if (labour.AllowAging)
                {
                    htmlWriter.WriteLine("\n<div class=\"holdermain\">");
                    htmlWriter.WriteLine("\n<div class=\"clearfix warningbanner\">");
                    htmlWriter.Write("<div class=\"typediv\">" + "Warning" + "</div>");
                    htmlWriter.Write("</div>");
                    htmlWriter.Write("\n<div class=\"warningcontent\">");
                    htmlWriter.Write("\n<div class=\"activityentry\">As this simulation allows aging of individuals (see Labour) these allocations may change over the duration of the simulation. ");
                    htmlWriter.Write("\n</div>");
                    htmlWriter.WriteLine("\n</div>");
                }
                htmlWriter.Write("\n</body>\n</html>");
                return htmlWriter.ToString(); 
            }
        }



        private string TableRowHTML(IModel model)
        {
            // create row
            using (StringWriter tblstr = new StringWriter())
            {
                if (validpAtt.Select(a => a.ParentType).Contains(model.GetType()))
                {
                    if (model.FindAllChildren<LabourRequirement>().Any())
                    {
                        foreach (LabourRequirement labourRequirement in model.FindAllChildren<LabourRequirement>())
                        {
                            tblstr.Write("<tr" + ((labourRequirement == null) ? " class=\"disabled\"" : "") + "><td" + ((labourRequirement == null) ? " class=\"disabled\"" : "") + ">" + model.Name + "</td>");
                            // for each labour type
                            foreach (LabourType lt in labourList)
                            {
                                tblstr.WriteLine("<td>");
                                // for each filter group
                                foreach (var item in labourRequirement.FindAllChildren<LabourGroup>())
                                {
                                    tblstr.Write("<div>");
                                    int level = 0;
                                    // while nested 
                                    var nested = labourRequirement.FindChild<LabourGroup>();
                                    bool found = false;
                                    while (nested != null)
                                    {
                                        nested.InitialiseFilters();
                                        level++;
                                        if (nested.Filter(lt))
                                        {
                                            found = true;
                                            break;
                                        }
                                        nested.ClearRules();
                                        nested = nested.FindAllChildren<LabourGroup>().FirstOrDefault();
                                    }
                                    if (found)
                                        tblstr.Write($"<span class=\"dot dot{((level < 5) ? level.ToString() : "4")}\"></span>");
                                    tblstr.Write("</div>");
                                }
                                tblstr.WriteLine("</td>");
                            }
                            tblstr.WriteLine("</tr>");
                        }
                    }
                    else
                        tblstr.Write($"<tr class=\"disabled\"><td class=\"disabled\">{model.Name}</td>{CreateRowHTML("", numberLabourTypes)}</tr>");
                }

                // add all rows for children
                foreach (Model child in model.Children.Where(a => a.Enabled))
                    tblstr.WriteLine(TableRowHTML(child));

                return tblstr.ToString();
            }
        }

        private string CreateRowHTML(string text, int columns)
        {
            using (StringWriter row = new StringWriter())
            {
                for (int i = 0; i < columns; i++)
                    row.Write($"<td>{text}</td>");
                return row.ToString();
            }
        }

        private string CreateMarkdown()
        {
            using (StringWriter markdownString = new StringWriter())
            {
                // Start building table
                IModel clem = model.FindAncestor<ZoneCLEM>() as IModel;

                // Get Labour resources
                labour = clem.FindAllDescendants<Labour>().FirstOrDefault() as Labour;
                if (labour == null)
                {
                    markdownString.Write("No Labour supplied in resources");
                    return markdownString.ToString();
                }

                numberLabourTypes = labour.FindAllChildren<LabourType>().Count();
                if (numberLabourTypes == 0)
                {
                    markdownString.Write("No Labour types supplied in Labour resource");
                    return markdownString.ToString();
                }

                labourList.Clear();
                foreach (LabourType lt in labour.FindAllChildren<LabourType>())
                {
                    var newLabour = new LabourType()
                    {
                        Parent = labour,
                        Name = lt.Name,
                        AgeInMonths = lt.InitialAge * 12,
                        Sex = lt.Sex
                    };
                    IndividualAttribute att = new IndividualAttribute() { StoredValue = lt.Name };
                    newLabour.Attributes.Add("Group", att);
                    labourList.Add(newLabour);
                }

                // get all parents of LabourRequirement
                validpAtt.AddRange(ReflectionUtilities.GetAttributes(typeof(LabourRequirement), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList());
                validpAtt.AddRange(ReflectionUtilities.GetAttributes(typeof(LabourRequirementNoUnitSize), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList());
                validpAtt.AddRange(ReflectionUtilities.GetAttributes(typeof(LabourRequirementSimple), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList());
                if (validpAtt.Count() == 0)
                {
                    markdownString.Write("No components allow Labour Requirements to be added");
                    return markdownString.ToString();
                }

                // walk through all activities
                // check if LabourRequirement can be added
                ActivitiesHolder activities = clem.FindAllDescendants<ActivitiesHolder>().FirstOrDefault() as ActivitiesHolder;
                if (activities == null)
                {
                    markdownString.Write("Could not find an Activities Holder");
                    return markdownString.ToString();
                }

                using (StringWriter tableHeader = new StringWriter())
                {
                    using (StringWriter tableSpacer = new StringWriter())
                    {
                        tableHeader.Write("| Activity");
                        tableSpacer.Write("| :---");
                        foreach (LabourType lt in labour.FindAllChildren<LabourType>())
                        {
                            tableHeader.Write(" | " + lt.Name.Replace("_", " "));
                            tableSpacer.Write(" | :---:");
                        }
                        tableHeader.Write(" |  \n");
                        tableSpacer.Write(" |  \n");

                        markdownString.Write(tableHeader.ToString());
                        markdownString.Write(tableSpacer.ToString());
                    }
                }
                markdownString.Write(TableRowMarkdown(activities));

                // add notes
                markdownString.Write("  \n***  \n");
                markdownString.Write("Notes  \n");
                markdownString.Write("-  Only activities capable of including a labour requirement are displayed.  \n");
                markdownString.Write("-  Activities with no labour requirement provided are displayed with italic text.  \n");
                markdownString.Write("-  Multiple rows for a given activity show where more than one individual is required.  \n");
                markdownString.Write("-  The preferential allocation of labour is identified from 1 (1st) to 5 (5th, max levels displayed)  \n");

                // aging note
                if (labour.AllowAging)
                {
                    markdownString.Write("  \n***  \n");
                    markdownString.Write("Warnings  \n");
                    markdownString.Write("-  As this simulation allows aging of individuals (see Labour) these allocations may change over the duration of the simulation.");
                }

                markdownString.Write("  \n***  \n");
                return markdownString.ToString(); 
            }
        }

        private string TableRowMarkdown(IModel model)
        {
            using (StringWriter tblstr = new StringWriter())
            {
                // create row
                // can row be included?
                if (validpAtt.Select(a => a.ParentType).Contains(model.GetType()))
                {
                    string emph = "_";
                    if (model.FindAllChildren<LabourRequirement>().Any())
                    {
                        foreach (LabourRequirement labourRequirement in model.FindAllChildren<LabourRequirement>())
                        {
                            emph = "";
                            tblstr.Write($"| {emph}{model.Name.Replace("_", " ")}{emph} |");
                            // for each labour type
                            foreach (LabourType lt in labourList)
                            {
                                // for each filter group
                                foreach (var item in labourRequirement.FindAllChildren<LabourGroup>())
                                {
                                    string levelstring = "";
                                    bool found = false;
                                    int level = 0;
                                    // while nested 
                                    var nested = item;
                                    while (nested != null)
                                    {
                                        nested.InitialiseFilters();
                                        level++;
                                        levelstring = (level < 5) ? level.ToString() : "4";
                                        if (nested.Filter(lt))
                                        {
                                            found = true;
                                            break;
                                        }
                                        nested.ClearRules();
                                        nested = nested.FindChild<LabourGroup>();
                                    }
                                    tblstr.Write($" {(found ? levelstring : "0")} |");
                                }
                            }
                            tblstr.Write("  \n");
                        }
                    }
                    else
                        tblstr.Write($"| {emph}{model.Name.Replace("_", " ")}{emph} | {CreateRowMarkdown("", numberLabourTypes)}  \n");
                }

                // add all rows for children
                foreach (var child in model.Children.Where(a => a.Enabled))
                    tblstr.Write(TableRowMarkdown(child));

                return tblstr.ToString(); 
            }
        }

        private string CreateRowMarkdown(string text, int columns)
        {
            using (StringWriter row = new StringWriter())
            {
                for (int i = 0; i < columns; i++)
                    row.Write($"{text} | ");

                return row.ToString();
            }
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }

        private static string ModifyHTMLStyle(string htmlString, bool isDarkMode )
        {
            string reformattedHTML = htmlString;
            if ( isDarkMode )
            {
                // dark theme
                reformattedHTML = reformattedHTML.Replace("[FontColor]", "#E5E5E5");
                reformattedHTML = reformattedHTML.Replace("[GridColor]", "#888");
                reformattedHTML = reformattedHTML.Replace("[WarningBackground]", "rgba(255, 102, 0, 0.4)");
                reformattedHTML = reformattedHTML.Replace("[MessageBackground]", "rgba(100, 149, 237, 0.4)");
                reformattedHTML = reformattedHTML.Replace("[DisabledColour]", "#666666");
                reformattedHTML = reformattedHTML.Replace("[TableBackground]", "background-color: rgba(50, 50, 50, 0.5);");
                reformattedHTML = reformattedHTML.Replace("[ContDefaultBack]", "#282828");
                reformattedHTML = reformattedHTML.Replace("[ContDefaultBanner]", "#686868");
                reformattedHTML = reformattedHTML.Replace("[HeaderFontColor]", "#333333");
            }
            else
            {
                // light theme
                reformattedHTML = reformattedHTML.Replace("[FontColor]", "#000000");
                reformattedHTML = reformattedHTML.Replace("[GridColor]", "Black");
                reformattedHTML = reformattedHTML.Replace("[WarningBackground]", "#FFFFFA");
                reformattedHTML = reformattedHTML.Replace("[MessageBackground]", "#FAFAFF");
                reformattedHTML = reformattedHTML.Replace("[DisabledColour]", "#cccccc");
                reformattedHTML = reformattedHTML.Replace("[TableBackground]", "background-color: white;");
                reformattedHTML = reformattedHTML.Replace("[ContDefaultBack]", "#FAFAFA");
                reformattedHTML = reformattedHTML.Replace("[ContDefaultBanner]", "#000");
                reformattedHTML = reformattedHTML.Replace("[HeaderFontColor]", "white");
            }

            return reformattedHTML;
        }

    }
}
