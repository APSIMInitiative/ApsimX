using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils
{
    public class IrrigationApplicationType : EventArgs
    {
        public double Amount;
        public bool will_runoff;
        public double Depth;
        public double NO3;
        public double NH4;
        public double CL;
    }
}
