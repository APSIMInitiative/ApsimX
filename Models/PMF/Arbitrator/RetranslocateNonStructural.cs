using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using Models.PMF.Organs;

namespace Models.PMF
{
    /// <summary>
    /// Process Retranslocation of BiomassType using Storage First and then Metabolic.
    /// 
    /// Arbitration is performed in two passes for each of the supply sources. On the
    /// first pass, biomass or nutrient supply is allocated to structural and metabolic
    /// pools of each organ based on their demand relative to the demand from all
    /// organs.  On the second pass any remaining supply is allocated to non-structural
    /// pool based on the organ's relative demand.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class RetranslocateNonStructural : Model, IRetranslocateMethod
    {
        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        public double Calculate(IOrgan organ)
        {
            GenericOrgan genOrgan = organ as GenericOrgan; // FIXME!
            return Math.Max(0, (genOrgan.StartLive.StorageN + genOrgan.StartLive.MetabolicN) * (1 - genOrgan.SenescenceRate.Value()) * genOrgan.NRetranslocationFactor.Value());
        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        public double CalculateBiomass(IOrgan organ)
        {
            GenericOrgan genOrgan = organ as GenericOrgan; // FIXME!
            double availableDM = Math.Max(0.0, genOrgan.StartLive.StorageWt - genOrgan.DMSupply.ReAllocation) * genOrgan.DMRetranslocationFactor.Value();
            if (MathUtilities.IsNegative(availableDM))
                throw new Exception("Negative DM retranslocation value computed in function " + Name + " in organ " + organ.Name);

            return availableDM;
        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="nitrogen"></param>
        public void Allocate(IOrgan organ, BiomassAllocationType nitrogen)
        {
            var genOrgan = organ as GenericOrgan;

            // Retranslocation
            if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, genOrgan.StartLive.StorageN + genOrgan.StartLive.MetabolicN - genOrgan.NSupply.ReAllocation))
                throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in function " + Name + " in organ " + organ.Name);

            double storageRetranslocation = Math.Min(genOrgan.Live.StorageN, nitrogen.Retranslocation);
            genOrgan.Live.StorageN -= storageRetranslocation;
            genOrgan.Allocated.StorageN -= storageRetranslocation;

            double metabolicRetranslocation = nitrogen.Retranslocation - storageRetranslocation;
            genOrgan.Live.MetabolicN -= metabolicRetranslocation;
            genOrgan.Allocated.MetabolicN -= metabolicRetranslocation;

        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="biomass"></param>
        public void AllocateBiomass(IOrgan organ, BiomassAllocationType biomass)
        {
            GenericOrgan genOrgan = organ as GenericOrgan;

            // get DM lost by respiration (growth respiration)
            // GrowthRespiration with unit CO2 
            // GrowthRespiration is calculated as 
            // Allocated CH2O from photosynthesis "1 / DMConversionEfficiency.Value()", converted 
            // into carbon through (12 / 30), then minus the carbon in the biomass, finally converted into 
            // CO2 (44/12).
            // FIXME - this is also calculated in GenericOrgan. Seems redundant to calculate this twice.
            double growthRespFactor = ((1.0 / genOrgan.DMConversionEfficiency.Value()) * (12.0 / 30.0) - 1.0 * genOrgan.CarbonConcentration.Value()) * 44.0 / 12.0;

            // Check retranslocation
            if (MathUtilities.IsGreaterThan(biomass.Retranslocation, genOrgan.StartLive.StorageWt))
                throw new Exception("Retranslocation exceeds non structural biomass in function " + Name + " in organ " + organ.Name);

            double diffWt = biomass.Storage - biomass.Retranslocation;
            if (diffWt > 0)
            {
                diffWt *= genOrgan.DMConversionEfficiency.Value();
                genOrgan.GrowthRespiration += diffWt * growthRespFactor;
            }
            genOrgan.Allocated.StorageWt = diffWt;
            genOrgan.Live.StorageWt += diffWt;
            // allocate metabolic DM
            genOrgan.Allocated.MetabolicWt = biomass.Metabolic * genOrgan.DMConversionEfficiency.Value();
            genOrgan.GrowthRespiration += genOrgan.Allocated.MetabolicWt * growthRespFactor;
            genOrgan.Live.MetabolicWt += genOrgan.Allocated.MetabolicWt;
        }
    }
}
