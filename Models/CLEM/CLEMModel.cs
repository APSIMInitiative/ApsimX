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

namespace Models.CLEM
{
    ///<summary>
    /// CLEM base model
    ///</summary> 
    [Serializable]
    [Description("This is the Base CLEM model and should not be used directly.")]
    public abstract class CLEMModel : Model, ICLEMUI
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
        public string NameWithParent => $"{Parent.Name}.{this.Name}";

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
            string html = "";
            if (model.GetType().IsSubclassOf(typeof(CLEMModel)))
            {
                CLEMModel cm = model as CLEMModel;
                html += cm.ModelSummaryOpeningTags(formatForParentControl);

                html += cm.ModelSummaryInnerOpeningTagsBeforeSummary();

                html += cm.ModelSummary(formatForParentControl);

                html += cm.ModelSummaryInnerOpeningTags(formatForParentControl);

                foreach (var item in (model as IModel).Children)
                {
                    html += GetFullSummary(item, true, htmlString);
                }
                html += cm.ModelSummaryInnerClosingTags(formatForParentControl);

                html += cm.ModelSummaryClosingTags(formatForParentControl);
            }
            return html;
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
            return "\n</div>\n</div>";
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

            string html = "";
            html += "\n<div class=\"holder" + ((extra == "") ? "main" : "sub") + " " + overall + "\" style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + ";\">";
            html += "\n<div class=\"clearfix " + overall + "banner" + extra + "\">" + this.ModelSummaryNameTypeHeader() + "</div>";
            html += "\n<div class=\"" + overall + "content" + ((extra != "") ? extra : "") + "\">";

            return html;
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
            string html = "";
            if (this.GetType().IsSubclassOf(typeof(CLEMResourceTypeBase)))
            {
                // add units when completed
                string units = (this as IResourceType).Units;
                if (units != "NA")
                {
                    html += "\n<div class=\"activityentry\">This resource is measured in  ";
                    if (units == null || units == "")
                    {
                        html += "<span class=\"errorlink\">NOT SET</span>";
                    }
                    else
                    {
                        html += "<span class=\"setvalue\">" + units + "</span>";
                    }
                    html += "</div>";
                }
            }
            if (this.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
            {
                if (this.Children.Count() == 0)
                {
                    html += "\n<div class=\"activityentry\">Empty</div>";
                }
            }
            return html;
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
            string html = "";
            html += "<div class=\"namediv\">" + this.Name + ((!this.Enabled) ? " - DISABLED!" : "") + "</div>";
            if (this.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
            {
                html += "<div class=\"partialdiv\"";
                switch ((this as CLEMActivityBase).OnPartialResourcesAvailableAction)
                {
                    case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                        html += " tooltip = \"Error and Stop on insufficient resources\">Stop";
                        break;
                    case OnPartialResourcesAvailableActionTypes.SkipActivity:
                        html += ">Skip";
                        break;
                    case OnPartialResourcesAvailableActionTypes.UseResourcesAvailable:
                        html += ">Partial";
                        break;
                    default:
                        break;
                }
                html += "</div>";
            }
            html += "<div class=\"typediv\">" + this.GetType().Name + "</div>";
            return html;
        }

        #endregion    }
    }
}
