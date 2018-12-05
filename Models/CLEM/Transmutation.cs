using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Models.Core.Attributes;

namespace Models.CLEM
{
    ///<summary>
    /// Resource transmutation
    /// Will convert one resource into another (e.g. $ => labour) 
    /// These re defined under each ResourceType in the Resources section of the UI tree
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IResourceType))]
    [Description("This Transmutation will convert any other resource into the current resource where there is a shortfall. This is placed under any resource type where you need to provide a transmutation. For example to convert Finance Type (money) into a Animal Food Store Type (Lucerne) or effectively purchase fodder when low.")]
    [Version(1, 0, 1, "", "CSIRO", "")]
    public class Transmutation: CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Transmutation()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <summary>
        /// Amount of this resource per unit purchased
        /// </summary>
        [Description("Amount of this resource per unit purchased")]
        [Required, GreaterThanEqualValue(1)]
        public double AmountPerUnitPurchase { get; set; }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (this.Children.Where(a => a.GetType() == typeof(TransmutationCost) | a.GetType() == typeof(TransmutationCostLabour)).Count() == 0) //   Apsim.Children (this, typeof(TransmutationCost)).Count() == 0)
            {
                string[] memberNames = new string[] { "TransmutationCosts" };
                results.Add(new ValidationResult("No costs provided under this transmutation", memberNames));
            }
            return results;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            string html = "<div class=\"activityentry\">";
            if (AmountPerUnitPurchase > 0)
            {
                html += "When needed <span class=\"setvalue\">" + AmountPerUnitPurchase.ToString("#,##0.##") + "</span> of this resource will be converted from";
            }
            else
            {
                html += "Invalid transmutation provided. No amout to purchase set";
            }
            html += "</div>";
            if (this.Children.OfType<TransmutationCost>().Count() + this.Children.OfType<TransmutationCostLabour>().Count() == 0)
            {
                html += "<div class=\"activityentry\">";
                html += "Invalid transmutation provided. No transmutation costs provided";
                html += "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool FormatForParentControl)
        {
            string html = "";
            html += "\n</div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool FormatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activitycontent clearfix\">";
            if (!(Apsim.Children(this, typeof(TransmutationCost)).Count() >= 1))
            {
                html += "<div class=\"errorlink\">No transmutation costs provided</div>";
            }
            return html;
        }


    }

    ///<summary>
    /// Resource transmutation cost item
    /// Determines the amount of resource required for the transmutation
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("This Transmutation cost specifies how much of a given resource (e.g. money) is needed to convert to the needed resource. Any number of these can be supplied under a Transmutation such that you may need money and labour to purchase supplements.")]
    [Version(1, 0, 1, "", "CSIRO", "")]
    public class TransmutationCost : CLEMModel, IValidatableObject
    {
        [XmlIgnore]
        [Link]
        private ResourcesHolder Resources = null;

        /// <summary>
        /// Name of resource to use
        /// </summary>
        [Description("Name of Resource to use")]
        [Required]
        public string ResourceName { get; set; }

        /// <summary>
        /// Type of resource to use
        /// </summary>
        public Type ResourceType { get; set; }

        /// <summary>
        /// Name of resource type to use
        /// </summary>
        [Description("Name of Resource Type to use")]
        [Required]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Cost of transmutation
        /// </summary>
        [Description("Cost per unit")]
        [Required, GreaterThanEqualValue(0)]
        public double CostPerUnit { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TransmutationCost()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            object result = Resources.GetResourceGroupByName(ResourceName);
            if (result == null)
            {
                string[] memberNames = new string[] { "ResourceTypeName" };
                results.Add(new ValidationResult("Could not find resource " + this.ResourceName + " in transmutation cost", memberNames));
            }
            return results;
        }

        // This was in commencing, but I don't think there is any reason it has to be
        // could be a problem in future, thus this message.


        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // determine resource type from name
            object result = Resources.GetResourceGroupByName(ResourceName);
            ResourceType = result.GetType();
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            string html = "";
            if (CostPerUnit > 0)
            {
                html += "<div class=\"activityentry\">";
                html += "<span class=\"setvalue\">"+CostPerUnit.ToString() + "</span> x ";
                html += (ResourceName!=null & ResourceName!="")? "<span class=\"setvalue\">" + ResourceName+"</span>.": "<span class=\"errorlink\">Unknown Resource</span>.";
                html += (ResourceTypeName != null & ResourceTypeName != "") ? "<span class=\"setvalue\">" + ResourceTypeName + "</span>" : "<span class=\"errorlink\">Unknown Type</span>";
                html += "</div>";
            }
            else
            {
                html += "<div class=\"errorlink\">";
                html += "Invalid transmutation cost. No cost per unit provided.";
                html += "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool FormatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool FormatForParentControl)
        {
            return "";
        }

    }

    ///<summary>
    /// Resource transmutation cost item
    /// Determines the amount of resource required for the transmutation
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("This Transmutation cost specifies how much of a given resource (e.g. money) is needed to convert to the needed resource. Any number of these can be supplied under a Transmutation such that you may need money and labour to purchase supplements.")]
    public class TransmutationCostLabour : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Type of resource to use
        /// </summary>
        public Type ResourceType { get; set; }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            return results;
        }

        // This was in commencing, but I don't think there is any reason it has to be
        // could be a problem in future, thus this message.

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ResourceType = typeof(Labour);
        }
    }


}
