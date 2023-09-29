﻿using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
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
    public class StartPhase : Model, IPhase
    {

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
                return 0.0;
            }
        }

        /// <summary>Units of progress through phase on this time step.</summary>
        [JsonIgnore]
        public double ProgressionForTimeStep { get; set; }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }


        // 3. Public methods
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>Compute the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            return true;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase() { ProgressThroughPhase = 0.0; }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public override IEnumerable<ITag> Document()
        {
            // Write description of this class.
            yield return new Paragraph($"This phase goes from {Start.ToLower()} to {End.ToLower()}.");

            // Write memos
            foreach (var tag in DocumentChildren<Memo>())
                yield return tag;

            yield return new Paragraph($"It has not length but sets plant status to emerged once progressed");

        }


        // 4. Private method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }
    }
}



