using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Models.PMF.Struct
{
    /// <summary>
    /// This is a tillering method to control the number of tillers and leaf area
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LeafCulms))]
    public class DynamicTillering : Model, ITilleringMethod
    {
		/// <summary>The parent Plant</summary>
		[Link]
		Plant plant = null;

		/// <summary> Culms on the leaf </summary>
		[Link] 
		public LeafCulms culms = null;

		/// <summary>The parent tilering class</summary>
		[Link]
		Phenology phenology = null;

		/// <summary>The parent tilering class</summary>
		[Link]
		SorghumLeaf leaf = null;

		/// <summary>The met data</summary>
		[Link]
		private IWeather metData = null;

		/// <summary> Culms on the leaf </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction areaCalc = null;

		/// <summary> Propoensity to Tiller Intercept </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction tillerSdIntercept = null;

		/// <summary> Propsenity to Tiller Slope </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction tillerSdSlope = null;

		/// <summary> LAI Value where tillers are no longer added </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction maxLAIForTillerAddition = null;

		/// <summary> LAI Value where tillers are no longer added </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction MaxDailyTillerReduction = null;
		
		/// <summary> LAI Value where tillers are no longer added </summary>
		[Link(Type = LinkType.Child, ByName = true)]
        IFunction tillerSlaBound = null;

		/// <summary> Culms on the leaf </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction slaMax = null;

        /// <summary>Number of potential Fertile Tillers at harvest</summary>
        [JsonIgnore]
        public double CalculatedTillerNumber { get; private set; }
        /// <summary>Current Number of Tillers</summary>
        [JsonIgnore]
        public double CurrentTillerNumber { get; set; }
        /// <summary>Current Number of Tillers</summary>
        [JsonIgnore]
        public double DltTillerNumber { get; set; }

        /// <summary>Actual Number of Fertile Tillers</summary>
        [JsonIgnore]
        public double FertileTillerNumber 
		{ 
			get => CurrentTillerNumber;
			set { throw new Exception("Cannot set the FertileTillerNumber for Dynamic Tillering. Make sure you set TilleringMethod before FertileTillerNmber"); }
		}

        /// <summary>Supply Demand Ratio used to calculate Tiller No</summary>
        [JsonIgnore]
        public double SupplyDemandRatio { get; private set; }

		private int flagStage;
		private int floweringStage;
		private int endJuvenilePhase;
		//private double tillersAdded;
		private int startThermalQuotientLeafNo = 3;
		private int endThermalQuotientLeafNo = 5;
		private double plantsPerMetre;
		private double linearLAI;

		private List<double> radiationAverages = new List<double>();

		private bool beforeFlagLeaf()
		{
			if (flagStage < 1) flagStage = phenology.EndStagePhaseIndex("FlagLeaf");
			return phenology.BeforePhase(flagStage);
		}

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

			var existingLeafNo = (int)Math.Floor(culms.Culms[0].CurrentLeafNo);
			double dltLeafNoMainCulm = 0.0;
			if (beforeEndJuvenileStage())
			{
				//ThermalTime Targets to EndJuv are not known until the end of the Juvenile Phase
				//FinalLeafNo is not known until the TT Target is known - meaning the potential leaf sizes aren't known
				culms.Culms.ForEach(c => c.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea));
			}

			dltLeafNoMainCulm = calcLeafAppearance(culms.Culms[0]);
			culms.dltLeafNo = dltLeafNoMainCulm;
			var newLeafNo = (int)Math.Floor(culms.Culms[0].CurrentLeafNo);

			if (CalculatedTillerNumber <= 0.0)
			{
				CalculatedTillerNumber = calcTillerNumber(newLeafNo, existingLeafNo);
				AddInitialTillers();
			}
			var fractionToAdd = calcTillerAppearance(newLeafNo, existingLeafNo);
			AddTillerProportion(fractionToAdd);


			for (int i = 1; i < culms.Culms.Count; i++)
			{
				calcLeafAppearance(culms.Culms[i]);
			}
			return dltLeafNoMainCulm;
		}

		private double calcLeafAppearance(Culm culm)
		{
			var leavesRemaining = culms.FinalLeafNo - culm.CurrentLeafNo;
			var leafAppearanceRate = culms.getLeafAppearanceRate(leavesRemaining);
			// if leaves are still growing, the cumulative number of phyllochrons or fully expanded leaves is calculated from thermal time for the day.
			var dltLeafNo = MathUtilities.Bound(MathUtilities.Divide(phenology.thermalTime.Value(), leafAppearanceRate, 0), 0.0, leavesRemaining);

			culm.AddNewLeaf(dltLeafNo);

			return dltLeafNo;
		}

		double calcTillerNumber(int newLeafNo, int existingLeafNo)
		{
			if (CalculatedTillerNumber > 0.0) return CalculatedTillerNumber;

			//the final tiller number (Ftn) is calculated after the full appearance of LeafNo 5 - when leaf 6 emerges.
			if (newLeafNo == endThermalQuotientLeafNo && existingLeafNo < endThermalQuotientLeafNo)
			{
				//Calc Supply = R/oCd * LA5 * Phy5
				var areaMethod = areaCalc as ICulmLeafArea;
				double L5Area = areaMethod.CalculateIndividualLeafArea(5, culms.FinalLeafNo);
				double L9Area = areaMethod.CalculateIndividualLeafArea(9, culms.FinalLeafNo);
				double Phy5 = culms.getLeafAppearanceRate(culms.FinalLeafNo - culms.Culms[0].CurrentLeafNo);

				//Calc Demand = LA9 - LA5
				var demand = L9Area - L5Area;
				var supply = radiationAverages.Average() * L5Area * Phy5;

                SupplyDemandRatio = MathUtilities.Divide(supply, demand, 0);

				return Math.Max(tillerSdIntercept.Value() + tillerSdSlope.Value() * SupplyDemandRatio, 0.0);
			}

			//need to calculate the average R/oCd per day during leaf 5 expansion
			if (newLeafNo == startThermalQuotientLeafNo)
			{
				double avgradn = metData.Radn / phenology.thermalTime.Value();
				radiationAverages.Add(avgradn);
			}
			return 0.0;
		}

		void AddInitialTillers()
        {
			if (CalculatedTillerNumber <= 0) return;

			if (CalculatedTillerNumber > 3)  //initiate T2:2 & T3:1
			{
				InitiateTiller(2, 1, 2);
                CurrentTillerNumber = 1;       // Reporting. 
			}

		}
		double calcTillerAppearance(int newLeafNo, int currentLeafNo)
		{
			//if there are still more tillers to add
			//and the newleaf is greater than 3
			// get number of tillers added so far

			if (CurrentTillerNumber >= CalculatedTillerNumber) return 0.0;
			// calculate linear LAI - plantsPerMeter is calculated at sowing
			//tpla is LAI/density - remove x density from plantspermetre calc?
			linearLAI = plantsPerMetre * (leaf.LAI + leaf.SenescedLai) / 10000.0;

			if (linearLAI < maxLAIForTillerAddition.Value())
			{
				var appRate = culms.getLeafAppearanceRate(5);
				return Math.Min(phenology.thermalTime.Value() / appRate, CalculatedTillerNumber - CurrentTillerNumber);
			}
			return 0.0;
		}

		/// <summary>
		/// Add a tiller.
		/// </summary>
		void InitiateTiller(int tillerNumber, double fractionToAdd, double initialLeaf)
        {
			double leafNoAtAppearance = 1.0;                            // DEBUG  parameter?
			double nTillersPresent = culms.Culms.Count - 1;

			Culm newCulm = new Culm(leafNoAtAppearance);

			newCulm.CulmNo = tillerNumber;
			newCulm.CurrentLeafNo = initialLeaf;
			newCulm.VertAdjValue = culms.MaxVerticalTillerAdjustment.Value() + (CurrentTillerNumber * culms.VerticalTillerAdjustment.Value());
			newCulm.Proportion = fractionToAdd;
			newCulm.FinalLeafNo = culms.Culms[0].FinalLeafNo;
			//newCulm.calcLeafAppearance();
			newCulm.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea);
			//newCulm.calculateLeafSizes();
			culms.Culms.Add(newCulm);
		}

		/// <summary>
		/// Add a tiller.
		/// </summary>
		/// <param name="fractionToAdd"></param>
		private void AddTillerProportion(double fractionToAdd)
		{
			//Add a fraction of a tiller every day.
			var lastCulm = culms.Culms.Last();
			double currentTillerFraction = lastCulm.Proportion;

			var tillerFraction = currentTillerFraction + fractionToAdd;
			lastCulm.Proportion = Math.Min(1.0, tillerFraction);

			if (tillerFraction > 1)
			{
				InitiateTiller(lastCulm.CulmNo + 1, tillerFraction - 1.0, 1);
			}
            DltTillerNumber = fractionToAdd;
            CurrentTillerNumber += fractionToAdd;
		}

		/// <summary> calculate the potential leaf area</summary>
		public double CalcPotentialLeafArea()
        {
			if (beforeFlowering())
				return areaCalc.Value();
			return 0.0;
		}

		/// <summary> calculate the actual leaf area</summary>
		public double CalcActualLeafArea(double dltStressedLAI)
		{
			//check current stage and current leaf number
			if (beforeEndJuvenileStage() || !beforeFlagLeaf())
			{
				culms.Culms.ForEach(c => c.TotalLAI = c.TotalLAI + c.DltStressedLAI);
				return dltStressedLAI;
			}

			double laiReductionForSLA = calcLeafReductionForCarbonLimitation(dltStressedLAI);
			var currentSLA = calcCurrentSLA(dltStressedLAI - laiReductionForSLA);

			var tillerLaiToReduce = calcCeaseTillerSignal(dltStressedLAI - laiReductionForSLA);

			bool moreToAdd = (CurrentTillerNumber < CalculatedTillerNumber) && (linearLAI < maxLAIForTillerAddition.Value());
			double nLeaves = culms.Culms.First().CurrentLeafNo;

			if (nLeaves > 7 && !moreToAdd && tillerLaiToReduce > 0.0)
			{
				//double maxTillerLoss = 0.3;             // externalise as parameter
				double accProportion = 0.0;
				double tillerLaiLeftToReduce = tillerLaiToReduce;

				for (int i = culms.Culms.Count() - 1; i >= 1; i--)
				{
					var culm = culms.Culms[i];
					if (accProportion < MaxDailyTillerReduction.Value() && tillerLaiLeftToReduce > 0)
					{
						double tillerArea = culm.TotalLAI + culm.DltStressedLAI;
						double tillerProportion = culm.Proportion;
						if (tillerProportion > 0.0 && tillerArea > 0.0)
						{
							//use the amount of LAI past the target as an indicator of how much of the tiller
							//to remove which will affect tomorrow's growth - up to the maxTillerLoss
							double propn = Math.Max(0.0, Math.Min(MaxDailyTillerReduction.Value() - accProportion, tillerLaiLeftToReduce / tillerArea));
							accProportion += propn;
							tillerLaiLeftToReduce -= propn * tillerArea;
							double remainingProportion = Math.Max(0.0, culm.Proportion - propn);
							culm.Proportion = remainingProportion; //can't increase the proportion

							//if leaf is over sla hard limit, remove as much of the new growth from this tiller first rather than proportionally across all
							double amountToRemove = Math.Min(laiReductionForSLA, culm.DltStressedLAI);
							culm.DltStressedLAI = culm.DltStressedLAI - amountToRemove;
							laiReductionForSLA -= amountToRemove;
						}
					}
				}
			}

			reduceAllTillersProportionately(laiReductionForSLA);
			culms.Culms.ForEach(c => c.TotalLAI = c.TotalLAI + c.DltStressedLAI);

			return dltStressedLAI - laiReductionForSLA;
		}

		private double calcLeafReductionForCarbonLimitation(double dltStressedLAI)
		{
			double dltDmGreen = leaf.potentialDMAllocation.Structural;
			if (dltDmGreen <= 0.0) return dltStressedLAI;
			//new growth should exceed the SLA limits - cannot grow too quickly so that new leaf is too thin
			return Math.Max(dltStressedLAI - (dltDmGreen * slaMax.Value().ConvertSqM2SqMM()), 0.0);
		}

		/// <summary>
		/// Calculate SLA for leafa rea including potential new growth - stressess effect
		/// </summary>
		/// <param name="stressedLAI"></param>
		/// <returns></returns>
		public double calcCurrentSLA(double stressedLAI)
		{
			double dmGreen = leaf.Live.Wt;
			double dltDmGreen = leaf.potentialDMAllocation.Structural;

			if (dmGreen + dltDmGreen <= 0.0) return 0.0;

			return (leaf.LAI + stressedLAI) / (dmGreen + dltDmGreen) * 10000; // (cm^2/g)
		}

		private double calcCeaseTillerSignal(double dltStressedLAI)
		{
			// calculate sla target that is below the actual SLA - so as the leaves gets thinner it signals to the tillers to cease growing further
			// max SLA (thinnest leaf) possible using Reeves (1960's Kansas) SLA = 429.72 - 18.158 * LeafNo
			var mainCulm = culms.Culms.First();
			double maxSLA = 429.72 - 18.158 * (mainCulm.CurrentLeafNo + mainCulm.dltLeafNo);
			maxSLA *= ((100 - tillerSlaBound.Value()) / 100.0);     // sla bound vary 30 - 40%
			maxSLA = Math.Min(400, maxSLA);
			maxSLA = Math.Max(150, maxSLA);

			//calc how much LAI we need to remove to get back to the SLA target line
			//this value will be limited by the proportion of tiller area in maxTillerLoss 
			//dltStressedLai can be greater than the actual SLA limitation would allow
			//provides a stronger signal
			double dmGreen = leaf.Live.Wt;
			double dltDmGreen = leaf.potentialDMAllocation.Structural;

			var maxLaiTarget = maxSLA * (dmGreen + dltDmGreen) / 10000;
			return Math.Max(leaf.LAI + dltStressedLAI - maxLaiTarget, 0);
		}

		void reduceAllTillersProportionately(double laiReduction)
		{
			if (laiReduction <= 0.0) return;

			double totalDltLeaf = culms.Culms.Sum(c => c.DltStressedLAI);
			if (totalDltLeaf <= 0.0) return;

			//reduce new leaf growth proportionally across all culms
			//not reducing the number of tillers at this stage
			culms.Culms.ForEach(c => c.DltStressedLAI = c.DltStressedLAI - Math.Max(c.DltStressedLAI / totalDltLeaf * laiReduction, 0.0));
		}

        /// <summary> Reset Culms at start of the simulation </summary>
        [EventSubscribe("StartOfSimulation")]
        private void StartOfSim(object sender, EventArgs e)
        {
            CurrentTillerNumber = 0.0;
			CalculatedTillerNumber = 0.0;
			DltTillerNumber = 0.0;
			SupplyDemandRatio = 0.0;
        }

        /// <summary>Called when crop is sowed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
		protected void OnPlantSowing(object sender, SowingParameters data)
		{
			if (data.Plant == plant && data.TilleringMethod == 1)
			{
				radiationAverages = new List<double>();
				plantsPerMetre = data.RowSpacing / 1000.0 * data.SkipDensityScale;
                //plantsPerMetre = data.Population * data.RowSpacing / 1000.0 * data.SkipDensityScale;
                CurrentTillerNumber = 0.0;
				CalculatedTillerNumber = 0.0;
			}
		}
	}
}
