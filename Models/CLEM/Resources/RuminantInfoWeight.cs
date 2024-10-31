using APSIM.Shared.Documentation.Extensions;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Ruminant weight tracking
    /// </summary>
    public class RuminantInfoWeight
    {
        private double live = 0;

        /// <summary>
        /// Track protein weight (kg)
        /// </summary>
        /// <remarks>
        /// Mass of protein excluding conceptus and fleece
        /// </remarks>
        public RuminantTrackingItemProtein Protein { get; set; } = new();

        /// <summary>
        /// Track protein weight of viscera (Oddy, kg)
        /// </summary>
        /// <remarks>
        /// Mass of visceral protein required for Oddy growth
        /// </remarks>
        public RuminantTrackingItemProtein ProteinV { get; set; }

        /// <summary>
        /// Track fat weight (kg)
        /// </summary>
        /// <remarks>
        /// Mass of fat excluding conceptus
        /// </remarks>
        public RuminantTrackingItem Fat { get; set; } = new();

        /// <summary>
        /// Track base weight (kg)
        /// </summary>
        /// <remarks>
        /// Live weight excluding conceptus and fleece
        /// </remarks>
        public RuminantTrackingItem Base { get; set; } = new();

        /// <summary>
        /// Track conceptus weight (kg)
        /// </summary>
        /// <remarks>
        /// Weight of conceptus and fetus. Set each timestep by calculation including weight.Fetus
        /// </remarks>
        public RuminantTrackingItem Conceptus { get; set; } = new();

        /// <summary>
        /// Track fetus weight (kg)
        /// </summary>
        /// <remarks>
        /// Weight of fetus. Running store of each fetus
        /// </remarks>
        public RuminantTrackingItem Fetus { get; set; } = new();

        /// <summary>
        /// Track conceptus protein weight (kg)
        /// </summary>
        /// <remarks>
        /// Weight of conceptus protein, includes fetus.
        /// </remarks>
        public RuminantTrackingItem ConceptusProtein { get; set; } = new();

        /// <summary>
        /// Track conceptus fat weight (kg)
        /// </summary>
        /// <remarks>
        /// Weight of conceptus fat, includes fetus.
        /// </remarks>
        public RuminantTrackingItem ConceptusFat { get; set; } = new();

        /// <summary>
        /// Track greasy wool weight (kg)
        /// </summary>
        /// <remarks>
        /// Current Weight of greasy wool
        /// </remarks>
        public RuminantTrackingItem Wool { get; set; } = new();

        /// <summary>
        /// Track clean wool weight (kg)
        /// </summary>
        /// <remarks>
        /// Current Weight of clean wool
        /// </remarks>
        public RuminantTrackingItem WoolClean { get; set; } = new();

        /// <summary>
        /// Track wool weight (kg)
        /// </summary>
        public RuminantTrackingItem Cashmere { get; set; } = new();

        /// <summary>
        /// Current weight of individual (kg) includes conceptus and wool
        /// </summary>
        /// <remarks>
        /// Live is calculated on change in base weight that assumes conceptus and wool weight have been updated for the time step
        /// This should be true as these are either done early (birth) on activitiy (shear) or in the growth model before weight gain.
        /// </remarks>
        [FilterByProperty]
        public double Live { get { return live; } }

        /// <summary>
        /// Previous live weight of individual (kg)
        /// </summary>
        /// <remarks>
        /// The live weight in the last timestep inclused conceptus and fleece
        /// </remarks>
        public double Previous { get { return Base.Previous + Conceptus.Previous + Wool.Previous; } }

        /// <summary>
        /// Current live weight gain of individual (kg)
        /// </summary>
        [FilterByProperty]
        public double Gain { get { return Live - Previous; } }

        /// <summary>
        /// Highest weight attained (kg)
        /// </summary>
        public double HighestAttained { get; private set; }

        /// <summary>
        /// Highest base weight attained (kg)
        /// </summary>
        public double HighestBaseAttained { get; private set; }

        /// <summary>
        /// Adult equivalent (live weight) 
        /// </summary>
        [FilterByProperty]
        public double AdultEquivalent { get; private set; }

        /// <summary>
        /// Live Weight at birth (kg)
        /// </summary>
        public double AtBirth { get; private set; }

        /// <summary>
        /// Maximum normalised weight for current age
        /// </summary>
        /// <remarks>
        /// Based on Base weight
        /// </remarks>
        public double MaximumNormalisedForAge { get; private set; }

        /// <summary>
        /// Normalised weight for current age
        /// </summary>
        /// <remarks>
        /// Based on Base weight
        /// </remarks>
        [FilterByProperty]
        public double NormalisedForAge { get; private set; }

        /// <summary>
        /// Set the normal weight for age of individual
        /// </summary>
        /// <param name="normalWeight">Normal weight</param>
        /// <param name="maxNormalWeight">The maximum normalised weight</param>
        public void SetNormalWeightForAge(double normalWeight, double maxNormalWeight)
        {
            if (normalWeight > NormalisedForAge)
                NormalisedForAge = normalWeight;
            MaximumNormalisedForAge = maxNormalWeight;
        }

        /// <summary>
        /// The current live weight as a proportion of highest weight achieved
        /// </summary>
        [FilterByProperty]
        public double ProportionOfHighWeight { get { return HighestAttained == 0 ? 1 : Live / HighestAttained; } }

        /// <summary>
        /// Standard reference weight
        /// </summary>
        /// <remarks>
        /// The mature base weight of an adult female (not pregnant, not lactating) at midpoint of BCS where achieved skeletal maturity.
        /// Technically includes fleece but can be ignored for most purposes and so assume.
        /// </remarks>
        public double StandardReferenceWeight { get; private set; }

        /// <summary>
        /// Set the standard reference weight of individual
        /// </summary>
        /// <param name="srw">SRW to assign</param>
        public void SetStandardReferenceWeight(double srw)
        {
            StandardReferenceWeight = srw;
        }

        /// <summary>
        /// Relative size based on highest base weight achieved (highest base weight / standard reference weight)
        /// </summary>
        [FilterByProperty]
        public double RelativeSizeByHighWeight { get { return HighestBaseAttained / StandardReferenceWeight; } }

        /// <summary>
        /// Relative condition (base weight / normalised weight)
        /// </summary>
        /// <remarks>
        /// Does not include conceptus weight in pregnant females, or wool in sheep.
        /// </remarks>
        [FilterByProperty]
        public double RelativeCondition { get { return Base.Amount / NormalisedForAge; } }

        /// <summary>
        /// The current base weight as a proportion of normalised weight for age
        /// </summary>
        [FilterByProperty]
        public double ProportionOfNormalisedWeight { get { return NormalisedForAge == 0 ? 1 : Base.Amount / NormalisedForAge; } }

        /// <summary>
        /// Relative size (normalised weight / standard reference weight), Z
        /// </summary>
        [FilterByProperty]
        public double RelativeSize { get { return  NormalisedForAge / StandardReferenceWeight; }  }

        /// <summary>
        /// Is still growing (Z less than or equal to 0.9)
        /// </summary>
        [FilterByProperty]
        public bool IsStillGrowing { get { return MathUtilities.IsLessThanOrEqual(RelativeSizeByHighWeight, 0.9); } }

        /// <summary>
        /// The empty body mass of the individual (kg)
        /// </summary>
        /// <remarks>
        /// Empty body mass excludes fleece and conceptus and is adjusted by gut fill when added to Live Weight.
        /// </remarks>
        [FilterByProperty]
        public double EmptyBodyMass { get; private set; }

        /// <summary>
        /// Empty body mass change  (kg)
        /// </summary>
        public double EmptyBodyMassChange { get; private set; }

        /// <summary>
        /// The empty body mass of the individual including fleece weight (kg)
        /// </summary>
        [FilterByProperty]
        public double EmptyBodyMassWithFleece { get { return EmptyBodyMass + Wool.Amount; } }

        /// <summary>
        /// Empty body mass change including fleece weight (kg)
        /// </summary>
        public double EmptyBodyMassChangeWithFleece { get { return EmptyBodyMassChange + Wool.Change; } }

        /// <summary>
        /// Calculate the current fleece weight as a proportion of standard fleece weight
        /// </summary>
        /// <param name="woolParameters">The object holding the individual's wool parameters</param>
        /// <param name="ageInDays">The age of the individual in days</param>
        /// <returns>Current greasy fleece weight as proportion of  </returns>
        public double FleeceWeightAsProportionOfSFW(RuminantParametersGrow24CW woolParameters, int ageInDays)
        {
            double expectedFleeceWeight = woolParameters.StandardFleeceWeight * StandardReferenceWeight * woolParameters.AgeFactorForWool(ageInDays);
            if (expectedFleeceWeight == 0)
                return 0;
            return Wool.Amount / (expectedFleeceWeight);
        }

        /// <summary>
        /// Adjust weight by a given live weight change
        /// </summary>
        /// <param name="wtChange">Change in weight (kg)</param>
        /// <param name="individual">THe individual to change</param>
        public void AdjustByWeightChange(double wtChange, Ruminant individual)
        {
            EmptyBodyMassChange = wtChange / (individual.Parameters.General?.EBW2LW_CG18 ?? 1.09);
            EmptyBodyMass += EmptyBodyMassChange;
            Base.Adjust(wtChange);

            UpdateLiveWeight();
            
            if (individual is RuminantFemale female)
            {
                female.UpdateHighWeightWhenNotPregnant(Live);
            }

            AdultEquivalent = Math.Pow(Live, 0.75) / Math.Pow(individual.Parameters.General.BaseAnimalEquivalent, 0.75);
            HighestAttained = Math.Max(HighestAttained, Live);
            HighestBaseAttained = Math.Max(HighestBaseAttained, Base.Amount);
        }

        /// <summary>
        /// Adjust empty body weight. All other weight will also be adjusted accordingly
        /// </summary>
        /// <param name="ebmChangeFatProtein">Change in empty body weight using fat and protein gain approach (kg)</param>
        /// <param name="individual">THe individual to change</param>
        public void AdjustByEBMChange(double ebmChangeFatProtein, Ruminant individual)
        {
            EmptyBodyMassChange = ebmChangeFatProtein;
            EmptyBodyMass += EmptyBodyMassChange;
            Base.Adjust(EmptyBodyMassChange * (individual.Parameters.General?.EBW2LW_CG18 ?? 1.09));

            UpdateLiveWeight();

            if (individual is RuminantFemale female)
                female.UpdateHighWeightWhenNotPregnant(Live);

            AdultEquivalent = Math.Pow(Live, 0.75) / Math.Pow(individual.Parameters.General.BaseAnimalEquivalent, 0.75);
            HighestAttained = Math.Max(HighestAttained, Live);
            HighestBaseAttained = Math.Max(HighestBaseAttained, Base.Amount);
        }

        /// <summary>
        /// Method to recalculate the live weight based on current base, conceptus and wool weights.
        /// </summary>
        public void UpdateLiveWeight()
        {
            live = Base.Amount + Conceptus.Amount + Wool.Amount;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantInfoWeight(double weightAtBirth)
        {
            AtBirth = weightAtBirth;
        }

        /// <summary>
        /// Calculate and set the initial fat and protein weights of the specified individual
        /// </summary>
        /// <param name="individual">The individual ruminant</param>
        /// <param name="initialFatProtein">Provide initial EBW fat proportion and initial EBW fat proportion (optional)</param>
        /// <param name="assumeInitialPercentage">If true initialFatProtein is provided as a percentage</param>
        public static void SetInitialFatProtein(Ruminant individual, double[] initialFatProtein = null, bool assumeInitialPercentage = true)
        {
            double pFat;
            double pProtein = -1;
            double initialFactor = 1.0;
            if (assumeInitialPercentage)
            {
                initialFactor = 0.01;
            }

            if (initialFatProtein is not null)
            {
                pFat = initialFatProtein[0]*initialFactor;

                if (initialFatProtein.Length >= 2)
                {
                    pProtein = initialFatProtein[1]*initialFactor;
                }
            }
            else
            {
                double RC = individual.Weight.RelativeCondition;
                if (individual.Weight.IsStillGrowing)
                    RC = 0.9;

                double sexFactor = 1.0;
                if (individual.Sex == Sex.Male && (individual as RuminantMale).IsAbleToBreed)
                    sexFactor = 0.85;

                double RCFatSlope = (individual.Parameters.General.ProportionEBWFatMax - individual.Parameters.General.ProportionEBWFat) / 0.5;
                pFat = (individual.Parameters.General.ProportionEBWFat + ((RC-1) * RCFatSlope)) * sexFactor;
            }

            if(!assumeInitialPercentage)
                individual.Weight.Fat.Set(pFat);
            else
                individual.Weight.Fat.Set(pFat * individual.Weight.EmptyBodyMass);

            if (pProtein >= 0)
            {
                if (!assumeInitialPercentage)
                    individual.Weight.Protein.Set(pProtein);
                else
                    individual.Weight.Protein.Set(pProtein * individual.Weight.EmptyBodyMass);
            }
            else
            {
                individual.Weight.Protein.Set(0.22 * (individual.Weight.EmptyBodyMass - individual.Weight.Fat.Amount));
            }

            // set fat and protein energy based on initial amounts
            individual.Energy.Fat.Set(individual.Weight.Fat.Amount * 39.3);
            individual.Energy.Protein.Set(individual.Weight.Protein.Amount * 23.6);
        }
    }
}
