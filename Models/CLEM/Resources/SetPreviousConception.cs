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
    public class SetPreviousConception : CLEMModel, IValidatableObject
    {
        [Link]
        private ResourcesHolder resources = null;
        [Link]
        private IClock clock = null;

        /// <summary>
        /// Number of months pregnant
        /// </summary>
        [Description("Number of months pregnant")]
        [GreaterThanValue(0)]
        public int NumberMonthsPregnant { get; set; }

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
            if (NumberMonthsPregnant < female.BreedParams.GestationLength && female.Age - NumberMonthsPregnant >= female.BreedParams.MinimumAge1stMating)
            {
                int offspring = female.CalulateNumberOfOffspringThisPregnancy();
                if (offspring > 0)
                {
                    female.UpdateConceptionDetails(offspring, 1, -1 * NumberMonthsPregnant);
                    // report conception status changed
                    female.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Conceived, female, clock.Today.AddMonths(-1 * NumberMonthsPregnant)));
                }
            }
        }

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            RuminantTypeCohort ruminantCohort = Parent as RuminantTypeCohort;
            var results = new List<ValidationResult>();
            if ((Parent as RuminantTypeCohort).Sex == Sex.Female)
            {
                // get the breed to check gestation
                RuminantType ruminantType = Structure.FindParent<RuminantType>(recurse: true);
                if (ruminantType is null)
                {
                    // find type from a specify ruminant component
                    var specifyRuminant = Structure.FindParent<SpecifyRuminant>(recurse: true);
                    if (specifyRuminant != null)
                        ruminantType = resources.FindResourceType<RuminantHerd, RuminantType>(this as Model, specifyRuminant.RuminantTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

                    if (ruminantType != null)
                    {
                        if (NumberMonthsPregnant < ruminantType.GestationLength)
                        {
                            string[] memberNames = new string[] { "Ruminant cohort details" };
                            results.Add(new ValidationResult($"The number of months pregant [{NumberMonthsPregnant}] for [r=SetPreviousConception] must be less than the gestation length of the breed [{ruminantType.GestationLength}]", memberNames));
                        }
                        // get the individual to check female and suitable age for conception supplied.
                        if (ruminantCohort.Age - NumberMonthsPregnant >= ruminantType.MinimumAge1stMating)
                        {
                            string[] memberNames = new string[] { "Ruminant cohort details" };
                            results.Add(new ValidationResult($"The individual specified must be at least [{ruminantType.MinimumAge1stMating}] month old at the time of conception [r=SetPreviousConception]", memberNames));
                        }
                    }
                    else
                    {
                        string[] memberNames = new string[] { "Ruminant cohort details" };
                        results.Add(new ValidationResult($"Cannot locate a [r=RuminantType] in tree structure above [r=SetPreviousConception]", memberNames));
                    }
                }
            }
            else
            {
                string[] memberNames = new string[] { "ActivityHolder" };
                results.Add(new ValidationResult("Previous conception status can only be calculated for female ruminants", memberNames));
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
                if (FormatForParentControl)
                {
                    // skip if this is inside the table summary of Initial Chohort
                    if (!(CurrentAncestorList.Count >= 3 && CurrentAncestorList[CurrentAncestorList.Count - 3] == typeof(RuminantInitialCohorts).Name))
                    {
                        htmlWriter.Write("\r\n<div class=\"resourcebanneralone\">");
                        htmlWriter.Write($"These individuals will be ");
                        if (NumberMonthsPregnant == 0)
                            htmlWriter.Write($"<span class=\"errorlink\">Not Set</span> ");
                        else
                            htmlWriter.Write($"<span class=\"setvalue\">{NumberMonthsPregnant}</span>");
                        htmlWriter.Write($" months pregnant</div>");
                    }
                }
                else
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">");
                    htmlWriter.Write($"Set last conception age to make these females ");
                    if (NumberMonthsPregnant == 0)
                        htmlWriter.Write($"<span class=\"errorlink\">Not Set</span> ");
                    else
                        htmlWriter.Write($"<span class=\"setvalue\">{NumberMonthsPregnant}</span>");
                    htmlWriter.Write($" months pregnant</div>");
                }
                return htmlWriter.ToString();
            }
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
