using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using APSIM.Shared.Utilities;
using System.Data;
using System.IO;
using Models.CLEM.Resources;
using Models.Core.Attributes;
using Models.Core.Run;
using Models.Storage;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing resource shortfall output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.CLEMReportResultsPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(Folder))]
    [Description("This report automatically generates a ledger of all shortfalls in resource requests")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/ResourceShortfalls.htm")]
    public class ReportResourceShortfalls: Models.Report
    {
        /// <summary>
        /// The pasture shortfall as proportion of desired intake before reported
        /// </summary>
        [Summary]
        [Description("Pasture shortfall as proportion of desired intake before reported")]
        [Required, GreaterThanEqualValue(0), Proportion]
        [System.ComponentModel.DefaultValueAttribute(0.03)]
        public double PropPastureShortfallOfDesiredIntake { get; set; }

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            List<string> variableNames = new List<string>
            {
                "[Clock].Today as Date",
                "[Activities].LastShortfallResourceRequest.ResourceTypeName as Resource",
                "[Activities].LastShortfallResourceRequest.ActivityModel.Name as Activity",
                "[Activities].LastShortfallResourceRequest.Category as Category",
                "[Activities].LastShortfallResourceRequest.Available as Available",
                "[Activities].LastShortfallResourceRequest.Required as Required",
//                "[Activities].LastShortfallResourceRequest.Provided as Provided",
                "[Activities].LastShortfallResourceRequest.ShortfallStatus as Status"
            };

            EventNames = new string[] { "[Activities].ResourceShortfallOccurred" };
            VariableNames = variableNames.ToArray();

            SubscribeToEvents();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReportResourceShortfalls()
        {
            CLEMModel.SetPropertyDefaults(this);
        }
    }
}
