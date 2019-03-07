using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
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
            double NotAllocated = TotalSupply * 0.1;
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
            var tmp = (Organs[2] as SorghumLeaf).Plant.Phenology.DaysAfterSowing;
            var clock = (Organs[2] as SorghumLeaf).Plant.Clock;

            //var demand = BAT.TotalStructuralDemand + BAT.TotalMetabolicDemand;// - BAT.StructuralDemand[grainIndex];
            var demand = BAT.TotalPlantDemand - BAT.StructuralDemand[leafIndex]; // calcNDemand in old sorghum did not include new leaf
            if (demand > 0.0)
                BAT.SupplyDemandRatioN = Math.Min( (BAT.TotalUptakeSupply * 0.1) / demand, 1.0);

            double rootAllocation = BAT.SupplyDemandRatioN * BAT.StructuralDemand[rootIndex];
            BAT.StructuralAllocation[rootIndex] += rootAllocation;
            NotAllocated -= (rootAllocation);
            TotalAllocated += (rootAllocation);

            AllocateStructural(stemIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateStructural(rachisIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateStructural(leafIndex, ref TotalAllocated, ref NotAllocated, BAT);

            //old sorghum didn't serparate structural and metabolic demands for stem and rachis - metabolic included the structural amount
            var nTotalDemand = BAT.StructuralDemand[stemIndex] + BAT.StructuralDemand[rachisIndex] +
                BAT.MetabolicDemand[leafIndex] + BAT.MetabolicDemand[stemIndex] + BAT.MetabolicDemand[rachisIndex];

            AllocateMetabolic(leafIndex, ref TotalAllocated, ref NotAllocated, BAT, BAT.MetabolicDemand[leafIndex], nTotalDemand);
            AllocateMetabolic(rachisIndex, ref TotalAllocated, ref NotAllocated, BAT, BAT.StructuralDemand[rachisIndex] + BAT.MetabolicDemand[rachisIndex], nTotalDemand);
            AllocateMetabolic(stemIndex, ref TotalAllocated, ref NotAllocated, BAT, BAT.StructuralDemand[stemIndex] + BAT.MetabolicDemand[stemIndex], nTotalDemand);

            //AllocateStorage(leafIndex, ref TotalAllocated, ref NotAllocated, BAT);
            //AllocateStorage(rachisIndex, ref TotalAllocated, ref NotAllocated, BAT);
            //AllocateStorage(stemIndex, ref TotalAllocated, ref NotAllocated, BAT);

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
        private void AllocateMetabolic(int i, ref double TotalAllocated, ref double NotAllocated, BiomassArbitrationType BAT, double organDemand, double nTotalDemand)
        {
            double MetabolicRequirement = Math.Max(0.0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
            if (organDemand > 0.0)
            {
                //double MetabolicAllocation = Math.Max(0.0, NotAllocated * MathUtilities.Divide(BAT.MetabolicDemand[i], nTotalDemand, 0));
                double Allocation = Math.Max(0.0, NotAllocated * MathUtilities.Divide(organDemand, nTotalDemand, 0));
                double MetabolicAllocation = Math.Min(MetabolicRequirement, Allocation);

                //to stop it from givig it too much metabolic - push the flowover from metabolic into storage
                BAT.MetabolicAllocation[i] += MetabolicAllocation; 
                NotAllocated -= (MetabolicAllocation);
                TotalAllocated += (MetabolicAllocation);

                //do storage if there is any leftover
                double storageAllocation = Allocation - MetabolicAllocation;
                BAT.StorageAllocation[i] += storageAllocation;
                NotAllocated -= (storageAllocation);
                TotalAllocated += (storageAllocation);

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
        /// <param name="BAT">The bat.</param>
        public void DoRetranslocation(IArbitration[] Organs, BiomassArbitrationType BAT)
        {
            double NotAllocated = BAT.TotalRetranslocationSupply;
            //var rootIndex = 1;
            var leafIndex = 2;
            var rachisIndex = 3;
            var stemIndex = 4;
            var grainIndex = 0;

            var tmp = (Organs[2] as SorghumLeaf).Plant.Phenology.DaysAfterSowing;
            var clock = (Organs[2] as SorghumLeaf).Plant.Clock;

            var stemDemand = BAT.StructuralDemand[stemIndex];
            var rachisDemand = BAT.StructuralDemand[rachisIndex];
            var leafDemand = BAT.StructuralDemand[leafIndex];

            var forStem = AllocateStructuralFromLeaf(Organs[leafIndex] as SorghumLeaf, leafIndex, stemIndex, BAT);
            var forRachis = AllocateStructuralFromLeaf(Organs[leafIndex] as SorghumLeaf, leafIndex, rachisIndex, BAT);
            var forLeaffromStem = AllocateStructuralFromOrgan(stemIndex, leafIndex, BAT);
            var forLeaf = AllocateStructuralFromLeaf(Organs[leafIndex] as SorghumLeaf, leafIndex, leafIndex, BAT);

            AllocateStructuralFromOrgan(rachisIndex, grainIndex, BAT);
            AllocateStructuralFromOrgan(stemIndex, grainIndex, BAT);
            AllocateStructuralFromLeaf(Organs[leafIndex] as SorghumLeaf, leafIndex, grainIndex, BAT);
        }

        /// <summary>Relatives the allocation.</summary>
        /// <param name="iSupply">The organs.</param>
        /// <param name="iSink">The organs.</param>
        /// <param name="BAT">The organs.</param>
        public double AllocateStructuralFromOrgan(int iSupply, int iSink, BiomassArbitrationType BAT)
        {
            var tmp1 = BAT.StructuralDemand[iSink];
            var tmp2 = BAT.StructuralAllocation[iSink];
            var tmpcheck = BAT.StructuralDemand[iSink] - BAT.StructuralAllocation[iSink];

            double StructuralRequirement = Math.Max(0.0, BAT.StructuralDemand[iSink] - BAT.StructuralAllocation[iSink]);
            if ((StructuralRequirement) > 0.0)
            {
                //only allocate as much structural as demanded - cyclical process so allow for any amounts already allocated to Retranslocation
                double StructuralAllocation = Math.Min(StructuralRequirement, BAT.RetranslocationSupply[iSupply] - BAT.Retranslocation[iSupply]);
                BAT.StructuralAllocation[iSink] += StructuralAllocation;
                BAT.Retranslocation[iSupply] += StructuralAllocation;
                return StructuralAllocation;
            }
            return 0.0;
        }

        /// <summary>Relatives the allocation.</summary>
        /// <param name="leaf">The organs.</param>
        /// <param name="iSupply">The organs.</param>
        /// <param name="iSink">The organs.</param>
        /// <param name="BAT">The organs.</param>
        public double AllocateStructuralFromLeaf(SorghumLeaf leaf, int iSupply, int iSink, BiomassArbitrationType BAT)
        {
            //leaf called
            double StructuralRequirement = Math.Max(0.0, BAT.StructuralDemand[iSink] - BAT.StructuralAllocation[iSink]);
            if ((StructuralRequirement) > 0.0)
            {
                double currentRetranslocatedN = leaf.DltRetranslocatedN; //-ve number

                double providedN = leaf.provideNRetranslocation(BAT, StructuralRequirement);
                BAT.StructuralAllocation[iSink] += providedN;

                double afterRetranslocatedN = leaf.DltRetranslocatedN;
                //Leaf keeps track of retranslocation - the return value can include DltLAI which is not techncally retraslocated
                //Let leaf handle the updating

                BAT.Retranslocation[iSupply] += Math.Abs(afterRetranslocatedN - currentRetranslocatedN);
                return providedN;
            }
            return 0.0;
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
