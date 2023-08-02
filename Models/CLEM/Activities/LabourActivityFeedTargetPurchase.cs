using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Target for feed activity purchases
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourActivityFeedToTargets))]
    [Description("Defines a food type for purchase towards targeted feeding")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourActivityFeedTargetPurchase.htm")]

    public class LabourActivityFeedTargetPurchase : CLEMModel
    {
        /// <summary>
        /// Name of food store
        /// </summary>
        [Description("Food store type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Food store required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(HumanFoodStore) } })]
        public string FoodStoreName { get; set; }

        /// <summary>
        /// Proportional purchase
        /// </summary>
        [Description("Proportion of remaining target")]
        [Proportion, GreaterThanEqualValue(0)]
        public double TargetProportion { get; set; }

        /// <summary>
        /// The final proportion to use. 
        /// </summary>
        [JsonIgnore]
        public double ProportionToPurchase { get; set; }


        /// <summary>
        /// The human food store linked to this target purchase
        /// </summary>
        [JsonIgnore]
        public HumanFoodStoreType FoodStore { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityFeedTargetPurchase()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");

                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(FoodStoreName, "Store not set"));
                htmlWriter.Write(" will be purchased to provide ");
                if (TargetProportion == 0)
                    htmlWriter.Write("<span class=\"errorlink\">NOT SET</span>: ");
                else
                    htmlWriter.Write("<span class=\"setvalue\">" + (TargetProportion).ToString("0.0%") + "</span>");
                htmlWriter.Write(" of remaining intake needed to meet current targets");
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
