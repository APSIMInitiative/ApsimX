using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of Land Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all land types for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Land/Land.htm")]
    public class Land: ResourceBaseWithTransactions
    {
        /// <summary>
        /// Unit of area to be used in this simulation
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute("hectares")]
        [Description("Unit of area to be used in this simulation")]
        [Required]
        public string UnitsOfArea { get; set; }

        /// <summary>
        /// Conversion of unit of area to hectares (10,000 square metres)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Description("Unit of area conversion to hectares")]
        [Required, GreaterThanEqualValue(0)]
        public double UnitsOfAreaToHaConversion { get; set; }

        /// <summary>
        /// A method with argument to test
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="intarg"></param>
        /// <param name="doublearg"></param>
        /// <returns></returns>
        public string TestMethod(string txt, int intarg, double doublearg)
        {
            return "string:" + txt + "_int:"+intarg.ToString()+"_double:"+doublearg.ToString();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Land()
        {
            ReportedLandAllocation = new LandActivityAllocation();
            this.SetDefaults();
        }

        /// <summary>
        /// Land allocation details for reporting
        /// </summary>
        [JsonIgnore]
        public LandActivityAllocation ReportedLandAllocation { get; set; }

        private bool ChangeOccurred = false;
        
        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (var child in Children)
            {
                if (child is IResourceWithTransactionType)
                {
                    (child as IResourceWithTransactionType).TransactionOccurred += Resource_TransactionOccurred; ;
                }
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (IResourceWithTransactionType childModel in this.FindAllChildren<IResourceWithTransactionType>())
            {
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
            }
        }

        /// <summary>
        /// Report allocatios at start of timestep
        /// </summary>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            foreach (LandType childModel in this.FindAllChildren<LandType>())
            {
                double total = 0;
                if (childModel.AllocatedActivitiesList != null)
                {
                    foreach (LandActivityAllocation item in childModel.AllocatedActivitiesList)
                    {
                        ReportedLandAllocation = item;
                        if (ChangeOccurred)
                        {
                            OnAllocationReported(new EventArgs());
                        }
                    }
                    total = childModel.AllocatedActivitiesList.Sum(a => a.LandAllocated);
                }
                if (ChangeOccurred && childModel.LandArea - total > 0)
                {
                    ReportedLandAllocation = new LandActivityAllocation()
                    {
                        ActivityName = "Unallocated",
                        LandName = childModel.Name,
                        LandAllocated = childModel.LandArea - total
                    };
                    OnAllocationReported(new EventArgs());
                }
            }
            ChangeOccurred = false;
        }


        #region Transactions

        // Must be included away from base class so that APSIM Event.Subscriber can find them 

        /// <summary>
        /// Override base event
        /// </summary>
        protected new void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public new event EventHandler TransactionOccurred;

        private void Resource_TransactionOccurred(object sender, EventArgs e)
        {
            LastTransaction = (e as TransactionEventArgs).Transaction;
            OnTransactionOccurred(e);
            ChangeOccurred = true;
        }

        #endregion

        /// <summary>
        /// Override base event
        /// </summary>
        protected void OnAllocationReported(EventArgs e)
        {
            AllocationReported?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public event EventHandler AllocationReported;

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("Reported in ");
                if (UnitsOfArea == null || UnitsOfArea == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">Unspecified units of area</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + UnitsOfArea + "</span>");
                }
                htmlWriter.Write("</span>");


                if (UnitsOfAreaToHaConversion != 1)
                {
                    htmlWriter.Write(" (1 " + UnitsOfArea + " = <span class=\"setvalue\">" + UnitsOfAreaToHaConversion.ToString() + "</span> hectares)");
                }
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        }

        #endregion
    }
}
