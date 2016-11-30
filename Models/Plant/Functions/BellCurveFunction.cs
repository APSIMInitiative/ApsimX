using System;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Calculates the maximum leaf size (mm2/leaf) given its node position (Elings, 2000 - Agronomy Journal 92, 436-444)
    /// </summary>
    [Serializable]
    public class BellCurveFunction : Model, IFunction
    {
        /// <summary>The largest leaf position</summary>
        [Link] IFunction LargestLeafPosition = null; // Node position where the largest leaf occurs (e.g. 10 is the 10th leaf from bottom to top)
        /// <summary>The area maximum</summary>
        [Link] IFunction AreaMax = null;             // Area of the largest leaf of a plant (mm2)
        /// <summary>The breadth</summary>
        [Link] IFunction Breadth = null;
        /// <summary>The skewness</summary>
        [Link] IFunction Skewness = null;
        /// <summary>The structure</summary>
        [Link] Structure Structure = null;

        /// <summary>Gets the value.</summary>
        public double Value
        {
            get
            {
                double LeafNo = Structure.LeafTipsAppeared;

                return AreaMax.Value * Math.Exp(Breadth.Value * Math.Pow(LeafNo - LargestLeafPosition.Value, 2.0)
                                  + Skewness.Value * (Math.Pow(LeafNo - LargestLeafPosition.Value, 3.0)));

            }
        }
    }
}
