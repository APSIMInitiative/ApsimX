using System;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from a start stage to an end stage and assumes
    /// germination will be reached on the day after sowing or the first day
    /// thereafter when the extractable soil water at sowing depth is greater than zero."
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GerminatingPhase : Model, IPhase, IPhaseWithSetableCompletionDate
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private IPhysical soilPhysical = null;

        /// <summary>Link to the soil water balance.</summary>
        [Link]
        private ISoilWater waterBalance = null;

        [Link]
        private Plant plant = null;

        [Link]
        private Phenology phenology = null;

        [Link]
        private IClock clock = null;

        [Link]
        private ISoilTemperature soilTemperature = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction minSoilTemperature = null;

        /// <summary>The soil layer in which the seed is sown.</summary>
        private int SowLayer = 0;

        /// <summary>Fraction of phase that is complete (0-1).on yesterdays timestep</summary>
        private double fractionCompleteYesterday;

        /// <summary>First date in this phase</summary>
        private DateTime startDate;


        /// <summary>Occurs when a plant is about to be sown.</summary>
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

        /// <summary>Accumulated units of thermal time as progress through phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Thermal time target to end this phase.</summary>
        [JsonIgnore]
        public double Target { get; set; } = 1;

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                return Phenology.FractionComplete(DateToProgress, ProgressThroughPhase, Target, startDate, clock.Today, fractionCompleteYesterday);
            }
        }

        /// <summary>Data to progress.  Is empty by default.  If set by external model, phase will ignore its mechanisum and wait for the specified date to progress</summary>
        [JsonIgnore]
        public string DateToProgress { get; set; } = "";

        /// <summary>Compute the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            if (!String.IsNullOrEmpty(DateToProgress))
            {
                proceedToNextPhase = Phenology.checkIfCompletionDate(ref startDate, clock.Today, DateToProgress, ref propOfDayToUse);
                if (proceedToNextPhase)
                {
                    doGermination();
                }
                return proceedToNextPhase;
            }
            double sowLayerTemperature = soilTemperature.Value[SowLayer];

            if (!phenology.OnStartDayOf("Sowing") && waterBalance.SWmm[SowLayer] > soilPhysical.LL15mm[SowLayer] && sowLayerTemperature >= minSoilTemperature.Value())
            {
                doGermination();
                proceedToNextPhase = true;
                propOfDayToUse = 1;
                ProgressThroughPhase = 1;
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
            DateToProgress = "";
            fractionCompleteYesterday = 0;
        }

        // 5. Private methods
        //-----------------------------------------------------------------------------------------------------------------

        private void doGermination()
        {
            if (SeedImbibed != null)
                SeedImbibed.Invoke(this, new EventArgs());
        }

        /// <summary>Called when crop is ending.</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            SowLayer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, plant.SowingData.Depth);
        }

    }
}