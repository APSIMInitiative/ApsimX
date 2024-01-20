using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of Land Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("Resource group for all land types in the simulation")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Land/Land.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class Land : ResourceBaseWithTransactions
    {
        private bool changeOccurred = false;

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
        public static string TestMethod(string txt, int intarg, double doublearg)
        {
            return "string:" + txt + "_int:" + intarg.ToString() + "_double:" + doublearg.ToString();
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
                        if (changeOccurred)
                            OnAllocationReported(new EventArgs());
                    }
                    total = childModel.AllocatedActivitiesList.Sum(a => a.LandAllocated);
                }
                if (changeOccurred && childModel.LandArea - total > 0)
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
            changeOccurred = false;
        }

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

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">");
            htmlWriter.Write("Reported in ");
            if (UnitsOfArea == null || UnitsOfArea == "")
                htmlWriter.Write("<span class=\"errorlink\">Unspecified units of area</span>");
            else
                htmlWriter.Write("<span class=\"setvalue\">" + UnitsOfArea + "</span>");
            htmlWriter.Write("</span>");


            if (UnitsOfAreaToHaConversion != 1)
                htmlWriter.Write(" (1 " + UnitsOfArea + " = <span class=\"setvalue\">" + UnitsOfAreaToHaConversion.ToString() + "</span> hectares)");

            htmlWriter.Write("</div>");
            return htmlWriter.ToString();
        }

        #endregion
    }
}
