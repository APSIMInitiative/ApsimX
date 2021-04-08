using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using System;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Models.PMF
{
    /// <summary>
    /// Priority then Relative allocation rules used to determine partitioning
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    [ValidParent(ParentType = typeof(IArbitrator))]
    public class PrioritythenRelativeAllocation : Model, IArbitrationMethod
    {
        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        public void DoAllocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            ////First time round allocate to met priority demands of each organ
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0.0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
                double MetabolicRequirement = Math.Max(0.0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement) > 0.0)
                {
                    double StructuralFraction = BAT.StructuralDemand[i] / (BAT.StructuralDemand[i] + BAT.MetabolicDemand[i]);
                    double StructuralAllocation = Math.Min(StructuralRequirement, NotAllocated * StructuralFraction);
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, NotAllocated * (1 - StructuralFraction));
                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation);
                }
            }
            // Second time round if there is still N to allocate let organs take N up to their Maximum
            double FirstPassNotallocated = NotAllocated;
            for (int i = 0; i < Organs.Length; i++)
            {
                double StorageRequirement = Math.Max(0.0, BAT.StorageDemand[i] - BAT.StorageAllocation[i]); //N needed to take organ up to maximum N concentration, Structural, Metabolic and Luxury N demands
                if (StorageRequirement > 0.0)
                {
                    double StorageAllocation = Math.Min(FirstPassNotallocated * MathUtilities.Divide(BAT.StorageDemand[i], BAT.TotalStorageDemand, 0), StorageRequirement);
                    BAT.StorageAllocation[i] += Math.Max(0, StorageAllocation);
                    NotAllocated -= StorageAllocation;
                    TotalAllocated += StorageAllocation;
                }
            }
        }
        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        protected override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                string PriorityTheRelativeDocStirng = "Arbitration is performed in two passes for each of the biomass supply sources.  On the first pass, structural and metabolic biomass is allocated to each organ based on their order of priority with higher priority organs recieving their full demand first. On the second pass any remaining biomass is allocated to non-structural demands based on the relative demand from all organs.";

                tags.Add(new AutoDocumentation.Paragraph(PriorityTheRelativeDocStirng, indent));
            }
        }
    }
}
