// -----------------------------------------------------------------------
// <copyright file="LifeStageImmigrationProcess.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace Models.LifeCycle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Models.Core;
    using Models.PMF.Functions;

    /// <summary>
    /// # [Name]
    /// An immigration process within a Lifestage.
    /// Immigration process that brings in new numbers for a new cohort in the linked lifestage
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeStage))]
    public class LifeStageImmigrationProcess : Model, ILifeStageProcess
    {
        [NonSerialized]
        private List<IFunction> FunctionList;

        /// <summary>
        /// The number of immigrants
        /// </summary>
        [Description("Immigration numbers into Lifestage")]
        public double Immigrants { get; set; }

        /// <summary>
        /// Applies each function in this Lifestage process to a cohort item that is owned by a Lifestage
        /// </summary>
        /// <param name="cohortItem">An existing cohort</param>
        public void ProcessCohort(Cohort cohortItem)
        {
            //apply the functions from the function list to this cohort
            foreach (IFunction func in FunctionList)
            {

            }
        }

        /// <summary>
        /// At the start of the simulation get the functions required
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
