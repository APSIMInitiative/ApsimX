using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;
using Models.CLEM.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to perform controlled burning of native pastures</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Apply controlled burning to a specified graze food store (i.e. native pasture paddock)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Pasture/BurnPasture.htm")]
    public class PastureActivityBurn: CLEMActivityBase, IHandlesActivityCompanionModels
    {
        private GrazeFoodStoreType pasture;
        private GreenhouseGasesType methaneStore;
        private GreenhouseGasesType n2oStore;
        private double areaToDo;
        private double pastureToDo;
        private double areaToSkip;

        /// <summary>
        /// Minimum proportion green for fire to carry
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0.5)]
        [Description("Minimum proportion green for fire to carry")]
        [Required(AllowEmptyStrings = false), Proportion]
        public double MinimumProportionGreen { get; set; }

        /// <summary>
        /// Name of graze food store/paddock to burn
        /// </summary>
        [Description("Name of graze food store/paddock to burn")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(GrazeFoodStore) } })]
        [Required(AllowEmptyStrings = false)]
        public string PaddockName { get; set; }

        /// <summary>
        /// Methane store for emissions
        /// </summary>
        [Description("Greenhouse gas store for methane emissions")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Use store named Methane if present", typeof(GreenhouseGases) } })]
        [System.ComponentModel.DefaultValue("Use store named Methane if present")]
        public string MethaneStoreName { get; set; }

        /// <summary>
        /// Nitrous oxide store for emissions
        /// </summary>
        [Description("Greenhouse gas store for nitrous oxide emissions")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Use store named N2O if present", typeof(GreenhouseGases) } })]
        [System.ComponentModel.DefaultValue("Use store named N2O if present")]
        public string NitrousOxideStoreName { get; set; }

        /// <summary>
        /// Burning efficency
        /// </summary>
        [Description("Biomass burning efficiency")]
        [System.ComponentModel.DefaultValue(0.76)]
        [Required, GreaterThanValue(0)]
        public double BurningEfficiency { get; set; }

        /// <summary>
        /// Carbon content
        /// </summary>
        [Description("Carbon content of fuel")]
        [System.ComponentModel.DefaultValue(0.46)]
        [Required, GreaterThanValue(0)]
        public double CarbonContent { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PastureActivityBurn()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get pasture
            pasture = Resources.FindResourceType<GrazeFoodStore, GrazeFoodStoreType>(this, PaddockName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            if (MethaneStoreName is null || MethaneStoreName == "Use store named Methane if present")
                methaneStore = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, "Methane", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
            else
                methaneStore = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, MethaneStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            if (NitrousOxideStoreName is null || NitrousOxideStoreName == "Use store named N2O if present")
                n2oStore = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, "N2O", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
            else
                n2oStore = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, NitrousOxideStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                            "per ha to burn",
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            areaToSkip = 0;
            areaToDo = pasture.Manager.Area;
            // proportion green
            double greenPasture = pasture.Pools.Where(a => a.Age < 2).Sum(a => a.Amount);
            pastureToDo = pasture.Amount;
            if (MathUtilities.IsPositive(pastureToDo))
            {
                if (MathUtilities.IsGreaterThan(greenPasture / pastureToDo, MinimumProportionGreen))
                {
                    areaToSkip = areaToDo;
                    Status = ActivityStatus.Warning;
                    AddStatusMessage("Too green to burn");
                }
            }

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per ha to burn":
                        valuesForCompanionModels[valueToSupply.Key] = areaToDo - areaToSkip;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if(MathUtilities.IsPositive(areaToDo - areaToSkip))
            {
                // remove biomass
                pasture.Remove(new ResourceRequest()
                {
                    ActivityModel = this,
                    Required = pastureToDo,
                    AllowTransmutation = false,
                    Category = TransactionCategory,
                    ResourceTypeName = PaddockName,
                    Resource = pasture
                }
                );

                // add emissions
                double burnkg = pastureToDo * BurningEfficiency * CarbonContent; // burnkg * burning efficiency * carbon content
                                                                                 //TODO change emissions for green material
                methaneStore?.Add(burnkg * 1.333 * 0.0035, this, PaddockName, TransactionCategory); // * 21; // methane emissions from fire (CO2 eq)

                n2oStore?.Add(burnkg * 1.571 * 0.0076 * 0.12, this, PaddockName, TransactionCategory); // * 21; // N20 emissions from fire (CO2 eq)

                // TODO: add fertilisation to pasture for given period.

            }
            SetStatusSuccessOrPartial(areaToSkip > 0);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write($"\r\n<div class=\"activityentry\">Burn {DisplaySummaryResourceTypeSnippet(PaddockName, "Pasture Not Set", nullGeneralYards: false)}");
            htmlWriter.Write($" if less than {DisplaySummaryValueSnippet(MinimumProportionGreen.ToString("0.#%"), warnZero: true)} green.");
            htmlWriter.Write($" with a burning efficiency of {DisplaySummaryValueSnippet(BurningEfficiency, warnZero: true)} and ");
            htmlWriter.Write($" and a carbon content of {DisplaySummaryValueSnippet(CarbonContent, warnZero: true)}");
            htmlWriter.Write("</div>");

            htmlWriter.Write("\r\n<div class=\"activityentry\">Methane emissions will be placed in ");
            if (MethaneStoreName is null || MethaneStoreName == "Use store named Methane if present")
                htmlWriter.Write("<span class=\"resourcelink\">[GreenhouseGases].Methane</span> if present");
            else
                htmlWriter.Write($"<span class=\"resourcelink\">{MethaneStoreName}</span>");

            htmlWriter.Write("</div>");

            htmlWriter.Write("\r\n<div class=\"activityentry\">Nitrous oxide emissions will be placed in ");
            if (NitrousOxideStoreName is null || NitrousOxideStoreName == "Use store named N2O if present")
                htmlWriter.Write("<span class=\"resourcelink\">[GreenhouseGases].N2O</span> if present");
            else
                htmlWriter.Write($"<span class=\"resourcelink\">{NitrousOxideStoreName}</span>");

            htmlWriter.Write("</div>");
            return htmlWriter.ToString();
        } 
        #endregion

    }
}
