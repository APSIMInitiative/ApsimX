using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Core;
using Models.PMF.Functions;

namespace Models.Lifecycle
{
    /// <summary>
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
    interface ILifestageProcess 
    {
        void ProcessCohort(Cohort cohortItem);
    }

    /// <summary>
    /// A process within a Lifestage which will be of ProcessType.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Lifestage))]
    public class LifestageProcess : Model, ILifestageProcess
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("Lifestage Process type")]
        public ProcessType ProcessAction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("Transfer to Lifestage")]
        public string TransferTo { get; set; }

        [NonSerialized]
        private List<IFunction> FunctionList;

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
                        Lifestage destStage = cohortItem.OwningStage.OwningCycle.ChildStages.Find(s => s.Name == TransferTo);
                        cohortItem.OwningStage.PromoteGraduates(cohortItem, destStage, numberToMove);
                    }
                }
                else if (ProcessAction == ProcessType.Mortality)
                {
                    //kill some creatures
                    double mortality = cohortItem.Count - cohortItem.Count * (1 - func.Value());
                    cohortItem.Count = cohortItem.Count * (1 - func.Value());
                    cohortItem.Mortality = mortality;
                }
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
