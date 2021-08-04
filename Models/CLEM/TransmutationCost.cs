using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using Models.CLEM.Interfaces;

namespace Models.CLEM
{
    ///<summary>
    /// Resource transmutation cost item
    /// Determines the amount of resource required for the transmutation
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("This Transmutation cost specifies how much of a given resource (e.g. money) is needed to convert to the needed resource. Any number of these can be supplied under a Transmutation such that you may need money and labour to purchase supplements.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Transmutation/TransmutationCost.htm")]
    public class TransmutationCost : CLEMModel, IValidatableObject, ITransmutationCost
    {
        [JsonIgnore]
        [Link]
        private ResourcesHolder Resources = null;

        /// <summary>
        /// Type of resource to use
        /// </summary>
        [JsonIgnore]
        [field: NonSerialized]
        public Type ResourceType { get; set; }

        /// <summary>
        /// Name of resource type to use
        /// </summary>
        [Description("Name of Resource Type to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore), typeof(Finance), typeof(HumanFoodStore), typeof(GreenhouseGases), typeof(Labour), typeof(ProductStore), typeof(WaterStore) } })]
        [Required]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Cost of transmutation
        /// </summary>
        [Description("Amount per unit required")]
        [Required, GreaterThanEqualValue(0)]
        public double CostPerUnit { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TransmutationCost()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        #region validation

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (ResourceTypeName != null && ResourceTypeName != "")
            {
                if (!ResourceTypeName.Contains("."))
                {
                    string[] memberNames = new string[] { "ResourceTypeName" };
                    results.Add(new ValidationResult("Invalid resource type entry. Please select resource type from the drop down list provided or ensure the value is formatted as ResourceGroup.ResourceType", memberNames));
                }
                else
                {
                    object result = Resources.GetResourceGroupByName(ResourceTypeName.Split('.').First());
                    if (result == null)
                    {
                        Summary.WriteWarning(this, $"Could not find resource group [r={ResourceTypeName.Split('.').First()}] in transmutation cost [{this.Name}]{Environment.NewLine}Finances will not be considered or limit this transmutation ");
                    }
                    else
                    {
                        object resultType = Resources.GetResourceItem(this, ResourceTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
                        if (resultType is null)
                        {
                            string[] memberNames = new string[] { "ResourceType" };
                            results.Add(new ValidationResult($"Could not find resource [r={ResourceTypeName.Split('.').First()}][r={ResourceTypeName.Split('.').Last()}] in transmutation cost", memberNames));
                        }
                    }
                }
            }
            return results;
        } 
        #endregion

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // determine resource type from name
            object result = Resources.GetResourceGroupByName(ResourceTypeName.Split('.').First());
            if (result != null)
            {
                ResourceType = result.GetType();
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if (CostPerUnit > 0)
            {
                html += "<div class=\"activityentry\">";
                html += "<span class=\"setvalue\">" + CostPerUnit.ToString("#,##0.##") + "</span> x ";
                html += (ResourceTypeName != null && ResourceTypeName != "") ? "<span class=\"resourcelink\">" + ResourceTypeName + "</span>" : "<span class=\"errorlink\">Unknown Resource</span>";
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

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "";
        }

        #endregion
    }

}
