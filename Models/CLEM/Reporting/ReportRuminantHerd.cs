using Models.Core;
using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Reporting
{
    /// <summary>Ruminant reporting</summary>
    /// <summary>This activity writes individual ruminant details for reporting</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [Description("Provides individual ruminant details for reporting. This uses the current timing rules and herd filters applied to its branch of the user interface tree. It also requires a suitable report object to be present.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/RuminantHerdReport.htm")]
    public class ReportRuminantHerd : CLEMModel, IValidatableObject
    {
        [Link]
        private ResourcesHolder resources = null;
        private RuminantHerd ruminantHerd;

        /// <summary>
        /// Report at initialisation
        /// </summary>
        [Description("Report at start of simulation")]
        [System.ComponentModel.DefaultValue(true)]
        public bool ReportAtStart { get; set; }

        /// <summary>
        /// Report item was generated event handler
        /// </summary>
        public event EventHandler OnReportItemGenerated;

        /// <summary>
        /// The details of the summary group for reporting
        /// </summary>
        [JsonIgnore]
        public RuminantReportItemEventArgs ReportDetails { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReportRuminantHerd()
        {
            SetDefaults();
        }

        /// <summary>
        /// Report item generated and ready for reporting 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void ReportItemGenerated(RuminantReportItemEventArgs e)
        {
            OnReportItemGenerated?.Invoke(this, e);
        }

        /// <summary>
        /// Function to report herd individuals each month
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMHerdSummary")]
        private void OnCLEMHerdSummary(object sender, EventArgs e)
        {
            if(TimingOK)
                ReportHerd();
        }

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            ruminantHerd = resources.FindResourceGroup<RuminantHerd>();
            var results = new List<ValidationResult>();
            // check that this activity has a parent of type CropActivityManageProduct

            if (ruminantHerd is null)
            {
                string[] memberNames = new string[] { "Missing resource" };
                results.Add(new ValidationResult($"No ruminant herd resource could be found for [ReportRuminantHerd] [{this.Name}]", memberNames));
            }
            if (!this.FindAllChildren<RuminantGroup>().Any())
            {
                string[] memberNames = new string[] { "Missing ruminant filter group" };
                results.Add(new ValidationResult($"The [ReportRuminantHerd] [{this.Name}] requires at least one filter group to identify individuals to report", memberNames));
            }
            return results;
        }

        #endregion

        /// <summary>
        /// Function to report herd individuals each month
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMValidate")]
        private void OnCLEMValidate(object sender, EventArgs e)
        {
            if(ReportAtStart)
                ReportHerd();
        }

        /// <summary>
        /// Do reporting of individuals
        /// </summary>
        /// <returns></returns>
        private void ReportHerd()
        {
            // warning if the same individual is in multiple filter groups it will be entered more than once

            // get all filter groups below.
            foreach (var fgroup in this.FindAllChildren<RuminantGroup>())
            {
                foreach (Ruminant item in fgroup.Filter(ruminantHerd?.Herd))
                {
                    ReportDetails = new RuminantReportItemEventArgs();
                    if (item is RuminantFemale)
                        ReportDetails.RumObj = item as RuminantFemale;
                    else
                        ReportDetails.RumObj = item as RuminantMale;
                    ReportItemGenerated(ReportDetails);
                }
            }
        }

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string html = "";
            return html;
        }

    }

    /// <summary>
    /// New ruminant report item event args
    /// </summary>
    [Serializable]
    public class RuminantReportItemEventArgs : EventArgs
    {
        /// <summary>
        /// Individual ruminant to report as Female
        /// </summary>
        public object RumObj { get; set; }
        /// <summary>
        /// Individual ruminant to report
        /// </summary>
        public Ruminant Individual { get { return RumObj as Ruminant; } }
        /// <summary>
        /// Individual ruminant to report as Female
        /// </summary>
        public RuminantFemale Female { get { return RumObj as RuminantFemale; } }
        /// <summary>
        /// Individual ruminant to report as Male
        /// </summary>
        public RuminantMale Male { get { return RumObj as RuminantMale; } }
        /// <summary>
        /// Category string
        /// </summary>
        public string Category { get; set; }
    }
}