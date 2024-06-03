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

		/// <summary>The Potential Area Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction a0 = null;

		/// <summary>The Potential Area Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction a1 = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction a2 = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
		IFunction b0 = null;

		/// <summary>The Potential Area Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction b1 = null;

        /// <summary>The Potential Area Calculation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction b2 = null;

        ///// <summary>Largest Leaf Position as a percentage of Final Leaf No</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
		//IFunction aX0 = null;

		/// <summary>The intercept of the regression, of position of the largest leaf against final leaf number(FLN)</summary>
		[Link(Type = LinkType.Child, ByName = true)]
        IFunction aX0I = null;

        /// <summary>The slope of the regression, of position of the largest leaf against final leaf number(FLN)</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction aX0S = null;

        /// <summary></summary>
        [Link(Type = LinkType.Child, ByName = true)]
		IFunction aMaxS = null;

		/// <summary>Senescence Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction aMaxI = null;

		/// <summary>Senescence Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction leafNoCorrection = null;

		/// <summary>Senescence Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction largestLeafPlateau = null;
        
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
				double leafNoEffective = Math.Min(culm.CurrentLeafNo + leafNoCorrection.Value(), culm.FinalLeafNo);
				var tmpArea = CalculateIndividualLeafArea(leafNoEffective, culm);

				culm.LeafArea = tmpArea.ConvertSqM2SqMM() * sowingDensity * culm.dltLeafNo; // in dltLai
                //culm.TotalLAI += culm.LeafArea; //not sure what this is doing as actual growth may adjust this
				
				culm.DltLAI = culm.LeafArea * culm.Proportion;
				//dltCulmArea += culm.DltLAI;

                double leafAreaNow = calcCulmArea(leafNoEffective - culm.dltLeafNo,culm);
                culm.LeafArea = calcCulmArea(leafNoEffective, culm);
                double dltLeafArea = Math.Max(culm.LeafArea - leafAreaNow, 0.0);
                culm.DltLAI = dltLeafArea.ConvertSqM2SqMM() * sowingDensity  * culm.Proportion;
                dltCulmArea += culm.DltLAI;
                culm.TotalLAI += culm.DltLAI;
            }

            return dltCulmArea;
		}
        double calcCulmArea(double nLeaves, Culm culm)
        {
            // Sum the area of each leaf plus the fraction of the last.
            double area = 0;
            for (int i = 0; i < Math.Ceiling(nLeaves) && Math.Ceiling(nLeaves) < culm.LeafSizes.Count; i++)
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
            // use finalLeafNo to calculate the size of the individual leafs
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
            //double leafPlateauStart = 24;
            //adding new code to handle varieties that grow very high number of leaves
            //double a0 = -0.009, a1 = -0.2;
            //double b0 = 0.0006, b1 = -0.43;

            // Bell shaped curve characteristics.
            var a = a0.Value() + (a1.Value() / (1 + a2.Value() * culm.FinalLeafNo));
            var b = b0.Value() + (b1.Value() / (1 + b2.Value() * culm.FinalLeafNo));

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
			double leafSize =
                culm.AreaOfLargestLeaf * 
					Math.Exp(a * Math.Pow((leafNo - culm.PositionOfLargestLeaf), 2) + 
					b * Math.Pow((leafNo - culm.PositionOfLargestLeaf), 3)) * 
					100;
			
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