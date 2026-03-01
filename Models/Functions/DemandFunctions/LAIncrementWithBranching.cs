using Models.Core;
using Models.PMF;
using System;
using System.Linq;
using APSIM.Core;

namespace Models.Functions.DemandFunctions
{
    /// <summary>
    /// Daily leaf area increment is calculated based on plant population, number of main stem nodes, number of branches, largest leaf 
    /// (or leaf pair) area, leaf appearance rate, and relative leaf area. The model assumes that, once fully developed, the main stem 
    /// and all branches exhibit similar characteristics. This function tracks branch appearance but relative growth is simulated at the
    /// branch level only (i.e., no tracking of individual leaves) assuming all leaves on a branch are the same size and growth at the 
    /// same rate. Branches appear at different times (i.e., node numbers) and each branch has its own series of leaf sizes. The lag 
    /// between each branch and the main stem is specified in terms of node number. The lag for the main stem is zero.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class LAIncrementWithBranching : Model, IFunction
    {
        /// <summary>The plant</summary>
        [Link]
        protected Plant parentPlant = null;

        /// <summary>The plant population</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction plantNumber = null;

        /// <summary>The number of nodes on the main stem</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction nodeNumber = null;

        /// <summary>The number of branches (excluding the main stem)</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction branchNumber = null;

        /// <summary>The area of the largest leaf</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction areaLargestLeaf = null;

        /// <summary>The rate of leaf appearance</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction lAR = null;

        /// <summary>The relative leaf area as a function of nodeNumber</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private XYPairs relativeArea = null;

        /// <summary>The lag (in terms of node number) between each individual branch (including the main stem) and the main stem</summary>
        private int[] branchNodeLag = Enumerable.Repeat(-1, 20).ToArray();

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double largestLeafArea = areaLargestLeaf.Value();
            double currNodeNumber = nodeNumber.Value();
            int numBranches = (int)branchNumber.Value();

            if (!parentPlant.IsAlive)
            {
                branchNodeLag = Enumerable.Repeat(-1, 20).ToArray();
                return 0;
            }

            // Main stem doesn't have a lag, so set it to 0.
            branchNodeLag[0] = 0;

            double cumDelta = relativeArea.ValueIndexed(currNodeNumber - branchNodeLag[0]);

            for (int i = 1; i < numBranches + 1; i++)
            {
                if (branchNodeLag[i] < 0)
                    branchNodeLag[i] = (int)currNodeNumber;

                cumDelta += relativeArea.ValueIndexed(currNodeNumber - branchNodeLag[i]);
            }
            return cumDelta * plantNumber.Value() * largestLeafArea * lAR.Value();
        }
    }
}