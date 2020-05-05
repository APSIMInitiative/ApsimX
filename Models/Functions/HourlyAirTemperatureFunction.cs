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
        "Eight interpolations of the air temperature are calculated uMath.sing a three-hour correction factor." +
        "For each air three-hour air temperature, a value is calculated.  The eight three-hour estimates" +
        "are then averaged to obtain the daily value.")]
    public class HourlyAirTemperatureFunction : Model, IFunction, ICustomDocumentation
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>Gets or sets the xy pairs.</summary>
        /// <value>The xy pairs.</value>
        [Link(Type = LinkType.Child, ByName = true)]
        private IIndexedFunction TemperatureResponse= null;   // Temperature effect on Growth Interpolation Set

        /// <summary>Temp_1hr.</summary>
        public double[] temp_1hr = null;

        /// <summary>Number of hourly temperatures</summary>
        private const int num1hr = 24;           // 

        /// <summary> hourly temp </summary>
        private double temp;
        /// <summary> maximum temperature of the previous day </summary>
        private double MaxTB;
        /// <summary> minimum temperature of the next day </summary>
        private double MinTA; 
        /// <summary> maximum temperature of the current day </summary>
        private double MaxT;
        /// <summary> minimum temperature of the current day </summary>
        private double MinT;  

        /// <summary>The daylight length.</summary>
        [Units("hours")]
        public double DayLength { get; set; }
        /// <summary>Initializes a new instance of the hourly temperature function</summary>

        /// <summary>calculating the hourly temperature based on Tmax, Tmin and daylength</summary>
        public double calcHourlyTemp(double MaxTB, double MinT, double MaxT, 
                                            double MinTA, double DayLength, double HOUR)
       //(MaxTB, MinT, MaxT, MinTA, DayLength, HOUR, P=1.5)
        {
            double TC=4.0; //nocturnal time coefficient for the exponential decrease is approximately 4 hours
            double P = 1.5; //time delay between solar noon and maximum temperature

            double SUNRIS = 12 - 0.5 * DayLength;
            double SUNSET = 12 + 0.5 * DayLength;
            double NIGHTL = 24 - DayLength;
            //TSUNST the temperature at sunset
            double TSUNST = MinT + (MaxTB - MinT) * 
                            Math.Sin(Math.PI * (DayLength / (DayLength + 2 * P)));
              
            if(HOUR <= SUNRIS) 
             {
                //  Hour between midnight and sunrise
                //  PERIOD A MaxTB is max. temperature, before day considered
                temp = (MinT - TSUNST * Math.Exp(-NIGHTL / TC) +
                        (TSUNST - MinT) * Math.Exp(-(HOUR + 24 - SUNSET) / TC)) /
                        (1 - Math.Exp(-NIGHTL / TC));
             }

            if(HOUR > SUNRIS & HOUR <= 12 + P)
            {
                // PERIOD B Hour between sunrise and normal time of MaxT
                temp = MinT + (MaxT - MinT) * 
                        Math.Sin(Math.PI * (HOUR - SUNRIS) / (DayLength + 2 * P));
            }
            if(HOUR >12 + P & HOUR <= SUNSET)
            {
                // PERIOD C Hour between normal time of MaxT and sunset
                //  MinTA is min. temperature, after day considered

                temp = MinTA + (MaxT - MinTA) * 
                    Math.Sin(Math.PI * (HOUR - SUNRIS) / (DayLength + 2 * P));
            }
  
            if(HOUR > SUNSET & HOUR< 24)  
            {
                // PERIOD D Hour between sunset and midnight
                TSUNST = MinTA + (MaxT - MinTA) * Math.Sin(Math.PI * (DayLength / (DayLength + 2 * P)));

                temp = (MinTA - TSUNST * Math.Exp(-NIGHTL / TC) +
                        (TSUNST - MinTA) * Math.Exp(-(HOUR - SUNSET) / TC)) /
                        (1 - Math.Exp(-NIGHTL / TC));
            }


            return temp;

            }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            // --------------------------------------------------------------------------
            // For each hourly temperature, a value
            // is calculated. 
            // -------------------------------------------------------------------------
            double tot = 0;
            foreach (double t in temp_1hr)
            {
                tot += (double)TemperatureResponse?.ValueIndexed(t);
            }
            return tot;
        }

        /// <summary>
        /// Set the hourly temperature values for the day
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("PreparingNewWeatherData")]
        private void OnNewMet(object sender, EventArgs e)
        {
            // --------------------------------------------------------------------------
            // 24 interpolations of the air temperature are
            // calculated sin after the sunrise and exponential decrease after sunset
            // --------------------------------------------------------------------------
            // hourly temperature interpolations
            temp_1hr = new double[num1hr];
            
            if (MetData != null)
                DayLength = MetData.CalculateDayLength(-6);
            else
                DayLength = 0;
            MaxTB = MetData.MaxT; 
            MinTA = MetData.MinT; 
            MaxT = MetData.MaxT;
            MinT = MetData.MinT;
            
            for (int period = 1; period <= num1hr; period++)
            {
                if (period < 1)
                    throw new Exception("1 hr. period number is below 1");
                else if (period > 24)
                    throw new Exception("1 hr. period number is above 24");

                // get mean temperature for 1 hr period (oC)            
                int HOUR = period - 1;
                temp_1hr[HOUR] = calcHourlyTemp(MaxTB, MinT, MaxT, MinTA, DayLength, HOUR);
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
                if (TemperatureResponse != null)
                {
                    tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + "</i> is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures.", indent));
                    //tags.Add(new AutoDocumentation.GraphAndTable(TemperatureResponse, string.Empty, "MeanAirTemperature", Name, indent));
                }
            }
        }

    }

}