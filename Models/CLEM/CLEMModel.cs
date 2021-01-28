using APSIM.Shared.Utilities;
using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;

namespace Models.CLEM
{
    ///<summary>
    /// CLEM base model
    ///</summary> 
    [Serializable]
    [Description("This is the Base CLEM model and should not be used directly.")]
    public abstract class CLEMModel : Model, ICLEMUI, ICLEMDescriptiveSummary
    {
        /// <summary>
        /// Link to summary
        /// </summary>
        [Link]
        [NonSerialized]
        public ISummary Summary = null;

        private Guid id = Guid.NewGuid();

        /// <summary>
        /// Identifies the last selected tab for display
        /// </summary>
        [JsonIgnore]
        public string SelectedTab { get; set; }

        /// <summary>
        /// Warning log for this CLEM model
        /// </summary>
        [JsonIgnore]
        public WarningLog Warnings = WarningLog.GetInstance(50);

        /// <summary>
        /// Allows unique id of activity to be set 
        /// </summary>
        /// <param name="id"></param>
        public void SetGuID(string id)
        {
            this.id = Guid.Parse(id);
        }

        /// <summary>
        /// Model identifier
        /// </summary>
        [JsonIgnore]
        public string UniqueID { get { return id.ToString(); } }

        /// <summary>
        /// Parent CLEM Zone
        /// Stored here so rapidly retrieved
        /// </summary>
        [JsonIgnore]
        public String CLEMParentName { get; set; }

        /// <summary>
        /// return combo name of ParentName.ModelName
        /// </summary>
        public string NameWithParent => $"{this.Parent.Name}.{this.Name}";

        /// <summary>
        /// Method to set defaults from   
        /// </summary>
        public void SetDefaults()
        {
            //Iterate through properties
            foreach (var property in GetType().GetProperties())
            {
                //Iterate through attributes of this property
                foreach (Attribute attr in property.GetCustomAttributes(true))
                {
                    //does this property have [DefaultValueAttribute]?
                    if (attr is System.ComponentModel.DefaultValueAttribute)
                    {
                        //So lets try to load default value to the property
                        System.ComponentModel.DefaultValueAttribute dv = (System.ComponentModel.DefaultValueAttribute)attr;
                        if (dv != null)
                        {
                            if (property.PropertyType.IsEnum)
                            {
                                property.SetValue(this, Enum.Parse(property.PropertyType, dv.Value.ToString()));
                            }
                            else
                            {
                                property.SetValue(this, dv.Value, null);
                            }
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Is timing ok for the current model
        /// </summary>
        public bool TimingOK
        {
            get
            {
                Console.WriteLine(this.Name);
                int res = this.Children.Where(a => typeof(IActivityTimer).IsAssignableFrom(a.GetType())).Sum(a => (a as IActivityTimer).ActivityDue ? 0 : 1);

                var q = this.Children.Where(a => typeof(IActivityTimer).IsAssignableFrom(a.GetType()));
                var w = q.Sum(a => (a as IActivityTimer).ActivityDue ? 0 : 1);

                return (res == 0);
            }
        }

        /// <summary>
        /// Returns the opacity value for this component in the summary display
        /// </summary>
        public double SummaryOpacity(bool formatForParent) => ((!this.Enabled & (!formatForParent | (formatForParent & this.Parent.Enabled))) ? 0.4 : 1.0);

        /// <summary>
        /// Determines if this component has a valid parent based on parent attributes
        /// </summary>
        /// <returns></returns>
        public bool ValidParent()
        {
            var parents = ReflectionUtilities.GetAttributes(this.GetType(), typeof(ValidParentAttribute), false).Cast<ValidParentAttribute>().ToList();
            return (parents.Where(a => a.ParentType.Name == this.Parent.GetType().Name).Count() > 0);
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public virtual string ModelSummary(bool formatForParentControl)
        {
            return "<div class=\"resourcenote\">No description provided</div>";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <param name="htmlString"></param>
        /// <returns></returns>
        public virtual string GetFullSummary(object model, bool formatForParentControl, string htmlString)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (model.GetType().IsSubclassOf(typeof(CLEMModel)))
                {
                    CLEMModel cm = model as CLEMModel;
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
        /// Styling to use for HTML summary
        /// </summary>
        [JsonIgnore]
        public virtual HTMLSummaryStyle ModelSummaryStyle { get; set; }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public virtual string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "\r\n</div>\r\n</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public virtual string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            string overall = "activity";
            string extra = "";

            if (this.ModelSummaryStyle == HTMLSummaryStyle.Default)
            {
                if (this is Relationship || this.GetType().IsSubclassOf(typeof(Relationship)))
                {
                    this.ModelSummaryStyle = HTMLSummaryStyle.Default;
                }
                else if (this.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
                {
                    this.ModelSummaryStyle = HTMLSummaryStyle.Resource;
                }
                else if (typeof(IResourceType).IsAssignableFrom(this.GetType()))
                {
                    this.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
                }
                else if (this.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
                {
                    this.ModelSummaryStyle = HTMLSummaryStyle.Activity;
                }
            }

            switch (ModelSummaryStyle)
            {
                case HTMLSummaryStyle.Default:
                    overall = "default";
                    break;
                case HTMLSummaryStyle.Resource:
                    overall = "resource";
                    break;
                case HTMLSummaryStyle.SubResource:
                    overall = "resource";
                    extra = "light";
                    break;
                case HTMLSummaryStyle.SubResourceLevel2:
                    overall = "resource";
                    extra = "dark";
                    break;
                case HTMLSummaryStyle.Activity:
                    break;
                case HTMLSummaryStyle.SubActivity:
                    extra = "light";
                    break;
                case HTMLSummaryStyle.Helper:
                    break;
                case HTMLSummaryStyle.SubActivityLevel2:
                    extra = "dark";
                    break;
                case HTMLSummaryStyle.FileReader:
                    overall = "file";
                    break;
                default:
                    break;
            }

            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"holder" + ((extra == "") ? "main" : "sub") + " " + overall + "\" style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + ";\">");
                htmlWriter.Write("\r\n<div class=\"clearfix " + overall + "banner" + extra + "\">" + this.ModelSummaryNameTypeHeader() + "</div>");
                htmlWriter.Write("\r\n<div class=\"" + overall + "content" + ((extra != "") ? extra : "") + "\">");

                return htmlWriter.ToString(); 
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public virtual string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public virtual string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (this.GetType().IsSubclassOf(typeof(CLEMResourceTypeBase)))
                {
                    // add units when completed
                    string units = (this as IResourceType).Units;
                    if (units != "NA")
                    {
                        htmlWriter.Write("\r\n<div class=\"activityentry\">This resource is measured in  ");
                        if (units == null || units == "")
                        {
                            htmlWriter.Write("<span class=\"errorlink\">NOT SET</span>");
                        }
                        else
                        {
                            htmlWriter.Write("<span class=\"setvalue\">" + units + "</span>");
                        }
                        htmlWriter.Write("</div>");
                    }
                }
                if (this.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
                {
                    if (this.Children.Count() == 0)
                    {
                        htmlWriter.Write("\r\n<div class=\"activityentry\">Empty</div>");
                    }
                }
                return htmlWriter.ToString(); 
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public virtual string ModelSummaryInnerOpeningTagsBeforeSummary()
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
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
                    switch ((this as CLEMActivityBase).OnPartialResourcesAvailableAction)
                    {
                        case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                            htmlWriter.Write(" tooltip = \"Error and Stop on insufficient resources\">Stop");
                            break;
                        case OnPartialResourcesAvailableActionTypes.SkipActivity:
                            htmlWriter.Write(">Skip");
                            break;
                        case OnPartialResourcesAvailableActionTypes.UseResourcesAvailable:
                            htmlWriter.Write(">Partial");
                            break;
                        default:
                            break;
                    }
                    htmlWriter.Write("</div>");
                }
                htmlWriter.Write("<div class=\"typediv\">" + this.GetType().Name + "</div>");
                return htmlWriter.ToString(); 
            }
        }

        /// <summary>
        /// Create the HTML for the descriptive summary display of a supplied component
        /// </summary>
        /// <param name="modelToSummarise">Model to create summary fpr</param>
        /// <param name="darkTheme">Boolean representing if in dark mode</param>
        /// <param name="bodyOnly">Only produve the body html</param>
        /// <param name="apsimFilename">Create master simulation summary header</param>
        /// <returns></returns>
        public static string CreateDescriptiveSummaryHTML(Model modelToSummarise, bool darkTheme = false, bool bodyOnly = false, string apsimFilename = "")
        {
            // currently includes autoupdate script for display of summary information in browser
            // give APSIM Next Gen no longer has access to WebKit HTMLView in GTK for .Net core
            // includes <!-- graphscript --> to add graphing js details if needed
            // includes <!-- CLEMZoneBody --> to add multiple components for overall summary

            string htmlString = "<!DOCTYPE html>\r\n" +
                "<html>\r\n<head>\r\n<script type=\"text / javascript\" src=\"https://livejs.com/live.js\"></script>\r\n" +
                "<meta http-equiv=\"Cache-Control\" content=\"no-cache, no-store, must-revalidate\" />\r\n" +
                "<meta http-equiv = \"Pragma\" content = \"no-cache\" />\r\n" +
                "<meta http-equiv = \"Expires\" content = \"0\" />\r\n" +
                "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />\r\n" +
                "<style>\r\n" +
                "body {color: [FontColor]; max-width:1000px; font-size:1em; font-family: Segoe UI, Arial, sans-serif}" +
                "table {border-collapse: collapse; font-size:0.8em; }" +
                ".resource table,th,td {border: 1px solid #996633; }" +
                "table th {padding:8px; color:[HeaderFontColor];}" +
                "table td {padding:8px; }" +
                " td:nth-child(n+2) {text-align:center;}" +
                " th:nth-child(1) {text-align:left;}" +
                ".resource th {background-color: #996633 !important; }" +
                ".resource tr:nth-child(2n+3) {background:[ResRowBack] !important;}" +
                ".resource tr:nth-child(2n+2) {background:[ResRowBack2] !important;}" +
                ".resource td.fill {background-color: #c1946c !important;}" +
                ".resourceborder {border-color:#996633; border-width:1px; border-style:solid; padding:0px; background-color:Cornsilk !important; }" +
                ".resource h1,h2,h3 {color:#996633; } .activity h1,h2,h3 { color:#009999; margin-bottom:5px; }" +
                ".resourcebanner {background-color:#996633 !important; color:[ResFontBanner]; padding:5px; font-weight:bold; border-radius:5px 5px 0px 0px; }" +
                ".resourcebannerlight {background-color:#c1946c !important; color:[ResFontBanner]; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".resourcebannerdark {background-color:#996633 !important; color:[ResFontBanner]; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".resourcecontent {background-color:[ResContBack] !important; margin-bottom:40px; border-radius:0px 0px 5px 5px; border-color:#996633; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcebanneralone {background-color:[ResContBack] !important; margin:10px 0px 5px 0px; border-radius:5px 5px 5px 5px; border-color:#996633; border-width:1px; border-style:solid solid solid solid; padding:5px;}" +
                ".resourcecontentlight {background-color:[ResContBackLight] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#c1946c; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcecontentdark {background-color:[ResContBackDark] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#996633; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".resourcelink {color:#996633; font-weight:bold; background-color:Cornsilk !important; border-color:#996633; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".activity th,td {padding:5px; }" +
                ".activity table,th,td {border: 1px solid #996633; }" +
                ".activity th {background-color: #996633 !important; }" +
                ".activity td.fill {background-color: #996633 !important; }" +
                ".activity table {border-collapse: collapse; font-size:0.8em; }" +
                ".activity h1 {color:#009999; } .activity h1,h2,h3 { color:#009999; margin-bottom:5px; }" +
                ".activityborder {border-color:#009999; border-width:2px; border-style:none none none solid; padding:0px 0px 0px 10px; margin-bottom:15px; }" +
                ".activityborderfull {border-color:#009999; border-radius:5px; background-color:#f0f0f0 !important; border-width:1px; border-style:solid; margin-bottom:40px; }" +
                ".activitybanner {background-color:#009999 !important; border-radius:5px 5px 0px 0px; color:#f0f0f0; padding:5px; font-weight:bold }" +
                ".activitybannerlight {background-color:#86b2b1 !important; border-radius:5px 5px 0px 0px; color:white; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".activitybannerdark {background-color:#009999 !important; border-radius:5px 5px 0px 0px; color:white; padding:5px 5px 5px 10px; margin-top:12px; font-weight:bold }" +
                ".activitybannercontent {background-color:#86b2b1 !important; border-radius:5px 5px 0px 0px; padding:5px 5px 5px 10px; margin-top:5px; }" +
                ".activitycontent {background-color:[ActContBack] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#009999; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".activitycontentlight {background-color:[ActContBackLight] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#86b2b1; border-width:0px 1px 1px 1px; border-style:solid; padding:10px;}" +
                ".activitycontentdark {background-color:[ActContBackDark] !important; margin-bottom:10px; border-radius:0px 0px 5px 5px; border-color:#86b2b1; border-width:0px 1px 1px 1px; border-style:none solid solid solid; padding:10px;}" +
                ".activitypadding {padding:10px; }" +
                ".activityentry {padding:5px 0px 5px 0px; }" +
                ".activityarea {padding:10px; }" +
                ".activitygroupsborder {border-color:#86b2b1; background-color:[ActContBackGroups] !important; border-width:1px; border-style:solid; padding:10px; margin-bottom:5px; margin-top:5px;}" +
                ".activitylink {color:#009999; font-weight:bold; background-color:[ActContBack] !important; border-color:#009999; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".topspacing { margin-top:10px; }" +
                ".disabled { color:#CCC; }" +
                ".clearfix { overflow: auto; }" +
                ".namediv { float:left; vertical-align:middle; }" +
                ".typediv { float:right; vertical-align:middle; font-size:0.6em; }" +
                ".partialdiv { font-size:0.8em; float:right; text-transform: uppercase; color:white; font-weight:bold; vertical-align:middle; border-color:white; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; margin-left: 10px;  border-radius:3px; }" +
                ".filelink {color:green; font-weight:bold; background-color:mintcream !important; border-color:green; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".errorlink {color:white; font-weight:bold; background-color:red !important; border-color:darkred; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px; }" +
                ".setvalue {font-weight:bold; background-color: [ValueSetBack] !important; Color: [ValueSetFont]; border-color:#697c7c; border-width:1px; border-style:solid; padding:0px 5px 0px 5px; border-radius:3px;}" +
                ".folder {color:#666666; font-style: italic; font-size:1.1em; }" +
                ".cropmixedlabel {color:#666666; font-style: italic; font-size:1.1em; padding: 5px 0px 10px 0px; }" +
                ".croprotationlabel {color:#666666; font-style: italic; font-size:1.1em; padding: 5px 0px 10px 0px; }" +
                ".cropmixedborder {border-color:#86b2b1; background-color:[CropRotationBack] !important; border-width:1px; border-style:solid; padding:0px 10px 0px 10px; margin-bottom:5px;margin-top:10px; }" +
                ".croprotationborder {border-color:#86b2b1; background-color:[CropRotationBack] !important; border-width:2px; border-style:solid; padding:0px 10px 0px 10px; margin-bottom:5px;margin-top:10px; }" +
                ".labourgroupsborder {border-color:[LabourGroupBorder]; background-color:[LabourGroupBack] !important; border-width:1px; border-style:solid; padding:10px; margin-bottom:5px; margin-top:5px;}" +
                ".labournote {font-style: italic; color:#666666; padding-top:7px;}" +
                ".warningbanner {background-color:Orange !important; border-radius:5px 5px 5px 5px; color:Black; padding:5px; font-weight:bold; margin-bottom:10px;margin-top:10px; }" +
                ".errorbanner {background-color:Red !important; border-radius:5px 5px 5px 5px; color:Black; padding:5px; font-weight:bold; margin-bottom:10px;margin-top:10px; }" +
                ".filtername {margin:10px 0px 5px 0px; font-size:0.9em; color:#cc33cc;font-weight:bold;}" +
                ".filterborder {display: block; width: 100% - 40px; border-color:#cc33cc; background-color:[FiltContBack] !important; border-width:1px; border-style:solid; padding:5px; margin:0px 0px 5px 0px; border-radius:5px; }" +
                ".filterset {float: left; font-size:0.85em; font-weight:bold; color:#cc33cc; background-color:[FiltContBack] !important; border-width:0px; border-style:none; padding: 0px 3px; margin: 2px 0px 0px 5px; border-radius:3px; }" +
                ".filteractivityborder {background-color:[FiltContActivityBack] !important; color:#fff; }" +
                ".filter {float: left; border-color:#cc33cc; background-color:#cc33cc !important; color:white; border-width:1px; border-style:solid; padding: 0px 5px 0px 5px; font-weight:bold; margin: 0px 5px 0px 5px;  border-radius:3px;}" +
                ".filtererror {float: left; border-color:red; background-color:red !important; color:white; border-width:1px; border-style:solid; padding: 0px 5px 0px 5px; font-weight:bold; margin: 0px 5px 0px 5px;  border-radius:3px;}" +
                ".filebanner {background-color:green !important; border-radius:5px 5px 0px 0px; color:mintcream; padding:5px; font-weight:bold }" +
                ".filecontent {background-color:[ContFileBack] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:green; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".defaultbanner {background-color:[ContDefaultBanner] !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; font-weight:bold }" +
                ".defaultcontent {background-color:[ContDefaultBack] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:[ContDefaultBanner]; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".holdermain {margin: 20px 0px 20px 0px}" +
                ".holdersub {margin: 5px 0px 5px}" +
                "@media print { body { -webkit - print - color - adjust: exact; }}" +
                "\r\n</style>\r\n<!-- graphscript --></ head>\r\n<body>\r\n<!-- CLEMZoneBody -->";

            // apply theme based settings
            if (!darkTheme)
            {
                // light theme
                htmlString = htmlString.Replace("[FontColor]", "black");
                htmlString = htmlString.Replace("[HeaderFontColor]", "white");

                // resources
                htmlString = htmlString.Replace("[ResRowBack]", "floralwhite");
                htmlString = htmlString.Replace("[ResRowBack2]", "white");
                htmlString = htmlString.Replace("[ResContBack]", "floralwhite");
                htmlString = htmlString.Replace("[ResContBackLight]", "white");
                htmlString = htmlString.Replace("[ResContBackDark]", "floralwhite");
                htmlString = htmlString.Replace("[ResFontBanner]", "white");
                htmlString = htmlString.Replace("[ResFontContent]", "black");

                //activities
                htmlString = htmlString.Replace("[ActContBack]", "#fdffff");
                htmlString = htmlString.Replace("[ActContBackLight]", "#ffffff");
                htmlString = htmlString.Replace("[ActContBackDark]", "#fdffff");
                htmlString = htmlString.Replace("[ActContBackGroups]", "#ffffff");

                htmlString = htmlString.Replace("[ContDefaultBack]", "#FAFAFA");
                htmlString = htmlString.Replace("[ContDefaultBanner]", "#000");

                htmlString = htmlString.Replace("[ContFileBack]", "#FCFFFC");

                htmlString = htmlString.Replace("[CropRotationBack]", "#FFFFFF");
                htmlString = htmlString.Replace("[LabourGroupBack]", "#FFFFFF");
                htmlString = htmlString.Replace("[LabourGroupBorder]", "#996633");

                // filters
                htmlString = htmlString.Replace("[FiltContBack]", "#fbe8fc");
                htmlString = htmlString.Replace("[FiltContActivityBack]", "#cc33cc");

                // values
                htmlString = htmlString.Replace("[ValueSetBack]", "#e8fbfc");
                htmlString = htmlString.Replace("[ValueSetFont]", "#000000");
            }
            else
            {
                // dark theme
                htmlString = htmlString.Replace("[FontColor]", "#E5E5E5");
                htmlString = htmlString.Replace("[HeaderFontColor]", "black");

                // resources
                htmlString = htmlString.Replace("[ResRowBack]", "#281A0E");
                htmlString = htmlString.Replace("[ResRowBack2]", "#3F2817");
                htmlString = htmlString.Replace("[ResContBack]", "#281A0E");
                htmlString = htmlString.Replace("[ResContBackLight]", "#3F2817");
                htmlString = htmlString.Replace("[ResContBackDark]", "#281A0E");
                htmlString = htmlString.Replace("[ResFontBanner]", "#ffffff");
                htmlString = htmlString.Replace("[ResFontContent]", "#ffffff");

                //activities
                htmlString = htmlString.Replace("[ActContBack]", "#003F3D");
                htmlString = htmlString.Replace("[ActContBackLight]", "#005954");
                htmlString = htmlString.Replace("[ActContBackDark]", "#f003F3D");
                htmlString = htmlString.Replace("[ActContBackGroups]", "#f003F3D");

                htmlString = htmlString.Replace("[ContDefaultBack]", "#282828");
                htmlString = htmlString.Replace("[ContDefaultBanner]", "#686868");

                htmlString = htmlString.Replace("[ContFileBack]", "#0C440C");

                htmlString = htmlString.Replace("[CropRotationBack]", "#97B2B1");
                htmlString = htmlString.Replace("[LabourGroupBack]", "#c1946c");
                htmlString = htmlString.Replace("[LabourGroupBorder]", "#c1946c");

                // filters
                htmlString = htmlString.Replace("[FiltContBack]", "#5c195e");
                htmlString = htmlString.Replace("[FiltContActivityBack]", "#cc33cc");

                // values
                htmlString = htmlString.Replace("[ValueSetBack]", "#49adc4");
                htmlString = htmlString.Replace("[ValueSetFont]", "#0e2023");
            }

            using (StringWriter htmlWriter = new StringWriter())
            {
                if (!bodyOnly)
                {
                    htmlWriter.Write(htmlString);

                    if (apsimFilename == "")
                    {
                        htmlWriter.Write("\r\n<span style=\"font-size:0.8em; font-weight:bold\">You will need to keep refreshing this page to see changes relating to the last component selected</span><br /><br />");
                    }
                }
                htmlWriter.Write("\r\n<div class=\"clearfix defaultbanner\">");

                string fullname = modelToSummarise.Name;
                if (modelToSummarise is CLEMModel)
                {
                    fullname = (modelToSummarise as CLEMModel).NameWithParent;
                }

                if (apsimFilename != "")
                {
                    htmlWriter.Write($"<div class=\"namediv\">Full simulation settings</div>");
                }
                else
                {
                    htmlWriter.Write($"<div class=\"namediv\">Component {modelToSummarise.GetType().Name} named {fullname}</div>");
                }
                htmlWriter.Write($"<div class=\"typediv\">Details</div>");
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"defaultcontent\">");

                if(apsimFilename != "")
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Filename: {apsimFilename}</div>");
                    Model sim = (modelToSummarise as Model).FindAncestor<Simulation>();
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Simulation: {sim.Name}</div>");
                }

                htmlWriter.Write($"\r\n<div class=\"activityentry\">Summary last created on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()}</div>");
                htmlWriter.Write("\r\n</div>");

                if (modelToSummarise is ZoneCLEM)
                {
                    htmlWriter.Write((modelToSummarise as ZoneCLEM).GetFullSummary(modelToSummarise, true, htmlWriter.ToString()));
                }
                else if (modelToSummarise is Market)
                {
                    htmlWriter.Write((modelToSummarise as Market).GetFullSummary(modelToSummarise, true, htmlWriter.ToString()));
                }
                else if (modelToSummarise is CLEMModel)
                {
                    htmlWriter.Write((modelToSummarise as CLEMModel).GetFullSummary(modelToSummarise, false, htmlWriter.ToString()));
                }
                else if (modelToSummarise is ICLEMDescriptiveSummary)
                {
                    htmlWriter.Write((modelToSummarise as ICLEMDescriptiveSummary).GetFullSummary(modelToSummarise, false, htmlWriter.ToString()));
                }
                else
                {
                    htmlWriter.Write("<b>This component has no descriptive summary</b>");
                }

                if (!bodyOnly)
                {
                    htmlWriter.WriteLine("\r\n</body>\r\n</html>");
                }

                if (htmlWriter.ToString().Contains("<canvas"))
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    StreamReader textStreamReader = new StreamReader(assembly.GetManifestResourceStream("Models.Resources.CLEM.Chart.min.js"));
                    string graphString = textStreamReader.ReadToEnd();
                    if (!darkTheme)
                    {
                        graphString = graphString.Replace("[GraphGridLineColour]", "#eee");
                        graphString = graphString.Replace("[GraphGridZeroLineColour]", "#999");
                        graphString = graphString.Replace("[GraphPointColour]", "#00bcd6");
                        graphString = graphString.Replace("[GraphLineColour]", "#fda50f");
                        graphString = graphString.Replace("[GraphLabelColour]", "#888");
                    }
                    else
                    {
                        // dark theme
                        graphString = graphString.Replace("[GraphGridLineColour]", "#555");
                        graphString = graphString.Replace("[GraphGridZeroLineColour]", "#888");
                        graphString = graphString.Replace("[GraphPointColour]", "#00bcd6");
                        graphString = graphString.Replace("[GraphLineColour]", "#ff0");
                        graphString = graphString.Replace("[GraphLabelColour]", "#888");
                    }

                    return htmlWriter.ToString().Replace("<!-- graphscript -->", $"<script>{graphString}</script>");
                }
                return htmlWriter.ToString();
            }
        }



        #endregion
    }
}
