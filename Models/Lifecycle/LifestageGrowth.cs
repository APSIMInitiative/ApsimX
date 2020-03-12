namespace Models.LifeCycle
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using Models.Functions;

    /// <summary>
    /// A process within a Lifestage which will be of ProcessType (Transfer, PhysiologicalGrowth, Mortality).
    /// 
    ///|Property          .|Type    .|Units  .|Description              .| 
    ///|---|---|---|:---|
    ///|ProcessAction     |ProcessType   |  |The type of this process    |
    ///|TransferTo        |string        |  |The name of the LifeStage to transfer cohorts to |
    ///
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeStage))]
    public class LifeStageGrowth : Model, ILifeStageProcess
    {

        [NonSerialized]
        private List<IFunction> FunctionList;

        /// <summary>
        /// Process this lifestage before cohorts are processed
        /// </summary>
        public void Process(LifeStage host)
        {
        }

        /// <summary>
        /// Applies each function in this Lifestage process to a cohort item that is owned by a Lifestage
        /// </summary>
        /// <param name="cohortItem"></param>
        public void ProcessCohort(Cohort cohortItem)
        {
            //apply the functions from the function list to this cohort
            foreach (IFunction func in FunctionList)
            {
                cohortItem.PhysiologicalAge += func.Value();
            }
        }
        
        /// <summary>
        /// At the start of the simulation get the functions required
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event parameters</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            FunctionList = new List<IFunction>();
            foreach (IFunction func in Apsim.Children(this, typeof(IFunction)))
            {
                FunctionList.Add(func);
            }
        }
    }
}
