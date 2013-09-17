using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    public class SwimWaterTable : Model
    {
        [Description("Depth of Water Table (mm)")]
        public double WaterTableDepth { get; set; }
    }

}
