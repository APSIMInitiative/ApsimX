using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Off farm labour activities
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages labour supplied and income derived from an off-farm task.")]
    public class LabourActivityOffFarm: CLEMActivityBase
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Daily labour rate
        /// </summary>
        [Description("Daily labour rate")]
        [Required]
        public double DailyRate { get; set; }

        /// <summary>
        /// Days worked
        /// </summary>
        [Description("Days work available each month")]
        [Required, ArrayItemCount(12, ErrorMessage ="Days works required for each of 12 months")]
        public double[] DaysWorkAvailableEachMonth { get; set; }

        /// <summary>
        /// Bank account name to pay to
        /// </summary>
        [Description("Bank account name to pay to")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of bank account to pay to required")]
        public string BankAccountName { get; set; }

        private FinanceType bankType { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate BankType resource
            bankType = Resources.GetResourceItem(this, typeof(Finance), BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as FinanceType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            // zero based month index for array
            int month = Clock.Today.Month - 1;

            if (DaysWorkAvailableEachMonth[month] > 0)
            {
                foreach (LabourFilterGroup filter in Apsim.Children(this, typeof(LabourFilterGroup)))
                {
                    if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
                    ResourceRequestList.Add(new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = DaysWorkAvailableEachMonth[month],
                        ResourceType = typeof(Labour),
                        ResourceTypeName = "",
                        ActivityModel = this,
                        Reason = this.Name,
                        FilterDetails = new List<object>() { filter }// filter.ToList<object>() // this.Children.Where(a => a.GetType() == typeof(LabourFilterGroup)).ToList<object>()
                    }
                    );
                }
            }
            else
            {
                return null;
            }
            return ResourceRequestList;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            // days provided from labour set in the only request in the resourceResquestList
            // receive payment for labour if bank type exists
            if (bankType != null)
            {
                bankType.Add(ResourceRequestList.FirstOrDefault().Available * DailyRate, "Off farm labour", this.Name);
            }
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }

    }
}
