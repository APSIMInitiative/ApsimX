namespace Models.LifeCycle
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using Models.Functions;

    /// <summary>
    /// # [Name]
    /// An immigration process within a Lifestage.
    /// Immigration process that brings in new numbers for a new cohort in the linked lifestage
    ///
    ///|Property          .|Type    .|Units  .|Description              .| 
    ///|---|---|---|:---|
    ///|Immigrants   |double   |  |The number of immigrants at this timestep    |
    ///
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeStage))]
    public class LifeStageImmigration : Model, ILifeStageProcess
    {
        [NonSerialized]
        private List<IFunction> FunctionList;

        /// <summary>
        /// The number of immigrants during this process
        /// </summary>
        [NonSerialized]
        private double immigrantNumbers;

        /// <summary>
        /// Report the number of immigrants at this timestep
        /// </summary>
        public double Immigrants
        {
            get
            {
                return immigrantNumbers;
            }
        }

        /// <summary>
        /// Process this lifestage before cohorts are processed.
        /// This process is immigration.
        /// </summary>
        /// <param name="host">The host LifeStage</param>
        public void Process(LifeStage host)
        {
            double number = 0;
            // apply the functions from the function list to calculate the total number of immigrants
            foreach (IFunction func in FunctionList)
            {
                number += func.Value();
            }
            immigrantNumbers = number;
            host.AddImmigrants(number);
        }

        /// <summary>
        /// Applies each function in this Lifestage process to a cohort item that is owned by a Lifestage
        /// </summary>
        /// <param name="cohortItem">An existing cohort</param>
        public void ProcessCohort(Cohort cohortItem)
        {
            //immigration does not require this
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
