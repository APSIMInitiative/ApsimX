using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using MathNet.Numerics.Statistics.Mcmc;
using Models.Core;
using Models.Functions;
using Models.GrazPlan;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.Utilities;
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
            set { throw new Exception("Cannot set the FertileTillerNumber for Dynamic Tillering. Make sure you set TilleringMethod before FertileTillerNmber"); }
        }

        /// <summary>Supply Demand Ratio used to calculate Tiller No</summary>
        [JsonIgnore]
        public double SupplyDemandRatio { get; private set; }

        private int flagStage;
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

        private bool BeforeFlagLeafStage()
        {
            return BeforePhase("FlagLeaf", ref flagStage);
        }

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
                culms.Culms.ForEach(c => c.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea));
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

            var newLeafNo = (int)Math.Floor(mainCulm.CurrentLeafNo);

            if (nLeaves > startThermalQuotientLeafNo)
            {
                CalcTillers(newLeafNo, existingLeafNo);
                CalcTillerAppearance(newLeafNo, existingLeafNo);
            }

            return dltLeafNoMainCulm;
        }

        private double CalcLeafAppearance(Culm culm)
        {
            var leavesRemaining = culms.FinalLeafNo - culm.CurrentLeafNo;
            var leafAppearanceRate = culms.getLeafAppearanceRate(leavesRemaining);
            // if leaves are still growing, the cumulative number of phyllochrons or fully expanded leaves is calculated from thermal time for the day.
            var dltLeafNo = MathUtilities.Bound(MathUtilities.Divide(phenology.thermalTime.Value(), leafAppearanceRate, 0), 0.0, leavesRemaining);

            culm.AddNewLeaf(dltLeafNo);

            return dltLeafNo;
        }

        private void CalcTillers(int newLeaf, int currentLeaf)
        {
            if (CalculatedTillerNumber > 0.0) return;

            // Up to L5 FE store PTQ. At L5 FE calculate tiller number (endThermalQuotientLeafNo).
            // At L5 FE newLeaf = 6 and currentLeaf = 5
            if (newLeaf >= startThermalQuotientLeafNo && currentLeaf < endThermalQuotientLeafNo)
            {
                radiationValues += metData.Radn;
                temperatureValues += phenology.thermalTime.Value();

                // L5 Fully Expanded
                if (newLeaf == endThermalQuotientLeafNo)
                {
                    double PTQ = radiationValues / temperatureValues;
                    CalcTillerNumber(PTQ);
                    AddInitialTillers();
                }
            }            
        }

        private void CalcTillerNumber(double PTQ)
        {
            // The final tiller number (Ftn) is calculated after the full appearance of LeafNo 5 - when leaf 6 emerges.
            // Calc Supply = R/oCd * LA5 * Phy5
            var areaMethod = areaCalc as ICulmLeafArea;
            var mainCulm = culms.Culms[0];
            double L5Area = areaMethod.CalculateIndividualLeafArea(5, mainCulm);
            double L9Area = areaMethod.CalculateIndividualLeafArea(9, mainCulm);

            double Phy5 = culms.getLeafAppearanceRate(culms.FinalLeafNo - culms.Culms[0].CurrentLeafNo);

            // Calc Demand = LA9 - LA5
            var demand = L9Area - L5Area;
            var supply = PTQ * L5Area * Phy5;
            SupplyDemandRatio = MathUtilities.Divide(supply, demand, 0);

            CalculatedTillerNumber = Math.Max(
                tillerSdIntercept.Value() + tillerSdSlope.Value() * SupplyDemandRatio, 
                0.0
            );
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
            double lLAI = CalcLinearLAI();

            if (newLeaf >= 5 &&
                newLeaf > currentLeaf &&
                CalculatedTillerNumber > tillersAdded &&
                lLAI < maxLAIForTillerAddition.Value()
            )
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
            Culm newCulm = new(leafNoAtAppearance)
            {
                CulmNo = tillerNumber,
                CurrentLeafNo = initialLeaf,
                VertAdjValue = culms.MaxVerticalTillerAdjustment.Value() + (CurrentTillerNumber * culms.VerticalTillerAdjustment.Value()),
                Proportion = fractionToAdd,
                FinalLeafNo = culms.Culms[0].FinalLeafNo
            };
            newCulm.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea);
            culms.Culms.Add(newCulm);
        }

        /// <summary>Calculate the potential leaf area</summary>
        public double CalcPotentialLeafArea()
        {
            if (BeforeFloweringStage())
            {
                return areaCalc.Value();
            }
            return 0.0;
        }

        /// <summary> calculate the actual leaf area</summary>
        public double CalcActualLeafArea(double dltStressedLAI)
        {
            //var mainCulm = culms.Culms.FirstOrDefault();

            //if (AfterEndJuvenileStage() && 
            //    CalculatedTillerNumber > 0.0 &&
            //    (mainCulm != null && mainCulm.CurrentLeafNo < X0Main )
            //)
            //{

            //}

            //check current stage and current leaf number
            if (BeforeEndJuvenileStage() || !BeforeFlagLeafStage())
            {
                culms.Culms.ForEach(c => c.TotalLAI += c.DltStressedLAI);
                return dltStressedLAI;
            }

            double laiReductionForSLA = CalcLeafReductionForCarbonLimitation(dltStressedLAI);
            var currentSLA = CalcCurrentSLA(dltStressedLAI - laiReductionForSLA);

            var tillerLaiToReduce = CalcCeaseTillerSignal(dltStressedLAI - laiReductionForSLA);

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

            ReduceAllTillersProportionately(laiReductionForSLA);
            culms.Culms.ForEach(c => c.TotalLAI += c.DltStressedLAI);

            return dltStressedLAI - laiReductionForSLA;
        }

        private double CalcLeafReductionForCarbonLimitation(double dltStressedLAI)
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
        public double CalcCurrentSLA(double stressedLAI)
        {
            double dmGreen = leaf.Live.Wt;
            double dltDmGreen = leaf.potentialDMAllocation.Structural;

            if (dmGreen + dltDmGreen <= 0.0) return 0.0;

            return (leaf.LAI + stressedLAI) / (dmGreen + dltDmGreen) * 10000; // (cm^2/g)
        }

        private double CalcCeaseTillerSignal(double dltStressedLAI)
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

        void ReduceAllTillersProportionately(double laiReduction)
        {
            if (laiReduction <= 0.0) return;

            double totalDltLeaf = culms.Culms.Sum(c => c.DltStressedLAI);
            if (totalDltLeaf <= 0.0) return;

            //reduce new leaf growth proportionally across all culms
            //not reducing the number of tillers at this stage
            culms.Culms.ForEach(
                c => 
                c.DltStressedLAI -= Math.Max(
                    c.DltStressedLAI / totalDltLeaf * laiReduction, 
                    0.0
                )
            );
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
                population = data.Population;
                plantsPerMetre = data.Population * data.RowSpacing / 1000.0 * data.SkipDensityScale;
                CurrentTillerNumber = 0.0;
                CalculatedTillerNumber = 0.0;

                radiationValues = 0.0;
                temperatureValues = 0.0;
            }
        }
    }
}
