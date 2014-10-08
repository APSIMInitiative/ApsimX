using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// SWIM water table
    /// </summary>
    [Serializable]
    public class SwimWaterTable : Model
    {
        /// <summary>Gets or sets the water table depth.</summary>
        /// <value>The water table depth.</value>
        [Description("Depth of Water Table (mm)")]
        public double WaterTableDepth { get; set; }
    }

}
