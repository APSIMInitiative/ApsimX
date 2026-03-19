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
        [Link(Type = LinkType.Child, ByName = true)] IFunction X0 = null; // Node position where the largest leaf occurs (e.g. 10 is the 10th leaf from bottom to top)
        /// <summary>The area maximum</summary>
        [Link(Type = LinkType.Child, ByName = true)] IFunction aMax = null;             // Area of the largest leaf of a plant (mm2)
        /// <summary>The breadth</summary>
        [Link(Type = LinkType.Child, ByName = true)] IFunction a = null;
        /// <summary>The skewness</summary>
        [Link(Type = LinkType.Child, ByName = true)] IFunction b = null;
        /// <summary>The structure</summary>
        [Link] Structure Structure = null;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            double FLN = Structure.LeafTipsAppeared;

            return aMax.Value(arrayIndex) * Math.Exp(a.Value(arrayIndex) * Math.Pow(FLN - X0.Value(arrayIndex), 2.0)
                                + b.Value(arrayIndex) * (Math.Pow(FLN - X0.Value(arrayIndex), 3.0)));
        }
    }
}
