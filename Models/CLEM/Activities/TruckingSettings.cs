using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Tracking settings for Ruminant purchases and sales</summary>
    /// <summary>If this model is provided within RuminantActivityBuySell, trucking costs and loading rules will occur</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityBuySell))]
    [Description("This provides trucking settings for the Ruminant Buy and Sell Activity and will determine costs and emissions if required.")]
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
        /// Truck NOx emissions per km
        /// </summary>
        [Description("Truck NOx emissions per km")]
        [Required, GreaterThanEqualValue(0)]
        public double TruckNOxEmissions { get; set; }

        private GreenhouseGasesType CO2Store;
        private GreenhouseGasesType MethaneStore;
        private GreenhouseGasesType NOxStore;

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
                if (TruckNOxEmissions > 0)
                {
                    NOxStore = Resources.GetResourceItem(this, typeof(GreenhouseGases), "NOx", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as GreenhouseGasesType;
                }
            }
        }

        /// <summary>
        /// Method to report trucking emissions.
        /// </summary>
        /// <param name="NumberOfTrucks">Number of trucks</param>
        /// <param name="IsSales">Determines if this is a sales or purchase shipment</param>
        public void ReportEmissions(int NumberOfTrucks, bool IsSales)
        {
            if(NumberOfTrucks > 0)
            {
                List<string> gases = new List<string>() { "Methane", "CO2", "NOx" };
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
                        case "NOx":
                            gasstore = NOxStore;
                            emissions = TruckNOxEmissions;
                            break;
                        default:
                            gasstore = null;
                            break;
                    }

                    if (gasstore != null & emissions > 0)
                    {
                        gasstore.Add(NumberOfTrucks * DistanceToMarket * emissions , this.Parent.Name, "Trucking "+(IsSales?"sales":"purchases"));
                    }
                }
            }
        }
    }
}
