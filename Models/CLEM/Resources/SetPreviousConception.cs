using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A component to specify previous concpetion for new females added to the herd
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantTypeCohort))]
    [Description("Specify the conception status of a new female")]
    [HelpUri(@"Content/Features/Resources/SetPreviousConception.htm")]
    [Version(1, 0, 1, "")]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGeneral) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class SetPreviousConception : CLEMModel, IValidatableObject
    {
        [Link]
        private readonly ResourcesHolder resources = null;
        [Link]
        private readonly IClock clock = null;

        /// <summary>
        /// Number of months pregnant
        /// </summary>
        [Description("Number of days pregnant")]
        [GreaterThanValue(0)]
        public int NumberDaysPregnant { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public SetPreviousConception()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Set the conception details of the female provided
        /// </summary>
        /// <param name="female">Female to set details</param>
        public void SetConceptionDetails(RuminantFemale female)
        {
            // if female can breed
            if (NumberDaysPregnant < female.Parameters.General.GestationLength.InDays && female.TimeSince(RuminantTimeSpanTypes.Birth,  clock.Today.AddDays(-NumberDaysPregnant)).TotalDays >= female.Parameters.General.MinimumAge1stMating.InDays)
            {
                int offspring = female.CalulateNumberOfOffspringThisPregnancy();
                if (offspring > 0)
                {
                    female.UpdateConceptionDetails(offspring, 1, -1 * NumberDaysPregnant, clock.Today);
                    // report conception status changed
                    female.BreedDetails.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Conceived, female, clock.Today.AddDays(-1 * NumberDaysPregnant)));
                }
            }
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            RuminantTypeCohort ruminantCohort = Parent as RuminantTypeCohort;
            if ((Parent as RuminantTypeCohort).Sex == Sex.Female)
            {
                // get the breed to check gestation
                RuminantType ruminantType = this.FindAncestor<RuminantType>();
                if (ruminantType is null)
                {
                    // find type from a specify ruminant component
                    var specifyRuminant = this.FindAncestor<SpecifyRuminant>();
                    if (specifyRuminant != null)
                        ruminantType = resources.FindResourceType<RuminantHerd, RuminantType>(this as Model, specifyRuminant.RuminantTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

                    if (ruminantType != null)
                    {
                        if (NumberDaysPregnant < ruminantType.Parameters.General.GestationLength.InDays)
                        {
                            string[] memberNames = new string[] { "Ruminant cohort details" };
                            yield return new ValidationResult($"The number of months pregant [{NumberDaysPregnant}] for [r=SetPreviousConception] must be less than the gestation length of the breed [{ruminantType.Parameters.General.GestationLength.InDays}]", memberNames);
                        }
                        // get the individual to check female and suitable age for conception supplied.
                        if (ruminantCohort.Age - NumberDaysPregnant >= ruminantType.Parameters.General.MinimumAge1stMating.InDays)
                        {
                            string[] memberNames = new string[] { "Ruminant cohort details" };
                            yield return new ValidationResult($"The individual specified must be at least [{ruminantType.Parameters.General}] month old at the time of conception [r=SetPreviousConception]", memberNames);
                        }
                    }
                    else
                    {
                        string[] memberNames = new string[] { "Ruminant cohort details" };
                        yield return new ValidationResult($"Cannot locate a [r=RuminantType] in tree structure above [r=SetPreviousConception]", memberNames);
                    }
                }
            }
            else
            {
                string[] memberNames = new string[] { "ActivityHolder" };
                yield return new ValidationResult("Previous conception status can only be calculated for female ruminants", memberNames);
            }
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            if (FormatForParentControl)
            {
                // skip if this is inside the table summary of Initial Chohort
                if (!(CurrentAncestorList.Count >= 3 && CurrentAncestorList[CurrentAncestorList.Count - 3] == typeof(RuminantInitialCohorts).Name))
                {
                    htmlWriter.Write("\r\n<div class=\"resourcebanneralone\">");
                    htmlWriter.Write($"These individuals will be ");
                    if (NumberDaysPregnant == 0)
                        htmlWriter.Write($"<span class=\"errorlink\">Not Set</span> ");
                    else
                        htmlWriter.Write($"<span class=\"setvalue\">{NumberDaysPregnant}</span>");
                    htmlWriter.Write($" dayss pregnant</div>");
                }
            }
            else
            {
                htmlWriter.Write($"\r\n<div class=\"activityentry\">");
                htmlWriter.Write($"Set last conception age to make these females ");
                if (NumberDaysPregnant == 0)
                    htmlWriter.Write($"<span class=\"errorlink\">Not Set</span> ");
                else
                    htmlWriter.Write($"<span class=\"setvalue\">{NumberDaysPregnant}</span>");
                htmlWriter.Write($" days pregnant</div>");
            }
            return htmlWriter.ToString();
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags()
        {
            return !FormatForParentControl ? base.ModelSummaryClosingTags() : "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags()
        {
            return !FormatForParentControl ? base.ModelSummaryOpeningTags() : "";
        }

        #endregion

    }
}
