using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    [Serializable]
    public class SwimSubsurfaceDrain : Model
    {
        [Description("Depth of subsurface drain (mm)")]
        [Bounds(Lower = 1.0, Upper = 1.0e6)]
        [Units("mm")]
        public double DrainDepth { get; set; }

        [Description("Distance between subsurface drains (mm)")]
        [Bounds(Lower = 1.0, Upper = 1.0e5)]
        [Units("mm")]
        public double DrainSpacing { get; set; }

        [Description("Radius of each subsurface drain (mm)")]
        [Bounds(Lower = 1.0, Upper = 1000.0)]
        [Units("mm")]
        public double DrainRadius { get; set; }

        [Description("Lateral saturated soil water conductivity (mm/d)")]
        [Bounds(Lower = 1.0, Upper = 10000.0)]
        [Units("mm/d")]
        public double Klat { get; set; }

        [Description("Depth to impermeable soil (mm)")]
        [Bounds(Lower = 1.0, Upper = 1.0e6)]
        [Units("mm")]
        public double ImpermDepth { get; set; }
    }
}
