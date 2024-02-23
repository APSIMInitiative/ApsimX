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

        /// <summary>Occurs when [phase changed].</summary>
        public event EventHandler<PhaseChangedType> ParallelPhaseChanged;

        /// <summary>The phenological stage at the start of this parallel phase.</summary>
        [JsonIgnore]
        public double StartStage { get; set; }

        /// <summary>Property specifying if we are currently with this phase</summary>
        [JsonIgnore]
        public bool IsInPhase { get; set; }

        private bool firstDayinPhase { get; set; }

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

        private bool hasBegun { get; set; } = false;
        private bool hasFinished { get; set; } = false;

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
                if (firstDayinPhase == true)
                {
                    StartStage = phenology.Stage;
                    firstDayinPhase = false;
                }
                ProgressThroughPhase += progression.Value();
                IsInPhase = true;
            }
            else
            {
                IsInPhase = false;
            }

            if ((hasBegun == false) && (IsInPhase == true))
            {
                PhaseChangedType PhaseChangedData = new PhaseChangedType();
                PhaseChangedData.StageName = "Start"+this.Name ;
                ParallelPhaseChanged?.Invoke(plant, PhaseChangedData);
            }
            if ((hasBegun == false) && (IsInPhase == true))
            {
                hasBegun = true;
            }
            if ((hasBegun == true) && (IsInPhase == false) && (hasFinished == false)) // only occurs on the day we complete this phase
            {
                PhaseChangedType PhaseChangedData = new PhaseChangedType();
                PhaseChangedData.StageName = "End"+this.Name ;
                ParallelPhaseChanged?.Invoke(plant, PhaseChangedData);
            }
            if ((hasBegun == true) && (IsInPhase == false))
            {
                hasFinished = true;
            }
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            ProgressThroughPhase = 0.0;
            IsInPhase = false;
            firstDayinPhase = true;
            hasBegun = false;
            hasFinished = false;
            StartStage = 0;  
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



