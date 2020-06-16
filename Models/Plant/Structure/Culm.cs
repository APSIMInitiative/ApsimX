using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.PMF.Struct
{
	/// <summary>
	/// 
	/// </summary>
	[Serializable]
    public class Culm
    {
		private const double smm2sm = 1e-6;

		private double leafNoAtAppearance;

		//Birch, Hammer bell shaped curve parameters

		private double largestLeafPlateau;
		private double dltLeafNo;

		//private double finalLeafCorrection;

		/// <summary>
		/// Vertical leaf adjustment.
		/// </summary>
		public double VertAdjValue { get; set; }

		//double noEmergence;
		//double initialTPLA;
		//double tplaInflectionRatio,tplaProductionCoef;
		// leaf appearance

		private CulmParams parameters;

		/// <summary>
		/// Culm number.
		/// </summary>
		public int CulmNo { get; set; }

		/// <summary>
		/// Leaf proportion?
		/// </summary>
		public double Proportion { get; set; }

		/// <summary>
		/// Final leaf number.
		/// </summary>
		public double FinalLeafNo { get; private set; }

		/// <summary>
		/// Current leaf number.
		/// </summary>
		public double CurrentLeafNo { get; set; }

		/// <summary>
		/// Leaf area.
		/// </summary>
		/// <remarks>
		/// Changes each day.
		/// </remarks>
		public double LeafArea { get; private set; }

		/// <summary>
		/// Accumulated lai for this culm.
		/// </summary>
		public double TotalLAI { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public List<double> LeafSizes { get; set; }

		// public Methods -------------------------------------------------------

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="leafAppearance"></param>
		/// <param name="parameters"></param>
		public Culm(double leafAppearance, CulmParams parameters)
		{
			//plant = p;
			leafNoAtAppearance = leafAppearance;
			this.parameters = parameters;
			Initialize();
			//doRegistrations();
		}

		/// <summary>
		/// Perform initialisation.
		/// </summary>
		public virtual void Initialize()
		{
			// leaf number
			FinalLeafNo = 0.0;
			dltLeafNo = 0.0;
			largestLeafPlateau = 0; //value less than 1 means ignore it
			CurrentLeafNo = parameters.LeafNoAtEmergence.Value();
			VertAdjValue = 0.0;
			Proportion = 1.0;
			TotalLAI = 0.0;
			CulmNo = 0;
			LeafSizes = new List<double>();
			//readParams();
		}

		/// <summary>
		/// TBI - but this may not be needed in apsimx.
		/// </summary>
		public virtual void ReadParams()
		{
			/*
			// leaf area individual leaf
			//Birch, Hammer bell shaped curve parameters
			scienceAPI.read("aX0", "", false, aX0);    // Eqn 14
													   //scienceAPI.read("aMaxA"       ,"", false, aMaxA); // Eqn 13
			scienceAPI.read("aMaxI", "", false, aMaxI);
			scienceAPI.read("aMaxS", "", false, aMaxS);
			scienceAPI.read("largestLeafPlateau", "", true, largestLeafPlateau);

			scienceAPI.read("leaf_no_correction", "", false, leafNoCorrection); //

			// leaf appearance rates
			scienceAPI.read("leaf_app_rate1", "", false, appearanceRate1);
			scienceAPI.read("leaf_app_rate2", "", false, appearanceRate2);
			scienceAPI.read("leaf_no_rate_change", "", false, noRateChange);

			scienceAPI.read("leaf_no_seed", "", false, noSeed);
			scienceAPI.read("leaf_init_rate", "", false, initRate);
			//scienceAPI.read("leaf_no_at_emerg", "", false, noEmergence);
			scienceAPI.read("leaf_no_min", "", false, minLeafNo);
			scienceAPI.read("leaf_no_max", "", false, maxLeafNo);

			density = plant->getPlantDensity();
			*/
		}

		/// <summary>
		/// Update Leaf state variables at the end of the day.
		/// </summary>
		public virtual void UpdateVars()
		{
			//currentLeafNo = currentLeafNo + dltLeafNo;
		}

		/// <summary>
		/// Calculate final leaf number.
		/// </summary>
		public void calcFinalLeafNo()
		{
			double initRate = parameters.InitRate.Value();
			double noSeed = parameters.NoSeed.Value();
			double minLeafNo = parameters.MinLeafNo.Value();
			double maxLeafNo = parameters.MaxLeafNo.Value();
			double ttFi = parameters.TTEmergToFI.Value();

			FinalLeafNo = MathUtilities.Bound(MathUtilities.Divide(ttFi, initRate, 0) + noSeed, minLeafNo, maxLeafNo);
		}

		/// <summary>
		/// Calculate leaf appearance. Called from LeafCulms::calcLeafNo().
		/// </summary>
		public double calcLeafAppearance()
		{
			dltLeafNo = 0.0;
			double remainingLeaves = FinalLeafNo - leafNoAtAppearance - CurrentLeafNo;//nLeaves is used in partitionDM, so need to retain it in Leaf
			if (remainingLeaves <= 0.0)
			{
				return 0.0;
			}

			// Peter's 2 stage version used here, modified to apply to last few leaves before flag
			// i.e. c_leaf_no_rate_change is leaf number from the top down (e.g. 4)
			// dh - todo - need to check this works with sorghum.
			double leafAppRate = parameters.AppearanceRate1.Value();
			if (remainingLeaves <= parameters.NoRateChange2.Value())
				leafAppRate = parameters.AppearanceRate3.Value();
			else if (remainingLeaves <= parameters.NoRateChange.Value())
				leafAppRate = parameters.AppearanceRate2.Value();
			else
				leafAppRate = parameters.AppearanceRate1.Value();

			// if leaves are still growing, the cumulative number of phyllochrons or fully expanded
			// leaves is calculated from thermal time for the day.
			dltLeafNo = MathUtilities.Bound(MathUtilities.Divide(parameters.DltTT.Value(), leafAppRate, 0), 0.0, remainingLeaves);

			CurrentLeafNo = CurrentLeafNo + dltLeafNo;
			return dltLeafNo;
		}

		/// <summary>
		/// Get leaf appearance rate.
		/// </summary>
		/// <param name="remainingLeaves"></param>
		/// <returns></returns>
		public double getLeafAppearanceRate(double remainingLeaves)
		{
			if (remainingLeaves <= parameters.NoRateChange.Value())
				return parameters.AppearanceRate2.Value();
			return parameters.AppearanceRate1.Value();
		}

		/// <summary>
		/// Calculate potential leaf area.
		/// </summary>
		/// <returns></returns>
		public double calcPotentialLeafArea()
		{
			//once leaf no is calculated leaf area of largest expanding leaf is determined
			double leafNoEffective = Math.Min(CurrentLeafNo + parameters.LeafNoCorrection.Value(), FinalLeafNo - leafNoAtAppearance);
			LeafArea = calcIndividualLeafSize(leafNoEffective);
			//leafArea = getAreaOfCurrentLeaf(leafNoEffective);		HACK
			//leafArea *= proportion; //proportion is 1 unless this tiller is a fraction ie: Fertile Tiller Number is 2.2, then 1 tiller is 0.2
			LeafArea = LeafArea * smm2sm * parameters.Density * dltLeafNo; // in dltLai
			TotalLAI += LeafArea;
			return (LeafArea * Proportion);
		}

		/// <summary>
		/// Note: this is using the sorghum code (not Maize!).
		/// </summary>
		/// <param name="leafNo"></param>
		public double calcIndividualLeafSize(double leafNo)
		{
			// use finalLeafNo to calculate the size of the individual leafs
			// Eqn 5 from Improved methods for predicting individual leaf area and leaf senescence in maize
			// (Zea mays) C.J. Birch, G.L. Hammer and K.G. Ricket. Aust. J Agric. Res., 1998, 49, 249-62
			//
			double correctedFinalLeafNo = FinalLeafNo;// - leafNoAtAppearance;
			double largestLeafPos = parameters.AX0.Value() * correctedFinalLeafNo; //aX0 = position of the final leaf
																//double leafPlateauStart = 24;
																//adding new code to handle varieties that grow very high number of leaves
			if (largestLeafPlateau > 1)
			{
				if (correctedFinalLeafNo > largestLeafPlateau)
				{
					largestLeafPos = parameters.AX0.Value() * largestLeafPlateau;

					if (leafNo > largestLeafPos)
					{
						double tailCount = largestLeafPlateau - largestLeafPos;
						if (leafNo < correctedFinalLeafNo - tailCount)
						{
							leafNo = largestLeafPos;
						}
						else
						{
							leafNo = largestLeafPlateau - (correctedFinalLeafNo - leafNo);
						}
					}
				}
			}
			double a0 = parameters.A0.Value();
			double a1 = parameters.A1.Value();
			double b0 = parameters.B0.Value();
			double b1 = parameters.B1.Value();

			double a = a0 - Math.Exp(a1 * correctedFinalLeafNo);
			double b = b0 - Math.Exp(b1 * correctedFinalLeafNo);

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
			double largestLeafSize = parameters.AMaxS.Value() * FinalLeafNo + parameters.AMaxI.Value(); //aMaxI is the intercept

			//a vertical adjustment is applied to each tiller - this was discussed in a meeting on 22/08/12 and derived 
			//from a set of graphs that I cant find that compared the curves of each tiller
			//the effect is to decrease the size of the largest leaf by 10% 
			largestLeafSize *= (1 - VertAdjValue);
			double leafSize = largestLeafSize * Math.Exp(a * Math.Pow((leafNo - largestLeafPos), 2) + b * Math.Pow((leafNo - largestLeafPos), 3)) * 100;
			return leafSize;
		}

		/// <summary>
		/// TBI - doesn't exist in old model.
		/// </summary>
		public void setFinalLeafCorrection(double finalLeafCorrection)
		{
			// Oddly, there is no implementation for this method in the old sorghum model.
			throw new NotImplementedException();
		}

		/// <summary>
		/// Calculate leaf sizes.
		/// </summary>
		public void calculateLeafSizes()
		{
			// calculate the leaf sizes for this culm
			LeafSizes.Clear();
			List<double> sizes = new List<double>();
			for (int i = 1; i < Math.Ceiling(FinalLeafNo) + 1; i++)
				sizes.Add(calcIndividualLeafSize(i));
			// offset for less leaves
			int offset = 0;
			if (CulmNo > 0)
				offset = 3 + CulmNo;
			for (int i = 0; i < Math.Ceiling(FinalLeafNo - (offset)); i++)
				LeafSizes.Add(sizes[i + offset]);
		}

		/// <summary>
		/// Calculate leaf sizes. Used when leafAreaCalcTypeSwitch is present.
		/// </summary>
		public void CalcLeafSizes()
		{
			LeafSizes.Clear();
			for (int i = 0; i < FinalLeafNo; i++)
				LeafSizes.Add(calcIndividualLeafSize2(i + 1));
		}

		/// <summary>
		/// Temp hack - fixme
		/// </summary>
		/// <param name="leafNo"></param>
		private double calcIndividualLeafSize2(double leafNo)
		{
			// use finalLeafNo to calculate the size of the individual leafs
			// Eqn 5 from Improved methods for predicting individual leaf area and leaf senescence in maize
			// (Zea mays) C.J. Birch, G.L. Hammer and K.G. Ricket. Aust. J Agric. Res., 1998, 49, 249-62
			// TODO	externalise these variables

			double a0 = parameters.A0.Value();
			double a1 = parameters.A1.Value();
			double b0 = parameters.B0.Value();
			double b1 = parameters.B1.Value();

			double aMaxA = parameters.AMaxA.Value();
			double aMaxB = parameters.AMaxB.Value();
			double aMaxC = parameters.AMaxC.Value();

			double a = a0 - Math.Exp(a1 * FinalLeafNo);                      // Eqn 18
			double b = b0 - Math.Exp(b1 * FinalLeafNo);                      // Eqn 19

			double aMax = aMaxA * Math.Exp(aMaxB + aMaxC * FinalLeafNo);         // Eqn 13

			double x0 = parameters.AX0.Value() * FinalLeafNo;                                          // Eqn 14

			return aMax * Math.Exp(a * Math.Pow((leafNo - x0), 2) + b * Math.Pow((leafNo - x0), 3)) * 100;  // Eqn 5
		}

		/// <summary>
		/// Calculate potential leaf area (used in Maize only - to be refactored out?).
		/// </summary>
		/// <param name="leafNo"></param>
		public double LeafAreaPotBellShapeCurve(double[] leafNo)
		{
			//once leaf no is calculated leaf area of largest expanding leaf is determined
			double leafNoEffective = Math.Min(leafNo.Sum() + parameters.LeafNoCorrection.Value(), FinalLeafNo);

			return dltLeafNo * calcIndividualLeafSize2(leafNoEffective) * smm2sm * parameters.Density;
		}

		/// <summary>
		/// Get area of current leaf.
		/// </summary>
		/// <param name="leafNo"></param>
		public double getAreaOfCurrentLeaf(double leafNo)
		{
			// interpolate leaf sizes to get area of this leaf
			// check upper
			if (leafNo > LeafSizes.Count)
				return LeafSizes.LastOrDefault();
			else
			{
				int leafIndx = (int)Math.Floor(leafNo) - 1;
				double leafPart = leafNo - Math.Floor(leafNo);
				double size = LeafSizes[leafIndx] + (LeafSizes[leafIndx + 1] - LeafSizes[leafIndx]) * leafPart;
				return size;
			}
		}
	}
}
