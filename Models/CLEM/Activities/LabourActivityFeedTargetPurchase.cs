using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Target for feed activity
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourActivityFeedToTargets))]
    [Description("This component defines a food type for purchase towards targeted feeding")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourActivityFeedTargetPurchase.htm")]

    public class LabourActivityFeedTargetPurchase : CLEMModel
    {
        /// <summary>
        /// Name of food store
        /// </summary>
        [Description("Food store type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Food store required")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(HumanFoodStore) })]
        public string FoodStoreName { get; set; }

        /// <summary>
        /// Proportional purchase
        /// </summary>
        [Description("Proportion of remaining target")]
        [Proportion, GreaterThanValue(0)]
        public double TargetProportion { get; set; }


        /// <summary>
        /// The final proportion to use. 
        /// </summary>
        public double ProportionToPurchase { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityFeedTargetPurchase()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (FoodStoreName == null || FoodStoreName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">[ACCOUNT NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + FoodStoreName + "</span>");
                }
                htmlWriter.Write(" will be purchased to provide ");
                if (TargetProportion == 0)
                {
                    htmlWriter.Write("<span class=\"errorlink\">NOT SET</span>: ");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + (TargetProportion).ToString("0.0%") + "</span>");
                }
                htmlWriter.Write(" of remaining intake needed to meet current targets");
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
