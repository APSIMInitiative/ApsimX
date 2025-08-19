using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using JetBrains.Annotations;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Interfaces;
using Models.Soils;
using Newtonsoft.Json;
using static Models.LifeCycle.LifeCyclePhase;

namespace Models.LifeCycle
{

    /// <summary>
    /// The LifeCycle model represents a population of organisms within a zone.  It assembles
    /// an arbitry number of LifeCyclePhases that cohorts of individuals (of the same developmental
    /// stage) pass through during their life.
    /// Each LifeCyclePhase assembles an arbitary number of cohorts.
    /// LifeCyclePhases have a set of parameters controlling the Development, Mortality
    /// and Reproduction of each cohort.  LifeCyclePhases may also contain plant damage functions
    /// which specifiy how each phase damages its host plants.
    /// Upon the DoLifecycle event the LifeCycle class loops through a list of each of its LifeCyclePhases
    /// The LifeCycle model is initialised with no members in any phase and the Infest() method must
    /// be called to insert members into a phase.
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(LifeCycle))]
    [ValidParent(ParentType = typeof(IPlant))]
    [ValidParent(ParentType = typeof(Soil))]
    [ValidParent(ParentType = typeof(ISurfaceOrganicMatter))]
    public class LifeCycle : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }


        [Link]
        ISummary mySummary = null;

        /// <summary>List of LifeCyclePhases that make up the LifeCycle model</summary>
        [JsonIgnore]
        public List<LifeCyclePhase> LifeCyclePhases = null;

        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler LifeCycleProcessComplete;

        /// <summary>List of the names of LifeCyclePhases</summary>
        [JsonIgnore]
        public string[] LifeCyclePhaseNames
        {
            get
            {
                return Structure.FindChildren<LifeCyclePhase>().Select(p => p.Name).ToArray();
            }
        }

        /// <summary>Total population of this lifecycle (summed across all LifeCyclePhases)</summary>
        public double TotalPopulation
        {
            get
            {
                double sum = 0;
                if (LifeCyclePhases != null)
                    foreach (LifeCyclePhase stage in LifeCyclePhases)
                    {
                        sum += stage.TotalPopulation;
                    }
                return sum;
            }
        }

        /// <summary>At the start of the simulation set up LifeCyclePhases</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            LifeCyclePhases = new List<LifeCyclePhase>();
            foreach (LifeCyclePhase stage in Structure.FindChildren<LifeCyclePhase>())
            {
                LifeCyclePhases.Add(stage);
            }

            int i = 0;
            //Set phase for graduates for each stage
            foreach (LifeCyclePhase stage in LifeCyclePhases)
            {
                if (i < LifeCyclePhases.Count - 1)
                    stage.LifeCyclePhaseForGraduates = LifeCyclePhases[i + 1];
                else
                    stage.LifeCyclePhaseForGraduates = null; //Last life cycle has no destination phase for graduates.  Everyone dies!!!
                stage.ZeorDeltas();
                i++;
            }
        }

        /// <summary>Handle the DoLifeCycle event and process each LifeStage</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoLifecycle")]
        private void OnDoLifecycle(object sender, EventArgs e)
        {
            foreach (LifeCyclePhase stage in LifeCyclePhases)
            {
                stage.Process();
            }
            // Invoke completion event.
            if (LifeCycleProcessComplete != null)
                LifeCycleProcessComplete.Invoke(this, new EventArgs());
        }

        /// <summary>Method to bring a new cohort of individuls to the specified LifeCyclePhase</summary>
        /// <param name="InfestationInfo"></param>
        public void Infest(SourceInfo InfestationInfo)
        {
            LifeCyclePhase InfestingPhase = Structure.FindChild<LifeCyclePhase>(InfestationInfo.LifeCyclePhase);
            InfestingPhase.NewCohort(InfestationInfo);
            mySummary.WriteMessage(this, "An infestation of  " + InfestationInfo.Population + " " + FullPath + " " + InfestationInfo.LifeCyclePhase + "'s occured today, just now :-)", MessageType.Diagnostic);
        }
    }
}
