using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    [Serializable]
    public class SwimSoluteParameters : Model
    {
        [Description("Dispersivity - dis ((cm^2/h)/(cm/h)^p)")]
        public double Dis { get; set; }
        [Description("Dispersivity Power - disp")]
        public double Disp { get; set; }
        [Description("Tortuosity Constant - a")]
        public double A { get; set; }
        [Description("Tortuoisty Offset - dthc")]
        public double DTHC { get; set; }
        [Description("Tortuoisty Power - dthp")]
        public double DTHP { get; set; }
        [Description("Water Table Cl Concentration (ppm)")]
        public double WaterTableCl { get; set; }
        [Description("Water Table NO3 Concentration (ppm)")]
        public double WaterTableNO3 { get; set; }
        [Description("Water Table NH4 Concentration (ppm)")]
        public double WaterTableNH4 { get; set; }
        [Description("Water Table Urea Concentration (ppm)")]
        public double WaterTableUrea { get; set; }
        [Description("Water Table Tracer (ppm)")]
        public double WaterTableTracer { get; set; }
        [Description("Water Table Mineralisation Inhibitor (ppm)")]
        public double WaterTableMineralisationInhibitor { get; set; }
        [Description("Water Table Urease Inhibitor (ppm)")]
        public double WaterTableUreaseInhibitor { get; set; }
        [Description("Water Table Nitrification Inhibitor (ppm)")]
        public double WaterTableNitrificationInhibitor { get; set; }
        [Description("Water Table Denitrification Inhibitor (ppm)")]
        public double WaterTableDenitrificationInhibitor { get; set; }

        public double[] Thickness { get; set; }
        public double[] NO3Exco { get; set; }
        public double[] NO3FIP { get; set; }
        public double[] NH4Exco { get; set; }
        public double[] NH4FIP { get; set; }
        public double[] UreaExco { get; set; }
        public double[] UreaFIP { get; set; }
        public double[] ClExco { get; set; }
        public double[] ClFIP { get; set; }
        public double[] TracerExco { get; set; }
        public double[] TracerFIP { get; set; }
        public double[] MineralisationInhibitorExco { get; set; }
        public double[] MineralisationInhibitorFIP { get; set; }
        public double[] UreaseInhibitorExco { get; set; }
        public double[] UreaseInhibitorFIP { get; set; }
        public double[] NitrificationInhibitorExco { get; set; }
        public double[] NitrificationInhibitorFIP { get; set; }
        public double[] DenitrificationInhibitorExco { get; set; }
        public double[] DenitrificationInhibitorFIP { get; set; }
    }

}
