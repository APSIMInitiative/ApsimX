using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// Data structure that holds parameters aspecific to each layer in the soil profile
    /// </summary>
    [Serializable]
    public class ProfileParameters : Model
    {
        /// <summary>
        /// Hourly Ksat values
        /// </summary>
        public double[] Ksat { get; set; }
        /// <summary>
        /// The amount of water mm stored in a layer at saturation
        /// </summary>
        public double[] SaturatedWaterDepth { get; set; }
        /// <summary>
        /// Constructor
        /// </summary>
        public ProfileParameters(){}
        /// <summary>
        /// Constructor
        /// </summary>
        public ProfileParameters(int Layers)
        {
            Ksat = new double[Layers];
            SaturatedWaterDepth = new double[Layers];
        }
    }
}
