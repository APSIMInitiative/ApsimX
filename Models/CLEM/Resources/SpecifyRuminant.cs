using Models.CLEM.Activities;
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

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A component to specify the details of a ruminant to be used
    /// Use to define purchases etc.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStockingENSO))]
    [ValidParent(ParentType = typeof(RuminantActivityTrade))]
    [Description("This component allows the details of a individual ruminant to be defined for")]
    [HelpUri(@"Content/Features/Filters/SpecifyRuminant.htm")]
    public class SpecifyRuminant : CLEMModel, IValidatableObject
    {
        [Link]
        ResourcesHolder Resources = null;

        /// <summary>
        /// Records if a warning about set weight occurred
        /// </summary>
        public bool WeightWarningOccurred = false;

        /// <summary>
        /// The type of ruminant
        /// </summary>
        [Description("Type of Ruminant")]
        [Models.Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(RuminantHerd) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Ruminant type required")]
        public string RuminantTypeName { get; set; }

        /// <summary>
        /// Proportion of individuals of this type
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Description("Proportion of this specification")]
        [Required, GreaterThanValue(0), Proportion]
        public double Proportion { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [field: NonSerialized]
        public RuminantTypeCohort Details { get; private set; }

        private RuminantType ruminantType;

        /// <summary>
        /// The ruminant type for this specified ruminant
        /// </summary>
        public RuminantType BreedParams { get { return ruminantType;} }

        /// <summary>
        /// Constructor
        /// </summary>
        public SpecifyRuminant()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResources")]
        private void OnCLEMInitialiseResources(object sender, EventArgs e)
        {
            Details = this.FindAllChildren<RuminantTypeCohort>().FirstOrDefault();
            ruminantType = Resources.GetResourceItem(this.Parent as Model, RuminantTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as RuminantType;
        }

        #region validation
        /// <summary>
        /// Validate this model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // check that this model contains children RuminantDestockGroups with filters
            var results = new List<ValidationResult>();
            // check that this activity contains at least one RuminantGroup with Destock reason (filters optional as someone might want to include entire herd)
            
            if(ruminantType is null)
            {
                string[] memberNames = new string[] { "Ruminant type" };
                results.Add(new ValidationResult("An invalid [r=RuminantType] was specified", memberNames));
            }

            if (this.FindAllChildren<RuminantTypeCohort>().Count() != 1)
            {
                string[] memberNames = new string[] { "Specify ruminant" };
                results.Add(new ValidationResult("A single [r=RuminantTypeCohort] must be present under each [f=SpecifyRuminant] component", memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary

        private bool cohortFound;

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"\r\n<div class=\"activityentry\"><span class=\"setvalue\">{Proportion.ToString("p0")}</span> of the individuals will be ");
                if (RuminantTypeName == null || RuminantTypeName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">TYPE NOT SET</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + RuminantTypeName + "</span>");
                }
                cohortFound = FindAllChildren<RuminantTypeCohort>().Count() > 0;
                if (cohortFound)
                {
                    htmlWriter.Write($" with the following details:</div>");
                }
                else
                {
                    htmlWriter.Write($"</div>");
                    htmlWriter.Write($"\r\n<div class=\"activityentry\"><span class=\"errorlink\">No <span class=\"resourcelink\">RuminantCohort</span> describing the individuals was provided!</div>");
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
            string html = "";
            //if (cohortFound)
            //{
            //    html = "</table></div>";
            //}

            //if (WeightWarningOccurred)
            //{
            //    html += "</br><span class=\"errorlink\">Warning: Initial weight differs from the expected normalised weight by more than 20%</span>";
            //}
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            //WeightWarningOccurred = false;
            //if (cohortFound)
            //{
            //    return "<div class=\"activityentry\"><table><tr><th>Name</th><th>Gender</th><th>Age</th><th>Weight</th><th>Norm.Wt.</th><th>Number</th><th>Suckling</th><th>Sire</th></tr>"; 
            //}
            return "";
        }

        #endregion


    }
}
