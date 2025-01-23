using System;
using System.Collections.Generic;
using System.Data;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>  Sums the tt progress between specified start and end stages and returns this relative to the total Tt duraiton of these phases</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ThermalTimeRemaining : Model, IFunction
    {
        /// <summary>The phenology</summary>
        [Link]
        private Phenology phenology = null;

        /// <summary>The start stage name</summary>
        [Description("Stage name to start accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string StartStageName { get; set; }

        /// <summary>The end stage name</summary>
        [Description("Stage name to stop accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string EndStageName { get; set; }


        /// <summary>The phases</summary>
        private List<IPhaseWithTarget> phases = new List<IPhaseWithTarget>();

        /// <summary>
        /// Refreshes the list of phases.
        /// </summary>
        private void RefreshPhases()
        {
            phases = new List<IPhaseWithTarget>();

            foreach (IPhase phase in phenology.phases)
                if (phenology.PhaseBetweenStages(StartStageName, EndStageName, phase))
                    if (phase is IPhaseWithTarget)
                        phases.Add(phase as IPhaseWithTarget);
                    else
                        throw new Exception("Can only use relative progress function over phases that have a target property. i.e phases of type IPhaseWithTarget");

        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            RefreshPhases();
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double totalDuration = 0;
            double totalProgress = 0;
            foreach (IPhaseWithTarget phase in phases)
            {
                totalDuration += phase.Target;
                totalProgress += phase.ProgressThroughPhase;
            }
            return Math.Max(totalDuration - totalProgress, 0);
        }
    }
}