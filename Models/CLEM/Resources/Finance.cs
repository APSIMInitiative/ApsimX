using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of finance models.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("Resource group for all finance types (bank accounts) in the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Finance/Finance.htm")]
    public class Finance : ResourceBaseWithTransactions
    {
        /// <summary>
        /// Currency used
        /// </summary>
        [Description("Name of currency")]
        public string CurrencyName { get; set; }


        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string html = $"<div class=\"activityentry\">Currency is {CLEMModel.DisplaySummaryValueSnippet(CurrencyName, "Not specified")}</div>";
            return html;
        } 


    }
}
