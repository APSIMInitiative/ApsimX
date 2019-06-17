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
            double NotAllocated = TotalSupply; // / 0.1; //g/m^2
            //allocate structural first - will be a different order to biomass so need to hard code the order until an interface is created
            //roots
            //stem
            //rachis
            //leaf

            //then allocate metabolic relative to demand
            var grainIndex = 0;
            var rootIndex = 1;
            var leafIndex = 2;
            var rachisIndex = 3;
            var stemIndex = 4;

            var grainDemand = BAT.StructuralDemand[grainIndex] + BAT.MetabolicDemand[grainIndex];
            var rootDemand = BAT.StructuralDemand[rootIndex] + BAT.MetabolicDemand[rootIndex];
            var stemDemand = BAT.StructuralDemand[stemIndex] + BAT.MetabolicDemand[stemIndex];
            var rachisDemand = BAT.StructuralDemand[rachisIndex] + BAT.MetabolicDemand[rachisIndex];
            var leafMetabolicDemand = BAT.MetabolicDemand[leafIndex];
            var leafStructuralDemand = BAT.StructuralDemand[leafIndex];

            //calc leaf demand separately - old sorghum doesn't quite fit
            var leaf = Organs[leafIndex] as SorghumLeaf;
            var leafAdjustment = leaf.calculateClassicDemandDelta();

            var totalPlantNDemand = BAT.TotalPlantDemand + leafAdjustment - grainDemand; // to replicate calcNDemand in old sorghum 
            if (MathUtilities.IsPositive(totalPlantNDemand))
            {
                BAT.SupplyDemandRatioN = MathUtilities.Divide(BAT.TotalUptakeSupply, totalPlantNDemand, 0);
                BAT.SupplyDemandRatioN = Math.Min(BAT.SupplyDemandRatioN, 1);
                // BAT.SupplyDemandRatioN = Math.Max(BAT.SupplyDemandRatioN, 0); // ?
            }

            double rootAllocation = BAT.SupplyDemandRatioN * BAT.StructuralDemand[rootIndex];
            BAT.StructuralAllocation[rootIndex] += rootAllocation;
            NotAllocated -= (rootAllocation);
            TotalAllocated += (rootAllocation);

            AllocateStructural(stemIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateStructural(rachisIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateStructural(leafIndex, ref TotalAllocated, ref NotAllocated, BAT);

            var nDemand = totalPlantNDemand - rootDemand;
            double totalMetabolicDemand = BAT.MetabolicDemand[leafIndex] + BAT.MetabolicDemand[rachisIndex] + BAT.MetabolicDemand[stemIndex];

            double leafProportion = MathUtilities.Divide(BAT.MetabolicDemand[leafIndex], totalMetabolicDemand, 0);
            double rachisProportion = MathUtilities.Divide(BAT.MetabolicDemand[rachisIndex], totalMetabolicDemand, 0);
            double stemProportion = MathUtilities.Divide(BAT.MetabolicDemand[stemIndex], totalMetabolicDemand, 0);

            var leafAlloc = NotAllocated * leafProportion;
            var rachisAlloc = NotAllocated * rachisProportion;
            var stemAlloc = NotAllocated * stemProportion;

            AllocateMetabolic(leafIndex, leafAlloc, BAT);
            AllocateMetabolic(rachisIndex, rachisAlloc, BAT);
            AllocateMetabolic(stemIndex, stemAlloc, BAT);

            if(!MathUtilities.FloatsAreEqual(leafAlloc+rachisAlloc+stemAlloc, NotAllocated))
            {
                //this is to check that nDemand is equal to old sorghum N demand calc
                throw new Exception("Proportional allocation of Metabolic N doesn't balance");
            }
            TotalAllocated += NotAllocated;
        }
        /// <summary>
        /// Calculating the amount of N to allocate to an organ using its proportion to totalDemand
        /// </summary>
        /// <param name="notAllocated">Amount of N that has not been allocated in g/m^2</param>
        /// <param name="organDemand">N demand for the organ as calculated in old Sorghum in g/m^2</param>
        /// <param name="totalDemand">Total N demand for Leaf, Stem and Rachis as calculated in old Sorghum in g/m^2</param>
        private double CalcPoportionalAllocation(double notAllocated, double organDemand, double totalDemand)
        {
            if(MathUtilities.IsNegative(organDemand) || MathUtilities.IsNegative(totalDemand))
            {
                throw new Exception("Invalid demand property");
            }
            return notAllocated * MathUtilities.Divide(organDemand, totalDemand, 0.0);
        }
        private void AllocateStructural(int i, ref double TotalAllocated, ref double NotAllocated, BiomassArbitrationType BAT)
        {
            double StructuralRequirement = Math.Max(0.0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
            if (MathUtilities.IsPositive(StructuralRequirement))
            {
                double StructuralAllocation = Math.Min(StructuralRequirement, NotAllocated);
                BAT.StructuralAllocation[i] += StructuralAllocation;
                NotAllocated -= (StructuralAllocation);
                TotalAllocated += (StructuralAllocation);
            }
        }
        private void AllocateMetabolic(int i, double allocation, BiomassArbitrationType BAT)
        {
            double MetabolicRequirement = Math.Max(0.0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
            if (MathUtilities.IsPositive(allocation))
            {
                //double MetabolicAllocation = Math.Max(0.0, NotAllocated * MathUtilities.Divide(BAT.MetabolicDemand[i], nTotalDemand, 0));
                //double Allocation = Math.Max(0.0, allocatation * MathUtilities.Divide(organDemand, nTotalDemand, 0));
                double MetabolicAllocation = Math.Min(MetabolicRequirement, allocation);

                //to stop it from givig it too much metabolic - push the flowover from metabolic into storage
                BAT.MetabolicAllocation[i] += MetabolicAllocation; 

                //do storage if there is any leftover
                double storageAllocation = allocation - MetabolicAllocation;
                BAT.StorageAllocation[i] += storageAllocation;
            }
        }
        private void AllocateStorage(int i, ref double TotalAllocated, ref double NotAllocated, BiomassArbitrationType BAT)
        {
            double StorageRequirement = Math.Max(0.0, BAT.StorageDemand[i] - BAT.StorageAllocation[i]); //N needed to take organ up to maximum N concentration, Structural, Metabolic and Luxury N demands
            if (MathUtilities.IsPositive(StorageRequirement))
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
            if (MathUtilities.IsPositive(StructuralRequirement))
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
            if (MathUtilities.IsPositive(StructuralRequirement))
            {
                double currentRetranslocatedN = leaf.DltRetranslocatedN; //-ve number

                bool forLeaf = iSupply == iSink;
                double providedN = leaf.provideNRetranslocation(BAT, StructuralRequirement, forLeaf);
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
