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
    public class C4LeafAreaM : Model, ICulmLeafArea, IFunction
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
        private readonly IFunction b0 = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction b1 = null;

        /// <summary>Largest Leaf Position as a percentage of Final Leaf No</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction aX0 = null;

        /// <summary></summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction aMaxA = null;

        /// <summary>Senescence Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction aMaxB = null;

        /// <summary>Senescence Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction aMaxC = null;

        /// <summary>Senescence Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction leafNoCorrection = null;

        private double sowingDensity;

        /// <summary> implement the IFunction interface - code is currently coupled to Leafculms
        /// Could be refactored to use an interface of Culms
        /// </summary>
        public double Value(int arrayIndex = -1)
        {
            culms ??= Parent as LeafCulms ?? throw new Exception("C4LeafAreaM expects a LeafCulms as a parent: " + Parent?.Name ?? "Null");

            return CalcPotentialLeafArea(culms);
        }

        /// <summary> Calculate the potential area for all culms</summary>
        public double CalcPotentialLeafArea(LeafCulms culms)
        {
            var dltCulmArea = 0.0;
            foreach (var culm in culms.Culms)
            {
                //once leaf no is calculated leaf area of largest expanding leaf is determined
                double leafNoEffective = Math.Min(culm.CurrentLeafNo + leafNoCorrection.Value(), culm.FinalLeafNo - culm.LeafNoAtAppearance);
                var tmpArea = CalculateIndividualLeafArea(leafNoEffective, culm).ConvertSqM2SqMM();

                // In dltLai
                culm.LeafArea = tmpArea * sowingDensity * culm.dltLeafNo;
                // Not sure what this is doing as actual growth may adjust this
                culm.TotalLAI += culm.LeafArea;

                dltCulmArea += culm.LeafArea * culm.Proportion;
            }
            return dltCulmArea;
        }

        /// <inheritdoc/>
        public double CalculateAreaOfLargestLeaf(double finalLeafNo, int culmNo)
        {
            //Largest Leaf calculation
            double a = aMaxA.Value();
            double b = aMaxB.Value();
            double c = aMaxC.Value();

            // Eqn 13
            return a * Math.Exp(b + c * finalLeafNo);
        }

        /// <inheritdoc/>
        public double CalculateLargestLeafPosition(double finalLeafNo, int culmNo)
        {
            double largestLeafPos = aX0.Value() * finalLeafNo;
            return largestLeafPos;
        }

        /// <inheritdoc/>
        public double CalculateIndividualLeafArea(double leafNo, Culm culm)
        {
            // Eqn 18
            double a = a0.Value() - Math.Exp(a1.Value() * culm.FinalLeafNo);
            // Eqn 19
            double b = b0.Value() - Math.Exp(b1.Value() * culm.FinalLeafNo);

            double leafSize =
                culm.AreaOfLargestLeaf *
                    Math.Exp(a * Math.Pow((leafNo - culm.PositionOfLargestLeaf), 2) +
                    b * Math.Pow((leafNo - culm.PositionOfLargestLeaf), 3)) *
                    100;

            return Math.Max(leafSize, 0.0);
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
