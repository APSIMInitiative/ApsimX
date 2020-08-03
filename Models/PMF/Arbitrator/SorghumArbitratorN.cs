using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Newtonsoft.Json;
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
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class SorghumArbitratorN : Model, IArbitrationMethod, ICustomDocumentation
    {
        /// <summary>
        /// Daily NSupply.
        /// </summary>
        [JsonIgnore]
        public double NSupply { get; set; }

        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        public void DoAllocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply; // / 0.1; //g/m^2
            NSupply = TotalSupply; // reporting variable
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
            //var stemDemand = BAT.StructuralDemand[stemIndex] + BAT.MetabolicDemand[stemIndex];
            //var rachisDemand = BAT.StructuralDemand[rachisIndex] + BAT.MetabolicDemand[rachisIndex];
            //var leafMetabolicDemand = BAT.MetabolicDemand[leafIndex];
            //var leafStructuralDemand = BAT.StructuralDemand[leafIndex];

            //calc leaf demand separately - old sorghum doesn't quite fit
            var leaf = Organs[leafIndex] as SorghumLeaf;
            var leafAdjustment = leaf.calculateClassicDemandDelta();

            //var totalPlantNDemand = BAT.TotalPlantDemand + leafAdjustment - grainDemand; // to replicate calcNDemand in old sorghum 

            // dh - Old apsim calls organ->calcNDemand() to get demands. This is equivalent to metabolic NDemand in new apsim.
            //      Root and grain had no separation of structural/metabolic N in old apsim. New apsim is similar, except it's all in
            //      structural demand, so we need to remember to take that into account as well.
            double totalDemand = BAT.TotalMetabolicDemand + BAT.StructuralDemand[rootIndex] + BAT.StructuralDemand[grainIndex];
            double plantNDemand = Math.Max(0, totalDemand - grainDemand); // to replicate calcNDemand in old sorghum 
            if (MathUtilities.IsPositive(plantNDemand))
            {
                BAT.SupplyDemandRatioN = MathUtilities.Divide(BAT.TotalUptakeSupply, plantNDemand, 0);
                BAT.SupplyDemandRatioN = Math.Min(BAT.SupplyDemandRatioN, 1);
                // BAT.SupplyDemandRatioN = Math.Max(BAT.SupplyDemandRatioN, 0); // ?
            }
            
            // todo - what if root demand exceeds supply?
            double rootAllocation = BAT.SupplyDemandRatioN * BAT.StructuralDemand[rootIndex];
            rootAllocation = Math.Min(rootAllocation, NotAllocated);
            BAT.StructuralAllocation[rootIndex] += rootAllocation;
            NotAllocated -= (rootAllocation);
            TotalAllocated += (rootAllocation);

            AllocateStructural(stemIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateStructural(rachisIndex, ref TotalAllocated, ref NotAllocated, BAT);
            AllocateStructural(leafIndex, ref TotalAllocated, ref NotAllocated, BAT);

            // Structural allocation is subtracted from metabolic demand in old apsim.
            BAT.MetabolicDemand[leafIndex] = Math.Max(0, BAT.MetabolicDemand[leafIndex] - BAT.StructuralAllocation[leafIndex]);
            BAT.MetabolicDemand[rachisIndex] = Math.Max(0, BAT.MetabolicDemand[rachisIndex] - BAT.StructuralAllocation[rachisIndex]);
            BAT.MetabolicDemand[stemIndex] = Math.Max(0, BAT.MetabolicDemand[stemIndex] - BAT.StructuralAllocation[stemIndex]);

            double leafDemand = BAT.MetabolicDemand[leafIndex];
            double rachisDemand = BAT.MetabolicDemand[rachisIndex];
            double stemDemand = BAT.MetabolicDemand[stemIndex];

            double totalMetabolicDemand = leafDemand + rachisDemand + stemDemand;

            double leafProportion = MathUtilities.Bound(MathUtilities.Divide(leafDemand, totalMetabolicDemand, 0), 0, 1);
            double rachisProportion = MathUtilities.Bound(MathUtilities.Divide(rachisDemand, totalMetabolicDemand, 0), 0, 1);
            double stemProportion = MathUtilities.Bound(MathUtilities.Divide(stemDemand, totalMetabolicDemand, 0), 0, 1);

            double leafAlloc = NotAllocated * leafProportion;
            leafAlloc = Math.Min(leafAlloc, leafDemand);

            double rachisAlloc = NotAllocated * rachisProportion;
            rachisAlloc = Math.Min(rachisAlloc, rachisDemand);

            double stemAlloc = NotAllocated - leafAlloc - rachisAlloc;

            AllocateMetabolic(leafIndex, leafAlloc, BAT);
            AllocateMetabolic(rachisIndex, rachisAlloc, BAT);
            AllocateMetabolic(stemIndex, stemAlloc, BAT);

            if(!MathUtilities.FloatsAreEqual(leafAlloc+rachisAlloc+stemAlloc, NotAllocated, 0.0001))
            {
                //this is to check that nDemand is equal to old sorghum N demand calc
                throw new Exception("Proportional allocation of Metabolic N doesn't balance");
            }
            TotalAllocated += NotAllocated;
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

                if (storageAllocation > BAT.StorageDemand[i])
                    BAT.StorageDemand[i] += storageAllocation;
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
        /// <param name="N">The N bat.</param>
        /// <param name="dm">The DM BAT.</param>
        public void DoRetranslocation(IArbitration[] Organs, BiomassArbitrationType N, BiomassArbitrationType dm)
        {
            double NotAllocated = N.TotalRetranslocationSupply;
            //var rootIndex = 1;
            var leafIndex = 2;
            var rachisIndex = 3;
            var stemIndex = 4;
            var grainIndex = 0;

            var stemDemand = N.StructuralDemand[stemIndex];
            var rachisDemand = N.StructuralDemand[rachisIndex];
            var leafDemand = N.StructuralDemand[leafIndex];

            var forStem = AllocateStructuralFromLeaf(Organs[leafIndex] as SorghumLeaf, leafIndex, stemIndex, N);
            var forRachis = AllocateStructuralFromLeaf(Organs[leafIndex] as SorghumLeaf, leafIndex, rachisIndex, N);
            var forLeaffromStem = AllocateStructuralFromStem(stemIndex, leafIndex, N, dm, Organs[stemIndex] as GenericOrgan);
            var forLeaf = AllocateStructuralFromLeaf(Organs[leafIndex] as SorghumLeaf, leafIndex, leafIndex, N);

            // Retranslocate to grain
            double fromRachis = AllocateStructuralFromRachis(rachisIndex, grainIndex, N, dm, Organs[rachisIndex] as GenericOrgan);
            double fromStem = AllocateStructuralFromStem(stemIndex, grainIndex, N, dm, Organs[stemIndex] as GenericOrgan);
            double fromLeaf = AllocateStructuralFromLeaf(Organs[leafIndex] as SorghumLeaf, leafIndex, grainIndex, N);
        }

        /// <summary>Relatives the allocation.</summary>
        /// <param name="iSupply">The organs.</param>
        /// <param name="iSink">The organs.</param>
        /// <param name="n">The organs.</param>
        /// <param name="dm">The dm BAT.</param>
        /// <param name="source">The organ which N will be taken from.</param>
        public double AllocateStructuralFromRachis(int iSupply, int iSink, BiomassArbitrationType n, BiomassArbitrationType dm, GenericOrgan source)
        {
            double StructuralRequirement = Math.Max(0.0, n.StructuralDemand[iSink] - n.StructuralAllocation[iSink]);
            if (MathUtilities.IsPositive(StructuralRequirement))
            {
                //only allocate as much structural as demanded - cyclical process so allow for any amounts already allocated to Retranslocation
                double StructuralAllocation = Math.Min(StructuralRequirement, n.RetranslocationSupply[iSupply] - n.Retranslocation[iSupply]);

                double dmGreen = source.Live.Wt;
                double dltDmGreen = dm.StructuralAllocation[iSupply] + dm.MetabolicAllocation[iSupply];
                double dltNGreen = n.StructuralAllocation[iSupply] + n.MetabolicAllocation[iSupply];
                double nConc = MathUtilities.Divide(source.Live.N, dmGreen + dltDmGreen, 0);
                // dh - no point multiplying both numbers by 100 as we do in old apsim.
                if (nConc < source.MinNconc)
                    return 0;

                n.StructuralAllocation[iSink] += StructuralAllocation;
                n.Retranslocation[iSupply] += StructuralAllocation;
                return StructuralAllocation;
            }
            return 0.0;
        }

        /// <summary>Relatives the allocation.</summary>
        /// <param name="iSupply">The organs.</param>
        /// <param name="iSink">The organs.</param>
        /// <param name="n">The organs.</param>
        /// <param name="dm">The dm BAT.</param>
        /// <param name="source">The organ which N will be taken from.</param>
        public double AllocateStructuralFromStem(int iSupply, int iSink, BiomassArbitrationType n, BiomassArbitrationType dm, GenericOrgan source)
        {
            double StructuralRequirement = Math.Max(0.0, n.StructuralDemand[iSink] - n.StructuralAllocation[iSink]);
            if (MathUtilities.IsPositive(StructuralRequirement))
            {
                //only allocate as much structural as demanded - cyclical process so allow for any amounts already allocated to Retranslocation
                double nAvailable = Math.Min(StructuralRequirement, n.RetranslocationSupply[iSupply] - n.Retranslocation[iSupply]);
                double nProvided = 0;
                double dmGreen = source.Live.Wt;
                double dltDmGreen = dm.StructuralAllocation[iSupply] + dm.MetabolicAllocation[iSupply];
                double dltNGreen = n.StructuralAllocation[iSupply] + n.MetabolicAllocation[iSupply] + n.StorageAllocation[iSupply];

                // dh - no point multiplying both numbers by 100 as we do in old apsim.
                // dh - need to make this check before providing any N.
                double nConc = MathUtilities.Divide(source.Live.N, dmGreen + dltDmGreen, 0);
                if (nConc < source.CritNconc)
                    return 0;

                if (dltNGreen > StructuralRequirement)
                {
                    n.StructuralAllocation[iSink] += StructuralRequirement;
                    n.Retranslocation[iSupply] += StructuralRequirement;
                    return StructuralRequirement;
                }
                else
                {
                    StructuralRequirement -= dltNGreen;
                    nProvided = dltNGreen;
                }

                double availableN = n.RetranslocationSupply[iSupply] - n.Retranslocation[iSupply];

                // cannot take below structural N
                double structN = (dmGreen + dltDmGreen) * source.CritNconc;
                nAvailable = Math.Min(nAvailable, source.Live.N - structN);
                nAvailable = Math.Max(nAvailable, 0);
                nProvided += Math.Min(StructuralRequirement, nAvailable);

                n.StructuralAllocation[iSink] += nProvided;
                n.Retranslocation[iSupply] += nProvided;
                return nProvided;
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
            double StructuralRequirement = Math.Max(0.0, BAT.StructuralDemand[iSink] - BAT.StructuralAllocation[iSink]);
            if (MathUtilities.IsPositive(StructuralRequirement))
            {
                bool forLeaf = iSupply == iSink;
                double providedN = leaf.provideNRetranslocation(BAT, StructuralRequirement, forLeaf);
                BAT.StructuralAllocation[iSink] += providedN;

                // Leaf's dltRetranslocatedN is negative (as in old apsim).
                BAT.Retranslocation[iSupply] = Math.Abs(leaf.DltRetranslocatedN);

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
