// -----------------------------------------------------------------------
// <copyright file="LifeStageProcess.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.LifeCycle
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using Models.Functions;

    /// <summary>
    /// # [Name]
    /// Specifies the type of lifestage process
    /// </summary>
    public enum ProcessType
    {
        /// <summary>
        /// Transfer process that will move cohort numbers to another lifestage
        /// </summary>
        Transfer,
        /// <summary>
        /// 
        /// </summary>
        PhysiologicalGrowth,
        /// <summary>
        /// Calculates and adjusts the cohort numbers based on a mortality function
        /// </summary>
        Mortality
    }

    /// <summary>
    /// The general description of a lifestage process. A Lifestage can contain a number of these.
    /// </summary>
    interface ILifeStageProcess 
    {
        void Process(LifeStage host);
        void ProcessCohort(Cohort cohortItem);
    }

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
    public class LifeStageProcess : Model, ILifeStageProcess
    {
        /// <summary>
        /// The process type of this Process
        /// </summary>
        [Description("Lifestage Process type")]
        public ProcessType ProcessAction { get; set; }

        /// <summary>
        /// The name of the LifeStage to transfer cohorts to
        /// </summary>
        [Description("Transfer to Lifestage")]
        public string TransferTo { get; set; }

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
                if (ProcessAction == ProcessType.PhysiologicalGrowth)
                {
                    cohortItem.PhysiologicalAge += func.Value();
                }
                else if (ProcessAction == ProcessType.Transfer)
                {
                    double numberToMove = cohortItem.Count * func.Value();
                    if (numberToMove > 0)
                    {
                        //transfer magic here
                        LifeStage destStage = cohortItem.OwningStage.OwningCycle.ChildStages.Find(s => s.Name == TransferTo);
                        cohortItem.OwningStage.PromoteGraduates(cohortItem, destStage, numberToMove);
                    }
                }
                else if (ProcessAction == ProcessType.Mortality)
                {
                    //kill some creatures
                    double mortality = cohortItem.Count - cohortItem.Count * (1 - func.Value());
                    cohortItem.Count = cohortItem.Count * (1 - func.Value());
                    cohortItem.Mortality += mortality;      // can be multiple mortality events in a lifestage step
                }
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
