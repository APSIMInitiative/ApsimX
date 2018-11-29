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
    /// Relative allocation rules used to determine partitioning
    /// </summary>
    [Serializable]
    public class SorghumArbitratorN : Model, IArbitrationMethod, ICustomDocumentation
    {
        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        public void DoAllocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            //allocate structural first - will be a different order to biomass so need to hard code the order until an interface is created
            //roots
            //stem
            //rachis
            //leaf

            //then allocate metabolic relative to demand
            //var grainIndex = 0;
            var rootIndex = 1;
            var leafIndex = 2;
            var rachisIndex = 3;
            var stemIndex = 4;

            //var demand = BAT.TotalStructuralDemand + BAT.TotalMetabolicDemand;// - BAT.StructuralDemand[grainIndex];
            var demand = BAT.TotalPlantDemand;// - BAT.StructuralDemand[grainIndex];
            var supplyDemand = 0.0;
            if (demand > 0.0)
                supplyDemand = Math.Min( (BAT.TotalUptakeSupply * 10.0) / demand, 1.0);


            double rootAllocation = supplyDemand * BAT.StructuralDemand[rootIndex];
            BAT.StructuralAllocation[rootIndex] += rootAllocation;
            NotAllocated -= (rootAllocation);
            TotalAllocated += (rootAllocation);

            AllocateStructural(stemIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateStructural(rachisIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateStructural(leafIndex, ref TotalAllocated, ref NotAllocated, BAT);

            AllocateMetabolic(leafIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateMetabolic(rachisIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateMetabolic(stemIndex, ref TotalAllocated, ref NotAllocated, BAT);

            AllocateStorage(leafIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateStorage(rachisIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateStorage(stemIndex, ref TotalAllocated, ref NotAllocated, BAT);

        }
        private void AllocateStructural(int i, ref double TotalAllocated, ref double NotAllocated, BiomassArbitrationType BAT)
        {
            double StructuralRequirement = Math.Max(0.0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
            if ((StructuralRequirement) > 0.0)
            {
                double StructuralAllocation = Math.Min(StructuralRequirement, NotAllocated);
                BAT.StructuralAllocation[i] += StructuralAllocation;
                NotAllocated -= (StructuralAllocation);
                TotalAllocated += (StructuralAllocation);
            }
        }
        private void AllocateMetabolic(int i, ref double TotalAllocated, ref double NotAllocated, BiomassArbitrationType BAT)
        {
            double MetabolicRequirement = Math.Max(0.0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
            if ((MetabolicRequirement) > 0.0)
            {
                double MetabolicAllocation = Math.Min(MetabolicRequirement, NotAllocated * MathUtilities.Divide(BAT.MetabolicDemand[i], BAT.TotalMetabolicDemand, 0));
                BAT.MetabolicAllocation[i] += MetabolicAllocation;
                NotAllocated -= (MetabolicAllocation);
                TotalAllocated += (MetabolicAllocation);
            }
        }
        private void AllocateStorage(int i, ref double TotalAllocated, ref double NotAllocated, BiomassArbitrationType BAT)
        {
            double StorageRequirement = Math.Max(0.0, BAT.StorageDemand[i] - BAT.StorageAllocation[i]); //N needed to take organ up to maximum N concentration, Structural, Metabolic and Luxury N demands
            if (StorageRequirement > 0.0)
            {
                double StorageAllocation = Math.Min(NotAllocated * MathUtilities.Divide(BAT.StorageDemand[i], BAT.TotalStorageDemand, 0), StorageRequirement);
                BAT.StorageAllocation[i] += Math.Max(0, StorageAllocation);
                NotAllocated -= StorageAllocation;
                TotalAllocated += StorageAllocation;
            }

        }


        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        public void DoRetranslocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            //var rootIndex = 1;
            var leafIndex = 2;
            var rachisIndex = 3;
            var stemIndex = 4;
            var grainIndex = 0;

            AllocateStructuralFrom(Organs, stemIndex, leafIndex, ref TotalAllocated, BAT);
            AllocateStructuralFrom(Organs, rachisIndex, leafIndex, ref TotalAllocated, BAT);
            AllocateStructuralFrom(Organs, leafIndex, stemIndex, ref TotalAllocated, BAT);
            AllocateStructuralFrom(Organs, leafIndex, leafIndex, ref TotalAllocated, BAT);

            AllocateStructuralFrom(Organs, grainIndex, rachisIndex, ref TotalAllocated, BAT);
            AllocateStructuralFrom(Organs, grainIndex, stemIndex, ref TotalAllocated, BAT);
            AllocateStructuralFrom(Organs, grainIndex, leafIndex, ref TotalAllocated, BAT);
        }

        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="iSink">The organs.</param>
        /// <param name="iSupply">The organs.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        public void AllocateStructuralFrom(IArbitration[] Organs, int iSink, int iSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double StructuralRequirement = Math.Max(0.0, BAT.StructuralDemand[iSink] - BAT.StructuralAllocation[iSink]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
            if ((StructuralRequirement) > 0.0)
            {
                double StructuralAllocation = Math.Min(StructuralRequirement, BAT.RetranslocationSupply[iSupply]);
                BAT.StructuralAllocation[iSink] += StructuralAllocation;
                TotalAllocated += (StructuralAllocation);
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

                string RelativeDocString = "Arbitration is performed in two passes for each of the supply sources.  On the first pass, biomass or nutrient supply is allocated to structural and metabolic pools of each organ based on their demand relative to the demand from all organs.  On the second pass any remaining supply is allocated to non-structural pool based on the organ's relative demand.";

                tags.Add(new AutoDocumentation.Paragraph(RelativeDocString, indent));
            }
        }
    }
}
