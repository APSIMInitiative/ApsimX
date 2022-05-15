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
using System.Globalization;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.ReportPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(Folder))]
    [Description("This report automatically generates a current balance column for each CLEM Resource Type\r\nassociated with the CLEM Resource Groups specified (name only) in the variable list.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/PasturePoolDetails.htm")]
    public class ReportPasturePoolDetails: Models.Report
    {
        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            List<string> variableNames = new List<string>();
            if (VariableNames != null)
            {
                for (int i = 0; i < this.VariableNames.Length; i++)
                {
                    // each variable name is now a GrazeFoodStoreType
                    bool isDuplicate = StringUtilities.IndexOfCaseInsensitive(variableNames, this.VariableNames[i].Trim()) != -1;
                    if (!isDuplicate && this.VariableNames[i] != string.Empty)
                    {
                        if (this.VariableNames[i].StartsWith("["))
                            variableNames.Add(this.VariableNames[i]);
                        else
                        {
                            string[] splitName = this.VariableNames[i].Split('.');
                            if (splitName.Count() == 2)
                            {
                                // make each pool entry
                                for (int j = 0; j <= 12; j++)
                                {
                                    if (splitName[1].ToLower() != "growth" | j == 0)
                                        variableNames.Add("[Resources].GrazeFoodStore." + splitName[0] + ".Pool(" + j.ToString() + ", true)." + splitName[1] + " as " + splitName[0] + "" + j.ToString() + "" + splitName[1]);
                                }
                                if (splitName[1] == "Amount")
                                {
                                    // add amounts
                                    variableNames.Add("[Resources].GrazeFoodStore." + splitName[0] + ".Amount as TotalAmount");
                                    variableNames.Add("[Resources].GrazeFoodStore." + splitName[0] + ".KilogramsPerHa as TotalkgPerHa");
                                }
                            }
                            else
                                throw new ApsimXException(this, "Invalid report property. Expecting full property link or [GrazeFoodStoreTypeName].Property");
                        }
                    }
                }
                // check if clock.today was included.
                if(!variableNames.Contains("[Clock].Today"))
                    variableNames.Insert(0, "[Clock].Today");
            }

            VariableNames = variableNames.ToArray();
            if (EventNames == null || EventNames.Count() == 0)
                EventNames = new string[] { "[Clock].CLEMHerdSummary" };

            SubscribeToEvents();
        }

    }
}
