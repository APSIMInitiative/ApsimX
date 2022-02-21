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
using System.Text.Json.Serialization;
using Models.CLEM.Interfaces;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Defines the labour required for an activity
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(ICanHandleIdentifiableChildModels))]
    //[ValidParent(ParentType = typeof(CropActivityManageProduct))]
    //[ValidParent(ParentType = typeof(CropActivityTask))]
    //[ValidParent(ParentType = typeof(RuminantActivityGrazeAll))]
    //[ValidParent(ParentType = typeof(RuminantActivityGrazePasture))]
    //[ValidParent(ParentType = typeof(RuminantActivityFeed))]
    //[ValidParent(ParentType = typeof(RuminantActivityHerdCost))]
    //[ValidParent(ParentType = typeof(RuminantActivityMilking))]
    //[ValidParent(ParentType = typeof(RuminantActivityWean))]
    //[ValidParent(ParentType = typeof(ManureActivityCollectAll))]
    //[ValidParent(ParentType = typeof(ManureActivityCollectPaddock))]
    //[ValidParent(ParentType = typeof(RuminantActivityMove))]
    //[ValidParent(ParentType = typeof(ResourceActivitySell))]
    //[ValidParent(ParentType = typeof(ResourceActivityBuy))]
    //[ValidParent(ParentType = typeof(ResourceActivityProcess))]
    //[ValidParent(ParentType = typeof(PastureActivityCutAndCarry))]
    //[ValidParent(ParentType = typeof(LabourActivityTask))]
    //[ValidParent(ParentType = typeof(LabourActivityOffFarm))]
    //[ValidParent(ParentType = typeof(RuminantActivityControlledMating))]
    [Description("Defines the amount and type of labour required for an activity. This component must have at least one LabourFilterGroup as a child")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourRequirement.htm")]
    public class LabourRequirement: CLEMModel, IValidatableObject, IIdentifiableChildModel
    {
        /// <summary>
        /// Link to resources
        /// </summary>
        [Link]
        public ResourcesHolder Resources = null;
        private double maximumDaysPerPerson = 0;
        private double maximumDaysPerGroup = 0;
        private double minimumDaysPerPerson = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourRequirement()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
            this.SetDefaults();
        }

        /// <summary>
        /// An identifier for this Labour requirement based on parent requirements
        /// </summary>
        [Description("Labour identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers")]
        public string Identifier { get; set; }

        /// <summary>
        /// Days labour required per unit or fixed (days)
        /// </summary>
        [Description("Days labour required [per unit or fixed]")]
        [Required, GreaterThanEqualValue(0)]
        [Category("Labour", "Rate")]
        public double LabourPerUnit { get; set; }

        /// <summary>
        /// Size of unit
        /// </summary>
        [Description("Number of units")]
        [Required, GreaterThanEqualValue(0)]
        [Category("Labour", "Units")]
        public double UnitSize { get; set; }

        /// <summary>
        /// Whole unit blocks only
        /// </summary>
        [Description("Request as whole unit blocks only")]
        [Category("Labour", "Units")]
        public bool WholeUnitBlocks { get; set; }

        /// <summary>
        /// Labour unit type
        /// </summary>
        [Description("Units to use")]
        [Category("Labour", "Units")]
        [Required]
        public string Units { get; set; }

        /// <summary>
        /// Labour limit style
        /// </summary>
        [Description("Limit style")]
        [System.ComponentModel.DefaultValueAttribute(LabourLimitType.ProportionOfDaysRequired)]
        [Category("Labour", "Limits")]
        [Required]
        public LabourLimitType LimitStyle { get; set; }

        /// <summary>
        /// Maximum labour allocated per labour group
        /// </summary>
        [Description("Maximum per group for task")]
        [Required, GreaterThanValue(0)]
        [Category("Labour", "Limits")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        public double MaximumPerGroup { get; set; }

        /// <summary>
        /// Minimum labour allocated per person for task
        /// </summary>
        [Description("Minimum per person for task")]
        [Required, GreaterThanEqualValue(0)]
        [Category("Labour", "Limits")]
        public double MinimumPerPerson { get; set; }

        /// <summary>
        /// Maximum labour allocated per person for task
        /// </summary>
        [Description("Maximum per person for task")]
        [Required, GreaterThanValue(0), GreaterThan("MinimumPerPerson", ErrorMessage ="Maximum per individual must be greater than minimum per individual in Labour Required")]
        [Category("Labour", "Limits")]
        public double MaximumPerPerson { get; set; }

        /// <summary>
        /// Allow shortfall to affect activity
        /// </summary>
        [Description("Allow labour shortfall to affect activity")]
        [Required]
        [System.ComponentModel.DefaultValueAttribute(false)]
        [Category("Labour", "General")]
        public bool ShortfallAffectsActivity { get; set; }

        /// <summary>
        /// Apply to all matching labour (everyone performs activity)
        /// </summary>
        [Description("Apply to all matching (everyone performs activity)")]
        [Required]
        [Category("Labour", "Rate")]
        public bool ApplyToAll { get; set; }

        /// <inheritdoc/>
        public string Measure { get { return "none"; } }

        /// <summary>
        /// Get the calculated maximum days per person for activity from CalculateLimits
        /// </summary>
        [JsonIgnore]
        public double MaximumDaysPerPerson { get { return maximumDaysPerPerson; } }

        /// <summary>
        /// Get the calculated maximum days per person for activity from CalculateLimits
        /// </summary>
        [JsonIgnore]
        public double MaximumDaysPerGroup { get { return maximumDaysPerGroup; } }

        /// <summary>
        /// Get the calculated maximum days per person for activity from CalculateLimits
        /// </summary>
        [JsonIgnore]
        public double MinimumDaysPerPerson { get { return minimumDaysPerPerson; } }


        /// <summary>
        /// Calcuate the limits for people and groups using the style
        /// </summary>
        public void CalculateLimits(double amountRequested)
        {
            switch (LimitStyle)
            {
                case LabourLimitType.AsDaysRequired:
                    double units = amountRequested / UnitSize / LabourPerUnit;
                    maximumDaysPerPerson = units * MaximumPerPerson;
                    maximumDaysPerGroup = units * MaximumPerGroup;
                    minimumDaysPerPerson = units * MinimumPerPerson;
                    break;
                case LabourLimitType.AsTotalAllowed:
                    maximumDaysPerPerson = MaximumPerPerson;
                    maximumDaysPerGroup = MaximumPerGroup;
                    minimumDaysPerPerson = MinimumPerPerson;
                    break;
                case LabourLimitType.ProportionOfDaysRequired:
                    maximumDaysPerPerson = amountRequested * MaximumPerPerson;
                    maximumDaysPerGroup = amountRequested * MaximumPerGroup;
                    minimumDaysPerPerson = amountRequested * MinimumPerPerson;
                    break;
                default:
                    break;
            }
            return;            
        }

        /// <inheritdoc/>
        public List<ResourceRequest> GetResourceRequests(double activityMetric)
        {
            return new List<ResourceRequest>();
        }

        #region validation

        /// <summary>
        /// A method to return the list of identifiers relavent to this parent activity
        /// </summary>
        /// <returns>A list of identifiers</returns>
        public List<string> ParentSuppliedIdentifiers()
        {
            if (Parent != null && Parent is ICanHandleIdentifiableChildModels)
                return (Parent as ICanHandleIdentifiableChildModels).DefineIdentifiableChildModelLabels<LabourRequirement>().Identifiers;
            else
                return new List<string>();
        }

        /// <summary>
        /// A method to return the list of units relavent to this parent activity
        /// </summary>
        /// <returns>A list of units</returns>
        public List<string> ParentSuppliedUnits()
        {
            if (Parent != null && Parent is ICanHandleIdentifiableChildModels)
                return (Parent as ICanHandleIdentifiableChildModels).DefineIdentifiableChildModelLabels<LabourRequirement>().Units;
            else
                return new List<string>();
        }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            // ensure labour resource added
            Labour lab = Resources.FindResource<Labour>();
            if (lab == null)
                Summary.WriteMessage(this, "[a=" + this.Parent.Name + "][f=" + this.Name + "] No labour resorces in simulation. Labour requirement will be ignored.", MessageType.Warning);
            else if (lab.Children.Count <= 0)
                Summary.WriteMessage(this, "[a=" + this.Parent.Name + "][f=" + this.Name + "] No labour resorce types are provided in the labour resource. Labour requirement will be ignored.", MessageType.Warning);

            // check filter groups present
            if (this.Children.OfType<LabourFilterGroup>().Count() == 0)
                Summary.WriteMessage(this, "No LabourFilterGroup is supplied with the LabourRequirement for [a=" + this.Parent.Name + "]. No labour will be used for this activity.", MessageType.Warning);

            // check for individual nesting.
            foreach (LabourFilterGroup fg in this.FindAllChildren<LabourFilterGroup>())
            {
                LabourFilterGroup currentfg = fg;
                while (currentfg != null && currentfg.FindAllChildren<LabourFilterGroup>().Any())
                {
                    if (currentfg.FindAllChildren<LabourFilterGroup>().Count() > 1)
                    {
                        string[] memberNames = new string[] { "Labour requirement" };
                        results.Add(new ValidationResult(String.Format("Invalid nested labour filter groups in [f={0}] for [a={1}]. Only one nested filter group is permitted each branch. Additional filtering will be ignored.", currentfg.Name, this.Name), memberNames));
                    }
                    currentfg = currentfg.FindAllChildren<LabourFilterGroup>().FirstOrDefault();
                }
            }

            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\"><span class=\"setvalue\">");
                // get amount
                htmlWriter.Write($"{LabourPerUnit}</span> days labour is required");
                if (Units.ToUpper() != "Fixed")
                {
                    if (UnitSize == 1)
                        htmlWriter.Write(" for each ");
                    else
                        htmlWriter.Write($" for every <span class=\"setvalue\">{UnitSize:#,##0.##}</span>");

                    htmlWriter.Write($"<span class=\"setvalue\">{Units}</span>");
                    if (WholeUnitBlocks)
                        htmlWriter.Write(" and will be supplied in blocks");
                }
                htmlWriter.Write("</div>");
                htmlWriter.Write($"\r\n<div class=\"activityentry\">Labour will be limited ");
                switch (LimitStyle)
                {
                    case LabourLimitType.AsDaysRequired:
                        htmlWriter.Write($"as the days required</div>");
                        break;
                    case LabourLimitType.AsTotalAllowed:
                        htmlWriter.Write($"as the total days permitted in the month</div>");
                        break;
                    case LabourLimitType.ProportionOfDaysRequired:
                        htmlWriter.Write($"as a proportion of the days required and therefore total required</div>");
                        break;
                    default:
                        break;
                }

                if (MaximumPerGroup > 0)
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Labour will be supplied for each filter group up to <span class=\"setvalue\">{MaximumPerGroup}</span> day{((MaximumPerGroup == 1) ? "" : "s")} is required</div>");

                if (MinimumPerPerson > 0)
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Labour will not be supplied if less than <span class=\"setvalue\">{MinimumPerPerson}</span> day{((MinimumPerPerson == 1) ? "" : "s")} is required</div>");

                if (MaximumPerPerson > 0 && MaximumPerPerson < 30)
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">No individual can provide more than <span class=\"setvalue\">{MaximumPerPerson}</span> days</div>");

                if (ApplyToAll)
                    htmlWriter.Write("\r\n<div class=\"activityentry\">All people matching the below criteria (first level) will perform this task. (e.g. all children)</div>");

                return htmlWriter.ToString(); 
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            return "\r\n</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
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


    }
}
