using System;

namespace Models.Soils
{
    /// <summary>
    /// Data structure that holds parameters and variables specific to each pore component in the soil horizion
    /// </summary>
    [Serializable]
    public class SubHourlyData : HourlyData
    {
        /// <summary>
        /// Initialise arays on construction
        /// </summary>
        public SubHourlyData()
        {
            Irrigation = new double[10];
            Rainfall = new double[10];
            Drainage = new double[10];
            Infiltration = new double[10];
        }
    }
}
