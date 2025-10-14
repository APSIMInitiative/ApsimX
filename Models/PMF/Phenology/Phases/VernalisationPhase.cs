using System;
using Models.Core;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from the specified start stage to the specified end stage
    /// and reaches end when vernalisation saturation occurs.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class VernalisationPhase : Model, IPhase, IPhaseWithTarget, IPhaseWithSetableCompletionDate
    {
        [Link]
        private IVrnExpression CAMP = null;

        [Link]
        private IClock clock = null;

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = true;

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [JsonIgnore]
        public double FractionComplete 
        {
            get
            {
                return Phenology.FractionComplete(DateToProgress,ProgressThroughPhase,Target,startDate, clock.Today, fractionCompleteYesterday);
            }
        }

        /// <summary>First date in this phase</summary>
        private DateTime startDate;

        /// <summary>Fraction of phase that is complete (0-1).on yesterdays timestep</summary>
        private double fractionCompleteYesterday;

        /// <summary>The relative progress throuh vernalisation that has already happend when the crop emerges</summary>
        private double relativeVernalisationAtEmergence { get; set; }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Data to progress.  Is empty by default.  If set by external model, phase will ignore its mechanisum and wait for the specified date to progress</summary>
        [JsonIgnore]
        public string DateToProgress { get; set; } = "";

        /// <summary>Thermal time target to end this phase.</summary>
        public double Target { get; set; } = 1.0;

        /// <summary>Summarise gene expression from CAMP into phenological progress</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            if (!String.IsNullOrEmpty(DateToProgress))
            {
                return Phenology.checkIfCompletionDate(ref startDate, clock.Today, DateToProgress, ref propOfDayToUse);
            }

            double vrnTarget = 1 + CAMP.Vrn2;
            double RelativeVernalisation = Math.Min((CAMP.BaseVrn + CAMP.Vrn1 + CAMP.Vrn3) / vrnTarget, CAMP.MaxVrn);
            if (startDate == DateTime.MinValue)
            {
                relativeVernalisationAtEmergence = RelativeVernalisation;
                startDate = clock.Today;
            }
            ProgressThroughPhase = Math.Min(1, (RelativeVernalisation - relativeVernalisationAtEmergence) / (1 - relativeVernalisationAtEmergence));
            fractionCompleteYesterday = FractionComplete;

            return CAMP.IsVernalised;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            relativeVernalisationAtEmergence = 0.0;
            ProgressThroughPhase = 0.0;
            DateToProgress = "";
            startDate = DateTime.MinValue;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }

    }
}
