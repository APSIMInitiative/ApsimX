using APSIM.Numerics;
using DocumentFormat.OpenXml.Drawing.Charts;
using Models.CLEM.Interfaces;
using NetTopologySuite.GeometriesGraph;
using System;
using System.Security.Cryptography;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Ruminant tracking item with protein functionality
    /// </summary>
    public class RuminantTrackingItemProtein: IRuminantTrackingItem
    {
        /// <summary>
        /// The proportion of dry relative to wet protein mass
        /// </summary>
        public double ProportionDry { get; private set; } = 0.0;

        /// <inheritdoc/>
        public double Amount { get; private set; }

        /// <summary>
        /// The total mass of wet protein (plus water and ash) as used by Oddy et a Model
        /// </summary>
        public double AmountWet { get { return Amount / ProportionDry; } }

        /// <inheritdoc/>
        public double Change { get; private set; }

        /// <summary>
        /// Change in the total mass of wet protein (kg timestep-1)
        /// </summary>
        public double ChangeWet { get { return Change / ProportionDry; } }

        /// <inheritdoc/>
        public double Previous { get { return Amount - Change; } }

        /// <summary>
        /// Previous total mass of wet protein (kg timestep-1)
        /// </summary>
        public double PreviousWet { get { return Previous / ProportionDry; } }

        /// <inheritdoc/>
        public double Net { get; set; }

        /// <summary>
        /// Report protein for maintenance (kg day-1)
        /// </summary>
        public double ForMaintenance { get; set; }

        /// <summary>
        /// Report protein for wool (kg day-1)
        /// </summary>
        public double ForWool { get; set; }

        /// <summary>
        /// Report protein for pregnancy (kg day-1)
        /// </summary>
        public double ForPregnancy { get; set; }

        /// <summary>
        /// Report protein for lactation (kg day-1)
        /// </summary>
        public double ForLactation { get; set; }

        /// <summary>
        /// Report protein reduction from lactation (kg day-1)
        /// </summary>
        public double FromLactationReduction { get; set; }

        /// <summary>
        /// Report protein taken from intake to meet energy deficit (kg day-1)
        /// </summary>
        public double FromIntakeForEnergy { get; set; }

        /// <summary>
        /// The proportion of the protein remaining after accounting for protein limited milk production
        /// </summary>
        public double ProteinToReducedLactationScalar
        {
            get
            {
                if (MathUtilities.FloatsAreEqual(ForLactation, 0.0))
                    return 0.0;
                return FromLactationReduction / ForLactation;
            }
        }

        /// <summary>
        /// Provide protein required for maintenance, pregnancy, lactation, and any protein remobilisation (kg day-1)
        /// </summary>
        public double BeforeGrowth { get { return ForMaintenance + ForPregnancy + ForLactation + FromBodyForMobilisation; } }

        /// <summary>
        /// Report protein required for kg gain defined from net energy (kg day-1)
        /// </summary>
        public double ForGain { get; set; }

        /// <summary>
        /// Report protein available after leaving stomach and accounting for other protein use (kg day-1)
        /// </summary>
        public double AvailableForGain { get; set; }

        /// <summary>
        /// Report protein mobilised from body for lactation needs (kg day-1)
        /// </summary>
        public double FromBodyForLactation { get; set; }
        /// <summary>
        /// Report protein lost in conversion ses during from body for lactation needs (kg day-1)
        /// </summary>
        public double FromBodyForMobilisation { get; set; }

        /// <summary>
        /// Protein mass at mature (kg)
        /// </summary>
        public double MassAtSRW { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantTrackingItemProtein(Ruminant ind, double initialAmount = 0)
        {
            // ToDo: work out what to do for Oddy and SCA07 that have their own protein content 

            Adjust(initialAmount, ind);
            //CalculateProportionDry(ind);
            //Amount = initialAmount;
        }

        /// <inheritdoc/>
        public void Adjust(double change, Ruminant ind)
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
        public void Reset()
        {
            Amount = 0;
            Change = 0;
            Net = 0;
        }

        /// <summary>
        /// Performs the resetting of time step tracking stores
        /// </summary>
        public void TimeStepReset()
        {
            ForMaintenance = 0;
            ForPregnancy = 0;
            ForGain = 0;
            AvailableForGain = 0;
            FromBodyForMobilisation = 0;
            ForWool = 0;
            ForLactation = 0;
            FromLactationReduction = 0;
            Net = 0;
            FromBodyForLactation = 0;
            FromBodyForMobilisation = 0;
            FromIntakeForEnergy = 0;
        }

        /// <inheritdoc/>
        public void Set(double amount)
        {
            Change = 0;
            Amount = amount;
        }
    }
}
