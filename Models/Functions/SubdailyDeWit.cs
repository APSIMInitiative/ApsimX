using System;
using System.Collections.Generic;
using Models.Climate;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions;
/// <summary>
/// Subdaily interpolation following DeWit 1978 method.
/// </summary>
[Serializable]
[Description("De Wit, Gourdriaan and van Laar 1978 method. Sunrise is MinT, 14:00 is MaxT, sinusoidal curve between.")]
[ValidParent(ParentType = typeof(SubDailyInterpolation))]
[ViewName("UserInterface.Views.PropertyView")]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
public class SubdailyDeWit : Model, IInterpolationMethod
{
    [Link]
    private IWeather _metData = null;

    /// <summary>
    /// Output value type.
    /// </summary>
    public string OutputValueType { get; set; } = "air temperature";

    /// <summary>
    /// Number of subdaily timesteps.
    /// </summary>
    [Description("Number of subdaily timesteps (24 for hourly, 8 for 3-hourly, ...).")]
    public int SubdailyTimeSteps { get; set; } = 24;

    /// <summary>
    /// Computes subdaily values. If a controlled environment is used, will supply values provided by that model.
    /// </summary>
    /// <returns>The subdaily values.</returns>
    public List<double> SubDailyValues()
    {
        if (_metData is ControlledEnvironment CE)
            return [.. CE.SubDailyTemperature];

        List<double> ret = new(SubdailyTimeSteps);
        var maxT = _metData.MaxT;
        var minT = _metData.MinT;
        var sunriseHour = _metData.CalculateSunRise();
        var sdMultiplier = 24.0 / SubdailyTimeSteps;
        for (int ts = 0; ts < SubdailyTimeSteps; ts++)
        {
            var hr = ts * sdMultiplier;
            if (hr < sunriseHour)
            {
                var yesterdayMaxT = _metData.YesterdaysMetData?.MaxT ?? maxT;
                var a1 = (yesterdayMaxT + minT) * 0.5;
                var a2 = (yesterdayMaxT - minT) * 0.5;
                ret.Add(a1 + a2 * Math.Cos(Math.PI * (hr + 10) / (10 + sunriseHour)));
            }
            else if (hr <= 14)
            {
                var a1 = (maxT + minT) * 0.5;
                var a2 = (maxT - minT) * 0.5;
                ret.Add(a1 - a2 * Math.Cos(Math.PI * (hr - sunriseHour) / (14 - sunriseHour)));
            }
            else
            {
                var tomorrowMinT = _metData.TomorrowsMetData?.MinT ?? minT;
                var a1 = (maxT + tomorrowMinT) * 0.5;
                var a2 = (maxT - tomorrowMinT) * 0.5;
                // We may want tomorrow's sunrise hour here, but the differences are going to be quite small
                // (typically in the order of seconds).
                ret.Add(a1 + a2 * Math.Cos(Math.PI * (hr - 14) / (10 + sunriseHour)));
            }
        }
        return ret;
    }
}
