using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of finance models.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all finance types (bank accounts) for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Finance/Finance.htm")]
    public class Finance : ResourceBaseWithTransactions
    {
        /// <summary>
        /// Currency used
        /// </summary>
        [Description("Name of currency")]
        public string CurrencyName { get; set; }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = $"<div class=\"activityentry\">Currency is {CLEMModel.DisplaySummaryValueSnippet(CurrencyName, "Not specified")}</div>";
            return html;
        } 

        #endregion

    }
}
