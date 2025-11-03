using APSIM.Numerics;
using Models.CLEM.Interfaces;
using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Ruminant tracking item with protein functionality
    /// </summary>
    public class RuminantTrackingItemProtein: RuminantTrackingItemBodyStore
    {
        private RuminantIntake intake;

        /// <summary>
        /// The proportion of dry relative to wet protein mass
        /// </summary>
        public double ProportionDry { get; private set; } = 0.0;

        /// <summary>
        /// The total mass of wet protein (plus water and ash) as used by Oddy et a Model and live weight calculations
        /// </summary>
        public double AmountWet { get { return Amount / ProportionDry; } }

        /// <summary>
        /// Change in the total mass of wet protein (kg time step-1)
        /// </summary>
        public double ChangeWet { get { return Change / ProportionDry; } }

        /// <summary>
        /// Previous total mass of wet protein (kg time step-1)
        /// </summary>
        public double PreviousWet { get { return Previous / ProportionDry; } }

        // Time step pools

        // todo: this needs to be completed, checked and tested
        /// <inheritdoc/>
        public new double Net { get { return FromIntake - ForUrinary - ForFaecal - ForPregnancy - ForWool - ForLactationFromIntake; } }

        /// <summary>
        /// Protein needed to reach normal protein mass by body size
        /// </summary>
        public double NormalShortfall { get; set; }

        /// <summary>
        /// Used as Endogenous Urinary Protein (kg day-1)
        /// </summary>
        public double ForEndogenousUrinary { get; set; }

        /// <summary>
        /// Used as Urinary Protein (kg day-1). Set in Grow Activities if needed.
        /// </summary>
        public double ForUrinary { get; set; }

        /// <summary>
        /// Used as Endogenous Fecal Protein (kg day-1)
        /// </summary>
        public double ForEndogenousFaecal { get; set; }

        /// <summary>
        /// Used as fecal Protein (kg day-1). Set in Grow Activities if needed.
        /// </summary>
        public double ForFaecal { get; set; }

        /// <summary>
        /// Used as Dermal Protein (kg day-1)
        /// </summary>
        public double ForDermal { get; set; }

        /// <summary>
        /// Used for maintenance (kg day-1)
        /// </summary>
        public double ForMaintenance { get { return ForEndogenousUrinary + ForEndogenousFaecal + ForDermal; } }

        /// <summary>
        /// Used for wool (kg day-1)
        /// </summary>
        public double ForWool { get; set; }

        /// <summary>
        /// Used for pregnancy (fetus + conceptus) (kg day-1)
        /// </summary>
        public double ForPregnancy { get; set; }

        /// <summary>
        /// Net used for lactation (kg day-1)
        /// </summary>
        public double ForLactation { get; set; }

        /// <summary>
        /// Total used for lactation (kg day-1)
        /// </summary>
        public double TotalNeededForLactation { get { return ForLactation + LactationReduction; } }

        /// <summary>
        /// Total used from intake for lactation (kg day-1)
        /// </summary>
        public double ForLactationFromIntake { get { return ForLactation - GetMobilisationProvidedByReason(MobilisationReasonType.LactationProtein); } }

        /// <summary>
        /// Protein freed from reduced lactation when protein deficit (kg day-1)
        /// </summary>
        public double LactationReduction { get; set; }

        /// <summary>
        /// The proportion of the protein remaining after accounting for protein limited milk production
        /// </summary>
        public double ProteinToReducedLactationScalar
        {
            get
            {
                if (MathUtilities.FloatsAreEqual(ForLactation, 0.0))
                    return 0.0;
                return LactationReduction / ForLactation;
            }
        }

        /// <summary>
        /// Provide protein required for maintenance, pregnancy, lactation (kg day-1)
        /// Protein mobilised from body to meet minimum lactation will be accounted for at growth
        /// </summary>
        public double BeforeGrowth { get { return ForMaintenance + ForPregnancy + ForLactation; } }

        /// <summary>
        /// Report protein required for kg gain defined from net energy (kg day-1)
        /// </summary>
        public double ForGain { get; set; }

        /// <summary>
        /// Report protein available after leaving stomach and accounting for other protein use (kg day-1)
        /// </summary>
        public double AvailableForGain { get; set; }

        /// <summary>
        /// Report protein required for kg gain defined from net energy (kg day-1)
        /// </summary>
        public double FromIntake { get { return intake.CrudeProtein; } }

        /// <summary>
        /// Protein mass at mature (kg)
        /// </summary>
        public double MassAtSRW { get; private set; }

        /// <summary>
        /// Set the protein mass expected at Standard Reference Weight
        /// </summary>
        /// <param name="standardReferenceWeight">Standard reference weight of individual accounting for sex and sterility</param>
        /// <param name="parameters">Ruminant general parameters</param>
        public void SetProteinMassAtSRW(double standardReferenceWeight, RuminantParametersGeneral parameters)
        {
            MassAtSRW = standardReferenceWeight * (1.0 / parameters.EBW2LW_CG18) * parameters.ProportionSRWEmptyBodyProtein;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantTrackingItemProtein(Ruminant ind, double initialAmount = 0)
        {
            // ToDo: work out what to do for Oddy and SCA07 that have their own protein content 
            intake = ind.Intake;
            Adjust(initialAmount, ind);
        }

        /// <inheritdoc/>
        public new void Adjust(double change, Ruminant ind)
        {
            CalculateProportionDry(ind);

            Change = change;
            if (Amount + change < 0)
                Change = -Amount;
            Amount += Change;
        }

        /// <summary>
        /// Method to determine the proportion dry of the protein mass and include age of the individual to account for higher water content in younger animals if needed
        /// </summary>
        /// <param name="ind"></param>
        private void CalculateProportionDry(Ruminant ind)
        {
            if (MathUtilities.IsGreaterThanOrEqual(ProportionDry, ind.Parameters.GrowPF_CG.ProteinContentOfFatFreeTissueGainWetBasis))
            {
                return;
            }
            double propAge = Math.Min(1.0, ind.AgeInDays / (double)ind.Parameters.GrowPF_CG.DaysUntilStandardProteinContentOfFatFreeTissueGainWetBasis);
            ProportionDry = (propAge * ind.Parameters.GrowPF_CG.ProteinContentOfFatFreeTissueGainWetBasis) + ((1.0 - propAge) * ind.Parameters.GrowPF_CG.ProteinContentOfFatFreeTissueGainWetBasisAtBirth);
        }

        /// <inheritdoc/>
        public new void Reset()
        {
            Amount = 0;
            Change = 0;
            //Net = 0;
        }

        /// <inheritdoc/>
        public new void TimeStepReset()
        {
            ForEndogenousFaecal = 0;
            ForEndogenousUrinary = 0;
            ForDermal = 0;
            ForPregnancy = 0;
            ForGain = 0;
            AvailableForGain = 0;
            ForWool = 0;
            ForLactation = 0;
            LactationReduction = 0;
            //Net = 0;
            ForUrinary = 0;
            ForFaecal = 0;

            // clear values in mobilisation pools
            foreach (var pool in mobilisationPools)
            {
                pool.Value.Reset();
            }
        }

        /// <inheritdoc/>
        public new void Set(double amount)
        {
            Change = 0;
            Amount = amount;
        }
    }
}
