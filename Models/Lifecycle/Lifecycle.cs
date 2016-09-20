using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.Lifecycle
{
    /// <summary>
    /// A lifecycle that contains lifestages. This lifecycle manages a list of cohorts.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class Lifecycle : Model
    {
        private List<Lifestage> ChildStages = null;

        /// <summary>
        /// 
        /// </summary>
        public Lifecycle()
        {
            ChildStages = new List<Lifestage>();
        }

        /// <summary>
        /// 
        /// </summary>
        [Description("init stages")]
        public double[] InitialPopulation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            foreach (Lifestage stage in Apsim.Children(this, typeof(Lifestage)))
            {
                ChildStages.Add(stage);
            }
            //dummy up some links
            for (int s = 0; s < ChildStages.Count; s++)
            {
                ChildStages[s].LinkNext(ChildStages[(s+1) % (ChildStages.Count+1)]);
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
                stage.Process();
            }
        }
    }
}
