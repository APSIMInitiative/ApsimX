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
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
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
        [GreaterThanEqualValue(0)]
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
            // estimated age at maturity is determined from the min size at maturity and the age for this normalised weight.
            if (NumberDaysPregnant < female.Parameters.General.GestationLength.InDays && female.TimeSince(RuminantTimeSpanTypes.Birth,  clock.Today.AddDays(-NumberDaysPregnant)).TotalDays >= female.Parameters.Details.EstimatedAgeAtMaturityFemale)
            {
                int offspring = female.CalulateNumberOfOffspringThisPregnancy();
                if (offspring > 0)
                {
                    female.UpdateConceptionDetails(offspring, 1, -1 * NumberDaysPregnant, clock.Today);
                    // report conception status changed
                    female.Parameters.Details.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Conceived, female, clock.Today.AddDays(-1 * NumberDaysPregnant)));
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
                RuminantType ruminantType = Structure.FindParent<RuminantType>(recurse: true);
                if (ruminantType is null)
                {
                    // find type from a specify ruminant component
                    var specifyRuminant = Structure.FindParent<SpecifyRuminant>(recurse: true);
                    if (specifyRuminant != null)
                        ruminantType = resources.FindResourceType<RuminantHerd, RuminantType>(this, specifyRuminant.RuminantTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

                    if (ruminantType != null)
                    {
                        if (NumberDaysPregnant < ruminantType.Parameters.General.GestationLength.InDays)
                        {
                            string[] memberNames = new string[] { "Ruminant cohort details" };
                            yield return new ValidationResult($"The number of days pregant [{NumberDaysPregnant}] for [r=SetPreviousConception] must be less than the gestation length of the breed [{ruminantType.Parameters.General.GestationLength.InDays}]", memberNames);
                        }
                        // get the individual to check female and suitable age for conception supplied.
                        if (ruminantCohort.Age - NumberDaysPregnant >= ruminantType.Parameters.Details.EstimatedAgeAtMaturityFemale)
                        {
                            string[] memberNames = new string[] { "Ruminant cohort details" };
                            yield return new ValidationResult($"The individual specified must be at least [{ruminantType.EstimatedAgeAtMaturityFemale}] days old at the time of conception [r=SetPreviousConception] based on estimated age at minimum size for maturity", memberNames);
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
    }
}
