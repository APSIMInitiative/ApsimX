using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using Newtonsoft.Json;

namespace Models.Functions
{
    /// <summary>
    /// [Name] is the [agregationMethod] of sub-daily values from a [Response.GetType()].
    /// [Document InterpolationMethod]
    /// Each of the interpolated [InterpolationMethod.OutputValueType]s are then passed into 
    /// the following Response and the [agregationMethod] taken to give daily [Name]
    /// [Document Response]
    /// </summary>

    [Serializable]
    [Description("Uses the specified InterpolationMethod to determine sub daily values then calcualtes a value for the Response at each of these time steps and returns either the sum or average depending on the AgrevationMethod selected")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class HourlyInterpolation : Model, IFunction
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary> Method for interpolating Max and Min temperature to sub daily values </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IInterpolationMethod InterpolationMethod = null;

        /// <summary>The temperature response function applied to each sub daily temperature and averaged to give daily mean</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IIndexedFunction Response = null;
        
        /// <summary>Method used to agreagate sub daily values</summary>
        [Description("Method used to agregate sub daily temperature function")]
        public AgregationMethod agregationMethod { get; set; }

        /// <summary>Method used to agreagate sub daily values</summary>
        public enum AgregationMethod
        {
            /// <summary>Return average of sub daily values</summary>
            Average,
            /// <summary>Return sum of sub daily values</summary>
            Sum
        }


        /// <summary>Temperatures interpolated to sub daily values from Tmin and Tmax</summary>
        [JsonIgnore]
        public List<double> SubDailyInput = null;

        /// <summary>Temperatures interpolated to sub daily values from Tmin and Tmax</summary>
        [JsonIgnore]
        public List<double> SubDailyResponse = null;

        /// <summary>Daily average temperature calculated from sub daily temperature interpolations</summary>
        public double Value(int arrayIndex = -1)
        {
            if (SubDailyResponse != null)
            {
                if (agregationMethod == AgregationMethod.Average)
                    return SubDailyResponse.Average();
                if (agregationMethod == AgregationMethod.Sum)
                    return SubDailyResponse.Sum();
                else
                    throw new Exception("invalid agregation method selected in " + this.Name + "temperature interpolation");
            }
            else
                return 0.0;
        }

        /// <summary> Set the sub dialy temperature values for the day then call temperature response function and set value for each sub daily period</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInitialisation(object sender, EventArgs e)
        {
		 SubDailyInput = InterpolationMethod.SubDailyValues();
            SubDailyResponse = new List<double>();
            foreach (double sdt in SubDailyInput)
            {
                SubDailyResponse.Add(Response.ValueIndexed(sdt));
            }

        }
    }

    /// <summary>
    /// Firstly 3-hourly estimates of air temperature (Ta) are interpolated 
    /// usig the method of [jones_ceres-maize:_1986] which assumes a sinusoidal temperature. 
    /// pattern between Tmax and Tmin.  
    /// </summary>
    [Serializable]
    [Description("A value is calculated at 3-hourly estimates using air temperature based on daily max and min temperatures\n\n" +
        "Eight interpolations of the air temperature are calculated using a three-hour correction factor." +
        "For each air three-hour air temperature, a value is calculated.")]
    [ValidParent(ParentType = typeof(HourlyInterpolation))]
    public class ThreeHourSin : Model, IInterpolationMethod
    {
        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>Factors used to multiply daily range to give diurnal pattern of temperatures between Tmax and Tmin</summary>
        public List<double> TempRangeFactors = null;

        /// <summary>The type of variable for sub-daily values</summary>
        [JsonIgnore]
        public string OutputValueType { get; set; } = "air temperature";

        /// <summary>
        /// Calculate temperatures at 3 hourly intervals from min and max using sin curve
        /// </summary>
        /// <returns>list of 8 temperature estimates for 3 hourly periods</returns>
        public List<double> SubDailyValues()
        {
            List<double> sdts = new List<Double>();
            double diurnal_range = MetData.MaxT - MetData.MinT;
            foreach (double trf in TempRangeFactors)
            {
                sdts.Add(MetData.MinT + trf * diurnal_range);
            }
            return sdts;
        }

        /// <summary> Set the sub daily temperature range factor values at sowing</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            TempRangeFactors = t_range_fract();
        }

        /// <summary>Fraction_of of day's range_of for this 3 hr period</summary>
        public List<double> t_range_fract()
        {
            List<int> periods = Enumerable.Range(1, 8).ToList();
            List<double> trfs = new List<double>();
            // pre calculate t_range_fract for speed reasons
            foreach (int period in periods)
            {
                trfs.Add(0.92105
                        + 0.1140 * period
                        - 0.0703 * Math.Pow(period, 2)
                        + 0.0053 * Math.Pow(period, 3));
            }
            if (trfs.Count != 8)
                throw new Exception("Incorrect number of subdaily temperature estimations in " + this.Name + " temperature interpolation");
            return trfs;
        }
    }

    /// <summary>
    /// Firstly hourly estimates of air temperature (Ta) are interpolated from Tmax, Tmin and daylength (d) 
    /// using the method of [Goudriaan1994].  
    /// During sunlight hours Ta is calculated each hour using a 
    /// sinusoidal curve fitted to Tmin and Tmax . 
    /// After sunset Ta is calculated as an exponential decline from Ta at sunset 
    /// to the Tmin at sunrise the next day.
    /// The hour (Th) of sunrise is calculated as Th = 12 − d/2 and Ta is assumed 
    /// to equal Tmin at this time.  Tmax is reached when Th equals 13.5. 
    /// </summary>
    [Serializable]
    [Description("calculating the hourly temperature based on Tmax, Tmin and daylength")]
    [ValidParent(ParentType = typeof(HourlyInterpolation))]
    public class HourlySinPpAdjusted : Model, IInterpolationMethod
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        private const double P = 1.5;

        private const double TC = 4.0;

        /// <summary>The type of variable for sub-daily values</summary>
        [JsonIgnore]
        public string OutputValueType { get; set; } = "air temperature";

        /// <summary>
        /// Temperature at the most recent sunset
        /// </summary>
        [JsonIgnore]
        public double Tsset { get; set; }

        /// <summary> Set the sub daily temperature range factor values at sowing</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {

        }

        /// <summary>Creates a list of temperature range factors used to estimate daily temperature from Min and Max temp</summary>
        /// <returns></returns>
        public List<double> SubDailyValues()
        {
            double d = MetData.CalculateDayLength(-6);
            double Tmin = MetData.MinT;
            double Tmax = MetData.MaxT;
            double TmaxB = MetData.YesterdaysMetData.MaxT;
            double TminA = MetData.TomorrowsMetData.MinT;
            double Hsrise = MetData.CalculateSunRise();
            double Hsset = MetData.CalculateSunSet();

            List<double> sdts = new List<double>();

            for (int Th = 0; Th <= 23; Th++)
            {
                double Ta = 1.0;
                if (Th < Hsrise)
                {
                    //  Hour between midnight and sunrise
                    //  PERIOD A MaxTB is max. temperature, before day considered

                    //this is the sunset temperature of based on the previous day
                    double n = 24 - d;
                    Tsset = Tmin + (TmaxB - Tmin) *
                                    Math.Sin(Math.PI * (d / (d + 2 * P)));

                    Ta = (Tmin - Tsset * Math.Exp(-n / TC) +
                            (Tsset - Tmin) * Math.Exp(-(Th + 24 - Hsset) / TC)) /
                            (1 - Math.Exp(-n / TC));
                }
                else if (Th >= Hsrise & Th < 12 + P)
                {
                    // PERIOD B Hour between sunrise and normal time of MaxT
                    Ta = Tmin + (Tmax - Tmin) *
                            Math.Sin(Math.PI * (Th - Hsrise) / (d + 2 * P));
                }
                else if (Th >= 12 + P & Th < Hsset)
                {
                    // PERIOD C Hour between normal time of MaxT and sunset
                    //  MinTA is min. temperature, after day considered

                    Ta = TminA + (Tmax - TminA) *
                        Math.Sin(Math.PI * (Th - Hsrise) / (d + 2 * P));
                }
                else
                {
                    // PERIOD D Hour between sunset and midnight
                    Tsset = TminA + (Tmax - TminA) * Math.Sin(Math.PI * (d / (d + 2 * P)));
                    double n = 24 - d;
                    Ta = (TminA - Tsset * Math.Exp(-n / TC) +
                            (Tsset - TminA) * Math.Exp(-(Th - Hsset) / TC)) /
                            (1 - Math.Exp(-n / TC));
                }
                sdts.Add(Ta);
            }
            return sdts;
        }

        

    }

    /// <summary>
    /// Firstly hourly estimates of solar radiation are interpolated from solar daily radiation
    /// </summary>
    [Serializable]
    [Description("Calculates the ground solar incident radiation per hour")]
    [ValidParent(ParentType = typeof(HourlyInterpolation))]
    public class HourlyRadiation : Model, IInterpolationMethod
    {
        /// <summary>
        /// Link to the weather object
        /// </summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>
        /// Link to the clock object
        /// </summary>
        [Link]
        protected Clock clock = null;

        /// <summary>The type of variable for sub-daily values</summary>
        [JsonIgnore]
        public string OutputValueType { get; set; } = "solar radiation";

        // Calculates the ground solar incident radiation per hour and scales it to the actual radiation
        // Developed by Greg McLean and adapted/modified by Behnam (Ben) Ababaei.

        /// <summary>
        /// Hourly radiation estimation ported from https://github.com/BrianCollinss/ApsimX/blob/12a89f9981e2636f13251b0faa30200a98b713ce/Models/Functions/SupplyFunctions/MaximumHourlyTrModel.cs#L260
        /// by Hamish
        /// Note, this has not been tested and probably has errors as it is not a dirrect port
        /// </summary>
        /// <returns></returns>
        public List<double> SubDailyValues()
        {
            List<double> hourlyRad = new List<double>();

            double latR = Math.PI / 180.0 * MetData.Latitude;       // convert latitude (degrees) to radians
            double GlobalRadiation = MetData.Radn * 1e6;     // solar radiation
            double PI = Math.PI;
            double RAD = PI / 180.0;

            //Declination of the sun as function of Daynumber (vDay)
            double Dec = -Math.Asin(Math.Sin(23.45 * RAD) * Math.Cos(2.0 * PI * ((double)clock.Today.DayOfYear + 10.0) / 365.0));

            //vSin, vCos and vRsc are intermediate variables
            double Sin = Math.Sin(latR) * Math.Sin(Dec);
            double Cos = Math.Cos(latR) * Math.Cos(Dec);
            double Rsc = Sin / Cos;

            //Astronomical daylength (hr)
            double DayL = MetData.CalculateDayLength(-6);
            double DailySinE = 3600.0 * (DayL * (Sin + 0.4 * (Sin * Sin + Cos * Cos * 0.5))
                     + 12.0 * Cos * (2.0 + 3.0 * 0.4 * Sin) * Math.Sqrt(1.0 - Rsc * Rsc) / PI);

            double riseHour = MetData.CalculateSunRise();
            double setHour = MetData.CalculateSunSet();

            for (int t = 0; t <= 23; t++)
            {
                double Hour1 = Math.Min(setHour, Math.Max(riseHour, t));
                double SinHeight1 = Math.Max(0.0, Sin + Cos * Math.Cos(2.0 * PI * (Hour1 - 12.0) / 24.0));
                double Hour2 = Math.Min(setHour, Math.Max(riseHour, t + 1));
                double SinHeight2 = Math.Max(0.0, Sin + Cos * Math.Cos(2.0 * PI * (Hour2 - 12.0) / 24.0));
                double SinHeight = 0.5 * (SinHeight1 + SinHeight2);
                hourlyRad.Add(Math.Max(0, GlobalRadiation * SinHeight * (1.0 + 0.4 * SinHeight) / DailySinE));
                hourlyRad[t] *= HourlyWeight(t + 1, riseHour, setHour);
            }

            return hourlyRad;
        }

        private double HourlyWeight(int t, double riseHour, double setHour)
        {
            double weight = new double();
            if (t < riseHour) weight = 0;
            else if (t > riseHour && t - 1 < riseHour) weight = t - riseHour;
            else if (t >= riseHour && t < setHour) weight = 1;
            else if (t > setHour && t - 1 < setHour) weight = 1 - (t - setHour);
            else if (t > setHour) weight = 0;
            return weight;
        }

    }
    /// <summary>An interface that defines what needs to be implemented by an organthat has a water demand.</summary>
    public interface IInterpolationMethod
    {
        /// <summary>Calculate temperature at specified periods during the day.</summary>
        List<double> SubDailyValues();
        /// <summary>The type of variable for sub-daily values</summary>
        string OutputValueType { get; set; }
    }

}
