using System;
using System.Collections.Generic;
using System.Data;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// List of possible ways to agregagte data from multiple sequential phases
    /// </summary>
    public enum AggregationOptions
    {
        /// <summary>Progress remaining to complete all phases (between specified Start and End Stages) is returned as an absolute difference from the target for all specified phases</summary>
        AbsoluteProgressRemaining,
        /// <summary>Progress remaining to complete all phases (between specified Start and End Stages) is returned relative to the target for all specified phases </summary>
        RelativeProgressRemaining,
        /// <summary>Progress accumulated over all phases (between specified Start and End Stages) is returned as an absolute value </summary>
        AbsoluteProgress,
        /// <summary>Progress accumulated over all phases (between specified Start and End Stages) is returned as a relative to the target for all specified phases</summary>
        RelativeProgress,
        /// <summary>The target to complete all phases (between specified Start and End Stages)</summary>
        Target,
    }

    /// <summary>  Aggregates data over multiple phases that fall between StartStage and EndStage specified.  AggregationOptions specifies the different methods for expressing this data</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AggregateOverMultiplePhases : Model, IFunction
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

        /// <summary>The type of biomass removal event.</summary>
        [Description("Remaining Tt experssed as absolute or relative value?")]
        public AggregationOptions Method { get; set; }


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
            double totalTarget = 0;
            double totalProgress = 0;
            foreach (IPhaseWithTarget phase in phases)
            {
                totalTarget += phase.Target;
                totalProgress += phase.ProgressThroughPhase;
            }
            if (Method == AggregationOptions.AbsoluteProgressRemaining)
                return Math.Max(totalTarget - totalProgress, 0);
            if (Method == AggregationOptions.RelativeProgressRemaining)
                return MathUtilities.Divide(totalTarget-totalProgress, totalTarget, 0);
            if (Method == AggregationOptions.AbsoluteProgress)
                return totalProgress;
            if (Method == AggregationOptions.RelativeProgress)
                return MathUtilities.Divide(totalProgress, totalTarget, 0);
            if (Method == AggregationOptions.Target)
                return totalTarget;
            else
                throw new Exception("Need to choose a method for aggreegating data");
        }

        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(StartStageName))
                throw new Exception("Need to specify StartStage for AgregateOverMultiplePhases, " + this.Name);
            if (String.IsNullOrEmpty(EndStageName))
                throw new Exception("Need to specify EndStage for AgregateOverMultiplePhases, " + this.Name);
        }
    }
}