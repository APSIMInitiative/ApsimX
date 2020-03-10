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
    /// Priority then Relative allocation rules used to determine partitioning
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    [ValidParent(ParentType = typeof(IArbitrator))]
    public class PrioritythenRelativeAllocation : Model, IArbitrationMethod, ICustomDocumentation
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

                string PriorityTheRelativeDocStirng = "Arbitration is performed in two passes for each of the biomass supply sources.  On the first pass, structural and metabolic biomass is allocated to each organ based on their order of priority with higher priority organs recieving their full demand first. On the second pass any remaining biomass is allocated to non-structural demands based on the relative demand from all organs.";

                tags.Add(new AutoDocumentation.Paragraph(PriorityTheRelativeDocStirng, indent));
            }
        }
    }
}
