using APSIM.Shared.Utilities;
using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Store of female ruminant lactation for the time-step
    /// </summary>
    [Serializable]
    public class RuminantInfoLactation
    {
        /// <summary>
        /// Potential rate (MJ/day)
        /// </summary>
        public double PotentialRate { get; set; }
        /// <summary>
        /// Production rate (MJ/day)
        /// </summary>
        public double ProductionRate { get; set; }
        /// <summary>
        /// Production rate from the previous time-step (MJ/day)
        /// </summary>
        public double ProductionRatePrevious { get; set; }
        /// <summary>
        /// Maximum production rate (MJ/day)
        /// </summary>
        public double MaximumRate { get; set; }
        ///// <summary>
        ///// Potential milk production (MJ/day)
        ///// </summary>
        //public double PotentialRate { get; set; }
        ///// <summary>
        ///// Potential milk production MP2 (MJ/day)
        ///// </summary>
        //public double PotentialRate2 { get; set; }
        /// <summary>
        /// Amount produced (kg)
        /// </summary>
        [FilterByProperty]
        public double Produced { get; set; }
        /// <summary>
        /// Amount currently available (kg)
        /// </summary>
        [FilterByProperty]
        public double Available { get; set; }
        /// <summary>
        /// Amount milked (kg)
        /// </summary>
        [FilterByProperty]
        public double Milked { get; set; }
        /// <summary>
        /// Amount suckled (kg)
        /// </summary>
        [FilterByProperty]
        public double Suckled { get; set; }
        /// <summary>
        /// Protein required for lactation (kg)
        /// </summary>
        public double Protein { get; set; }
        /// <summary>
        /// Protein saved by lactation reduction (kg)
        /// </summary>
        public double ProteinReduced { get; set; }
        /// <summary>
        /// The proportion of the protein remaining after accounting for protein limited milk production
        /// </summary>
        public double ProteinToReducedProteinScalar 
        {
            get
            {
                if (MathUtilities.FloatsAreEqual(Protein, 0.0))
                    return 0.0;
                return ProteinReduced / Protein; 
            }
        }
        /// <summary>
        /// The percent protein of the milk produced (kg/kg * 100)
        /// </summary>
        public double ProteinPercent { get; set; }
        /// <summary>
        /// The energy content of the milk produced (MJ)
        /// </summary>
        public double EnergyContent { get; set; }
        /// <summary>
        /// Lag term for milk production
        /// </summary>
        public double Lag { get; set; }
        /// <summary>
        /// Tracks the nutrition after peak lactation for milk production.
        /// </summary>
        public double NutritionAfterPeakLactationFactor { get; set; }
        /// <summary>
        /// Determines if milking has been performed on individual to increase milk production
        /// </summary>
        public bool MilkingPerformed { get; set; } = false;

        ///// <summary>
        ///// The proportion of the potential milk production achieved in timestep
        ///// </summary>
        //public double ProportionMilkProductionAchieved 
        //{ 
        //    get 
        //    { 
        //        if (PotentialRate > 0)
        //            return ProductionRate / PotentialRate;
        //        return 0;
        //    }
        //}

        /// <summary>
        /// Method to remove milk from female
        /// </summary>
        /// <param name="amount">Amount to take</param>
        /// <param name="reason">Reason for taking milk</param>
        public void Take(double amount, MilkUseReason reason)
        {
            amount = Math.Min(amount, Available);
            Available -= amount;
            switch (reason)
            {
                case MilkUseReason.Suckling:
                    Suckled += amount;
                    break;
                case MilkUseReason.Milked:
                    Milked += amount;
                    break;
                default:
                    throw new ApplicationException($"Unknown MilkUseReason [{reason}] in TakeMilk method of [r=RuminantFemale]");
            }
        }

        /// <summary>
        /// Reset milk stores
        /// </summary>
        public void Reset()
        {
            PotentialRate = 0;
            ProductionRate = 0;
            ProductionRatePrevious = 0;
            Produced = 0;
            Available = 0;
            Milked = 0;
            Suckled = 0;
            ProteinReduced = 0;
            Protein = 0;
        }
    }
}
