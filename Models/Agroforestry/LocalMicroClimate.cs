using System;
using Models.Core;
using Models.PMF;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.Agroforestry
{
    /// <summary>
    /// # [Name]
    /// Class to calculate and communicate local microclimate in agroforestry systems
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class LocalMicroClimate : Model, IWeather
    {

        [Link]
        Weather weather = null; // parent weather.
        [Link]
        AgroforestrySystem ParentSystem = null;

        /// <summary>Gets the start date of the weather file</summary>
        public DateTime StartDate { get { return weather.StartDate; } }

        /// <summary>Gets the end date of the weather file</summary>
        public DateTime EndDate { get { return weather.EndDate; } }

        /// <summary>Gets or sets the maximum temperature (oc)</summary>
        public double MaxT { get { return weather.MaxT; } }

        /// <summary>Gets or sets the minimum temperature (oc)</summary>
        public double MinT { get { return weather.MinT; } }

        /// <summary>
        /// Daily Mean temperature (oC)
        /// </summary>
        public double MeanT { get { return (MaxT + MinT) / 2; } }

        /// <summary>
        /// Daily mean VPD (hPa)
        /// </summary>
        [Units("hPa")]
        public double VPD
        {
            get
            {
                const double SVPfrac = 0.66;
                double VPDmint = MetUtilities.svp((float)MinT) - VP;
                VPDmint = Math.Max(VPDmint, 0.0);

                double VPDmaxt = MetUtilities.svp((float)MaxT) - VP;
                VPDmaxt = Math.Max(VPDmaxt, 0.0);

                return SVPfrac * VPDmaxt + (1 - SVPfrac) * VPDmint;
            }
        }

        /// <summary>Gets or sets the rainfall (mm)</summary>
        public double Rain { get { return weather.Rain; } }

        /// <summary>Gets or sets the solar radiation. MJ/m2/day</summary>
        public double Radn { get { return weather.Radn * ParentSystem.GetRadiationReduction(Parent as Zone) ; } }

        /// <summary>Gets or sets the vapor pressure</summary>
        public double VP { get { return weather.VP; } }

        /// <summary>
        /// Gets or sets the wind value found in weather file or zero if not specified.
        /// </summary>
        public double Wind { get { return weather.Wind * ParentSystem.GetWindReduction(Parent as Zone); } }

        /// <summary>
        /// Gets or sets the CO2 level. If not specified in the weather file the default is 350.
        /// </summary>
        public double CO2 { get { return weather.CO2; } }

        /// <summary>
        /// Gets or sets the atmospheric air pressure. If not specified in the weather file the default is 1010 hPa.
        /// </summary>
        public double AirPressure { get { return weather.AirPressure; } }

        /// <summary>Gets the latitude</summary>
        public double Latitude { get { return weather.Latitude; } }

        /// <summary>Gets the average temperature</summary>
        public double Tav { get { return weather.Tav; } }

        /// <summary>Gets the temperature amplitude.</summary>
        public double Amp { get { return weather.Amp; } }

        /// <summary>Gets the duration of the day in hours.</summary>
        public double CalculateDayLength(double Twilight) { return weather.CalculateDayLength(Twilight); }

    }
}

