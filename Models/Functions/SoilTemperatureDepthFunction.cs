using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// # [Name]
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

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// </exception>
        public double Value(int arrayIndex = -1)
        {
            int Layer = LayerIndex(Depth, Soil.Thickness);

            return Soil.Temperature[Layer];
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