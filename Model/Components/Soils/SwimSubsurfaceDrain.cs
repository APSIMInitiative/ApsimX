using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Core;

namespace Model.Components.Soils
{
    public class SwimSubsurfaceDrain : Model.Core.Model
    {
        [Description("Depth of subsurface drain (mm)")]
        public double DrainDepth { get; set; }
        [Description("Distance between subsurface drains (mm)")]
        public double DrainSpacing { get; set; }
        [Description("Radius of each subsurface drain (mm)")]
        public double DrainRadius { get; set; }
        [Description("Lateral saturated soil water conductivity (mm/d)")]
        public double Klat { get; set; }
        [Description("Depth to impermeable soil (mm)")]
        public double ImpermDepth { get; set; }
    }

}
