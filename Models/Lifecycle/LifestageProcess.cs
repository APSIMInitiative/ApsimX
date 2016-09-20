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
        /// Reproduction process that creates new numbers for a new cohort in the linked lifestage
        /// </summary>
        Reproduction,
        /// <summary>
        /// Calculates and adjusts the cohort numbers based on a mortality function
        /// </summary>
        Mortality
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Lifestage))]
    public class LifestageProcess : Model
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
        /// 
        /// </summary>
        /// <param name="cohortItem"></param>
        public void ProcessCohort(Cohort cohortItem)
        {
            //apply the functions from the function list to this cohort
            foreach (IFunction func in FunctionList)
            {
                if (ProcessAction == ProcessType.Transfer)
                {
                    double numberToMove = cohortItem.Count * func.Value;
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
                    cohortItem.Count = cohortItem.Count * (1 - func.Value);
                }
                else if (ProcessAction == ProcessType.Reproduction)
                {
                    double numberToCreate = cohortItem.Count * func.Value;
                    if (numberToCreate > 0)
                    {
                        //transfer magic here
                        Lifestage destStage = cohortItem.OwningStage.OwningCycle.ChildStages.Find(s => s.Name == TransferTo);
                        cohortItem.OwningStage.Reproduce(cohortItem, destStage, numberToCreate);
                    }
                }
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
            FunctionList = new List<IFunction>();
            foreach (IFunction stage in Apsim.Children(this, typeof(IFunction)))
            {
                FunctionList.Add(stage);
            }
        }
    }
}
