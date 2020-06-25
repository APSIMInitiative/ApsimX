using System;
using Models.Core;
using Models.Functions;

namespace Models.LifeCycle
{
    /// <summary>
    /// # [Name]
    /// Iterates through each cohort and adds the value of the Expression: 
    /// </summary>

    [Serializable]
    [Description("Iterates through each cohort and adds the value of the Expression")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCyclePhase))]
    public class AccumulateCohortExpression : Model, IFunction
    {
        
        /// <summary>The parent LifeCycle phase from which cohorts are evaluated</summary>
        private LifeCyclePhase parent { get; set; }

        /// <summary>The expression that will be calculated for each cohort</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction expression = null;

        private double AccumulatedCohortExpression { get; set; }

        /// <summary>The cohort currently being evaluated</summary>
        public Cohort CurrentCohort { get; set; }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            return AccumulatedCohortExpression;
        }
        
        /// <summary>At the start of the simulation get the parent</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            parent = Apsim.Parent(this, typeof(LifeCyclePhase)) as LifeCyclePhase;
        }

        /// <summary>When core LifeCycle processes are complete, calculate additional cohort specific expression</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("LifeCycleProcessComplete")]
        private void OnLifeCycleProcessComplete(object sender, EventArgs e)
        {
            AccumulatedCohortExpression = 0;
            if (parent.Cohorts != null)
            {
                foreach (Cohort c in parent.Cohorts)
                {
                    CurrentCohort = c;
                    AccumulatedCohortExpression += expression.Value();
                }
            }
        }
    }
}
