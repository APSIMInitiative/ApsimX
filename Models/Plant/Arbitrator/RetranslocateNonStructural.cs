using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using System;
using System.Collections.Generic;

namespace Models.PMF
{
    /// <summary>
    /// Process Retranslocation of BiomassType using Storage First and then Metabolic
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GenericOrgan))]
    public class RetranslocateNonStructural : Model, IRetranslocateMethod, ICustomDocumentation
    {
        /// <summary>Allocate the retranslocated material.</summary>
        /// <param name="organ"></param>
        public double CalculateN(GenericOrgan organ)
        {
            return Math.Max(0, (organ.StartLive.StorageN + organ.StartLive.MetabolicN) * (1 - organ.SenescenceRate.Value()) * organ.NRetranslocationFactor.Value());
        }

        /// <summary>Allocate the retranslocated material.</summary>
        /// <param name="organ"></param>
        public double CalculateBiomass(GenericOrgan organ)
        {
            double availableDM = Math.Max(0.0, organ.StartLive.StorageWt - organ.DMSupply.Reallocation) * organ.DMRetranslocationFactor.Value();
            if (availableDM < 0)
                throw new Exception("Negative DM retranslocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="nitrogen"></param>
        public void AllocateN(GenericOrgan organ, BiomassAllocationType nitrogen)
        {
            // Retranslocation
            if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, organ.StartLive.StorageN + organ.StartLive.MetabolicN - organ.NSupply.Retranslocation))
                throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            double storageRetranslocation = Math.Min(organ.Live.StorageN, nitrogen.Retranslocation);
            organ.Live.StorageN -= storageRetranslocation;
            organ.Allocated.StorageN -= storageRetranslocation;

            double metabolicRetranslocation = nitrogen.Retranslocation - storageRetranslocation;
            organ.Live.MetabolicN -= metabolicRetranslocation;
            organ.Allocated.MetabolicN -= metabolicRetranslocation;

        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="biomass"></param>
        public void AllocateBiomass(GenericOrgan organ, BiomassAllocationType biomass)
        {
            // Get DM lost by respiration (growth respiration)
            // GrowthRespiration with unit CO2 
            // GrowthRespiration is calculated as 
            // Allocated CH2O from photosynthesis "1 / DMConversionEfficiency.Value()", converted 
            // into carbon through (12 / 30), then minus the carbon in the biomass, finally converted into 
            // CO2 (44/12).
            double dmConversionEfficiency = organ.DMConversionEfficiency.Value();
            double carbonConcentration = organ.CarbonConcentration.Value();
            double growthRespFactor = ((1.0 / dmConversionEfficiency) * (12.0 / 30.0) - 1.0 * carbonConcentration) * 44.0 / 12.0;

            organ.GrowthRespiration = 0.0;

            // Allocate structural DM
            organ.Allocated.StructuralWt = Math.Min(biomass.Structural * dmConversionEfficiency, organ.DMDemand.Structural);
            organ.Live.StructuralWt += organ.Allocated.StructuralWt;
            organ.GrowthRespiration += organ.Allocated.StructuralWt * growthRespFactor;

            // Allocate non structural DM
            if ((biomass.Storage * dmConversionEfficiency - organ.DMDemand.Storage) > organ.BiomassToleranceValue)
                throw new Exception("Non structural DM allocation to " + Name + " is in excess of its capacity");

            // Check retranslocation
            if (biomass.Retranslocation - organ.StartLive.StorageWt > organ.BiomassToleranceValue)
                throw new Exception("Retranslocation exceeds non structural biomass in organ: " + Name);

            double diffWt = biomass.Storage - biomass.Retranslocation;
            if (diffWt > 0)
            {
                diffWt *= dmConversionEfficiency;
                organ.GrowthRespiration += diffWt * growthRespFactor;
            }
            organ.Allocated.StorageWt = diffWt;
            organ.Live.StorageWt += diffWt;

            // Allocate metabolic DM
            organ.Allocated.MetabolicWt = biomass.Metabolic * dmConversionEfficiency;
            organ.GrowthRespiration += organ.Allocated.MetabolicWt * growthRespFactor;
            organ.Live.MetabolicWt += organ.Allocated.MetabolicWt;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                string RelativeDocString = "Arbitration is performed in two passes for each of the supply sources.  On the first pass, biomass or nutrient supply is allocated to structural and metabolic pools of each organ based on their demand relative to the demand from all organs.  On the second pass any remaining supply is allocated to non-structural pool based on the organ's relative demand.";

                tags.Add(new AutoDocumentation.Paragraph(RelativeDocString, indent));
            }
        }
    }
}
