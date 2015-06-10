using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Return soil temperature (oC) from a specified soil profile layer.
    /// The source of soil temperature array can be either SoilN ("st" property) or SoilTemp ("ave_soil_temp" property)
    /// </summary>
    [Serializable]
    [Description("Return soil temperature (oC) from a specified soil profile layer.  The source of soil temperature array can be either SoilN (st) or SoilTemp (ave_soil_temp) property")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilTemperatureDepthFunction : Model, IFunction
    {

        /// <summary>The soil</summary>
        [Link]
        Soils.Soil Soil = null;


        /// <summary>The depth</summary>
        [Units("mm")]
        [Description("Depth")]
        public double Depth { get; set; }


        /// <summary>The soil temporary source</summary>
        string soilTempSource = "Unknown"; //Flag for the name of soil temperature array


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// </exception>
        [Units("oC")]
        public double Value
        {
            get
            {
                int Layer = LayerIndex(Depth, Soil.Thickness);

                if (soilTempSource == "Unknown")
                {
                    if (Soil.SoilNitrogen.ave_soil_temp != null)
                    {
                        soilTempSource = "ave_soil_temp";
                    }
                    else if (Soil.SoilNitrogen.st != null)
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
                        return Soil.SoilNitrogen.ave_soil_temp[Layer];
                    case "st":
                        return Soil.SoilNitrogen.st[Layer];
                    default:
                        throw new Exception(Name + ": Unknown soil temperature source: " + soilTempSource);
                }

            }
        }
        /// <summary>Returns the soil layer index for a specified soil depth (mm)</summary>
        /// <param name="depth">Soil depth (mm)</param>
        /// <param name="dlayer">Array of soil layer depths in the profile (mm)</param>
        /// <returns>soil layer index</returns>
        /// <exception cref="System.Exception"></exception>
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