using System;
using System.Collections.Generic;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>
    /// A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures.  
    /// </summary>
    [Serializable]
    [Description("A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures\n\n" +
        "Eight interpolations of the air temperature are calculated using a three-hour correction factor." +
        "For each air three-hour air temperature, a value is calculated.  The eight three-hour estimates" +
        "are then averaged to obtain the daily value.")]
    public class AirTemperatureFunction : Model, IFunction, ICustomDocumentation
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>Gets or sets the xy pairs.</summary>
        /// <value>The xy pairs.</value>
        [Link(Type = LinkType.Child, ByName = true)]
        private XYPairs XYPairs = null;   // Temperature effect on Growth Interpolation Set

        /// <summary>Temp_3hrs the specified tmax.</summary>
        public double[] temp_3hr = null;

        /// <summary>Number of 3 hourly temperatures</summary>
        private const int num3hr = 24 / 3;           // 

        /// <summary>Fraction_of of day's range_of for this 3 hr period</summary>
        private double[] t_range_fract = null;

        /// <summary>Initializes a new instance of the <see cref="AirTemperatureFunction"/> class.</summary>
        public AirTemperatureFunction()
        {
            t_range_fract = new double[num3hr];

            // pre calculate t_range_fract for speed reasons
            for (int period = 1; period <= num3hr; period++)
            {
                double period_no = period;
                t_range_fract[period-1] = 0.92105
                                    + 0.1140 * period_no
                                    - 0.0703 * Math.Pow(period_no, 2)
                                    + 0.0053 * Math.Pow(period_no, 3);
            }
        }


        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            // --------------------------------------------------------------------------
            // For each air three-hour air temperature, a value
            // is calculated.  The eight three-hour estimates
            // are then averaged to obtain the daily value.
            // --------------------------------------------------------------------------
            double tot = 0;
            
            foreach(double t in temp_3hr)
            {
                tot += XYPairs.ValueIndexed(t);
            }

            return tot / (double)num3hr;

        }

        /// <summary>
        /// Set the 3 hourly temperature values for the day
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("PreparingNewWeatherData")]
        private void OnNewMet(object sender, EventArgs e)
        {
            // --------------------------------------------------------------------------
            // Eight interpolations of the air temperature are
            // calculated using a three-hour correction factor.
            // --------------------------------------------------------------------------

            // 3 hourly temperature interpolations
            temp_3hr = new double[num3hr];
            // diurnal temperature range for the day (oC)
            double diurnal_range = MetData.MaxT - MetData.MinT;

            for (int period = 1; period <= num3hr; period++)
            {
                if (period < 1)
                    throw new Exception("3 hr. period number is below 1");
                else if (period > 8)
                    throw new Exception("3 hr. period number is above 8");

                // get mean temperature for 3 hr period (oC)
                // deviation from day's minimum for this 3 hr period
                double t_deviation = t_range_fract[period - 1] * diurnal_range;

                temp_3hr[period-1] = MetData.MinT + t_deviation;
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

                // add graph and table.
                if (XYPairs != null)
                {
                    tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + "</i> is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures.", indent));
                    tags.Add(new AutoDocumentation.GraphAndTable(XYPairs, string.Empty, "MeanAirTemperature", Name, indent));
                }
            }
        }

    }

}