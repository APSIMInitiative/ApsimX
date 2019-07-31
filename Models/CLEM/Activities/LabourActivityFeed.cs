using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant feed activity</summary>
    /// <summary>This activity provides food to specified people based on a feeding style</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs human feeding based upon the current labour filtering and a feeding style.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/LabourFeed.htm")]
    public class LabourActivityFeed : CLEMActivityBase
    {
        //[Link]
        //Clock Clock = null;

        /// <summary>
        /// Name of Human Food to use (with Resource Group name appended to the front [separated with a '.'])
        /// </summary>
        [Description("Food to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] {typeof(HumanFoodStore)} )]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Food type required")]
        public string FeedTypeName { get; set; }

        /// <summary>
        /// Feed type
        /// </summary>
        [XmlIgnore]
        public HumanFoodStoreType FeedType { get; set; }

        //private double feedRequired = 0;

        /// <summary>
        /// Feeding style to use
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(LabourFeedActivityTypes.SpecifiedDailyAmountPerAE)]
        [Description("Feeding style to use")]
        [Required]
        public LabourFeedActivityTypes FeedStyle { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityFeed()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate FeedType resource
            FeedType = Resources.GetResourceItem(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as HumanFoodStoreType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded = 0;
            return daysNeeded;
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
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
            ResourceShortfallOccurred?.Invoke(this, e);
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
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">Feed people ";

            html += "</div>";

            return html;
        }
    }
}
