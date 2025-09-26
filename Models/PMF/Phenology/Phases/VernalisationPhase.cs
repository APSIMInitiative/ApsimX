using System;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Bibliography;
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

        private bool firstDay = true;
        private double relativeVernalisationAtEmergence { get; set; }
        /// <summary>First date in this phase</summary>
        private DateTime firstDate { get; set; }

        /// <summary>Flag for the first day of this phase</summary>
        private bool first { get; set; }

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
                if (String.IsNullOrEmpty(DateToProgress))
                {
                    return ProgressThroughPhase;
                }
                else
                {
                    double dayDurationOfPhase = (DateUtilities.GetDate(DateToProgress) - firstDate).Days;
                    double daysInPhase = (clock.Today - firstDate).Days;
                    return daysInPhase / dayDurationOfPhase;
                }
            }
        }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Data to progress.  Is empty by default.  If set by external model, phase will ignore its mechanisum and wait for the specified date to progress</summary>
        [JsonIgnore]
        public string DateToProgress { get; set; } = "";

        /// <summary>Thermal time target to end this phase.</summary>
        [JsonIgnore]
        public double Target { get; set; }

        /// <summary>Summarise gene expression from CAMP into phenological progress</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            if (String.IsNullOrEmpty(DateToProgress))
            {
                Target = 1 + CAMP.Vrn2;
                double RelativeVernalisation = Math.Min((CAMP.BaseVrn + CAMP.Vrn1 + CAMP.Vrn3) / Target, CAMP.MaxVrn);
                if (firstDay)
                {
                    relativeVernalisationAtEmergence = RelativeVernalisation;
                    firstDay = false;
                }
                ProgressThroughPhase = Math.Min(1, (RelativeVernalisation - relativeVernalisationAtEmergence) / (1 - relativeVernalisationAtEmergence));
                proceedToNextPhase = CAMP.IsVernalised;
            }
            else
            {
                if (first)
                {
                    firstDate = clock.Today;
                    first = false;
                }
                if (DateUtilities.DatesAreEqual(DateToProgress, clock.Today))
                {
                    proceedToNextPhase = true;
                    propOfDayToUse = 1;
                }
            }
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            firstDay = true;
            relativeVernalisationAtEmergence = 0.0;
            ProgressThroughPhase = 0.0;
            DateToProgress = "";
            first = true;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }

    }
}
