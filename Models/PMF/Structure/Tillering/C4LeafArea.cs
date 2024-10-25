using Models.Core;
using Models.Functions;
using Models.PMF.Struct;
using Models.Utilities;
using System;

namespace Models.PMF
{
    /// <summary>
    /// This is the basic organ class that contains biomass structures and transfers
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LeafCulms))]
    public class C4LeafArea : Model, ICulmLeafArea, IFunction
    {
        /// <summary>The parent Plant</summary>
        [Link]
        private readonly Plant plant = null;

        /// <summary> Culms on the leaf </summary>
        [Link]
        private LeafCulms culms = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction a0 = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction a1 = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction a2 = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction b0 = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction b1 = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction b2 = null;

        /// <summary>The intercept of the regression, of position of the largest leaf against final leaf number(FLN)</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction aX0I = null;

        /// <summary>The slope of the regression, of position of the largest leaf against final leaf number(FLN)</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction aX0S = null;

        /// <summary></summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction aMaxS = null;

        /// <summary>Senescence Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction aMaxI = null;

        /// <summary>Senescence Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction leafNoCorrection = null;

        /// <summary>Senescence Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction largestLeafPlateau = null;

        /// <summary>
        /// The sowing density, set from the sowing rule.
        /// </summary>
        private double sowingDensity;

        /// <summary> implement the IFunction interface - code is currently coupled to Leafculms
        /// Could be refactored to use an interface of Culms
        /// </summary>
        public double Value(int _)
        {
            culms ??= Parent as LeafCulms ?? throw new Exception("C4LeafArea expects a LeafCulms as a parent: " + Parent?.Name ?? "Null");

            return CalcPotentialLeafArea(culms);
        }

        /// <summary> Calculate the potential area for all culms</summary>
        public double CalcPotentialLeafArea(LeafCulms culms)
        {
            var dltCulmArea = 0.0;
            foreach (var culm in culms.Culms)
            {
                // Once leaf no is calculated leaf area of largest expanding leaf is determined
                double leafNoEffective = Math.Min(culm.CurrentLeafNo + leafNoCorrection.Value(), culm.FinalLeafNo);
                var tmpArea = CalculateIndividualLeafArea(leafNoEffective, culm);

                culm.LeafArea = tmpArea.ConvertSqM2SqMM() * sowingDensity * culm.dltLeafNo;
                culm.DltLAI = culm.LeafArea * culm.Proportion;

                double leafAreaNow = CalcCulmArea(leafNoEffective - culm.dltLeafNo, culm);
                culm.LeafArea = CalcCulmArea(leafNoEffective, culm);
                double dltLeafArea = Math.Max(culm.LeafArea - leafAreaNow, 0.0);
                culm.DltLAI = dltLeafArea.ConvertSqM2SqMM() * sowingDensity * culm.Proportion;
                dltCulmArea += culm.DltLAI;
                culm.TotalLAI += culm.DltLAI;
            }

            return dltCulmArea;
        }

        private static double CalcCulmArea(double nLeaves, Culm culm)
        {
            // Sum the area of each leaf plus the fraction of the last.
            double area = 0;
           for (int i = 0; i < Math.Ceiling(nLeaves) && i < culm.LeafSizes.Count; i++)
            {
                double fraction = Math.Min(1.0, nLeaves - i);
                area += culm.LeafSizes[i] * fraction;
            }
            return area;
        }

        /// <inheritdoc/>
        public double CalculateAreaOfLargestLeaf(double finalLeafNo, int culmNo)
        {
            double areaOfLargestLeaf = C4LeafCalculations.CalculateAreaOfLargestLeaf(
                aMaxI.Value(),
                aMaxS.Value(),
                finalLeafNo,
                culmNo
            );

            return areaOfLargestLeaf;
        }

        /// <inheritdoc/>
        public double CalculateLargestLeafPosition(double finalLeafNo, int culmNo)
        {
            // Use finalLeafNo to calculate the size of the individual leafs
            // Eqn 5 from Improved methods for predicting individual leaf area and leaf senescence in maize
            // (Zea mays) C.J. Birch, G.L. Hammer and K.G. Ricket. Aust. J Agric. Res., 1998, 49, 249-62
            double largestLeafPosition = C4LeafCalculations.CalculateLargestLeafPosition(
                aX0I.Value(),
                aX0S.Value(),
                finalLeafNo,
                culmNo
            );

            return largestLeafPosition;
        }

        /// <inheritdoc/>
        public double CalculateIndividualLeafArea(double leafNo, Culm culm)
        {
            leafNo = AdjustLeafNumberForPlateuEffect(leafNo, culm.FinalLeafNo, largestLeafPlateau.Value(), culm.CulmNo);

            // Bell shaped curve characteristics.
            var a = a0.Value() + (a1.Value() / (1 + a2.Value() * Math.Min(Math.Max(culm.FinalLeafNo,10),23)));
            var b = b0.Value() + (b1.Value() / (1 + b2.Value() * Math.Min(Math.Max(culm.FinalLeafNo, 10), 23)));

            // A vertical adjustment is applied to each tiller - this was discussed in a meeting on 22/08/12 and derived 
            // from a set of graphs in Tonge's paper
            // the effect is to decrease the size of the largest leaf by 10% for each subsequent tiller
            double leafSize =
                culm.AreaOfLargestLeaf *
                    Math.Exp(a * Math.Pow((leafNo - culm.PositionOfLargestLeaf), 2) +
                    b * Math.Pow((leafNo - culm.PositionOfLargestLeaf), 3)) *
                    100;

            return Math.Max(leafSize, 0.0);
        }

        private double AdjustLeafNumberForPlateuEffect(double leafNo, double finalLeafNo, double largestLeafPlateau, int culmNo)
        {
            // Checks to prevent getting into an error state.
            if (largestLeafPlateau <= 0) return leafNo;
            if (finalLeafNo < largestLeafPlateau) return leafNo;

            var largestLeafPos = C4LeafCalculations.CalculateLargestLeafPosition(aX0S.Value(), aX0I.Value(), largestLeafPlateau, culmNo);
            if (leafNo <= largestLeafPos) return leafNo;

            // Some varieties with large numbers of leaves have a plateau effect for leaf size where the leaves
            // plateau in size and they are all the same size as the largest leaf
            double tailCount = largestLeafPlateau - largestLeafPos;
            if (leafNo < finalLeafNo - tailCount)
            {
                return largestLeafPos;
            }
            return largestLeafPlateau - (finalLeafNo - leafNo);
        }

        /// <summary>Called when crop is sowed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == plant)
            {
                sowingDensity = data.Population;
            }
        }
    }
}