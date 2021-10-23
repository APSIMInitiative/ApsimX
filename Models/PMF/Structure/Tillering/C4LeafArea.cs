using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Struct;
using Newtonsoft.Json;
using PdfSharpCore.Pdf.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.PMF
{
	/// <summary>
	/// This is the basic organ class that contains biomass structures and transfers
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.PropertyView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(LeafCulms))]
	public class C4LeafArea : Model, ICulmLeafArea
	{
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
		IFunction aMaxS = null;

		/// <summary>Senescence Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction aMaxI = null;

		/// <summary>Senescence Calculation</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction largestLeafPlateau = null;

		/// <summary>Calculate potential LeafArea</summary>
		public double CalculateIndividualLeafArea(double leafNo, double finalLeafNo, double vertAdjust = 0.0)
		{
			// use finalLeafNo to calculate the size of the individual leafs
			// Eqn 5 from Improved methods for predicting individual leaf area and leaf senescence in maize
			// (Zea mays) C.J. Birch, G.L. Hammer and K.G. Ricket. Aust. J Agric. Res., 1998, 49, 249-62
			//
			double largestLeafPos = aX0.Value() * finalLeafNo;
			leafNo = adjustLeafNumberForPlateuEffect(leafNo, finalLeafNo, largestLeafPlateau.Value());
			//double leafPlateauStart = 24;
			//adding new code to handle varieties that grow very high number of leaves
			//double a0 = -0.009, a1 = -0.2;
			//double b0 = 0.0006, b1 = -0.43;

			double a = a0.Value() - Math.Exp(a1.Value() * finalLeafNo);
			double b = b0.Value() - Math.Exp(b1.Value() * finalLeafNo);

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
			double largestLeafSize = aMaxS.Value() * finalLeafNo + aMaxI.Value(); //aMaxI is the intercept

			//a vertical adjustment is applied to each tiller - this was discussed in a meeting on 22/08/12 and derived 
			//from a set of graphs in Tonge's paper
			//the effect is to decrease the size of the largest leaf by 10% for each subsequent tiller 
			largestLeafSize *= (1 - vertAdjust);
			double leafSize = largestLeafSize * Math.Exp(a * Math.Pow((leafNo - largestLeafPos), 2) + b * Math.Pow((leafNo - largestLeafPos), 3)) * 100;
			return leafSize;
		}

		private double adjustLeafNumberForPlateuEffect(double leafNo, double finalLeafNo, double largestLeafPlateau)
		{
			if (largestLeafPlateau <= 0) return leafNo;
			if (finalLeafNo < largestLeafPlateau) return leafNo; //so it doesn't get in an error state

			var largestLeafPos = aX0.Value() * largestLeafPlateau;
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
	}
}
