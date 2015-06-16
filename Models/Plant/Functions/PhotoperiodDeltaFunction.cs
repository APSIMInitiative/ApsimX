using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns the difference between today's and yesterday's photoperiods in hours.
    /// </summary>
    [Serializable]
    [Description("Returns the difference between today's and yesterday's photoperiods in hours.")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PhotoperiodDeltaFunction : Model, IFunction
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>The clock</summary>
        [Link]
        protected Clock Clock = null;

        /// <summary>The twilight</summary>
        [Description("Twilight")]
        public double Twilight = 0;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                double PhotoperiodToday = MathUtilities.DayLength(Clock.Today.DayOfYear, Twilight, MetData.Latitude);
                double PhotoperiodYesterday = MathUtilities.DayLength(Clock.Today.DayOfYear - 1, Twilight, MetData.Latitude);
                double PhotoperiodDelta = PhotoperiodToday - PhotoperiodYesterday;
                return PhotoperiodDelta;
            }
        }

    }
}
