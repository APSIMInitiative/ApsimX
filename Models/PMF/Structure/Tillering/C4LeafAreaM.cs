using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Struct;
using Models.Utilities;

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
        public Plant plant = null;

        /// <summary> Culms on the leaf </summary>
        [Link]
        public LeafCulms culms = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction a0 = null;
        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction a1 = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction b0 = null;
        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction b1 = null;

        /// <summary>Largest Leaf Position as a percentage of Final Leaf No</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction aX0 = null;

        /// <summary></summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction aMaxA = null;

        /// <summary>Senescence Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction aMaxB = null;

        /// <summary>Senescence Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction aMaxC = null;

        /// <summary>Senescence Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction leafNoCorrection = null;

        private double sowingDensity;

        /// <summary> implement the IFunction interface - code is currently coupled to Leafculms
        /// Could be refactored to use an interface of Culms
        /// </summary>
        public double Value(int arrayIndex = -1)
        {
            if (culms == null) culms = Parent as LeafCulms ?? throw new Exception("C4LeafArea expects a LeafCulms as a parent: " + Parent?.Name ?? "Null");

            return calcPotentialLeafArea(culms);
        }

        /// <summary> Calculate the potential area for all culms</summary>
        public double calcPotentialLeafArea(LeafCulms culms)
        {
            var dltCulmArea = 0.0;
            foreach (var culm in culms.Culms)
            {
                //once leaf no is calculated leaf area of largest expanding leaf is determined
                double leafNoEffective = Math.Min(culm.CurrentLeafNo + leafNoCorrection.Value(), culm.FinalLeafNo - culm.LeafNoAtAppearance);
                var tmpArea = CalculateIndividualLeafArea(leafNoEffective, culm.FinalLeafNo, culm.VertAdjValue).ConvertSqM2SqMM();

                culm.LeafArea = tmpArea * sowingDensity * culm.dltLeafNo; // in dltLai
                culm.TotalLAI += culm.LeafArea; //not sure what this is doing as actual growth may adjust this

                dltCulmArea += culm.LeafArea * culm.Proportion;
            }
            return dltCulmArea;
        }

        /// <summary>Calculate potential LeafArea</summary>
        public double CalculateIndividualLeafArea(double leafNo, double finalLeafNo, double vertAdjust = 0.0)
        {
            // use finalLeafNo to calculate the size of the individual leafs
            // Eqn 5 from Improved methods for predicting individual leaf area and leaf senescence in maize
            // (Zea mays) C.J. Birch, G.L. Hammer and K.G. Ricket. Aust. J Agric. Res., 1998, 49, 249-62
            // TODO	externalise these variables

            double a = a0.Value() - Math.Exp(a1.Value() * finalLeafNo);                      // Eqn 18
            double b = b0.Value() - Math.Exp(b1.Value() * finalLeafNo);                      // Eqn 19

            double largestLeafSize = calcLargestLeafSize(finalLeafNo);
            largestLeafSize *= (1 - vertAdjust);

            double largestLeafPos = aX0.Value() * finalLeafNo;                                          // Eqn 14
            double relativeLeafPos = leafNo - largestLeafPos;

            return largestLeafSize * Math.Exp(a * Math.Pow(relativeLeafPos, 2) + b * Math.Pow(relativeLeafPos, 3)) * 100;  // Eqn 5
        }

        private double calcLargestLeafSize(double finalLeafNo)
        {
            //Largest Leaf calculation
            double a = aMaxA.Value();
            double b = aMaxB.Value();
            double c = aMaxC.Value();

            return a * Math.Exp(b + c * finalLeafNo);         // Eqn 13

            //originally from "Improved methods for predicting individual leaf area and leaf senescence in maize" - Birch, Hammer, Rickert 1998
            //double aMaxB = 4.629148, aMaxC = 6.6261562; 
            //double aMax = aMaxA * (1 - exp(-aMaxB * (finalLeafNo - aMaxC)));  // maximum individual leaf area
            //Calculation then changed to use the relationship as described in the Carberry paper in Table 2
            //The actual intercept and slope will be determined by the cultivar, and read from the config file (sorghum.xml)
            //aMaxS = 19.5; //not 100% sure what this number should be - tried a range and this provided the best fit forthe test data

            //return aMaxS.Value() * finalLeafNo + aMaxI.Value();
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
