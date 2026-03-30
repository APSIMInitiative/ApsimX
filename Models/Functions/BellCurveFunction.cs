using System;
using APSIM.Core;
using MessagePack.Formatters;
using Models.Core;
using Models.PMF.Organs;
using Models.PMF.Struct;

namespace Models.Functions
{
    /// <summary>
    /// Calculates the maximum leaf size (mm2/leaf) given its node position (Elings, 2000 - Agronomy Journal 92, 436-444)
    /// </summary>
    [Serializable]
    public class BellCurveFunction : Model, IFunction
    {
        [Link(Type = LinkType.Child, ByName = true)] IFunction PositionLargestLeaf = null; // Node position where the largest leaf occurs (e.g. 10 is the 10th leaf from bottom to top)
        /// <summary>The area maximum</summary>
        [Link(Type = LinkType.Child, ByName = true)] IFunction AreaLargestLeaf = null;             // Area of the largest leaf of a plant (mm2)
        /// <summary>The breadth</summary>
        [Link(Type = LinkType.Child, ByName = true)] IFunction Skewness = null;
        /// <summary>The skewness</summary>
        [Link(Type = LinkType.Child, ByName = true)] IFunction Breadth = null;
        /// <summary>The structure</summary>
        [Link] Leaf leaf = null;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            double x = leaf.AppearedCohortNo;
            double x0 = PositionLargestLeaf.Value();
            double a = Breadth.Value();
            double b = Skewness.Value();
            double aMax = AreaLargestLeaf.Value();
            return aMax * Math.Exp(a * Math.Pow((x - x0),2) + b * Math.Pow((x - x0),3));
        }
    }
}
