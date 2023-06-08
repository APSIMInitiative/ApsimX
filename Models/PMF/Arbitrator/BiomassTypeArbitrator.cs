using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.PMF.Arbitrator;
using Models.PMF.Interfaces;

namespace Models.PMF
{
    /// <summary>
    /// This class holds the functions for arbitrating Biomass - either DM or N
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IArbitrator))]
    public class BiomassTypeArbitrator : Model
    {
        private List<IPartitionMethod> potentialPartitioningMethods = null;
        private List<IPartitionMethod> actualPartitioningMethods = null;
        private List<IAllocationMethod> allocationMethods = null;

        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        protected IArbitrationMethod ArbitrationMethod = null;

        /// <summary>The method used to Allocate Uptakes
        /// DM doesn't need this Method, so it has been made optional
        /// It needs access to the ArbitrationMethod, so it is easiest to be in here
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        protected IPartitionMethod AllocateUptakesMethod = null;

        /// <summary>Functions called at DoPotentialPartitioning.</summary>
        public void DoPotentialPartitioning(IArbitration[] Organs, BiomassArbitrationType DM)
        {
            potentialPartitioningMethods.ForEach(pm => pm.Calculate(Organs, DM, ArbitrationMethod));
        }
        /// <summary>Functions called at DoActualPartitioning.</summary>
        public void DoActualPartitioning(IArbitration[] Organs, BiomassArbitrationType DM)
        {
            actualPartitioningMethods.ForEach(pm => pm.Calculate(Organs, DM, ArbitrationMethod));
        }

        /// <summary>Functions called at DoAllocations.</summary>
        public void DoAllocations(IArbitration[] Organs, BiomassArbitrationType DM)
        {
            allocationMethods.ForEach(pm => pm.Allocate(Organs, DM));
        }

        /// <summary>Functions called at DoUptakes.</summary>
        public void DoUptakes(IArbitration[] Organs, BiomassArbitrationType DM)
        {
            AllocateUptakesMethod.Calculate(Organs, DM, ArbitrationMethod);
        }

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            potentialPartitioningMethods = FindChild<Folder>("PotentialPartitioningMethods")?.FindAllChildren<IPartitionMethod>().ToList();
            actualPartitioningMethods = FindChild<Folder>("ActualPartitioningMethods")?.FindAllChildren<IPartitionMethod>().ToList();
            allocationMethods = FindChild<Folder>("AllocationMethods")?.FindAllChildren<IAllocationMethod>().ToList();
        }
    }
}
