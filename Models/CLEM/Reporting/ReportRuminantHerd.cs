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

namespace Models.CLEM.Reporting
{
    /// <summary>Ruminant reporting</summary>
    /// <summary>This activity writes individual ruminant details for reporting</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [Description("This component will generate a report of individual ruminant details. It uses the current timing rules and herd filters applied to its branch of the user interface tree. It also requires a suitable report object to be present.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/RuminantHerdReport.htm")]
    public class ReportRuminantHerd : CLEMModel
    {
        [Link]
        private ResourcesHolder Resources = null;

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
            ReportHerd();
        }

        /// <summary>
        /// Function to report herd individuals each month
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMValidate")]
        private void OncCLEMValidate(object sender, EventArgs e)
        {
            ReportHerd();
        }

        /// <summary>
        /// Do reporting of individuals
        /// </summary>
        /// <returns></returns>
        private void ReportHerd()
        {
            // warning if the same individual is in multiple filter groups it will be entered more than once

            if (this.Children.Where(a => a.GetType() == typeof(RuminantGroup)).Count() > 0)
            {
                // get all filter groups below.
                foreach (var fgroup in this.Children.Where(a => a.GetType() == typeof(RuminantGroup)))
                {
                    foreach (Ruminant item in Resources.RuminantHerd().Herd.Filter(fgroup))
                    {
                        ReportDetails = new RuminantReportItemEventArgs();
                        if (item is RuminantFemale)
                        {
                            ReportDetails.RumObj = item as RuminantFemale;
                        }
                        else
                        {
                            ReportDetails.RumObj = item as RuminantMale;
                        }
                        ReportItemGenerated(ReportDetails);
                    }
                }
            }
            else // no filter. Use entire herd
            {
                foreach (Ruminant item in Resources.RuminantHerd().Herd)
                {
                    ReportDetails = new RuminantReportItemEventArgs();
                    if (item is RuminantFemale)
                    {
                        ReportDetails.RumObj = item as RuminantFemale;
                    }
                    else
                    {
                        ReportDetails.RumObj = item as RuminantMale;
                    }
                    ReportItemGenerated(ReportDetails);
                }
            }
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
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