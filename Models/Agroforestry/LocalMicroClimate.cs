using System;
using Models.Core;
using Models.PMF;
using Models.Interfaces;
namespace Models.Agroforestry
{
    /// <summary>
    /// Class to calculate and communicate local microclimate in agroforestry systems
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class LocalMicroClimate : Model, IWeather
    {

        [Link]
        Weather weather = null; // parent weather.
        [Link]
        StaticForestrySystem ParentSystem = null;
        [Link]
        Clock clock = null;

        /// <summary>Gets the start date of the weather file</summary>
        public DateTime StartDate { get { return weather.StartDate; } }

        /// <summary>Gets the end date of the weather file</summary>
        public DateTime EndDate { get { return weather.EndDate; } }

        /// <summary>Gets or sets the maximum temperature (oc)</summary>
        public double MaxT { get { return weather.MaxT; } }

        /// <summary>Gets or sets the minimum temperature (oc)</summary>
        public double MinT { get { return weather.MinT; } }

        /// <summary>Gets or sets the rainfall (mm)</summary>
        public double Rain { get { return weather.Rain; } }

        /// <summary>Gets or sets the solar radiation. MJ/m2/day</summary>
        public double Radn { get { return weather.Radn * (1-ParentSystem.GetShade(Parent as Zone) / 100); ; } }

        /// <summary>Gets or sets the vapor pressure</summary>
        public double VP { get { return weather.VP; } }

        /// <summary>
        /// Gets or sets the wind value found in weather file or zero if not specified.
        /// </summary>
        public double Wind { get { return weather.Wind * ParentSystem.GetWindReduction(Parent as Zone, clock.Today); } }

        /// <summary>
        /// Gets or sets the CO2 level. If not specified in the weather file the default is 350.
        /// </summary>
        public double CO2 { get { return weather.CO2; } }

        /// <summary>Gets the latitude</summary>
        public double Latitude { get { return weather.Latitude; } }

        /// <summary>Gets the average temperature</summary>
        public double Tav { get { return weather.Tav; } }

        /// <summary>Gets the temperature amplitude.</summary>
        public double Amp { get { return weather.Amp; } }

        /// <summary>Gets the duration of the day in hours.</summary>
        public double DayLength { get { return weather.DayLength; } }

    }
}

