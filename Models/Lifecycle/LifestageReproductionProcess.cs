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
    /// A Reproduction process within a Lifestage.
    /// Reproduction process that creates new numbers for a new cohort in the linked lifestage
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Lifestage))]
    public class LifestageReproductionProcess: Model, ILifestageProcess
    {
        private IFunction ProgenyFunc = null;
        private IFunction FecundityFunc = null;

        /// <summary>
        /// 
        /// </summary>
        [Description("Transfer to Lifestage")]
        public string TransferTo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("Reproduction - Fecundity function name")]
        public string FecundityFunctionName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("Reproduction - Progeny production function name")]
        public string ProgenyFunctonName { get; set; }

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
                if (func == ProgenyFunc)    // assume there is only one of these for the process
                {
                    if (FecundityFunc != null && ProgenyFunc != null)
                    {
                        if (cohortItem.Fecundity < 0)
                            cohortItem.Fecundity = FecundityFunc.Value();
                        double progenyrate = ProgenyFunc.Value();

                        if (cohortItem.Fecundity > 0)
                        {
                            //number of Progeny produced per individual per timestep
                            double progenyRate = Math.Max(0.0, Math.Min(cohortItem.Fecundity, progenyrate));
                            if (progenyRate > 0)
                            {
                                double numberToCreate = cohortItem.Count * progenyRate;
                                if (numberToCreate > 0)
                                {
                                    //transfer magic here
                                    Lifestage destStage = cohortItem.OwningStage.OwningCycle.ChildStages.Find(s => s.Name == TransferTo);
                                    cohortItem.OwningStage.Reproduce(cohortItem, destStage, numberToCreate);
                                }
                                cohortItem.Fecundity -= progenyRate;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Fecundity and Progeny functions must be configured for " + this.Name + " lifestage");
                    }
                }
                else
                {
                    //execute another function
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
                // store the specific functions for use later in ProcessCohort()
                if (String.Compare(((IModel)func).Name, FecundityFunctionName, true) == 0)
                {
                    FecundityFunc = func;
                }
                else if (String.Compare(((IModel)func).Name, ProgenyFunctonName, true) == 0)
                {
                    ProgenyFunc = func;
                }
            }
        }
    }
}
