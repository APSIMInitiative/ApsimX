using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Returns the age (in years) of the crop")]
    public class AgeCalculatorFunction : Function
    {
        private int _Age = 0;

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            _Age = _Age + 1;
        }
        
        [Units("y")]
        public override double Value
        {
            get
            {
                return _Age / 365.25;
            }
        }
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