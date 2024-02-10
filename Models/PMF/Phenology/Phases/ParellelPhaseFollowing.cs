using System;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Phase is parallel to main phenology sequence, starting at an arbitary stage value and ending after the progression has accumulated to the 
    /// specified target
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class ParallelPhaseFollowing : Model, IParallelPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction target = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction progression = null;

        [Link(Type = LinkType.Scoped)] private Phenology phenology = null;

        [Link(Type = LinkType.Ancestor)] IPlant plant = null;

        // 2. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The parallel phenological phase prior to this phase.</summary>
        [Description("Prior Phase")]
        public string PriorParallelPhaseName { get; set; }

        private IParallelPhase priorPhase = null;

        /// <summary>The phenological stage at the start of this parallel phase.</summary>
        [JsonIgnore]
        public Nullable<double> StartStage { get; set; }

        /// <summary>Property specifying if we are currently with this phase</summary>
        [JsonIgnore]
        public bool IsInPhase { get; set; }

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                if (Target == 0.0)
                    return 1.0;
                else
                    return ProgressThroughPhase / Target;
            }
        }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Thermal time target to end this phase.</summary>
        [JsonIgnore]
        [Units("oD")]
        public double Target { get { return target.Value(); } }

        // 3. Public methods
        //-----------------------------------------------------------------------------------------------------------------
        
        /// <summary>Compute the phenological development following the main phenology loop</summary>
        [EventSubscribe("PostPhenology")]
        public void OnPostPhenology(object sender, EventArgs e)
        {
             
            if ((priorPhase.FractionComplete >= 1.0) && (ProgressThroughPhase <= Target))
            {
                if (StartStage == null)
                    StartStage = phenology.Stage;
                ProgressThroughPhase += progression.Value();
                IsInPhase = true;
            }
            else
            {
                IsInPhase = false;
            }
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            ProgressThroughPhase = 0.0;
            IsInPhase = false;
            StartStage = null;  
        }

        // 4. Private method
        //-----------------------------------------------------------------------------------------------------------------

        [EventSubscribe("StageWasReset")]
        private void onStageWasReset(object sender, StageSetType sst)
        {
            if (sst.StageNumber <= StartStage)
            {
                ResetPhase();   
            }
        }
        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void onSimulationCommencing(object sender, EventArgs e)
        {
            priorPhase = plant.FindDescendant<IParallelPhase>(PriorParallelPhaseName);
            ResetPhase();
        }
    }
}



