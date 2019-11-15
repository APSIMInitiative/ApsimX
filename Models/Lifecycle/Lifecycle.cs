// -----------------------------------------------------------------------
// <copyright file="LifeCycle.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace Models.LifeCycle
{
    using System;
    using System.Collections.Generic;
    using Models.Core;

    /// <summary>
    /// # [Name]
    /// A LifeCycle that contains LifeStages.
    /// 
    ///|Property          .|Type    .|Units  .|Description              .| 
    ///|---|---|---|:---|
    ///|ChildStages       |List of LifeStage |  |List of child LifeStages       |
    ///|CurrentLifeStage  |LifeStage         |  |Current LifeStage begin processed |
    ///|InitialPopulation |double[]          |  |Initial population for each LifeStage |
    ///|TotalPopulation   |double            |  |Total population of all the cohorts in this LifeCycle |
    ///
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class LifeCycle : Model
    {
        [Link]
        private ISummary mySummary = null;

        /// <summary>
        /// List of child LifeStages
        /// </summary>
        [NonSerialized]
        public List<LifeStage> ChildStages = null;

        /// <summary>
        /// Current Lifestage being processed.
        /// </summary>
        public LifeStage CurrentLifeStage { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public LifeCycle()
        {
            
        }

        /// <summary>
        /// Population of the initial cohort at each lifestage. e.g. [2,6,3]
        /// </summary>
        [Description("Initial population at each LifeStage")]
        public double[] InitialPopulation { get; set; }

        /// <summary>
        /// Total population of all the cohorts in this lifecycle
        /// </summary>
        public double TotalPopulation
        {
            get
            {
                double sum = 0;
                foreach (LifeStage stage in ChildStages)
                {
                    sum += stage.TotalPopulation;
                }
                return sum;
            }
        }

        /// <summary>
        /// At the start of the simulation construct the list of child LifeStages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ChildStages = new List<LifeStage>();
            foreach (LifeStage stage in Apsim.Children(this, typeof(LifeStage)))
            {
                stage.OwningCycle = this;
                ChildStages.Add(stage);
            }
            
            int i= 0;
            //create new cohorts from the InitialPopulation[]
            foreach (LifeStage stage in ChildStages)
            {
                if ((InitialPopulation != null) && (InitialPopulation.Length > i))
                {
                    Cohort newCohort = stage.NewCohort();
                    newCohort.Count = InitialPopulation[i];
                }
                else
                {
                    mySummary.WriteWarning(this, "No initial population in " + stage.Name);
                }
                i++;
            }
        }

        /// <summary>
        /// Handle the DoLifeCycle event and process each LifeStage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoLifecycle")]
        private void OnDoLifecycle(object sender, EventArgs e)
        {
            foreach (LifeStage stage in ChildStages)
            {
                CurrentLifeStage = stage;
                stage.Process();
            }
        }
    }
}
