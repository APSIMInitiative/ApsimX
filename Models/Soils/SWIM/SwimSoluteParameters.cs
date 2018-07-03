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
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Swim3))]
    public class SwimSoluteParameters : Model
    {
        /// <summary>Gets or sets the dis.</summary>
        [Description("Dispersivity - dis ((cm^2/h)/(cm/h)^p)")]
        public double Dis { get; set; }

        /// <summary>Gets or sets the disp.</summary>
        [Description("Dispersivity Power - disp")]
        public double Disp { get; set; }
        
        /// <summary>Gets or sets a.</summary>
        [Description("Tortuosity Constant - a")]
        public double A { get; set; }
        
        /// <summary>Gets or sets the DTHC.</summary>
        [Description("Tortuoisty Offset - dthc")]
        public double DTHC { get; set; }
        
        /// <summary>Gets or sets the DTHP.</summary>
        [Description("Tortuoisty Power - dthp")]
        public double DTHP { get; set; }
        
        /// <summary>Gets or sets the water table cl.</summary>
        [Description("Water Table Cl Concentration (ppm)")]
        public double WaterTableCl { get; set; }
        
        /// <summary>Gets or sets the water table n o3.</summary>
        [Description("Water Table NO3 Concentration (ppm)")]
        public double WaterTableNO3 { get; set; }
        
        /// <summary>Gets or sets the water table n h4.</summary>
        [Description("Water Table NH4 Concentration (ppm)")]
        public double WaterTableNH4 { get; set; }
        
        /// <summary>Gets or sets the water table urea.</summary>
        [Description("Water Table Urea Concentration (ppm)")]
        public double WaterTableUrea { get; set; }
        
        /// <summary>Gets or sets the water table tracer.</summary>
        [Description("Water Table Tracer (ppm)")]
        public double WaterTableTracer { get; set; }
        
        /// <summary>Gets or sets the water table mineralisation inhibitor.</summary>
        [Description("Water Table Mineralisation Inhibitor (ppm)")]
        public double WaterTableMineralisationInhibitor { get; set; }
        
        /// <summary>Gets or sets the water table urease inhibitor.</summary>
        [Description("Water Table Urease Inhibitor (ppm)")]
        public double WaterTableUreaseInhibitor { get; set; }
        
        /// <summary>Gets or sets the water table nitrification inhibitor.</summary>
        [Description("Water Table Nitrification Inhibitor (ppm)")]
        public double WaterTableNitrificationInhibitor { get; set; }
        
        /// <summary>Gets or sets the water table denitrification inhibitor.</summary>
        [Description("Water Table Denitrification Inhibitor (ppm)")]
        public double WaterTableDenitrificationInhibitor { get; set; }

        /// <summary>Gets or sets the thickness.</summary>
        [Description("Depth (mm)")]
        public double[] Thickness { get; set; }

        /// <summary>Gets or sets the n o3 exco.</summary>
        [Description("NO3Exco")]
        public double[] NO3Exco { get; set; }

        /// <summary>Gets or sets the n o3 fip.</summary>
        [Description("NO3FIP")]
        public double[] NO3FIP { get; set; }

        /// <summary>Gets or sets the n h4 exco.</summary>
        [Description("NH4Exco")]
        public double[] NH4Exco { get; set; }

        /// <summary>Gets or sets the n h4 fip.</summary>
        [Description("NH4FIP")]
        public double[] NH4FIP { get; set; }

        /// <summary>Gets or sets the urea exco.</summary>
        [Description("UreaExco")]
        public double[] UreaExco { get; set; }

        /// <summary>Gets or sets the urea fip.</summary>
        [Description("UreaFIP")]
        public double[] UreaFIP { get; set; }

        /// <summary>Gets or sets the cl exco.</summary>
        [Description("ClExco")]
        public double[] ClExco { get; set; }

        /// <summary>Gets or sets the cl fip.</summary>
        [Description("ClFIP")]
        public double[] ClFIP { get; set; }

        /// <summary>Gets or sets the tracer exco.</summary>
        [Description("TracerExco")]
        public double[] TracerExco { get; set; }

        /// <summary>Gets or sets the tracer fip.</summary>
        [Description("TracerFIP")]
        public double[] TracerFIP { get; set; }

        /// <summary>Gets or sets the mineralisation inhibitor exco.</summary>
        [Description("MineralisationInhibitorExco")]
        public double[] MineralisationInhibitorExco { get; set; }

        /// <summary>Gets or sets the mineralisation inhibitor fip.</summary>
        [Description("MineralisationInhibitorFIP")]
        public double[] MineralisationInhibitorFIP { get; set; }

        /// <summary>Gets or sets the urease inhibitor exco.</summary>
        [Description("UreaseInhibitorExco")]
        public double[] UreaseInhibitorExco { get; set; }

        /// <summary>Gets or sets the urease inhibitor fip.</summary>
        [Description("UreaseInhibitorFIP")]
        public double[] UreaseInhibitorFIP { get; set; }

        /// <summary>Gets or sets the nitrification inhibitor exco.</summary>
        [Description("NitrificationInhibitorExco")]
        public double[] NitrificationInhibitorExco { get; set; }

        /// <summary>Gets or sets the nitrification inhibitor fip.</summary>
        [Description("NitrificationInhibitorFIP")]
        public double[] NitrificationInhibitorFIP { get; set; }

        /// <summary>Gets or sets the denitrification inhibitor exco.</summary>
        [Description("DenitrificationInhibitorExco")]
        public double[] DenitrificationInhibitorExco { get; set; }

        /// <summary>Gets or sets the denitrification inhibitor fip.</summary>
        [Description("DenitrificationInhibitorFIP")]
        public double[] DenitrificationInhibitorFIP { get; set; }
    }

}
