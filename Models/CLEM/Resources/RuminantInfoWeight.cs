using APSIM.Numerics;
using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Ruminant weight tracking
    /// </summary>
    public class RuminantInfoWeight
    {
        private double live = 0;
        private readonly Ruminant ruminant;

        /// <summary>
        /// Track dry and wet protein weight (kg)
        /// </summary>
        /// <remarks>
        /// Mass of dry and wet protein excluding conceptus and fleece
        /// </remarks>
        public RuminantTrackingItemProtein Protein { get; set; }

        /// <summary>
        /// Track dry and wet protein weight of viscera (Oddy, kg)
        /// </summary>
        /// <remarks>
        /// Mass of wet visceral protein required for Oddy growth
        /// </remarks>
        public RuminantTrackingItemProtein ProteinViscera { get; set; }

        /// <summary>
        /// Sum total dry protein accounting for any non-visceral and visceral protein pools
        /// </summary>
        public double ProteinTotal { get { return (Protein?.Amount ?? 0) + (ProteinViscera?.Amount ?? 0); } }

        /// <summary>
        /// Sum change in dry protein accounting for any non-visceral and visceral protein pools
        /// </summary>
        public double ProteinChange { get { return (Protein?.Change ?? 0) + (ProteinViscera?.Change ?? 0); } }

        /// <summary>
        /// Sum total wet protein accounting for any non-visceral and visceral protein pools
        /// </summary>
        public double ProteinWetTotal { get { return (Protein?.AmountWet ?? 0) + (ProteinViscera?.AmountWet ?? 0); }  }

        /// <summary>
        /// Sum change in wet protein accounting for any non-visceral and visceral protein pools
        /// </summary>
        public double ProteinWetChange { get { return (Protein?.ChangeWet ?? 0) + (ProteinViscera?.ChangeWet ?? 0); } }

        /// <summary>
        /// Track fat weight (kg)
        /// </summary>
        /// <remarks>
        /// Mass of fat excluding conceptus
        /// </remarks>
        public RuminantTrackingItemBodyStore Fat { get; set; }

        /// <summary>
        /// Total fat energy accounting for missing Fat pool
        /// </summary>
        public double FatTotal { get { return (Fat?.Amount ?? 0); } }

        /// <summary>
        /// Change in fat energy accounting for missing fat pool
        /// </summary>
        public double FatChange { get { return (Fat?.Change ?? 0); } }

        /// <summary>
        /// Proportion of fat in the empty body (EBF)
        /// </summary>
        public double EBF { get { return (Fat?.Amount ?? 0)/ EmptyBodyMass; } }

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
        /// Track cashmere weight (kg). DEPRECIATED FROM IAT
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
        public double Previous { get { return Base.Previous + (Conceptus?.Previous??0) + (Wool?.Previous??0); } }

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
        public void SetStandardReferenceWeight()
        {
            StandardReferenceWeight = ruminant.Parameters.General.SRWFemale;
            if (ruminant.Sex == Sex.Male)
            {
                if (ruminant.IsSterilised)
                {
                    StandardReferenceWeight *= ruminant.Parameters.General.SRWCastrateMaleMultiplier;
                }
                else
                {
                    StandardReferenceWeight *= ruminant.Parameters.General.SRWMaleMultiplier;
                }
            }
            Protein?.SetProteinMassAtSRW(StandardReferenceWeight, ruminant.Parameters.General);
        }

        /// <summary>
        /// A method to define the protein mass at SRW
        /// </summary>
        public void SetProteinMassAtSRW()
        {
            // todo: not sure this is needed anymore as moved to protein and calculated in SetSWR(), but is used one other time in new Ruminant constructor

            // update the protein mass at SRW as this only relies on SRW and specified constants.
            Protein?.SetProteinMassAtSRW(StandardReferenceWeight, ruminant.Parameters.General);
        }

        /// <summary>
        /// Relative size (normalised weight / standard reference weight), Z
        /// </summary>
        [FilterByProperty]
        public double RelativeSize { get { return NormalisedForAge / StandardReferenceWeight; } }

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
        public double EmptyBodyMassWithFleece { get { return EmptyBodyMass + (Wool?.Amount??0); } }

        /// <summary>
        /// Empty body mass change including fleece weight (kg)
        /// </summary>
        public double EmptyBodyMassWithFleeceChange { get { return EmptyBodyMassChange + (Wool?.Change ?? 0);  } }

        /// <summary>
        /// Calculate the current fleece weight as a proportion of standard fleece weight
        /// </summary>
        /// <param name="parameters">Access to the parameter set of the ruminant</param>
        /// <param name="ageInDays">The age of the individual in days</param>
        /// <returns>Current greasy fleece weight as proportion of  </returns>
        public double FleeceWeightAsProportionOfSFW(RuminantParameters parameters, int ageInDays)
        {
            if (ruminant.Parameters.General.IncludeWool == false)
                return 0;   
            double expectedFleeceWeight = FleeceWeightExpectedByAge();
            if (expectedFleeceWeight == 0)
                return 0;
            return Wool.Amount / (expectedFleeceWeight);
        }

        /// <summary>
        /// Calculate the expected fleece weight based on age
        /// </summary>
        /// <returns>Current greasy fleece weight as proportion of  </returns>
        public double FleeceWeightExpectedByAge()
        {
            return ruminant.Parameters.GrowPF_CW.StandardFleeceWeight * StandardReferenceWeight * ruminant.Parameters.GrowPF_CW.AgeFactorForWool(ruminant.AgeInDays);
        }

        /// <summary>
        /// Method to recalculate the live weight based on current base, conceptus and wool weights.
        /// </summary>
        public void UpdateLiveWeight()
        {
            live = Base.Amount + Conceptus.Amount + Wool.Amount;
        }

        /// <summary>
        /// Adjust weight
        /// </summary>
        /// <param name="wtChange">Optional change in empty body mass (kg)</param>
        public void Adjust(double wtChange = 0)
        {
            EmptyBodyMassChange = 0;

            if (Protein is not null && wtChange == 0)
            {
                // assume change is based on fat and protein mass changes previously calculated for time step
                wtChange = ProteinWetChange + FatChange;
            }

            if (Base.Amount > 0) // not new individual
            {
                EmptyBodyMassChange = wtChange;
            }
            EmptyBodyMass += wtChange;

            // account for gut fill and adjust base weight
            Base.Adjust(wtChange * (ruminant.Parameters.General?.EBW2LW_CG18 ?? 1.09));

            UpdateLiveWeight();

            if (ruminant is RuminantFemale female)
            {
                female.UpdateHighWeightWhenNotPregnant(Live);
            }

            AdultEquivalent = Math.Pow(Live, 0.75) / Math.Pow(ruminant.Parameters.General.BaseAnimalEquivalent, 0.75);
            HighestAttained = Math.Max(HighestAttained, Live);
            HighestBaseAttained = Math.Max(HighestBaseAttained, Base.Amount);
        }

        /// <summary>
        /// Set fleece and conceptus free body weight at startup
        /// </summary>
        /// <param name="weight">Initial base weight (kg)</param>
        public void SetInitialBaseWeight(double weight)
        {
            Adjust(weight / (ruminant.Parameters.General?.EBW2LW_CG18 ?? 1.09));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantInfoWeight(Ruminant ruminant, double weightAtBirth)
        {
            this.ruminant = ruminant;
            AtBirth = weightAtBirth;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantInfoWeight(Ruminant ruminant)
        {
            this.ruminant = ruminant;
        }

        /// <summary>
        /// Reset all BodyStore Tracking items for time step
        /// </summary>
        public void TimeStepReset()
        {
            Protein?.TimeStepReset();
            ProteinViscera?.TimeStepReset();
            Fat?.TimeStepReset();
        }

    }
}
