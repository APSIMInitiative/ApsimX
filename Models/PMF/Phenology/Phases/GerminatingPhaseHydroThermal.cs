using System;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.Soils;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from a start stage to an end stage and
    /// predicts the day on which 50% of the seeds have germinated (T50) at any temperature and water
    /// potential at the sowing depth. Germination of the median seed is treated as a progress rate: each
    /// day the seed progresses by a fraction of the way to germination, r(T, ψ) = Rmax · f(T) · fW(ψ, T) (d⁻¹),
    /// where Rmax = 1 / DaysToGerminationAtOptimum, f(T) is the Wang and Engel beta function of the seed
    /// layer soil temperature (child WangEngelTempFunction, a WangEngelTempFunction with MinTemp = Tb,
    /// OptTemp = To and MaxTemp = Tx; 1 at To and 0 outside Tb..Tx) and fW(ψ, T) is the soil water
    /// potential response (GerminatingWaterPotentialResponse). The rate is zero during the optional
    /// dormancy phase (days after sowing) and when the seed layer soil temperature is below the optional
    /// minimum germination temperature (MinSoilTemperature). Progress accumulates from the sowing day,
    /// and germination occurs on the day the accumulated rate reaches 1, at the earliest on the day after sowing.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GerminatingPhaseHydroThermal : Model, IPhase, IPhaseWithSetableCompletionDate
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private IPhysical soilPhysical = null;

        [Link]
        private Plant plant = null;

        [Link]
        private Phenology phenology = null;

        [Link]
        private IClock clock = null;

        [Link]
        private ISoilTemperature soilTemperature = null;

        /// <summary>Soil water potential factor on germination rate at sowing depth, fW(ψ, T).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private GerminatingWaterPotentialResponse GerminatingWaterPotentialResponse = null;

        /// <summary>Wang and Engel temperature response, evaluated at the seed layer soil temperature, f(T).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private WangEngelTempFunction WangEngelTempFunction = null;

        /// <summary>Minimum soil temperature for germination.</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private IFunction MinSoilTemperature = null;

        /// <summary>Dormancy phase as days after sowing.</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private IFunction DormancyPhase = null;

        /// <summary>Days to germination at optimum condition, used to calculate Rmax.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction DaysToGerminationAtOptimum = null;

        /// <summary>The soil layer in which the seed is sown.</summary>
        private int sowLayer = 0;

        /// <summary>Fraction of phase that is complete (0-1).on yesterdays timestep</summary>
        private double fractionCompleteYesterday;

        /// <summary>First date in this phase</summary>
        private DateTime startDate;

        // 2. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Occurs when the seed has imbibed and germinated.</summary>
        public event EventHandler SeedImbibed;

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = false;

        /// <summary>Accumulated germination progress of the median seed (sum of daily rates).</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Germination progress target to end this phase (50% germination).</summary>
        [JsonIgnore]
        public double Target { get { return 1.0; } }

        /// <summary>Germination progress accumulated after the dormancy phase is completed.</summary>
        [JsonIgnore]
        public double ProgressAfterDormancy { get { return ProgressThroughPhase; } }

        /// <summary>Number of days since sowing.</summary>
        [JsonIgnore]
        [Units("d")]
        public int DaysSinceSowing { get; private set; }

        /// <summary>Soil temperature of the layer where the seed is.</summary>
        [JsonIgnore]
        [Units("oC")]
        public double SoilTemperatureSeed { get; private set; }

        /// <summary>Maximum germination rate (Rmax) at optimum temperature and no water stress.</summary>
        [JsonIgnore]
        [Units("/d")]
        public double MaxGerminationRate { get; private set; }

        /// <summary>Relative temperature response factor (0-1) applied today, f(T).</summary>
        [JsonIgnore]
        public double TemperatureFactor { get; private set; }

        /// <summary>Soil water response factor (0-1) applied today, fW(ψ, T).</summary>
        [JsonIgnore]
        public double WaterFactor { get; private set; }

        /// <summary>Germination rate of the median seed today, r = Rmax · f(T) · fW(ψ, T).</summary>
        [JsonIgnore]
        [Units("/d")]
        public double DailyGerminationRate { get; private set; }

        /// <summary>
        /// Fraction of phase that is complete (0-1). Reported as 0 on the sowing day, although progress
        /// accumulates from the sowing day, so that stages derived from it (e.g. Zadok) start the day after sowing.
        /// </summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                if (phenology.OnStartDayOf("Sowing"))
                    return 0;
                return Phenology.FractionComplete(DateToProgress, ProgressThroughPhase, Target, startDate, clock.Today, fractionCompleteYesterday);
            }
        }

        /// <summary>Data to progress.  Is empty by default.  If set by external model, phase will ignore its mechanisum and wait for the specified date to progress</summary>
        [JsonIgnore]
        public string DateToProgress { get; set; } = "";

        // 3. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Compute the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            if (!String.IsNullOrEmpty(DateToProgress))
            {
                proceedToNextPhase = Phenology.checkIfCompletionDate(ref startDate, clock.Today, DateToProgress, ref propOfDayToUse);
                if (proceedToNextPhase)
                    doGermination();
                return proceedToNextPhase;
            }

            double daysAtOptimum = DaysToGerminationAtOptimum.Value();
            if (daysAtOptimum <= 0)
                throw new Exception($"{Name}: DaysToGerminationAtOptimum must be greater than zero.");

            SoilTemperatureSeed = soilTemperature.Value[sowLayer];
            WaterFactor = GerminatingWaterPotentialResponse.Calculate();
            // ValueIndexed evaluates the function at the given temperature; Value() would use air temperature.
            TemperatureFactor = Math.Max(0, WangEngelTempFunction.ValueIndexed(SoilTemperatureSeed));
            MaxGerminationRate = 1.0 / daysAtOptimum;

            bool tooCold = MinSoilTemperature != null && SoilTemperatureSeed < MinSoilTemperature.Value();
            bool dormant = DormancyPhase != null && DaysSinceSowing < DormancyPhase.Value();
            DailyGerminationRate = (tooCold || dormant) ? 0 : MaxGerminationRate * TemperatureFactor * WaterFactor;
            ProgressThroughPhase += DailyGerminationRate;
            DaysSinceSowing += 1;

            // Progress accumulates from the sowing day, but germination takes at least one day.
            if (!phenology.OnStartDayOf("Sowing") && ProgressThroughPhase >= Target)
            {
                doGermination();
                proceedToNextPhase = true;
                propOfDayToUse = 1;
            }
            fractionCompleteYesterday = FractionComplete;
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase()
        {
            DateToProgress = "";
            startDate = DateTime.MinValue;
            ProgressThroughPhase = 0.0;
            fractionCompleteYesterday = 0;
            DaysSinceSowing = 0;
            SoilTemperatureSeed = 0;
            MaxGerminationRate = 0;
            TemperatureFactor = 0;
            WaterFactor = 0;
            DailyGerminationRate = 0;
        }

        // 4. Private methods
        //-----------------------------------------------------------------------------------------------------------------

        private void doGermination()
        {
            SeedImbibed?.Invoke(this, new EventArgs());
        }

        /// <summary>Called when crop is sown.</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            sowLayer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, plant.SowingData.Depth);
        }
    }
}
