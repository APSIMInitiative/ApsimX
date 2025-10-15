using System;
using APSIM.Core;
using Models.Core;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from the specified start stage to the specified end stage and
    /// its duration is determined by leaf appearance rate and the number of leaves to
    /// complete the phase.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class SimpleLeafAppearancePhase : Model, IPhase, IPhaseWithSetableCompletionDate
    {

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction targetLeafNumber = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction currentLeafNumber = null;

        [Link]
        private IClock clock = null;

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = true;

        /// <summary>Leaves appeared when this phase is entered</summary>
        private double leafNoAtStart { get; set; }

        /// <summary>relative progress through the phase yesterday</summary>
        private double fractionCompleteYesterday = 0;

        /// <summary>The target for progresson to the next phase</summary>
        private double target = 0;

        /// <summary>First date in this phase</summary>
        private DateTime startDate;

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Data to progress.  Is empty by default.  If set by external model, phase will ignore its mechanisum and wait for the specified date to progress</summary>
        [JsonIgnore]
        public string DateToProgress { get; set; } = "";

        /// <summary>Return a fraction of phase complete.</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                return Phenology.FractionComplete(DateToProgress, ProgressThroughPhase, target, startDate, clock.Today, fractionCompleteYesterday);
            }
        }

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            if (!String.IsNullOrEmpty(DateToProgress))
            {
                return Phenology.checkIfCompletionDate(ref startDate, clock.Today, DateToProgress, ref propOfDayToUse);
            }

            if (leafNoAtStart == 0)
            {
                leafNoAtStart = currentLeafNumber.Value();
                target = targetLeafNumber.Value() - leafNoAtStart;
            }

            ProgressThroughPhase = currentLeafNumber.Value() - leafNoAtStart;
            fractionCompleteYesterday = FractionComplete;

            bool proceedToNextPhase = false;
            if (FractionComplete >= 1)
            {
                propOfDayToUse = 0.00001;  //assumes we use most of the Tt today to get to final leaf.  Should be calculated as a function of the phyllochron
                proceedToNextPhase = true;
            }
            return proceedToNextPhase;
        }

        /// <summary>Reset phase</summary>
        public void ResetPhase()
        {
            leafNoAtStart = 0;
            fractionCompleteYesterday = 0;
            target = 0;
            ProgressThroughPhase = 0;
            DateToProgress = "";
        }

        //7. Private methode
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }
    }
}
