using APSIM.Numerics;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual ruminants for determining death by a current condition
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityDeath))]
    [Description("Manages the death of specified ruminants based on body condition.")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantDeathGroupCondition.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantDeathGroupCondition : RuminantDeathGroup, IRuminantDeathGroup
    {
        /// <summary>
        /// Metric for calculating condition-based mortality
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Condition metric to use")]
        [System.ComponentModel.DefaultValue(ConditionBasedCalculationStyle.RelativeCondition)]
        [Required]
        public ConditionBasedCalculationStyle ConditionMetric { get; set; }

        /// <summary>
        /// Cut-off for condition-based mortality
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Cut-off for condition-based mortality")]
        [Required, GreaterThanValue(0)]
        public double CutOff { get; set; }

        /// <summary>
        /// Probability of dying if less than condition-based mortality cut-off
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Probability of death if below condition-based cut-off")]
        [System.ComponentModel.DefaultValue(1)]
        [Required, GreaterThanValue(0), Proportion]
        public double ProbabilityOfDying { get; set; }

        /// <inheritdoc/>
        public override List<Ruminant> DetermineDeaths(IEnumerable<Ruminant> individuals)
        {
            List<Ruminant> died = new List<Ruminant>();
            switch (ConditionMetric)
            {
                case ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive:
                    died = individuals.Where(a => a.Died == false && MathUtilities.IsLessThanOrEqual(a.Weight.ProportionOfHighWeight, CutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), ProbabilityOfDying)).ToList();
                    break;
                case ConditionBasedCalculationStyle.RelativeCondition:
                    died = individuals.Where(a => a.Died == false && MathUtilities.IsLessThanOrEqual(a.Weight.RelativeCondition, CutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), ProbabilityOfDying)).ToList();
                    break;
                case ConditionBasedCalculationStyle.BodyConditionScore:
                    died = individuals.Where(a => a.Died == false && MathUtilities.IsLessThanOrEqual(a.BodyConditionScore, CutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), ProbabilityOfDying)).ToList();
                    break;
                case ConditionBasedCalculationStyle.EmptyBodyFatProportion:
                    died = individuals.Where(a => a.Died == false && MathUtilities.IsLessThanOrEqual(a.Weight.EBF, CutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), ProbabilityOfDying)).ToList();
                    break;
                default:
                    break;
            }

            foreach (Ruminant ind in died)
            {
                ind.Died = true;
                ind.SaleFlag = HerdChangeReason.DiedUnderweight;
            }
            return died;
        }
    }

}

