using Models.Core;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace UserCode
{
    [Serializable]
    public class GenericFruitTreeSensibilityPostSimulationTests : Model, ITest
    {
        [Link] private DataStore store = null!;

        private const double Epsilon = 1e-9;

        private static readonly string[] FiniteColumns = new[]
        {
            "BiomassLeavesOutput",
            "BiomassFruitsOutput",
            "QualityBrix",
            "QualityDMPctOutput",
            "QualityAcidPctOutput",
            "Arbitrator.fruitOrgan.Live.Wt",
            "Arbitrator.fruitOrgan.Dead.Wt",
            "Arbitrator.fruitOrgan.Senesced.Wt",
            "Arbitrator.fruitOrgan.Nflower",
            "Arbitrator.fruitOrgan.Nfruit",
            "Arbitrator.leafOrgan.LAI",
            "Arbitrator.leafOrgan.Fw",
            "FruitTree.PruningLeafFractionOutput",
            "FruitTree.PruningStructuralFractionOutput",
            "FruitTree.ActiveStructuralPruningFractionOutput",
            "FruitTree.CanopyStructuralSizeFactorOutput",
            "FruitTree.CanopyStructuralLightFactorOutput",
            "Arbitrator.leafOrgan.Wt",
            "PotentialEP",
            "WaterDemand",
            "FruitSinkStrengthOutput",
            "LeafSinkStrengthOutput",
            "RootZonePAWFractionOutput",
            "SupplyIndexRawOutput",
            "SupplyIndexEffectiveOutput",
            "GrowthStressModifierOutput",
            "StressForReproOutput",
            "ReproSetRateOutput",
            "ReproDropRateOutput",
            "FruitWaterFractionOutput",
            "ReserveSupplyDMOutput",
            "ReserveCriticalDemandDMOutput",
            "ReserveStoredDMOutput",
            "ReservePoolDMOutput",
            "ReserveCapacityDMOutput",
            "ReserveMobilisedDMOutput",
            "Arbitrator.woodOrgan.Live.StructuralWt",
            "Arbitrator.woodOrgan.Senesced.Wt",
            "Arbitrator.woodOrgan.DMDemand.Structural",
            "OrchardManagement.Script.ReservePoolCapacityRatioOutput",
            "OrchardManagement.Script.ReserveCumulativeSurplusDMOutput",
            "OrchardManagement.Script.ReserveCumulativeDeficitDMOutput",
            "SoilPlantConductanceOutput",
            "HydraulicSoilConductanceOutput",
            "HydraulicPlantConductanceOutput",
            "HydraulicReferenceConductanceOutput",
            "HydraulicKTermOutput",
            "HydraulicPAWTermOutput",
            "HydraulicRootedPAWCmmOutput",
            "HydraulicPlantLimitedOutput",
            "Arbitrator.waterUptakeMethod.WDemand",
            "Arbitrator.waterUptakeMethod.WSupply",
            "Arbitrator.waterUptakeMethod.WAllocated",
            "VPDOutput",
            "ChillUnitsAccumulatedOutput",
            "ChillUnitsRequiredOutput",
            "ChillUnitsAtLastTransitionOutput",
            "ForcingUnitsAccumulatedOutput",
            "ForcingUnitsRequiredOutput",
            "ForcingUnitsAtLastTransitionOutput"
        };

        private static readonly string[] NonNegativeColumns = new[]
        {
            "BiomassLeavesOutput",
            "BiomassFruitsOutput",
            "Arbitrator.fruitOrgan.Live.Wt",
            "Arbitrator.fruitOrgan.Dead.Wt",
            "Arbitrator.fruitOrgan.Senesced.Wt",
            "Arbitrator.fruitOrgan.Nflower",
            "Arbitrator.fruitOrgan.Nfruit",
            "Arbitrator.leafOrgan.LAI",
            "Arbitrator.leafOrgan.Fw",
            "FruitTree.PruningLeafFractionOutput",
            "FruitTree.PruningStructuralFractionOutput",
            "FruitTree.ActiveStructuralPruningFractionOutput",
            "FruitTree.CanopyStructuralSizeFactorOutput",
            "FruitTree.CanopyStructuralLightFactorOutput",
            "Arbitrator.leafOrgan.Wt",
            "PotentialEP",
            "WaterDemand",
            "ReserveSupplyDMOutput",
            "ReserveCriticalDemandDMOutput",
            "ReserveStoredDMOutput",
            "ReservePoolDMOutput",
            "ReserveCapacityDMOutput",
            "ReserveMobilisedDMOutput",
            "Arbitrator.woodOrgan.Live.StructuralWt",
            "Arbitrator.woodOrgan.Senesced.Wt",
            "Arbitrator.woodOrgan.DMDemand.Structural",
            "OrchardManagement.Script.ReservePoolCapacityRatioOutput",
            "OrchardManagement.Script.ReserveCumulativeSurplusDMOutput",
            "OrchardManagement.Script.ReserveCumulativeDeficitDMOutput",
            "SoilPlantConductanceOutput",
            "HydraulicSoilConductanceOutput",
            "HydraulicPlantConductanceOutput",
            "HydraulicReferenceConductanceOutput",
            "HydraulicKTermOutput",
            "HydraulicPAWTermOutput",
            "HydraulicRootedPAWCmmOutput",
            "HydraulicPlantLimitedOutput",
            "Arbitrator.waterUptakeMethod.WDemand",
            "Arbitrator.waterUptakeMethod.WSupply",
            "Arbitrator.waterUptakeMethod.WAllocated",
            "ChillUnitsAccumulatedOutput",
            "ChillUnitsRequiredOutput",
            "ChillUnitsAtLastTransitionOutput",
            "ForcingUnitsAccumulatedOutput",
            "ForcingUnitsRequiredOutput",
            "ForcingUnitsAtLastTransitionOutput"
        };

        private static readonly string[] UnitIntervalColumns = new[]
        {
            "SupplyIndexRawOutput",
            "SupplyIndexEffectiveOutput",
            "GrowthStressModifierOutput",
            "Arbitrator.leafOrgan.Fw",
            "RootZonePAWFractionOutput",
            "StressForReproOutput",
            "FruitWaterFractionOutput",
            "FruitTree.PruningLeafFractionOutput",
            "FruitTree.PruningStructuralFractionOutput",
            "FruitTree.ActiveStructuralPruningFractionOutput",
            "FruitTree.CanopyStructuralSizeFactorOutput",
            "FruitTree.CanopyStructuralLightFactorOutput",
            "HydraulicKTermOutput",
            "HydraulicPAWTermOutput",
            "HydraulicPlantLimitedOutput",
            "DormancyPhaseActiveOutput",
            "ForcingPhaseActiveOutput",
            "ChillRequirementSatisfiedTodayOutput",
            "ForcingRequirementSatisfiedTodayOutput"
        };

        private static readonly string[] TimingColumns = new[]
        {
            "SimulationName",
            "Clock.Today",
            "Phenology.CurrentStageName",
            "PhenologyPhaseNameOutput",
            "IsReadyForHarvesting",
            "Arbitrator.fruitOrgan.IsHarvestable"
        };

        private static readonly string[] ReproductiveStageOrder = new[]
        {
            "BudBreak",
            "Flowering",
            "FruitSet",
            "FruitFill",
            "Maturity"
        };

        private class Metrics
        {
            public double MaxLeaf;
            public double MaxFruit;
            public double FinalFruit;
            public double FinalFruitDeadOrSenesced;
            public double MaxFruitAfterThinWindow;
            public double MeanSupply;
            public double MeanGrowth;
            public double MeanPAW;
            public double MeanConductance;
            public double MeanHydraulicPlantConductance;
            public double MeanHydraulicReferenceConductance;
            public double MeanHydraulicKTerm;
            public double MeanHydraulicPlantLimited;
            public double MeanLeafWaterStress;
            public double MeanWaterDemand;
            public double MeanWaterSupply;
            public double MeanFruitWater;
            public double MaxBrix;
            public double MaxDMPct;
            public double MaxAcid;
            public double MaxBudLoad;
            public double MaxFlower;
            public double MaxNfruit;
            public double MaxSetRate;
            public double MaxReserveCapacity;
            public double MaxReservePool;
            public double MaxReserveStored;
            public double MaxReserveMobilised;
            public double CumulativeReserveStored;
            public double CumulativeReserveMobilised;
            public double CumulativeReserveSurplus;
            public double CumulativeReserveDeficit;
            public double MaxReservePoolCapacityRatio;
            public double MaxLiveStructuralWood;
            public double MaxWoodSenesced;
            public double MeanWoodStructuralDemand;
            public double Rows;
            public int FirstFruitIndex;
            public int FirstReadyIndex;
            public int FirstHarvestableIndex;
            public int FloweringEntries;
            public int FruitSetEntries;
            public int DormancyEntries;
            public double MaxDormancyPhaseActive;
            public double MaxForcingPhaseActive;
            public double MaxChillUnitsAccumulated;
            public double MaxChillUnitsAtTransition;
            public double ChillSatisfiedDays;
            public double MaxForcingUnitsAccumulated;
            public double MaxForcingUnitsAtTransition;
            public double ForcingSatisfiedDays;
            public double SecondSeasonMaxFruit;
        }

        public void Run()
        {
            DataTable data = store.Reader.GetData("Report");
            if (data == null || data.Rows.Count == 0)
                throw new Exception("PostSimulationTests failed Report table check: GenericFruitTree sensibility Report table was not produced.");

            ValidateCommonInvariants(data);
            CheckTimingSensibility(data);

            var bySimulation = data.Rows.Cast<DataRow>()
                .GroupBy(row => Convert.ToString(row["SimulationName"], CultureInfo.InvariantCulture))
                .ToDictionary(group => group.Key, group => Summarise(group.ToList()));

            Require(bySimulation, "Baseline");
            RequirePositive("Baseline", "rows produced", bySimulation["Baseline"].Rows);
            RequirePositive("Baseline", "leaf growth", bySimulation["Baseline"].MaxLeaf);
            RequirePositive("Baseline", "fruit biomass", bySimulation["Baseline"].MaxFruit);
            RequirePositive("Baseline", "fruit quality signal", bySimulation["Baseline"].MaxBrix);

            CheckDescending(bySimulation, new[] { "Water_WellWatered", "Water_ModerateDeficit", "Water_SevereDeficit" }, m => m.MeanPAW, "rooted-zone PAW");
            CheckDescending(bySimulation, new[] { "Water_WellWatered", "Water_ModerateDeficit", "Water_SevereDeficit" }, m => m.MeanSupply, "supply index");
            CheckDescending(bySimulation, new[] { "Water_WellWatered", "Water_ModerateDeficit", "Water_SevereDeficit" }, m => m.MeanGrowth, "growth modifier");
            CheckLessThan("Water_SevereDeficit", "seasonal fruit biomass vs well-watered", bySimulation["Water_SevereDeficit"].MaxFruit, "Water_WellWatered", bySimulation["Water_WellWatered"].MaxFruit);

            CheckPruningResponse(data);

            CheckThinningResponse(data);
            CheckDescending(bySimulation, new[] { "Thinning_None", "Thinning_Moderate", "Thinning_Heavy" }, m => m.MaxFruit, "seasonal fruit biomass response after thinning");
            CheckLessThan("Thinning_Heavy", "fruit biomass shortly after thinning", bySimulation["Thinning_Heavy"].MaxFruitAfterThinWindow, "Thinning_None", bySimulation["Thinning_None"].MaxFruitAfterThinWindow);

            CheckHydraulicConductanceDiagnostic(bySimulation);
            CheckHydraulicGrowthResponseDiagnostic(bySimulation);

            CheckAscending(bySimulation, new[] { "Repro_LowBudLoad", "Repro_MediumBudLoad", "Repro_HighBudLoad" }, m => m.MaxBudLoad, "bud load");
            CheckAscending(bySimulation, new[] { "Repro_LowBudLoad", "Repro_MediumBudLoad", "Repro_HighBudLoad" }, m => m.MaxFlower, "flower number");
            CheckAscending(bySimulation, new[] { "Repro_LowBudLoad", "Repro_MediumBudLoad", "Repro_HighBudLoad" }, m => m.MaxNfruit, "fruit number from reproductive potential");
            CheckAscending(bySimulation, new[] { "Repro_LowBudLoad", "Repro_MediumBudLoad", "Repro_HighBudLoad" }, m => m.MaxSetRate, "fruit set rate");

            CheckReserveDiagnostics(bySimulation);

            CheckHarvestRules(bySimulation);

            CheckDescending(bySimulation, new[] { "Quality_WellWatered", "Quality_MildStress", "Quality_SevereStress" }, m => m.MeanPAW, "quality scenario water status");
            CheckDescending(bySimulation, new[] { "Quality_WellWatered", "Quality_MildStress", "Quality_SevereStress" }, m => m.MeanFruitWater, "fruit water fraction under stress");
            CheckAscending(bySimulation, new[] { "Quality_WellWatered", "Quality_MildStress", "Quality_SevereStress" }, m => m.MaxBrix, "Brix under water stress");
            CheckAscending(bySimulation, new[] { "Quality_WellWatered", "Quality_MildStress", "Quality_SevereStress" }, m => m.MaxDMPct, "dry matter percentage under water stress");
            CheckAscending(bySimulation, new[] { "Quality_WellWatered", "Quality_MildStress", "Quality_SevereStress" }, m => m.MaxAcid, "acid under water stress");

            Require(bySimulation, "Dormancy_InsufficientChill");
            Require(bySimulation, "Dormancy_AdequateChill");
            Require(bySimulation, "Forcing_LowHeat");
            Require(bySimulation, "Forcing_AdequateHeat");
            CheckDormancyForcingDiagnostics(bySimulation);

            Require(bySimulation, "MultiYear_ResetEnabled");
            Require(bySimulation, "MultiYear_ResetDisabled");
            if (bySimulation["MultiYear_ResetEnabled"].FloweringEntries < 2 || bySimulation["MultiYear_ResetEnabled"].FruitSetEntries < 2)
                throw new Exception($"MultiYear_ResetEnabled failed repeated seasonal cycle check: floweringEntries={bySimulation["MultiYear_ResetEnabled"].FloweringEntries}, fruitSetEntries={bySimulation["MultiYear_ResetEnabled"].FruitSetEntries}");
            RequirePositive("MultiYear_ResetEnabled", "second-season fruit biomass", bySimulation["MultiYear_ResetEnabled"].SecondSeasonMaxFruit);
            if (bySimulation["MultiYear_ResetDisabled"].FloweringEntries > 1)
                throw new Exception($"MultiYear_ResetDisabled failed no-second-season-flowering check: floweringEntries={bySimulation["MultiYear_ResetDisabled"].FloweringEntries}");
            if (bySimulation["MultiYear_ResetDisabled"].FloweringEntries >= bySimulation["MultiYear_ResetEnabled"].FloweringEntries)
                throw new Exception($"MultiYear_ResetDisabled failed reset comparison check: disabledFloweringEntries={bySimulation["MultiYear_ResetDisabled"].FloweringEntries}, enabledFloweringEntries={bySimulation["MultiYear_ResetEnabled"].FloweringEntries}");

            Require(bySimulation, "FruitFate_Persist");
            Require(bySimulation, "FruitFate_Abscise");
            Require(bySimulation, "FruitFate_SenesceToDead");
            Require(bySimulation, "FruitFate_AutoHarvest");
            CheckGreaterThan("FruitFate_Persist", "final live fruit biomass vs abscise", bySimulation["FruitFate_Persist"].FinalFruit, "FruitFate_Abscise", bySimulation["FruitFate_Abscise"].FinalFruit);
            CheckGreaterThan("FruitFate_Persist", "final live fruit biomass vs autoharvest", bySimulation["FruitFate_Persist"].FinalFruit, "FruitFate_AutoHarvest", bySimulation["FruitFate_AutoHarvest"].FinalFruit);
            CheckLessThan("FruitFate_AutoHarvest", "final live fruit biomass after harvest readiness", bySimulation["FruitFate_AutoHarvest"].FinalFruit, "FruitFate_Persist", bySimulation["FruitFate_Persist"].FinalFruit);
            CheckGreaterThan("FruitFate_SenesceToDead", "final dead/senesced fruit biomass vs abscise", bySimulation["FruitFate_SenesceToDead"].FinalFruitDeadOrSenesced, "FruitFate_Abscise", bySimulation["FruitFate_Abscise"].FinalFruitDeadOrSenesced);
        }

        private static void ValidateCommonInvariants(DataTable data)
        {
            foreach (string column in FiniteColumns.Concat(NonNegativeColumns).Concat(UnitIntervalColumns).Distinct())
                RequireColumn(data, column);
            foreach (string column in TimingColumns)
                RequireColumn(data, column);

            foreach (DataRow row in data.Rows)
            {
                string scenario = GetScenario(row);
                DateTime date = Date(row);
                string dateText = date == DateTime.MinValue ? "unknown date" : date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                foreach (string column in FiniteColumns)
                {
                    double value = Get(row, column);
                    if (double.IsNaN(value) || double.IsInfinity(value))
                        throw new Exception($"{scenario} failed finite output check for {column} on {dateText}: value={value}");
                }

                foreach (string column in NonNegativeColumns)
                {
                    double value = Get(row, column);
                    if (value < -Epsilon)
                        throw new Exception($"{scenario} failed non-negative output check for {column} on {dateText}: value={value:G17}");
                }

                foreach (string column in UnitIntervalColumns)
                {
                    double value = Get(row, column);
                    if (value < -Epsilon || value > 1.0 + Epsilon)
                        throw new Exception($"{scenario} failed 0-1 bounds check for {column} on {dateText}: value={value:G17}");
                }

                double reservePool = Get(row, "ReservePoolDMOutput");
                double reserveCapacity = Get(row, "ReserveCapacityDMOutput");
                if (reservePool > reserveCapacity + 1e-6)
                    throw new Exception($"{scenario} failed reserve pool capacity check on {dateText}: pool={reservePool:G17}, capacity={reserveCapacity:G17}");
            }
        }

        private static void CheckTimingSensibility(DataTable data)
        {
            var bySimulation = data.Rows.Cast<DataRow>()
                .GroupBy(GetScenario)
                .ToDictionary(group => group.Key, group => group.OrderBy(Date).ToList());

            foreach (KeyValuePair<string, List<DataRow>> item in bySimulation)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                    continue;

                CheckReproductiveStageOrder(item.Key, item.Value);
                CheckFlowerNumberTiming(item.Key, item.Value);
                CheckHarvestReadinessTiming(item.Key, item.Value);
            }

            CheckDormancyForcingTiming(bySimulation);
        }

        private static void CheckReproductiveStageOrder(string scenario, List<DataRow> rows)
        {
            int previousRank = -1;
            string previousStage = string.Empty;

            for (int i = 0; i < rows.Count; i++)
            {
                string stage = Stage(rows[i]);
                if (StageEquals(stage, "Dormancy") || StageEquals(stage, "BudBreak"))
                {
                    previousRank = StageEquals(stage, "BudBreak") ? 0 : -1;
                    previousStage = stage;
                    continue;
                }

                int currentRank = Array.FindIndex(ReproductiveStageOrder, expected => StageEquals(stage, expected));
                if (currentRank < 0)
                    continue;

                if (previousRank >= 0 && currentRank <= previousRank)
                    throw new Exception($"{scenario} failed phenology transition order check on {DateText(rows[i])}: {stage} followed {previousStage} without a seasonal reset.");

                previousRank = currentRank;
                previousStage = stage;
            }
        }

        private static void CheckFlowerNumberTiming(string scenario, List<DataRow> rows)
        {
            bool floweringReachedInCycle = false;
            for (int i = 0; i < rows.Count; i++)
            {
                DataRow row = rows[i];
                string stage = Stage(row);
                if (StageEquals(stage, "Dormancy") || StageEquals(stage, "BudBreak"))
                    floweringReachedInCycle = false;
                if (StageEquals(stage, "Flowering"))
                    floweringReachedInCycle = true;

                double nflower = Get(row, "Arbitrator.fruitOrgan.Nflower");
                if (nflower > Epsilon && !floweringReachedInCycle)
                    throw new Exception($"{scenario} failed Nflower timing check on {DateText(row)}: Nflower={nflower:G17} before flowering in the current seasonal cycle.");
            }
        }

        private static void CheckHarvestReadinessTiming(string scenario, List<DataRow> rows)
        {
            bool maturityReached = false;
            bool nonPhenologyHarvestAllowed = IsNonPhenologyHarvestScenario(scenario);

            foreach (DataRow row in rows)
            {
                if (IsStage(row, "Maturity") || GetBool(row, "Arbitrator.fruitOrgan.IsHarvestable"))
                    maturityReached = true;

                if (GetBool(row, "IsReadyForHarvesting") && !maturityReached && !nonPhenologyHarvestAllowed)
                    throw new Exception($"{scenario} failed harvest readiness timing check on {DateText(row)}: readiness occurred before maturity without a configured non-phenology harvest rule.");
            }
        }

        private static bool IsNonPhenologyHarvestScenario(string scenario)
        {
            return string.Equals(scenario, "Harvest_QualityThreshold", StringComparison.OrdinalIgnoreCase)
                || string.Equals(scenario, "Harvest_NoCriteriaFallback", StringComparison.OrdinalIgnoreCase);
        }

        private static void CheckDormancyForcingTiming(Dictionary<string, List<DataRow>> rowsBySimulation)
        {
            Require(rowsBySimulation, "Dormancy_InsufficientChill");
            Require(rowsBySimulation, "Dormancy_AdequateChill");
            Require(rowsBySimulation, "Forcing_LowHeat");
            Require(rowsBySimulation, "Forcing_AdequateHeat");

            CheckBlockedBeforeFlowering("Dormancy_InsufficientChill", rowsBySimulation["Dormancy_InsufficientChill"]);
            CheckBlockedBeforeFlowering("Forcing_LowHeat", rowsBySimulation["Forcing_LowHeat"]);

            CheckGateSequence("Dormancy_AdequateChill", rowsBySimulation["Dormancy_AdequateChill"]);
            CheckGateSequence("Forcing_AdequateHeat", rowsBySimulation["Forcing_AdequateHeat"]);
        }

        private static void CheckBlockedBeforeFlowering(string scenario, List<DataRow> rows)
        {
            foreach (string stage in new[] { "Flowering", "FruitSet", "FruitFill", "Maturity" })
            {
                int index = FirstIndex(rows, row => IsStage(row, stage));
                if (index >= 0)
                    throw new Exception($"{scenario} failed dormancy/forcing gate timing check: blocked scenario reached {stage} on {DateText(rows[index])}.");
            }
        }

        private static void CheckGateSequence(string scenario, List<DataRow> rows)
        {
            int chillIndex = FirstIndex(rows, row => Get(row, "ChillRequirementSatisfiedTodayOutput") > 0.0);
            int forcingIndex = FirstIndex(rows, row => Get(row, "ForcingRequirementSatisfiedTodayOutput") > 0.0);
            int floweringIndex = FirstIndex(rows, row => IsStage(row, "Flowering"));

            if (chillIndex < 0 || forcingIndex < 0 || floweringIndex < 0)
                throw new Exception($"{scenario} failed dormancy/forcing gate sequence check: chillIndex={chillIndex}, forcingIndex={forcingIndex}, floweringIndex={floweringIndex}.");
            if (forcingIndex <= chillIndex)
                throw new Exception($"{scenario} failed dormancy/forcing gate sequence check: forcingIndex={forcingIndex} did not occur after chillIndex={chillIndex}.");
            if (floweringIndex <= forcingIndex)
                throw new Exception($"{scenario} failed dormancy/forcing gate sequence check: floweringIndex={floweringIndex} did not occur after forcingIndex={forcingIndex}.");
        }


        private static void CheckReserveDiagnostics(Dictionary<string, Metrics> metrics)
        {
            string[] capacityNames = { "Reserves_LowCapacity", "Reserves_MediumCapacity", "Reserves_HighCapacity" };
            string[] storageNames = { "Reserves_LowStorageRate", "Reserves_MediumStorageRate", "Reserves_HighStorageRate" };
            string[] mobilisationNames = { "Reserves_LowMobilisation", "Reserves_MediumMobilisation", "Reserves_HighMobilisation" };

            CheckStrictAscending(metrics, capacityNames, m => m.MaxReserveCapacity, "reserve capacity treatment");
            CheckStrictAscending(metrics, capacityNames, m => m.MaxReservePool, "possible stored reserve with capacity treatment");
            CheckStrictAscending(metrics, storageNames, m => m.CumulativeReserveStored, "cumulative reserve storage with storage-rate treatment");
            CheckStrictAscending(metrics, mobilisationNames, m => m.CumulativeReserveMobilised, "cumulative reserve mobilisation with mobilisation-rate treatment");

            const string highSourceLowSink = "Reserves_HighSourceLowSink";
            const string lowSourceHighSink = "Reserves_LowSourceHighSink";
            Require(metrics, highSourceLowSink);
            Require(metrics, lowSourceHighSink);
            CheckGreaterThan(highSourceLowSink, "cumulative surplus vs low-source high-sink contrast",
                metrics[highSourceLowSink].CumulativeReserveSurplus, lowSourceHighSink, metrics[lowSourceHighSink].CumulativeReserveSurplus);
            CheckLessThan(highSourceLowSink, "cumulative deficit vs low-source high-sink contrast",
                metrics[highSourceLowSink].CumulativeReserveDeficit, lowSourceHighSink, metrics[lowSourceHighSink].CumulativeReserveDeficit);

            foreach (string name in capacityNames.Concat(storageNames).Concat(mobilisationNames).Concat(new[] { highSourceLowSink, lowSourceHighSink }))
            {
                Require(metrics, name);
                RequirePositive(name, "live structural wood diagnostic", metrics[name].MaxLiveStructuralWood);
                RequirePositive(name, "reserve capacity diagnostic", metrics[name].MaxReserveCapacity);
            }
        }


        private static void CheckPruningResponse(DataTable data)
        {
            string[] names = { "Pruning_None", "Pruning_Moderate", "Pruning_Heavy" };
            DateTime eventDate = FirstDateForDayOfYear(data, names, 210);
            DateTime nextDate = eventDate.AddDays(1);

            DataRow[] eventRows = names.Select(name => GetRow(data, name, eventDate, "Pruning response check")).ToArray();
            DataRow[] nextRows = names.Select(name => GetRow(data, name, nextDate, "Pruning response check")).ToArray();

            CheckStrictAscendingRows(eventRows, names, "FruitTree.PruningLeafFractionOutput", "event-day pruning leaf fraction");
            CheckStrictAscendingRows(eventRows, names, "FruitTree.PruningStructuralFractionOutput", "event-day pruning structural fraction");
            CheckStrictAscendingRows(eventRows, names, "FruitTree.ActiveStructuralPruningFractionOutput", "event-day active structural pruning fraction");

            CheckStrictDescendingRows(eventRows, names, "BiomassLeavesOutput", "event-day leaf biomass");
            CheckStrictDescendingRows(eventRows, names, "Arbitrator.leafOrgan.LAI", "event-day LAI");

            CheckStrictDescendingRows(nextRows, names, "FruitTree.CanopyStructuralLightFactorOutput", "next-day structural light factor");
            CheckStrictDescendingRows(nextRows, names, "FruitTree.CanopyStructuralSizeFactorOutput", "next-day structural size factor");
            CheckStrictDescendingRows(nextRows, names, "Arbitrator.leafOrgan.RadiationIntercepted", "next-day radiation interception");
        }

        private static void CheckThinningResponse(DataTable data)
        {
            string[] names = { "Thinning_None", "Thinning_Moderate", "Thinning_Heavy" };
            DateTime[] eventDates = data.Rows.Cast<DataRow>()
                .Where(row => names.Contains(GetScenario(row)) && DayOfYear(row) == 55)
                .Select(row => Date(row).Date)
                .Where(date => date != DateTime.MinValue)
                .Distinct()
                .OrderBy(date => date)
                .ToArray();

            if (eventDates.Length == 0)
                throw new Exception("Thinning response check failed: no Report rows found for day-of-year 55.");

            foreach (DateTime eventDate in eventDates)
            {
                DateTime previousDate = eventDate.AddDays(-1);
                DataRow[] eventRows = names.Select(name => GetRow(data, name, eventDate, "Thinning response check")).ToArray();
                DataRow[] previousRows = names.Select(name => GetRow(data, name, previousDate, "Thinning response check")).ToArray();

                CheckStrictDescendingRows(eventRows, names, "Arbitrator.fruitOrgan.Nfruit", "event-day retained fruit number after thinning");
                CheckStrictDescendingRows(eventRows, names, "BiomassFruitsOutput", "event-day fruit biomass after thinning");

                for (int i = 1; i < names.Length; i++)
                {
                    CheckEventDrop(names[i], eventDate, "Arbitrator.fruitOrgan.Nfruit", previousRows[i], eventRows[i], "retained fruit number");
                    CheckEventDrop(names[i], eventDate, "BiomassFruitsOutput", previousRows[i], eventRows[i], "fruit biomass");
                    CheckNoPostThinningNfruitRefill(data, names[i], eventDate, Get(eventRows[i], "Arbitrator.fruitOrgan.Nfruit"));
                }
            }
        }

        private static void CheckEventDrop(string scenario, DateTime eventDate, string column, DataRow previousRow, DataRow eventRow, string label)
        {
            double previous = Get(previousRow, column);
            double current = Get(eventRow, column);
            if (current >= previous - Epsilon)
                throw new Exception($"{scenario} failed event-day thinning {label} drop check on {eventDate:yyyy-MM-dd}: previous={previous:G17}, eventDay={current:G17}");
        }

        private static void CheckNoPostThinningNfruitRefill(DataTable data, string scenario, DateTime eventDate, double eventDayNfruit)
        {
            double postEventMax = Max(data.Rows.Cast<DataRow>()
                .Where(row => GetScenario(row) == scenario
                              && Date(row).Date > eventDate.Date
                              && Date(row).Year == eventDate.Year
                              && DayOfYear(row) <= 260),
                "Arbitrator.fruitOrgan.Nfruit");

            if (postEventMax > eventDayNfruit + Epsilon)
                throw new Exception($"{scenario} failed post-thinning no-refill check after fruit set closed on {eventDate:yyyy-MM-dd}: eventDayNfruit={eventDayNfruit:G17}, postEventMax={postEventMax:G17}");
        }

        private static DateTime FirstDateForDayOfYear(DataTable data, string[] names, int dayOfYear)
        {
            DateTime[] dates = data.Rows.Cast<DataRow>()
                .Where(row => names.Contains(GetScenario(row)) && DayOfYear(row) == dayOfYear)
                .Select(row => Date(row).Date)
                .Where(date => date != DateTime.MinValue)
                .Distinct()
                .OrderBy(date => date)
                .ToArray();

            if (dates.Length == 0)
                throw new Exception($"Pruning response check failed: no Report rows found for day-of-year {dayOfYear}.");
            return dates[0];
        }

        private static DataRow GetRow(DataTable data, string simulation, DateTime date, string checkName)
        {
            DataRow[] rows = data.Rows.Cast<DataRow>()
                .Where(row => GetScenario(row) == simulation && Date(row).Date == date.Date)
                .ToArray();

            if (rows.Length != 1)
                throw new Exception($"{checkName} failed: expected one row for {simulation} on {date:yyyy-MM-dd}, found {rows.Length}.");
            return rows[0];
        }

        private static void CheckStrictAscendingRows(DataRow[] rows, string[] names, string column, string label)
        {
            for (int i = 1; i < rows.Length; i++)
            {
                double previous = Get(rows[i - 1], column);
                double current = Get(rows[i], column);
                if (current <= previous + Epsilon)
                    throw new Exception($"{names[i]} failed ascending {label} check for {column}: previousScenario={names[i - 1]}, previous={previous:G17}, current={current:G17}");
            }
        }

        private static void CheckStrictDescendingRows(DataRow[] rows, string[] names, string column, string label)
        {
            for (int i = 1; i < rows.Length; i++)
            {
                double previous = Get(rows[i - 1], column);
                double current = Get(rows[i], column);
                if (current >= previous - Epsilon)
                    throw new Exception($"{names[i]} failed descending {label} check for {column}: previousScenario={names[i - 1]}, previous={previous:G17}, current={current:G17}");
            }
        }

        private static void CheckHarvestRules(Dictionary<string, Metrics> metrics)
        {
            Require(metrics, "Harvest_Phenology");
            Require(metrics, "Harvest_QualityThreshold");
            Require(metrics, "Harvest_NoCriteriaFallback");

            if (metrics["Harvest_QualityThreshold"].FirstReadyIndex <= metrics["Harvest_QualityThreshold"].FirstFruitIndex)
                throw new Exception($"Harvest_QualityThreshold failed readiness timing check: firstReadyIndex={metrics["Harvest_QualityThreshold"].FirstReadyIndex}, firstFruitIndex={metrics["Harvest_QualityThreshold"].FirstFruitIndex}");
            if (metrics["Harvest_NoCriteriaFallback"].FirstReadyIndex < 0)
                throw new Exception("Harvest_NoCriteriaFallback failed fallback readiness check: scenario never became ready after fruit was present.");
            CheckGreaterThan("Harvest_Phenology", "seasonal fruit biomass vs quality threshold", metrics["Harvest_Phenology"].MaxFruit, "Harvest_QualityThreshold", metrics["Harvest_QualityThreshold"].MaxFruit);
            CheckGreaterThan("Harvest_QualityThreshold", "seasonal fruit biomass vs no-criteria fallback", metrics["Harvest_QualityThreshold"].MaxFruit, "Harvest_NoCriteriaFallback", metrics["Harvest_NoCriteriaFallback"].MaxFruit);
        }

        private static void CheckDormancyForcingDiagnostics(Dictionary<string, Metrics> metrics)
        {
            Require(metrics, "Dormancy_InsufficientChill");
            Require(metrics, "Dormancy_AdequateChill");
            Require(metrics, "Forcing_LowHeat");
            Require(metrics, "Forcing_AdequateHeat");

            Metrics insufficientChill = metrics["Dormancy_InsufficientChill"];
            Metrics adequateChill = metrics["Dormancy_AdequateChill"];
            Metrics lowHeat = metrics["Forcing_LowHeat"];
            Metrics adequateHeat = metrics["Forcing_AdequateHeat"];

            RequirePositive("Dormancy_InsufficientChill", "dormancy phase diagnostic", insufficientChill.MaxDormancyPhaseActive);
            if (insufficientChill.ChillSatisfiedDays > 0.0 || insufficientChill.MaxChillUnitsAtTransition > 0.0)
                throw new Exception($"Dormancy_InsufficientChill failed chill blocking check: chillSatisfiedDays={insufficientChill.ChillSatisfiedDays:G17}, chillAtTransition={insufficientChill.MaxChillUnitsAtTransition:G17}");
            if (insufficientChill.MaxForcingPhaseActive > 0.0 || insufficientChill.ForcingSatisfiedDays > 0.0)
                throw new Exception($"Dormancy_InsufficientChill failed no-forcing-entry check: forcingPhaseActive={insufficientChill.MaxForcingPhaseActive:G17}, forcingSatisfiedDays={insufficientChill.ForcingSatisfiedDays:G17}");

            RequirePositive("Dormancy_AdequateChill", "chill transition diagnostic", adequateChill.MaxChillUnitsAtTransition);
            RequirePositive("Dormancy_AdequateChill", "chill satisfied-day diagnostic", adequateChill.ChillSatisfiedDays);
            RequirePositive("Dormancy_AdequateChill", "forcing transition diagnostic", adequateChill.MaxForcingUnitsAtTransition);
            RequirePositive("Dormancy_AdequateChill", "forcing satisfied-day diagnostic", adequateChill.ForcingSatisfiedDays);

            RequirePositive("Forcing_LowHeat", "chill transition diagnostic", lowHeat.MaxChillUnitsAtTransition);
            RequirePositive("Forcing_LowHeat", "forcing phase diagnostic", lowHeat.MaxForcingPhaseActive);
            if (lowHeat.ForcingSatisfiedDays > 0.0 || lowHeat.MaxForcingUnitsAtTransition > 0.0)
                throw new Exception($"Forcing_LowHeat failed forcing blocking check: forcingSatisfiedDays={lowHeat.ForcingSatisfiedDays:G17}, forcingAtTransition={lowHeat.MaxForcingUnitsAtTransition:G17}");

            RequirePositive("Forcing_AdequateHeat", "chill transition diagnostic", adequateHeat.MaxChillUnitsAtTransition);
            RequirePositive("Forcing_AdequateHeat", "forcing transition diagnostic", adequateHeat.MaxForcingUnitsAtTransition);
            RequirePositive("Forcing_AdequateHeat", "forcing satisfied-day diagnostic", adequateHeat.ForcingSatisfiedDays);
        }

        private static void CheckHydraulicConductanceDiagnostic(Dictionary<string, Metrics> metrics)
        {
            string[] names = { "Hydraulics_LowKplant", "Hydraulics_MediumKplant", "Hydraulics_HighKplant" };
            foreach (string name in names)
                Require(metrics, name);

            CheckAscending(metrics, names, m => m.MeanHydraulicPlantConductance, "Kplant treatment order");
            CheckAscending(metrics, names, m => m.MeanConductance, "soil-plant total conductance diagnostic");
            CheckAscending(metrics, names, m => m.MeanHydraulicReferenceConductance, "reference conductance diagnostic");

            if (metrics["Hydraulics_LowKplant"].MeanHydraulicPlantLimited < 0.99)
                throw new Exception($"Hydraulics_LowKplant failed plant-limited diagnostic check: plantLimitedFraction={metrics["Hydraulics_LowKplant"].MeanHydraulicPlantLimited:G17}");

            foreach (string name in new[] { "Hydraulics_MediumKplant", "Hydraulics_HighKplant" })
            {
                if (metrics[name].MeanHydraulicPlantLimited > 0.01)
                    throw new Exception($"{name} failed non-plant-limited diagnostic check: plantLimitedFraction={metrics[name].MeanHydraulicPlantLimited:G17}");
            }

            CheckAscending(metrics, names, m => m.MeanWaterDemand, "water demand context");
            CheckAscending(metrics, names, m => m.MeanWaterSupply, "realised water supply context");
        }

        private static void CheckHydraulicGrowthResponseDiagnostic(Dictionary<string, Metrics> metrics)
        {
            const string lowName = "HydraulicGrowth_LowKplant";
            const string highName = "HydraulicGrowth_HighKplant";
            Require(metrics, lowName);
            Require(metrics, highName);

            Metrics low = metrics[lowName];
            Metrics high = metrics[highName];

            CheckLessThan(lowName, "Kplant treatment order", low.MeanHydraulicPlantConductance, highName, high.MeanHydraulicPlantConductance);
            CheckGreaterThan(lowName, "plant-limited hydraulic diagnostic vs high Kplant", low.MeanHydraulicPlantLimited, highName, high.MeanHydraulicPlantLimited);

            if (low.MeanHydraulicPlantLimited < high.MeanHydraulicPlantLimited + 0.5)
                throw new Exception($"{lowName} failed stronger plant-side hydraulic limitation check: lowPlantLimited={low.MeanHydraulicPlantLimited:G17}, highPlantLimited={high.MeanHydraulicPlantLimited:G17}");

            // Supply/growth outputs remain diagnostic context here; the memo classifies
            // the scenario as diagnostic-only when these local stress signals do not separate.
        }

        private static Metrics Summarise(List<DataRow> rows)
        {
            DataRow last = rows.Last();
            return new Metrics
            {
                Rows = rows.Count,
                MaxLeaf = Max(rows, "BiomassLeavesOutput"),
                MaxFruit = Max(rows, "BiomassFruitsOutput"),
                FinalFruit = Get(last, "BiomassFruitsOutput"),
                FinalFruitDeadOrSenesced = Get(last, "Arbitrator.fruitOrgan.Dead.Wt") + Get(last, "Arbitrator.fruitOrgan.Senesced.Wt"),
                MaxFruitAfterThinWindow = Max(rows.Where(IsShortlyAfterThinningWindow), "BiomassFruitsOutput"),
                MeanSupply = Mean(rows, "SupplyIndexEffectiveOutput"),
                MeanGrowth = Mean(rows, "GrowthStressModifierOutput"),
                MeanPAW = Mean(rows, "RootZonePAWFractionOutput"),
                MeanConductance = Mean(rows, "SoilPlantConductanceOutput"),
                MeanHydraulicPlantConductance = Mean(rows, "HydraulicPlantConductanceOutput"),
                MeanHydraulicReferenceConductance = Mean(rows, "HydraulicReferenceConductanceOutput"),
                MeanHydraulicKTerm = Mean(rows, "HydraulicKTermOutput"),
                MeanHydraulicPlantLimited = Mean(rows, "HydraulicPlantLimitedOutput"),
                MeanLeafWaterStress = Mean(rows, "Arbitrator.leafOrgan.Fw"),
                MeanWaterDemand = Mean(rows, "Arbitrator.waterUptakeMethod.WDemand"),
                MeanWaterSupply = Mean(rows, "Arbitrator.waterUptakeMethod.WSupply"),
                MeanFruitWater = Mean(rows, "FruitWaterFractionOutput"),
                MaxBrix = Max(rows, "QualityBrix"),
                MaxDMPct = Max(rows, "QualityDMPctOutput"),
                MaxAcid = Max(rows, "QualityAcidPctOutput"),
                MaxBudLoad = Max(rows, "BudLoadPerAreaOutput"),
                MaxFlower = Max(rows, "Arbitrator.fruitOrgan.Nflower"),
                MaxNfruit = Max(rows, "Arbitrator.fruitOrgan.Nfruit"),
                MaxSetRate = Max(rows, "ReproSetRateOutput"),
                MaxReserveCapacity = Max(rows, "ReserveCapacityDMOutput"),
                MaxReservePool = Max(rows, "ReservePoolDMOutput"),
                MaxReserveStored = Max(rows, "ReserveStoredDMOutput"),
                MaxReserveMobilised = Max(rows, "ReserveMobilisedDMOutput"),
                CumulativeReserveStored = Sum(rows, "ReserveStoredDMOutput"),
                CumulativeReserveMobilised = Sum(rows, "ReserveMobilisedDMOutput"),
                CumulativeReserveSurplus = Max(rows, "OrchardManagement.Script.ReserveCumulativeSurplusDMOutput"),
                CumulativeReserveDeficit = Max(rows, "OrchardManagement.Script.ReserveCumulativeDeficitDMOutput"),
                MaxReservePoolCapacityRatio = Max(rows, "OrchardManagement.Script.ReservePoolCapacityRatioOutput"),
                MaxLiveStructuralWood = Max(rows, "Arbitrator.woodOrgan.Live.StructuralWt"),
                MaxWoodSenesced = Max(rows, "Arbitrator.woodOrgan.Senesced.Wt"),
                MeanWoodStructuralDemand = Mean(rows, "Arbitrator.woodOrgan.DMDemand.Structural"),
                FirstFruitIndex = FirstIndex(rows, row => Get(row, "BiomassFruitsOutput") > 0.0),
                FirstReadyIndex = FirstIndex(rows, row => GetBool(row, "IsReadyForHarvesting")),
                FirstHarvestableIndex = FirstIndex(rows, row => GetBool(row, "Arbitrator.fruitOrgan.IsHarvestable")),
                FloweringEntries = CountStageEntries(rows, "Flowering"),
                FruitSetEntries = CountStageEntries(rows, "FruitSet"),
                DormancyEntries = CountStageEntries(rows, "Dormancy"),
                MaxDormancyPhaseActive = Max(rows, "DormancyPhaseActiveOutput"),
                MaxForcingPhaseActive = Max(rows, "ForcingPhaseActiveOutput"),
                MaxChillUnitsAccumulated = Max(rows, "ChillUnitsAccumulatedOutput"),
                MaxChillUnitsAtTransition = Max(rows, "ChillUnitsAtLastTransitionOutput"),
                ChillSatisfiedDays = Sum(rows, "ChillRequirementSatisfiedTodayOutput"),
                MaxForcingUnitsAccumulated = Max(rows, "ForcingUnitsAccumulatedOutput"),
                MaxForcingUnitsAtTransition = Max(rows, "ForcingUnitsAtLastTransitionOutput"),
                ForcingSatisfiedDays = Sum(rows, "ForcingRequirementSatisfiedTodayOutput"),
                SecondSeasonMaxFruit = Max(rows.Where(row => Date(row) > new DateTime(1998, 12, 1)), "BiomassFruitsOutput")
            };
        }

        private static bool IsShortlyAfterThinningWindow(DataRow row)
        {
            int day = DayOfYear(row);
            return day >= 55 && day <= 120;
        }

        private static int DayOfYear(DataRow row)
        {
            DateTime date = Date(row);
            return date == DateTime.MinValue ? 0 : date.DayOfYear;
        }

        private static DateTime Date(DataRow row)
        {
            if (!row.Table.Columns.Contains("Clock.Today") || row["Clock.Today"] == DBNull.Value)
                return DateTime.MinValue;
            object raw = row["Clock.Today"];
            if (raw is DateTime date)
                return date;
            if (DateTime.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                return parsed;
            return DateTime.MinValue;
        }

        private static int CountStageEntries(List<DataRow> rows, string stage)
        {
            int count = 0;
            string previous = string.Empty;
            foreach (DataRow row in rows)
            {
                string current = GetString(row, "Phenology.CurrentStageName");
                if (current == stage && previous != stage)
                    count++;
                previous = current;
            }
            return count;
        }

        private static int FirstIndex(List<DataRow> rows, Func<DataRow, bool> predicate)
        {
            for (int i = 0; i < rows.Count; i++)
                if (predicate(rows[i]))
                    return i;
            return -1;
        }

        private static void CheckDescending(Dictionary<string, Metrics> metrics, string[] names, Func<Metrics, double> selector, string label)
        {
            foreach (string name in names)
                Require(metrics, name);
            for (int i = 1; i < names.Length; i++)
            {
                double previous = selector(metrics[names[i - 1]]);
                double current = selector(metrics[names[i]]);
                if (current > previous + Epsilon)
                    throw new Exception($"{names[i]} failed descending {label} check: previousScenario={names[i - 1]}, previous={previous:G17}, current={current:G17}");
            }
        }

        private static void CheckAscending(Dictionary<string, Metrics> metrics, string[] names, Func<Metrics, double> selector, string label)
        {
            foreach (string name in names)
                Require(metrics, name);
            for (int i = 1; i < names.Length; i++)
            {
                double previous = selector(metrics[names[i - 1]]);
                double current = selector(metrics[names[i]]);
                if (current + Epsilon < previous)
                    throw new Exception($"{names[i]} failed ascending {label} check: previousScenario={names[i - 1]}, previous={previous:G17}, current={current:G17}");
            }
        }

        private static void CheckStrictAscending(Dictionary<string, Metrics> metrics, string[] names, Func<Metrics, double> selector, string label)
        {
            foreach (string name in names)
                Require(metrics, name);
            for (int i = 1; i < names.Length; i++)
            {
                double previous = selector(metrics[names[i - 1]]);
                double current = selector(metrics[names[i]]);
                if (current <= previous + Epsilon)
                    throw new Exception($"{names[i]} failed strict ascending {label} check: previousScenario={names[i - 1]}, previous={previous:G17}, current={current:G17}");
            }
        }

        private static void CheckLessThan(string scenario, string check, double value, string comparisonScenario, double comparisonValue)
        {
            if (value >= comparisonValue - Epsilon)
                throw new Exception($"{scenario} failed {check}: {scenario}={value:G17}, {comparisonScenario}={comparisonValue:G17}");
        }

        private static void CheckGreaterThan(string scenario, string check, double value, string comparisonScenario, double comparisonValue)
        {
            if (value <= comparisonValue + Epsilon)
                throw new Exception($"{scenario} failed {check}: {scenario}={value:G17}, {comparisonScenario}={comparisonValue:G17}");
        }

        private static void CheckApproximatelyEqual(string scenario, string check, double value, double expected, double relativeTolerance)
        {
            double scale = Math.Max(1.0, Math.Abs(expected));
            double difference = Math.Abs(value - expected);
            if (difference > relativeTolerance * scale + Epsilon)
                throw new Exception($"{scenario} failed {check}: value={value:G17}, expected={expected:G17}, relativeTolerance={relativeTolerance:G17}");
        }

        private static void Require(Dictionary<string, Metrics> metrics, string name)
        {
            if (!metrics.ContainsKey(name))
                throw new Exception($"{name} failed scenario presence check: missing from Report table.");
        }

        private static void Require(Dictionary<string, List<DataRow>> rowsBySimulation, string name)
        {
            if (!rowsBySimulation.ContainsKey(name))
                throw new Exception($"{name} failed scenario presence check: missing from Report table.");
        }

        private static void RequirePositive(string scenario, string check, double value)
        {
            if (value <= 0.0 || double.IsNaN(value) || double.IsInfinity(value))
                throw new Exception($"{scenario} failed {check} check: value={value:G17}");
        }

        private static void RequireColumn(DataTable table, string column)
        {
            if (!table.Columns.Contains(column))
                throw new Exception($"PostSimulationTests failed Report schema check: missing required column '{column}'.");
        }

        private static double Max(IEnumerable<DataRow> rows, string column) => rows.Select(row => Get(row, column)).DefaultIfEmpty(0.0).Max();
        private static double Sum(IEnumerable<DataRow> rows, string column) => rows.Select(row => Get(row, column)).Where(value => !double.IsNaN(value)).DefaultIfEmpty(0.0).Sum();
        private static double Mean(IEnumerable<DataRow> rows, string column) => rows.Select(row => Get(row, column)).Where(value => !double.IsNaN(value)).DefaultIfEmpty(0.0).Average();

        private static double Get(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return 0.0;
            if (double.TryParse(Convert.ToString(row[column], CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return value;
            return 0.0;
        }

        private static bool GetBool(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return false;
            object raw = row[column];
            if (raw is bool b)
                return b;
            string text = Convert.ToString(raw, CultureInfo.InvariantCulture);
            return string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || text == "1";
        }

        private static string GetString(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return string.Empty;
            return Convert.ToString(row[column], CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string Stage(DataRow row) => GetString(row, "Phenology.CurrentStageName");
        private static bool IsStage(DataRow row, string stage) => StageEquals(Stage(row), stage);
        private static bool StageEquals(string actual, string expected) => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        private static string DateText(DataRow row)
        {
            DateTime date = Date(row);
            return date == DateTime.MinValue ? "unknown date" : date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static string GetScenario(DataRow row) => GetString(row, "SimulationName");
    }
}
