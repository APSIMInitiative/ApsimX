using System;

namespace Models.Aqua
    {

    /// <summary>
    /// Stores the information about the water but not the volume of it.
    /// Applies to any given volume/amount of this water.
    /// </summary>
    [Serializable]
    public class WaterProperties
        {

        /// <summary>
        /// Temperature of the water (oC)
        /// </summary>
        public double Temperature;
 
        /// <summary>
        /// Salinity (kg/m^3)
        /// </summary>
        public double Salinity; 
        
        /// <summary>
        /// PH 
        /// </summary>
        public double PH;

        /// <summary>
        /// Nitrogen (kg/m^3)
        /// </summary>
        public double N;

        /// <summary>
        /// Phosphorus (kg/m^3)
        /// </summary>
        public double P;

        /// <summary>
        /// Total Suspended Solids (kg/m^3)
        /// </summary>
        public double TSS;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Temperature">Temperature (oC)</param>
        /// <param name="Salinity">Salinity (kg/m^3)</param>
        /// <param name="PH">PH</param>
        /// <param name="N">Nitrogen (kg/m^3)</param>
        /// <param name="P">Phosporus (kg/m^3)</param>
        /// <param name="TSS">Total Suspended Solids (kg/m^3)</param>
        public WaterProperties(double Temperature, double Salinity, double PH, double N, double P, double TSS)
            {
            this.Temperature = Temperature;
            this.Salinity = Salinity;
            this.PH = PH;
            this.N = N;
            this.P = P;
            this.TSS = TSS;
            }

        /// <summary>
        /// Zero all the water properties
        /// </summary>
        public void ZeroProperties()
            {
            this.Temperature = 0;
            this.Salinity = 0;
            this.PH = 0;  
            this.N = 0;
            this.P = 0;
            this.TSS = 0;
            }


        }



    }
