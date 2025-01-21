using Models.Core;
using System;

namespace Models.DCAPST
{
    /// <summary>
    /// Describes parameters used in leaf temperature calculations
    /// </summary>
    [Serializable]
    public class LeafTemperatureParameters
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("C")]
        public double C { get; set; }

        /// <summary>
        /// The maximum temperature
        /// </summary>
        [Description("Maximum Temperature")]
        public double TMax { get; set; }
        
        /// <summary>
        /// The minimum temperature
        /// </summary>
        [Description("Minimum Temperature")]
        public double TMin { get; set; }
        
        /// <summary>
        /// The optimum temperature
        /// </summary>
        [Description("Optimal Temperature")]
        public double TOpt { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("Beta")]
        public double Beta { get; set; }
    }
}
