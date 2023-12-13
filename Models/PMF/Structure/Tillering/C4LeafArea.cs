using Models.Core;
using Models.Functions;
using Models.PMF.Phen;
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
		public Plant plant = null;
        /// <summary>
		/// 
		/// </summary>
		[Link]
        public Phenology phenology = null;
        /// <summary>
		/// 
		/// </summary>
		[Link]
        public IClock clock = null;

        /// <summary> Culms on the leaf </summary>
        [Link]
		public LeafCulms culms = null;

		///// <summary>The Potential Area Calculation</summary>
		//[Link(Type = LinkType.Child, ByName = true)]
		//readonly IFunction a0 = null;

		///// <summary>The Potential Area Calculation</summary>
		//[Link(Type = LinkType.Child, ByName = true)]
		//readonly IFunction a1 = null;

		///// <summary>The Potential Area Calculation</summary>
		//[Link(Type = LinkType.Child, ByName = true)]
		//readonly IFunction b0 = null;

		///// <summary>The Potential Area Calculation</summary>
		//[Link(Type = LinkType.Child, ByName = true)]
		//readonly IFunction b1 = null;

		///// <summary>Largest Leaf Position as a percentage of Final Leaf No</summary>
		//[Link(Type = LinkType.Child, ByName = true)]
		//IFunction aX0 = null;

		/// <summary>The intercept of the regression, of position of the largest leaf against final leaf number(FLN)</summary>
		[Link(Type = LinkType.Child, ByName = true)]
        readonly IFunction aX0I = null;

        /// <summary>The slope of the regression, of position of the largest leaf against final leaf number(FLN)</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        readonly IFunction aX0S = null;

        /// <summary></summary>
        [Link(Type = LinkType.Child, ByName = true)]
		readonly IFunction aMaxS = null;

		/// <summary>Senescence Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		readonly IFunction aMaxI = null;

		/// <summary>Senescence Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		readonly IFunction leafNoCorrection = null;

		/// <summary>Senescence Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		readonly IFunction largestLeafPlateau = null;
        
		private double sowingDensity;
		
		/// <summary> implement the IFunction interface - code is currently coupled to Leafculms
		/// Could be refactored to use an interface of Culms
		/// </summary>
		public double Value(int arrayIndex = -1)
		{
			if (culms == null) culms = Parent as LeafCulms ?? throw new Exception("C4LeafArea expects a LeafCulms as a parent: " + Parent?.Name ?? "Null");

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
				var tmpArea = CalculateIndividualLeafArea(leafNoEffective, culm.FinalLeafNo, culm.CulmNo);

				culm.LeafArea = tmpArea.ConvertSqM2SqMM() * sowingDensity * culm.dltLeafNo; // in dltLai
                culm.TotalLAI += culm.LeafArea; //not sure what this is doing as actual growth may adjust this
				
				culm.DltLAI = culm.LeafArea * culm.Proportion;
				dltCulmArea += culm.DltLAI;
			}

            return dltCulmArea;
		}

		/// <summary>Calculate potential LeafArea</summary>
		public double CalculateIndividualLeafArea(double leafNo, double finalLeafNo, int culmNo)
		{
			// use finalLeafNo to calculate the size of the individual leafs
			// Eqn 5 from Improved methods for predicting individual leaf area and leaf senescence in maize
			// (Zea mays) C.J. Birch, G.L. Hammer and K.G. Ricket. Aust. J Agric. Res., 1998, 49, 249-62
			double largestLeafPos = C4LeafCalculations.CalculateLargestLeafPosition(
				aX0I.Value(),
				aX0S.Value(),
				finalLeafNo, 
				culmNo
			);

            double largestLeafSize = C4LeafCalculations.CalculateAreaOfLargestLeaf(
                aMaxI.Value(),
                aMaxS.Value(),
                finalLeafNo,
				culmNo
            );

            leafNo = AdjustLeafNumberForPlateuEffect(leafNo, finalLeafNo, largestLeafPlateau.Value(), culmNo);
            //double leafPlateauStart = 24;
            //adding new code to handle varieties that grow very high number of leaves
            //double a0 = -0.009, a1 = -0.2;
            //double b0 = 0.0006, b1 = -0.43;


            var a0 = 0.00955;
            var a1 = 0.0608;
            var a2 = -0.1293;
            var b0 = 0.00144;
            var b1 = 0.0025;
            var b2 = -0.11;

            // Bell shaped curve characteristics.
            var a = a0 + (a1 / (1 + a2 * finalLeafNo));
            var b = b0 + (b1 / (1 + b2 * finalLeafNo));

            //Relationship for calculating maximum individual leaf area from Total Leaf No
            //Source: Modelling genotypic and environmental control of leaf area dynamics in grain sorghum. II. Individual leaf level 
            //Carberry, Muchow, Hammer,1992
            //written as Y = Y0*exp(a*pow(X-X0,2)+b*(pow(X-X0,3))) 
            //pg314 -Leaf area production model

            //Largest Leaf calculation
            //originally from "Improved methods for predicting individual leaf area and leaf senescence in maize" - Birch, Hammer, Rickert 1998
            //double aMaxB = 4.629148, aMaxC = 6.6261562; 
            //double aMax = aMaxA * (1 - exp(-aMaxB * (finalLeafNo - aMaxC)));  // maximum individual leaf area
            //Calculation then changed to use the relationship as described in the Carberry paper in Table 2
            //The actual intercept and slope will be determined by the cultivar, and read from the config file (sorghum.xml)
            //aMaxS = 19.5; //not 100% sure what this number should be - tried a range and this provided the best fit forthe test data


            //a vertical adjustment is applied to each tiller - this was discussed in a meeting on 22/08/12 and derived 
            //from a set of graphs in Tonge's paper
            //the effect is to decrease the size of the largest leaf by 10% for each subsequent tiller
			double leafSize = largestLeafSize * Math.Exp(a * Math.Pow((leafNo - largestLeafPos), 2) + b * Math.Pow((leafNo - largestLeafPos), 3)) * 100;
			return Math.Max(leafSize,0.0);
        }

		private double AdjustLeafNumberForPlateuEffect(double leafNo, double finalLeafNo, double largestLeafPlateau, int culmNo)
		{
			if (largestLeafPlateau <= 0) return leafNo;
			if (finalLeafNo < largestLeafPlateau) return leafNo; //so it doesn't get in an error state

			var largestLeafPos = C4LeafCalculations.CalculateLargestLeafPosition(aX0S.Value(), aX0I.Value(), largestLeafPlateau, culmNo);
			if (leafNo <= largestLeafPos) return leafNo;

			//Some varieties with large numbers of leaves have a plateau effect for leaf size where the leaves
			//plateau in size and they are all the same size as the largest leaf

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