using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Tracking settings for Ruminant purchases and sales</summary>
    /// <summary>If this model is provided within RuminantActivityBuySell, trucking costs and loading rules will occur</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityBuySell))]
    [Description("This provides trucking settings for the Ruminant Buy and Sell Activity and will determine costs and emissions if required.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/Trucking.htm")]
    public class TruckingSettings : CLEMModel
    {
        [Link]
        private ResourcesHolder Resources = null;

        /// <summary>
        /// Distance to market
        /// </summary>
        [Description("Distance to market (km)")]
        [Required, GreaterThanEqualValue(0)]
        public double DistanceToMarket { get; set; }

        /// <summary>
        /// Cost of trucking ($/km/truck)
        /// </summary>
        [Description("Cost of trucking ($/km/truck)")]
        [Required, GreaterThanEqualValue(0)]
        public double CostPerKmTrucking { get; set; }

        /// <summary>
        /// Number of 450kg animals per truck load
        /// </summary>
        [Description("Number of 450kg animals per truck load")]
        [Required, GreaterThanEqualValue(0)]
        public double Number450kgPerTruck { get; set; }

        /// <summary>
        /// Minimum number of truck loads before selling (0 continuous sales)
        /// </summary>
        [Description("Minimum number of truck loads before selling (0 continuous sales)")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumTrucksBeforeSelling { get; set; }

        /// <summary>
        /// Minimum proportion of truck load before selling (0 continuous sales)
        /// </summary>
        [Description("Minimum proportion of truck load before selling (0 continuous sales)")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumLoadBeforeSelling { get; set; }

        /// <summary>
        /// Minimum number of truck loads before buying (0 continuous purchase)
        /// </summary>
        [Description("Minimum number of truck loads before buying (0 no limit)")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumTrucksBeforeBuying { get; set; }

        /// <summary>
        /// Minimum proportion of truck load before buying (0 continuous purchase)
        /// </summary>
        [Description("Minimum proportion of truck load before buying (0 no limit)")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumLoadBeforeBuying { get; set; }

        /// <summary>
        /// Truck CO2 emissions per km
        /// </summary>
        [Description("Truck CO2 emissions per km")]
        [Required, GreaterThanEqualValue(0)]
        public double TruckCO2Emissions { get; set; }

        /// <summary>
        /// Truck methane emissions per km
        /// </summary>
        [Description("Truck Methane emissions per km")]
        [Required, GreaterThanEqualValue(0)]
        public double TruckMethaneEmissions { get; set; }

        /// <summary>
        /// Truck N2O emissions per km
        /// </summary>
        [Description("Truck Nitrous oxide emissions per km")]
        [Required, GreaterThanEqualValue(0)]
        public double TruckN2OEmissions { get; set; }

        private GreenhouseGasesType CO2Store;
        private GreenhouseGasesType MethaneStore;
        private GreenhouseGasesType N2OStore;

        /// <summary>
        /// Constructor
        /// </summary>
        public TruckingSettings()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            GreenhouseGases gasesPresent = Resources.GreenhouseGases();

            if (gasesPresent != null)
            {
                if (TruckMethaneEmissions > 0)
                {
                    MethaneStore = Resources.GetResourceItem(this, typeof(GreenhouseGases), "Methane", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as GreenhouseGasesType;
                }
                if (TruckCO2Emissions > 0)
                {
                    CO2Store = Resources.GetResourceItem(this, typeof(GreenhouseGases), "CO2", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as GreenhouseGasesType;
                }
                if (TruckN2OEmissions > 0)
                {
                    N2OStore = Resources.GetResourceItem(this, typeof(GreenhouseGases), "N2O", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as GreenhouseGasesType;
                }
            }
        }

        /// <summary>
        /// Method to report trucking emissions.
        /// </summary>
        /// <param name="numberOfTrucks">Number of trucks</param>
        /// <param name="isSales">Determines if this is a sales or purchase shipment</param>
        public void ReportEmissions(int numberOfTrucks, bool isSales)
        {
            if(numberOfTrucks > 0)
            {
                List<string> gases = new List<string>() { "Methane", "CO2", "N2O" };
                double emissions = 0;
                foreach (string gas in gases)
                {
                    GreenhouseGasesType gasstore = null;
                    switch (gas)
                    {
                        case "Methane":
                            gasstore = MethaneStore;
                            emissions = TruckMethaneEmissions;
                            break;
                        case "CO2":
                            gasstore = CO2Store;
                            emissions = TruckCO2Emissions;
                            break;
                        case "N2O":
                            gasstore = N2OStore;
                            emissions = TruckN2OEmissions;
                            break;
                        default:
                            gasstore = null;
                            break;
                    }

                    if (gasstore != null && emissions > 0)
                    {
                        gasstore.Add(numberOfTrucks * DistanceToMarket * emissions , this.Parent as CLEMModel, "Trucking "+(isSales?"sales":"purchases"));
                    }
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
            html += "\n<div class=\"activityentry\">It is <span class=\"setvalue\">" + DistanceToMarket.ToString("#.###") + "</span> km to market and costs <span class=\"setvalue\">" + CostPerKmTrucking.ToString("0.###") + "</span> per km per truck";
            html += "</div>";

            html += "\n<div class=\"activityentry\">Each truck load can carry <span class=\"setvalue\">" + Number450kgPerTruck.ToString("#.###") + "</span> 450 kg individuals ";
            html += "</div>";

            if(MinimumLoadBeforeSelling>0 || MinimumTrucksBeforeSelling>0)
            {
                html += "\n<div class=\"activityentry\">";
                if(MinimumTrucksBeforeSelling>0)
                {
                    html += "A minimum of <span class=\"setvalue\">" + MinimumTrucksBeforeSelling.ToString("###") + "</span> truck loads is required";
                }
                if (MinimumLoadBeforeSelling > 0)
                {
                    if(MinimumTrucksBeforeSelling>0)
                    {
                        html += " and each ";
                    }
                    else
                    {
                        html += "Each ";

                    }
                    html += "truck must be at least <span class=\"setvalue\">" + MinimumLoadBeforeSelling.ToString("0.##%") + "</span> full";
                }
                html += " for sales</div>";
            }

            if (MinimumLoadBeforeBuying > 0 || MinimumTrucksBeforeBuying > 0)
            {
                html += "\n<div class=\"activityentry\">";
                if (MinimumTrucksBeforeBuying > 0)
                {
                    html += "A minimum of <span class=\"setvalue\">" + MinimumTrucksBeforeBuying.ToString("###") + "</span> truck loads is required";
                }
                if (MinimumLoadBeforeBuying > 0)
                {
                    if (MinimumTrucksBeforeBuying > 0)
                    {
                        html += " and each ";
                    }
                    else
                    {
                        html += "Each ";

                    }
                    html += "truck must be at least <span class=\"setvalue\">" + MinimumLoadBeforeBuying.ToString("0.##%") + "</span> full";
                }
                html += " for purchases</div>";
            }


            if (TruckMethaneEmissions > 0 || TruckN2OEmissions > 0)
            {
                html += "\n<div class=\"activityentry\">Each truck will emmit <span class=\"setvalue\">";
                if (TruckMethaneEmissions > 0)
                {
                    html += TruckMethaneEmissions.ToString("0.###") + "</span> kg methane per km";
                }
                if (MinimumLoadBeforeSelling > 0)
                {
                    if (MinimumTrucksBeforeSelling > 0)
                    {
                        html += " and ";
                    }
                    else
                    {
                        html += "<span class=\"setvalue\">" + TruckN2OEmissions.ToString("0.###") + "</span> kg N<sub>2</sub>O per km";
                    }
                }
                html += "</div>";
            }

            return html;
        }

    }
}
