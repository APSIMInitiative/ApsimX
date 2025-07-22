using System;
using APSIM.Core;
using Models.Core;
using Models.PMF.Struct;

namespace Models.Functions
{
    /// <summary>
    /// Calculates the maximum leaf size (mm2/leaf) given its node position (Elings, 2000 - Agronomy Journal 92, 436-444)
    /// </summary>
    [Serializable]
    public class BellCurveFunction : Model, IFunction
    {
        /// <summary>The largest leaf position</summary>
        [Link(Type = LinkType.Child, ByName = true)] IFunction LargestLeafPosition = null; // Node position where the largest leaf occurs (e.g. 10 is the 10th leaf from bottom to top)
        /// <summary>The area maximum</summary>
        [Link(Type = LinkType.Child, ByName = true)] IFunction AreaMax = null;             // Area of the largest leaf of a plant (mm2)
        /// <summary>The breadth</summary>
        [Link(Type = LinkType.Child, ByName = true)] IFunction Breadth = null;
        /// <summary>The skewness</summary>
        [Link(Type = LinkType.Child, ByName = true)] IFunction Skewness = null;
        /// <summary>The structure</summary>
        [Link] Structure Structure = null;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            double LeafNo = Structure.LeafTipsAppeared;

            return AreaMax.Value(arrayIndex) * Math.Exp(Breadth.Value(arrayIndex) * Math.Pow(LeafNo - LargestLeafPosition.Value(arrayIndex), 2.0)
                                + Skewness.Value(arrayIndex) * (Math.Pow(LeafNo - LargestLeafPosition.Value(arrayIndex), 3.0)));
        }
    }
}
