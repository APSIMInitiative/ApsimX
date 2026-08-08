using System;
using APSIM.Core;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Phen;
using Models.PMF.Organs;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.Agroforestry
{
    /// <summary>
    /// A reproductive organ for fruit trees using a single smooth cohort lifecycle.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(GenericFruitTree))]
    public class FruitOrgan : ReproductiveOrgan
    {
        /// <summary>
        /// Link to the parent tree so we can retrieve stage-name settings and stress drivers.
        /// </summary>
        [Link(Type = LinkType.Ancestor)]
        private GenericFruitTree tree = null!;

        /// <summary>
        /// Function that returns potential flower number per unit area.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction NumberFunction = null!;

        // --- Demand functions (duplicated links because base class handlers are private) ---
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction DMDemandFunction = null!;

        [Link(Type = LinkType.Child, ByName = true)]
        private NutrientPoolFunctions dmDemandPriorityFactors = null!;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction NFillingRate = null!;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction MaximumNConc = null!;

        [Link]
        private IClock clock = null!;

        private const double Epsilon = 1e-9;

        private bool fruitSetWindowActive;
        private bool fruitFillWindowActive;
        private bool isHarvestable;
        private DateTime lastCohortUpdateDate = DateTime.MinValue;
        private double dailySeedDMDemand_g_m2;
        private double dailySetFlux_fruit_m2;
        private double dailyDropFlux_fruit_m2;

        /// <summary>Duration of fruit set/retention window.</summary>
        [Description("Duration of fruit set/retention window (species default / derived behavior; excluded from compact calibration)")]
        [Units("d")]
        public double FruitSetDurationDays { get; set; } = 14.0;

        /// <summary>Maximum fraction of flowers retained as fruit.</summary>
        [Description("Maximum fraction of flowers retained as fruit (0-1) (species default / derived behavior; excluded from compact calibration)")]
        [Units("0-1")]
        public double FruitSetMaxFrac { get; set; } = 0.7;

        /// <summary>Baseline daily fruit abscission rate during set and fill.</summary>
        [Description("Baseline daily fruit drop rate during set and fill (species default / derived behavior; excluded from compact calibration)")]
        [Units("1/d")]
        public double FruitDropRate { get; set; } = 0.03;

        /// <summary>Leaf-water-stress midpoint for fruit set response.</summary>
        [Description("Leaf water stress midpoint for fruit set response (species default / derived behavior; excluded from compact calibration)")]
        [Units("0-1")]
        public double Fw50_Set { get; set; } = 0.5;

        /// <summary>Leaf-water-stress slope for fruit set response.</summary>
        [Description("Leaf water stress slope for fruit set response (species default / derived behavior; excluded from compact calibration)")]
        [Units("-")]
        public double FwSlope_Set { get; set; } = 12.0;

        /// <summary>Seed dry matter added per newly retained fruit.</summary>
        [Description("Seed dry matter added per newly set fruit (species default / derived behavior; excluded from compact calibration)")]
        [Units("g/fruit")]
        public double FruitSeedDM_gPerFruit { get; set; } = 0.05;

        /// <summary>Optional post-maturity daily fruit drop rate.</summary>
        [Description("Optional post-maturity fruit drop rate (species default / derived behavior; excluded from compact calibration)")]
        [Units("1/d")]
        public double PostMaturityDropRate { get; set; } = 0.0;

        /// <summary>Potential reproductive units formed per bud.</summary>
        [Description("Potential reproductive units formed per bud")]
        [Units("fruit/bud")]
        public double ReproPotentialPerBud { get; set; } = 1.0;

        /// <summary>Maximum daily set kinetics coefficient.</summary>
        [Description("Maximum daily fruit set kinetics coefficient")]
        [Units("1/d")]
        public double SetRateMax { get; set; } = 0.35;

        /// <summary>Base daily fruit drop kinetics coefficient.</summary>
        [Description("Base daily fruit drop kinetics coefficient")]
        [Units("1/d")]
        public double DropRateBase { get; set; } = 0.01;

        /// <summary>Additional daily drop coefficient per unit stress deficit.</summary>
        [Description("Additional daily fruit drop coefficient per unit stress deficit")]
        [Units("1/d")]
        public double DropStressSensitivity { get; set; } = 0.08;

        /// <summary>Potential fruit dry matter target per fruit at maturity.</summary>
        [Description("Potential fruit dry matter target per fruit at maturity")]
        [Units("g/fruit")]
        public double PotentialFruitSizeDM { get; set; } = 6.0;

        /// <summary>Potential flowers per m2 for the active season.</summary>
        [JsonIgnore]
        [Units("flower/m^2")]
        public double Nflower { get; private set; }

        /// <summary>Retained fruit number per m2 for the active cohort.</summary>
        [JsonIgnore]
        [Units("fruit/m^2")]
        public double Nfruit { get; private set; }

        /// <summary>Total fruit dry matter for the active cohort (g/m2).</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double FruitDM_g_m2 { get; private set; }

        /// <summary>Accumulated thermal time since fruit set began.</summary>
        [JsonIgnore]
        [Units("oCd")]
        public double FruitAgeTT { get; private set; }

        /// <summary>True while the seasonal cohort is active.</summary>
        [JsonIgnore]
        public bool InReproSeason { get; private set; }

        /// <summary>True once maturity stage is reached.</summary>
        [JsonIgnore]
        public bool IsHarvestable => isHarvestable;

        /// <summary>Daily set flux diagnostic (fruit/m^2/day).</summary>
        [JsonIgnore]
        public double ReproSetRateOutput => Math.Max(0.0, dailySetFlux_fruit_m2);

        /// <summary>Daily drop/senescence flux diagnostic (fruit/m^2/day).</summary>
        [JsonIgnore]
        public double ReproDropRateOutput => Math.Max(0.0, dailyDropFlux_fruit_m2);

        /// <summary>
        /// Reset cohort state at simulation start.
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencingCohort(object sender, EventArgs e)
        {
            ResetCohortState();
        }

        /// <summary>
        /// Reset cohort state at sowing.
        /// </summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == tree)
                ResetCohortState();
        }

        /// <summary>
        /// Handle phenology stage transitions as season anchors.
        /// </summary>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType args)
        {
            if (sender != tree)
                return;

            string stage = args.StageName?.Trim() ?? string.Empty;
            bool isFlowering = string.Equals(stage, tree.FloweringStage, StringComparison.OrdinalIgnoreCase);
            bool isFruitSet = string.Equals(stage, tree.FruitSetStage, StringComparison.OrdinalIgnoreCase);
            bool isFruitFill = string.Equals(stage, tree.FruitFillStage, StringComparison.OrdinalIgnoreCase);
            bool isMaturity = string.Equals(stage, tree.MaturityStage, StringComparison.OrdinalIgnoreCase);

            if (!isMaturity && !tree.AllowReproductivePhase(stage))
                return;

            Summary.WriteMessage(this, $"[FruitOrgan] PhaseChanged -> stage='{stage}'", MessageType.Diagnostic);
            SyncCohortFromLivePools();

            if (isFlowering)
            {
                Nflower = ComputePotentialFlowerCount();
                FruitAgeTT = 0.0;
                InReproSeason = true;
                isHarvestable = false;
                fruitSetWindowActive = false;
                fruitFillWindowActive = false;
                SyncNumberFromCohort();
                Summary.WriteMessage(this, $"[{Name}] flowering begins: Nflower={Nflower:F2}/m^2", MessageType.Information);
                return;
            }

            if (isFruitSet)
            {
                InReproSeason = true;
                isHarvestable = false;
                fruitSetWindowActive = true;
                fruitFillWindowActive = false;
                if (Nflower <= 0.0)
                    Nflower = ComputePotentialFlowerCount();
                Summary.WriteMessage(this, $"[{Name}] fruit set window opened", MessageType.Information);
                return;
            }

            if (isFruitFill)
            {
                InReproSeason = true;
                fruitSetWindowActive = false;
                fruitFillWindowActive = true;
                Summary.WriteMessage(this, $"[{Name}] fruit fill window opened", MessageType.Information);
                return;
            }

            if (isMaturity)
            {
                fruitSetWindowActive = false;
                fruitFillWindowActive = false;
                isHarvestable = true;
                Summary.WriteMessage(this, $"[{Name}] maturity reached; fruit fate policy now applies", MessageType.Information);
            }
        }

        /// <summary>
        /// Keep Number aligned to retained fruit.
        /// </summary>
        private void SyncNumberFromCohort()
        {
            Number = Math.Max(0.0, Nfruit);
        }

        /// <summary>
        /// Clamp helper.
        /// </summary>
        private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

        /// <summary>
        /// Clamp to 0-1.
        /// </summary>
        private static double Clamp01(double value) => Clamp(value, 0.0, 1.0);

        /// <summary>
        /// Numerically safe logistic response.
        /// </summary>
        private static double Logistic(double x, double x50, double slope)
        {
            double exponent = Clamp(-slope * (x - x50), -60.0, 60.0);
            return 1.0 / (1.0 + Math.Exp(exponent));
        }

        /// <summary>
        /// Stress response for fruit set/retention.
        /// </summary>
        private double ComputeSetStress()
        {
            double fw = Clamp01(tree?.StressForReproOutput ?? 0.0);
            return Clamp01(Logistic(fw, Fw50_Set, FwSlope_Set));
        }

        /// <summary>Can the current phenological state create newly retained fruit?</summary>
        private bool CanSetFruitNumber() => InReproSeason && !fruitFillWindowActive && !isHarvestable;

        /// <summary>Compute potential flowers from bud load when available, otherwise fallback to NumberFunction.</summary>
        private double ComputePotentialFlowerCount()
        {
            double budLoad = Math.Max(0.0, tree?.BudLoadPerAreaOutput ?? 0.0);
            double potentialFromBuds = budLoad > Epsilon
                ? budLoad * Math.Max(0.0, ReproPotentialPerBud)
                : 0.0;

            if (potentialFromBuds > Epsilon)
                return potentialFromBuds;

            return Math.Max(0.0, NumberFunction?.Value() ?? 0.0);
        }

        /// <summary>
        /// Apply continuous fruit drop and remove biomass proportionally.
        /// </summary>
        private double ApplyFruitDrop(double effectiveRatePerDay)
        {
            double rate = Math.Max(0.0, effectiveRatePerDay);
            if (rate <= 0.0 || Nfruit <= Epsilon)
                return 0.0;

            double nBefore = Nfruit;
            double survival = Math.Exp(-rate);
            Nfruit = Math.Max(0.0, nBefore * survival);
            double removedCount = Math.Max(0.0, nBefore - Nfruit);

            double removedFraction = Clamp01(1.0 - (Nfruit / Math.Max(Epsilon, nBefore)));
            if (removedFraction > Epsilon)
                RemoveBiomass(liveToRemove: 0.0, deadToRemove: 0.0, liveToResidue: removedFraction, deadToResidue: 0.0);

            return removedCount;
        }

        /// <summary>Move a fraction of live fruit biomass into senesced pools while reducing cohort count smoothly.</summary>
        private double ApplySenescenceToDead(double effectiveRatePerDay)
        {
            double rate = Math.Max(0.0, effectiveRatePerDay);
            if (rate <= 0.0 || Nfruit <= Epsilon)
                return 0.0;

            double nBefore = Nfruit;
            double survival = Math.Exp(-rate);
            double nAfter = Math.Max(0.0, nBefore * survival);
            double removedFraction = Clamp01(1.0 - (nAfter / Math.Max(Epsilon, nBefore)));

            if (removedFraction > Epsilon)
            {
                var loss = new Biomass
                {
                    StructuralWt = Live.StructuralWt * removedFraction,
                    StorageWt = Live.StorageWt * removedFraction,
                    MetabolicWt = Live.MetabolicWt * removedFraction,
                    StructuralN = Live.StructuralN * removedFraction,
                    StorageN = Live.StorageN * removedFraction,
                    MetabolicN = Live.MetabolicN * removedFraction,
                };
                Live.Subtract(loss);
                Senesced.Add(loss);
            }

            Nfruit = nAfter;
            return Math.Max(0.0, nBefore - nAfter);
        }

        /// <summary>
        /// Sync cohort state with current live fruit pools.
        /// </summary>
        public void SyncCohortFromLivePools()
        {
            double liveFruitDM = Math.Max(0.0, Live.StructuralWt + Live.StorageWt);

            if (FruitDM_g_m2 > Epsilon && liveFruitDM < FruitDM_g_m2 - Epsilon && Nfruit > Epsilon)
            {
                double retainedFraction = Clamp01(liveFruitDM / Math.Max(Epsilon, FruitDM_g_m2));
                Nfruit *= retainedFraction;
            }

            FruitDM_g_m2 = liveFruitDM;

            SyncNumberFromCohort();
        }

        /// <summary>
        /// Update smooth set, drop and fruit age once per day.
        /// </summary>
        private void UpdateCohortDaily()
        {
            DateTime today = clock?.Today ?? DateTime.MinValue;
            if (today != DateTime.MinValue && lastCohortUpdateDate == today)
                return;
            lastCohortUpdateDate = today;

            SyncCohortFromLivePools();

            dailySetFlux_fruit_m2 = 0.0;
            dailyDropFlux_fruit_m2 = 0.0;
            dailySeedDMDemand_g_m2 = 0.0;

            if (!InReproSeason && !fruitSetWindowActive && !fruitFillWindowActive && !isHarvestable && Nfruit <= Epsilon && FruitDM_g_m2 <= Epsilon)
                return;

            double stress = ComputeSetStress();
            if (Nflower <= Epsilon && InReproSeason)
                Nflower = ComputePotentialFlowerCount();

            double nBefore = Nfruit;
            double targetFruit = Math.Max(0.0, Nflower) * Clamp01(FruitSetMaxFrac);
            double setGain = 0.0;
            if (CanSetFruitNumber())
            {
                double kSet = Math.Max(0.0, SetRateMax) * stress;
                setGain = Math.Max(0.0, targetFruit - nBefore) * (1.0 - Math.Exp(-kSet));
            }
            double nAfterSet = nBefore + setGain;

            double baseDropRate = Math.Max(0.0, DropRateBase)
                                + Math.Max(0.0, DropStressSensitivity) * (1.0 - stress)
                                + Math.Max(0.0, FruitDropRate) * (1.0 - stress);
            double dropLoss = nAfterSet * (1.0 - Math.Exp(-Math.Max(0.0, baseDropRate)));
            Nfruit = Math.Max(0.0, nAfterSet - dropLoss);

            dailySetFlux_fruit_m2 = Math.Max(0.0, setGain);
            dailyDropFlux_fruit_m2 = Math.Max(0.0, dropLoss);
            dailySeedDMDemand_g_m2 = Math.Max(0.0, FruitSeedDM_gPerFruit) * dailySetFlux_fruit_m2;

            if (isHarvestable && Nfruit > Epsilon)
            {
                FruitFatePolicy fatePolicy = tree?.FruitFatePolicy ?? global::Models.Agroforestry.FruitFatePolicy.Abscise;
                switch (fatePolicy)
                {
                    case global::Models.Agroforestry.FruitFatePolicy.Persist:
                    case global::Models.Agroforestry.FruitFatePolicy.AutoHarvest:
                        break;

                    case global::Models.Agroforestry.FruitFatePolicy.SenesceToDead:
                    {
                        double senescenceRate = PostMaturityDropRate > 0.0
                            ? PostMaturityDropRate
                            : Math.Max(0.0, DropRateBase);
                        dailyDropFlux_fruit_m2 += ApplySenescenceToDead(senescenceRate);
                        break;
                    }

                    case global::Models.Agroforestry.FruitFatePolicy.Abscise:
                    default:
                    {
                        double maturityDropRate = PostMaturityDropRate > 0.0
                            ? PostMaturityDropRate
                            : Math.Max(0.0, DropRateBase);
                        dailyDropFlux_fruit_m2 += ApplyFruitDrop(maturityDropRate);
                        break;
                    }
                }
            }

            FruitAgeTT += Math.Max(0.0, tree?.DailyFruitThermalTimeIncrement ?? 0.0);
            SyncCohortFromLivePools();

            if (isHarvestable && FruitDM_g_m2 <= Epsilon && Nfruit <= Epsilon)
                InReproSeason = false;

            Summary.WriteMessage(this,
                $"[{Name}] cohort update: Nflower={Nflower:F2}, Nfruit={Nfruit:F2}, dN_set={dailySetFlux_fruit_m2:F4}, dN_drop={dailyDropFlux_fruit_m2:F4}, FruitDM={FruitDM_g_m2:F3} g/m^2, stress={stress:F3}",
                MessageType.Diagnostic);
        }

        /// <summary>
        /// Reset all cohort state variables.
        /// </summary>
        private void ResetCohortState()
        {
            Nflower = 0.0;
            Nfruit = 0.0;
            FruitDM_g_m2 = 0.0;
            FruitAgeTT = 0.0;
            InReproSeason = false;
            fruitSetWindowActive = false;
            fruitFillWindowActive = false;
            isHarvestable = false;
            lastCohortUpdateDate = DateTime.MinValue;
            dailySeedDMDemand_g_m2 = 0.0;
            dailySetFlux_fruit_m2 = 0.0;
            dailyDropFlux_fruit_m2 = 0.0;
            Number = 0.0;
        }

        /// <summary>
        /// Public helper for parent tree to reset cohort after harvest if fruit is gone.
        /// </summary>
        public void ResetCohortIfNoFruit()
        {
            SyncCohortFromLivePools();
            if (FruitDM_g_m2 <= Epsilon)
                ResetCohortState();
        }

        /// <summary>
        /// End the seasonal cohort cleanly after an auto-harvest event.
        /// </summary>
        public void ResetCohortAfterHarvest()
        {
            if (Live.Wt > Epsilon || Dead.Wt > Epsilon)
            {
                RemoveBiomass(
                    liveToRemove: 0.0,
                    deadToRemove: 0.0,
                    liveToResidue: 1.0,
                    deadToResidue: 1.0);
            }

            SyncCohortFromLivePools();
            ResetCohortState();
        }

        /// <summary>
        /// Keep cohort coherent after harvest operations.
        /// </summary>
        [EventSubscribe("PostHarvesting")]
        private void OnPostHarvestingCohort(object sender, HarvestingParameters e)
        {
            if (e.RemoveBiomass
                && (tree?.FruitFatePolicy ?? global::Models.Agroforestry.FruitFatePolicy.Abscise)
                    == global::Models.Agroforestry.FruitFatePolicy.AutoHarvest)
            {
                ResetCohortAfterHarvest();
                return;
            }

            SyncCohortFromLivePools();
            if (FruitDM_g_m2 <= Epsilon)
                ResetCohortState();
        }

        /// <summary>
        /// Ensure Number reflects cohort state even though base class updates Number daily.
        /// </summary>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowthSyncNumber(object sender, EventArgs e)
        {
            SyncNumberFromCohort();
        }

        /// <summary>Calculate dry matter demand for arbitration.</summary>
        [EventSubscribe("SetDMDemand")]
        private void SetDMDemand(object sender, EventArgs e)
        {
            UpdateCohortDaily();
            SyncNumberFromCohort();

            if (Nfruit <= Epsilon && dailySeedDMDemand_g_m2 <= Epsilon)
            {
                DMDemand.Clear();
                return;
            }

            double targetFruitDm = Math.Max(0.0, PotentialFruitSizeDM) * Math.Max(0.0, Nfruit);
            double growthGapDm = Math.Max(0.0, targetFruitDm - FruitDM_g_m2);
            double growthTimeScale = Math.Max(1.0, FruitSetDurationDays);
            double growthDemand = growthGapDm / growthTimeScale;

            // Legacy DMDemandFunction remains as a species-level cap, not as a free multiplier.
            double growthDemandCap = Math.Max(0.0, DMDemandFunction.Value());
            if (growthDemandCap > 0.0)
                growthDemand = Math.Min(growthDemand, growthDemandCap);

            double cohortDemand = Math.Max(0.0, growthDemand) + Math.Max(0.0, dailySeedDMDemand_g_m2);

            if (cohortDemand <= Epsilon)
            {
                DMDemand.Clear();
                return;
            }

            double dMCE = Math.Max(Epsilon, DMConversionEfficiency.Value());
            DMDemand.Structural = cohortDemand / dMCE;
            DMDemand.QStructuralPriority = dmDemandPriorityFactors.Structural.Value();
            DMDemand.QMetabolicPriority = dmDemandPriorityFactors.Metabolic.Value();
            DMDemand.QStoragePriority = dmDemandPriorityFactors.Storage.Value();
        }

        /// <summary>Calculate nitrogen demand for arbitration.</summary>
        [EventSubscribe("SetNDemand")]
        private void SetNDemand(object sender, EventArgs e)
        {
            UpdateCohortDaily();
            SyncNumberFromCohort();

            if (Nfruit <= Epsilon || DMDemand.Structural <= Epsilon)
            {
                NDemand.Clear();
                return;
            }

            double fruitScale = Nflower > Epsilon ? Nfruit / Math.Max(Epsilon, Nflower) : 0.0;
            fruitScale = Clamp01(fruitScale);

            double demand = Math.Max(0.0, NFillingRate.Value()) * fruitScale;
            demand = Math.Min(demand, MaximumNConc.Value() * potentialDMAllocation.Structural);
            NDemand.Structural = demand;
        }
    }
}
