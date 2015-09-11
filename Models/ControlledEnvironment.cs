// -----------------------------------------------------------------------
// <copyright file="WeatherFile.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml.Serialization;
    using Models.Core;
    using APSIM.Shared.Utilities;
    using Models.Interfaces;

    ///<summary>
    /// Reads in controlled environment weather data and makes it available to models.
    ///</summary>
    ///    
    ///<remarks>
    ///
    ///</remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(typeof(Simulation))]
    public class ControlledEnvironment : Model, IWeather
    {
        /// <summary>
        /// A link to the clock model.
        /// </summary>
        [Link]
        private Clock clock = null;
        /// <summary>
        /// Gets the start date of the weather file
        /// </summary>
        public DateTime StartDate
        {
            get
            {
                return clock.StartDate;
            }
        }

        /// <summary>
        /// Gets the end date of the weather file
        /// </summary>
        public DateTime EndDate
        {
            get
            {
                return clock.EndDate;
            }
        }

        /// <summary>
        /// An instance of the weather data.
        /// </summary>
        private Weather.NewMetType todaysMetData = new Weather.NewMetType();

        /// <summary>
        /// This event will be invoked immediately before models get their weather data.
        /// models and scripts an opportunity to change the weather data before other models
        /// reads it.
        /// </summary>
        public event EventHandler PreparingNewWeatherData;

        /// <summary>
        /// Gets the weather data as a single structure.
        /// </summary>
        public Weather.NewMetType MetData
        {
            get
            {
                return this.todaysMetData;
            }
        }

        /// <summary>
        /// Gets or sets the maximum temperature (oC)
        /// </summary>
        [Description("Maximum Air Temperature (oC)")]
        public double MaxT
        {
            get
            {
                return this.MetData.Maxt;
            }

            set
            {
                this.todaysMetData.Maxt = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum temperature (oC)
        /// </summary>
        [Description("Minimum Air Temperature (oC)")]
        public double MinT
        {
            get
            {
                return this.MetData.Mint;
            }

            set
            {
                this.todaysMetData.Mint = value;
            }
        }

        /// <summary>
        /// Gets or sets the rainfall (mm)
        /// </summary>
        [Description("Rainfall (mm)")]        
        public double Rain
        {
            get
            {
                return this.MetData.Rain;
            }

            set
            {
                this.todaysMetData.Rain = value;
            }
        }

        /// <summary>
        /// Gets or sets the solar radiation. MJ/m2/day
        /// </summary>
        [Description("Solar Radiation (MJ/m2/d)")]
        public double Radn
        {
            get
            {
                return this.MetData.Radn;
            }

            set
            {
                this.todaysMetData.Radn = value;
            }
        }
		
        /// <summary>
        /// Gets or sets the Pan Evaporation (mm) (Class A pan)
        /// </summary>
        [Description("Pan Evaporation (mm)")]
        public double PanEvap
            {
            get
                {
                return this.MetData.PanEvap;
                }

            set
                {
                this.todaysMetData.PanEvap = value;
                }
            }

        /// <summary>
        /// Gets or sets the vapor pressure (hPa)
        /// </summary>
        [Description("Vapour Pressure (hPa)")]
        public double VP
        {
            get
            {
                return this.MetData.VP;
            }

            set
            {
                this.todaysMetData.VP = value;
            }
        }

        /// <summary>
        /// Gets or sets the wind value found in weather file or zero if not specified. (code says 3.0 not zero)
        /// </summary>
        [Description("Wind Speed (m/s)")]
        public double Wind
        {
            get
            {
                return this.MetData.Wind;
            }
            set
            {
                this.todaysMetData.Wind = value;
            }
        }

        /// <summary>
        /// Gets or sets the CO2 level. If not specified in the weather file the default is 350.
        /// </summary>
        [Description("CO2 concentration of the air (ppm)")]
        public double CO2 { get; set; }

        /// <summary>
        /// Gets the latitude
        /// </summary>
        [Description("Latitude (deg)")]        
        public double Latitude{ get; set; }

        /// <summary>
        /// Gets the average temperature
        /// </summary>
        public double Tav
        {
            get
            {

                return (this.MetData.Mint + this.MetData.Maxt)/2;
            }
        }

        /// <summary>
        /// Gets the temperature amplitude.
        /// </summary>
        public double Amp
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the duration of the day in hours.
        /// </summary>
        [Description("Day Length (h)")]
        public double DayLength {get; set;}

        /// <summary>
        /// Calculate daylength using a given twilight angle
        /// </summary>
        /// <param name="twilight"></param>
        /// <returns></returns>
        public double CalculateDayLength(double twilight)
        {
            return DayLength;
        }

        /// <summary>
        /// Overrides the base class method to allow for initialization.
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// An event handler for the daily DoWeather event.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("DoWeather")]
        private void OnDoWeather(object sender, EventArgs e)
        {

            if (this.PreparingNewWeatherData != null)
            {
                this.PreparingNewWeatherData.Invoke(this, new EventArgs());
            }
        }


        /// <summary>
        /// An estimate of mean air temperature for the period number in this time step
        /// </summary>
        /// <param name="tempMax">The maximum temperature</param>
        /// <param name="tempMin">The minimum temperature</param>
        /// <param name="period">The period number 1 to 8</param>
        /// <returns>The mean air temperature</returns>
        private double Temp3Hr(double tempMax, double tempMin, double period)
        {
            double tempRangeFract = 0.92105 + (0.1140 * period) -
                                           (0.0703 * Math.Pow(period, 2)) +
                                           (0.0053 * Math.Pow(period, 3));
            double diurnalRange = tempMax - tempMin;
            double deviation = tempRangeFract * diurnalRange;
            return tempMin + deviation;
        }


    }
}