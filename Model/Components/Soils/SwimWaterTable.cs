using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Core;

namespace Model.Components.Soils
{
    public class SwimWaterTable : Model.Core.Model
    {
        [Description("Depth of Water Table (mm)")]
        public double WaterTableDepth { get; set; }
    }

}
