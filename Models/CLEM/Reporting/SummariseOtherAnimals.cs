using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Models.CLEM
{
    /// <summary>Ruminant summary</summary>
    /// <summary>This activity provides the other animal cohorts for reporting</summary>
    /// <summary>Remove if you do not need monthly herd summaries</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This component will generate an event for each cohort pr the animal types.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/OtherAnimalsSummary.htm")]
    public class SummariseOtherAnimals : CLEMModel
    {
        [Link]
        private ResourcesHolder resources = null;
        private int timestep = 0;
        private OtherAnimals otherAnimals;

        /// <summary>
        /// Report item was generated event handler
        /// </summary>
        public event EventHandler OnReportItemGenerated;

        /// <summary>
        /// The details of the summary group for reporting
        /// </summary>
        [JsonIgnore]
        public HerdReportItemGeneratedEventArgs ReportDetails { get; set; }

        /// <summary>
        /// List of filters that define the herd
        /// </summary>
        [JsonIgnore]
        private List<OtherAnimalsGroup> cohortFilters { get; set; }

        /// <summary>
        /// Report item generated and ready for reporting 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void ReportItemGenerated(HerdReportItemGeneratedEventArgs e)
        {
            OnReportItemGenerated?.Invoke(this, e);
        }

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            otherAnimals = resources.FindResourceGroup<OtherAnimals>();

            // determine any herd filtering
            cohortFilters = new List<OtherAnimalsGroup>();
            IModel current = this;
            while (current.GetType() != typeof(ZoneCLEM))
            {
                var filtergroup = current.Children.OfType<OtherAnimalsGroup>();
                if (filtergroup.Count() > 1)
                {
                    Summary.WriteMessage(this, "Multiple other animal filter groups have been supplied for [" + current.Name + "]" + Environment.NewLine + "Only the first filter group will be used.", MessageType.Warning);
                }
                if (filtergroup.FirstOrDefault() != null)
                {
                    cohortFilters.Insert(0, filtergroup.FirstOrDefault());
                }
                current = current.Parent as IModel;
            }

            // get full name for reporting
            current = this;
            string name = this.Name;
            while (current.GetType() != typeof(ZoneCLEM))
            {
                string quoteName = (current.GetType() == typeof(ActivitiesHolder)) ? "[" + current.Name + "]" : current.Name;
                name = quoteName + "." + name;
                current = current.Parent as IModel;
            }
        }

        /// <summary>
        /// Function to summarise the herd based on cohorts each month
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMHerdSummary")]
        private void OnCLEMHerdSummary(object sender, EventArgs e)
        {
            timestep++;
            if (!this.TimingOK) return;

            // get all cohorts
            IEnumerable<OtherAnimalsTypeCohort> cohortsToReport = otherAnimals.GetCohorts(cohortFilters, true);

            // weaned
            foreach (var oaCohort in  cohortsToReport.OrderBy(a => a.Sex).ThenBy(a => a.Age))
            {
                ReportDetails = new HerdReportItemGeneratedEventArgs()
                {
                    Group = oaCohort.ID.ToString(),
                    Age = oaCohort.Age,
                    AgeInYears = oaCohort.Age / 12,
                    Sex = oaCohort.Sex.ToString(),
                    Number = oaCohort.Number,
                    AverageWeight = oaCohort.Weight,
                    Breed = oaCohort.AnimalType.Name,
                    TimeStep = timestep
                };
                ReportItemGenerated(ReportDetails);
            }
        }
    }
}
