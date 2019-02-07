using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM
{
    ///<summary>
    /// CLEM base model
    ///</summary> 
    [Serializable]
    [Description("This is the Base CLEM model and should not be used directly.")]
    public abstract class CLEMModel: Model
    {
        /// <summary>
        /// Link to summary
        /// </summary>
        [Link]
        public ISummary Summary = null;

        private Guid id = Guid.NewGuid();

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
        public string UniqueID { get { return id.ToString(); } }

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
                        try
                        {
                            //Is it an array?
                            if (property.PropertyType.IsArray)
                            {
                                property.SetValue(this, dv.Value, null);
                            }
                            else
                            {
                                //Use set value for.. not arrays
                                property.SetValue(this, dv.Value, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            Summary.WriteWarning(this, ex.Message);
                            //eat it... Or maybe Debug.Writeline(ex);
                        }
                    }
                }
            }
        }

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
        [XmlIgnore]
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

            if(this.ModelSummaryStyle == HTMLSummaryStyle.Default)
            {
                if (this.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
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
                else if (this.GetType().IsSubclassOf(typeof(CLEMModel)))
                {
                    this.ModelSummaryStyle = HTMLSummaryStyle.Activity;
                }
            }

            switch (ModelSummaryStyle)
            {
                case HTMLSummaryStyle.Default:
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
            html += "\n<div class=\"holder"+ ((extra == "") ? "main" : "sub") + " " + overall  +"\">";
            html += "\n<div class=\"clearfix "+overall+"banner"+extra+"\">" + this.ModelSummaryNameTypeHeader() + "</div>";
            html += "\n<div class=\""+overall+"content"+  ((extra!="")? extra: "")+"\">";

            if(this.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
            {
                //html += "\n<div class=\"activityentry\">This resource is measured in ";
                //if((this as ResourceBaseWithTransactions).Units != "")
                //{
                //    html += "<span class=\"setvalue\">" + (this as ResourceBaseWithTransactions).Units + "</span> ";
                //}
                //html += "</div>";
            }
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
            return "";
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
            html += "<div class=\"namediv\">" + this.Name + "</div>";
            if (this.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
            {
                html += "<div class=\"partialdiv\"";
                switch ((this as CLEMActivityBase).OnPartialResourcesAvailableAction)
                {
                    case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                        html += " tooltip = \"Error and Stop on insifficient resources\">Stop";
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
    }
}
