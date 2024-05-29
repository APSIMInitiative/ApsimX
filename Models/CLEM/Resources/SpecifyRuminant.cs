using DocumentFormat.OpenXml.Spreadsheet;
using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A component to specify the details of a ruminant to be used
    /// Use to define purchases etc.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStockingENSO))]
    [ValidParent(ParentType = typeof(RuminantActivityPurchase))]
    [ValidParent(ParentType = typeof(RuminantActivityManage))]
    [Description("Specify the details of a individual ruminant to be used by an activity")]
    [HelpUri(@"Content/Resources/Ruminanta/SpecifyRuminant.htm")]
    [Version(1, 0, 1, "Includes attribute specification")]
    public class SpecifyRuminant : CLEMModel, IValidatableObject
    {
        [Link(IsOptional = true)]
        private readonly ResourcesHolder resources = null;
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;

        private RuminantType ruminantType;

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
        /// The local store of the first type Cohort provided as child of this component
        /// </summary>
        [JsonIgnore]
        public RuminantTypeCohort Details { get; private set; }

        /// <summary>
        /// The local store of an example individual for checking against filters
        /// </summary>
        [JsonIgnore]
        public Ruminant ExampleIndividual { get; private set; }

        /// <summary>
        /// The ruminant type for this specified ruminant
        /// </summary>
        public RuminantType BreedType { get { return ruminantType; } }

        /// <summary>
        /// Records if a warning about set weight occurred
        /// </summary>
        [JsonIgnore]
        public bool WeightWarningOccurred { get; private set; } = false;

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
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            Details = this.FindAllChildren<RuminantTypeCohort>().FirstOrDefault();
            ruminantType = resources.FindResourceType<RuminantHerd, RuminantType>(this.Parent as Model, RuminantTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            if (Details is not null && ruminantType.Parameters.General is not null)
            {
                // create example ruminant
                Details.Number = 1;
                ExampleIndividual = Details.CreateIndividuals(null, events.Clock.Today, BreedType).FirstOrDefault();
            }
        }

        #region validation
        /// <inheritdoc/>>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // check that this model contains children RuminantDestockGroups with filters
            // check that this activity contains at least one RuminantGroup with Destock reason (filters optional as someone might want to include entire herd)
            if (ruminantType is null)
            {
                yield return new ValidationResult("An invalid [r=RuminantType] was specified", new string[] { "Ruminant type" });
            }

            if (this.FindAllChildren<RuminantTypeCohort>().Count() != 1)
            {
                yield return new ValidationResult("A single [r=RuminantTypeCohort] must be present under each [f=SpecifyRuminant] component", new string[] { "Specify ruminant" });
            }
        }
        #endregion

        #region descriptive summary

        private bool cohortFound;

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"\r\n<div class=\"activityentry\"><span class=\"setvalue\">{Proportion.ToString("p0")}</span> of the individuals will be ");
                if (RuminantTypeName == null || RuminantTypeName == "")
                    htmlWriter.Write("<span class=\"errorlink\">TYPE NOT SET</span>");
                else
                    htmlWriter.Write("<span class=\"resourcelink\">" + RuminantTypeName + "</span>");

                cohortFound = FindAllChildren<RuminantTypeCohort>().Count() > 0;
                if (cohortFound)
                    htmlWriter.Write($" with the following details:</div>");
                else
                {
                    htmlWriter.Write($"</div>");
                    htmlWriter.Write($"\r\n<div class=\"activityentry\"><span class=\"errorlink\">No <span class=\"resourcelink\">RuminantCohort</span> describing the individuals was provided!</div>");
                }

                return htmlWriter.ToString();
            }
        }

        #endregion


    }
}
