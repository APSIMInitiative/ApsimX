using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF;

namespace Models.Functions
{
    /// <summary>
    /// A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures.  
    /// </summary>
    [Serializable]
    [Description("Interoplates Daily Min and Max temperatures out to sub daily values using the Interpolation Method, applyes a temperature response function and returns a daily agrigate")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AirTemperatureFunction : Model, IFunction, ICustomDocumentation
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        [Link]
        private IPlant plant = null;

        /// <summary> Method for interpolating Max and Min temperature to sub daily values </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IInterpolationMethod InterpolationMethod = null;

        /// <summary>The temperature response function applied to each sub daily temperature and averaged to give daily mean</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IIndexedFunction TemperatureResponse = null;

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

        /// <summary>Factors used to multiply daily range to give diurnal pattern of temperatures between Tmax and Tmin</summary>
        public List<double> TempRangeFactors = null;

        /// <summary>Temperatures interpolated to sub daily values from Tmin and Tmax</summary>
        public List<double> SubDailyTemperatures = null;

        /// <summary>Temperatures interpolated to sub daily values from Tmin and Tmax</summary>
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

        /// <summary> Set the sub daily temperature range factor values at sowing</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, EventArgs e)
        {
            TempRangeFactors = InterpolationMethod.t_range_fract();
        }

        /// <summary> Set the sub dialy temperature values for the day then call temperature response function and set value for each sub daily period</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("PrePhenology")]
        private void OnPrePhenology(object sender, EventArgs e)
        {
            if (plant.IsAlive)
            {
                if (InterpolationMethod.GetType().Name == "HourlySinPpAdjusted")
                    TempRangeFactors = InterpolationMethod.t_range_fract();

                SubDailyTemperatures = new List<Double>();
                double diurnal_range = MetData.MaxT - MetData.MinT;
                foreach (double trf in TempRangeFactors)
                {
                    SubDailyTemperatures.Add(MetData.MinT + trf * diurnal_range);
                }

                SubDailyResponse = new List<double>();
                foreach (double sdt in SubDailyTemperatures)
                {
                    SubDailyResponse.Add(TemperatureResponse.ValueIndexed(sdt));
                }
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);
            }
        }

    }

    /// <summary>
    /// A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures.  
    /// </summary>
    [Serializable]
    [Description("A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures\n\n" +
        "Eight interpolations of the air temperature are calculated using a three-hour correction factor." +
        "For each air three-hour air temperature, a value is calculated.  The eight three-hour estimates" +
        "are then averaged to obtain the daily value.")]
    public class ThreeHourSin : Model, IInterpolationMethod
    {
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
    /// Junqi, write a summary here
    /// </summary>
    [Serializable]
    [Description("provide a description")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class HourlySinPpAdjusted : Model, IInterpolationMethod
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        private double Photoperiod = 0;

        /// <summary>Creates a list of temperature range factors used to estimate daily temperature from Min and Max temp</summary>
        /// <returns></returns>
        public List<double> t_range_fract()
        {
            Photoperiod = MetData.CalculateDayLength(-6);
            List<double> trfs = new List<double>();
            // pre calculate t_range_fract for speed reasons
            for (int period = 1; period <= 24; period++)
            {
                trfs.Add(1.0); // replace 1.0 with a calculation for the factor that determins where current temp is between Min (trf = 0) and Max (trf = 1) temp  
            }
            return trfs;
        }
    }



    /// <summary>An interface that defines what needs to be implemented by an organthat has a water demand.</summary>
    public interface IInterpolationMethod
    {
        /// <summary>Calculate temperature at specified periods during the day.</summary>
        List<double> t_range_fract();
    }

}