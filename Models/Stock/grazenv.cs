namespace Models.GrazPlan
{
    using System;

    /// <summary>
    /// Environment interface
    /// </summary>
    public static class GrazEnv
    {
        // Unit conversion constants    
        
        /// <summary>
        /// Convert day-of-year to radians
        /// </summary>
        public const double DAY2RAD = 2 * Math.PI / 365;    
        
        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        public const double DEG2RAD = 2 * Math.PI / 360;    
        
        /// <summary>
        /// Convert km/d to m/s
        /// </summary>
        public const double KMD_2_MS = 1.0E3 / (24 * 60 * 60); 
        
        /// <summary>
        /// Convert W/m^2 to MJ/m^2/d
        /// </summary>
        public const double WM2_2_MJM2 = 1.0E6 / (24 * 60 * 60);  
        
        /// <summary>
        /// Convert degrees C to K
        /// </summary>
        public const double C_2_K = 273.15;               
        
        /// <summary>
        /// The herbage albedo
        /// </summary>
        public const double HERBAGE_ALBEDO = 0.23;

        /// <summary>
        /// Reference [CO2] in ppm
        /// </summary>
        public const double REFERENCE_CO2 = 350.0;                                                                       
    }
}
