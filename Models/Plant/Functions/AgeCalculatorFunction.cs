using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// An age calculator function
    /// </summary>
    [Serializable]
    [Description("Returns the age (in years) of the crop")]
    public class AgeCalculatorFunction : Model, Function
    {
        /// <summary>The _ age</summary>
        private int _Age = 0;

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            _Age = _Age + 1;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        [Units("y")]
        public double Value
        {
            get
            {
                return _Age / 365.25;
            }
        }
        /// <summary>Gets the age.</summary>
        /// <value>The age.</value>
        [Units("y")]
        public double Age
        {
            get
            {
                return _Age / 365.25;
            }
        }

    }
}