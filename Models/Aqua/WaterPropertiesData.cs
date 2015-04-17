using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Aqua
    {

    /// <summary>
    /// Stores the information about the water but not the volume of it.
    /// </summary>
    [Serializable]
    public class WaterProperties
        {

        public double Temperature; 
        public double Salinity; 
        public double PH;
        public double N;
        public double P;
        public double TSS;


        public WaterProperties(double Temperature, double Salinity, double PH, double N, double P, double TSS)
            {
            this.Temperature = Temperature;
            this.Salinity = Salinity;
            this.PH = PH;
            this.N = N;
            this.P = P;
            this.TSS = TSS;
            }

        }



    }
