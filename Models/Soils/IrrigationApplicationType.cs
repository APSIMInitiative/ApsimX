using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils
{
    /// <summary>
    /// Irrigation application type
    /// </summary>
    public class IrrigationApplicationType : EventArgs
    {
        /// <summary>The amount</summary>
        public double Amount;
        /// <summary>The will_runoff</summary>
        public bool will_runoff;
        /// <summary>The depth</summary>
        public double Depth;
        /// <summary>The n o3</summary>
        public double NO3;
        /// <summary>The n h4</summary>
        public double NH4;
        /// <summary>The cl</summary>
        public double CL;
    }
}
