using System;
using APSIM.Core;
using Models.Core;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// The phase goes from the a start stage to and end stage. The class requires a target and a progression function.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GenericPhase : Model, IPhase, IPhaseWithTarget, IPhaseWithSetableCompletionDate
    {
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction target = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction progression = null;

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

        /// <summary>First date in this phase</summary>
        private DateTime startDate;

        /// <summary>Flag for the first day of this phase</summary>
        private double fractionCompleteYesterday;

        /// <summary>Units of progress through phase on this time step.</summary>
        [JsonIgnore]
        public double ProgressionForTimeStep { get; set; }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Thermal time target to end this phase.</summary>
        [JsonIgnore]
        [Units("oD")]
        public double Target { get { return target.Value(); } }

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

        /// <summary>Compute the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            if (!String.IsNullOrEmpty(DateToProgress))
            {
                return Phenology.checkIfCompletionDate(ref startDate, clock.Today, DateToProgress, ref propOfDayToUse);
            }

            bool proceedToNextPhase = false;
            if (ProgressThroughPhase >= Target)
            {
                // We have entered this timestep after Target decrease below progress so exit without doing anything
                proceedToNextPhase = true;
            }
            else
            {
                ProgressionForTimeStep = progression.Value() * propOfDayToUse;
                ProgressThroughPhase += ProgressionForTimeStep;
                fractionCompleteYesterday = FractionComplete;

                if (ProgressThroughPhase > Target)
                {
                    if (ProgressionForTimeStep > 0.0)
                    {
                        proceedToNextPhase = true;
                        propOfDayToUse *= (ProgressThroughPhase - Target) / ProgressionForTimeStep;
                        ProgressionForTimeStep *= (1 - propOfDayToUse);
                    }
                    ProgressThroughPhase = Target;
                }
            }

            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase() 
        { 
            ProgressThroughPhase = 0.0;
            DateToProgress = "";
            fractionCompleteYesterday = 0;
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



