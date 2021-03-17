using Models.Core;
using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Defines the labour required for an activity
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityManageProduct))]
    [ValidParent(ParentType = typeof(CropActivityTask))]
    [ValidParent(ParentType = typeof(RuminantActivityGrazeAll))]
    [ValidParent(ParentType = typeof(RuminantActivityGrazePasture))]
    [ValidParent(ParentType = typeof(RuminantActivityFeed))]
    [ValidParent(ParentType = typeof(RuminantActivityHerdCost))]
    [ValidParent(ParentType = typeof(RuminantActivityMilking))]
    [ValidParent(ParentType = typeof(RuminantActivitySellDryBreeders))]
    [ValidParent(ParentType = typeof(RuminantActivityWean))]
    [ValidParent(ParentType = typeof(ManureActivityCollectAll))]
    [ValidParent(ParentType = typeof(ManureActivityCollectPaddock))]
    [ValidParent(ParentType = typeof(RuminantActivityMove))]
    [ValidParent(ParentType = typeof(ResourceActivitySell))]
    [ValidParent(ParentType = typeof(ResourceActivityBuy))]
    [ValidParent(ParentType = typeof(ResourceActivityProcess))]
    [ValidParent(ParentType = typeof(PastureActivityCutAndCarry))]
    [ValidParent(ParentType = typeof(LabourActivityTask))]
    [ValidParent(ParentType = typeof(LabourActivityOffFarm))]
    [Description("Defines the amount and type of labour required for an activity. This model component must have at least one LabourFilterGroup nested below in the UI tree structure")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourRequirement.htm")]
    public class LabourRequirement: CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Link to resources
        /// </summary>
        [Link]
        public ResourcesHolder Resources = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourRequirement()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
            this.SetDefaults();
        }

        /// <summary>
        /// Days labour required per unit or fixed (days)
        /// </summary>
        [Description("Days labour required [per unit or fixed] (days)")]
        [Required, GreaterThanEqualValue(0)]
        public double LabourPerUnit { get; set; }

        /// <summary>
        /// Size of unit
        /// </summary>
        [Description("Number of units")]
        [Required, GreaterThanEqualValue(0)]
        public double UnitSize { get; set; }

        /// <summary>
        /// Whole unit blocks only
        /// </summary>
        [Description("Request as whole unit blocks only")]
        public bool WholeUnitBlocks { get; set; }

        /// <summary>
        /// Labour unit type
        /// </summary>
        [Description("Units to use")]
        [Required]
        public LabourUnitType UnitType { get; set; }

        /// <summary>
        /// Minimum labour allocated per person for task
        /// </summary>
        [Description("Minimum per person for task")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumPerPerson { get; set; }

        /// <summary>
        /// Maximum labour allocated per person for task
        /// </summary>
        [Description("Maximum per person for task")]
        [Required, GreaterThanValue(0), GreaterThan("MinimumPerPerson", ErrorMessage ="Maximum per task must be greater than minimum per task is Labour Required")]
        public double MaximumPerPerson { get; set; }

        /// <summary>
        /// Allow labour shortfall to affect activity
        /// </summary>
        [Description("Allow labour shortfall to affect activity")]
        [Required]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool LabourShortfallAffectsActivity { get; set; }

        /// <summary>
        /// Apply to all matching labour (everyone performs activity)
        /// </summary>
        [Description("Apply to all matching (everyone performs activity)")]
        [Required]
        public bool ApplyToAll { get; set; }

        #region validation

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            // ensure labour resource added
            Labour lab = Resources.GetResourceGroupByType(typeof(Labour)) as Labour;
            if (lab == null)
            {
                Summary.WriteWarning(this, "[a=" + this.Parent.Name + "][f=" + this.Name + "] No labour resorces in simulation. Labour requirement will be ignored.");
            }
            else
            {
                if (lab.Children.Count <= 0)
                {
                    Summary.WriteWarning(this, "[a=" + this.Parent.Name + "][f=" + this.Name + "] No labour resorce types are provided in the labour resource. Labour requirement will be ignored.");
                }
            }

            // check filter groups present
            if (this.Children.OfType<LabourFilterGroup>().Count() == 0)
            {
                Summary.WriteWarning(this, "No LabourFilterGroup is supplied with the LabourRequirement for [a=" + this.Parent.Name + "]. No labour will be used for this activity.");
            }

            // check for individual nesting.
            foreach (LabourFilterGroup fg in this.Children.OfType<LabourFilterGroup>())
            {
                LabourFilterGroup currentfg = fg;
                while (currentfg != null && currentfg.Children.OfType<LabourFilterGroup>().Count() >= 1)
                {
                    if (currentfg.Children.OfType<LabourFilterGroup>().Count() > 1)
                    {
                        string[] memberNames = new string[] { "Labour requirement" };
                        results.Add(new ValidationResult(String.Format("Invalid nested labour filter groups in [f={0}] for [a={1}]. Only one nested filter group is permitted each branch. Additional filtering will be ignored.", currentfg.Name, this.Name), memberNames));
                    }
                    currentfg = currentfg.Children.OfType<LabourFilterGroup>().FirstOrDefault();
                }
            }

            return results;
        }
        #endregion

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
                htmlWriter.Write("\r\n<div class=\"activityentry\"><span class=\"setvalue\">");
                // get amount
                htmlWriter.Write(LabourPerUnit.ToString() + "</span> days labour is required");
                if (UnitType != LabourUnitType.Fixed)
                {
                    if (UnitSize == 1)
                    {
                        htmlWriter.Write(" for each ");
                    }
                    else
                    {
                        htmlWriter.Write(" for every <span class=\"setvalue\">" + UnitSize.ToString("#,##0.##") + "</span>");
                    }
                    htmlWriter.Write("<span class=\"setvalue\">" + UnitType2HTML() + "</span>");
                    if (WholeUnitBlocks)
                    {
                        htmlWriter.Write(" and will be supplied in blocks");
                    }
                }
                htmlWriter.Write("</div>");

                if (MinimumPerPerson > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">Labour will not be supplied if less than <span class=\"setvalue\">" + MinimumPerPerson.ToString() + "</span> day" + ((MinimumPerPerson == 1) ? "" : "s") + " is required</div>");
                }
                if (MaximumPerPerson > 0 && MaximumPerPerson < 30)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">No individual can provide more than <span class=\"setvalue\">" + MaximumPerPerson.ToString() + "</span> days</div>");
                }
                if (ApplyToAll)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">All people matching the below criteria (first level) will perform this task. (e.g. all children)</div>");
                }

                return htmlWriter.ToString(); 
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            return "\r\n</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"labourgroupsborder\">");
                htmlWriter.Write("<div class=\"labournote\">The required labour will be taken from each of the following groups</div>");

                if (this.FindAllChildren<LabourFilterGroup>().Count() == 0)
                {
                    htmlWriter.Write("\r\n<div class=\"filterborder clearfix\">");
                    htmlWriter.Write("<div class=\"filtererror\">No filter group provided</div>");
                    htmlWriter.Write("</div>");
                }
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

        private string UnitType2HTML()
        {
            switch (UnitType)
            {
                case LabourUnitType.Fixed:
                    return "";
                case LabourUnitType.perHa:
                    return "hectare";
                case LabourUnitType.perUnitOfLand:
                    return "land unit";
                case LabourUnitType.perTree:
                    return "tree";
                case LabourUnitType.perHead:
                    return "head";
                case LabourUnitType.perAE:
                    return "AE";
                case LabourUnitType.perKg:
                    return "kg";
                case LabourUnitType.perUnit:
                    return "unit";
                default:
                    return "Unknown";
            }
        }

    }
}
