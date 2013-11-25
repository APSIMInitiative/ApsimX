using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Purpose: Return soil temperature (oC) from a specified soil profile layer.
    /// The source of soil temperature array can be either SoilN ("st" property) or SoilTemp ("ave_soil_temp" property)
    /// </summary>
    [Description("Return soil temperature (oC) from a specified soil profile layer.  The source of soil temperature array can be either SoilN (st) or SoilTemp (ave_soil_temp) property")]
    public class SoilTemperatureDepthFunction : Function
    {

        [Link]
        Soils.SoilNitrogen SoilN = null;
        [Link]
        Soils.SoilWater SoilWat = null;

        [Units("mm")]
        public double Depth = 0;
               

        string soilTempSource = "Unknown"; //Flag for the name of soil temperature array

        
        [Units("oC")]
        public override double Value
        {
            get
            {
                int Layer = LayerIndex(Depth, SoilWat.dlayer);

                if (soilTempSource == "Unknown")
                {
                    if (SoilN.ave_soil_temp != null)
                    {
                        soilTempSource = "ave_soil_temp";
                    }
                    else if (SoilN.st != null)
                    {
                        soilTempSource = "st";
                    }
                    else
                    {
                        throw new Exception(Name + ": Soil temperature was not found ");

                    }
                }

                switch (soilTempSource)
                {
                    case "ave_soil_temp":
                        return SoilN.ave_soil_temp[Layer];
                    case "st":
                        return SoilN.st[Layer];
                    default:
                        throw new Exception(Name + ": Unknown soil temperature source: " + soilTempSource);
                }

            }
        }
        /// <summary>
        /// Returns the soil layer index for a specified soil depth (mm)
        /// </summary>
        /// <param name="depth">Soil depth (mm)</param>
        /// <param name="dlayer">Array of soil layer depths in the profile (mm)</param>
        /// <returns>soil layer index</returns>
        private int LayerIndex(double depth, double[] dlayer)
        {
            double CumDepth = 0.0;
            for (int i = 0; i < dlayer.Length; i++)
            {
                CumDepth = CumDepth + dlayer[i];
                if (CumDepth >= depth) { return i; }
            }
            throw new Exception(Name + ": Specified soil depth of " + Depth.ToString() + " mm is greater than profile depth of " + CumDepth.ToString() + " mm");
        }



    }
}