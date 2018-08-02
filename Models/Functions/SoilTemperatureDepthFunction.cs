namespace Models.Functions
{
    using Models.Core;
    using Models.Soils;
    using System;

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
        [Link] private Soil soil = null;

        /// <summary>The depth</summary>
        [Units("mm")]
        [Description("Depth")]
        public double Depth { get; set; }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            int layer = Soil.LayerIndexOfDepth(Depth, soil.Thickness);
            return soil.Temperature[layer];
        }
    }
}