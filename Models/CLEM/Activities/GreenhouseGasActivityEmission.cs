using APSIM.Shared.Utilities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models.CLEM.Activities
{
    /// <summary>Greenhouse gas emission</summary>
    /// <summary>This component will create a greenouse gas emmission</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityGrow))]
    [ValidParent(ParentType = typeof(TruckingSettings))]
    [ValidParent(ParentType = typeof(PastureActivityBurn))]
    [Description("Define an emission based on parent activity details")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/GreenhouseGases/Emission.htm")]

    public class GreenhouseGasActivityEmission : CLEMModel, IIdentifiableChildModel
    {
        [Link]
        private ResourcesHolder resources = null;
        private GreenhouseGasesType emissionStore;

        /// <summary>
        /// An identifier for this emission based on parent requirements
        /// </summary>
        [Description("Emission identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers")]
        public string Identifier { get; set; }

        /// <summary>
        /// Greenhouse gas store for emissions
        /// </summary>
        [Description("Greenhouse gas store for emission")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Use store with same name as this component if present", typeof(GreenhouseGases) } })]
        [System.ComponentModel.DefaultValue("Use store with same name as this component if present")]
        public string GreenhouseGasStoreName { get; set; }

        /// <summary>
        /// Payment style
        /// </summary>
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedUnits")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Units of measure required")]
        [Description("Calculation style")]
        public string Units { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Amount")]
        [Required, GreaterThanEqualValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// Label to assign each transaction created by this activity in ledgers
        /// </summary>
        [Description("Category for transactions")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Category for transactions required")]
        [Models.Core.Display(Order = 500)]
        public string TransactionCategory { get; set; }

        /// <inheritdoc/>
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool ShortfallCanAffectParentActivity { get; set; }

        /// <inheritdoc/>
        public virtual List<ResourceRequest> DetermineResourcesForActivity(double activityMetric)
        {
            return new List<ResourceRequest>();
        }

        /// <inheritdoc/>
        public void PerformTasksForActivity(double activityMetric)
        {
            double amountOfEmission;
            switch (Units)
            {
                case "fixed":
                    amountOfEmission = Amount;
                    break;
                case "total provided":
                    amountOfEmission = activityMetric;
                    break;
                case "per":
                    amountOfEmission = activityMetric * Amount;
                    break;
                default:
                    throw new NotImplementedException($"Unknown units [{((Units == "") ? "Blank" : Units)}] in [a={NameWithParent}]");
            }

            if (emissionStore != null && MathUtilities.IsPositive(amountOfEmission))
                emissionStore.Add(amountOfEmission, this.Parent as CLEMModel, "", TransactionCategory);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            if (MathUtilities.IsPositive(Amount))
            {
                if (GreenhouseGasStoreName is null || GreenhouseGasStoreName == "Use store with same name as this component if present")
                    emissionStore = resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, Name, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
                else
                    emissionStore = resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, GreenhouseGasStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GreenhouseGasesType;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public GreenhouseGasActivityEmission()
        {
            this.SetDefaults();
            TransactionCategory = "GreenhouseGas.[Emission]";
        }

        /// <summary>
        /// A method to return the list of identifiers relavent to this parent activity
        /// </summary>
        /// <returns>A list of identifiers</returns>
        public List<string> ParentSuppliedIdentifiers()
        {
            if (Parent != null && Parent is ICanHandleIdentifiableChildModels)
                return (Parent as ICanHandleIdentifiableChildModels).DefineIdentifiableChildModelLabels<GreenhouseGasActivityEmission>().Identifiers;
            else
                return new List<string>();
        }

        /// <summary>
        /// A method to return the list of units relavent to this parent activity
        /// </summary>
        /// <returns>A list of units</returns>
        public List<string> ParentSuppliedUnits()
        {
            if (Parent != null && Parent is ICanHandleIdentifiableChildModels)
                return (Parent as ICanHandleIdentifiableChildModels).DefineIdentifiableChildModelLabels<GreenhouseGasActivityEmission>().Units;
            else
                return new List<string>();
        }

        //#region descriptive summary

        ///// <inheritdoc/>
        //public override string ModelSummary()
        //{
        //    using (StringWriter htmlWriter = new StringWriter())
        //    {
        //        htmlWriter.Write("\r\n<div class=\"activityentry\">Pay ");
        //        htmlWriter.Write($"<span class=\"setvalue\">{Amount:#,##0.##}</span> ");
        //        htmlWriter.Write($"<span class=\"setvalue\">{Units}</span> ");
        //        htmlWriter.Write(" from ");

        //        htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(BankAccountName, "Account not set", HTMLSummaryStyle.Resource));
        //        htmlWriter.Write("</div>");
        //        htmlWriter.Write("\r\n<div class=\"activityentry\">This activity uses a category label ");

        //        htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(TransactionCategory, "Not set"));
        //        htmlWriter.Write(" for all transactions</div>");
        //        return htmlWriter.ToString();
        //    }
        //}
        //#endregion


    }
}
