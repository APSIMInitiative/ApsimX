using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions.DemandFunctions
{
    /// <summary>Daily increment in leaf area is calculated from plant population, number of nodes on the main stem, number of branchers,
    /// area of the largest leaf (or leaf pair), rate of leaf appearance, and relative leaf area. It is assumed that the main stem and all 
    /// branches are similar when fully grown.</summary>
    [Serializable]
    public class LAIncrementWithBranching : Model, IFunction
    {
        /// <summary>The plant population</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction PlantNumber = null;

        /// <summary>The number of nodes on the main stem</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction NodeNumber = null;

        /// <summary>The number of branchers</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction BranchNumber = null;

        /// <summary>The area of the largest leaf</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction AreaLargestLeaf = null;

        /// <summary>The rate of leaf appearance</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction LAR = null;

        /// <summary>The relative leaf area as a function of NodeNumber</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private XYPairs RelativeArea = null;

        /// <summary>The lag (in terms of node number) between each individual branch (including the main stem) and the main stem</summary>
        private int[] branchNodeLag = Enumerable.Repeat(-1, 20).ToArray();

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double currNodeNumber = NodeNumber.Value();
            int numBranches = (int)BranchNumber.Value();
            double cumDelta = 0;

            for (int i = 0; i < numBranches; i++)
            {
                if (branchNodeLag[i] < 0)
                    branchNodeLag[i] = (int)currNodeNumber;

                cumDelta += PlantNumber.Value() * AreaLargestLeaf.Value() * LAR.Value() * RelativeArea.ValueIndexed(currNodeNumber - branchNodeLag[i]);
            }
            return cumDelta;
        }

        /// <summary>Document the model.</summary>
        public override IEnumerable<ITag> Document()
        {
            // Write description of this class from summary and remarks XML documentation.
            foreach (var tag in GetModelDescription())
                yield return tag;

            foreach (var tag in DocumentChildren<IModel>())
                yield return tag;
        }
    }
}