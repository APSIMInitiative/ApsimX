using Models.Core;
using Models.PMF;
using System;
using System.Linq;
using APSIM.Core;

namespace Models.Functions.DemandFunctions
{
    /// <summary>
    /// Daily leaf area increment is calculated based on plant population, number of main stem nodes, number of branches, 
    /// largest leaf (or leaf pair) area, leaf appearance rate, and relative leaf area. The model assumes that, once fully developed, 
    /// the main stem and all branches exhibit similar characteristics. This function tracks leaf sizes across all branches and ensures 
    /// that no leaf exceeds the maximum area permitted by the model parameters (i.e., relative leaf area multiplied by the largest leaf area)
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

        /// <summary>Track leaf sizes?</summary>
        [Description("Track leaf sizes?")]
        public bool DoTrackLeafSize { get; set; } = true;

        /// <summary>The lag (in terms of node number) between each individual branch (including the main stem) and the main stem</summary>
        private int[] branchNodeLag = Enumerable.Repeat(-1, 20).ToArray();

        /// <summary>A 2D array of leaf size relative to `AreaLargestLeaf`. Rows are branches. Columns are nodes.</summary>
        private double[,] leafRelSize = new double[20, 50];

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

            if (DoTrackLeafSize)
            {
                double plantLeafGrowth = 0;

                for (int br = 0; br <= numBranches; br++)
                {
                    if (branchNodeLag[br] < 0)
                        branchNodeLag[br] = (int)currNodeNumber;

                    // Number of nodes on branch `br`.
                    int currBranchNodeNumber = (int)(currNodeNumber - branchNodeLag[br]);

                    for (int node = 0; node <= currBranchNodeNumber; node++)
                    {
                        // Keep the old size of the leaf.
                        double oldLeafRelSize = leafRelSize[br, node];

                        // Maximum size of leaf relative to `largestLeafArea` on this `node` of branch `br`.
                        double currNodeMaxRelArea = relativeArea.ValueIndexed(node);

                        // Maximum possible growth of leaf on this `node` of branch `br`. 
                        double maxNodeLeafRelGrowth = lAR.Value() * currNodeMaxRelArea;

                        // Updated size of the leaf on this `node` of branch `br`. 
                        leafRelSize[br, node] = Math.Min(oldLeafRelSize + maxNodeLeafRelGrowth, currNodeMaxRelArea);

                        // Actual growth of leaf on this `node` of branch `br`. 
                        double actualNodeLeafGrowth = leafRelSize[br, node] - oldLeafRelSize;

                        // Total leaf growth of the plant.
                        plantLeafGrowth += actualNodeLeafGrowth * largestLeafArea;
                    }
                }

                // Total increase in leaf area at the crop level.
                return plantLeafGrowth * plantNumber.Value();

            }
            else
            {
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
}