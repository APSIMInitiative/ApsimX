using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.Lifecycle
{
    /// <summary>
    /// A lifecycle that contains lifestages. 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class Lifecycle : Model
    {
        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        public List<Lifestage> ChildStages = null;

        /// <summary>
        /// Current Lifestage being processed.
        /// </summary>
        public Lifestage CurrentLifestage { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Lifecycle()
        {
            
        }

        /// <summary>
        /// Population of the initial cohort at each lifestage. e.g. [2,6,3]
        /// </summary>
        [Description("Initial population at each lifestage")]
        public double[] InitialPopulation { get; set; }

        /// <summary>
        /// Total population of all the cohorts in this lifecycle
        /// </summary>
        public double TotalPopulation
        {
            get
            {
                double sum = 0;
                foreach (Lifestage stage in ChildStages)
                {
                    sum += stage.TotalPopulation;
                }
                return sum;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ChildStages = new List<Lifestage>();
            foreach (Lifestage stage in Apsim.Children(this, typeof(Lifestage)))
            {
                stage.OwningCycle = this;
                ChildStages.Add(stage);
            }
            
            int i= 0;
            //create new cohorts from the InitialPopulation[]
            foreach (Lifestage stage in ChildStages)
            {
                if (InitialPopulation.Length > i)
                {
                    Cohort newCohort = stage.NewCohort();
                    newCohort.Count = InitialPopulation[i];
                }
                i++;
            }
        }

        [EventSubscribe("DoLifecycle")]
        private void OnDoLifecycle(object sender, EventArgs e)
        {
            foreach (Lifestage stage in ChildStages)
            {
                CurrentLifestage = stage;
                stage.Process();
            }
        }
    }
}
