using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Core;

namespace Model.Components.Soils
{
    public class SoilTemperature2
    {
        [Units("hours")]
        public double MaxTTimeDefault { get; set; }

        [Description("Boundary layer conductance source")]
        [Units("(calc/constant)")]
        public string BoundaryLayerConductanceSource { get; set; }

        [Description("Boundary layer conductance")]
        [Units("(W/m2/K)")]
        public double BoundaryLayerConductance { get; set; }

        [Description("Number of iterations to calc boundary layer conductance (0-10)")]
        public int BoundaryLayerConductanceIterations { get; set; }

        [Description("Net radiation source (calc/eos)")]
        public string NetRadiationSource { get; set; }

        [Description("Default wind speed")]
        [Units("m/s")]
        public double DefaultWindSpeed { get; set; }

        [Description("Default altitude (m) 275m (700 ft) is approx 980 hPa")]
        [Units("m")]
        public double DefaultAltitude { get; set; }

        [Description("Default instrument height for wind and temperature")]
        [Units("m")]
        public double DefaultInstrumentHeight { get; set; }

        [Description("Height of bare soil")]
        [Units("mm")]
        public double BareSoilHeight { get; set; }

        [Description("Note")]
        public string Note { get; set; }
    }

}
