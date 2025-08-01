using System;
using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Interfaces;
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
        /// The Clock model
        /// </summary>
        [Link]
        private Clock clock = null;

        [Link]
        private Weather weather = null;

        private int years = 0;

        private double daysThroughYear = 0.0;

        private double fractionComplete = 0.0;


        /// <summary>If checked the crops age will increase on the winter solstice (suitabe for perennial crops).  
        /// If unchecked the crops birthday will be sowing date.</summary>
        [Description("Use Winter Solstice as Birthday.  Leave unchecked for annual crops when birthday will be day of sowing")]
        public bool AnniversaryOnWinterSolstice { get; set; } = false;
        
        private DateTime Anniversary { get; set; }
        
        /// <summary>
        /// The age of the crop
        /// </summary>
        [JsonIgnore]
        [Units("y")]
        public int Years { 
            get { return years; } 
            set { years = value; } }

        /// <summary>
        /// The progress through the current year
        /// </summary>
        [JsonIgnore]
        [Units("y")]
        public double FractionComplete 
        { 
            get 
            { 
                return fractionComplete; 
            } 
            set 
            { 
                fractionComplete = value;
                daysThroughYear = (int)(365 * value);
            } 
        }

        /// <summary>
        /// The progress through the life i decimal
        /// </summary>
        [JsonIgnore]
        public double YearDecimal { get { return Years + FractionComplete; } }

        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            daysThroughYear += 1;
            if (DateUtilities.DatesAreEqual(Anniversary.ToString("dd-MMM"), clock.Today))
            {
                    Years += 1;
                    daysThroughYear = 0;
            }
            fractionComplete = daysThroughYear / 365;
        }

        [EventSubscribe("Sowing")]
        private void onSowing(object sender, EventArgs e)
        {
            if (AnniversaryOnWinterSolstice)
            {
                int birthYear = clock.Today.Year;
                if (clock.Today.DayOfYear > weather.WinterSolsticeDOY)
                    birthYear -= 1;
                Anniversary = DateUtilities.GetDate(weather.WinterSolsticeDOY-1, birthYear); 
            }
            else
            {
                Anniversary = clock.Today;
            }
        }
    }
}