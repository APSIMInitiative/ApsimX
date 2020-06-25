using APSIM.Shared.Utilities;
using Models.CLEM;
using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter to display HTML report of labour allocation to activities
    /// </summary>
    public class LabourAllocationPresenter : IPresenter
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

        private List<ValidParentAttribute> validpAtt = new List<ValidParentAttribute>();
        private int numberLabourTypes = 0;
        private Labour labour;
        private List<LabourType> labourList = new List<LabourType>();

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

            string htmlString = "<!DOCTYPE html>\n" +
                "<html>\n<head>\n<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />\n<style>\n" +
                "body {color: [FontColor]; max-width:1000px; font-size:10pt;}" + 
                "th,td {padding:5px;}" +
                "th,td {border: 1px dotted [GridColor]; }" +
                "table {border: 0px none #009999; border-collapse: collapse;}" +
                "table.main {[TableBackground] }" +
                "table.main tr td.disabled {color: [DisabledColour]; }" +
                ".dot { margin:auto; display:block; height:20px; width:20px; line-height:20px; background-color:black; -moz-border-radius: 10px; border-radius: 10px; }" +
                ".dot1 { background-color:lightgreen; }" +
                ".dot2 { background-color:lightskyblue; }" +
                ".dot4 { background-color:coral; }" +
                ".dot3 { background-color:lightpink; }" +
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
                ".holdermain {margin: 20px 0px 20px 0px}" +
                "\n</style>\n</head>\n<body>";

            // Start building table

            // apply theme based settings
            if (!Utility.Configuration.Settings.DarkTheme)
            {
                // light theme
                htmlString = htmlString.Replace("[FontColor]", "#000000");
                htmlString = htmlString.Replace("[GridColor]", "Black");
                htmlString = htmlString.Replace("[WarningBackground]", "#FFFFFA");
                htmlString = htmlString.Replace("[MessageBackground]", "#FAFAFF");
                htmlString = htmlString.Replace("[DisabledColour]", "#cccccc");
                htmlString = htmlString.Replace("[TableBackground]", "background-color: white;");
            }
            else
            {
                // dark theme
                htmlString = htmlString.Replace("[FontColor]", "#E5E5E5");
                htmlString = htmlString.Replace("[GridColor]", "#888");
                htmlString = htmlString.Replace("[WarningBackground]", "rgba(255, 102, 0, 0.4)");
                htmlString = htmlString.Replace("[MessageBackground]", "rgba(100, 149, 237, 0.4)");
                htmlString = htmlString.Replace("[DisabledColour]", "#666666");
                htmlString = htmlString.Replace("[TableBackground]", "background-color: rgba(50, 50, 50, 0.5);");
            }

            // get CLEM Zone
            IModel clem = model as IModel;
            while(!(clem is ZoneCLEM))
            {
                clem = clem.Parent;
            }

            // Get Labour resources
            labour = Apsim.ChildrenRecursively(clem, typeof(Labour)).FirstOrDefault() as Labour;
            if(labour == null)
            {
                htmlString += "No Labour supplied in resources";
                EndHTML(htmlString);
            }

            numberLabourTypes = Apsim.Children(labour, typeof(LabourType)).Count();
            if (numberLabourTypes == 0)
            {
                htmlString += "No Labour types supplied in Labour resource";
                EndHTML(htmlString);
            }

            // create labour list
            foreach (LabourType lt in Apsim.Children(labour, typeof(LabourType)))
            {
                labourList.Add(new LabourType()
                {
                    Parent = labour,
                    Name = lt.Name,
                    AgeInMonths = lt.InitialAge*12,
                    Gender = lt.Gender
                }
                );
            }

            // get all parents of LabourRequirement
            validpAtt.AddRange(ReflectionUtilities.GetAttributes(typeof(LabourRequirement), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList());
            validpAtt.AddRange(ReflectionUtilities.GetAttributes(typeof(LabourRequirementNoUnitSize), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList());
            validpAtt.AddRange(ReflectionUtilities.GetAttributes(typeof(LabourRequirementSimple), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList());
            if (validpAtt.Count() == 0)
            {
                htmlString += "No components allow Labour Requirements to be added";
                EndHTML(htmlString);
            }

            // walk through all activities
            // check if LabourRequirement can be added
            ActivitiesHolder activities = Apsim.ChildrenRecursively(clem, typeof(ActivitiesHolder)).FirstOrDefault() as ActivitiesHolder;
            if (activities == null)
            {
                htmlString += "Could not find an Activities Holder";
                EndHTML(htmlString);
            }

            string tableHtml = "";
            tableHtml += "<table class=\"main\">";
            tableHtml += "<tr><th>Activity</th>";
            foreach (LabourType lt in Apsim.Children(labour, typeof(LabourType)))
            {
                tableHtml += "<th>"+lt.Name+"</th>";
            }
            tableHtml += "</tr>";
            tableHtml += TableRow(activities);
            tableHtml += "</table>";

            htmlString += tableHtml;

            // add notes
            htmlString += "\n<div class=\"holdermain\">";
            htmlString += "\n<div class=\"clearfix messagebanner\">";
            htmlString += "<div class=\"typediv\">" + "Notes" + "</div>";
            htmlString += "</div>";
            htmlString += "\n<div class=\"messagecontent\">";
            htmlString += "\n<ul>";
            htmlString += "\n<li>Only activities capable of including a labour requirement are displayed.</li>";
            htmlString += "\n<li>Activities with no labour requirement provided are displayed with grey text.</li>";
            htmlString += "\n<li>Multiple rows of icons (circles) for a given activity show where more than one individual is required.</li>";
            htmlString += "\n<li>The preferential allocation of labour is displayed in the following order:" +
                "<table class=\"blank\">" +
                "<tr><td><span class=\"dot dot1 \">" + "</span></td><td>1st preference</td></tr>" +
                "<tr><td><span class=\"dot dot2 \">" + "</span></td><td>2nd preference</td></tr>" +
                "<tr><td><span class=\"dot dot3 \">" + "</span></td><td>3rd preference</td></tr>" +
                "<tr><td><span class=\"dot dot4 \">" + "</span></td><td>4th+ preference</td></tr>" +
                "</table></li>";
            htmlString += "\n</ul>";
            htmlString += "\n</div>";

            // aging note
            if (labour.AllowAging)
            {
                htmlString += "\n<div class=\"holdermain\">";
                htmlString += "\n<div class=\"clearfix warningbanner\">";
                htmlString += "<div class=\"typediv\">" + "Warning" + "</div>";
                htmlString += "</div>";
                htmlString += "\n<div class=\"warningcontent\">";
                htmlString += "\n<div class=\"activityentry\">As this simulation allows aging of individuals (see Labour) these allocations may change over the duration of the simulation. ";
                htmlString += "\n</div>";
                htmlString += "\n</div>";
            }

            EndHTML(htmlString);
        }

        private string TableRow(IModel model)
        {
            string tblstr = "";
            // create row

            // can row be included?
            if(validpAtt.Select(a => a.ParentType).Contains(model.GetType()))
            {
                Model labourRequirement = Apsim.Children(model, typeof(IModel)).Where(a => a.GetType().ToString().Contains("LabourRequirement")).FirstOrDefault() as Model;
                tblstr += "<tr"+((labourRequirement == null)? " class=\"disabled\"":"") +"><td" + ((labourRequirement == null) ? " class=\"disabled\"" : "") + ">" + model.Name + "</td>";

                // does activity have a Labour Requirement
                if (!(labourRequirement == null))
                {
                    // for each labour type
                    foreach (LabourType lt in labourList)
                    {
                        tblstr += "<td>";
                        // for each filter group
                        foreach (Model item in Apsim.Children(labourRequirement, typeof(LabourFilterGroup)))
                        {
                            tblstr += "<div>";
                            int level = 0;
                            // while nested 
                            Model nested = labourRequirement as Model;

                            while (Apsim.Children(nested, typeof(LabourFilterGroup)).Count() > 0)
                            {
                                level++;
                                nested = Apsim.Children(nested, typeof(LabourFilterGroup)).FirstOrDefault() as Model;
                                List<LabourType> ltlist = new List<LabourType>() { lt };
                                if (ltlist.Filter(nested).Count() >= 1)
                                {
                                    tblstr += "<span class=\"dot dot"+((level<5)?level.ToString():"5")+" \">"+"</span>";
                                }
                            }
                            tblstr += "</div>";
                        }
                        tblstr += "</td>";
                    }
                }
                else
                {
                    tblstr += CreateRow("", numberLabourTypes);
                }
                tblstr += "</tr>";
            }

            // add all rows for children
            foreach (Model child in model.Children)
            {
                tblstr += TableRow(child);
            }
            return tblstr;
        }

        private void EndHTML(string htmlString)
        {
            htmlString += "\n</body>\n</html>";
            this.genericView.SetContents(htmlString, false, false);
        }

        private string CreateRow(string text, int columns)
        {
            string row = "";
            for (int i = 0; i < columns; i++)
            {
                row += "<td>" + text + "</td>";
            }
            return row;
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }

    }
}
