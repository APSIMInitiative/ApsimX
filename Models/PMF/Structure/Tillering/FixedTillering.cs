using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.Utilities;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Models.PMF.Struct
{
    /// <summary>
    /// This is a tillering method to control the number of tillers and leaf area
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LeafCulms))]
    public class FixedTillering : Model, ITilleringMethod
    {
		/// <summary>The parent Plant</summary>
		[Link]
		Plant plant = null;

		/// <summary>
        /// Link to clock (used for FTN calculations at time of sowing).
        /// </summary>
        [Link]
		private IClock clock = null;

		/// <summary>
		/// Link to weather.
		/// </summary>
		[Link]
		private IWeather weather = null;

		/// <summary> Culms on the leaf </summary>
		[Link]
		LeafCulms culms = null;

		/// <summary> Leaf organ</summary>
		[Link]
		SorghumLeaf leaf = null;

		/// <summary> Culms on the leaf </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction areaCalc = null;

		/// <summary> Culms on the leaf </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction slaMax = null;

		/// <summary>The parent tilering class</summary>
		[Link]
		Phenology phenology = null;

        /// <summary>Number of Fertile Tillers at Harvest</summary>
        [JsonIgnore]
        public double FertileTillerNumber { get; set; }

        /// <summary>Current Number of Tillers</summary>
        [JsonIgnore]
        public double CurrentTillerNumber { get; set; }

		/// <summary>Current Number of Tillers</summary>
		[JsonIgnore]
		public double CalculatedTillerNumber { get; set; } = 0.0;

		/// <summary>Maximum SLA for tiller cessation</summary>
		[JsonIgnore]
		public double MaxSLA { get; set; } = 0.0;

        private int floweringStage;
		private int endJuvenilePhase;
		private int dayOfClassicsEmergence = -1;

        private bool beforeFlowering()
{
			if (floweringStage < 1) floweringStage = phenology.EndStagePhaseIndex("Flowering");
			return phenology.BeforePhase(floweringStage);
		}
		private bool beforeEndJuvenileStage()
		{
			if (endJuvenilePhase < 1) endJuvenilePhase = phenology.StartStagePhaseIndex("EndJuvenile");
			return phenology.BeforePhase(endJuvenilePhase);
		}

		/// <summary> Calculate number of leaves</summary>
		public double CalcLeafNumber()
        {
			if (culms.Culms?.Count == 0) return 0.0;
			if (!plant.IsEmerged) return 0.0;
			if (dayOfClassicsEmergence == -1)
			{
				//classic had a delay due to emergence being set to a whole number (3) at emergence
				//this created a slight offset where the calculated day of emergence is 1 day earlier in NextGen
				//In Classic, the leaf calc would then start at 1 on the day after emergence - which creates a 2 day difference that becomes significant
				//so leave the leafNo at 1 on the day after emergence
				dayOfClassicsEmergence = 0; //When it runs tomorrow it will be the day of ermergence
				return 1.0;
			}

			var currentLeafNo = culms.Culms[0].CurrentLeafNo;
			double dltLeafNoMainCulm = 0.0;
			if (beforeEndJuvenileStage())
			{
				//ThermalTime Targets to EndJuv are not known until the end of the Juvenile Phase
				//FinalLeafNo is not known until the TT Target is known - meaning the potential leaf sizes aren't known
				culms.Culms.ForEach(c => c.UpdatePotentialLeafSizes(culms.Culms[0], areaCalc as ICulmLeafArea));
			}

			dltLeafNoMainCulm = calcLeafAppearance(culms.Culms[0]);
			culms.dltLeafNo = dltLeafNoMainCulm;
			double newLeafNo = culms.Culms[0].CurrentLeafNo;

			//should there be any growth after flowering?
			calcTillerAppearance((int)Math.Floor(newLeafNo), (int)Math.Floor(currentLeafNo));

			for (int i = 1; i < culms.Culms.Count; i++)
			{
				calcLeafAppearance(culms.Culms[i]);
			}
			return dltLeafNoMainCulm;
        }

		/// <summary> Calculate the potential leaf area for the tillers</summary>
		public double CalcPotentialLeafArea()
        {
			//if(beforeFlowering())
				return areaCalc.Value();
			//return 0.0;
		}

		/// <summary> Calculate actual area - which is constrained by the SLA of the leaf</summary>
		public double CalcActualLeafArea(double dltStressedLAI)
		{
			if (beforeEndJuvenileStage()) return dltStressedLAI;

			double dltDmGreen = leaf.potentialDMAllocation.Structural;
			if (dltDmGreen <= 0.0) return dltStressedLAI;

			return Math.Min(dltStressedLAI, dltDmGreen * slaMax.Value().ConvertSqM2SqMM());
		}

		private double calcLeafAppearance(Culm culm)
		{
			var leavesRemaining = Math.Max(0.0, culms.FinalLeafNo - culm.LeafNoAtAppearance - culm.CurrentLeafNo);
			var leafAppearanceRate = culms.GetLeafAppearanceRate(leavesRemaining);
			// if leaves are still growing, the cumulative number of phyllochrons or fully expanded leaves is calculated from thermal time for the day.
			var dltLeafNo = MathUtilities.Bound(MathUtilities.Divide(phenology.thermalTime.Value(), leafAppearanceRate, 0), 0.0, leavesRemaining);

			// In sorghum, this is added to current leafno immediately. In
			// maize, this doesn't happen until end of day.
			culm.AddNewLeaf(dltLeafNo);

			return dltLeafNo;
		}

		void calcTillerAppearance(int newLeafNo, int currentLeafNo)
		{
			if (newLeafNo <= currentLeafNo) return;
			if (newLeafNo < 3) return; //don't add before leaf 3

			//if there are still more tillers to add and the newleaf is greater than 3
			if (CurrentTillerNumber >= FertileTillerNumber) return;

			//tiller emergence is more closely aligned with tip apearance, but we don't track tip, so will use ligule appearance
			//could also use Thermal Time calcs if needed
			//Environmental & Genotypic Control of Tillering in Sorghum ppt - Hae Koo Kim
			//T2=L3, T3=L4, T4=L5, T5=L6

			//logic to add new tillers depends on which tiller, which is defined by FTN (fertileTillerNo)
			//this should be provided at sowing
			//2 tillers = T3 + T4
			//3 tillers = T2 + T3 + T4
			//4 tillers = T2 + T3 + T4 + T5
			//more than that is too many tillers - but will assume existing pattern for 3 and 4
			//5 tillers = T2 + T3 + T4 + T5 + T6

			double leafAppearance = culms.Culms.Count + 2; //first culm added will equal 3
			double fraction = 1.0;

			if (FertileTillerNumber > 2 && FertileTillerNumber < 3 && leafAppearance < 4)
			{
				fraction = FertileTillerNumber % 1;
			}
			else
			{
				if (FertileTillerNumber - CurrentTillerNumber < 1)
					fraction = FertileTillerNumber - CurrentTillerNumber;
			}
			AddTiller(leafAppearance, currentLeafNo, fraction);
		}
		/// <summary>
		/// Add a tiller.
		/// </summary>
		/// <param name="leafAtAppearance"></param>
		/// <param name="Leaves"></param>
		/// <param name="fractionToAdd"></param>
		private void AddTiller(double leafAtAppearance, double Leaves, double fractionToAdd)
		{
			double fraction = 1;
			if (FertileTillerNumber - CurrentTillerNumber < 1)
				fraction = FertileTillerNumber - CurrentTillerNumber;

			// get number of tillers
			// add fractionToAdd
			// if new tiller is neded add one
			// fraction goes to proportions
			double tillerFraction = culms.Culms.Last().Proportion;
			//tillerFraction +=fractionToAdd;
			fraction = tillerFraction + fractionToAdd - Math.Floor(tillerFraction);
			//a new tiller is created with each new leaf, up the number of fertileTillers
			if (tillerFraction + fractionToAdd > 1)
			{
				Culm newCulm = new Culm(leafAtAppearance);

				//bell curve distribution is adjusted horizontally by moving the curve to the left.
				//This will cause the first leaf to have the same value as the nth leaf on the main culm.
				//T3&T4 were defined during dicussion at initial tillering meeting 27/06/12
				//all others are an assumption
				//T2 = 3 Leaves
				//T3 = 4 Leaves
				//T4 = 5 leaves
				//T5 = 6 leaves
				//T6 = 7 leaves
				newCulm.CulmNo = culms.Culms.Count;
				newCulm.CurrentLeafNo = newCulm.CulmNo + 2;

                newCulm.VertAdjValue = culms.MaxVerticalTillerAdjustment.Value() + (CurrentTillerNumber * culms.VerticalTillerAdjustment.Value());
				newCulm.Proportion = fraction;
				newCulm.FinalLeafNo = culms.Culms[0].FinalLeafNo;
				//newCulm.calcLeafAppearance();
				newCulm.UpdatePotentialLeafSizes(culms.Culms[0], areaCalc as ICulmLeafArea);
				calcLeafAppearance(newCulm);
				//newCulm.calculateLeafSizes();
				culms.Culms.Add(newCulm);
			}
			else
			{
				culms.Culms.Last().Proportion = fraction;
			}
            CurrentTillerNumber += fractionToAdd;
		}

        /// <summary> Reset Culms at start of the simulation </summary>
        [EventSubscribe("StartOfSimulation")]
        private void StartOfSim(object sender, EventArgs e)
        {
			FertileTillerNumber = 0.0;
			CurrentTillerNumber = 0.0;
        }

        /// <summary>Called when crop is sowed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
		protected void OnPlantSowing(object sender, SowingParameters data)
		{
			if (data.Plant == plant)
			{
				if (data.TilleringMethod == -1)
					FertileTillerNumber = CalculateFtn();
				else
					FertileTillerNumber = data.FTN;
                CurrentTillerNumber = 0.0;
                dayOfClassicsEmergence = -1;
            }
        }

		private double CalculateFtn()
		{
			// Estimate tillering given latitude, density, time of planting and
			// row configuration. this will be replaced with dynamic
			// calculations in the near future. Above latitude -25 is CQ, -25
			// to -29 is SQ, below is NNSW.
			double intercept, slope;

			if (weather.Latitude > -12.5 || weather.Latitude < -38.0)
				// Unknown region.
				throw new Exception("Unable to estimate number of tillers at latitude {weather.Latitude}");

			if (weather.Latitude > -25.0)
			{
				// Central Queensland.
				if (clock.Today.DayOfYear < 319 && clock.Today.DayOfYear > 182)
				{
					// Between 1 July and 15 November.
					if (plant.SowingData.SkipRow > 1.9)
					{
						// Double (2.0).
						intercept = 0.5786; slope = -0.0521;
					}
					else if (plant.SowingData.SkipRow > 1.4)
					{
						// Single (1.5).
						intercept = 0.8786; slope = -0.0696;
					}
					else
					{
						// Solid (1.0).
						intercept = 1.1786; slope = -0.0871;
					}
				}
				else
				{
					// After 15 November.
					if (plant.SowingData.SkipRow > 1.9)
					{
						// Double (2.0).
						intercept = 0.4786; slope = -0.0421;
					}
					else if (plant.SowingData.SkipRow > 1.4)
					{
						// Single (1.5)
						intercept = 0.6393; slope = -0.0486;
					}
					else
					{
						// Solid (1.0).
						intercept = 0.8000; slope = -0.0550;
					}
				}
			}
			else if (weather.Latitude > -29.0)
			{
				// South Queensland.
				if (clock.Today.DayOfYear < 319 && clock.Today.DayOfYear > 182)
				{
					// Between 1 July and 15 November.
					if (plant.SowingData.SkipRow > 1.9)
					{
						// Double  (2.0).
						intercept = 1.1571; slope = -0.1043;
					}
					else if (plant.SowingData.SkipRow > 1.4)
					{
						// Single (1.5).
						intercept = 1.7571; slope = -0.1393;
					}
					else
					{
						// Solid (1.0).
						intercept = 2.3571; slope = -0.1743;
					}
				}
				else
				{
					// After 15 November.
					if (plant.SowingData.SkipRow > 1.9)
					{
						// Double (2.0).
						intercept = 0.6786; slope = -0.0621;
					}
					else if (plant.SowingData.SkipRow > 1.4)
					{
						// Single (1.5).
						intercept = 1.1679; slope = -0.0957;
					}
					else
					{
						// Solid (1.0).
						intercept = 1.6571; slope = -0.1293;
					}
				}
			}
			else
			{
				// Northern NSW.
				if (clock.Today.DayOfYear < 319 && clock.Today.DayOfYear > 182)
				{
					//  Between 1 July and 15 November.
					if (plant.SowingData.SkipRow > 1.9)
					{
						// Double (2.0).
						intercept = 1.3571; slope = -0.1243;
					}
					else if (plant.SowingData.SkipRow > 1.4)
					{
						// Single (1.5).
						intercept = 2.2357; slope = -0.1814;
					}
					else
					{
						// Solid (1.0).
						intercept = 3.1143; slope = -0.2386;
					}
				}
				else if (clock.Today.DayOfYear > 349 || clock.Today.DayOfYear < 182)
				{
					// Between 15 December and 1 July.
					if (plant.SowingData.SkipRow > 1.9)
					{
						// Double (2.0).
						intercept = 0.4000; slope = -0.0400;
					}
					else if (plant.SowingData.SkipRow > 1.4)
					{
						// Single (1.5).
						intercept = 1.0571; slope = -0.0943;
					}
					else
					{
						// Solid (1.0).
						intercept = 1.7143; slope = -0.1486;
					}
				}
				else
				{
					// Between 15 November and 15 December.
					if (plant.SowingData.SkipRow > 1.9)
					{
						// Double (2.0).
						intercept = 0.8786; slope = -0.0821;
					}
					else if (plant.SowingData.SkipRow > 1.4)
					{
						// Single (1.5).
						intercept = 1.6464; slope = -0.1379;
					}
					else
					{
						// Solid (1.0).
						intercept = 2.4143; slope = -0.1936;
					}
				}
			}

			return Math.Max(slope * plant.SowingData.Population + intercept, 0);
		}
	}
}
