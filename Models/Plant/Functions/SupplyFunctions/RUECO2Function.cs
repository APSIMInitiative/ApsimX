// ----------------------------------------------------------------------
// <copyright file="RUECO2Function.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions.SupplyFunctions
{
    using Models.Core;
    using Models.Interfaces;
    using System;

    /// <summary>
    /// # [Name]
    /// This model calculates CO2 Impact on RUE using the approach of [Reyenga1999].
    /// </summary>
    [Serializable]
    [Description("This model calculates CO2 Impact on RUE using the approach of <br>Reyenga, Howden, Meinke, Mckeon (1999) <br>Modelling global change impact on wheat cropping in south-east Queensland, Australia. <br>Enivironmental Modelling & Software 14:297-306")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class RUECO2Function : BaseFunction
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];

        /// <summary>The photosynthetic pathway</summary>
        [Description("PhotosyntheticPathway")]
        public String PhotosyntheticPathway { get; set; }

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// Average daily temperature too high for RUE CO2 Function
        /// or
        /// CO2 concentration too low for RUE CO2 Function
        /// or
        /// Unknown photosynthetic pathway in RUECO2Function
        /// </exception>
        public override double[] Values()
        {
            if (PhotosyntheticPathway == "C3")
            {

                double temp = (MetData.MaxT + MetData.MinT) / 2.0; // Average temperature


                if (temp >= 50.0)
                    throw new Exception("Average daily temperature too high for RUE CO2 Function");

                if (MetData.CO2 < 350)
                    throw new Exception("CO2 concentration too low for RUE CO2 Function");
                else if (MetData.CO2 == 350)
                    returnValue[0] = 1.0;
                else
                {
                    double CP;      //co2 compensation point (ppm)
                    double first;
                    double second;

                    CP = (163.0 - temp) / (5.0 - 0.1 * temp);

                    first = (MetData.CO2 - CP) * (350.0 + 2.0 * CP);
                    second = (MetData.CO2 + 2.0 * CP) * (350.0 - CP);
                    returnValue[0] = first / second;
                }
            }
            else if (PhotosyntheticPathway == "C4")
            {
                returnValue[0] = 0.000143 * MetData.CO2 + 0.95; //Mark Howden, personal communication
            }
            else
                throw new Exception("Unknown photosynthetic pathway in RUECO2Function");

            return returnValue;
        }
    }
}