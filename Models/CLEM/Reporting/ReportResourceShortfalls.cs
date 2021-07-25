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

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing resource shortfall output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.ReportPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(Folder))]
    [Description("This report automatically generates a ledger of all shortfalls in CLEM Resource requests.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/ResourceShortfalls.htm")]
    public class ReportResourceShortfalls: Models.Report
    {
        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            dataToWriteToDb = null;
            // sanitise the variable names and remove duplicates
            List<string> variableNames = new List<string>
            {
                "[Clock].Today as Date",
                "[Activities].LastShortfallResourceRequest.ResourceTypeName as Resource",
                "[Activities].LastShortfallResourceRequest.ActivityModel.Name as Activity",
                "[Activities].LastShortfallResourceRequest.Category as Category",
                "[Activities].LastShortfallResourceRequest.Required as Required",
                "[Activities].LastShortfallResourceRequest.Available as Available"
            };

            EventNames = new string[] { "[Activities].ResourceShortfallOccurred" };

            // Tidy up variable/event names.
            VariableNames = variableNames.ToArray();
            VariableNames = TidyUpVariableNames();
            EventNames = TidyUpEventNames();
            this.FindVariableMembers();

            // Subscribe to events.
            foreach (string eventName in EventNames)
            {
                if (eventName != string.Empty)
                {
                    events.Subscribe(eventName.Trim(), DoOutputEvent);
                }
            }
        }

    }
}
