using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions.SupplyFunctions
{
    /// <summary>
    /// This model calculates the CO~2~ impact on RUE using the approach of [Reyenga1999].
    /// 
    /// For C3 plants,
    /// 
    ///     _F~CO2~ = (CO~2~ - CP) x (350 + 2 x CP)/(CO~2~ + 2 x CP) x (350 - CP)_
    ///     
    /// where CP, is the compensation point calculated from daily average temperature (T) as
    /// 
    ///     _CP = (163.0 - T) / (5.0 - 0.1 * T)_
    /// 
    /// For C4 plants,
    /// 
    ///     _F~CO2~ = 0.000143 * CO~2~ + 0.95_
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class RUECO2Function : Model, IFunction
    {
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
        public double Value(int arrayIndex = -1)
        {
            if (PhotosyntheticPathway == "C3")
            {

                double temp = (MetData.MaxT + MetData.MinT) / 2.0; // Average temperature


                if (temp >= 50.0)
                    throw new Exception("Average daily temperature too high for RUE CO2 Function");

                double CO2ref = 350.0; // ppm reference CO2 

                if (MetData.CO2 < 300)
                   // throw new Exception("CO2 concentration too low for RUE CO2 Function");
                    return 1.0; // FIXME: need to clearly define what happens < 350 ppm for C3

               // else if (MetData.CO2 == 350)
                else if (MetData.CO2 == CO2ref)
                    return 1.0;
                else
                {
                    double CP;      //co2 compensation point (ppm)
                    double first;
                    double second;

                    CP = (163.0 - temp) / (5.0 - 0.1 * temp);

                    //first = (MetData.CO2 - CP) * (350.0 + 2.0 * CP);
                   // second = (MetData.CO2 + 2.0 * CP) * (350.0 - CP);
                    first = (MetData.CO2 - CP) * (CO2ref + 2.0 * CP);
                    second = (MetData.CO2 + 2.0 * CP) * (CO2ref - CP);
                    return first / second;
                }
            }
            else if (PhotosyntheticPathway == "C4")
            {
                return 0.000143 * MetData.CO2 + 0.95; //Mark Howden, personal communication
            }
            else
                throw new Exception("Unknown photosynthetic pathway in RUECO2Function");
        }

        /// <summary>Document the model.</summary>
        public override IEnumerable<ITag> Document()
        {
            // Write description of this class from summary and remarks XML documentation.
            foreach (var tag in GetModelDescription())
                yield return tag;

            foreach (var tag in DocumentChildren<IModel>())
                yield return tag;
        }
    }
}