using System;
using APSIM.Core;
using Models.Core;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from a start stage to an end stage and simulates time to
    /// emergence as a function of sowing depth.
    /// Progress toward emergence is driven by a thermal time accumulation child function.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class EmergingPhase : Model, IPhase, IPhaseWithTarget, IPhaseWithSetableCompletionDate
    {
        [Link]
        Phenology phenology = null;

        [Link]
        IClock clock = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction target = null;

        /// <summary>Fraction of phase that is complete (0-1).on yesterdays timestep</summary>
        private double fractionCompleteYesterday; 

        /// <summary>First date in this phase</summary>
        private DateTime startDate;

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = false;


        /// <summary>Thermal time target to end this phase.</summary>
        [JsonIgnore]
        public double Target { get; set; }

        /// <summary>Thermal time for this time-step.</summary>
        public double TTForTimeStep { get; set; }

        /// <summary>Accumulated units of thermal time as progress through phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Data to progress.  Is empty by default.  If set by external model, phase will ignore its mechanisum and wait for the specified date to progress</summary>
        [JsonIgnore]
        public string DateToProgress { get; set; } = "";

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                return Phenology.FractionComplete(DateToProgress, ProgressThroughPhase, Target, startDate, clock.Today, fractionCompleteYesterday);
            }
        }

        /// <summary>Computes the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {

            if (!String.IsNullOrEmpty(DateToProgress))
            {
                return Phenology.checkIfCompletionDate(ref startDate, clock.Today, DateToProgress, ref propOfDayToUse);
            }

            bool proceedToNextPhase = false;
            TTForTimeStep = phenology.thermalTime.Value() * propOfDayToUse;
            ProgressThroughPhase += TTForTimeStep;
            fractionCompleteYesterday = FractionComplete;
            if (ProgressThroughPhase > Target)
            {
                if (TTForTimeStep > 0.0)
                {
                    proceedToNextPhase = true;
                    propOfDayToUse = (ProgressThroughPhase - Target) / TTForTimeStep;
                    TTForTimeStep *= (1 - propOfDayToUse);
                }
                ProgressThroughPhase = Target;
            }
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase()
        {
            TTForTimeStep = 0;
            ProgressThroughPhase = 0;
            Target = 0;
            DateToProgress = null;
            startDate = DateTime.MinValue;
        }

        // 4. Private method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            Target = target.Value();
        }

    }
}
