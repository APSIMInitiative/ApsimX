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

        /// <summary>
        /// The number of winters the crop has passed
        /// </summary>
        [JsonIgnore]
        [Units("y")]
        public int Years { get { return years; } set { years = value; } }

        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            if (weather.DaysSinceWinterSolstice == 20)
                Years += 1;
        }
    }
}