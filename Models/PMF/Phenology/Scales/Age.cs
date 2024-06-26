using System;
using Models.Climate;
using Models.Core;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// The number of winters a plant has experienced
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class Age : Model
    {
        /// <summary>
        /// The Weather model
        /// </summary>
        [Link]
        Weather weather = null;

        private int years = 0;

        private double daysThroughYear = 0.0;

        private double fractionComplete = 0.0;

        /// <summary>
        /// The number of winters the crop has passed
        /// </summary>
        [JsonIgnore]
        [Units("y")]
        public int Years { get { return years; } set { years = value; } }

        /// <summary>
        /// The progress through the current year
        /// </summary>
        [JsonIgnore]
        [Units("y")]
        public double FractionComplete { get { return fractionComplete; } set { fractionComplete = value; } }

        /// <summary>
        /// The progress through the life i decimal
        /// </summary>
        [JsonIgnore]
        public double YearDecimal { get { return Years + FractionComplete; } }

        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            daysThroughYear += 1;
            if (weather.DaysSinceWinterSolstice == 20)
            { 
                Years += 1;
                daysThroughYear = 0;
            }
            fractionComplete = daysThroughYear / 365;
        }
    }
}