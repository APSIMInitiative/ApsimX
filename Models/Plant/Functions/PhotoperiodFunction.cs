using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Returns the value of todays photoperiod calculated using the specified latitude and twilight sun angle threshold.  If variable called ClimateControl.PhotoPeriod can be found this will be used instead")]
    public class PhotoperiodFunction : Function
    {
        public double Twilight = 0;

        
        public override double FunctionValue
        {
            get
            {
                object val = this.Get("ClimateControl.PhotoPeriod");
                //If simulatation environment has a variable called ClimateControl.PhotoPeriod will use that other wise calculate from day and location
                if (val != null)  //FIXME.  If climatecontrol does not contain a variable called photoperiod it still returns a value of zero.
                {
                    double CCPP = Convert.ToDouble(val);
                    if (CCPP > 0.0)
                        return Convert.ToDouble(val);
                    else
                        return Utility.Math.DayLength(Clock.Today.DayOfYear, Twilight, MetData.Latitude);
                }
                else
                    return Utility.Math.DayLength(Clock.Today.DayOfYear, Twilight, MetData.Latitude);
            }
        }

    }
}