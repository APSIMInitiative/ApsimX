using Models.Core;
using Models.CLEM.Resources;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Models.CLEM.Interfaces;

namespace Models.CLEM
{
    ///<summary>
    /// Resource transmutation labour cost item
    /// Determines the amount of labour required for the transmutation
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("This Transmutation cost specifies how much of a given resource (e.g. money) is needed to convert to the needed resource. Any number of these can be supplied under a Transmutation such that you may need money and labour to purchase supplements.")]
    [HelpUri(@"Content/Features/Transmutation/TransmutationCostLabour.htm")]
    public class TransmutationCostLabour : CLEMModel, ITransmutationCost
    {
        /// <summary>
        /// Type of resource to use
        /// </summary>
        [JsonIgnore]
        [field: NonSerialized]
        public Type ResourceType { get; set; }

        /// <summary>
        /// Cost of transmutation
        /// </summary>
        [Description("Days per unit required")]
        [Required, GreaterThanEqualValue(0)]
        public double CostPerUnit { get; set; }

        /// <summary>
        /// Name of resource type to use
        /// Not used in this model but part of interface
        /// </summary>
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TransmutationCostLabour()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ResourceType = typeof(Labour);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if (CostPerUnit > 0)
            {
                html += "<div class=\"activityentry\">";
                html += "<span class=\"setvalue\">" + CostPerUnit.ToString("#,##0.##") + "</span> days from ";
                html += "</div>";
            }
            else
            {
                html += "<div class=\"errorlink\">";
                html += "Invalid transmutation cost. No days labour per unit provided.";
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
