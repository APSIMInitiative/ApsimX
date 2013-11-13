using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Returns the age (in years) of the crop")]
    class AgeCalculatorFunction : Function
    {
        private int _Age = 0;

        [EventSubscribe("Tick")]
        private void OnTick()
        {
            _Age = _Age + 1;
        }
        
        [Units("y")]
        public override double FunctionValue
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