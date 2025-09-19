using System;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
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
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        Phenology phenology = null;

        [Link]
        IClock clock = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction target = null;

        // 2. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = false;

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                if (Target == 0)
                    return 1;
                else
                    return ProgressThroughPhase / Target;
            }
        }

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

        // 3. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Computes the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            if (String.IsNullOrEmpty(DateToProgress))
            {
                TTForTimeStep = phenology.thermalTime.Value() * propOfDayToUse;

                ProgressThroughPhase += TTForTimeStep;
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
            }
            else
            {
                if (DateUtilities.DatesAreEqual(DateToProgress, clock.Today))
                {
                    proceedToNextPhase = true;
                    propOfDayToUse = 1;
                }
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
