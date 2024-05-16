using APSIM.Shared.Utilities;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
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

namespace Models.CLEM.Groupings
{
    /// <summary>
    /// Manages the death of ruminants each time step based using the original IAT/NABSA approach
    /// The base mortality rate is modified by adult body condition or the mother's body condition for sucklings
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityDeath))]
    [Description("Manages the death of specified ruminants based on their condition.")]
    [HelpUri(@"Content/Features/Filters/Groups/Ruminant/RuminantDeathGroup.htm")]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrow), typeof(RuminantParametersGrowMortality) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType })]
    public class RuminantDeathGroup : CLEMRuminantActivityBase, IRuminantDeathGroup, IHandlesActivityCompanionModels, IValidatableObject
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantDeathGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <inheritdoc/>
        public void DetermineDeaths(IEnumerable<Ruminant> individuals)
        {
            // order descending to ensure mothers' deaths are determined before juveniles.
            foreach (var ind in GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.AllOnFarm))
            {
                double mortalityRate;
                if (!ind.Weaned)
                {
                    // ToDo: see if we can remove the breed parameter from grow below
                    if (ind.Mother == null || MathUtilities.IsLessThan(ind.Mother.Weight.Live, (ind.Parameters?.Breeding?.CriticalCowWeight ?? 0) * ind.Weight.StandardReferenceWeight))
                        // if no mother assigned or mother's weight is < CriticalCowWeight * SFR
                        mortalityRate = ind.Parameters.GrowMortality.JuvenileMortalityMaximum;
                    else
                        // if mother's weight >= criticalCowWeight * SFR
                        mortalityRate = Math.Exp(-Math.Pow(ind.Parameters.GrowMortality.JuvenileMortalityCoefficient * (ind.Mother.Weight.Live / ind.Mother.Weight.NormalisedForAge), ind.Parameters.GrowMortality.JuvenileMortalityExponent));

                    mortalityRate += ind.Parameters.Grow.MortalityBase;
                    mortalityRate = Math.Min(mortalityRate, ind.Parameters.GrowMortality.JuvenileMortalityMaximum);
                }
                else
                    mortalityRate = 1 - (1 - ind.Parameters.Grow.MortalityBase) * (1 - Math.Exp(Math.Pow(-(ind.Parameters.GrowMortality.MortalityCoefficient * (ind.Weight.Live / ind.Weight.NormalisedForAge - ind.Parameters.GrowMortality.MortalityIntercept)), ind.Parameters.GrowMortality.MortalityExponent)));

                // convert mortality from annual (calculated) to time-step (applied).
                mortalityRate /= (DateTime.IsLeapYear(events.Clock.Today.Year) ? 366 : 365) / events.Interval;

                if (MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), mortalityRate))
                {
                    ind.Died = true;
                    ind.SaleFlag = HerdChangeReason.DiedMortality;
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
            var results = new List<ValidationResult>();

            if (FindInScope<RuminantParametersGrow>() is null)
            {
                string[] memberNames = new string[] { "Missing Ruminant.Grow parameters" };
                results.Add(new ValidationResult($"[a=RuminantActivityDeathOriginal] requires parameters defined in [r=Ruminant.Parameters.RuminantParametersGrow].{Environment.NewLine}Ensure [r=Ruminant.Parameters.RuminantParametersGrow] is present and has the parameters for your breed provided..", memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write($"\r\n<div class=\"activityentry\">Any death of specified individuals is determined using the breed base mortality modified by adult mody condition and the condition of mothers for suckling individuals.</div>");
            return htmlWriter.ToString();
        }

        #endregion
    }

}

