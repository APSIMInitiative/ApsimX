using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;

namespace Models.PMF.Functions
{
	/// <summary>
    /// The day length for a specified day and location
    /// </summary>
	/// 
	/// \pre A \ref Models.WeatherFile function has to be existed.
	/// \pre A \ref Models.Clock function has to be existed to retrieve day of year
	/// \param Twilight The interval between sunrise or sunset and the time when the true centre of the sun is below the horizon as a specified angle.
	/// \retval The day length of a specified day and location. Variable "photoperiod" will be returned if simulation environment has a variable called ClimateControl.PhotoPeriod.
    /// <remarks>
    /// The day length is calculated with \ref Utility.Math.DayLength.
	/// </remarks>
    [Serializable]
    [Description("Returns the value of todays photoperiod calculated using the specified latitude and twilight sun angle threshold.  If variable called ClimateControl.PhotoPeriod can be found this will be used instead")]
    public class PhotoperiodFunction : Function
    {
        public double Twilight = 0;

        
        public override double Value
        {
            get
            {
                object val = this.Get("ClimateControl.PhotoPeriod");
                //If simulation environment has a variable called ClimateControl.PhotoPeriod will use that other wise calculate from day and location
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