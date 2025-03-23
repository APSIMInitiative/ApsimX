using APSIM.Shared.Utilities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Activities
{
    /// <summary>Greenhouse gas emission</summary>
    /// <summary>This component will create a greenouse gas emmission</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityGrowPF))]
    [ValidParent(ParentType = typeof(RuminantTrucking))]
    [ValidParent(ParentType = typeof(PastureActivityBurn))]
    [Description("Define an emission based on parent activity details")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/GreenhouseGases/GreehnouseGasActivityEmission.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class GreenhouseGasActivityEmission : CLEMModel, IActivityCompanionModel
    {
        [Link]
        private readonly ResourcesHolder resources = null;
        private GreenhouseGasesType emissionStore;

        /// <summary>
        /// An identifier for this emission based on parent requirements
        /// </summary>
        [Description("Emission identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers", VisibleCallback = "ParentSuppliedIdentifiersPresent")]
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
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedMeasures", VisibleCallback = "ParentSuppliedMeasuresPresent")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Measure required")]
        [Description("Measure to use")]
        public string Measure { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Rate")]
        [Required, GreaterThanEqualValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// Label to assign each transaction created by this activity in ledgers
        /// </summary>
        [Description("Category for transactions")]
        [Models.Core.Display(Order = 500)]
        public string TransactionCategory { get; set; }

        /// <inheritdoc/>
        public void PrepareForTimestep()
        {
        }

        /// <inheritdoc/>
        public virtual List<ResourceRequest> RequestResourcesForTimestep(double activityMetric)
        {
            return new List<ResourceRequest>();
        }

        /// <inheritdoc/>
        public void PerformTasksForTimestep(double activityMetric)
        {
            double amountOfEmission;
            switch (Measure)
            {
                case "fixed":
                    amountOfEmission = Amount;
                    break;
                case "per total provided":
                    amountOfEmission = activityMetric * Amount;
                    break;
                case "per tonne km":
                    amountOfEmission = activityMetric * Amount;
                    if(Amount == 0)
                    {
                        string warn = $"No vehicle mass or km travelled has been provided for [a={NameWithParent}] as per tonne km to generate greenhouse gas emissions. Ensure vehicle mass is specified in Trucking settings of [{Parent.Name}]";
                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
                    }
                    break;
                default:
                    throw new NotImplementedException($"Unknown units [{((Measure == "") ? "Blank" : Measure)}] in [a={NameWithParent}]");
            }

            if (emissionStore != null && MathUtilities.IsPositive(amountOfEmission))
                emissionStore.Add(amountOfEmission, this.Parent as CLEMModel, null, TransactionCategory);
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
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            return $"\r\n<div class=\"activityentry\">Produce {DisplaySummaryResourceTypeSnippet(GreenhouseGasStoreName)} at rate of {CLEMModel.DisplaySummaryValueSnippet(Amount, warnZero:true)} {CLEMModel.DisplaySummaryValueSnippet(Measure)}</div>";
        }
        #endregion


    }
}
