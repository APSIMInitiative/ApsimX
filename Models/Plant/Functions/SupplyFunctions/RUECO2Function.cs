using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Plant.Functions.SupplyFunctions
{
    [Description("This model calculates CO2 Impact on RUE using the approach of <br>Reyenga, Howden, Meinke, Mckeon (1999) <br>Modelling global change impact on wheat cropping in south-east Queensland, Australia. <br>Enivironmental Modelling & Software 14:297-306")]
    public class RUECO2Function : Function
    {
        public String PhotosyntheticPathway = "";

        //[Input]
        //public NewMetType MetData;

        double CO2 = 350;  // If CO2 is not supplied we default to 350 ppm

        
        public override double Value
        {
            get
            {

                if (PhotosyntheticPathway == "C3")
                {

                    double temp = (MetData.MaxT + MetData.MinT) / 2.0; // Average temperature


                    if (temp >= 50.0)
                        throw new Exception("Average daily temperature too high for RUE CO2 Function");

                    if (CO2 < 350)
                        throw new Exception("CO2 concentration too low for RUE CO2 Function");
                    else if (CO2 == 350)
                        return 1.0;
                    else
                    {
                        double CP;      //co2 compensation point (ppm)
                        double first;
                        double second;

                        CP = (163.0 - temp) / (5.0 - 0.1 * temp);

                        first = (CO2 - CP) * (350.0 + 2.0 * CP);
                        second = (CO2 + 2.0 * CP) * (350.0 - CP);
                        return first / second;
                    }
                }
                else if (PhotosyntheticPathway == "C4")
                {
                    return 0.000143 * CO2 + 0.95; //Mark Howden, personal communication
                }
                else
                    throw new Exception("Unknown photosynthetic pathway in RUECO2Function");
            }

        }
    }
}