using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Models.PMF
{
    /// <summary>
    /// Single Pass Relative allocation rules used to determine partitioning
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class RelativeAllocationSinglePass : Model, IArbitrationMethod, ICustomDocumentation
    {
        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        public void DoAllocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            ////allocate to all pools based on their relative demands
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
                double MetabolicRequirement = Math.Max(0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                double StorageRequirement = Math.Max(0, BAT.StorageDemand[i] - BAT.StorageAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement + StorageRequirement) > 0.0)
                {
                    double StructuralAllocation = Math.Min(StructuralRequirement, TotalSupply * MathUtilities.Divide(BAT.StructuralDemand[i], BAT.TotalPlantDemand,0));
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, TotalSupply * MathUtilities.Divide(BAT.MetabolicDemand[i], BAT.TotalPlantDemand, 0));
                    double StorageAllocation = Math.Min(StorageRequirement, TotalSupply *  MathUtilities.Divide(BAT.StorageDemand[i], BAT.TotalPlantDemand, 0));

                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    BAT.StorageAllocation[i] += StorageAllocation;
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                }
            }
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

                string SinglePassDocString = "Arbitration is performed in a single pass for each of the biomass supply sources.  Biomass is partitioned between organs based on their relative demand in a single pass so non-structural demands compete dirrectly with structural demands.";

                tags.Add(new AutoDocumentation.Paragraph(SinglePassDocString, indent));
            }
        }
    }
}
