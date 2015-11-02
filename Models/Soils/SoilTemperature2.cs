using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// A model for capturing soil temperature parameters
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    public class SoilTemperature2 : Model
    {
        /// <summary>Gets or sets the maximum t time default.</summary>
        /// <value>The maximum t time default.</value>
        [Units("hours")]
        public double MaxTTimeDefault { get; set; }

        /// <summary>Gets or sets the boundary layer conductance source.</summary>
        /// <value>The boundary layer conductance source.</value>
        [Description("Boundary layer conductance source")]
        [Units("(calc/constant)")]
        public string BoundaryLayerConductanceSource { get; set; }

        /// <summary>Gets or sets the boundary layer conductance.</summary>
        /// <value>The boundary layer conductance.</value>
        [Description("Boundary layer conductance")]
        [Units("(W/m2/K)")]
        public double BoundaryLayerConductance { get; set; }

        /// <summary>Gets or sets the boundary layer conductance iterations.</summary>
        /// <value>The boundary layer conductance iterations.</value>
        [Description("Number of iterations to calc boundary layer conductance (0-10)")]
        public int BoundaryLayerConductanceIterations { get; set; }

        /// <summary>Gets or sets the net radiation source.</summary>
        /// <value>The net radiation source.</value>
        [Description("Net radiation source (calc/eos)")]
        public string NetRadiationSource { get; set; }

        /// <summary>Gets or sets the default wind speed.</summary>
        /// <value>The default wind speed.</value>
        [Description("Default wind speed")]
        [Units("m/s")]
        public double DefaultWindSpeed { get; set; }

        /// <summary>Gets or sets the default altitude.</summary>
        /// <value>The default altitude.</value>
        [Description("Default altitude (m) 275m (700 ft) is approx 980 hPa")]
        [Units("m")]
        public double DefaultAltitude { get; set; }

        /// <summary>Gets or sets the default height of the instrument.</summary>
        /// <value>The default height of the instrument.</value>
        [Description("Default instrument height for wind and temperature")]
        [Units("m")]
        public double DefaultInstrumentHeight { get; set; }

        /// <summary>Gets or sets the height of the bare soil.</summary>
        /// <value>The height of the bare soil.</value>
        [Description("Height of bare soil")]
        [Units("mm")]
        public double BareSoilHeight { get; set; }

        /// <summary>Gets or sets the note.</summary>
        /// <value>The note.</value>
        [Description("Note")]
        public string Note { get; set; }
    }

}
