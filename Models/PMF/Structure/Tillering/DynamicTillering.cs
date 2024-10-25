using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Newtonsoft.Json;
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
        /// <summary>
        /// Used to log to the summary window.
        /// </summary>
        [Link]
        readonly ISummary summary = null;

        /// <summary>
        /// Link to clock (used for FTN calculations at time of sowing).
        /// </summary>
        [Link]
        readonly IClock clock = null;

        /// <summary>The parent Plant</summary>
        [Link]
        readonly Plant plant = null;

        /// <summary> Culms on the leaf </summary>
        [Link]
        readonly LeafCulms culms = null;

        /// <summary>The parent tilering class</summary>
        [Link]
        readonly Phenology phenology = null;

        /// <summary>The parent tilering class</summary>
        [Link]
        readonly SorghumLeaf leaf = null;

        /// <summary>
		/// Link to weather.
		/// </summary>
		[Link]
        readonly IWeather weather = null;

        /// <summary> Culms on the leaf </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        readonly IFunction areaCalc = null;

        /// <summary> Propoensity to Tiller Intercept </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        readonly IFunction tillerSdIntercept = null;

        /// <summary> Propsenity to Tiller Slope </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        readonly IFunction tillerSdSlope = null;

        /// <summary> LAI Value where tillers are no longer added </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        readonly IFunction maxLAIForTillerAddition = null;

        /// <summary> LAI Value where tillers are no longer added </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        readonly IFunction MaxDailyTillerReduction = null;

        /// <summary> LAI Value where tillers are no longer added </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        readonly IFunction tillerSlaBound = null;

        [Link(Type = LinkType.Child, ByName = true)]
        readonly IFunction slaLeafNoCoefficient = null;

        [Link(Type = LinkType.Child, ByName = true)]
        readonly IFunction maxSLAAdjustment = null;

        /// <summary>Number of potential Fertile Tillers at harvest</summary>
        [JsonIgnore]
        public double CalculatedTillerNumber { get; set; }

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
            set { }
        }

        /// <summary>Maximum SLA for tiller cessation</summary>
        [JsonIgnore]
        public double MaxSLA { get; set; }

        /// <summary>Supply Demand Ratio used to calculate Tiller No</summary>
        [JsonIgnore]
        public double SupplyDemandRatio { get; private set; }

        /// <summary>A value that can be used to override the calculated tillers.</summary>
        public double TillerNumberOverride { get; set; } = -1.0;

        private int floweringStage;
        private int endJuvenilePhase;
        private int startOfGrainFillPhase;
        private const int startThermalQuotientLeafNo = 3;
        private const int endThermalQuotientLeafNo = 5;
        private double plantsPerMetre;
        private double population;
        private double linearLAI;
        private double radiationValues = 0.0;
        private double temperatureValues = 0.0;
        private readonly List<int> tillerOrder = new();
        private bool initialTillersAdded = false;
        private bool isDynamicTillering = true;
        private TilleringMethodEnum tilleringMethod = TilleringMethodEnum.DynamicTillering;

        private double GetDeltaDmGreen() { return leaf.potentialDMAllocation.Structural; }

        private bool BeforeFloweringStage()
        {
            return BeforePhase("Flowering", ref floweringStage);
        }

        private bool BeforeEndJuvenileStage()
        {
            return BeforePhase("EndJuvenile", ref endJuvenilePhase);
        }

        private bool AfterEndJuvenileStage()
        {
            return AfterPhase("EndJuvenile", ref endJuvenilePhase);
        }

        private bool BeforeStartOfGrainFillStage()
        {
            return BeforePhase("StartGrainFill", ref startOfGrainFillPhase);
        }

        private bool BeforePhase(string phaseName, ref int phaseIndex)
        {
            if (phaseIndex < 1) phaseIndex = phenology.StartStagePhaseIndex(phaseName);
            return phenology.BeforePhase(phaseIndex);
        }

        private bool AfterPhase(string phaseName, ref int phaseIndex)
        {
            if (phaseIndex < 1) phaseIndex = phenology.StartStagePhaseIndex(phaseName);
            return phenology.BeyondPhase(phaseIndex);
        }

        /// <summary> Calculate number of leaves</summary>
        public double CalcLeafNumber()
        {
            if (culms.Culms?.Count == 0) return 0.0;
            if (!plant.IsEmerged) return 0.0;

            if (BeforeEndJuvenileStage())
            {
                // ThermalTime Targets to EndJuv are not known until the end of the Juvenile Phase
                // FinalLeafNo is not known until the TT Target is known - meaning the potential leaf sizes aren't known
                culms.Culms.ForEach(c => c.UpdatePotentialLeafSizes(culms.Culms[0], areaCalc as ICulmLeafArea));
            }

            var mainCulm = culms.Culms[0];
            var existingLeafNo = (int)Math.Floor(mainCulm.CurrentLeafNo);
            var nLeaves = mainCulm.CurrentLeafNo;
            var dltLeafNoMainCulm = 0.0;
            culms.dltLeafNo = dltLeafNoMainCulm;

            if (BeforeStartOfGrainFillStage())
            {
                // Calculate the leaf apperance on the main culm.
                dltLeafNoMainCulm = CalcLeafAppearance(mainCulm);

                // Now calculate the leaf apperance on all of the other culms.
                for (int i = 1; i < culms.Culms.Count; i++)
                {
                    CalcLeafAppearance(culms.Culms[i]);
                }
            }

            // Calculate Potential Tiller Number.
            var newLeafNo = (int)Math.Floor(mainCulm.CurrentLeafNo);

            // Up to L5 FE store PTQ. At L5 FE calculate tiller number (endThermalQuotientLeafNo).
            // At L5 FE newLeaf = 6 and currentLeaf = 5
            if (newLeafNo < startThermalQuotientLeafNo) initialTillersAdded = false;
            if (newLeafNo >= startThermalQuotientLeafNo && existingLeafNo < endThermalQuotientLeafNo)
            {
                radiationValues += weather.Radn;
                temperatureValues += phenology.thermalTime.Value();
            }
            if (newLeafNo == endThermalQuotientLeafNo && !initialTillersAdded)   // L5 Fully Expanded
            {
                // Dynamic calculation
                if (isDynamicTillering)
                {
                    double ptq = radiationValues / temperatureValues;
                    CalcTillerNumber(ptq);
                }

                AddInitialTillers();
                initialTillersAdded = true;
            }

            if (newLeafNo >= 5)
            {
                CalcTillerAppearance(newLeafNo, existingLeafNo);
            }

            return dltLeafNoMainCulm;
        }

        private double CalcLeafAppearance(Culm culm)
        {
            var leavesRemaining = culms.FinalLeafNo - culm.CurrentLeafNo;
            var leafAppearanceRate = culms.GetLeafAppearanceRate(leavesRemaining);
            // if leaves are still growing, the cumulative number of phyllochrons or fully expanded leaves is calculated from thermal time for the day.
            var dltLeafNo = MathUtilities.Bound(MathUtilities.Divide(phenology.thermalTime.Value(), leafAppearanceRate, 0), 0.0, leavesRemaining);

            culm.AddNewLeaf(dltLeafNo);

            return dltLeafNo;
        }

        private void CalcTillerNumber(double PTQ)
        {
            // The final tiller number (Ftn) is calculated after the full appearance of LeafNo 5 - when leaf 6 emerges.
            // Calc Supply = R/oCd * LA5 * Phy5
            var areaMethod = areaCalc as ICulmLeafArea;
            var mainCulm = culms.Culms[0];
            double L5Area = areaMethod.CalculateIndividualLeafArea(5, mainCulm);
            double L9Area = areaMethod.CalculateIndividualLeafArea(9, mainCulm);
            double Phy5 = culms.GetLeafAppearanceRate(culms.FinalLeafNo - culms.Culms[0].CurrentLeafNo);

            // Calc Demand = LA9 - LA5
            var demand = L9Area - L5Area;
            var supply = PTQ * L5Area * Phy5;
            SupplyDemandRatio = MathUtilities.Divide(supply, demand, 0);

            // If the tiller number override has been set (we initialise it to -1) then use that rather
            // than calculating the tiller number using the slope/intercept etc.
            if (TillerNumberOverride >= 0)
            {
                CalculatedTillerNumber = TillerNumberOverride;
            }
            else
            {
                CalculatedTillerNumber = Math.Max(
                    tillerSdIntercept.Value() + tillerSdSlope.Value() * SupplyDemandRatio,
                    0.0
                );
            }
        }

        void AddInitialTillers()
        {
            tillerOrder.Clear();

            if (CalculatedTillerNumber <= 0) return;

            // Lafarge et al. (2002) reported a common hierarchy of tiller emergence of T3>T4>T2>T1>T5>T6 across diverse density treatments
            // 1 tiller  = T3 
            // 2 tillers = T3 + T4
            // 3 tillers = T2 + T3 + T4
            // 4 tillers = T1 + T2 + T3 + T4
            // 5 tillers = T1 + T2 + T3 + T4 + T5
            // 6 tillers = T1 + T2 + T3 + T4 + T5 + T6

            // At leaf 5 fully expanded only initialize T1 with 2 leaves if present.

            int nTillers = (int)Math.Ceiling(CalculatedTillerNumber);
            if (nTillers <= 0) return;

            if (nTillers < 3) tillerOrder.Add(3);
            if (nTillers == 2) tillerOrder.Add(4);
            if (nTillers == 3)
            {
                tillerOrder.Add(2);
                tillerOrder.Add(3);
                tillerOrder.Add(4);
            }
            if (nTillers > 3)
            {
                for (int i = 1; i <= nTillers; i++)
                {
                    tillerOrder.Add(i);
                }
            }

            if (nTillers > 3)
            {
                InitiateTiller(1, 1, 2);
                CurrentTillerNumber = 1;
            }
        }

        private void CalcTillerAppearance(int newLeaf, int currentLeaf)
        {
            // Each time a leaf becomes fully expanded starting at 5 see if a tiller should be initiated.
            // When a leaf is fully expanded a tiller can be initiated at the axil 3 leaves less
            // So at L5 FE (newLeaf = 6, currentLeaf = 5) a Tiller might be at axil 2. i.e. a T2 

            // Add any new tillers and then calc each tiller in turn. Add a tiller if:
            // 1. There are more tillers to add.
            // 2. linearLAI < maxLAIForTillerAddition
            // 3. A leaf has fully expanded.  (newLeaf >= 6, newLeaf > currentLeaf)
            // 4. there should be a tiller at that node. (Check tillerOrder)

            var tillersAdded = culms.Culms.Count - 1;
            if (isDynamicTillering)
            {
                double lLAI = CalcLinearLAI();
                if (lLAI > maxLAIForTillerAddition.Value()) return;
            }

            if (newLeaf >= 5 && newLeaf > currentLeaf && CalculatedTillerNumber > tillersAdded)
            {
                // Axil = currentLeaf - 3
                int newNodeNumber = newLeaf - 3;
                if (tillerOrder.Contains(newNodeNumber))
                {
                    var fractionToAdd = Math.Min(1.0, CalculatedTillerNumber - tillersAdded);

                    DltTillerNumber = fractionToAdd;
                    CurrentTillerNumber += fractionToAdd;

                    InitiateTiller(newNodeNumber, fractionToAdd, 1);
                }
            }
        }

        private double CalcLinearLAI()
        {
            var tpla = (leaf.LAI + leaf.SenescedLai) / population * 10000; // Leaf area of one plant.
            linearLAI = plantsPerMetre * tpla / 10000.0;
            return linearLAI;
        }

        /// <summary>
        /// Add a tiller.
        /// </summary>
        void InitiateTiller(int tillerNumber, double fractionToAdd, double initialLeaf)
        {
            double leafNoAtAppearance = 1.0;
            var mainCulm = culms.Culms[0];

            Culm newCulm = new(leafNoAtAppearance)
            {
                CulmNo = tillerNumber,
                CurrentLeafNo = initialLeaf,
                VertAdjValue = culms.MaxVerticalTillerAdjustment.Value() + (CurrentTillerNumber * culms.VerticalTillerAdjustment.Value()),
                Proportion = fractionToAdd,
                FinalLeafNo = mainCulm.FinalLeafNo - tillerNumber
            };
            newCulm.UpdatePotentialLeafSizes(mainCulm, areaCalc as ICulmLeafArea);
            culms.Culms.Add(newCulm);
        }

        /// <summary>Calculate the potential leaf area</summary>
        public double CalcPotentialLeafArea()
        {
            culms.Culms.ForEach(c => c.DltLAI = 0);
            if (BeforeFloweringStage())
            {
                return areaCalc.Value();
            }
            return 0.0;
        }

        /// <summary> calculate the actual leaf area</summary>
        public double CalcActualLeafArea(double dltStressedLAI)
        {
            var mainCulm = culms.Culms.FirstOrDefault();
            var updatedDltStressedLAI = dltStressedLAI;

            if (isDynamicTillering)
            {
                if (mainCulm != null &&
                    AfterEndJuvenileStage() &&
                    CalculatedTillerNumber > 0.0 &&
                    mainCulm.CurrentLeafNo < mainCulm.PositionOfLargestLeaf
                )
                {
                    CalculateTillerCessation();
                }

                updatedDltStressedLAI = 0.0;
                foreach (var culm in culms.Culms)
                {
                    updatedDltStressedLAI += culm.DltLAI;
                }
            }

            double laiSlaReductionFraction = CalcCarbonLimitation(updatedDltStressedLAI);
            double leaf = mainCulm.CurrentLeafNo;
            var dltLAI = Math.Max(updatedDltStressedLAI * laiSlaReductionFraction, 0.0);

            // Apply to each culm
            if (laiSlaReductionFraction < 1.0)
            {
                ReduceAllTillersProportionately(laiSlaReductionFraction);
            }

            culms.Culms.ForEach(c => c.TotalLAI += c.DltStressedLAI);

            return dltLAI;
        }

        private double CalcCarbonLimitation(double dltStressedLAI)
        {
            var slaLeafNoCoefficientValue = slaLeafNoCoefficient.Value();

            // If the coefficient is zero or negative, we don't want to apply this limitation.
            if (slaLeafNoCoefficientValue <= 0.0) return 1.0;
            if (dltStressedLAI <= 0.0) return 1.0;

            // Get the total leaf mass and LAI of the plant.
            var leafMassGm2 = leaf.Live.Wt;
            var laiTotalM2 = leaf.LAITotal;

            // If there is no leaf mass then there is nothing to do.
            if (leafMassGm2 < 0.0) return 1.0;

            double dltDmGreen = GetDeltaDmGreen();
            var leafMassNewGm2 = (leafMassGm2 + dltDmGreen);
            if (leafMassNewGm2 < 0.0) return 1.0;
            var slaNewCm2g = (laiTotalM2 + dltStressedLAI) * 10000 / leafMassNewGm2;

            var mainCulm = culms.Culms[0];
            double nLeaves = mainCulm.CurrentLeafNo;
            CalculateMaxSLALeafGrowth(nLeaves);

            if (slaNewCm2g <= MaxSLA) return 1.0;

            // Leaf is getting too thin, reduce area growth.
            var dltLaiPossible = (MaxSLA / 10000) * leafMassNewGm2 - laiTotalM2;
            var laiFractionalReductionForSLA = Math.Max(dltLaiPossible / dltStressedLAI, 0.0);

            if (laiFractionalReductionForSLA < 1)
            {
                summary.WriteMessage(this, $"Leaf Area reduced due to carbon limitation: {Environment.NewLine}", MessageType.Information);
                summary.WriteMessage(this, $"LaiTotal: {laiTotalM2:F3}. MaxSLA: {MaxSLA:F3}. SlaNew: {slaNewCm2g:F3}. dltStressedLAI: {dltStressedLAI:F3}. Reduce by: {laiFractionalReductionForSLA:F3}. dltDmGreen: {dltDmGreen:F3}", MessageType.Information);
            }
            return laiFractionalReductionForSLA;
        }

        private void CalculateMaxSLALeafGrowth(double nLeaves)
        {
            var slaLeafNoCoefficientValue = slaLeafNoCoefficient.Value();
            double adj = maxSLAAdjustment.Value();
            adj = 50;
            MaxSLA = 429.72 - slaLeafNoCoefficientValue * (nLeaves);
//            MaxSLA *= (100 + maxSLAAdjustment.Value()) / 100.0;
            MaxSLA *= (100 + adj) / 100.0;
            MaxSLA = Math.Min(400, MaxSLA);
            MaxSLA = Math.Max(150, MaxSLA);
        }

        private void CalculateMaxSLATillerCessation(double nLeaves)
        {
            var slaLeafNoCoefficientValue = slaLeafNoCoefficient.Value();
            MaxSLA = 429.72 - slaLeafNoCoefficientValue * (nLeaves);
            MaxSLA *= (100 - tillerSlaBound.Value()) / 100.0;
            MaxSLA = Math.Min(400, MaxSLA);
            MaxSLA = Math.Max(150, MaxSLA);
        }

        private void CalculateTillerCessation()
        {
            bool moreToAdd = (CurrentTillerNumber < CalculatedTillerNumber) && (linearLAI < maxLAIForTillerAddition.Value());
            var tillerLaiToReduce = CalcCeaseTillerSignal();
            double nLeaves = culms.Culms[0].CurrentLeafNo;

            if (nLeaves < 8 || moreToAdd || tillerLaiToReduce < 0.00001) return;

            double maxTillerLoss = MaxDailyTillerReduction.Value();
            double accProportion = 0.0;
            double tillerLaiLeftToReduce = tillerLaiToReduce;

            for (var culmIndex = culms.Culms.Count - 1; culmIndex >= 1; culmIndex--)
            {
                if (accProportion < maxTillerLoss && tillerLaiLeftToReduce > 0)
                {
                    var culm = culms.Culms[culmIndex];

                    double tillerLAI = culm.TotalLAI;
                    double tillerProportion = culm.Proportion;

                    if (tillerProportion > 0.0 && tillerLAI > 0.0)
                    {
                        // Use the amount of LAI past the target as an indicator of how much of the tiller
                        // to remove which will affect tomorrow's growth - up to the maxTillerLoss
                        double propn = Math.Max(
                            0.0,
                            Math.Min(maxTillerLoss - accProportion, tillerLaiLeftToReduce / tillerLAI)
                        );

                        accProportion += propn;
                        tillerLaiLeftToReduce -= propn * tillerLAI;
                        double remainingProportion = Math.Max(0.0, culm.Proportion - propn);
                        // Can't increase the proportion
                        culm.Proportion = remainingProportion;

                        culm.TotalLAI -= propn * tillerLAI;
                    }
                }

                if (!(tillerLaiLeftToReduce > 0) || accProportion >= maxTillerLoss) break;
            }
            CurrentTillerNumber = 0;
            culms.Culms.ForEach(c => CurrentTillerNumber += c.Proportion); CurrentTillerNumber -= 1;
        }

        private double CalcCeaseTillerSignal()
        {
            var mainCulm = culms.Culms.FirstOrDefault();

            // Calculate sla target that is below the actual SLA - so as the leaves gets thinner it signals to the tillers to cease growing further
            // max SLA (thinnest leaf) possible using Reeves (1960's Kansas) SLA = 429.72 - 18.158 * LeafNo
            double nLeaves = mainCulm.CurrentLeafNo;
            CalculateMaxSLATillerCessation(nLeaves);
            double dmGreen = leaf.Live.Wt;

            // Calc how much LAI we need to remove to get back to the SLA target line.
            // This is done by reducing the proportion of tiller area.
            //            var maxLaiTarget = MaxSLA * (dmGreen + dltDmGreen) / 10000;
            //          return Math.Max(leaf.LAI + dltStressedLAI - maxLaiTarget, 0);
            var maxLaiTarget = MaxSLA * (dmGreen) / 10000;
            return Math.Max(leaf.LAI - maxLaiTarget, 0);
        }

        /// <summary>
        /// Calculate SLA for leafa rea including potential new growth - stressess effect
        /// </summary>
        /// <param name="stressedLAI"></param>
        /// <returns></returns>
        public double CalcCurrentSLA(double stressedLAI)
        {
            double dmGreen = leaf.Live.Wt;
            double dltDmGreen = GetDeltaDmGreen();

            if (dmGreen + dltDmGreen <= 0.0) return 0.0;

            return (leaf.LAI + stressedLAI) / (dmGreen + dltDmGreen) * 10000; // (cm^2/g)
        }

        void ReduceAllTillersProportionately(double laiReduction)
        {
            if (laiReduction <= 0.0) return;

            double totalDltLeaf = culms.Culms.Sum(c => c.DltStressedLAI);
            if (totalDltLeaf <= 0.0) return;

            // Reduce new leaf growth proportionally across all culms
            // not reducing the number of tillers at this stage.
            culms.Culms.ForEach(c => c.DltStressedLAI *= laiReduction);
        }

        /// <summary> Reset Culms at start of the simulation </summary>
        [EventSubscribe("StartOfSimulation")]
        private void StartOfSim(object sender, EventArgs e)
        {
            TillerNumberOverride = -1.0;
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
            if (data.Plant == plant)
            {
                population = data.Population;
                plantsPerMetre = data.Population * data.RowSpacing / 1000.0 * data.SkipDensityScale;
                CurrentTillerNumber = 0.0;
                CalculatedTillerNumber = 0.0;
                isDynamicTillering = false;
                radiationValues = 0.0;
                temperatureValues = 0.0;

                tilleringMethod = (TilleringMethodEnum)data.TilleringMethod;

                switch (tilleringMethod)
                {
                    case TilleringMethodEnum.RulOfThumb:
                        CalculatedTillerNumber = RuleOfThumbFTNGenerator.CalculateFtn(weather, plant, clock);
                        break;

                    case TilleringMethodEnum.FixedTillering:
                        CalculatedTillerNumber = data.FTN;
                        break;

                    case TilleringMethodEnum.DynamicTillering:
                        isDynamicTillering = true;
                        CalculatedTillerNumber = 0.0;
                        break;
                }
            }
        }
    }
}
