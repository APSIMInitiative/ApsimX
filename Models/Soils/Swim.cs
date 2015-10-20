using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using Models.Core;
using Models;
using System.Xml.Serialization;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.Soils
{

    //------ NewProfile ------
    /// <summary>
    /// strucuture for data associated with the NewProfile event
    /// </summary>
    public class NewProfileType
    {
        /// <summary>
        /// Array of layer depths
        /// </summary>
        public Single[] dlayer;
        /// <summary>
        /// Array of air-dry values
        /// </summary>
        public Single[] air_dry_dep;
        /// <summary>
        /// Array of -15 bar values
        /// </summary>
        public Single[] ll15_dep;
        /// <summary>
        /// Array of drained upper limit values
        /// </summary>
        public Single[] dul_dep;
        /// <summary>
        /// Array of saturated values
        /// </summary>
        public Single[] sat_dep;
        /// <summary>
        /// Array of soil water values
        /// </summary>
        public Single[] sw_dep;
        /// <summary>
        /// Array of bulk density values
        /// </summary>
        public Single[] bd;
    }

    /// <summary>
    /// Delegate for issuing a NewProfile event
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void NewProfileDelegate(NewProfileType Data);


    //------ CohortWaterSupplyRootSystemLayer ------
    /// <summary>
    /// strucuture for data associated with one root layer in CohortWaterSupply event
    /// </summary>
    public class CohortWaterSupplyRootSystemLayerType
    {
        /// <summary>
        /// Bottom
        /// </summary>
        public Double Bottom;
        /// <summary>
        /// Supply
        /// </summary>
        public Double Supply;
    }

    // public delegate void CohortWaterSupplyRootSystemLayerDelegate(CohortWaterSupplyRootSystemLayerType Data);

    //------ CohortWaterSupply ------
    /// <summary>
    /// Strucuture for data associated with the CohortWaterSupply event
    /// </summary>
    public class CohortWaterSupplyType
    {
        /// <summary>
        /// ID of the cohort
        /// </summary>
        public String CohortID = "";
        /// <summary>
        /// Array of values for each root layer
        /// </summary>
        public CohortWaterSupplyRootSystemLayerType[] RootSystemLayer;
    }

    /// <summary>
    /// Delegate for the CohortWaterSupply event
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void CohortWaterSupplyDelegate(CohortWaterSupplyType Data);

    /// <summary>
    /// .NET port of the Fortran SWIM3 model
    /// Ported by Eric Zurcher July-August 2014
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]   // Until we have a better view for SWIM...
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    public class Swim3 : Model
    {
        #region Links

        /// <summary>
        /// The clock
        /// </summary>
        [Link]
        private Clock clock = null;

        //[Link]
        //private Component My = null;  // Get access to "Warning" function

        /// <summary>
        /// The paddock
        /// </summary>
        [Link]
        Simulation paddock = null;

        /// <summary>
        /// The summary
        /// </summary>
        [Link]
        private ISummary summary = null;

        #endregion

        #region Constants

        /// <summary>
        /// The effpar
        /// </summary>
        const double effpar = 0.184;
        /// <summary>
        /// The psi_ll15
        /// </summary>
        const double psi_ll15 = -15000.0;
        /// <summary>
        /// The psiad
        /// </summary>
        const double psiad = -1e6;
        /// <summary>
        /// The psi0
        /// </summary>
        const double psi0 = -0.6e7;

        #endregion

        #region user settable parameters

        /// <summary>
        /// Gets or sets the salb.
        /// </summary>
        /// <value>
        /// The salb.
        /// </value>
        [Description("Bare soil albedo")]
        [Units("0-1")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double Salb
        {
            get
            {
                return salb;
            }
            set
            {
                salb = value;
            }
        }

        /// <summary>
        /// Gets or sets the c n2 bare.
        /// </summary>
        /// <value>
        /// The c n2 bare.
        /// </value>
        [Description("Bare soil runoff curve number")]
        [Bounds(Lower = 0.0, Upper = 100.0)]
        public double CN2Bare { get; set; }

        /// <summary>
        /// Gets or sets the cn red.
        /// </summary>
        /// <value>
        /// The cn red.
        /// </value>
        [Description("Max. reduction in curve number due to cover")]
        [Bounds(Lower = 0.0, Upper = 100.0)]
        public double CNRed { get; set; }

        /// <summary>
        /// Gets or sets the cn cov.
        /// </summary>
        /// <value>
        /// The cn cov.
        /// </value>
        [Description("Cover for max curve number reduction")]
        [Bounds(Lower = 0.0, Upper = 100.0)]
        public double CNCov { get; set; }

        /// <summary>
        /// Gets or sets the k dul.
        /// </summary>
        /// <value>
        /// The k dul.
        /// </value>
        [Description("Hydraulic conductivity at DUL (mm/d)")]
        [Units("mm/d")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        public double KDul { get; set; }

        /// <summary>
        /// Gets or sets the psi dul.
        /// </summary>
        /// <value>
        /// The psi dul.
        /// </value>
        [Description("Matric Potential at DUL (cm)")]
        [Units("cm")]
        [Bounds(Lower = -1e3, Upper = 0.0)]
        public double PSIDul { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Swim3" /> is vc.
        /// </summary>
        /// <value>
        ///   <c>true</c> if vc; otherwise, <c>false</c>.
        /// </value>
        [Description("Vapour Conductivity Calculations?")]
        public bool VC { get; set; }

        /// <summary>
        /// Gets or sets the d tmin.
        /// </summary>
        /// <value>
        /// The d tmin.
        /// </value>
        [Description("Minimum Timestep (min)")]
        [Units("min")]
        [Bounds(Lower = 0.0, Upper = 1440.0)]
        public double DTmin { get; set; }

        /// <summary>
        /// Gets or sets the d tmax.
        /// </summary>
        /// <value>
        /// The d tmax.
        /// </value>
        [Description("Maximum Timestep (min)")]
        [Units("min")]
        [Bounds(Lower = 0.01, Upper = 1440.0)]
        public double DTmax { get; set; }

        /// <summary>
        /// Gets or sets the maximum water increment.
        /// </summary>
        /// <value>
        /// The maximum water increment.
        /// </value>
        [Description("Maximum water increment (mm)")]
        public double MaxWaterIncrement { get; set; }

        /// <summary>
        /// Gets or sets the space weighting factor.
        /// </summary>
        /// <value>
        /// The space weighting factor.
        /// </value>
        [Description("Space weighting factor")]
        public double SpaceWeightingFactor { get; set; }

        /// <summary>
        /// Gets or sets the solute space weighting factor.
        /// </summary>
        /// <value>
        /// The solute space weighting factor.
        /// </value>
        [Description("Solute space weighting factor")]
        public double SoluteSpaceWeightingFactor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Swim3" /> is diagnostics.
        /// </summary>
        /// <value>
        ///   <c>true</c> if diagnostics; otherwise, <c>false</c>.
        /// </value>
        [Description("Diagnostic Information?")]
        public bool Diagnostics { get; set; }

        #endregion

        /// <summary>
        /// Gets or sets the dtmin.
        /// </summary>
        /// <value>
        /// The dtmin.
        /// </value>
        [Units("min")]
        [Bounds(Lower = 0.0, Upper = 1440.0)]
        public double dtmin { get; set; }

        /// <summary>
        /// Gets or sets the dtmax.
        /// </summary>
        /// <value>
        /// The dtmax.
        /// </value>
        [Units("min")]
        [Bounds(Lower = 0.01, Upper = 1440.0)]
        public double dtmax { get; set; }

        /// <summary>
        /// Gets or sets the dw.
        /// </summary>
        /// <value>
        /// The dw.
        /// </value>
        [Bounds(Lower = 1e-3, Upper = 10.0)]
        double dw { get; set; }

        /// <summary>
        /// Gets or sets the SWT.
        /// </summary>
        /// <value>
        /// The SWT.
        /// </value>
        public double swt { get; set; }

        /// <summary>
        /// Gets or sets the negative_conc_warn.
        /// </summary>
        /// <value>
        /// The negative_conc_warn.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        public double negative_conc_warn { get; set; }

        /// <summary>
        /// Gets or sets the negative_conc_fatal.
        /// </summary>
        /// <value>
        /// The negative_conc_fatal.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        public double negative_conc_fatal { get; set; }

        /// <summary>
        /// Gets or sets the max_iterations.
        /// </summary>
        /// <value>
        /// The max_iterations.
        /// </value>
        [Bounds(Lower = 1, Upper = 100)]
        // [Description("number of iterations before timestep is halved")]
        public int max_iterations { get; set; }

        /// <summary>
        /// The _HM0
        /// </summary>
        private double _hm0 = 0;
        /// <summary>
        /// Gets or sets the HM0.
        /// </summary>
        /// <value>
        /// The HM0.
        /// </value>
        [Units("mm")]
        public double hm0 { get; set; }

        /// <summary>
        /// Gets or sets the minimum_surface_storage.
        /// </summary>
        /// <value>
        /// The minimum_surface_storage.
        /// </value>
        [Bounds(Lower = 1e-3, Upper = 100.0)]
        [Units("mm")]
        public double minimum_surface_storage { get; set; }

        /// <summary>
        /// The _HM1
        /// </summary>
        private double _hm1 = 0;
        /// <summary>
        /// Gets or sets the HM1.
        /// </summary>
        /// <value>
        /// The HM1.
        /// </value>
        [Units("mm")]
        public double hm1 { get; set; }

        /// <summary>
        /// Gets or sets the maximum_surface_storage.
        /// </summary>
        /// <value>
        /// The maximum_surface_storage.
        /// </value>
        [Bounds(Lower = 1e-3, Upper = 1000.0)]
        [Units("mm")]
        public double maximum_surface_storage { get; set; }

        /// <summary>
        /// The _HRC
        /// </summary>
        private double _hrc = 0;
        /// <summary>
        /// Gets or sets the HRC.
        /// </summary>
        /// <value>
        /// The HRC.
        /// </value>
        [Units("mm")]
        public double hrc { get; set; }

        /// <summary>
        /// The _GRC
        /// </summary>
        private double _grc = 0;
        /// <summary>
        /// Gets or sets the GRC.
        /// </summary>
        /// <value>
        /// The GRC.
        /// </value>
        [Units("cm")]
        public double grc { get; set; }

        /// <summary>
        /// Gets or sets the roff0.
        /// </summary>
        /// <value>
        /// The roff0.
        /// </value>
        [Bounds(Lower = 1.0e-6, Upper = 100.0)]
        [Units("mm/mm^p")]
        public double roff0 { get; set; }

        /// <summary>
        /// Gets or sets the roff1.
        /// </summary>
        /// <value>
        /// The roff1.
        /// </value>
        [Bounds(Lower = 0.1, Upper = 10.0)]
        public double roff1 { get; set; }

        /// <summary>
        /// Gets or sets the cover_effects.
        /// </summary>
        /// <value>
        /// The cover_effects.
        /// </value>
        public string cover_effects { get; set; }

        /// <summary>
        /// Gets or sets the DPPL.
        /// </summary>
        /// <value>
        /// The DPPL.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        public double dppl { get; set; }

        /// <summary>
        /// Gets or sets the DPNL.
        /// </summary>
        /// <value>
        /// The DPNL.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        public double dpnl { get; set; }

        /// <summary>
        /// Gets or sets the slcerr.
        /// </summary>
        /// <value>
        /// The slcerr.
        /// </value>
        [Bounds(Lower = 1e-8, Upper = 1e-4)]
        public double slcerr { get; set; }

        /// <summary>
        /// The _G0
        /// </summary>
        private double _g0 = 0;

        /// <summary>
        /// The g0
        /// </summary>
        [Units("mm")]
        public double g0 = Double.NaN;

        /// <summary>
        /// Gets or sets the minimum_conductance.
        /// </summary>
        /// <value>
        /// The minimum_conductance.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Units("/h")]
        public double minimum_conductance { get; set; }

        /// <summary>
        /// The _G1
        /// </summary>
        private double _g1 = 0;

        /// <summary>
        /// The g1
        /// </summary>
        [Units("/h")]
        public double g1 = Double.NaN;

        /// <summary>
        /// Gets or sets the maximum_conductance.
        /// </summary>
        /// <value>
        /// The maximum_conductance.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0e6)]
        [Units("/h")]
        public double maximum_conductance { get; set; }

        /// <summary>
        /// Gets or sets the ersoil.
        /// </summary>
        /// <value>
        /// The ersoil.
        /// </value>
        [Bounds(Lower = 1.0e-10, Upper = 1.0)]
        public double ersoil { get; set; }

        /// <summary>
        /// Gets or sets the ernode.
        /// </summary>
        /// <value>
        /// The ernode.
        /// </value>
        [Bounds(Lower = 1.0e-10, Upper = 1.0)]
        public double ernode { get; set; }

        /// <summary>
        /// Gets or sets the errex.
        /// </summary>
        /// <value>
        /// The errex.
        /// </value>
        [Bounds(Lower = 1.0e-10, Upper = 1.0)]
        public double errex { get; set; }

        /// <summary>
        /// Gets or sets the SLSWT.
        /// </summary>
        /// <value>
        /// The SLSWT.
        /// </value>
        [Bounds(Lower = -1.0, Upper = 1.0)]
        public double slswt { get; set; }

        /// <summary>
        /// Gets or sets the a_to_evap_fact.
        /// </summary>
        /// <value>
        /// The a_to_evap_fact.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        //[Description("converts residue specfic area 'A' to")]
        public double a_to_evap_fact { get; set; }

        /// <summary>
        /// Gets or sets the canopy_eos_coef.
        /// </summary>
        /// <value>
        /// The canopy_eos_coef.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        //[Description("coef. in exp effect of canopy on")]
        public double canopy_eos_coef { get; set; }

        /// <summary>
        /// The _SWF
        /// </summary>
        private double[] _swf = null;

        //[Units("mm")]
        //private double rain = Double.NaN;   // from met file

        //[Units("oC")]
        //private double mint;

        //[Units("oC")]
        //private double maxt;

        //[Units("MJ")]
        //private double radn;

        /// <summary>
        /// kdul
        /// </summary>
        /// <value>
        /// The kdul.
        /// </value>
        [Units("mm/d")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        public double kdul { get; set; }

        /// <summary>
        /// psidul
        /// </summary>
        /// <value>
        /// The psidul.
        /// </value>
        [Units("cm")]
        [Bounds(Lower = -1e3, Upper = 0.0)]
        public double psidul { get; set; }

        /// <summary>
        /// Canopy factors for cover runoff effect
        /// </summary>
        /// <value>
        /// The canopy_fact.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] canopy_fact { get; set; }

        /// <summary>
        /// Heights for canopy factors (mm)
        /// </summary>
        /// <value>
        /// The canopy_fact_height.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 100000.0)]
        [Units("mm")]
        public double[] canopy_fact_height { get; set; }

        /// <summary>
        /// Default canopy factor in absence of height
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double canopy_fact_default;

        /// <summary>
        /// Gets or sets the cn2_bare.
        /// </summary>
        /// <value>
        /// The cn2_bare.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        public double cn2_bare { get; set; }

        /// <summary>
        /// Gets or sets the cn_red.
        /// </summary>
        /// <value>
        /// The cn_red.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        public double cn_red { get; set; }

        /// <summary>
        /// Gets or sets the cn_cov.
        /// </summary>
        /// <value>
        /// The cn_cov.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        public double cn_cov { get; set; }

        /// <summary>
        /// Default time of rainfall (hh:mm)
        /// </summary>
        /// <value>
        /// The default_rain_time.
        /// </value>
        public string default_rain_time { get; set; }

        /// <summary>
        /// Default duration of rainfall (min)
        /// </summary>
        /// <value>
        /// The default_rain_duration.
        /// </value>
        [Bounds(Lower = 0, Upper = 1440)]
        [Units("min")]
        public double default_rain_duration { get; set; }

        /// <summary>
        /// Default time of evaporation (hh:mm)
        /// </summary>
        /// <value>
        /// The default_evap_time.
        /// </value>
        public string default_evap_time { get; set; }

        /// <summary>
        /// Default duration of evaporation (min)
        /// </summary>
        /// <value>
        /// The default_evap_duration.
        /// </value>
        [Bounds(Lower = 0, Upper = 1440)]
        [Units("min")]
        public double default_evap_duration { get; set; }

        /// <summary>
        /// Gets or sets the hydrol_effective_depth.
        /// </summary>
        /// <value>
        /// The hydrol_effective_depth.
        /// </value>
        [Bounds(Lower = 0, Upper = 1e4)]
        [Units("mm")]
        public double hydrol_effective_depth { get; set; }

        /// <summary>
        /// Gets or sets the min_crit_temp.
        /// </summary>
        /// <value>
        /// The min_crit_temp.
        /// </value>
        [Bounds(Lower = -10.0, Upper = 100.0)]
        [Units("oC")]
        //[Description("temperature below which eeq decreases (oC)")]
        public double min_crit_temp { get; set; }

        /// <summary>
        /// Gets or sets the max_crit_temp.
        /// </summary>
        /// <value>
        /// The max_crit_temp.
        /// </value>
        [Bounds(Lower = -10.0, Upper = 100.0)]
        [Units("oC")]
        //[Description("temperature above which eeq increases (oC)")]
        public double max_crit_temp { get; set; }

        /// <summary>
        /// Gets or sets the max_albedo.
        /// </summary>
        /// <value>
        /// The max_albedo.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        //[Description("albedo at 100% green crop cover (0-1).")]
        public double max_albedo { get; set; }

        /// <summary>
        /// Gets or sets the residue_albedo.
        /// </summary>
        /// <value>
        /// The residue_albedo.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("albedo at 100% residue cover (0-1).")]
        public double residue_albedo { get; set; }

        /// <summary>
        /// Gets the cover_surface_runoff.
        /// </summary>
        /// <value>
        /// The cover_surface_runoff.
        /// </value>
        [Units("0-1")]
        public double cover_surface_runoff
        {
            get
            {
                return _cover_surface_runoff;
            }
        }

        /// <summary>
        /// Gets or sets the evap_source.
        /// </summary>
        /// <value>
        /// The evap_source.
        /// </value>
        public string evap_source { get; set; }

        /// <summary>
        /// The mint
        /// </summary>
        [Units("oC")]
        private double mint = 0;

        /// <summary>
        /// The maxt
        /// </summary>
        [Units("oC")]
        private double maxt = 0;

        /// <summary>
        /// The radn
        /// </summary>
        [Units("MJ")]
        private double radn = 0;

        /// <summary>
        /// The eo_time
        /// </summary>
        private string eo_time = string.Empty;

        /// <summary>
        /// The eo_durn
        /// </summary>
        private double eo_durn = Double.NaN;

        /// <summary>
        /// The cover_green_sum
        /// </summary>
        private double cover_green_sum = Double.NaN;

        /// <summary>
        /// The interception
        /// </summary>
        [Units("mm")]
        private double interception = Double.NaN;

        /// <summary>
        /// The residueinterception
        /// </summary>
        [Units("mm")]
        private double residueinterception = Double.NaN;

        /// <summary>
        /// The surfaceom_cover
        /// </summary>
        [Units("0-1")]
        private double surfaceom_cover = Double.NaN;

        /// <summary>
        /// The salb
        /// </summary>
        private double salb;
        /// <summary>
        /// The _dlayer
        /// </summary>
        private double[] _dlayer = null;
        /// <summary>
        /// The _LL15
        /// </summary>
        private double[] _ll15;
        /// <summary>
        /// The _dul
        /// </summary>
        private double[] _dul;
        /// <summary>
        /// The _sat
        /// </summary>
        private double[] _sat = null;
        /// <summary>
        /// The _KS
        /// </summary>
        private double[] _ks = null;
        /// <summary>
        /// The _air_dry
        /// </summary>
        private double[] _air_dry;

        /// <summary>
        /// The swim rain time
        /// </summary>
        private double[] SWIMRainTime = new double[0];
        /// <summary>
        /// The swim rain amt
        /// </summary>
        private double[] SWIMRainAmt = new double[0];
        /// <summary>
        /// The swim eq rain time
        /// </summary>
        private double[] SWIMEqRainTime = new double[0];
        /// <summary>
        /// The swim eq rain amt
        /// </summary>
        private double[] SWIMEqRainAmt = new double[0];
        /// <summary>
        /// The swim evap time
        /// </summary>
        private double[] SWIMEvapTime = new double[0];
        /// <summary>
        /// The swim evap amt
        /// </summary>
        private double[] SWIMEvapAmt = new double[0];
        /// <summary>
        /// The swim sol time
        /// </summary>
        private double[][] SWIMSolTime = null;
        /// <summary>
        /// The swim sol amt
        /// </summary>
        private double[][] SWIMSolAmt = null;
        /// <summary>
        /// The sub surface in flow
        /// </summary>
        private double[] SubSurfaceInFlow = null;
        /// <summary>
        /// The day
        /// </summary>
        private int day = 0;
        /// <summary>
        /// The year
        /// </summary>
        private int year = 0;
        /// <summary>
        /// The apsim_timestep
        /// </summary>
        private double apsim_timestep = 1440.0;
        /// <summary>
        /// The start_day
        /// </summary>
        private int start_day = 0;
        /// <summary>
        /// The start_year
        /// </summary>
        private int start_year = 0;
        /// <summary>
        /// The apsim_time
        /// </summary>
        private string apsim_time = "00:00";
        /// <summary>
        /// The run_has_started
        /// </summary>
        private bool run_has_started;

        ///// <summary>The psim</summary>
        // private double psim;
        ///// <summary>The psimin</summary>
        /// <summary>
        /// The psimin
        /// </summary>
        private double[] psimin = null;
        /// <summary>
        /// The RLD
        /// </summary>
        private double[][] rld = null;
        /// <summary>
        /// The rc
        /// </summary>
        private double[][] rc = null;
        /// <summary>
        /// The RTP
        /// </summary>
        private double[] rtp = null;
        /// <summary>
        /// The rt
        /// </summary>
        private double[] rt = null;
        /// <summary>
        /// The CTP
        /// </summary>
        private double[] ctp = null;
        /// <summary>
        /// The ct
        /// </summary>
        private double[] ct = null;
        /// <summary>
        /// The qr
        /// </summary>
        private double[][] qr = null;
        /// <summary>
        /// The qrpot
        /// </summary>
        private double[][] qrpot = null;

        /// <summary>
        /// array of crop names
        /// </summary>
        private string[] crop_names;
        /// <summary>
        /// array of crop owners (component names)
        /// </summary>
        private string[] crop_owners;
        /// <summary>
        /// array of crop owner ids
        /// </summary>
        private int[] crop_owner_id;
        /// <summary>
        /// array indicating whether crop is "in"
        /// </summary>
        private bool[] crop_in;
        /// <summary>
        /// array indicating whether demand has been received for each crop
        /// </summary>
        private bool[] demand_received;
        /// <summary>
        /// number of crops (and size of the associated arrays)
        /// </summary>
        private int num_crops = 0;
        /// <summary>
        /// event numbers for sending CohortWaterSupply
        /// </summary>
        private int[] supply_event_id; 

        ///// <summary>The uptake_water_id</summary>
        /// <summary>
        /// The uptake_water_id
        /// </summary>
        private int[] uptake_water_id; // Property number for returning crop water uptake
        ///// <summary>The supply_solute_id</summary>
        /// <summary>
        /// The supply_solute_id
        /// </summary>
        private int[][] supply_solute_id; // Property number for returning crop solute supply

        ///// <summary>The leach_id</summary>
        /// <summary>
        /// The leach_id
        /// </summary>
        private int[] leach_id;
        ///// <summary>The flow_id</summary>
        /// <summary>
        /// The flow_id
        /// </summary>
        private int[] flow_id;
        ///// <summary>The exco_id</summary>
        /// <summary>
        /// The exco_id
        /// </summary>
        private int[] exco_id;
        ///// <summary>The conc_water_id</summary>
        /// <summary>
        /// The conc_water_id
        /// </summary>
        private int[] conc_water_id;
        ///// <summary>The conc_adsorb_id</summary>
        /// <summary>
        /// The conc_adsorb_id
        /// </summary>
        private int[] conc_adsorb_id;
        ///// <summary>The subsurface_drain_id</summary>
        /// <summary>
        /// The subsurface_drain_id
        /// </summary>
        private int[] subsurface_drain_id;

        /// <summary>
        /// The nveg
        /// </summary>
        private int nveg = 0;
        /// <summary>
        /// The root radius
        /// </summary>
        private double[][] RootRadius = null; // Was root_radius
        /// <summary>
        /// The root conductance
        /// </summary>
        private double[][] RootConductance = null; // was root_conductance
        /// <summary>
        /// The pep
        /// </summary>
        private double[] pep = null;
        /// <summary>
        /// The solute_demand
        /// </summary>
        private double[][] solute_demand = null;
        ///// <summary>The canopy_height</summary>
        /// <summary>
        /// The canopy_height
        /// </summary>
        private double[] canopy_height;
        ///// <summary>The cover_tot</summary>
        /// <summary>
        /// The cover_tot
        /// </summary>
        private double[] cover_tot;

        /// <summary>
        /// The crop_cover
        /// </summary>
        private double crop_cover = 0;
        /// <summary>
        /// The residue_cover
        /// </summary>
        private double residue_cover = 0;
        ///// <summary>The _cover_green_sum</summary>
        /// <summary>
        /// The _cover_green_sum
        /// </summary>
        private double _cover_green_sum;
        ///// <summary>The _cover_surface_runoff</summary>
        /// <summary>
        /// The _cover_surface_runoff
        /// </summary>
        private double _cover_surface_runoff = 0;

        /// <summary>
        /// The QBP
        /// </summary>
        private double qbp;
        ///// <summary>The QBPD</summary>
        // private double qbpd;
        /// <summary>
        /// The QSLBP
        /// </summary>
        private double[] qslbp = null;

        /// <summary>
        /// The gf
        /// </summary>
        private double gf = 0;

        /// <summary>
        /// The swta
        /// </summary>
        private double[] swta = null;

        /// <summary>
        /// The psuptake
        /// </summary>
        private double[][][] psuptake = null;
        /// <summary>
        /// The pwuptake
        /// </summary>
        private double[][] pwuptake = null;
        /// <summary>
        /// The pwuptakepot
        /// </summary>
        private double[][] pwuptakepot = null;
        ///// <summary>The cslold</summary>
        /// <summary>
        /// The cslold
        /// </summary>
        private double[][] cslold;
        /// <summary>
        /// The cslstart
        /// </summary>
        private double[][] cslstart = null;

        ///// <summary>The crops_found</summary>
        // private bool crops_found;
        ///// <summary>The _psix</summary>
        /// <summary>
        /// The _psix
        /// </summary>
        private double[] _psix = null;

        ///// <summary>The c n_runoff</summary>
        /// <summary>
        /// The c n_runoff
        /// </summary>
        private double CN_runoff;
        ///// <summary>The de lk</summary>
        /// <summary>
        /// The de lk
        /// </summary>
        private double[,] DELk;
        ///// <summary>The mk</summary>
        /// <summary>
        /// The mk
        /// </summary>
        private double[,] Mk;
        /// <summary>
        /// The m0
        /// </summary>
        private double[,] M0 = null;
        /// <summary>
        /// The m1
        /// </summary>
        private double[,] M1 = null;
        /// <summary>
        /// The y0
        /// </summary>
        private double[,] Y0 = null;
        /// <summary>
        /// The y1
        /// </summary>
        private double[,] Y1 = null;
        /// <summary>
        /// The micro p
        /// </summary>
        private double[] MicroP = null;
        /// <summary>
        /// The micro ks
        /// </summary>
        private double[] MicroKs = null;
        ///// <summary>The kdula</summary>
        /// <summary>
        /// The kdula
        /// </summary>
        private double[] Kdula;
        /// <summary>
        /// The macro p
        /// </summary>
        private double[] MacroP = null;

        /// <summary>
        /// The t d_runoff
        /// </summary>
        private double TD_runoff;
        /// <summary>
        /// The t d_rain
        /// </summary>
        private double TD_rain;
        /// <summary>
        /// The t d_evap
        /// </summary>
        private double TD_evap;
        /// <summary>
        /// The t d_pevap
        /// </summary>
        private double TD_pevap;
        /// <summary>
        /// The t d_drain
        /// </summary>
        private double TD_drain;
        /// <summary>
        /// The t d_subsurface_drain
        /// </summary>
        private double TD_subsurface_drain;
        /// <summary>
        /// The t d_soldrain
        /// </summary>
        private double[] TD_soldrain = null;
        /// <summary>
        /// The t d_slssof
        /// </summary>
        private double[] TD_slssof = null;
        /// <summary>
        /// The t d_wflow
        /// </summary>
        private double[] TD_wflow = null;
        /// <summary>
        /// The t d_sflow
        /// </summary>
        private double[][] TD_sflow = null;

        /// <summary>
        /// The t
        /// </summary>
        private double t;
        /// <summary>
        /// The _DT
        /// </summary>
        private double _dt;

        /// <summary>
        /// The _WP
        /// </summary>
        private double _wp;
        ///// <summary>The WP0</summary>
        // private double wp0;

        /// <summary>
        /// The _p
        /// </summary>
        private double[] _p = null;
        /// <summary>
        /// The _psi
        /// </summary>
        private double[] _psi = null;
        /// <summary>
        /// The th
        /// </summary>
        private double[] th = null;
        /// <summary>
        /// The thold
        /// </summary>
        private double[] thold = null;
        /// <summary>
        /// The hk
        /// </summary>
        private double[] hk;
        /// <summary>
        /// The q
        /// </summary>
        private double[] q = null;
        /// <summary>
        /// The _h
        /// </summary>
        private double _h;
        /// <summary>
        /// The hold
        /// </summary>
        private double hold;
        /// <summary>
        /// The ron
        /// </summary>
        private double ron;
        /// <summary>
        /// The roff
        /// </summary>
        private double roff;
        /// <summary>
        /// The resource
        /// </summary>
        private double res;
        /// <summary>
        /// The resp
        /// </summary>
        private double resp;
        /// <summary>
        /// The rex
        /// </summary>
        private double rex;
        /// <summary>
        /// The RSSF
        /// </summary>
        private double rssf;
        /// <summary>
        /// The qs
        /// </summary>
        private double[] qs = null;
        /// <summary>
        /// The qex
        /// </summary>
        private double[] qex;
        /// <summary>
        /// The qexpot
        /// </summary>
        private double[] qexpot = null;
        /// <summary>
        /// The qssif
        /// </summary>
        private double[] qssif = null;
        /// <summary>
        /// The qssof
        /// </summary>
        private double[] qssof = null;


        /// <summary>
        /// The dc
        /// </summary>
        private double[][] dc = null;
        /// <summary>
        /// The CSL
        /// </summary>
        private double[][] csl;
        /// <summary>
        /// The CSLT
        /// </summary>
        private double[][] cslt = null;
        /// <summary>
        /// The QSL
        /// </summary>
        private double[][] qsl = null;
        /// <summary>
        /// The QSLS
        /// </summary>
        private double[][] qsls = null;
        /// <summary>
        /// The slsur
        /// </summary>
        private double[] slsur = null;
        /// <summary>
        /// The cslsur
        /// </summary>
        private double[] cslsur = null;
        /// <summary>
        /// The rslon
        /// </summary>
        private double[] rslon = null;
        /// <summary>
        /// The rsloff
        /// </summary>
        private double[] rsloff = null;
        /// <summary>
        /// The rslex
        /// </summary>
        private double[] rslex = null;

        /// <summary>
        /// The demand_is_met
        /// </summary>
        private bool[][] demand_is_met = null;

        /// <summary>
        /// The solute_owners
        /// </summary>
        private int[] solute_owners = null;

        /// <summary>
        /// The _work
        /// </summary>
        private double _work;
        /// <summary>
        /// The slwork
        /// </summary>
        private double slwork;

        /// <summary>
        /// The _hmin
        /// </summary>
        private double _hmin = Double.NaN;

        /// <summary>
        /// The gsurf
        /// </summary>
        private double gsurf;

        /// <summary>
        /// The psid
        /// </summary>
        private double[] psid = null;

        /// <summary>
        /// The n
        /// </summary>
        private int n = 0;
        /// <summary>
        /// The x
        /// </summary>
        private double[] x = null;
        /// <summary>
        /// The dx
        /// </summary>
        private double[] dx = null;

        /// <summary>
        /// The c
        /// </summary>
        private double[] c = null;
        /// <summary>
        /// The k
        /// </summary>
        private double[] k = null;

        /// <summary>
        /// The ivap
        /// </summary>
        private bool ivap = false;
        /// <summary>
        /// The isbc
        /// </summary>
        private int isbc = 0;
        /// <summary>
        /// The itbc
        /// </summary>
        private int itbc = 0;
        /// <summary>
        /// The ibbc
        /// </summary>
        private int ibbc = 0;

        /// <summary>
        /// The ex
        /// </summary>
        private double[][] ex = null;
        /// <summary>
        /// The CSLGW
        /// </summary>
        private double[] cslgw = null;
        ///// <summary>The slupf</summary>
        /// <summary>
        /// The slupf
        /// </summary>
        private double[] slupf;
        /// <summary>
        /// The slsci
        /// </summary>
        private double[] slsci = null;
        /// <summary>
        /// The SLSCR
        /// </summary>
        private double[] slscr = null;
        /// <summary>
        /// The dcon
        /// </summary>
        private double[] dcon = null;

        /// <summary>
        /// The initial psi
        /// </summary>
        private double[] init_psi;
        /// <summary>
        /// The rhob
        /// </summary>
        private double[] rhob;
        /// <summary>
        /// The exco
        /// </summary>
        private double[][] exco;

        /// <summary>
        /// The fip
        /// </summary>
        double[][] fip = null;

        /// <summary>
        /// The slos
        /// </summary>
        private double[] slos;
        ///// <summary>The d0</summary>
        /// <summary>
        /// The d0
        /// </summary>
        private double[] d0;

        /// <summary>
        /// The solute_names
        /// </summary>
        private string[] solute_names = null;
        /// <summary>
        /// The num_solutes
        /// </summary>
        private int num_solutes = 0;

        /// <summary>
        /// The bbc_value
        /// </summary>
        private double bbc_value = 0;
        /// <summary>
        /// The water_table_conductance
        /// </summary>
        private double water_table_conductance = 0;

        /// <summary>
        /// The subsurface drain
        /// </summary>
        private string subsurfaceDrain = null;

        /// <summary>
        /// Gets or sets the swim solute parameters.
        /// </summary>
        /// <value>
        /// The swim solute parameters.
        /// </value>
        public SwimSoluteParameters SwimSoluteParameters { get; set; }
        /// <summary>
        /// Gets or sets the swim water table.
        /// </summary>
        /// <value>
        /// The swim water table.
        /// </value>
        public SwimWaterTable SwimWaterTable { get; set; }
        /// <summary>
        /// Gets or sets the swim subsurface drain.
        /// </summary>
        /// <value>
        /// The swim subsurface drain.
        /// </value>
        public SwimSubsurfaceDrain SwimSubsurfaceDrain { get; set; }

        // In the Fortran version, the data for ponding water was held in
        // array members with an index of -1.
        // In this version, I've created this PondingData structure to hold those values.
        // Note, however, that SWIM3 in APSIM never allowed the user to
        // set the value for isbc, which controls the way ponding is handled;
        // as a consequence, this version of the logic remains untested.
        /// <summary>
        /// 
        /// </summary>
        private struct PondingData
        {
            /// <summary>
            /// The b
            /// </summary>
            public double b;
            /// <summary>
            /// The c
            /// </summary>
            public double c;
            /// <summary>
            /// The RHS
            /// </summary>
            public double rhs;
            /// <summary>
            /// The v
            /// </summary>
            public double v;
        };

        /// <summary>
        /// WaterUptakesCalculated event
        /// </summary>
        public event PMF.WaterUptakesCalculatedDelegate WaterUptakesCalculated;
        /// <summary>
        /// CohortWaterSupply event
        /// </summary>
        public event CohortWaterSupplyDelegate CohortWaterSupply;
        /// <summary>
        /// NewProfile event
        /// </summary>
        public event NitrogenChangedDelegate NitrogenChanged;

        //private bool initDone;

        #region event handlers
        /// <summary>
        /// Eventhandeler - initialisation
        /// </summary>
        [EventSubscribe("Initialised")]
        public void OnInitialised()
        {
            ////// TODO OnReset();
            //initDone = true;
            OnSum_Report();
        }

        /// <summary>
        /// EventHandler - generate a summary report
        /// </summary>
        /// <exception cref="System.Exception">bad bottom boundary conditions switch</exception>
        [EventSubscribe("SumReport")]
        public void OnSum_Report()
        {
            //Manager module can request that each module write its variables out to the summary file. This handles that event. 

            //+  Purpose
            //   Report all initial conditions and input parameters to the
            //   summary file.

            //+  Constant Values
            const int num_psio = 8;

            double[,] tho = new double[n + 1, num_psio];
            double[,] hklo = new double[n + 1, num_psio];
            double[,] hko = new double[n + 1, num_psio];
            double thd;
            double hklgd;

            //+  Initial Data Values
            double[] psio = new double[num_psio] { -0.01, -10.0, -25.0, -100.0, -1000.0, -15000.0, -1.0e6, -1.0e7 };

            summary.WriteMessage(this, Environment.NewLine + Environment.NewLine +
                  "                      APSIM Soil Profile" + Environment.NewLine +
                  "                      ------------------" + Environment.NewLine);

            summary.WriteMessage(this, "---------------------------------------------------------------" + Environment.NewLine +
                              " x    dlayer   BD   SW     LL15   DUL   SAT      Ks      Psi");
            summary.WriteMessage(this, "---------------------------------------------------------------");

            for (int layer = 0; layer <= n; layer++)
            {
                summary.WriteMessage(this, String.Format("{0,6:F1} {1,6:F1}  {2,4:F3}  {3,5:F3}  {4,5:F3}  {5,5:F3}  {6,5:F3} {7,6:F2} {8,8:F2}",
                                           x[layer] * 10.0,
                                           _dlayer[layer], rhob[layer], th[layer],
                                           _ll15[layer], _dul[layer], _sat[layer], _ks[layer],
                                           _psi[layer]));

            }
            summary.WriteMessage(this, "---------------------------------------------------------------");

            // calculate Theta and g%hk for each psio

            for (int i = 0; i < num_psio; i++)
            {
                for (int j = 0; j <= n; j++)
                {
                    Interp(j, psio[i], out tho[j, i], out thd, out hklo[j, i], out hklgd);
                    hko[j, i] = Math.Pow(10.0, hklo[j, i]);
                }
            }

            summary.WriteMessage(this, Environment.NewLine + Environment.NewLine +
                              "                  Soil Moisture Characteristics" + Environment.NewLine +
                              "                  -----------------------------" + Environment.NewLine);
            summary.WriteMessage(this, "--------------------------------------------------------------------");
            summary.WriteMessage(this, "                         Soil Water Potential (cm)");
            summary.WriteMessage(this, "    x       0      10      25    100   1000  15000   10^6   10^7");
            summary.WriteMessage(this, "--------------------------------------------------------------------");

            for (int j = 0; j <= n; j++)
            {
                summary.WriteMessage(this, String.Format("{0,6:F1} | {1,6:F4} {2,6:F4} {3,6:F4} {4,6:F4} {5,6:F4} {6,6:F4} {7,6:F4} {8,6:F4}",
                    x[j] * 10.0, tho[j, 0], tho[j, 1], tho[j, 2], tho[j, 3], tho[j, 4], tho[j, 5], tho[j, 6], tho[j, 7]));
            }

            summary.WriteMessage(this, "--------------------------------------------------------------------");
            summary.WriteMessage(this, Environment.NewLine + Environment.NewLine +
                              "                   Soil Hydraulic Conductivity" + Environment.NewLine +
                              "                   ---------------------------" + Environment.NewLine);
            summary.WriteMessage(this, "-----------------------------------------------------------------------");
            summary.WriteMessage(this, "                         Soil Water Potential (cm)");
            summary.WriteMessage(this, "    x       0        10       25       100     1000    15000     10^6");
            summary.WriteMessage(this, "-----------------------------------------------------------------------");

            for (int j = 0; j <= n; j++)
            {
                summary.WriteMessage(this, String.Format("{0,6:F1} | {1,8:G3} {2,8:G3} {3,8:G3} {4,8:G3} {5,8:G3} {6,8:G3} {7,8:G3}",
                    x[j] * 10.0, hko[j, 0] * 24.0 * 10.0, hko[j, 1] * 24.0 * 10.0, hko[j, 2] * 24.0 * 10.0, hko[j, 3] * 24.0 * 10.0,
                                 hko[j, 4] * 24.0 * 10.0, hko[j, 5] * 24.0 * 10.0, hko[j, 6] * 24.0 * 10.0));
            }

            summary.WriteMessage(this, "-----------------------------------------------------------------------");
            summary.WriteMessage(this, Environment.NewLine);

            if (ibbc == 0)
                summary.WriteMessage(this, String.Format("     bottom boundary condition = specified gradient ({0,10:F3})",
                                  bbc_value));
            else if (ibbc == 1)
                summary.WriteMessage(this, "     bottom boundary condition = specified potential");
            else if (ibbc == 2)
                summary.WriteMessage(this, "     bottom boundary condition = zero flux");
            else if (ibbc == 3)
                summary.WriteMessage(this, "     bottom boundary condition = free drainage");
            else if (ibbc == 4)
                summary.WriteMessage(this, "     bottom boundary condition = water table");
            else
                throw new Exception("bad bottom boundary conditions switch");

            if (ivap)
                summary.WriteMessage(this, "     vapour conductivity       = on");
            else
                summary.WriteMessage(this, "     vapour conductivity       = off");

            summary.WriteMessage(this, "     Evaporation Source        = " + evap_source + Environment.NewLine);

            if (SwimSubsurfaceDrain != null && !Double.IsNaN(SwimSubsurfaceDrain.DrainDepth))
            {
                summary.WriteMessage(this, " Subsurface Drain Model");
                summary.WriteMessage(this, " ======================" + Environment.NewLine);

                summary.WriteMessage(this, string.Format("     Drain Depth (mm) ={0,10:F3}", SwimSubsurfaceDrain.DrainDepth));
                summary.WriteMessage(this, string.Format("     Drain Spacing (mm) ={0,10:F3}", SwimSubsurfaceDrain.DrainSpacing));
                summary.WriteMessage(this, string.Format("     Drain Radius (mm) ={0,10:F3}", SwimSubsurfaceDrain.DrainRadius));
                summary.WriteMessage(this, string.Format("     Imperm Layer Depth (mm)  ={0,10:F3}", SwimSubsurfaceDrain.ImpermDepth));
                summary.WriteMessage(this, string.Format("     Lateral Conductivity (mm/d)  ={0,10:F3}", SwimSubsurfaceDrain.Klat));
            }

        }

        #endregion

        /// <summary>
        /// Resizes arrays related to the soil profile
        /// That is, handles changes in the number of layers
        /// </summary>
        /// <param name="newSize">The new size.</param>
        private void ResizeProfileArrays(int newSize)
        {
            int oldSize = n + 1;

            DELk = new double[newSize, 4];
            Mk = new double[newSize, 4];
            M0 = new double[newSize, 5];
            M1 = new double[newSize, 5];
            Y0 = new double[newSize, 5];
            Y1 = new double[newSize, 5];

            Array.Resize(ref _swf, newSize);
            Array.Resize(ref SubSurfaceInFlow, newSize);
            Array.Resize(ref TD_wflow, newSize + 1);
            Array.Resize(ref _p, newSize);
            Array.Resize(ref _psi, newSize);
            Array.Resize(ref th, newSize);
            Array.Resize(ref thold, newSize);
            Array.Resize(ref hk, newSize);
            Array.Resize(ref q, newSize + 1);
            Array.Resize(ref qs, newSize);
            Array.Resize(ref qex, newSize);
            Array.Resize(ref qexpot, newSize);
            Array.Resize(ref qssif, newSize);
            Array.Resize(ref qssof, newSize + 1);
            Array.Resize(ref rld, newSize);
            Array.Resize(ref rc, newSize);
            Array.Resize(ref qr, newSize);
            Array.Resize(ref qrpot, newSize);
            Array.Resize(ref RootRadius, newSize);
            Array.Resize(ref RootConductance, newSize);
            Array.Resize(ref swta, newSize);
            Array.Resize(ref MicroP, newSize);
            Array.Resize(ref MicroKs, newSize);
            Array.Resize(ref Kdula, newSize);
            Array.Resize(ref MacroP, newSize);
            Array.Resize(ref _dlayer, newSize);
            Array.Resize(ref _ll15, newSize);
            Array.Resize(ref _dul, newSize);
            Array.Resize(ref _sat, newSize);
            Array.Resize(ref _ks, newSize);
            Array.Resize(ref _air_dry, newSize);
            Array.Resize(ref c, newSize);
            Array.Resize(ref k, newSize);
            Array.Resize(ref psid, newSize);
            Array.Resize(ref init_psi, newSize);
            Array.Resize(ref rhob, newSize);
            Array.Resize(ref x, newSize);
            Array.Resize(ref dx, newSize);

            for (int i = oldSize; i < newSize; i++)
            {
                Array.Resize(ref rld[i], num_crops);
                Array.Resize(ref rc[i], num_crops);
                Array.Resize(ref qr[i], num_crops);
                Array.Resize(ref qrpot[i], num_crops);
                Array.Resize(ref RootRadius[i], num_crops);
                Array.Resize(ref RootConductance[i], num_crops);
            }

            for (int crop = 0; crop < num_crops; crop++)
            {
                Array.Resize(ref pwuptake[crop], newSize);
                Array.Resize(ref pwuptakepot[crop], newSize);
            }

            for (int sol = 0; sol < num_solutes; sol++)
            {
                Array.Resize(ref TD_sflow[sol], newSize + 1);
                Array.Resize(ref dc[sol], newSize);
                Array.Resize(ref csl[sol], newSize + 1);
                Array.Resize(ref cslt[sol], newSize);
                Array.Resize(ref qsl[sol], newSize + 1);
                Array.Resize(ref qsls[sol], newSize);
                Array.Resize(ref cslold[sol], newSize);
                Array.Resize(ref cslstart[sol], newSize);
                Array.Resize(ref exco[sol], newSize);
                Array.Resize(ref ex[sol], newSize);
                Array.Resize(ref fip[sol], newSize);

                for (int crop = 0; crop < num_crops; crop++)
                {
                    Array.Resize(ref psuptake[sol][crop], newSize);
                }
            }

            n = newSize - 1;
        }

        /// <summary>
        /// Resizes arrays relating to crops
        /// </summary>
        /// <param name="newSize">The new size.</param>
        private void ResizeCropArrays(int newSize)
        {
            int oldSize = num_crops;

            Array.Resize(ref psimin, newSize);
            Array.Resize(ref rtp, newSize);
            Array.Resize(ref rt, newSize);
            Array.Resize(ref ctp, newSize);
            Array.Resize(ref ct, newSize);
            Array.Resize(ref crop_names, newSize);
            Array.Resize(ref crop_owners, newSize);
            Array.Resize(ref crop_owner_id, newSize);
            Array.Resize(ref crop_in, newSize);
            Array.Resize(ref demand_received, newSize);
            Array.Resize(ref supply_event_id, newSize);
            Array.Resize(ref pep, newSize);
            Array.Resize(ref canopy_height, newSize);
            Array.Resize(ref cover_tot, newSize);
            Array.Resize(ref _psix, newSize);
            Array.Resize(ref pwuptake, newSize);
            Array.Resize(ref pwuptakepot, newSize);
            Array.Resize(ref uptake_water_id, newSize);
            Array.Resize(ref supply_solute_id, newSize);
            Array.Resize(ref demand_is_met, newSize);
            Array.Resize(ref solute_demand, newSize);

            for (int idx = oldSize; idx < newSize; idx++)
            {
                Array.Resize(ref pwuptake[idx], n + 1);
                Array.Resize(ref pwuptakepot[idx], n + 1);

                Array.Resize(ref demand_is_met[idx], num_solutes);
                Array.Resize(ref solute_demand[idx], num_solutes);
                Array.Resize(ref supply_solute_id[idx], num_solutes);
            }

            for (int i = 0; i <= n; i++)
            {
                Array.Resize(ref rld[i], newSize);
                Array.Resize(ref rc[i], newSize);
                Array.Resize(ref qr[i], newSize);
                Array.Resize(ref qrpot[i], newSize);
                Array.Resize(ref RootRadius[i], newSize);
                Array.Resize(ref RootConductance[i], newSize);

            }

            for (int sol = 0; sol < num_solutes; sol++)
            {
                Array.Resize(ref psuptake[sol], newSize);
                for (int idx = oldSize; idx < newSize; idx++)
                {
                    Array.Resize(ref psuptake[sol][idx], n + 1);
                }
            }

            /* Don't resize these; they are Params
            Array.Resize(ref crop_name, newSize);
            Array.Resize(ref min_xylem_potential, newSize);
            Array.Resize(ref root_radius, newSize);
            Array.Resize(ref root_conductance, newSize);
             */

            num_crops = newSize;
        }

        /// <summary>
        /// Resizes arrays related to solutes
        /// </summary>
        /// <param name="newSize">The new size.</param>
        private void ResizeSoluteArrays(int newSize)
        {
            int oldSize = num_solutes;
            Array.Resize(ref solute_names, newSize);
            Array.Resize(ref solute_owners, newSize);

            Array.Resize(ref SWIMSolTime, newSize);
            Array.Resize(ref SWIMSolAmt, newSize);
            Array.Resize(ref TD_soldrain, newSize);
            Array.Resize(ref TD_slssof, newSize);
            Array.Resize(ref slsur, newSize);
            Array.Resize(ref cslsur, newSize);
            Array.Resize(ref rslon, newSize);
            Array.Resize(ref rsloff, newSize);
            Array.Resize(ref rslex, newSize);
            Array.Resize(ref qslbp, newSize);
            Array.Resize(ref cslgw, newSize);
            Array.Resize(ref slupf, newSize);
            Array.Resize(ref slsci, newSize);
            Array.Resize(ref slscr, newSize);
            Array.Resize(ref dcon, newSize);
            Array.Resize(ref slos, newSize);
            Array.Resize(ref d0, newSize);

            Array.Resize(ref TD_sflow, newSize);
            Array.Resize(ref dc, newSize);
            Array.Resize(ref csl, newSize);
            Array.Resize(ref cslt, newSize);
            Array.Resize(ref qsl, newSize);
            Array.Resize(ref qsls, newSize);
            Array.Resize(ref cslold, newSize);
            Array.Resize(ref cslstart, newSize);
            Array.Resize(ref ex, newSize);
            Array.Resize(ref fip, newSize);
            Array.Resize(ref exco, newSize);

            Array.Resize(ref leach_id, newSize);
            Array.Resize(ref flow_id, newSize);
            Array.Resize(ref exco_id, newSize);
            Array.Resize(ref conc_water_id, newSize);
            Array.Resize(ref conc_adsorb_id, newSize);
            Array.Resize(ref subsurface_drain_id, newSize);

            Array.Resize(ref psuptake, newSize); // 0->num_crops, 0->n

            for (int idx = oldSize; idx < newSize; idx++)
            {
                Array.Resize(ref TD_sflow[idx], n + 2);
                Array.Resize(ref dc[idx], n + 1);
                Array.Resize(ref csl[idx], n + 2);
                Array.Resize(ref cslt[idx], n + 1);
                Array.Resize(ref qsl[idx], n + 2);
                Array.Resize(ref qsls[idx], n + 1);
                Array.Resize(ref cslold[idx], n + 1);
                Array.Resize(ref cslstart[idx], n + 1);
                Array.Resize(ref ex[idx], n + 1);
                Array.Resize(ref fip[idx], n + 1);
                Array.Resize(ref exco[idx], n + 1);
                SWIMSolTime[idx] = new double[0];
                SWIMSolAmt[idx] = new double[0];

                Array.Resize(ref psuptake[idx], num_crops);
                for (int crop = 0; crop < num_crops; crop++)
                    Array.Resize(ref psuptake[idx][crop], n + 1);
            }

            for (int i = 0; i < num_crops; i++)
            {
                Array.Resize(ref demand_is_met[i], newSize);
                Array.Resize(ref solute_demand[i], newSize);
                Array.Resize(ref supply_solute_id[i], newSize);
            }
            num_solutes = newSize;
        }

        /// <summary>
        /// Gets the value of residue cover
        /// </summary>
        /// <exception cref="System.Exception">Value for residue_cover outside the expected range of 0 to 1</exception>
        private void GetResidueVariables()
        {
            //+   Purpose
            //      Get the values of residue variables from other modules
            if (!Double.IsNaN(surfaceom_cover))
            {
                residue_cover = surfaceom_cover;
                if (residue_cover < 0.0 || residue_cover > 1.0)
                    throw new Exception("Value for residue_cover outside the expected range of 0 to 1");
            }
            else
                residue_cover = 0.0;
        }

        /// <summary>
        /// Removes intercepted rainfall
        /// </summary>
        private void RemoveInterception()
        {
            double intercep = 0.0;
            double residueIntercep = 0.0;
            if (!Double.IsNaN(interception))
                intercep = interception;
            if (!Double.IsNaN(residueinterception))
                residueIntercep = residueinterception;
            if (intercep < 0.0 || residueIntercep < 0.0 || intercep > 1000.0 || residueIntercep > 1000.0)
                IssueWarning("Interception value out of bounds");
            RemoveFromRainfall(intercep + residueIntercep);
        }

        /// <summary>
        /// Reduces the amount of rainfall (e.g., because of interception)
        /// </summary>
        /// <param name="amount">Amount of rainfall to remove (mm)</param>
        /// <exception cref="System.Exception">Interception > Rainfall</exception>
        private void RemoveFromRainfall(double amount)
        {
            if (amount > 0.0)
            {
                // Firstly, find the record for start of rainfall for the
                // current day - ie assume interception cannot come from
                // rainfall that started before the current day.

                int time_mins = TimeToMins(apsim_time);
                double start_timestep = Time(year, day, time_mins);
                int start = 0;

                for (int counter = 0; counter < SWIMRainTime.Length; counter++)
                {
                    if (SWIMRainTime[counter] >= start_timestep)
                    {
                        // we have found the first record for the current timestep
                        start = counter;
                        break;
                    }
                }

                // Assume that interception is taken over all rainfall
                // information given thus far - can do nothing better than this

                double tot_rain = SWIMRainAmt[SWIMRainAmt.Length - 1] - SWIMRainAmt[start];

                double fraction = MathUtilities.Divide(amount, tot_rain, 1e6);
                if (fraction > 1.0)
                    throw new Exception("Interception > Rainfall");
                else
                    for (int counter = start + 1; counter < SWIMRainAmt.Length; counter++)
                    {
                        SWIMRainAmt[counter] = SWIMRainAmt[start] + (SWIMRainAmt[counter] - SWIMRainAmt[start]) * (1.0 - fraction);
                    }
            }
        }

        /// <summary>
        /// Sets up calculation of evaporation variables
        /// </summary>
        /// <exception cref="System.Exception">
        /// apswim can only calculate Eo for daily timestep
        /// or
        /// Failure to supply eo duration data
        /// </exception>
        private void CalcEvapVariables()
        {
            if (!MathUtilities.FloatsAreEqual(apsim_timestep, 1440.0))
                throw new Exception("apswim can only calculate Eo for daily timestep");

            string time;
            double duration = 0.0;
            if (string.IsNullOrWhiteSpace(eo_time))
            {
                time = default_evap_time;
                duration = default_evap_duration;
            }
            else
            {
                time = eo_time;
                if (Double.IsNaN(eo_durn))
                    throw new Exception("Failure to supply eo duration data");
                duration = eo_durn;
            }
            if (duration < 0.0 || duration > 1440.0 * 30)
                IssueWarning("Value for eo duration outside expected range");

            int timeOfDay = TimeToMins(time);
            double timeMins = Time(year, day, timeOfDay);

            _cover_green_sum = GetGreenCover();
            double amount = PotEvapotranspiration();

            InsertLoginfo(timeMins, duration, amount, ref SWIMEvapTime, ref SWIMEvapAmt);
        }

        /// <summary>
        /// Gets total green cover
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception">cover_green_sum outside the expected range of 0 to 1</exception>
        private double GetGreenCover()
        {
            if (!Double.IsNaN(cover_green_sum))
            {
                if (cover_green_sum < 0.0 || cover_green_sum > 1.0)
                    throw new Exception("cover_green_sum outside the expected range of 0 to 1");
                return cover_green_sum;
            }
            else
            {
                double bare = 1.0;

                double[] cover_green = new double[0];
                int NumberOfCrops = 0;

                List<IModel> models = Apsim.FindAll(paddock, typeof(ICanopy));

                foreach (Model m in models)
                {
                    Array.Resize(ref cover_green, NumberOfCrops + 1);
                    Array.Resize(ref cover_tot, NumberOfCrops + 1);
                    Array.Resize(ref canopy_height, NumberOfCrops + 1);

                    ICanopy Canopy = m as ICanopy;
                    cover_green[NumberOfCrops] = Canopy.CoverGreen;
                    cover_tot[NumberOfCrops] = Canopy.CoverTotal;
                    canopy_height[NumberOfCrops] = Canopy.Height;
                    NumberOfCrops += 1;
                }

                List<IModel> models2 = Apsim.FindAll(paddock, typeof(ICrop2));

                foreach (Model m in models2)
                {
                    Array.Resize(ref cover_green, NumberOfCrops + 1);
                    Array.Resize(ref cover_tot, NumberOfCrops + 1);
                    Array.Resize(ref canopy_height, NumberOfCrops + 1);

                    ICrop2 Crop2 = m as ICrop2;
                    if (Crop2.CanopyProperties != null)
                    {
                        cover_green[NumberOfCrops] = Crop2.CanopyProperties.CoverGreen;
                        cover_tot[NumberOfCrops] = Crop2.CanopyProperties.CoverTot;
                        canopy_height[NumberOfCrops] = Crop2.CanopyProperties.CanopyHeight;
                    }
                    else
                    {
                        cover_green[NumberOfCrops] = 0;
                        cover_tot[NumberOfCrops] = 0;
                        canopy_height[NumberOfCrops] = 0;
                    }
                    NumberOfCrops += 1;
                }

                /////                foreach (Component Comp in paddock.Crops)
                /////                {
                ////                    Double coverGreen;
                ////                    bool foundCG = Paddock.Get(Comp.FullName + ".cover_green", out coverGreen);

                // Note - this is based on a reduction of Beers law
                // cover1+cover2 = 1 - exp (-(k1*lai1 + k2*lai2))
                //if (foundCG)
                foreach (double cov in cover_green)
                {
                    bare = bare * (1.0 - cov);
                }
                return 1.0 - bare;
            }
        }

        /// <summary>
        /// Calculates potential evapotranspiration
        /// </summary>
        /// <returns></returns>
        private double PotEvapotranspiration()
        {
            // ******* calculate potential evaporation from soil surface (eos) ******

            // find equilibrium evap rate as a
            // function of radiation, albedo, and temp.
            double surface_albedo = salb + (residue_albedo - salb) * residue_cover;
            double albedo = max_albedo - (max_albedo - surface_albedo) * (1.0 - _cover_green_sum);
            // wt_ave_temp is mean temp, weighted towards max.
            double wt_ave_temp = 0.6 * maxt + 0.4 * mint;

            double eeq = radn * 23.8846 * (0.000204 - 0.000183 * albedo) * (wt_ave_temp + 29.0);
            // find potential evapotranspiration (pot_eo)
            // from equilibrium evap rate
            return eeq * EeqFac();
        }

        /// <summary>
        /// Calculates coefficient for equilibrium evaporation rate
        /// </summary>
        /// <returns></returns>
        private double EeqFac()
        {
            //+  Purpose
            //                 calculate coefficient for equilibrium evaporation rate
            if (maxt > max_crit_temp)
            {
                // at very high max temps eo/eeq increases
                // beyond its normal value of 1.1
                return (maxt - max_crit_temp) * 0.05 + 1.1;
            }
            else if (maxt < min_crit_temp)
            {
                // at very low max temperatures eo/eeq
                // decreases below its normal value of 1.1
                // note that there is a discontinuity at tmax = 5
                // it would be better at tmax = 6.1, or change the
                // .18 to .188 or change the 20 to 21.1
                return 0.01 * Math.Exp(0.18 * (maxt + 20.0));
            }
            else
            {
                // temperature is in the normal range, eo/eeq = 1.1
                return 1.1;
            }
        }

        /// <summary>
        /// Current does nothing!
        /// </summary>
        private void SetOtherVariables()
        {
            // Does nothing!
        }

        /// <summary>
        /// Sets the values of solute variables from other modules
        /// </summary>
        /// <exception cref="System.Exception">
        /// -ve concentration in apswim_set_solute_variables + Environment.NewLine + mess
        /// or
        /// -ve value for solute concentration + Environment.NewLine + mess
        /// </exception>
        private void SetSoluteVariables()
        {
            //+  Purpose
            //      Set the values of solute variables from other modules

            //+  Changes
            //   21-6-96 NIH - Changed set_double_array to post construct
            //   RCichota - 26/01/2010 - Add test to make sure SWIM will not send a -ve value
            //   RCichota - 12/Jul/2010 - add simple test for -ve solution concentration

            NitrogenChangedType ndata = new NitrogenChangedType();
            double[] solute_n = new double[n + 1];     // solute concn in layers(kg/ha)
            double[] dlt_solute_s = new double[n + 1]; // solute concn in layers(kg/ha)

            // initialise the NitrogenChanged data to zero
            ndata.DeltaUrea = new double[n + 1];
            ndata.DeltaNH4 = new double[n + 1];
            ndata.DeltaNO3 = new double[n + 1];

            for (int solnum = 0; solnum < num_solutes; solnum++)
            {
                for (int node = 0; node <= n; node++)
                {
                    // Step One - calculate total solute in node from solute in
                    // water and Freundlich isotherm.

                    if (csl[solnum][node] < 0.0)
                    {
                        string mess = String.Format(" solution {0}({1,3}) = {2,12:G6}",
                                     solute_names[solnum],
                                     node,
                                     csl[solnum][node]);
                        throw new Exception("-ve concentration in apswim_set_solute_variables" + Environment.NewLine + mess);
                    }
                    double Ctot, dCtot;
                    Freundlich(node, solnum, ref csl[solnum][node], out Ctot, out dCtot);

                    // Note:- Sometimes small numerical errors can leave
                    // -ve concentrations. Test if values are within limits.

                    if (Math.Abs(Ctot) < 1e-100)
                    {
                        // Ctot is REALLY small, its value can be disregarded
                        // set to zero to avoid underflow with reals

                        Ctot = 0.0;
                    }

                    else if (Ctot < 0.0)
                    {
                        // Ctot is negative and a fatal error is thrown. Should not happen as it has been tested on apswim_freundlich
                        string mess = String.Format(" Total {0}({1,3}) = {2,12:G6}",
                                            solute_names[solnum],
                                            node,
                                            Ctot);
                        throw new Exception("-ve value for solute concentration" + Environment.NewLine + mess);
                        // Ctot = 0.0;
                    }
                    //else Ctot is positive

                    // convert solute ug/cc soil to kg/ha for node
                    //
                    //  kg      ug        cc soil      kg
                    //  -- = -------- p%x -------- p%x --
                    //  ha    cc soil        ha        ug

                    Ctot = Ctot                   // ug/cc soil
                         * (dx[node] * 1.0e8)     // cc soil/ha
                         * 1e-9;                  // kg/ug

                    // finished testing - assign value to array element
                    solute_n[node] = Ctot;
                    dlt_solute_s[node] = Ctot - cslstart[solnum][node];
                }

                // Added by RCichota - using NitrogenChanged event to modify dlt_N's
                if (solute_names[solnum] == "urea")
                    Array.Copy(dlt_solute_s, ndata.DeltaUrea, n + 1);
                else if (solute_names[solnum] == "nh4")
                    Array.Copy(dlt_solute_s, ndata.DeltaNH4, n + 1);
                else if (solute_names[solnum] == "no3")
                    Array.Copy(dlt_solute_s, ndata.DeltaNO3, n + 1);
                else
                {
                    ///// TODO:
                    /////                    string compName = paddock.SiblingNameFromId(solute_owners[solnum]);
                    /////                    My.Set(compName + ".dlt_" + solute_names[solnum], dlt_solute_s);
                }
            }

            // Send a NitrogenChanged event to the system
            ndata.Sender = "SWIM";
            ndata.SenderType = "WaterModule";
            NitrogenChanged.Invoke(ndata);
        }

        /// <summary>
        /// Publishes uptake values
        /// </summary>
        private void PublishUptakes()
        {
            CohortWaterSupplyType Supply = new CohortWaterSupplyType();
            PMF.WaterUptakesCalculatedType Water = new PMF.WaterUptakesCalculatedType();
            Water.Uptakes = new PMF.WaterUptakesCalculatedUptakesType[num_crops];

            for (int counter = 0; counter < num_crops; counter++)
            {
                Water.Uptakes[counter] = new PMF.WaterUptakesCalculatedUptakesType();
                if (supply_event_id[counter] != 0)
                {
                    Supply.CohortID = crop_names[counter];
                    Supply.RootSystemLayer = new CohortWaterSupplyRootSystemLayerType[n + 1];
                    double bottom = 0.0;
                    for (int node = 0; node <= n; node++)
                    {
                        // This uses "bottom", not "dlayer", so we need to convert
                        bottom += _dlayer[node];
                        Supply.RootSystemLayer[node].Bottom = bottom;

                        // uptake may be very small -ve - assume error small
                        Supply.RootSystemLayer[node].Supply = Math.Max(pwuptake[counter][node], 0.0);
                    }
                    CohortWaterSupply.Invoke(Supply);
                }

                string cropname;
                if (supply_event_id[counter] == 0)
                    cropname = crop_owners[counter];
                else
                    cropname = crop_owners[counter] + "_" + crop_names[counter];

                Water.Uptakes[counter].Name = cropname;
                Water.Uptakes[counter].Amount = new double[n + 1];

                for (int node = 0; node <= n; node++)
                {
                    // uptake may be very small -ve - assume error small
                    Water.Uptakes[counter].Amount[node] = Math.Max(pwuptake[counter][node], 0.0);
                }
            }

            WaterUptakesCalculated.Invoke(Water);
        }

        /// <summary>
        /// Get evaporation values from other modules
        /// </summary>
        /// <exception cref="System.Exception">Failure to supply eo duration data</exception>
        private void GetObsEvapVariables()
        {
            //  Purpose
            //      Get the evap values from other modules
            double amount = 0.0;

            ////// TODO
            //////if (!My.Get(evap_source, out amount))
            //////    throw new Exception("No module provided Eo value for APSwim");

            //////if (amount < 0.0 || amount > 1000.0)
            //////    IssueWarning("Value for evaporation outside expected range");

            string time;
            double duration = 0.0;
            if (String.IsNullOrWhiteSpace(eo_time))
            {
                time = default_evap_time;
                duration = default_evap_duration;
            }
            else
            {
                time = eo_time;
                if (Double.IsNaN(eo_durn))
                    throw new Exception("Failure to supply eo duration data");
                duration = eo_durn;
            }
            if (duration < 0.0 || duration > 1440.0 * 30)
                IssueWarning("Value for eo duration outside expected range");

            int timeOfDay = TimeToMins(time);
            double timeMins = Time(year, day, timeOfDay);
            InsertLoginfo(timeMins, duration, amount, ref SWIMEvapTime, ref SWIMEvapAmt);
        }

        /// <summary>
        /// Recalcs the eqrain.
        /// </summary>
        private void RecalcEqrain()
        {
            Array.Resize(ref SWIMEqRainTime, SWIMRainTime.Length);
            Array.Resize(ref SWIMEqRainAmt, SWIMRainAmt.Length);
            // leave the first element alone to keep magnitude in order
            for (int counter = 1; counter < SWIMRainAmt.Length; counter++)
            {
                double eqrain = 0.0;
                double amount = (SWIMRainAmt[counter] - SWIMRainAmt[counter - 1]) / 10.0;
                double duration = SWIMRainTime[counter] - SWIMRainTime[counter - 1];
                double avinten = MathUtilities.Divide(amount, duration, 0.0);

                if (avinten > 0.0)
                    eqrain = (1.0 + effpar * Math.Log(avinten / 2.5)) * amount;

                SWIMEqRainTime[counter] = SWIMRainTime[counter];
                SWIMEqRainAmt[counter] = SWIMRainAmt[counter] + eqrain;
            }
        }

        /// <summary>
        /// Converts an hours:minutes time string to a number of minutes
        /// </summary>
        /// <param name="timeString">The time string.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">bad time format</exception>
        private int TimeToMins(string timeString)
        {
            DateTime timeValue;
            if (!DateTime.TryParseExact(timeString, "H:m", new CultureInfo("en-AU"), DateTimeStyles.AllowWhiteSpaces, out timeValue))
                throw new Exception("bad time format");
            return timeValue.Hour * 60 + timeValue.Minute;
        }

        /// <summary>
        /// Cns the runoff.
        /// </summary>
        private void CNRunoff()
        {
            CoverSurfaceRunoff(ref _cover_surface_runoff);

            double startOfDay = Time(year, day, TimeToMins(apsim_time));
            double endOfDay = Time(year, day, TimeToMins(apsim_time) + (int)apsim_timestep);

            double rain = (CRain(endOfDay) - CRain(startOfDay)) * 10.0;

            SCSRunoff(rain, out CN_runoff);
            RemoveFromRainfall(CN_runoff);
            TD_runoff += CN_runoff;
        }

        /// <summary>
        /// Covers the surface runoff.
        /// </summary>
        /// <param name="coverSurfaceRunoff">The cover surface runoff.</param>
        private void CoverSurfaceRunoff(ref double coverSurfaceRunoff)
        {
            // cover cn response from perfect   - ML  & dms 7-7-95
            // nb. perfect assumed crop canopy was 1/2 effect of mulch
            // This allows the taller canopies to have less effect on runoff
            // and the cover close to ground to have full effect (jngh)

            //! weight effectiveness of crop canopies
            //    0 (no effect) to 1 (full effect)

            double cover_surface_crop = 0.0;
            double canopyFact;
            for (int crop = 0; crop < num_crops; crop++)
            {
                if (canopy_height[crop] >= 0.0)
                {
                    bool didInterp;
                    canopyFact = MathUtilities.LinearInterpReal(canopy_height[crop], canopy_fact_height, canopy_fact, out didInterp);
                }
                else
                {
                    canopyFact = canopy_fact_default;
                }

                double effective_crop_cover = cover_tot[crop] * canopyFact;
                cover_surface_crop = AddCover(cover_surface_crop, effective_crop_cover);
            }

            // add cover known to affect runoff
            //!    ie residue with canopy shading residue

            coverSurfaceRunoff = AddCover(cover_surface_crop, residue_cover);
        }

        /// <summary>
        /// Combines cover values
        /// </summary>
        /// <param name="cover1">The cover1.</param>
        /// <param name="cover2">The cover2.</param>
        /// <returns></returns>
        private double AddCover(double cover1, double cover2)
        {
            //!+ Sub-Program Arguments
            //   real       cover1                ! (INPUT) first cover to combine (0-1)
            //   real       cover2                ! (INPUT) second cover to combine (0-1)

            //!+ Purpose
            //!     Combines two covers

            //!+  Definition
            //!     "cover1" and "cover2" are numbers between 0 and 1 which
            //!     indicate what fraction of sunlight is intercepted by the
            //!     foliage of plants.  This function returns a number between
            //!     0 and 1 indicating the fraction of sunlight intercepted
            //!     when "cover1" is combined with "cover2", i.e. both sets of
            //!     plants are present.

            //!+  Mission Statement
            //!     cover as a result of %1 and %2

            double bare;     //! bare proportion (0-1)

            bare = (1.0 - cover1) * (1.0 - cover2);
            return (1.0 - bare);

        }

        /// <summary>
        /// Calculates runoff using SCS curve number method
        /// </summary>
        /// <param name="rain">The rain.</param>
        /// <param name="runoff">The runoff.</param>
        private void SCSRunoff(double rain, out double runoff)
        {
            //+  Purpose
            //        calculate runoff using scs curve number method

            // revision of the runoff calculation according to scs curve number
            // cnpd  : fractional avail. soil water weighted over the
            //         hyd.eff. depth  <dms 7-7-95>
            // cn1   : curve number for dry soil
            // cn3   : curve number for wet soil
            // s     : s value from scs equation, transfer to mm scale
            //         = max. pot. retention (~infiltration) (mm)

            // check if hydro_effective_depth applies for eroded profile.

            double[] runoff_wf = new double[n + 1];
            RunoffDepthFactor(out runoff_wf);

            double cnpd = 0.0;
            for (int layer = 0; layer <= n; layer++)
            {
                cnpd = cnpd + (th[layer] - _ll15[layer]) / (_dul[layer] - _ll15[layer]) * runoff_wf[layer];
            }
            cnpd = MathUtilities.Bound(cnpd, 0.0, 1.0);

            // reduce CN2 for the day due to cover effect

            double cover_fract = MathUtilities.Divide(cover_surface_runoff, cn_cov, 0.0);
            cover_fract = MathUtilities.Bound(cover_fract, 0.0, 1.0);

            double cn2_new = cn2_bare - (cn_red * cover_fract);

            cn2_new = MathUtilities.Bound(cn2_new, 0.0, 100.0);

            double cn1 = MathUtilities.Divide(cn2_new, (2.334 - 0.01334 * cn2_new), 0.0);
            double cn3 = MathUtilities.Divide(cn2_new, (0.4036 + 0.005964 * cn2_new), 0.0);
            double cn = cn1 + (cn3 - cn1) * cnpd;

            //! curve number will be decided from scs curve number table ??dms

            double s = 254.0 * (MathUtilities.Divide(100.0, cn, 1000000.0) - 1.0);
            double xpb = rain - 0.2 * s;
            xpb = Math.Max(xpb, 0.0);

            runoff = (xpb * xpb) / (rain + 0.8 * s);
        }

        /// <summary>
        /// Calculates the weighting factor hydraulic effectiveness used
        /// to weight the effect of soil moisture on runoff.
        /// </summary>
        /// <param name="runoff_wf">The runoff_wf.</param>
        private void RunoffDepthFactor(out double[] runoff_wf)
        {
            //+  Purpose
            //      Calculate the weighting factor hydraulic effectiveness used
            //      to weight the effect of soil moisture on runoff.

            //+  Local Variables
            double profile_depth;          // current depth of soil profile
            // - for when erosion turned on
            double cum_depth;              // cumulative depth (mm)
            double hydrolEffectiveDepth; // hydrologically effective depth for runoff (mm)
            int hydrolEffectiveLayer; // layer number that the effective depth occurs in ()
            int layer;                  // layer counter

            double scale_fact;             // scaling factor for wf function to sum to 1
            double wf_tot;                 // total of wf ()
            double wx;                     //. depth weighting factor for current
            //    total depth.
            //    intermediate variable for
            //    deriving wf
            //    (total wfs to current layer)
            double xx;                     // intermediate variable for deriving wf
            // total wfs to previous layer

            runoff_wf = new double[n + 1];
            xx = 0.0;
            cum_depth = 0.0;
            wf_tot = 0.0;

            // check if hydro_effective_depth applies for eroded profile.

            profile_depth = MathUtilities.Sum(_dlayer);

            hydrolEffectiveDepth = Math.Min(hydrol_effective_depth, profile_depth);

            scale_fact = 1.0 / (1.0 - Math.Exp(-4.16));
            hydrolEffectiveLayer = FindSwimLayer(hydrolEffectiveDepth);

            for (layer = 0; layer <= hydrolEffectiveLayer; layer++)
            {
                cum_depth += _dlayer[layer];
                cum_depth = Math.Min(cum_depth, hydrolEffectiveDepth);

                // assume water content to c%hydrol_effective_depth affects runoff
                // sum of wf should = 1 - may need to be bounded? <dms 7-7-95>

                wx = scale_fact * (1.0 - Math.Exp(-4.16 * cum_depth / hydrolEffectiveDepth));
                runoff_wf[layer] = wx - xx;
                xx = wx;

                wf_tot = wf_tot + runoff_wf[layer];
            }

            if (wf_tot < 0.9999 || wf_tot > 1.0001)
                IssueWarning("The value for 'wf_tot' of " + wf_tot.ToString() + " is not near the expected value of 1.0");
        }

        /// <summary>
        /// Returns the layer number for a depth within the soil profile
        /// </summary>
        /// <param name="depth">The depth.</param>
        /// <returns></returns>
        private int FindSwimLayer(double depth)
        {
            double cumDepth = 0.0;
            for (int i = 0; i <= n; i++)
            {
                cumDepth += _dlayer[i];
                if (cumDepth > depth)
                    return i;
            }
            return n;
        }

        /// <summary>
        /// Times the specified yy.
        /// </summary>
        /// <param name="yy">The yy.</param>
        /// <param name="dd">The dd.</param>
        /// <param name="tt">The tt.</param>
        /// <returns></returns>
        private double Time(int yy, int dd, int tt)
        {
            // first we must calculate the julian date for the starting date.
            // We will calculate time relative to this date.
            double beginStartYear = DateUtilities.DateTimeToJulianDayNumber(new DateTime(start_year, 1, 1)) - 1.0;
            double julianStartDate = beginStartYear + start_day - 1.0;

            // all times are relative to beginning of the day

            double beginYear = DateUtilities.DateTimeToJulianDayNumber(new DateTime(yy, 1, 1)) - 1.0;
            double julianDate = beginYear + dd - 1.0;

            return (julianDate - julianStartDate) * 24.0 + tt / 60.0; // Convert to hours
        }

        /// <summary>
        /// Purges the log information.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="SWIMTime">The swim time.</param>
        /// <param name="SWIMAmt">The swim amt.</param>
        private void PurgeLogInfo(double time, ref double[] SWIMTime, ref double[] SWIMAmt)
        {
            int old_numpairs = SWIMTime.Length;
            int new_start = 0;

            for (int counter = old_numpairs - 1; counter >= 0; counter--)
            {
                if (SWIMTime[counter] <= time)
                {
                    // we have found the oldest record we need to keep
                    new_start = counter;
                    break;
                }
            }

            int new_index = 0;
            for (int counter = new_start; counter < old_numpairs - 1; counter++)
            {
                new_index++;
                SWIMTime[new_index] = SWIMTime[counter];
                SWIMAmt[new_index] = SWIMAmt[counter];
            }
            Array.Resize(ref SWIMTime, new_index);
            Array.Resize(ref SWIMAmt, new_index);
        }

        /// <summary>
        /// Inserts the loginfo.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="SWIMTime">The swim time.</param>
        /// <param name="SWIMAmt">The swim amt.</param>
        /// <exception cref="System.Exception">log time before start of run</exception>
        private void InsertLoginfo(double time,     // min since start
                                   double duration, // min
                                   double amount,   // mm
                                   ref double[] SWIMTime,
                                   ref double[] SWIMAmt)
        {
            bool inserted = false;
            double SAmt = 0.0;
            double FAmt = 0.0;
            double FTime = time + duration / 60.0;
            if (SWIMTime.Length > 0)
            {
                if (time < SWIMTime[0])
                    throw new Exception("log time before start of run");

                SAmt = MathUtilities.LinearInterpReal(time, SWIMTime, SWIMAmt, out inserted);
                FAmt = MathUtilities.LinearInterpReal(FTime, SWIMTime, SWIMAmt, out inserted);

                // Insert starting element placeholder into log
                for (int counter = 0; counter < SWIMTime.Length; counter++)
                {
                    if (MathUtilities.FloatsAreEqual(SWIMTime[counter], time))
                    {
                        inserted = true;
                        break;  // There is already a placeholder there
                    }
                    else if (SWIMTime[counter] > time)
                    {
                        Array.Resize(ref SWIMTime, SWIMTime.Length + 1);
                        Array.Resize(ref SWIMAmt, SWIMAmt.Length + 1);
                        for (int counter2 = SWIMTime.Length - 1; counter2 > counter; counter2--)
                        {
                            SWIMTime[counter2] = SWIMTime[counter2 - 1];
                            SWIMAmt[counter2] = SWIMAmt[counter2 - 1];
                        }
                        SWIMTime[counter] = time;
                        SWIMAmt[counter] = SAmt;
                        inserted = true;
                        break;
                    }
                }
            }
            if (!inserted)
            {
                // time > last log entry
                Array.Resize(ref SWIMTime, SWIMTime.Length + 1);
                Array.Resize(ref SWIMAmt, SWIMAmt.Length + 1);
                SWIMTime[SWIMTime.Length - 1] = time;
                SWIMAmt[SWIMAmt.Length - 1] = SAmt;
            }

            // Insert ending element placeholder into log
            inserted = false;
            for (int counter = 0; counter < SWIMTime.Length; counter++)
            {
                if (MathUtilities.FloatsAreEqual(SWIMTime[counter], FTime))
                {
                    inserted = true;
                    break;  // There is already a placeholder there
                }
                else if (SWIMTime[counter] > FTime)
                {
                    Array.Resize(ref SWIMTime, SWIMTime.Length + 1);
                    Array.Resize(ref SWIMAmt, SWIMAmt.Length + 1);
                    for (int counter2 = SWIMTime.Length - 1; counter2 > counter; counter2--)
                    {
                        SWIMTime[counter2] = SWIMTime[counter2 - 1];
                        SWIMAmt[counter2] = SWIMAmt[counter2 - 1];
                    }
                    SWIMTime[counter] = FTime;
                    SWIMAmt[counter] = FAmt;
                    inserted = true;
                    break;
                }
            }
            if (!inserted)
            {
                // time > last log entry
                Array.Resize(ref SWIMTime, SWIMTime.Length + 1);
                Array.Resize(ref SWIMAmt, SWIMAmt.Length + 1);
                SWIMTime[SWIMTime.Length - 1] = FTime;
                SWIMAmt[SWIMAmt.Length - 1] = FAmt;
            }

            // Now add extra quantity to each log entry are required

            double avInt = amount / (duration / 60.0);

            for (int counter = 0; counter < SWIMTime.Length; counter++)
            {
                double extra = 0.0;
                if (SWIMTime[counter] > time)
                    extra = avInt * Math.Min(SWIMTime[counter] - time, duration / 60.0);
                SWIMAmt[counter] += extra;
            }
        }

        /// <summary>
        /// Resets daily accumlators to zero
        /// </summary>
        private void ResetDailyTotals()
        {
            TD_runoff = 0.0;
            TD_rain = 0.0;
            TD_evap = 0.0;
            TD_pevap = 0.0;
            TD_drain = 0.0;
            TD_subsurface_drain = 0.0;

            for (int sol = 0; sol < num_solutes; sol++)
            {
                TD_slssof[sol] = 0.0;
                TD_soldrain[sol] = 0.0;
                for (int node = 0; node <= n; node++)
                    TD_sflow[sol][node] = 0.0;
            }

            for (int node = 0; node <= n; node++)
            {
                TD_wflow[node] = 0.0;
                for (int vegnum = 0; vegnum < num_crops; vegnum++)
                {
                    for (int sol = 0; sol < num_solutes; sol++)
                    {
                        psuptake[sol][vegnum][node] = 0.0;
                    }
                    pwuptake[vegnum][node] = 0.0;
                    pwuptakepot[vegnum][node] = 0.0;
                    _psix[vegnum] = 0.0;
                }
            }
        }

        /// <summary>
        /// Sets up the theta curve
        /// </summary>
        private void SetupThetaCurve()
        {
            for (int layer = 0; layer <= n; layer++)
            {
                psid[layer] = psidul;  //- (p%x(p%n) - p%x(layer))

                DELk[layer, 0] = (_dul[layer] - _sat[layer]) / (Math.Log10(-psid[layer]));
                DELk[layer, 1] = (_ll15[layer] - _dul[layer]) / (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                DELk[layer, 2] = -_ll15[layer] / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                DELk[layer, 3] = -_ll15[layer] / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));

                Mk[layer, 0] = 0.0;
                Mk[layer, 1] = (DELk[layer, 0] + DELk[layer, 1]) / 2.0;
                Mk[layer, 2] = (DELk[layer, 1] + DELk[layer, 2]) / 2.0;
                Mk[layer, 3] = DELk[layer, 3];

                // First bit might not be monotonic so check and adjust
                double alpha = Mk[layer, 0] / DELk[layer, 0];
                double beta = Mk[layer, 1] / DELk[layer, 0];
                double phi = alpha - (Math.Pow(2.0 * alpha + beta - 3.0, 2.0) / (3.0 * (alpha + beta - 2.0)));
                if (phi <= 0)
                {
                    double tau = 3.0 / Math.Sqrt(alpha * alpha + beta * beta);
                    Mk[layer, 0] = tau * alpha * DELk[layer, 0];
                    Mk[layer, 1] = tau * beta * DELk[layer, 0];
                }

                M0[layer, 0] = 0.0;
                M1[layer, 0] = 0.0;
                Y0[layer, 0] = _sat[layer];
                Y1[layer, 0] = _sat[layer];

                M0[layer, 1] = Mk[layer, 0] * (Math.Log10(-psid[layer]) - 0.0);
                M1[layer, 1] = Mk[layer, 1] * (Math.Log10(-psid[layer]) - 0.0);
                Y0[layer, 1] = _sat[layer];
                Y1[layer, 1] = _dul[layer];

                M0[layer, 2] = Mk[layer, 1] * (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                M1[layer, 2] = Mk[layer, 2] * (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
                Y0[layer, 2] = _dul[layer];
                Y1[layer, 2] = _ll15[layer];

                M0[layer, 3] = Mk[layer, 2] * (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                M1[layer, 3] = Mk[layer, 3] * (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
                Y0[layer, 3] = _ll15[layer];
                Y1[layer, 3] = 0.0;

                M0[layer, 4] = 0.0;
                M1[layer, 4] = 0.0;
                Y0[layer, 4] = 0.0;
                Y1[layer, 4] = 0.0;
            }
        }

        /// <summary>
        /// Sets up the K curve
        /// </summary>
        private void SetupKCurve()
        {
            for (int layer = 0; layer <= n; layer++)
            {
                double b = -Math.Log(psidul / psi_ll15) / Math.Log(_dul[layer] / _ll15[layer]);
                MicroP[layer] = b * 2.0 + 3.0;
                Kdula[layer] = Math.Min(0.99 * kdul, _ks[layer]);
                MicroKs[layer] = Kdula[layer] / Math.Pow(_dul[layer] / _sat[layer], MicroP[layer]);

                double Sdul = _dul[layer] / _sat[layer];
                MacroP[layer] = Math.Log10(Kdula[layer] / 99.0 / (_ks[layer] - MicroKs[layer])) / Math.Log10(Sdul);
            }
        }

        /// <summary>
        /// Resets the water balance.
        /// </summary>
        /// <param name="wcFlag">The wc flag.</param>
        /// <param name="waterContent">Content of the water.</param>
        /// <exception cref="System.Exception">Bad wc_type flag value</exception>
        private void ResetWaterBalance(int wcFlag, ref double[] waterContent)
        {
            for (int i = 0; i <= n; i++)
            {
                if (wcFlag == 1)
                {
                    // water content was supplied in volumetric SW
                    // so calculate matric potential
                    th[i] = waterContent[i];
                    _psi[i] = Suction(i, th[i]);
                }
                else if (wcFlag == 2)
                {
                    // matric potential was supplied
                    // so calculate water content
                    _psi[i] = waterContent[i];
                    th[i] = Theta(i, _psi[i]);
                }
                else
                    throw new Exception("Bad wc_type flag value");
                _p[i] = Pf(_psi[i]);
            }
            _wp = Wpf();
        }

        /// <summary>
        /// cs the sol.
        /// </summary>
        /// <param name="solnum">The solnum.</param>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        private double CSol(int solnum, double time)
        {
            //  Purpose
            //        cumulative solute in ug/cm^2
            int numPairs = SWIMSolTime[solnum].Length;
            double[] SAmount = new double[numPairs];
            double[] STime = new double[numPairs];

            if (numPairs > 0)
            {
                for (int counter = 0; counter < numPairs; counter++)
                {
                    SAmount[counter] = SWIMSolAmt[solnum][counter];
                    STime[counter] = SWIMSolTime[solnum][counter];
                }

                // Solute arrays are in kg/ha of added solute.  From swim's equations
                // with everything in cm and ug per g water we convert the output to
                // ug per cm^2 because the cm^2 area and height in cm gives g water.
                // There are 10^9 ug/kg and 10^8 cm^2 per ha therefore we get a
                // conversion factor of 10.

                bool interp;
                return MathUtilities.LinearInterpReal(time, STime, SAmount, out interp) * 10.0;
            }
            else
                return 0.0;
        }

        /// <summary>
        /// cs the rain.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        private double CRain(double time)
        {
            bool interp;
            if (SWIMRainTime.Length > 0)
                return MathUtilities.LinearInterpReal(time, SWIMRainTime, SWIMRainAmt, out interp) / 10.0;
            else
                return 0.0;
        }

        /// <summary>
        /// cs the evap.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        private double CEvap(double time)
        {
            bool interp;
            if (SWIMEvapTime.Length > 0)
                return MathUtilities.LinearInterpReal(time, SWIMEvapTime, SWIMEvapAmt, out interp) / 10.0;
            else
                return 0.0;
        }

        /// <summary>
        /// Eqrains the specified time.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        private double Eqrain(double time)
        {
            bool interp;
            if (SWIMEqRainTime.Length > 0)
                return MathUtilities.LinearInterpReal(time, SWIMEqRainTime, SWIMEqRainAmt, out interp);
            else
                return 0.0;
        }

        /// <summary>
        /// Calculates depth of water table from soil surface
        /// </summary>
        /// <returns></returns>
        private double WaterTable()
        {
            //   Purpose
            //      Calculate depth of water table from soil surface
            for (int i = 0; i <= n; i++)
            {
                if (_psi[i] > 0)
                    return (x[i] - _psi[i]) * 10.0;
            }
            // set default value to bottom of soil profile.
            return x[n] * 10.0;
        }

        /// <summary>
        /// Suctions the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="theta">The theta.</param>
        /// <returns></returns>
        private double Suction(int node, double theta)
        {
            //  Purpose
            //   Calculate the suction for a given water content for a given node.
            const int maxIterations = 1000;
            const double tolerance = 1e-9;
            const double dpsi = 0.01;

            if (theta == _sat[node])
                return 0.0;
            else
            {
                double psiValue = -100.0; // Initial estimate
                for (int iter = 0; iter < maxIterations; iter++)
                {
                    double est = SimpleTheta(node, psiValue);
                    double m = (SimpleTheta(node, psiValue + dpsi) - est) / dpsi;

                    if (Math.Abs(est - theta) < tolerance)
                        break;
                    psiValue -= (est - theta) / m;
                }
                return psiValue;
            }
        }

        /// <summary>
        /// Simples the s.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <param name="psiValue">The psi value.</param>
        /// <returns></returns>
        private double SimpleS(int layer, double psiValue)
        {
            //  Purpose
            //      Calculate S for a given node for a specified suction.
            return SimpleTheta(layer, psiValue) / _sat[layer];
        }

        /// <summary>
        /// Simples the theta.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <param name="psiValue">The psi value.</param>
        /// <returns></returns>
        private double SimpleTheta(int layer, double psiValue)
        {
            //  Purpose
            //     Calculate Theta for a given node for a specified suction.
            int i;
            double t;

            if (psiValue >= -1.0)
            {
                i = 0;
                t = 0.0;
            }
            else if (psiValue > psid[layer])
            {
                i = 1;
                t = (Math.Log10(-psiValue) - 0.0) / (Math.Log10(-psid[layer]) - 0.0);
            }
            else if (psiValue > psi_ll15)
            {
                i = 2;
                t = (Math.Log10(-psiValue) - Math.Log10(-psid[layer])) / (Math.Log10(-psi_ll15) - Math.Log10(-psid[layer]));
            }
            else if (psiValue > psi0)
            {
                i = 3;
                t = (Math.Log10(-psiValue) - Math.Log10(-psi_ll15)) / (Math.Log10(-psi0) - Math.Log10(-psi_ll15));
            }
            else
            {
                i = 4;
                t = 0.0;
            }

            double tSqr = t * t;
            double tCube = tSqr * t;

            return (2 * tCube - 3 * tSqr + 1) * Y0[layer, i] + (tCube - 2 * tSqr + t) * M0[layer, i]
                    + (-2 * tCube + 3 * tSqr) * Y1[layer, i] + (tCube - tSqr) * M1[layer, i];
        }

        /// <summary>
        /// Interps the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="tpsi">The tpsi.</param>
        /// <param name="tth">The TTH.</param>
        /// <param name="thd">The THD.</param>
        /// <param name="hklg">The HKLG.</param>
        /// <param name="hklgd">The HKLGD.</param>
        private void Interp(int node, double tpsi, out double tth, out double thd, out double hklg, out double hklgd)
        {
            //  Purpose
            //   interpolate water characteristics for given potential for a given
            //   node.

            const double dpsi = 0.0001;
            double temp;

            tth = SimpleTheta(node, tpsi);
            temp = SimpleTheta(node, tpsi + dpsi);
            thd = (temp - tth) / Math.Log10((tpsi + dpsi) / tpsi);
            hklg = Math.Log10(SimpleK(node, tpsi));
            temp = Math.Log10(SimpleK(node, tpsi + dpsi));
            hklgd = (temp - hklg) / Math.Log10((tpsi + dpsi) / tpsi);
        }

        /// <summary>
        /// Simples the k.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <param name="psiValue">The psi value.</param>
        /// <returns></returns>
        private double SimpleK(int layer, double psiValue)
        {
            //  Purpose
            //      Calculate Conductivity for a given node for a specified suction.

            double S = SimpleS(layer, psiValue);
            double simpleK;

            if (S <= 0.0)
                simpleK = 1e-100;
            else
            {
                double microK = MicroKs[layer] * Math.Pow(S, MicroP[layer]);

                if (MicroKs[layer] >= _ks[layer])
                    simpleK = microK;
                else
                {
                    double macroK = (_ks[layer] - MicroKs[layer]) * Math.Pow(S, MacroP[layer]);
                    simpleK = microK + macroK;
                }
            }
            return simpleK / 24.0 / 10.0;
        }

        /// <summary>
        /// Thetas the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="suction">The suction.</param>
        /// <returns></returns>
        private double Theta(int node, double suction)
        {
            double theta;
            double thd;
            double hklg;
            double hklgd;

            Interp(node, suction, out theta, out thd, out hklg, out hklgd);
            return theta;
        }

        /// <summary>
        /// Does the swim.
        /// </summary>
        /// <param name="timestepStart">The timestep start.</param>
        /// <param name="timestep">The timestep.</param>
        /// <returns></returns>
        private bool DoSwim(double timestepStart, double timestep)
        {
            //  Notes
            //     SWIM solves Richards' equation for one dimensional vertical soil water
            //     infiltration and movement.  A surface seal, variable height of surface
            //     ponding, and variable runoff rates are optional.  Deep drainage occurs
            //     under a given matric potential gradient or given potl or zero flux or
            //     seepage.  The method uses a fixed space grid and a sinh transform of
            //     the matric potential, as reported in :
            //     Ross, P.J., 1990.  Efficient numerical methods for infiltration using
            //     Richards' equation.  Water Resources Res. 26, 279-290.

            double timestepRemaining = timestep;
            t = timestepStart;
            bool fail = false;
            int itlim;
            double qmax;

            //     define iteration limit for soln of balance eqns
            if (run_has_started)
            {
                // itlim = 20;
                itlim = max_iterations;
            }
            else
            {
                // this is our first timestep - allow for initial stabilisation
                // itlim = 50;
                itlim = max_iterations + 20;
            }

            //     solve until end of time step
            do
            {
                double dr;
                // call event_send(unknown_module,'swim_timestep_preparation')

                //        calculate next step size_of g%dt

                // Start with first guess as largest size_of possible
                _dt = dtmax;
                if (MathUtilities.FloatsAreEqual(dtmin, dtmax))
                    _dt = dtmin;
                else
                {
                    if (!run_has_started)
                    {
                        if (MathUtilities.FloatsAreEqual(dtmin, 0.0))
                            _dt = Math.Min(0.01 * (timestepRemaining), 0.25);
                        else
                            _dt = dtmin;
                        ron = 0.0;
                        qmax = 0.0;
                    }
                    else
                    {
                        qmax = Math.Max(0.0, roff);
                        qmax = Math.Max(qmax, res);
                        for (int i = 0; i <= n; i++)
                        {
                            qmax = Math.Max(qmax, qex[i]);
                            // qmax = Math.Max(qmax, qexpot[i]); // this to make steps small when pot is large therefore to
                            // provide accurate pot supply back to crops
                            qmax = Math.Max(qmax, Math.Abs(qs[i]));
                            qmax = Math.Max(qmax, Math.Abs(qssif[i]));
                            qmax = Math.Max(qmax, Math.Abs(qssof[i]));
                            qmax = Math.Max(qmax, Math.Abs(q[i]));
                        }
                        qmax = Math.Max(qmax, Math.Abs(q[n + 1]));
                        if (qmax > 0.0)
                            _dt = MathUtilities.Divide(dw, qmax, 0.0);
                    }

                    _dt = Math.Min(_dt, timestepRemaining);

                    double crt = CRain(t);
                    dr = CRain(t + _dt) - crt;

                    double dw1 = (ron == 0.0) ? 0.1 * dw : dw;

                    double t2 = 0.0;
                    if (dr > 1.1 * dw1)
                    {
                        double t1 = t;
                        for (int i = 0; i < 10; i++)
                        {
                            _dt *= 0.5;
                            t2 = t1 + _dt;
                            dr = CRain(t2) - crt;
                            if (dr < 0.9 * dw1)
                                t1 = t2;
                            else if (dr <= 1.1 * dw1)
                                break;
                        }
                        _dt = t2 - t;
                    }

                    _dt = Math.Min(_dt, dtmax);
                    _dt = Math.Max(_dt, dtmin);
                }

                double dtiny = Math.Max(0.01 * _dt, dtmin);
                //        initialise and take new step
                //        ----------------------------

                double wpold = _wp;
                double old_hmin = _hmin;
                double old_gsurf = gsurf;
                double[] pold = new double[n + 1];
                double[,] cslold = new double[num_solutes, n + 1];
                hold = _h;
                for (int i = 0; i <= n; i++)
                {
                    // save transformed potls and water contents
                    pold[i] = _p[i];
                    thold[i] = th[i];
                    //nh
                    //             psiold[i] = _psi[i];
                    for (int solnum = 0; solnum < num_solutes; solnum++)
                        cslold[solnum, i] = csl[solnum][i];
                }

                double old_time = t;

                //   new step
            //40       continue
            retry:

                t += _dt;
                if (timestepRemaining - _dt < 0.1 * _dt)
                {
                    t = t - _dt + timestepRemaining;
                    _dt = timestepRemaining;
                }

                dr = CRain(t) - CRain(t - _dt);
                ron = dr / _dt; // it could just be rain_intensity

                //cnh
                for (int i = 0; i < num_solutes; i++)
                    rslon[i] = (CSol(i, t) - CSol(i, t - _dt)) / _dt;

                PStat(0, ref resp);

                double deqr = Eqrain(t) - Eqrain(t - _dt);
                if (isbc == 2)
                    HMin(deqr, ref _hmin);
                if (itbc == 2)
                    GSurf(deqr, ref gsurf);
                //cnh
                CheckDemand();

                //call event_send(unknown_module,'pre_swim_timestep')

                // integrate for step _dt

                Solve(itlim, ref fail);

                if (fail)
                {
                    // SWIM failed to find a solution, should reset values to its previous state 
                    // and attempt to solve again with a smaller dt

                    ShowDiagnostics(pold);
                    // Reset values
                    t = old_time;
                    _hmin = old_hmin;
                    gsurf = old_gsurf;
                    _wp = wpold;
                    _dt = 0.5 * _dt;
                    _h = hold;
                    for (int i = 0; i <= n; i++)
                    {
                        _p[i] = pold[i];
                        th[i] = thold[i];
                        for (int solnum = 0; solnum < num_solutes; solnum++)
                            csl[solnum][i] = cslold[solnum, i];
                    }

                    //RC   lines for g%th and g%csl added by RCichota, 09/02/2010

                    _dt = 0.5 * _dt;

                    // Tell user that SWIM is changing dt
                    summary.WriteMessage(this, "ApsimSwim|apswim_swim - Changing dt value from: " + String.Format("{0,15:F3}", _dt * 2.0) + " to: " + String.Format("{0,15:F3}", _dt));
                    if (_dt >= dtiny)
                        goto retry;
                }
                else
                {
                    // update variables
                    TD_runoff += roff * _dt * 10.0;
                    TD_evap += res * _dt * 10.0;
                    TD_drain += q[n + 1] * _dt * 10.0;
                    TD_rain += ron * _dt * 10.0;
                    TD_pevap += resp * _dt * 10.0;
                    TD_subsurface_drain += MathUtilities.Sum(qssof) * _dt * 10.0;
                    for (int node = 0; node <= n + 1; node++)
                        TD_wflow[node] += q[node] * _dt * 10.0;

                    for (int solnum = 0; solnum < num_solutes; solnum++)
                    {
                        // kg    cm ug          g   kg
                        // -- = (--p%x--) p%x hr p%x -- p%x --
                        // ha    hr  g         ha   ug

                        TD_soldrain[solnum] +=
                                  qsl[solnum][n + 1] * _dt
                                  * (1e4) * (1e4)   // cm^2/ha = g/ha
                                  * 1e-9;          // kg/ug

                        for (int node = 0; node <= n + 1; node++)
                        {
                            TD_sflow[solnum][node] += qsl[solnum][node] * _dt * (1e4) * (1e4) * 1e-9;
                            TD_slssof[solnum] += csl[solnum][node] * qssof[node] * _dt * (1e4) * (1e4) * 1e-9;
                        }
                    }

                    //cnh
                    PStat(1, ref resp);
                    PStat(2, ref resp);

                    //cnh
                    // call event_send(unknown_module,'post_swim_timestep')

                }

                // We have now finished our first timestep
                run_has_started = true;
                timestepRemaining -= _dt;
            }
            while (timestepRemaining > 0.0 && !fail);
            return fail;
        }

        /// <summary>
        /// Shows the diagnostics.
        /// </summary>
        /// <param name="pold">The pold.</param>
        private void ShowDiagnostics(double[] pold)
        {
            if (Diagnostics)
            {
                summary.WriteMessage(this, "     APSwim Numerical Diagnostics");
                summary.WriteMessage(this, "     ------------------------------------------------------------------------------");
                summary.WriteMessage(this, "      depth      Theta         psi        K           p          p*");
                summary.WriteMessage(this, "     ------------------------------------------------------------------------------");

                double k;
                double dummy1, dummy2, dummy3, dummy4, dummy5 = 0.0, dummy6 = 0.0;
                for (int layer = 0; layer < x.Length; layer++)
                {
                    Watvar(layer, _p[layer], out dummy1, out dummy2, out dummy3, out dummy4, ref dummy5, out k, ref dummy6);
                    summary.WriteMessage(this, String.Format("     {0,6:F1}         {1,9:F7} {2,10:0.###} {3,10:F3} {4,10:F3} {5,10:F3}",
                                      x[layer] * 10.0,
                                      th[layer],
                                      _psi[layer],
                                      k,
                                      _p[layer],
                                      pold[layer]));
                }
                summary.WriteMessage(this, "     ------------------------------------------------------------------------------");
            }
        }

        /// <summary>
        /// Checks the demand.
        /// </summary>
        private void CheckDemand()
        {
            for (int crop = 0; crop < num_crops; crop++)
                for (int solnum = 0; solnum < num_solutes; solnum++)
                {
                    double tpsuptake = 0.0;
                    for (int layer = 0; layer <= n; layer++)
                        tpsuptake += Math.Max(psuptake[solnum][crop][layer], 0.0);

                    double demand = Math.Max(solute_demand[crop][solnum] - tpsuptake, 0.0);

                    demand_is_met[crop][solnum] = demand <= 0.0;
                }
        }

        /// <summary>
        /// hes the minimum.
        /// </summary>
        /// <param name="deqrain">The deqrain.</param>
        /// <param name="sstorage">The sstorage.</param>
        private void HMin(double deqrain, ref double sstorage)
        {
            // Ideally, if timesteps are small we could just use
            // dHmin/dEqr = -1/p%hrc p%x (g%hmin - p%hm0)
            // but because this is really just a linear approximation of the
            // curve for longer timesteps we had better be explicit and
            // calculate the difference from the exponential decay curve.

            if (hrc != 0)
            {
                // first calculate the amount of Energy that must have been
                // applied to reach the current g%hmin.

                double decayFraction = MathUtilities.Divide(_hmin - _hm0, _hm1 - _hm0, 0.0);

                if (MathUtilities.FloatsAreEqual(decayFraction, 0.0))
                {
                    // the roughness is totally decayed
                    sstorage = _hm0;
                }
                else
                {
                    double ceqrain = -_hrc * Math.Log(decayFraction);

                    // now add rainfall energy for this timestep
                    if (cover_effects != null && cover_effects.Trim() == "on")
                        ceqrain += deqrain * (1.0 - residue_cover);
                    else
                        ceqrain += deqrain;

                    // now calculate new surface storage from new energy
                    sstorage = _hm0 + (_hm1 - _hm0) * Math.Exp(-ceqrain / _hrc);
                }
            }
            //else
            // nih - commented out to keep storage const
            //! sstorage = _hm0;
        }

        /// <summary>
        /// gs the surf.
        /// </summary>
        /// <param name="deqrain">The deqrain.</param>
        /// <param name="surfcon">The surfcon.</param>
        private void GSurf(double deqrain, ref double surfcon)
        {
            //     Short Description:
            //     gets soil surface conductance, surfcon

            // Ideally, if timesteps are small we could just use
            // dgsurf/dEqr = -1/grc x (gsurf - g0)
            // but because this is really just a linear approximation of the
            // curve for longer timesteps we had better be explicit and
            // calculate the difference from the exponential decay curve.

            if (grc != 0)
            {
                // first calculate the amount of Energy that must have been
                // applied to reach the current conductance.

                double decayFraction = MathUtilities.Divide(gsurf - _g0, _g1 - _g0, 0.0);

                if (MathUtilities.FloatsAreEqual(decayFraction, 0.0))
                {
                    // seal is totally decayed
                    surfcon = _g0;
                }
                else
                {
                    double ceqrain = -_grc * Math.Log(decayFraction);

                    // now add rainfall energy for this timestep
                    if (cover_effects != null && cover_effects.Trim() == "on")
                        ceqrain += deqrain * (1.0 - residue_cover);
                    else
                        ceqrain += deqrain;

                    // now calculate new surface storage from new energy
                    surfcon = _g0 + (_g1 - _g0) * Math.Exp(-ceqrain / _grc);
                }
            }
            else
                surfcon = gsurf;
        }

        /// <summary>
        /// Solves the specified itlim.
        /// </summary>
        /// <param name="itlim">The itlim.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        private void Solve(int itlim, ref bool fail)
        {
            //     Short description:
            //     solves for this time step

            int it = 0;
            double wpold = _wp;
            int iroots = 0;
            // loop until solved or too many iterations or Thomas algorithm fails
            int i1;
            int i2;
            double[] a = new double[n + 1];
            double[] b = new double[n + 1];
            double[] c = new double[n + 1];
            double[] d = new double[n + 1];
            double[] rhs = new double[n + 1];
            double[] dp = new double[n + 1];
            double[] vbp = new double[n + 1];
            PondingData pondingData = new PondingData();

            do
            {
                it++;
                //        get balance eqns
                // LOOK OUT. THE FORTRAN CODE USED ARRAY INDICES STARTING AT -1
                Baleq(it, ref iroots, ref slos, ref csl, out i1, out i2, ref a, ref b, ref c, ref rhs, ref pondingData);
                //   test for convergence to soln
                // nh hey - wpf has no arguments !
                // nh         _wp = wpf(n, _dx, th)
                _wp = Wpf();

                double balerr = ron - roff - q[n + 1] - rex - res + rssf - (_h - hold + _wp - wpold) / _dt;
                double err = 0.0;
                for (int i = i1; i <= i2; i++)
                {
                    double aerr = Math.Abs(rhs[i]);
                    if (err < aerr)
                        err = aerr;
                }

                // switch off iteration for root extraction if err small enough
                if (err < errex * rex && iroots == 0)
                    iroots = 1;
                if (Math.Abs(balerr) < ersoil && err < ernode)
                    fail = false;
                else
                {
                    int neq = i2 - i1 + 1;
                    Thomas(i1, neq, ref a, ref b, ref c, ref rhs, ref d, ref dp, ref pondingData, out fail);
                    _work += neq;
                    //nh            if(fail)go to 90
                    if (fail)
                    {
                        //nh               call warning_error(Err_internal,
                        //nh     :            'swim will reduce timestep to solve water movement')
                        summary.WriteMessage(this, "swim will reduce timestep to avoid error in water balance");
                        break;
                    }

                    fail = true;
                    //           limit step size_of for soil nodesn
                    int i0 = Math.Max(i1, 0);
                    for (int i = i0; i <= i2; i++)
                    {
                        if (dp[i] > dppl)
                            dp[i] = dppl;
                        if (dp[i] < -dpnl)
                            dp[i] = -dpnl;
                    }
                    //           update solution
                    int j = i0;
                    for (int i = i0; i <= i2; i++)
                    {
                        _p[j] += dp[i];
                        if (j > 0 && j < n - 1)
                        {
                            if (MathUtilities.FloatsAreEqual(x[j], x[j + 1]))
                            {
                                j++;
                                _p[j] = _p[j - 1];
                            }
                        }
                        j++;
                    }
                    if (i1 == -1)
                        _h = Math.Max(0.0, _h + pondingData.v);
                    //_h = Math.Max(0.0, _h + dp[-1]);
                }
            }
            while (fail && it < itlim);

            if (fail)
            {
                summary.WriteMessage(this, clock.Today.ToString("dd-mmm-yyyy"));
                summary.WriteMessage(this, "Maximum iterations reached - swim will reduce timestep");
            }

            //     solve for solute movement
            else
            {
                for (int solnum = 0; solnum < num_solutes; solnum++)
                {
                    GetSol(solnum, ref a, ref b, ref c, ref d, ref rhs, ref dp, ref vbp, ref pondingData, ref fail);
                    if (fail)
                    {
                        summary.WriteMessage(this, "swim will reduce timestep to solve solute movement");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the sol.
        /// </summary>
        /// <param name="solnum">The solnum.</param>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <param name="d">The d.</param>
        /// <param name="rhs">The RHS.</param>
        /// <param name="c1">The c1.</param>
        /// <param name="c2">The c2.</param>
        /// <param name="pondingData">The ponding data.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        private void GetSol(int solnum, ref double[] a, ref double[] b, ref double[] c, ref double[] d, ref double[] rhs, ref double[] c1, ref double[] c2, ref PondingData pondingData, ref bool fail)
        {
            //     Short description:
            //     get and solve solute balance eqns

            //     Constant Values
            const int itmax = 20;
            const int constant_conc = 1;
            const int convection_only = 2;

            //     Determine type of solute BBC to use
            int solute_bbc;
            int j;
            double rslovr;
            bool nonlin = false;
            double wtime = 0.0, wtime1 = 0.0;

            if (ibbc == 1)
                // water table boundary condition
                solute_bbc = constant_conc;
            else if (((ibbc == 0) || (ibbc == 4)) && (q[n + 1] < 0))
                // you have a gradient with flow upward 
                solute_bbc = constant_conc;
            else
                solute_bbc = convection_only;

            //    surface solute balance - assume evap. (g%res) comes from x0 store
            double rovr = roff + qbp;
            double rinf = q[0] + res;
            if (rinf > Math.Min(ersoil, ernode))
            {
                cslsur[solnum] = (rslon[solnum] + hold * cslsur[solnum] / _dt) / (rovr + rinf + _h / _dt);
                qsl[solnum][0] = rinf * cslsur[solnum];
                rslovr = rovr * cslsur[solnum];
                if (slsur[solnum] > 0.0)
                {
                    if (cslsur[solnum] < slsci[solnum])
                    {
                        if (slsur[solnum] > rinf * _dt * (slsci[solnum] - cslsur[solnum]))
                        {
                            qsl[solnum][0] = rinf * slsci[solnum];
                            slsur[solnum] = slsur[solnum] - rinf * _dt * (slsci[solnum] - cslsur[solnum]);
                        }
                        else
                        {
                            qsl[solnum][0] = rinf * cslsur[solnum] + slsur[solnum] / _dt;
                            slsur[solnum] = 0.0;
                        }
                    }
                    if (cslsur[solnum] < slscr[solnum])
                    {
                        if (slsur[solnum] > rovr * _dt * (slscr[solnum] - cslsur[solnum]))
                        {
                            rslovr = rovr * slscr[solnum];
                            slsur[solnum] = slsur[solnum] - rovr * _dt * (slscr[solnum] - cslsur[solnum]);
                        }
                        else
                        {
                            rslovr = rovr * cslsur[solnum] + slsur[solnum] / _dt;
                            slsur[solnum] = 0.0;
                        }
                        if (slsur[solnum] > _h * (slscr[solnum] - cslsur[solnum]))
                        {
                            slsur[solnum] = slsur[solnum] - _h * (slscr[solnum] - cslsur[solnum]);
                            cslsur[solnum] = slscr[solnum];
                        }
                        else
                        {
                            if (_h > 0.0)
                                cslsur[solnum] = cslsur[solnum] + slsur[solnum] / _h;
                            slsur[solnum] = 0.0;
                        }
                    }
                }
            }
            else
            {
                cslsur[solnum] = 0.0;
                qsl[solnum][0] = 0.0;
                rslovr = 0.0;
            }

            //     get eqn coeffs
            //     get production and storage components
            double thi;
            double exco1;
            //nh      call slprod
            for (int i = 0; i <= n; i++)
            {
                c1[i] = csl[solnum][i];
                thi = th[i];
                //nh         j=indxsl(solnum,i)
                j = i;
                nonlin = false;

                //Peter's CHANGE 21/10/98 to ensure zero exchange is treated as linear
                //         if (p%fip(solnum,j).eq.1.) then
                if ((MathUtilities.FloatsAreEqual(ex[solnum][j], 0.0)) || (MathUtilities.FloatsAreEqual(fip[solnum][j], 1.0)))
                {
                    //           linear exchange isotherm
                    c2[i] = 1.0;
                    exco1 = ex[solnum][j];
                }
                else
                {
                    //           nonlinear Freundlich exchange isotherm
                    nonlin = true;
                    c2[i] = 0.0;
                    if (c1[i] > 0.0)
                        c2[i] = Math.Pow(c1[i], fip[solnum][i] - 1.0);
                    //``````````````````````````````````````````````````````````````````````````````````````````````````
                    //RC         Changed by RCichota 30/jan/2010
                    exco1 = ex[solnum][j] * c2[i];
                    //            exco1=p%ex(solnum,j)*p%fip(solnum,j)*c2(i)    !<---old code
                    //			
                }
                b[i] = (-(thi + exco1) / _dt) * dx[i] - qssof[i];
                //nh     1        apswim_slupf(1,solnum)*g%qex(i)-g%qssof(i)
                for (int crop = 0; crop < num_crops; crop++)
                    b[i] = b[i] - Slupf(crop, solnum) * qr[i][crop];
                //nh     1        p%slupf(solnum)*g%qex(i)
                rhs[i] = -(csl[solnum][i] * ((thold[i] + exco1) / _dt)) * dx[i];
                qsls[solnum][i] = -(csl[solnum][i] * (thold[i] + ex[solnum][j] * c2[i]) / _dt) * dx[i];
            }

            //     get dispersive and convective components
            //        use central diffs in time for convection, backward diffs for rest
            //        use central diffs in space, but for convection may need some
            //        upstream weighting to avoid instability

            for (int i = 1; i <= n; i++) // NOTE: staring from 1 is deliberate this time
            {
                if (!MathUtilities.FloatsAreEqual(x[i - 1], x[i]))
                {
                    double w1;
                    double thav = 0.5 * (th[i - 1] + th[i]);
                    double aq = Math.Abs(q[i]);
                    dc[solnum][i] = dcon[solnum] * Math.Pow(thav - SwimSoluteParameters.DTHC, SwimSoluteParameters.DTHP) +
                                    SwimSoluteParameters.Dis * Math.Pow(aq / thav, SwimSoluteParameters.Disp);
                    double dfac = thav * dc[solnum][i] / (x[i] - x[i - 1]);
                    if (slswt >= 0.5 && slswt <= 1.0)
                    {
                        //              use fixed space weighting on convection
                        w1 = MathUtilities.Sign(2.0 * slswt, q[i]);
                    }
                    else
                    {
                        //              use central diffs for convection if possible, else use
                        //                 just enough upstream weighting to avoid oscillation
                        //                 user may increase acceptable level for central diffs
                        //                 by setting p%slswt < -1
                        double accept = Math.Max(1.0, -slswt);
                        double wt = 0.0;
                        if (aq != 0.0)
                            wt = MathUtilities.Sign(Math.Max(0.0, 1.0 - 2.0 * accept * dfac / aq), q[i]);
                        w1 = 1.0 + wt;
                    }
                    double w2 = 2.0 - w1;

                    //Peter's CHANGE 21/10/98 to remove/restore Crank-Nicolson time weighting
                    //for convection
                    //            fq=.25*g%q(i)
                    //            fqc=fq*(w1*g%csl(solnum,i-1)+w2*g%csl(solnum,i))
                    //            wtime=0.25D0
                    //            wtime1=1.0D0
                    wtime = 0.5;
                    wtime1 = 0.0;
                    double fq = wtime * q[i];
                    double fqc = wtime1 * fq * (w1 * csl[solnum][i - 1] + w2 * csl[solnum][i]);

                    //           get convective component from old time level
                    qsl[solnum][i] = fqc;
                    b[i - 1] = b[i - 1] - dfac - fq * w1;
                    c[i - 1] = dfac - fq * w2;
                    a[i] = dfac + fq * w1;
                    b[i] = b[i] - dfac + fq * w2;
                    rhs[i - 1] = rhs[i - 1] + fqc;
                    rhs[i] = rhs[i] - fqc;
                }
            }

            //     allow for bypass flow
            qslbp[solnum] = 0.0;

            //     impose boundary conditions
            int k;
            if (itbc == 1)
            {
                //        constant concentration
                k = 1;
            }
            else
            {
                k = 0;
                rhs[0] = rhs[0] - qsl[solnum][0];
                if (rinf < -Math.Min(ersoil, ernode))
                {
                    b[0] = b[0] + 0.5 * rinf;
                    rhs[0] = rhs[0] - 0.5 * rinf * csl[solnum][0];
                }
            }

            int neq;
            if (solute_bbc == constant_conc)
            {
                //        constant concentration
                //nh
                csl[solnum][n] = cslgw[solnum];
                //nh
                rhs[n - 1] = rhs[n - 1] - c[n - 1] * csl[solnum][n];
                neq = n;
            }
            else
            {
                //        convection only
                b[n] = b[n] - 0.5 * q[n + 1];
                rhs[n] = rhs[n] + 0.5 * q[n + 1] * csl[solnum][n];
                neq = n + 1;
            }
            //     allow for two nodes at same depth
            j = 0;
            for (int i = 1; i <= n; i++)
            {
                if (!MathUtilities.FloatsAreEqual(x[i - 1], x[i]))
                {
                    j = j + 1;
                    a[j] = a[i];
                    b[j] = b[i];
                    rhs[j] = rhs[i];
                    c[j - 1] = c[i - 1];
                }
                else
                {
                    b[j] = b[j] + b[i];
                    rhs[j] = rhs[j] + rhs[i];
                }
            }
            //     save old g%csl(0),g%csl(p%n)
            double csl0 = csl[solnum][0];
            double csln = csl[solnum][n];
            neq = neq - (n - j);
            int itcnt = 0;
        //     solve for concentrations

    loop:
            //nh      call thomas(neq,0,a(k),b(k),c(k),rhs(k),dum,d(k),g%csl(solnum,k),
            //nh     :            dum,fail)
            double[] csltemp = new double[n + 1];
            for (int i = 0; i <= n; i++)
                csltemp[i] = csl[solnum][i];
            Thomas(k, neq, ref a, ref b, ref c, ref rhs, ref d, ref csltemp, ref pondingData, out fail);
            for (int i = 0; i <= n; i++)
                csl[solnum][i] = csltemp[i];
            // nh end subroutine
            itcnt++;
            slwork = slwork + neq;
            if (fail)
                return;
            j = k + neq - 1;
            if (solute_bbc == convection_only)
            {
                csl[solnum][n] = csl[solnum][j];
                j--;
            }
            for (int i = n - 1; i > 0; i--)
            {
                if (!MathUtilities.FloatsAreEqual(x[i], x[i + 1]))
                {
                    csl[solnum][i] = csl[solnum][j];
                    j--;
                }
                else
                {
                    csl[solnum][i] = csl[solnum][i + 1];
                }
            }

            if (nonlin)
            {
                //        test for convergence
                double dmax = 0.0;
                for (int i = 0; i <= n; i++)
                {
                    double dabs = Math.Abs(csl[solnum][i] - c1[i]);
                    if (dmax < dabs)
                        dmax = dabs;
                }
                if (dmax > slcerr)
                {
                    if (itcnt == itmax)
                    {
                        fail = true;
                        return;
                    }
                    //           keep iterating using Newton-Raphson technique
                    //           next c^fip for Freundlich isotherm is approximated as
                    //              cn^fip=c^fip+p%fip*c^(p%fip-1)*(cn-c)
                    //                    =p%fip*c^(p%fip-1)*cn+(1-p%fip)*c^fip
                    j = 0;
                    for (int i = 0; i <= n; i++)
                    {
                        if (!MathUtilities.FloatsAreEqual(x[i - 1], x[i]))
                        {
                            if (i > 0)
                                j++;
                        }
                        //cnh               kk=indxsl(solnum,i)
                        int kk = i;
                        if (!MathUtilities.FloatsAreEqual(fip[solnum][i], 1.0))
                        {
                            double cp = 0.0;
                            if (csl[solnum][i] > 0.0)
                                cp = Math.Pow(csl[solnum][i], fip[solnum][i] - 1.0);

                            //````````````````````````````````````````````````````````````````````````````````````````````````````````````````
                            //RC      Changed by RCichota (29/Jan/2010), original code is commented out
                            double d1 = cp - c2[i];
                            //                  d1=p%fip(solnum,kk)*(cp-c2(i))
                            //                  d2=(1.-p%fip(solnum,kk))
                            //     :              *(g%csl(solnum,i)*cp-c1(i)*c2(i))
                            c1[i] = csl[solnum][i];
                            c2[i] = cp;
                            b[j] = b[j] - (ex[solnum][kk] / _dt) * d1 * dx[i];
                            //                  rhs(j)=rhs(j)+(p%ex(solnum,kk)/g%dt
                            //     :                            -p%betaex(solnum,kk))
                            //     :                          *d2*p%dx(i)
                            //````````````````````````````````````````````````````````````````````````````````````````````````````````````````
                            // Changes in the calc of d1 are to agree with the calc of exco1 above (no need to multiply by p%fip
                            // If p%fip < 1, the unkown is Cw, and is only used in the calc of b. thus rhs is commented out.
                            //`	 
                        }
                    }
                    goto loop;
                }
            }

            //     get surface solute balance?
            if (rinf < -Math.Min(ersoil, ernode))
            {
                //        flow out of surface
                //CHANGES 6/11/98 to remove/restore Crank-Nicolson time weighting for convection
                //-----
                //         g%qsl(solnum,0)=.5*rinf*(csl0+g%csl(solnum,0))
                qsl[solnum][0] = 0.5 * rinf * (wtime1 * csl0 + 4.0 * wtime * csl[solnum][0]);

                double rslout = -qsl[solnum][0];
                if (slsur[solnum] > 0.0)
                {
                    //           allow for surface applied solute
                    if (csl[solnum][0] < slsci[solnum])
                    {
                        if (slsur[solnum] > -rinf * _dt * (slsci[solnum] - csl[solnum][0]))
                        {
                            rslout = -rinf * slsci[solnum];
                            slsur[solnum] = slsur[solnum] + rinf * _dt * (slsci[solnum] - csl[solnum][0]);
                        }
                        else
                        {
                            rslout = rslout + slsur[solnum] / _dt;
                            slsur[solnum] = 0.0;
                        }
                    }
                }
                //        get surface solute balance
                cslsur[solnum] = (rslon[solnum] + rslout + hold * cslsur[solnum] / _dt) / (rovr + _h / _dt);
                rslovr = rovr * cslsur[solnum];
            }

            rsloff[solnum] = rslovr - qslbp[solnum];
            //     get solute fluxes
            for (int i = 1; i <= n; i++)
            {
                if (!MathUtilities.FloatsAreEqual(x[i - 1], x[i]))
                {
                    double dfac = 0.5 * (th[i - 1] + th[i]) * dc[solnum][i] / (x[i] - x[i - 1]);
                    double aq = Math.Abs(q[i]);
                    double accept = Math.Max(1.0, -slswt);
                    double wt = 0.0;
                    if (aq != 0.0)
                        wt = MathUtilities.Sign(Math.Max(0.0, 1.0 - 2.0 * accept * dfac / aq), q[i]);
                    //Peter's CHANGES 21/10/98 to remove/restore Crank-Nicolson time weighting
                    //for convection
                    //            g%qsl(solnum,i)=g%qsl(solnum,i)
                    //     :                    +.25*g%q(i)*((1.+wt)*g%csl(solnum,i-1)
                    //     :                    +(1.-wt)*g%csl(solnum,i))
                    //     1                    +dfac*(g%csl(solnum,i-1)-g%csl(solnum,i))
                    qsl[solnum][i] = qsl[solnum][i] + wtime * q[i] * ((1.0 + wt) * csl[solnum][i - 1]
                                     + (1.0 - wt) * csl[solnum][i]) + dfac * (csl[solnum][i - 1] - csl[solnum][i]);
                }

            }
            for (int i = 2; i < n; i++)
            {
                if (MathUtilities.FloatsAreEqual(x[i - 1], x[i]))
                {
                    qsl[solnum][i] = (dx[i] * qsl[solnum][i - 1] + dx[i - 1] * qsl[solnum][i + 1]) / (dx[i - 1] + dx[i]);
                }
            }

            rslex[solnum] = 0.0;
            for (int i = 0; i <= n; i++)
            {
                //nh         j=indxsl(solnum,i)
                j = i;
                double cp = 1.0;
                if (!MathUtilities.FloatsAreEqual(fip[solnum][i], 1.0))
                {
                    cp = 0.0;
                    if (csl[solnum][i] > 0.0)
                        cp = Math.Pow(csl[solnum][i], fip[solnum][i] - 1.0);
                }
                cslt[solnum][i] = (th[i] + ex[solnum][j] * cp) * csl[solnum][i];

                for (int crop = 0; crop < num_crops; crop++)
                    rslex[solnum] += qr[i][crop] * csl[solnum][i] * Slupf(crop, solnum);

                qsls[solnum][i] += (csl[solnum][i] * (thold[i] + ex[solnum][j] * cp) / _dt) * dx[i];
            }

            if (solute_bbc == constant_conc)
            {
                //        constant concentration
                //nh         j=indxsl(solnum,p%n)
                j = n;
                qsl[solnum][n + 1] = qsl[solnum][n] - qsls[solnum][n] - qssof[n] * csl[solnum][n];
                //nh     :                  -g%qex(p%n)*g%csl(solnum,p%n)*p%slupf(solnum)
                //nh     :              -g%qex(p%n)*g%csl(solnum,p%n)*apswim_slupf(1,solnum)                 

                for (int crop = 0; crop < num_crops; crop++)
                    qsl[solnum][n + 1] -= qr[n][crop] * csl[solnum][n] * Slupf(crop, solnum);
            }
            else
            {
                //        convection only
                //CHANGES 6/11/98 to remove/restore Crank-Nicolson time weighting for convection
                //-----
                //         g%qsl(solnum,p%n+1)=.5*g%q(p%n+1)*(csln+g%csl(solnum,p%n))
                qsl[solnum][n + 1] = 0.5 * q[n + 1] * (wtime1 * csln + 4.0 * wtime * csl[solnum][n]);
            }
        }

        /// <summary>
        /// Gets the solute variables.
        /// </summary>
        private void GetSoluteVariables()
        {
            double[] solute_n = new double[n + 1];

            for (int solnum = 0; solnum < num_solutes; solnum++)
            {
                ConcWaterSolute(solute_names[solnum], ref solute_n);
                for (int node = 0; node <= n; node++)
                    csl[solnum][node] = solute_n[node];
            }
        }

        /// <summary>
        /// Gets the flow.
        /// </summary>
        /// <param name="flowName">Name of the flow.</param>
        /// <param name="flowArray">The flow array.</param>
        /// <param name="flowFlag">if set to <c>true</c> [flow flag].</param>
        private void GetFlow(string flowName, out double[] flowArray, out bool flowFlag)
        {
            //+  Initial Data Values
            // set to false to start - if match is found it is
            // set to true.
            flowFlag = false;

            //string flowUnits;
            flowArray = new double[n + 1];

            if (flowName == "water")
            {
                flowFlag = true;
                //flowUnits = "(mm)";
                for (int node = 0; node <= n + 1; node++)
                    flowArray[node] = TD_wflow[node];
            }
            else
            {
                for (int solnum = 0; solnum < num_solutes; solnum++)
                {
                    if (solute_names[solnum] == flowName)
                    {
                        for (int node = 0; node <= n + 1; node++)
                            flowArray[node] = TD_sflow[solnum][node];
                        flowFlag = true;
                        //flowUnits = "(kg/ha)";
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Concs the water solute.
        /// </summary>
        /// <param name="solname">The solname.</param>
        /// <param name="concWaterSolute">The conc water solute.</param>
        /// <exception cref="System.Exception">No module has registered ownership for solute:  + solname
        /// or
        /// -ve value for solute was passed to SWIM + mess
        /// or
        /// You have asked apswim to use a solute that it does not know about :- + solname</exception>
        private void ConcWaterSolute(string solname, ref double[] concWaterSolute)
        {
            //+  Changes
            //     30-01-2010 - RCichota - added test for -ve values, causes a fatal error if so


            //+  Purpose
            //      Calculate the concentration of solute in water (ug/l).  Note that
            //      this routine is used to calculate output variables and input
            //      variablesand so can be called at any time during the simulation.
            //      It therefore must use a solute profile obtained from the solute's
            //      owner module.  It therefore also follows that this routine cannot
            //      be used for internal calculations of solute concentration during
            //      the process stage etc.


            //*+  Initial Data Values
            concWaterSolute = new double[n + 1]; // Init with zeroes
            double[] solute_n = new double[n + 1]; // solute at each node

            int solnum = SoluteNumber(solname);

            if (solnum >= 0)
            {
                // only continue if solute exists.
                if (solute_owners[solnum] != 0)
                {
                    //// TODO !!! We need to store the full path name of the owner
                    string compName = ""; // = Paddock.SiblingNameFromId(solute_owners[solnum]);
                    object objValue = Apsim.Get(this, compName + "." + solname);
                    if (objValue == null)
                        throw new Exception("No module has registered ownership for solute: " + solname);
                    double[] Value = objValue as double[];
                    // Should check array size here to be sure it matches...
                    Array.Copy(Value, solute_n, Math.Min(Value.Length, solute_n.Length));
                }

                for (int node = 0; node <= n; node++)
                {
                    //````````````````````````````````````````````````````````````````````````````````
                    //RC            Changes by RCichota, 30/Jan/2010
                    // Note: Sometimes small numerical errors can leave -ve concentrations.
                    // This will check for -ve or very small values being passed by other modules
                    //  and define the appropriate response:

                    if (solute_n[node] < -(negative_conc_fatal))
                    {

                        string mess = String.Format("   Total {0}({1,3}) = {2,12:G6}",
                                        solute_names[solnum],
                                        node,
                                        solute_n[node]);
                        solute_n[node] = 0.0;
                        throw new Exception("-ve value for solute was passed to SWIM" + mess);
                    }

                    else if (solute_n[node] < -(negative_conc_warn))
                    {
                        string mess = String.Format("   Total {0}({1,3}) = {2,12:G6} - Value will be set to zero",
                                        solute_names[solnum],
                                        node,
                                        solute_n[node]);
                        IssueWarning("'-ve value for solute was passed to SWIM" + mess);

                        solute_n[node] = 0.0;
                    }
                    else if (solute_n[node] < 1e-100)
                    {
                        // Value is REALLY small, no need to tell user,
                        // set value to zero to avoid underflow with reals

                        solute_n[node] = 0.0;
                    }
                    // else value is positive and considerable

                    //````````````````````````````````````````````````````````````````````````````````

                    // convert solute from kg/ha to ug/cc soil
                    // ug Sol    kg Sol    ug   ha(node)
                    // ------- = ------- * -- * -------
                    // cc soil   ha(node)  kg   cc soil

                    cslstart[solnum][node] = solute_n[node];
                    solute_n[node] = solute_n[node]
                                     * 1.0e9               // ug/kg
                                     / (dx[node] * 1.0e8); // cc soil/ha

                    concWaterSolute[node] = SolveFreundlich(node, solnum, solute_n[node]);
                }
            }

            else
                throw new Exception("You have asked apswim to use a solute that it does not know about :-" + solname);
        }

        /// <summary>
        /// Concs the adsorb solute.
        /// </summary>
        /// <param name="solname">The solname.</param>
        /// <param name="concAdsorbSolute">The conc adsorb solute.</param>
        /// <exception cref="System.Exception">No module has registered ownership for solute:  + solname
        /// or
        /// -ve value for solute was passed to SWIM + Environment.NewLine + mess
        /// or
        /// You have asked apswim to use a solute that it does not know about :- + solname</exception>
        private void ConcAdsorbSolute(string solname, ref double[] concAdsorbSolute)
        {
            //+  Purpose
            //      Calculate the concentration of solute adsorbed (ug/g soil). Note that
            //      this routine is used to calculate output variables and input
            //      variablesand so can be called at any time during the simulation.
            //      It therefore must use a solute profile obtained from the solute's
            //      owner module.  It therefore also follows that this routine cannot
            //      be used for internal calculations of solute concentration during
            //      the process stage etc.
            //+  Changes
            //     30-01-2010 - RCichota - added test for -ve values, causes a fatal error if so

            concAdsorbSolute = new double[n + 1];  // init with zeroes
            double[] solute_n = new double[n + 1]; // solute at each node

            int solnum = SoluteNumber(solname);

            if (solnum >= 0)
            {
                // only continue if solute exists.
                if (solute_owners[solnum] != 0)
                {
                    //// TODO !!! We need to store the full path name of the owner
                    string compName = ""; //  = Paddock.SiblingNameFromId(solute_owners[solnum]);
                    object objValue = Apsim.Get(this, compName + "." + solname);
                    if (objValue == null)
                        throw new Exception("No module has registered ownership for solute: " + solname);
                    double[] Value = objValue as double[];
                    // Should check array size here to be sure it matches...
                    Array.Copy(Value, solute_n, Math.Min(Value.Length, solute_n.Length));
                }

                for (int node = 0; node <= n; node++)
                {

                    //````````````````````````````````````````````````````````````````````````````````
                    //RC            Changes by RCichota, 30/Jan/2010
                    // Note: Sometimes small numerical errors can leave -ve concentrations.
                    // This will check for -ve or very small values being passed by other modules
                    //  and define the appropriate response:

                    if (solute_n[node] < -(negative_conc_fatal))
                    {

                        string mess = String.Format("   Total {0}({1,3}) = {2,12:G6}",
                                        solute_names[solnum],
                                        node,
                                        solute_n[node]);
                        solute_n[node] = 0.0;
                        throw new Exception("-ve value for solute was passed to SWIM" + Environment.NewLine + mess);
                    }

                    else if (solute_n[node] < -(negative_conc_warn))
                    {
                        string mess = String.Format("   Total {0}({1,3}) = {2,12:G6} - Value will be set to zero",
                                        solute_names[solnum],
                                        node,
                                        solute_n[node]);
                        IssueWarning("'-ve value for solute was passed to SWIM" + Environment.NewLine + mess);

                        solute_n[node] = 0.0;
                    }

                    else if (solute_n[node] < 1.0e-100)
                    {
                        // Value is REALLY small, no need to tell user,
                        // set value to zero to avoid underflow with reals

                        solute_n[node] = 0.0;
                    }

                    // else Value is positive and considerable

                    //````````````````````````````````````````````````````````````````````````````````

                    // convert solute from kg/ha to ug/cc soil
                    // ug Sol    kg Sol    ug   ha(node)
                    // ------- = ------- * -- * -------
                    // cc soil   ha(node)  kg   cc soil

                    solute_n[node] = solute_n[node]   // kg/ha
                                * 1.0e9               // ug/kg
                                / (dx[node] * 1.0e8); // cc soil/ha

                    double concWaterSolute = SolveFreundlich(node, solnum, solute_n[node]);

                    //                  conc_adsorb_solute(node) =
                    //     :              ddivide(solute_n(node)
                    //     :                         - conc_water_solute * g%th(node)
                    //     :                      ,p%rhob(node)
                    //     :                      ,0d0)

                    concAdsorbSolute[node] = ex[solnum][node] * Math.Pow(concWaterSolute, fip[solnum][node]);
                }
            }

            else
                throw new Exception("You have asked apswim to use a solute that it does not know about :-" + solname);
        }

        /// <summary>
        /// Solutes the number.
        /// </summary>
        /// <param name="solname">The solname.</param>
        /// <returns></returns>
        private int SoluteNumber(string solname)
        {
            for (int counter = 0; counter < num_solutes; counter++)
                if (solute_names[counter] == solname)
                    return counter;
            return -1;
        }

        /// <summary>
        /// Solves the freundlich.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="solnum">The solnum.</param>
        /// <param name="Ctot">The ctot.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">-ve concentration was passed to Freundlich solution + mess
        /// or
        /// -ve value for Cw on solving Freundlich1 + mess
        /// or
        /// -ve value for Cw on solving Freundlich2 + mess
        /// or
        /// -ve value for solute found in adsorption isotherm + mess
        /// or
        /// APSwim failed to solve the freundlich isotherm</exception>
        private double SolveFreundlich(int node, int solnum, double Ctot)
        {
            //+  Purpose
            //   Calculate the solute in solution for a given total solute
            //   concentration for a given node.
            //+  Changes
            //   RCichota - 30/Jan/2010 - update name of proc., organize the newton solution
            //                 added tests for -ve and very small values

            //*+  Constant Values
            const int max_iterations = 1000;
            const double tolerance = 1e-10;

            //RC    Changes by RCichota, 30/Jan/2010, updated in 10/Jul/2010
            // Organize solution and add test for no adsorption or linear adsorption

            bool solved = false;
            double Cw;
            double f;
            double dfdCw;
            if (Math.Abs(Ctot) < 1e-100)
            {
                // really small value or zero, solution is zero

                solved = true;
                Cw = 0.0;
            }
            else if (Ctot < 0.0)
            {
                // negative value for Ctot, this should have been catched already
                string mess = String.Format("   Total {0}({1,3}) = {2,12:G6}",
                                           solute_names[solnum],
                                           node,
                                           Ctot);
                throw new Exception("-ve concentration was passed to Freundlich solution" + mess);
            }
            else
            {
                // Ctot is OK, proceed to calculations

                // Check for no adsorption and whether the isotherm is linear
                if (MathUtilities.FloatsAreEqual(ex[solnum][node], 0.0))
                {
                    //There is no adsorption:

                    Cw = MathUtilities.Divide(Ctot, th[node], 0.0);
                    solved = true;
                }

                else if (MathUtilities.FloatsAreEqual(fip[solnum][node], 0.0))
                {
                    // Full adsorption, solute is immobile:

                    Cw = 0.0;
                    solved = true;
                }

                else if (MathUtilities.FloatsAreEqual(fip[solnum][node], 1.0))
                {
                    // Linear adsorption:

                    Cw = MathUtilities.Divide(Ctot, th[node] + ex[solnum][node], 0.0);
                    solved = true;
                }
                else
                {
                    // Non linear isotherm:

                    // take initial guess for Cw
                    Cw = Math.Pow(MathUtilities.Divide(Ctot, (th[node] + ex[solnum][node]), 0.0), (1.0 / fip[solnum][node]));
                    if (Cw < 0.0)            // test added by RCichota 09/Jul/2010
                    {
                        string mess = String.Format("  {0}({1}) = {2,12:G6} - Iteration: 0",
                                            solute_names[solnum],
                                            node,
                                            Cw);
                        throw new Exception("-ve value for Cw on solving Freundlich1" + mess);
                    }

                    // calculate value of isotherm function and the derivative.

                    Freundlich(node, solnum, ref Cw, out f, out dfdCw);

                    double error_amount = f - Ctot;

                    if (Math.Abs(error_amount) < tolerance)
                    {
                        // It is already solved

                        solved = true;
                    }
                    else if (Math.Abs(dfdCw) < 1e-100)
                    {
                        // We are at zero (approximately) so Cw must be zero - this is a solution too

                        Cw = 0.0;
                        solved = true;
                    }
                    //            elseif (dfdCw) .gt. 1d100) then
                    //                derivative is too large, so Cw must be zero - this is a solution too
                    //               Cw = 0d0
                    //               solved = .true.

                    else
                    {
                        // Iterate until a solution is found or max_iterations is reached

                        solved = false;
                        for (int iter = 0; iter < max_iterations; iter++)
                        {

                            // next value for Cw
                            Cw = Cw - MathUtilities.Divide(error_amount, 2 * dfdCw, 0.0);
                            if (Cw < 0.0)             // test added by RCichota 09/Jul/2010
                            {
                                string mess = String.Format("  {0}({1}) = {2,12:G6} - Iteration: {3}",
                                              solute_names[solnum],
                                              node,
                                              Cw,
                                              iter);
                                throw new Exception("-ve value for Cw on solving Freundlich2" + mess);
                            }

                            // calculate new value of isotherm function and derivative.
                            Freundlich(node, solnum, ref Cw, out f, out dfdCw);
                            error_amount = f - Ctot;

                            if (Math.Abs(error_amount) < tolerance)
                            {
                                solved = true;
                                break;
                            }
                        }
                    }
                }
            }
            if (solved)
            {
                //````````````````````````````````````````````````````````````````````````````````
                // Note: Sometimes small numerical errors can leave -ve concentrations.
                // This will evaluate the error and define the appropriate response:
                //RC      Changes by RCichota, 30/Jan/2010

                if (Math.Abs(Cw) < 1e-100)
                {
                    // Value is REALLY small or zero and can be diregarded,
                    //  set value to zero to avoid underflow with reals

                    Cw = 0.0;
                }

                else if (Cw < 0)
                {
                    // Cw is negative, this is a fatal error.
                    string mess = String.Format(" {0}({1,3}) = {2,12:G6}",
                                         solute_names[solnum],
                                         node,
                                         Cw);
                    throw new Exception("-ve value for solute found in adsorption isotherm" + mess);
                    //Cw = 0.0;
                }
                // else Cw is positive and considerable

                //````````````````````````````````````````````````````````````````````````````````

                // Publish the computed value
                return Cw;
            }

            else
            {
                // A solution was not found

                throw new Exception("APSwim failed to solve the freundlich isotherm");
                //return MathUtilities.Divide(Ctot, th[node], 0.0);
            }
        }

        /// <summary>
        /// Freundliches the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="solnum">The solnum.</param>
        /// <param name="Cw">The cw.</param>
        /// <param name="Ctot">The ctot.</param>
        /// <param name="dCtot">The d ctot.</param>
        /// <exception cref="System.Exception">-ve value has been passed to Freundlich solution + mess</exception>
        private void Freundlich(int node, int solnum, ref double Cw, out double Ctot, out double dCtot)
        {
            //+  Changes
            //   RCichota - 30/Jan/2010 - implement test for -ve values passed in, test for linear isotherm
            //                  and test for very small values (these were ammended in 10/Jul/2010)

            // first make sure Cw has not been passed negative. Changes by RCichota, 30/jan/2010, ammended 09/Jul/2010

            if (Math.Abs(Cw) < 1e-100)
            {
                // Cw is zero or REALLY small and can be diregarded,
                //  set to zero to avoid underflow with reals

                Cw = 0.0;
                Ctot = 0.0;
                dCtot = th[node];   //if n<1 it actually equals infinity at Cw = zero
                //this value will not be used if Cw = 0
            }
            else if (Cw < 0.0)
            {
                // Cw is negative, this is a fatal error.
                string mess = String.Format(" Solution {0}({1,3}) = {2,12:G6}",
                                     solute_names[solnum],
                                     node,
                                     Cw);
                Ctot = 0.0;
                dCtot = 0.0;
                throw new Exception("-ve value has been passed to Freundlich solution" + mess);
            }
            else
            {
                // Cw is positive, proceed with the calculations

                if ((MathUtilities.FloatsAreEqual(fip[solnum][node], 1.0))
                     || (MathUtilities.FloatsAreEqual(ex[solnum][node], 0.0)))
                {
                    // Linear isotherm or no adsorption

                    Ctot = Cw * (th[node] + ex[solnum][node]);
                    dCtot = th[node] + ex[solnum][node];
                }
                else
                {
                    // nonlinear isotherm
                    Ctot = th[node] * Cw + ex[solnum][node] * Math.Pow(Cw, fip[solnum][node]);
                    dCtot = th[node] + ex[solnum][node] * fip[solnum][node] * Math.Pow(Cw, fip[solnum][node] - 1.0);
                }
            }

            //````````````````````````````````````````````````````````````````````````````````
            // Check for very small values, set to zero to avoid underflow with reals

            if (Math.Abs(Ctot) < 1e-100)
                Ctot = 0.0;

            if (Math.Abs(dCtot) < 1e-100)
                dCtot = 0.0;
        }

        /// <summary>
        /// WPFs this instance.
        /// </summary>
        /// <returns></returns>
        private double Wpf()
        {
            //     Short description:
            //     gets water present in profile
            double wpf = 0.0;
            for (int i = 0; i <= n; i++)
            {
                wpf += th[i] * dx[i];
            }
            return wpf;
        }

        /// <summary>
        /// Pfs the specified psi value.
        /// </summary>
        /// <param name="psiValue">The psi value.</param>
        /// <returns></returns>
        private double Pf(double psiValue)
        {
            //     Short description:
            //     returns transform p
            const double psi_0 = -50.0;
            const double psi_1 = psi_0 / 10.0;

            double v = -(psiValue - psi_0) / psi_1;
            if (psiValue < psi_0)
                return Math.Log(v + Math.Sqrt(v * v + 1.0));
            else
                return v;
        }

        /// <summary>
        /// ps the stat.
        /// </summary>
        /// <param name="istat">The istat.</param>
        /// <param name="tresp">The tresp.</param>
        private void PStat(int istat, ref double tresp)
        {
            //     Short Description:
            //     gets potl evap. for soil and veg., and root length densities
            //
            //     g%resp,p%slupf and g%csl were renamed to tslupf,trep,tcsl as there were
            //     already variables with those names in common

            if (istat == 0)
            {
                // calc. potl evap.
                double sep;  // soil evaporation demand
                double rep = (CEvap(t) - CEvap(t - _dt)) / _dt;
                if (cover_effects != null && cover_effects.Trim() == "on")
                {

                    // Use Soilwat cover effects on evaporation.
                    sep = rep * _dt * CoverEosRedn();
                }
                else
                {
                    sep = rep * _dt * (1.0 - crop_cover);
                }

                // Note: g%pep is passed to swim as total ep for a plant for the
                // entire apsim timestep. so rate will be (CEp = cum EP)
                //   dCEp   Total daily EP     dEo
                //   ---- = -------------- p%x --------
                //   g%dt    Total daily Eo      g%dt

                double start_of_day = Time(year, day, TimeToMins(apsim_time));
                double end_of_day = Time(year, day, TimeToMins(apsim_time) + (int)apsim_timestep);

                double TD_Eo = CEvap(end_of_day) - CEvap(start_of_day);

                for (int j = 0; j < nveg; j++)
                    rtp[j] = MathUtilities.Divide(pep[j], TD_Eo, 0.0) * rep;

                // pot soil evap rate is not linked to apsim timestep
                tresp = sep / _dt;

                for (int iveg = 0; iveg < nveg; iveg++)
                {
                    for (int i = 0; i <= n; i++)
                    {
                        if (rld[i][iveg] < 1.0e-20)
                            rld[i][iveg] = 1.0e-20;
                        double rldi = rld[i][iveg];

                        rc[i][iveg] = -Math.Log(Math.PI * RootRadius[i][iveg] * RootRadius[i][iveg] * rldi) / (4.0 * Math.PI * rldi * dx[i]);
                    }
                }
            }
            else if (istat == 1)
            {
                //         update cumulative transpiration
                for (int i = 0; i < nveg; i++)
                {
                    ctp[i] += rtp[i] * _dt;
                    ct[i] += rt[i] * _dt;
                    //cnh
                    for (int j = 0; j <= n; j++)
                    {
                        pwuptake[i][j] += qr[j][i] * _dt * 10.0;
                        // cm -> mm __/
                        pwuptakepot[i][j] += qrpot[j][i] * _dt * 10.0;
                        // cm -> mm __/
                    }
                }
            }
            else if (istat == 2)
            {
                //        update cumulative solute uptake

                for (int i = 0; i < nveg; i++)
                    for (int j = 0; j <= n; j++)
                        for (int solnum = 0; solnum < num_solutes; solnum++)
                            psuptake[solnum][i][j] += Slupf(i, solnum) * csl[solnum][j] * qr[j][i] / 10.0 * _dt;
                // nh     :                                  slupf[solnum] * csl[solnum][j] * qr[j][i] / 10.0 * _dt
                //   /
                // ppm -> kg/ha
                //        this doesn't make sense....g%csl has already been changed from it
                //        was at the start of the timestep.  need to find a different way
                //        of calculating it.  what about qsl???
                //        or try getting g%csl at start of timestep.
                //        BUT NUMBERS DO ADD UP OK????? does he then update at start of next
                //        timestep??????? !!
            }
        }

        /// <summary>
        /// Slupfs the specified crop.
        /// </summary>
        /// <param name="crop">The crop.</param>
        /// <param name="solnum">The solnum.</param>
        /// <returns></returns>
        private double Slupf(int crop, int solnum)
        {
            return 0.0;
        }

        /// <summary>
        /// Covers the eos redn.
        /// </summary>
        /// <returns></returns>
        private double CoverEosRedn()
        {
            //+  Purpose
            //      Calculate reduction in potential soil evaporation
            //      due to residues on the soil surface.
            //      Approach taken from directly from Soilwat code.

            //---------------------------------------+
            // reduce Eo to that under plant CANOPY                    <DMS June 95>
            //---------------------------------------+

            //  Based on Adams, Arkin & Ritchie (1976) Soil Sci. Soc. Am. J. 40:436-
            //  Reduction in potential soil evaporation under a canopy is determined
            //  the "% shade" (ie cover) of the crop canopy - this should include g%th
            //  green & dead canopy ie. the total canopy cover (but NOT near/on-grou
            //  residues).  From fig. 5 & eqn 2.                       <dms June 95>
            //  Default value for c%canopy_eos_coef = 1.7
            //              ...minimum reduction (at cover =0.0) is 1.0
            //              ...maximum reduction (at cover =1.0) is 0.183.

            double eos_canopy_fract = Math.Exp(-canopy_eos_coef * crop_cover);

            //-----------------------------------------------+
            // reduce Eo under canopy to that under mulch            <DMS June 95>
            //-----------------------------------------------+

            //1a. adjust potential soil evaporation to account for
            //    the effects of surface residue (Adams et al, 1975)
            //    as used in Perfect
            // BUT taking into account that residue can be a mix of
            // residues from various crop types <dms june 95>

            //    [DM. Silburn unpublished data, June 95 ]
            //    <temporary value - will reproduce Adams et al 75 effect>
            //     c%A_to_evap_fact = 0.00022 / 0.0005 = 0.44

            double eos_residue_fract = Math.Pow(1.0 - residue_cover, a_to_evap_fact);


            return eos_canopy_fract * eos_residue_fract;
        }

        /// <summary>
        /// Watvars the specified ix.
        /// </summary>
        /// <param name="ix">The ix.</param>
        /// <param name="tp">The tp.</param>
        /// <param name="tpsi">The tpsi.</param>
        /// <param name="psip">The psip.</param>
        /// <param name="psipp">The psipp.</param>
        /// <param name="tth">The TTH.</param>
        /// <param name="thp">The THP.</param>
        /// <param name="thk">The THK.</param>
        /// <param name="hkp">The HKP.</param>
        private void Watvar(int ix, double tp, out double tpsi, out double psip, out double psipp, out double tth, ref double thp, out double thk, ref double hkp)
        {
            //     Short Description:
            //     calculates water variables from transform value g%p at grid point ix
            //     using cubic interpolation between given values of water content p%wc,
            //     log10 conductivity p%hkl, and their derivatives p%wcd, p%hkld with respect
            //     to log10 suction p%sl
            //
            //     nih - some local variables had the same name as globals so I had
            //     to rename them. I added a g%t (for temp) to start of name for
            //     g%psi, g%hk, g%p, g%th, p%x, p%dx,g%dc

            //     notes

            //         dTheta     dTheta       d(log g%psi)
            //         ------ = ----------  p%x  ---------
            //           dP     d(log g%psi)        d g%p

            //                    dTheta        d g%psi           1
            //                = ----------  p%x  -------  p%x ------------
            //                  d(log g%psi)       d g%p       ln(10).g%psi


            //         dHK          dHK       d(log g%psi)
            //        ------  = ----------  p%x ----------
            //          dP      d(log g%psi)       d g%p

            //                   ln(10).g%hk   d(log(g%hk))     dPsi        1
            //                =  --------- p%x ----------  p%x ------ p%x ----------
            //                        1      d(log(g%psi))     dP     ln(10).g%psi

            //                    g%hk       d(log(g%hk))     dPsi
            //                =  -----  p%x  ----------  p%x  ----
            //                    g%psi      d(log(g%psi))     dP

            //     note:- d(log(y)/p%dx) = 1/y . dy/p%dx
            //
            //     Therefore:-
            //
            //            d(log10(y))/p%dx = d(ln(y)/ln(10))/p%dx
            //                           = 1/ln(10) . d(ln(y))/p%dx
            //                           = 1/ln(10) . 1/y . dy/p%dx


            //     Constant Values
            const double al10 = 2.3025850929940457;
            const double vcon1 = 7.28e-9;
            const double vcon2 = 7.26e-7;

            double thd, hklg, hklgd;

            Trans(tp, out tpsi, out psip, out psipp);

            Interp(ix, tpsi, out tth, out thd, out hklg, out hklgd);

            thk = Math.Exp(al10 * hklg);

            if (tpsi != 0.0)
            {
                thp = (thd * psip) / (al10 * tpsi);
                hkp = (thk * hklgd * psip) / tpsi;
            }

            double thsat = _sat[ix];  // NOTE: this assumes that the wettest p%wc is
            //! first in the pairs of log suction vs p%wc

            // EJZ - this was in the fortran source, but is clearly futile
            //if (thsat == 0.0)       
            //    thsat = _sat[ix];

            if (ivap)
            {
                //        add vapour conductivity hkv
                double phi = thsat / 0.93 - tth;
                double hkv = vcon1 * phi * Math.Exp(vcon2 * tpsi);
                thk = thk + hkv;
                hkp = hkp + hkv * (vcon2 * psip - thp / phi);
            }
        }

        /// <summary>
        /// Transes the specified p.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="psi">The psi.</param>
        /// <param name="psip">The psip.</param>
        /// <param name="psipp">The psipp.</param>
        private void Trans(double p, out double psi, out double psip, out double psipp)
        {
            //     Short description:
            //     gets psi and its partial derivitives

            // Constants
            const double psi_0 = -50e0;
            const double psi_1 = psi_0 / 10.0;

            if (p < 0.0)
            {
                double ep = Math.Exp(p);
                double emp = 1.0 / ep;
                double sinhp = 0.5 * (ep - emp);
                double coshp = 0.5 * (ep + emp);
                double v = psi_1 * sinhp;
                psi = psi_0 - v;
                psip = -psi_1 * coshp;
                psipp = -v;
            }
            else
            {
                psi = psi_0 - psi_1 * p;
                psip = -psi_1;
                psipp = 0.0;
            }
        }

        /// <summary>
        /// Thomases the specified istart.
        /// </summary>
        /// <param name="istart">The istart.</param>
        /// <param name="n">The n.</param>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <param name="rhs">The RHS.</param>
        /// <param name="d">The d.</param>
        /// <param name="v">The v.</param>
        /// <param name="pondingData">The ponding data.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        private void Thomas(int istart, int n, ref double[] a, ref double[] b, ref double[] c, ref double[] rhs, ref double[] d, ref double[] v, ref PondingData pondingData, out bool fail)
        {
            //     Short description:
            //     Thomas algorithm for solving tridiagonal system of eqns

            fail = true; // Indicate failure if we return early

            double piv = b[istart];
            if (istart == -1)
                piv = pondingData.b;
            if (piv == 0.0)
                return;
            if (istart == -1)
                pondingData.v = pondingData.rhs / piv;
            else
                v[istart] = rhs[istart] / piv;
            for (int i = istart + 1; i < istart + n; i++)
            {
                if (i == 0)
                    d[i] = pondingData.c / piv;
                else
                    d[i] = c[i - 1] / piv;
                piv = b[i] - a[i] * d[i];
                if (piv == 0.0)
                    return;
                if (i == 0)
                    v[i] = (rhs[i] - a[i] * pondingData.v) / piv;
                else
                    v[i] = (rhs[i] - a[i] * v[i - 1]) / piv;
            }

            for (int i = istart + n - 2; i >= istart; i--)
            {
                if (i == -1)
                    pondingData.v = pondingData.v - d[i + 1] * v[i + 1];
                else
                    v[i] = v[i] - d[i + 1] * v[i + 1];
            }

            fail = false;
        }

        // Variables used only in Baleq, but which need to be saved across invocations
        /// <summary>
        /// The ifirst
        /// </summary>
        private int ifirst = 0;
        /// <summary>
        /// The ilast
        /// </summary>
        private int ilast = 0;
        /// <summary>
        /// The gr
        /// </summary>
        private double gr = 0.0;

        /// <summary>
        /// Baleqs the specified it.
        /// </summary>
        /// <param name="it">It.</param>
        /// <param name="iroots">The iroots.</param>
        /// <param name="tslos">The tslos.</param>
        /// <param name="tcsl">The TCSL.</param>
        /// <param name="ibegin">The ibegin.</param>
        /// <param name="iend">The iend.</param>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <param name="rhs">The RHS.</param>
        /// <param name="pondingData">The ponding data.</param>
        private void Baleq(int it, ref int iroots, ref double[] tslos, ref double[][] tcsl, out int ibegin, out int iend, ref double[] a, ref double[] b, ref double[] c, ref double[] rhs, ref PondingData pondingData)
        {
            //     Short Description:
            //     gets coefficient matrix and rhs for Newton soln of balance eqns
            //
            //     Some variables had the same name as some global variables and so
            //     these were renamed (by prefixing with g%t - for temp)
            //     this include p%isol, g%csl, p%slos

            const double hcon = 7.0e-7;
            const double hair = 0.5;

            double[] psip = new double[n + 1];
            double[] psipp = new double[n + 1];
            double[] thp = new double[n + 1];
            double[] hkp = new double[n + 1];
            double[] qsp = new double[n + 1];
            double[] qp1 = new double[n + 2];
            double[] qp2 = new double[n + 2];
            double[] psios = new double[n + 1];
            double[,] qexp = new double[3, n + 1];
            double[] qdrain = new double[n + 1];
            double[] qdrainpsi = new double[n + 1];
            double[] qssofp = new double[n + 1];
            double v1;

            //   initialise for first iteration
            if (it == 1)
            {
                ifirst = 0;
                ilast = n;
                if (itbc == 2 && hold > 0.0)
                    ifirst = -1;
                if (ibbc == 0)
                    gr = bbc_value;

                if (ibbc == 1)
                {
                    _psi[n] = bbc_value;
                    _p[n] = Pf(_psi[n]);
                }
            }

            //   get soil water variables and their derivatives
            for (int i = 0; i <= n; i++)
                Watvar(i, _p[i], out _psi[i], out psip[i], out psipp[i], out th[i], ref thp[i], out hk[i], ref hkp[i]);

            //   check boundary potls
            if (itbc == 0 && isbc == 0 && _psi[0] > 0.0)
            {
                //        infinite conductance and no ponding allowed
                _psi[0] = 0.0;
                _p[0] = Pf(_psi[0]);
                Watvar(0, _p[0], out v1, out psip[0], out psipp[0], out th[0], ref thp[0], out hk[0], ref hkp[0]);
            }
            if (ibbc == 3 && _psi[n] > bbc_value)
            {
                //        seepage at bottom boundary
                _psi[n] = bbc_value;
                _p[n] = Pf(_psi[n]);
                Watvar(n, _p[n], out v1, out psip[n], out psipp[n], out th[n], ref thp[n], out hk[n], ref hkp[n]);
            }

            //   get fluxes between nodes
            double absgf = Math.Abs(gf);
            double w1, w2;
            double deltap;
            double deltax;
            double skd;
            double hkdp1;
            double hkdp2;
            double hsoil;
            for (int i = 1; i <= n; i++)
            {
                if (!MathUtilities.FloatsAreEqual(x[i - 1], x[i]))
                {
                    deltax = x[i] - x[i - 1];
                    deltap = _p[i] - _p[i - 1];
                    double hkd1 = hk[i - 1] * psip[i - 1];
                    double hkd2 = hk[i] * psip[i];
                    hkdp1 = hk[i - 1] * psipp[i - 1] + hkp[i - 1] * psip[i - 1];
                    hkdp2 = hk[i] * psipp[i] + hkp[i] * psip[i];
                    skd = hkd1 + hkd2;
                    if (swt >= 0.5 && swt <= 1.0)
                    {
                        //              use fixed space weighting on gravity flow
                        w1 = MathUtilities.Sign(2.0 * swt, gf);
                    }
                    else
                    {
                        //              use central diffs for gravity flow if possible, else use
                        //                 just enough upstream weighting to avoid instability
                        //                 user may increase acceptable level for central diffs
                        //                 by setting p%swt < -1
                        double accept = Math.Max(1.0, -swt);
                        double wt = 0.0;
                        //               if(absgf.ne.0..and.hkp(i).ne.0.)then
                        double gfhkp = gf * hkp[i];
                        if (gfhkp != 0.0)
                        {
                            if (it == 1)
                            {
                                //                     value=1.-accept*(skd+(g%p(i)-g%p(i-1))*hkdp2)/(absgf*deltax*hkp(i))
                                double value = 1.0 - accept * (skd) / (Math.Abs(gfhkp) * deltax);
                                //                     value=min(1d0,value)
                                swta[i] = MathUtilities.Sign(Math.Max(0.0, value), gfhkp);
                            }
                            wt = swta[i];
                        }

                        w1 = 1.0 + wt;
                    }
                    w2 = 2.0 - w1;

                    if ((w1 > 2.0) || (w1 < 0.0))
                        IssueWarning("bad space weighting factor");

                    q[i] = -0.5 * (skd * deltap / deltax - gf * (w1 * hk[i - 1] + w2 * hk[i]));
                    qp1[i] = -0.5 * ((hkdp1 * deltap - skd) / deltax - gf * w1 * hkp[i - 1]);
                    qp2[i] = -0.5 * ((hkdp2 * deltap + skd) / deltax - gf * w2 * hkp[i]);

                    _swf[i] = w1;
                }
            }
            //   get fluxes to storage
            for (int i = 0; i <= n; i++)
            {
                qs[i] = (th[i] - thold[i]) * dx[i] / _dt;
                qsp[i] = thp[i] * dx[i] / _dt;
            }
            //   get uptake fluxes to roots if still in iterations
            if (iroots < 2)
            {
                for (int i = 0; i <= n; i++)
                {
                    psios[i] = _psi[i];
                    for (int solnum = 0; solnum < num_solutes; solnum++)
                        psios[i] = psios[i] - tslos[solnum] * tcsl[solnum][i];
                }
                Uptake(ref psios, ref hk, ref psip, ref hkp, ref qex, ref qexp);
            }
            rex = 0.0;
            for (int i = 0; i <= n; i++)
                rex = rex + qex[i];

            //   NIH  get subsurface fluxes
            Drain(out qdrain, out qdrainpsi);

            rssf = 0.0;
            for (int i = 0; i <= n; i++)
            {
                qssif[i] = SubSurfaceInFlow[i] / 10.0 / 24.0; // assumes mm and daily timestep - need something better !!!!
                qssof[i] = qdrain[i]; // Add outflow calc here later
                qssofp[i] = qdrainpsi[i] * psip[i];
                rssf += qssif[i] - qssof[i];
            }

            //   get soil surface fluxes, taking account of top boundary condition
            //   
            double respsi;
            double roffd;
            if (itbc == 0)
            {
                //       infinite conductance
                ifirst = 0;
                if (_psi[0] < 0.0)
                {
                    hsoil = Math.Max(hair, Math.Exp(hcon * _psi[0]));
                    res = resp * (hsoil - hair) / (1.0 - hair);
                    respsi = resp * hcon * hsoil / (1.0 - hair);
                }
                else
                {
                    res = resp;
                    respsi = 0.0;
                }

                if (isbc == 0)
                {
                    //           no ponding allowed
                    _h = 0.0;
                    double q0 = ron - res + hold / _dt;

                    if (_psi[0] < 0.0 || q0 < qs[0] + qex[0] + q[1] - qssif[0] + qssof[0])
                    {
                        q[0] = q0;
                        qp2[0] = -respsi * psip[0];
                        roff = 0.0;
                        roffd = 0.0;
                    }
                    else
                    {
                        //              const zero potl
                        ifirst = 1;
                        q[0] = qs[0] + qex[0] + q[1] - qssif[0] + qssof[0];
                        roff = q0 - q[0];
                        roffd = -qp2[1];
                    }
                }
                else
                {
                    //           runoff zero or given by a function
                    if (_psi[0] < 0.0)
                    {
                        _h = 0.0;
                        roff = 0.0;
                        q[0] = ron - res + hold / _dt;
                        qp2[0] = -respsi * psip[0];
                    }
                    else
                    {
                        _h = _psi[0];
                        roff = 0.0;
                        roffd = 0.0;
                        if (isbc == 2)
                            Runoff(t, _h, out roff, out roffd);
                        q[0] = ron - roff - res - (_h - hold) / _dt;
                        qp2[0] = (-roffd - respsi - 1.0 / _dt) * psip[0];
                    }
                }
            }

            if (itbc == 1)
            {
                //       const potl
                ifirst = 1;
                if (_psi[0] < 0.0)
                {
                    hsoil = Math.Exp(hcon * _psi[0]);
                    res = resp * (hsoil - hair) / (1.0 - hair);
                }
                else
                {
                    res = resp;
                }
                _h = Math.Max(_psi[0], 0.0);
                q[0] = qs[0] + qex[0] + q[1] - qssif[0] + qssof[0];
                //        flow to source of potl treated as "runoff" (but no bypass flow)
                roff = ron - res - (_h - hold) / _dt - q[0];
            }
            else if (itbc == 2)
            {
                //       conductance given by a function
                double g_, gh;
                double q0 = ron - resp + hold / _dt;
                if (isbc == 0)
                {
                    //           no ponding allowed
                    ifirst = 0;
                    _h = 0.0;
                    SCond(t, _h, out g_, out gh);

                    if (q0 > -g_ * _psi[0])
                    {
                        res = resp;
                        respsi = 0.0;
                        q[0] = -g_ * _psi[0];
                        qp2[0] = -g_ * psip[0];
                        roff = q0 - q[0];
                        roffd = -qp2[0];
                    }
                    else
                    {
                        hsoil = Math.Exp(hcon * _psi[0]);
                        res = resp * (hsoil - hair) / (1.0 - hair);
                        respsi = resp * hcon * hsoil / (1.0 - hair);
                        q0 = ron - res + hold / _dt;
                        q[0] = q0;
                        qp2[0] = -respsi * psip[0];
                        roff = 0.0;
                    }
                }
                else
                {
                    //           runoff zero or given by a function
                    SCond(t, _h, out g_, out gh);
                    if (q0 > -g_ * _psi[0])
                    {
                        //              initialise _h if necessary
                        if (ifirst == 0)
                            _h = Math.Max(_psi[0], 0.0);
                        ifirst = -1;
                        res = resp;
                        roff = 0.0;
                        roffd = 0.0;
                        if (isbc == 2 && _h > 0.0)
                            Runoff(t, _h, out roff, out roffd);
                        q[0] = g_ * (_h - _psi[0]);
                        qp1[0] = g_ + gh * (_h - _psi[0]);
                        qp2[0] = -g_ * psip[0];
                        // WE MAY NEED TO HANDLE THE -1 INDICES SOMEHOW (though I'm not sure they are ever used)
                        pondingData.rhs = -(ron - roff - res - q[0] - (_h - hold) / _dt);
                        pondingData.b = -roffd - qp1[0] - 1.0 / _dt;
                        pondingData.c = -qp2[0];
                        //rhs[-1] = -(ron - roff - res - q[0] - (_h - hold) / _dt);
                        //b[-1] = -roffd - qp1[0] - 1.0 / _dt;
                        //c[-1] = -qp2[0];
                    }
                    else
                    {
                        ifirst = 0;
                        _h = 0.0;
                        roff = 0.0;
                        hsoil = Math.Exp(hcon * _psi[0]);
                        res = resp * (hsoil - hair) / (1.0 - hair);
                        respsi = resp * hcon * hsoil / (1.0 - hair);
                        q[0] = ron - res + hold / _dt;
                        qp2[0] = -respsi * psip[0];
                    }
                }
            }
            //     bypass flow?
            qbp = 0.0;
            //qbpd = 0.0;
            //double qbpp = 0.0;
            //double qbps = 0.0;
            //double qbpsp = 0.0;

            //   bottom boundary condition
            if (ibbc == 0)
            {
                //       zero matric potl gradient
                q[n + 1] = (gf + gr) * hk[n];
                qp1[n + 1] = (gf + gr) * hkp[n];
            }
            else if (ibbc == 1)
            {
                //       const potl
                ilast = n - 1;
                q[n + 1] = q[n] - qs[n] - qex[n] + qssif[n] - qssof[n];
            }
            else if (ibbc == 2)
            {
                //       zero flux
                q[n + 1] = 0.0;
                qp1[n + 1] = 0.0;
            }
            else if (ibbc == 3)
            {
                //       seepage
                //nh added to allow seepage to user potential at bbc
                if (_psi[n] >= bbc_value)
                {
                    q[n + 1] = q[n] - qs[n] - qex[n] + qssif[n] - qssof[n];
                    if (q[n + 1] >= 0.0)
                    {
                        ilast = n - 1;
                        //qbpd = 0.0;
                    }
                    else
                    {
                        ilast = n;
                    }
                }
                if (ilast == n)
                {
                    q[n + 1] = 0.0;
                    qp1[n + 1] = 0.0;
                }
            }
            else if (ibbc == 4)
            {
                //       flux calculated according to head difference from water table
                double headdiff = _psi[n] - x[n] + bbc_value / 10.0;
                q[n + 1] = headdiff * water_table_conductance;
                qp1[n + 1] = psip[n] * water_table_conductance;
            }
            //    get Newton-Raphson equations
            int i1 = Math.Max(ifirst, 0);
            int k = i1 - 1;
            bool xidif = true;
            for (int i = i1; i <= ilast; i++)
            {
                //        allow for two nodes at same depth
                bool xipdif = true;
                if (xidif)
                {
                    k = k + 1;
                    int j = i + 1;
                    //           j is next different node, k is equation
                    if (i > 0 && i < n - 1)
                    {
                        if (MathUtilities.FloatsAreEqual(x[i], x[i + 1]))
                        {
                            xipdif = false;
                            j = i + 2;
                            q[i + 1] = ((x[j] - x[i]) * q[i] + (x[i] - x[i - 1]) * q[j]) / (x[j] - x[i - 1]);
                        }
                    }
                    rhs[k] = -(q[i] - q[j]);
                    a[k] = qp1[i];
                    b[k] = qp2[i] - qp1[j];
                    c[k] = -qp2[j];
                }
                rhs[k] = rhs[k] + qs[i] + qex[i] - qssif[i] + qssof[i];
                b[k] = b[k] - qsp[i] - qssofp[i];

                if (iroots == 0)
                {
                    //            a(k)=a(k)-qexp(1,i)
                    b[k] = b[k] - qexp[1, i];
                    //            c(k)=c(k)-qexp(3,i)
                }
                else
                {
                    iroots = 2;
                }
                xidif = xipdif;
            }

            ibegin = ifirst;
            iend = k;
        }

        /// <summary>
        /// ses the cond.
        /// </summary>
        /// <param name="ttt">The TTT.</param>
        /// <param name="tth">The TTH.</param>
        /// <param name="g_">The g_.</param>
        /// <param name="gh">The gh.</param>
        private void SCond(double ttt, double tth, out double g_, out double gh)
        {
            //     Short Description:
            //     gets soil surface conductance g and derivative gh
            //
            //     g%t was renamed to ttt as g%t already exists in common
            //     g%h was renamed to tth as g%h already exists in common
            g_ = gsurf;
            gh = 0.0;
        }

        /// <summary>
        /// Runoffs the specified t.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <param name="h">The h.</param>
        /// <param name="roff">The roff.</param>
        /// <param name="roffh">The roffh.</param>
        private void Runoff(double t, double h, out double roff, out double roffh)
        {
            //     Short Description:
            //     gets runoff rate

            if (h > _hmin)
            {
                double v = roff0 * Math.Pow(h - _hmin, roff1 - 1.0);
                roff = v * (h - _hmin);
                roffh = roff1 * v;
            }
            else
            {
                roff = 0.0;
                roffh = 0.0;
            }
        }

        /// <summary>
        /// Uptakes the specified tpsi.
        /// </summary>
        /// <param name="tpsi">The tpsi.</param>
        /// <param name="thk">The THK.</param>
        /// <param name="tpsip">The tpsip.</param>
        /// <param name="thkp">The THKP.</param>
        /// <param name="tqex">The tqex.</param>
        /// <param name="tqexp">The tqexp.</param>
        private void Uptake(ref double[] tpsi, ref double[] thk, ref double[] tpsip, ref double[] thkp, ref double[] tqex, ref double[,] tqexp)
        {
            //     gets flow rates to roots and total water extraction rates
            //
            //     set root conductance gr (alter as required)
            //      double precision gr  ! cm/g%h
            //      parameter (gr=1.4d-7)

            //
            for (int i = 0; i <= n; i++)
            {
                tqex[i] = 0.0;
                qexpot[i] = 0.0;
                for (int j = 0; j < 3; j++)
                    tqexp[j, i] = 0.0;
                //nh
                for (int k = 0; k < nveg; k++)
                {
                    qr[i][k] = 0.0;
                    qrpot[i][k] = 0.0;
                }
                //nh
            }

            double[] g_ = new double[n + 1];

            for (int iveg = 0; iveg < nveg; iveg++)
            {
                //        find transpiration rates
                rt[iveg] = 0.0;
                double ttr = rtp[iveg];
                if (ttr > 0.0)
                {
                    double psix = psimin[iveg];
                    //           get soil->xylem conductances
                    double a = 0.0;
                    double b = 0.0;
                    bool stress;
                    for (int i = 0; i <= n; i++)
                    {
                        g_[i] = 0.0;
                        if (tpsi[i] > psix)
                        {
                            //nh root conductance is not an input
                            //nh                  g(i)=1./(g%rc(i,iveg)/thk(i)+1./(gr*g%rld(i,iveg)*p%dx(i)))
                            g_[i] = 1.0 / (rc[i][iveg] / thk[i] + 1.0 / (RootConductance[i][iveg] * rld[i][iveg] * dx[i]));
                        }
                        a = a + g_[i] * tpsi[i];
                        b = b + g_[i];
                    }
                    if (b == 0.0)
                        stress = true;
                    else if ((a - ttr) / b < psix)
                        stress = true;
                    else
                        stress = false;
                    if (!stress)
                    {
                        //              get xylem potl
                        bool change = true;
                        while (change)
                        {
                            change = false;
                            psix = (a - ttr) / b;
                            for (int i = 0; i <= n; i++)
                            {
                                if (tpsi[i] < psix && g_[i] != 0.0)
                                {
                                    change = true;
                                    a = a - g_[i] * tpsi[i];
                                    b = b - g_[i];
                                    g_[i] = 0.0;
                                }
                            }
                        }
                    }

                    if (_psix[iveg] > psix)
                        _psix[iveg] = psix;

                    for (int i = 0; i <= n; i++)
                    {
                        if (g_[i] != 0.0)
                        {
                            double tq = g_[i] * (tpsi[i] - psix);
                            tqex[i] = tqex[i] + tq;
                            //                 get partial derivs of tqex at i-1, i, i+1 wrt g%p
                            double qpsi = g_[i];
                            double qhk = g_[i] * rc[i][iveg] * tq / (thk[i] * thk[i]);
                            if (!stress)
                            {
                                double derp = qpsi * tpsip[i] + qhk * thkp[i];
                                if (i > 0)
                                    tqexp[2, i - 1] = tqexp[2, i - 1] - g_[i - 1] * derp / b;
                                if (i < n)
                                    tqexp[0, i + 1] = tqexp[0, i + 1] - g_[i + 1] * derp / b;
                                qpsi = qpsi * (1.0 - g_[i] / b);
                                qhk = qhk * (1.0 - g_[i] / b);
                            }
                            tqexp[1, i] = tqexp[1, i] + qpsi * tpsip[i] + qhk * thkp[i];
                            rt[iveg] = rt[iveg] + tq;
                            qr[i][iveg] = tq;
                        }
                        else
                        {
                            qr[i][iveg] = 0.0;
                        }
                        if (ttr > 0)
                        {
                            qrpot[i][iveg] = g_[i] * (tpsi[i] - psimin[iveg]);
                            qexpot[i] = qexpot[i] + qrpot[i][iveg];
                        }
                        else
                        {
                            qrpot[i][iveg] = 0.0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Drains the specified qdrain.
        /// </summary>
        /// <param name="qdrain">The qdrain.</param>
        /// <param name="qdrainpsi">The qdrainpsi.</param>
        private void Drain(out double[] qdrain, out double[] qdrainpsi)
        {
            //     Short Description:
            //     gets flow rate into drain
            //     All units are mm and days

            //     Constant Values
            const double dpsi = 0.01;

            qdrain = new double[n + 1];
            qdrainpsi = new double[n + 1];
            double wt_above_drain;
            double wt_above_drain2;
            double[] qdrain2 = new double[n + 1];

            if (SwimSubsurfaceDrain != null && 
                !string.IsNullOrEmpty(subsurfaceDrain) && subsurfaceDrain.Trim() == "on")
            {
                int drain_node = FindLayerNo(SwimSubsurfaceDrain.DrainDepth);

                double d = SwimSubsurfaceDrain.ImpermDepth - SwimSubsurfaceDrain.DrainDepth;
                if (_psi[drain_node] > 0)
                    wt_above_drain = _psi[drain_node] * 10.0;
                else
                    wt_above_drain = 0.0;

                double q = Hooghoudt(d, wt_above_drain, SwimSubsurfaceDrain.DrainSpacing,
                                     SwimSubsurfaceDrain.DrainRadius, SwimSubsurfaceDrain.Klat);

                qdrain[drain_node] = q / 10.0 / 24.0;

                if (_psi[drain_node] + dpsi > 0.0)
                    wt_above_drain2 = (_psi[drain_node] + dpsi) * 10.0;
                else
                    wt_above_drain2 = 0.0;

                double q2 = Hooghoudt(d, wt_above_drain2, SwimSubsurfaceDrain.DrainSpacing,
                                      SwimSubsurfaceDrain.DrainRadius, SwimSubsurfaceDrain.Klat);

                qdrain2[drain_node] = q2 / 10.0 / 24.0;

                qdrainpsi[drain_node] = (qdrain2[drain_node] - qdrain[drain_node]) / dpsi;
            }
        }


        /// <summary>
        /// Hooghoudts the specified d.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="m">The m.</param>
        /// <param name="L">The l.</param>
        /// <param name="r">The r.</param>
        /// <param name="Ke">The ke.</param>
        /// <returns></returns>
        private double Hooghoudt(double d, double m, double L, double r, double Ke)
        {
            //  Purpose
            //       Drainage loss to subsurface drain using Hooghoudts drainage equation. (mm/d)


            const double C = 1.0;                 // ratio of flux between drains to flux midway between drains.
            // value of 1.0 usually used as a simplification.
            double de;          // effective d to correct for convergence near the drain. (mm)
            double alpha;       // intermediate variable in de calculation

            if (d / L <= 0)
                de = 0.0;
            else if (d / L < 0.3)
            {
                alpha = 3.55 - 1.6 * (d / L) + 2 * (d / L) * (d / L);
                de = d / (1.0 + d / L * (8.0 / Math.PI * Math.Log(d / r) - alpha));
            }
            else
            {
                de = L * Math.PI / (8.0 * Math.Log(L / r) - 1.15);
            }

            return (8.0 * Ke * de * m + 4 * Ke * m * m) / (C * L * L);
        }

        /// <summary>
        /// Finds the layer no.
        /// </summary>
        /// <param name="depth">The depth.</param>
        /// <returns></returns>
        private int FindLayerNo(double depth)
        {
            // Find the soil layer in which the indicated depth is located
            // NOTE: The returned layer number is 0-based
            // If the depth is not reached, the last element is used
            double depth_cum = 0.0;
            for (int i = 0; i < _dlayer.Length; i++)
            {
                depth_cum = depth_cum + _dlayer[i];
                if (depth_cum >= depth)
                    return i;
            }
            return _dlayer.Length - 1;
        }

        /// <summary>
        /// Issues the warning.
        /// </summary>
        /// <param name="warningText">The warning text.</param>
        private void IssueWarning(string warningText)
        {
            if (summary != null)
                summary.WriteWarning(this, warningText);
            else
                Console.WriteLine(warningText);
        }
    }
}
