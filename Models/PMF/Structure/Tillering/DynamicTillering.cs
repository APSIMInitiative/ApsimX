using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
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

		///// <summary> Maximum SLA </summary>
		//[Link(Type = LinkType.Child, ByName = true)]
		//IFunction slaMax = null;

		/// <summary> Propoensity to Tiller</summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction propensityToTiller = null;

		/// <summary> Propoensity to Tiller Intercept </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction tillerSdIntercept = null;

		/// <summary> Propsenity to Tiller Slope </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction tillerSdSlope = null;

		/// <summary> LAI Value where tillers are no longer added </summary>
		[Link(Type = LinkType.Child, ByName = true)]
		IFunction maxLAIForTillerAddition = null;

		///// <summary> LAI Value where tillers are no longer added </summary>
		//[Link(Type = LinkType.Child, ByName = true)]
		//IFunction tillerSlaBound = null;
		
		/// <summary>Number of Fertile Tillers at Harvest</summary>
		public double FertileTillerNumber { get; private set; }

		/// <summary>Supply Demand Ratio used to calculate Tiller No</summary>
		public double SupplyDemandRatio { get; private set; }

		private int floweringStage;
		private int endJuvenilePhase;
		private double tillersAdded;
		private int startThermalQuotientLeafNo = 3;
		private int endThermalQuotientLeafNo = 5;
		private double plantsPerMetre;
		private double linearLAI;

		private List<double> radiationAverages = new List<double>();

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

			var currentLeafNo = culms.Culms[0].CurrentLeafNo;
			double dltLeafNoMainCulm = 0.0;
			if (beforeEndJuvenileStage())
			{
				//ThermalTime Targets to EndJuv are not known until the end of the Juvenile Phase
				//FinalLeafNo is not known until the TT Target is known - meaning the potential leaf sizes aren't known
				culms.Culms.ForEach(c => c.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea));
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

		private double calcLeafAppearance(Culm culm)
		{
			var leavesRemaining = culms.FinalLeafNo - culm.CurrentLeafNo;
			var leafAppearanceRate = culms.LeafAppearanceRate.ValueForX(leavesRemaining);
			// if leaves are still growing, the cumulative number of phyllochrons or fully expanded leaves is calculated from thermal time for the day.
			var dltLeafNo = MathUtilities.Bound(MathUtilities.Divide(phenology.thermalTime.Value(), leafAppearanceRate, 0), 0.0, leavesRemaining);

			culm.AddNewLeaf(dltLeafNo);

			return dltLeafNo;
		}
		void calcTillerNumber(int newLeafNo, int currentLeafNo)
		{
			//need to calculate the average R/oCd per day during leaf 5 expansion
			if (newLeafNo == startThermalQuotientLeafNo)
			{
				double avgradn = metData.Radn / phenology.thermalTime.Value();
				radiationAverages.Add(avgradn);
			}
			//
			if (newLeafNo == endThermalQuotientLeafNo && currentLeafNo < endThermalQuotientLeafNo)
			{
				//the final tiller number (Ftn) is calculated after the full appearance of LeafNo 5 - when leaf 6 emerges.
				//Calc Supply = R/oCd * LA5 * Phy5
				var areaMethod = areaCalc as ICulmLeafArea;
				double L5Area = areaMethod.CalculateIndividualLeafArea(5, culms.FinalLeafNo);
				double L9Area = areaMethod.CalculateIndividualLeafArea(9, culms.FinalLeafNo);
				double Phy5 = culms.Culms[0].getLeafAppearanceRate(culms.FinalLeafNo - culms.Culms[0].CurrentLeafNo);

				//Calc Demand = LA9 - LA5
				var demand = L9Area - L5Area;
				var supply = radiationAverages.Average() * L5Area * Phy5;

				SupplyDemandRatio = supply / demand;
				FertileTillerNumber = propensityToTiller.Value();
				var calculatedTillers = tillerSdIntercept.Value() + tillerSdSlope.Value() * SupplyDemandRatio;
				FertileTillerNumber = Math.Max(FertileTillerNumber, 0.0);

				//char msg[120];
				//sprintf(msg, "Calculated Tiller Number = %.3f\n", calculatedTillers);
				//scienceAPI.write(msg);
				//sprintf(msg, "Calculated Supply = %.3f\n", supply);
				//scienceAPI.write(msg);
				//sprintf(msg, "Calculated Demand = %.3f\n", demand);
				//scienceAPI.write(msg);

				AddInitialTillers();
			}
		}

		void AddInitialTillers()
        {
			if (FertileTillerNumber <= 0) return;

			if (FertileTillerNumber > 3)  //initiate T2:2 & T3:1
			{
				InitiateTiller(2, 1, 2);
				tillersAdded = 1;       // Reporting. 
			}

		}
		void calcTillerAppearance(int newLeafNo, int currentLeafNo)
		{
			//if there are still more tillers to add
			//and the newleaf is greater than 3
			// get number of tillers added so far

			if (tillersAdded >= FertileTillerNumber) return;
			// calculate linear LAI - plantsPerMeter is calculated at sowing
			//tpla is LAI/density - remove x density from plantspermetre calc?
			linearLAI = plantsPerMetre * (leaf.LAI + leaf.SenescedLai) / 10000.0;

			if (linearLAI < maxLAIForTillerAddition.Value())
			{
				var appRate = culms.Culms[0].getLeafAppearanceRate(5);
				double fractionToAdd = Math.Min(phenology.thermalTime.Value() / appRate, FertileTillerNumber - tillersAdded);
				AddTillerProportion(1, fractionToAdd);
				tillersAdded += fractionToAdd;

				//AddTiller(leafAppearance, currentLeafNo, fraction);
			}
		}

		/// <summary>
		/// Add a tiller.
		/// </summary>
		void InitiateTiller(int tillerNumber, double fractionToAdd, double initialLeaf)
        {
			double leafNoAtAppearance = 1.0;                            // DEBUG  parameter?
			double nTillersPresent = culms.Culms.Count - 1;

			Culm newCulm = new Culm(leafNoAtAppearance, culms.Culms[0].parameters);

			newCulm.CulmNo = tillerNumber;
			newCulm.CurrentLeafNo = initialLeaf;
			newCulm.VertAdjValue = culms.MaxVerticalTillerAdjustment.Value() + (tillersAdded * culms.VerticalTillerAdjustment.Value());
			newCulm.Proportion = fractionToAdd;
			newCulm.FinalLeafNo = culms.Culms[0].FinalLeafNo;
			//newCulm.calcLeafAppearance();
			newCulm.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea);
			calcLeafAppearance(newCulm);
			//newCulm.calculateLeafSizes();
			culms.Culms.Add(newCulm);
		}

		/// <summary>
		/// Add a tiller.
		/// </summary>
		/// <param name="leafAtAppearance"></param>
		/// <param name="fractionToAdd"></param>
		private void AddTillerProportion(double leafAtAppearance, double fractionToAdd)
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

			tillersAdded += fractionToAdd;
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
			return 0.0;
			//// Calculate todays SLA and LAI.
			//double todaysBiomass = dmGreen + dltDmGreen;
			//var todaysLAI = culms.Culms.Sum(c => c.TotalLAI + c.Proportion);

			//var SLA = MathUtilities.Divide(todaysLAI,todaysBiomass,0) * 10000;    // (cm^2/g)

			//// Calculate maximum possible SLA (thinnest leaf)
			//// Max SLA possible using Reeves (1960's Kansas) SLA = 429.72 - 18.158 * LeafNo.
			////maxSLA = 429.72 - 18.158 * (nLeaves + dltLeafNo);
			//var maxSLA = 429.72 - 18.158 * (culms.Culms[0].CurrentLeafNo);
			//maxSLA *= ((100 - tillerSlaBound.Value()) / 100.0);     // sla bound vary 30 - 40%
			//maxSLA = Math.Min(400, maxSLA);
			//maxSLA = Math.Max(150, maxSLA);

			//// If there is not enough biomass to support dltStressedLAI at slaMax, reduce todays lai contribution from each culm.
			//double reductionFactor = 1.0;
			//if (stage >= endJuv && stage < flag)
			//{
			//	reductionFactor = Math.Min((dltDmGreen * slaMax).ConvertSqM2SqMM() / dltStressedLAI, 1.0);
			//}
			//for (int i = 0; i < (int)Culms.size(); ++i)
			//{
			//	culms.Culms[i].calcDltLAI(reductionFactor);
			//}
			//dltLAI = dltStressedLAI * reductionFactor;

			//// Reduce tillers if SLA is above maxSLA and conditions are right.

			//// Don't reduce tillers if we are still adding them.
			//bool moreTillersToAdd = (tillersAdded < FertileTillerNumber) && (linearLAI < maxLAIForTillerAddition);
			//double nLeaves = culms.Culms[0].CurrentLeafNo;

			//// See if there are any active tillers to reduce!
			//int lastActiveTiller = 0;
			//for (int i = 1; i < Culms.size(); i++)
			//{
			//	if (Culms[i]->getProportion() > 0.0)
			//		lastActiveTiller++;
			//}

			//if (!moreTillersToAdd && stage <= flag && lastActiveTiller > 0)
			////		if (!moreTillersToAdd && Culms[lastActiveTiller]->getCurrentLeafNo() < 6 && lastActiveTiller > 0)
			//{

			//	// Calculate todays SLA and LAI.
			//	double todaysBiomass = dmGreen + dltDmGreen;
			//	double todaysLAI = 0;
			//	for (int i = 0; i < Culms.size(); i++)
			//	{
			//		todaysLAI += Culms[i]->getTotalLAI() * Culms[i]->getProportion();
			//	}
			//	if (todaysBiomass > 0.0)
			//		SLA = todaysLAI / todaysBiomass * 10000;    // (cm^2/g)

			//	// Reduce tillers if SLA > maxSLA
			//	if (SLA > maxSLA)
			//	{
			//		// How much LAI can maxSLA support?
			//		double maxSlaLai = maxSLA * todaysBiomass / 10000;
			//		double laiReduction = todaysLAI - maxSlaLai;

			//		// Achieve this maxSlaLai by reducing the proportion of tillers, starting at the last tiller.
			//		// What proportion of the last active tiller (lat) is the laiReduction?
			//		// Limit the decrease in tillering to 0.3 tillers per day.
			//		double latPropn = Min(laiReduction / Culms[lastActiveTiller]->getTotalLAI(), 0.3);
			//		double newLatProporn = Max(Culms[lastActiveTiller]->getProportion() - latPropn, 0);

			//		Culms[lastActiveTiller]->setProportion(newLatProporn);
			//	}
			//}   
			

		}

		/// <summary>Called when crop is sowed</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("PlantSowing")]
		protected void OnPlantSowing(object sender, SowingParameters data)
		{
			if (data.Plant == plant)
			{
				radiationAverages = new List<double>();
				plantsPerMetre = data.RowSpacing / 1000.0 * data.SkipDensityScale;
				//plantsPerMetre = data.Population * data.RowSpacing / 1000.0 * data.SkipDensityScale;

				FertileTillerNumber = data.BudNumber;
				tillersAdded = 0.0;
			}
		}
	}
}
