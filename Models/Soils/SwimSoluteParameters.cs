using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// A model for capturing swim solute parameters
    /// </summary>
    [Serializable]
    [ValidParent(ParentType=typeof(Swim3))]
    public class SwimSoluteParameters : Model
    {
        /// <summary>Gets or sets the dis.</summary>
        /// <value>The dis.</value>
        [Description("Dispersivity - dis ((cm^2/h)/(cm/h)^p)")]
        public double Dis { get; set; }
        /// <summary>Gets or sets the disp.</summary>
        /// <value>The disp.</value>
        [Description("Dispersivity Power - disp")]
        public double Disp { get; set; }
        /// <summary>Gets or sets a.</summary>
        /// <value>a.</value>
        [Description("Tortuosity Constant - a")]
        public double A { get; set; }
        /// <summary>Gets or sets the DTHC.</summary>
        /// <value>The DTHC.</value>
        [Description("Tortuoisty Offset - dthc")]
        public double DTHC { get; set; }
        /// <summary>Gets or sets the DTHP.</summary>
        /// <value>The DTHP.</value>
        [Description("Tortuoisty Power - dthp")]
        public double DTHP { get; set; }
        /// <summary>Gets or sets the water table cl.</summary>
        /// <value>The water table cl.</value>
        [Description("Water Table Cl Concentration (ppm)")]
        public double WaterTableCl { get; set; }
        /// <summary>Gets or sets the water table n o3.</summary>
        /// <value>The water table n o3.</value>
        [Description("Water Table NO3 Concentration (ppm)")]
        public double WaterTableNO3 { get; set; }
        /// <summary>Gets or sets the water table n h4.</summary>
        /// <value>The water table n h4.</value>
        [Description("Water Table NH4 Concentration (ppm)")]
        public double WaterTableNH4 { get; set; }
        /// <summary>Gets or sets the water table urea.</summary>
        /// <value>The water table urea.</value>
        [Description("Water Table Urea Concentration (ppm)")]
        public double WaterTableUrea { get; set; }
        /// <summary>Gets or sets the water table tracer.</summary>
        /// <value>The water table tracer.</value>
        [Description("Water Table Tracer (ppm)")]
        public double WaterTableTracer { get; set; }
        /// <summary>Gets or sets the water table mineralisation inhibitor.</summary>
        /// <value>The water table mineralisation inhibitor.</value>
        [Description("Water Table Mineralisation Inhibitor (ppm)")]
        public double WaterTableMineralisationInhibitor { get; set; }
        /// <summary>Gets or sets the water table urease inhibitor.</summary>
        /// <value>The water table urease inhibitor.</value>
        [Description("Water Table Urease Inhibitor (ppm)")]
        public double WaterTableUreaseInhibitor { get; set; }
        /// <summary>Gets or sets the water table nitrification inhibitor.</summary>
        /// <value>The water table nitrification inhibitor.</value>
        [Description("Water Table Nitrification Inhibitor (ppm)")]
        public double WaterTableNitrificationInhibitor { get; set; }
        /// <summary>Gets or sets the water table denitrification inhibitor.</summary>
        /// <value>The water table denitrification inhibitor.</value>
        [Description("Water Table Denitrification Inhibitor (ppm)")]
        public double WaterTableDenitrificationInhibitor { get; set; }

        /// <summary>Gets or sets the thickness.</summary>
        /// <value>The thickness.</value>
        public double[] Thickness { get; set; }
        /// <summary>Gets or sets the n o3 exco.</summary>
        /// <value>The n o3 exco.</value>
        public double[] NO3Exco { get; set; }
        /// <summary>Gets or sets the n o3 fip.</summary>
        /// <value>The n o3 fip.</value>
        public double[] NO3FIP { get; set; }
        /// <summary>Gets or sets the n h4 exco.</summary>
        /// <value>The n h4 exco.</value>
        public double[] NH4Exco { get; set; }
        /// <summary>Gets or sets the n h4 fip.</summary>
        /// <value>The n h4 fip.</value>
        public double[] NH4FIP { get; set; }
        /// <summary>Gets or sets the urea exco.</summary>
        /// <value>The urea exco.</value>
        public double[] UreaExco { get; set; }
        /// <summary>Gets or sets the urea fip.</summary>
        /// <value>The urea fip.</value>
        public double[] UreaFIP { get; set; }
        /// <summary>Gets or sets the cl exco.</summary>
        /// <value>The cl exco.</value>
        public double[] ClExco { get; set; }
        /// <summary>Gets or sets the cl fip.</summary>
        /// <value>The cl fip.</value>
        public double[] ClFIP { get; set; }
        /// <summary>Gets or sets the tracer exco.</summary>
        /// <value>The tracer exco.</value>
        public double[] TracerExco { get; set; }
        /// <summary>Gets or sets the tracer fip.</summary>
        /// <value>The tracer fip.</value>
        public double[] TracerFIP { get; set; }
        /// <summary>Gets or sets the mineralisation inhibitor exco.</summary>
        /// <value>The mineralisation inhibitor exco.</value>
        public double[] MineralisationInhibitorExco { get; set; }
        /// <summary>Gets or sets the mineralisation inhibitor fip.</summary>
        /// <value>The mineralisation inhibitor fip.</value>
        public double[] MineralisationInhibitorFIP { get; set; }
        /// <summary>Gets or sets the urease inhibitor exco.</summary>
        /// <value>The urease inhibitor exco.</value>
        public double[] UreaseInhibitorExco { get; set; }
        /// <summary>Gets or sets the urease inhibitor fip.</summary>
        /// <value>The urease inhibitor fip.</value>
        public double[] UreaseInhibitorFIP { get; set; }
        /// <summary>Gets or sets the nitrification inhibitor exco.</summary>
        /// <value>The nitrification inhibitor exco.</value>
        public double[] NitrificationInhibitorExco { get; set; }
        /// <summary>Gets or sets the nitrification inhibitor fip.</summary>
        /// <value>The nitrification inhibitor fip.</value>
        public double[] NitrificationInhibitorFIP { get; set; }
        /// <summary>Gets or sets the denitrification inhibitor exco.</summary>
        /// <value>The denitrification inhibitor exco.</value>
        public double[] DenitrificationInhibitorExco { get; set; }
        /// <summary>Gets or sets the denitrification inhibitor fip.</summary>
        /// <value>The denitrification inhibitor fip.</value>
        public double[] DenitrificationInhibitorFIP { get; set; }
    }

}
