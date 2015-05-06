

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Models.Core;
using Models;
using System.Xml.Serialization;
using Models.PMF;
using System.Runtime.Serialization;
using Models.SurfaceOM;

using Models.Soils.SoilWaterBackend;
using Models.Interfaces;


namespace Models.Soils
{

    /// <summary>
    /// .NET port of the Fortran SoilWat model
    /// Ported by Shaun Verrall Mar 2011
    /// Extended by Eric Zurcher Mar 2012
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class SoilWater : Model
    {


        //GUI
        //***

        #region Needed for the GUI's "SoilWater" node (DO NOT USE THESE VARIABLES DIRECTLY)




        //SOIL "Properties" (NOT LAYERED)
        
        //To use these variables you should use Soil.SoilWater.VariableName (eg. Soil.SoilWater.SummerCona)


        //Evaporation

        //different evap for summer and winter
        //summer

        /// <summary>
        /// Gets or sets the summer date.
        /// </summary>
        /// <value>
        /// The summer date.
        /// </value>
        [Description("Start Date for Summer evaporation (dd-mmm)")]
        public string SummerDate { get; set; }

        /// <summary>
        /// Gets or sets the summer u.
        /// </summary>
        /// <value>
        /// The summer u.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 40.0)]
        [Units("mm")]
        [Description("Summer: Cummulative Evap for end of stage 1 soil evaporation (U)")]
        public double SummerU { get; set; }

        /// <summary>
        /// Gets or sets the summer cona.
        /// </summary>
        /// <value>
        /// The summer cona.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Description("Summer: Stage 2 drying coefficient (Cona)")]
        public double SummerCona { get; set; }




        //winter

        /// <summary>
        /// Gets or sets the winter date.
        /// </summary>
        /// <value>
        /// The winter date.
        /// </value>
        [Description("Start Date for Winter evaporation (dd-mmm)")]
        public string WinterDate { get; set; }

        /// <summary>
        /// Gets or sets the winter u.
        /// </summary>
        /// <value>
        /// The winter u.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm")]
        [Description("Winter: Cummulative Evap for end of stage 1 soil evaporation (U)")]
        public double WinterU { get; set; }

        /// <summary>
        /// Gets or sets the winter cona.
        /// </summary>
        /// <value>
        /// The winter cona.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Description("Winter: Stage 2 drying coefficient (Cona)")]
        public double WinterCona { get; set; }





        /// <summary>
        /// Gets or sets the diffus constant.
        /// </summary>
        /// <value>
        /// The diffus constant.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Description("Diffusivity constant for soil texture")]
        public double DiffusConst { get; set; }

        /// <summary>
        /// Gets or sets the diffus slope.
        /// </summary>
        /// <value>
        /// The diffus slope.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Description("Diffusivity slope for diffusivity/soil water content relationship")]
        public double DiffusSlope { get; set; }

        /// <summary>
        /// Gets or sets the salb.
        /// </summary>
        /// <value>
        /// The salb.
        /// </value>
        [Description("Bare soil albedo")]
        public double Salb { get; set; }



        //Runoff


        /// <summary>
        /// Gets or sets the c n2 bare.
        /// </summary>
        /// <value>
        /// The c n2 bare.
        /// </value>
        [Bounds(Lower = 1.0, Upper = 100.0)]
        [Units("0-100")]
        [Description("Runoff Curve Number of bare soil with average moisture (cn2_bare)")]
        public double CN2Bare { get; set; }

        /// <summary>
        /// Gets or sets the cn red.
        /// </summary>
        /// <value>
        /// The cn red.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Units("0-100")]
        [Description("Maximum reduction in cn2_bare due to cover (cn_red)")]
        public double CNRed { get; set; }

        /// <summary>
        /// Gets or sets the cn cov.
        /// </summary>
        /// <value>
        /// The cn cov.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Cover at which cn_red occurs (cn_cov)")]
        public double CNCov { get; set; }



        //Lateral flow

        /// <summary>
        /// Gets or sets the slope.
        /// </summary>
        /// <value>
        /// The slope.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Lateral Flow: Slope of Catchment Area")]
        public double slope { get; set; }

        /// <summary>
        /// Gets or sets the discharge_width.
        /// </summary>
        /// <value>
        /// The discharge_width.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0e8F)]     //1.0e8F = 100000000
        [Units("m")]
        [Description("Lateral Flow: Basal width of downslope boundary of the catchment")]
        public double discharge_width { get; set; }

        /// <summary>
        /// Gets or sets the catchment_area.
        /// </summary>
        /// <value>
        /// The catchment_area.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0e8F)]     //1.0e8F = 100000000
        [Units("m^2")]
        [Description("Lateral Flow: Catchment Area")]
        public double catchment_area { get; set; }



        //Ponding

        /// <summary>
        /// Gets or sets the max_pond.
        /// </summary>
        /// <value>
        /// The max_pond.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        [Description("Maximum Ponding Depth (eg. Rice Paddy)")]
        public double max_pond { get; set; }






        // SOIL "Profile" (LAYERED)

        //To see these variables you can use Soil.VariableName (eg. Soil.SWCON)

        //Adding variables below will automatically add them to Soil.SoilWater.VariableName
        //BUT you will also need to add them to Soil.cs and use the layer converter functions, so that you can then use them by Soil.VariableName

        //Do not use these by using Soil.SoilWater, because the layer thicknesses under the SoilWater node can be in a different layer thicknesses to the standard layer thicknesses.
        //Instead use Soil.VariableName and access them from the top level 

        // Variables with different layer structure
        //------------------------------------------
        // The 4 variables below (Thickness, SWCON, MWCON and KLAT) may be specified in the GUI in different layer thicknesses 
        // to the standardised thicknesses used in the top level Soil.
        // So instead of using these 4 variables you should ask the top level Soil for the value of SWCON, MWCON etc as they will then be mapped into a standardised layer thicknesses.
        // The "standardised" layer thicknesses in the top level Soil uses the layer thicknesses specified in the "Water" node of the GUI (aka. sub class Soil.SoilWater.SWmm).

        // nb. To see the "Water" node values from the GUI use Soil.Water.VariableName  (or Link to it) (eg. Soil.Water.SAT), 
        //     to see these variables below from "SoilWater" node use Soil.SoilWater.VariableName  (eg.Soil.SoilWater.SWCON)
        //     to ask the Soil for the variables for these values below use Soil.VariableName (eg. Soil.SWCON which is mapped to standard layer thicknesses)


        /// <summary>
        /// Thicknesses specified in "SoilWater" node of GUI (in mm as double).
        /// This is the NON standard layer thickness.
        /// </summary>
        /// <value>
        /// The thickness.
        /// </value>
        public double[] Thickness { get; set; }



        /// <summary>
        /// This "Depth" variable is only needed for the "Depth" column in the "SoilWater" node of the GUI.
        /// Just converts back and forth between "Depth" (in cm as string) AND "Thickness" (in mm as double).
        /// </summary>
        /// <value>
        /// The depth.
        /// </value>
        [XmlIgnore]
        [Units("cm")]
        [Description("Depth")]
        public string[] Depth
            {
            get
                {
                return Soil.ToDepthStrings(Thickness);
                }
            set
                {
                Thickness = Soil.ToThickness(value);
                }
            }



        /// <summary>
        /// Between (SAT and DUL) soil water conductivity constant for each soil layer.
        /// At thicknesses specified in "SoilWater" node of GUI.
        /// Use Soil.SWCON for SWCON in standard thickness
        /// </summary>
        /// <value>
        /// The swcon.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/d")]
        [Description("SWCON")]
        public double[] SWCON { get; set; }


        /// <summary>
        /// Lateral flow soil water conductivity constant for each soil layer.
        /// At thicknesses specified in "SoilWater" node of GUI.
        /// Use Soil.KLAT for KLAT in standard thickness
        /// </summary>
        /// <value>
        /// The klat.
        /// </value>
        [Bounds(Lower = 0, Upper = 1.0e3F)] //1.0e3F = 1000
        [Units("mm/d")]
        [Description("KLAT")]
        public double[] KLAT { get; set; }



        #endregion






        //MODEL
        //*****


        #region Links

        /// <summary>
        /// The clock
        /// </summary>
        [Link]
        private Clock Clock = null;


        /// <summary>
        /// The weather
        /// </summary>
        [Link]
        private IWeather Weather = null;


        ////needed for "interception"
        //[Link]
        //MicroClimate MicroClimate;


        /// <summary>
        /// The soil
        /// </summary>
        [Link]
        private Soil Soil = null;


        ////Gives you access to the "Water" tab of the GUI. Don't use this, Use top level Soil to get values in the Default layer structure.
        //[Link]
        //private Water Water = null; 


        //Paddock is needed to find all the Crops so you can work out Canopy data.
        /// <summary>
        /// The paddock
        /// </summary>
        [Link]
        private Simulation paddock = null;


        //Needed for SurfaceCover
        /// <summary>
        /// The surface om
        /// </summary>
        [Link]
        SurfaceOrganicMatter SurfaceOM = null;


        /// <summary>
        /// The summary
        /// </summary>
        [Link]
        ISummary Summary = null;



        #endregion



        #region Module Constants (Default Values) (NOT specified in GUI)

        /// <summary>
        /// Temperature below which eeq decreases
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("oC")]
        public double min_crit_temp { get; set; }


        /// <summary>
        /// Temperature above which eeq increases
        /// </summary>
        /// <value>
        /// The max_crit_temp.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 50.0)]
        [Units("oC")]
        public double max_crit_temp { get; set; }


        /// <summary>
        /// Maximum bare ground soil albedo
        /// </summary>
        /// <value>
        /// The max_albedo.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double max_albedo { get; set; }


        /// <summary>
        /// Factor to convert 'A' to coefficient in Adam's type residue effect on Eos
        /// </summary>
        /// <value>
        /// The a_to_evap_fact.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double A_to_evap_fact { get; set; }


        /// <summary>
        /// Coefficient in cover Eos reduction equation
        /// </summary>
        /// <value>
        /// The canopy_eos_coef.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("0-10")]
        public double canopy_eos_coef { get; set; }


        /// <summary>
        /// Critical sw ratio in top layer below which stage 2 evaporation occurs
        /// </summary>
        /// <value>
        /// The sw_top_crit.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double sw_top_crit { get; set; }


        /// <summary>
        /// Upper limit of sumes1
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        public double sumes1_max { get; set; }


        /// <summary>
        /// Upper limit of sumes2
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        public double sumes2_max { get; set; }


        /// <summary>
        /// Efficiency of moving solute with unsaturated flow
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] solute_flow_eff { get; set; }


        /// <summary>
        /// Efficiency of moving solute with flux (saturated flow)
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] solute_flux_eff { get; set; }


        /// <summary>
        /// Gradient due to hydraulic differentials
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double gravity_gradient { get; set; }


        /// <summary>
        /// Specific bulk density
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("g/cm^3")]
        public double specific_bd { get; set; }


        /// <summary>
        /// Hydrologically effective depth for runoff
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 1.0, Upper = 1000.0)]
        [Units("mm")]
        public double hydrol_effective_depth { get; set; }


        /// <summary>
        /// Names of all possible mobile solutes
        /// </summary>
        [XmlIgnore]
        public string[] mobile_solutes { get; set; }


        /// <summary>
        /// Names of all possible immobile solutes
        /// </summary>
        [XmlIgnore]
        public string[] immobile_solutes { get; set; }


        /// <summary>
        /// Canopy factors for cover runoff effect
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] canopy_fact { get; set; }


        /// <summary>
        /// Heights for canopy factors
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 100000.0)]
        [Units("mm")]
        public double[] canopy_fact_height { get; set; }


        /// <summary>
        /// Default canopy factor in absence of height
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double canopy_fact_default { get; set; }


        /// <summary>
        /// Actual soil evaporation model being used
        /// </summary>
        [XmlIgnore]
        public string act_evap_method { get; set; }  
 


        #endregion



        #region Manager Variables (Default Values) (can be altered in Manager Code) 


        //TODO: put obsrunoff_name and eo_source into "Optional Daily Inputs" secion

        //Two below, store the Apsim variable names that you ask the System to provide by doing a Variables.Get();
        //The response can come from a Met file or Input File or Manager Script.

        //[Description("System variable name of external eo source")]
        //public string eo_source { get; set; }    



        //TODO: turn these into MANAGER COMMANDS by turning them into Set methods. Just like SetMaxPond
        //maybe get rid of the set; and just leave a get; so they are still available as outputs  



        #endregion



        #region Constructor



        /// <summary>
        /// Initializes a new instance of the <see cref="SoilWater"/> class.
        /// </summary>
        public SoilWater()
            {

            //GUI variables (Default values)

            SummerDate = "not_read";
            SummerU = Double.NaN;
            SummerCona = Double.NaN;
            WinterDate = "not_read";
            WinterU = Double.NaN;
            WinterCona = Double.NaN;
            DiffusConst = 40.0;
            DiffusSlope = 16.0;
            Salb = Double.NaN;
            CN2Bare = 73.0;
            CNRed = 20.0;
            CNCov = 0.8;
            slope = Double.NaN;
            discharge_width = Double.NaN;  
            catchment_area = Double.NaN;   
            max_pond = 0.0;





            //Module Constants

            min_crit_temp = 5.0;
            max_crit_temp = 35.0;
            max_albedo = 0.23;
            A_to_evap_fact = 0.44;
            canopy_eos_coef = 1.7;
            sw_top_crit = 0.9;
            sumes1_max = 100;
            sumes2_max = 25;
            solute_flow_eff = new double[] { 1.0 };
            solute_flux_eff = new double[] { 1.0 };
            gravity_gradient = 0.00002;
            specific_bd = 2.65;
            hydrol_effective_depth = 450;
            mobile_solutes = new string[] { "NO3", "urea", "cl", "br", "org_n", "org_c_pool1", "org_c_pool2", "org_c_pool3" };
            immobile_solutes = new string[] { "NH4" };
            canopy_fact = new double[] { 1, 1, 0, 0 };
            canopy_fact_height = new double[] { 0, 600, 1800, 30000 };
            canopy_fact_default = 0.5;
            act_evap_method = "ritchie";




            //Manager Variables

            //eo_source = ""; 


            }




        #endregion



        #region Ouputs (NOT Layered)



        /// <summary>
        /// Effective potential evapotranspiration
        /// </summary>
        /// <value>
        /// The eo.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Eo { get{return surface.Eo;} }

        /// <summary>
        /// Gets the eos.
        /// </summary>
        /// <value>
        /// The eos.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Eos { get{return surface.Eos;} }


        /// <summary>
        /// Total es
        /// </summary>
        /// <value>
        /// The es.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Es { get { return surface.Es; } }

        /// <summary>
        /// time after 2nd-stage soil evaporation begins (d)
        /// </summary>
        /// <value>
        /// The t.
        /// </value>
        [XmlIgnore]
        [Units("d")]
        public double t
            {
            get
                {
                switch (surface.SurfaceType)
                    {
                    case Surfaces.NormalSurface:
                        NormalSurface normal = (NormalSurface)surface;
                        return normal.t;

                    case Surfaces.PondSurface:
                        PondSurface pond = (PondSurface)surface;
                        return pond.t;

                    default:
                        return Double.NaN;
                    }
                }

            }

        /// <summary>
        /// Gets the cn2_new.
        /// </summary>
        /// <value>
        /// The cn2_new.
        /// </value>
        [XmlIgnore]
        public double cn2_new
            {
            get {
                switch (surface.SurfaceType)
                    {
                    case Surfaces.NormalSurface :
                        NormalSurface normal = (NormalSurface) surface;
                        return normal.cn2_new;

                    case Surfaces.PondSurface :
                        PondSurface pond = (PondSurface) surface;
                        return pond.cn2_new;

                    default:
                        return Double.NaN;
                    }     
                }

            }

        /// <summary>
        /// Drainage rate from bottom layer
        /// </summary>
        /// <value>
        /// The drainage.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Drainage { get{return SoilObject.Drainage;} }



        /// <summary>
        /// Drainage rate from bottom layer
        /// </summary>
        /// <value>
        /// The leach n o3.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double LeachNO3 { get { return SoilObject.LeachNO3; } }         //! Leaching from bottom layer (kg/ha) // 

        /// <summary>
        /// Drainage rate from bottom layer
        /// </summary>
        /// <value>
        /// The leach n h4.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double LeachNH4 { get { return SoilObject.LeachNH4; } }         //! Leaching from bottom layer (kg/ha) // 

        /// <summary>
        /// Drainage rate from bottom layer
        /// </summary>
        /// <value>
        /// The leach urea.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double LeachUrea { get { return SoilObject.LeachUrea; } }         //! Leaching from bottom layer (kg/ha) // 




        /// <summary>
        /// Infiltration into top layer
        /// </summary>
        /// <value>
        /// The infiltration.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Infiltration { get{return surface.Infiltration;} }

        /// <summary>
        /// Runoff from top layer
        /// </summary>
        /// <value>
        /// The runoff.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Runoff { get{return surface.Runoff;} }

        /// <summary>
        /// Evaporation from the surface of the pond
        /// </summary>
        /// <value>
        /// The pond_evap.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double pond_evap
            {
            get {
                if (surface.SurfaceType == Surfaces.PondSurface)
                    {
                    PondSurface pond = (PondSurface) surface;
                    return pond.pond_evap;
                    }
                else
                    {
                    return Double.NaN;
                    }     
                }
            }

        /// <summary>
        /// Surface water ponding depth
        /// </summary>
        /// <value>
        /// The pond.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double pond
            {
            get {
                if (surface.SurfaceType == Surfaces.PondSurface)
                    {
                    PondSurface pond = (PondSurface) surface;
                    return pond.pond;
                    }
                else
                    {
                    return Double.NaN;
                    }     
                }
            }

        /// <summary>
        /// Water table depth
        /// (depth below the ground surface of the first saturated layer)
        /// </summary>
        /// <value>
        /// The water table.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double WaterTable
            { 
            get{return SoilObject.DepthToWaterTable;}
            set { SetWaterTable(value); } //TODO: remove this later, have manager scripts directly use SoilWater.SetWaterTable(amount) instead
            }


        /// <summary>
        /// Extractable Soil Water
        /// (sw - ll15) of each layer summed over the profile.
        /// </summary>
        /// <value>
        /// The esw.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double ESW
        { get { return SoilObject.esw; } }





        /// <summary>
        /// Layer that irrigation went into.
        /// Needed for subsurface irrigations because you specify a depth.
        /// It is helpful to see exactly which layer that depth put the irrigation into.
        /// layer = 0 means, top layer but via a surface irrigation (depth = 0) (NOT subsurface)
        /// layer = 1 means, top layer but via a subsurface irrigation
        /// layer &gt; 1 means that layer (one based not zero based)
        /// </summary>
        /// <value>
        /// The irrig layer.
        /// </value>
        [XmlIgnore]
        [Units("")]
        public int IrrigLayer
        { get { return irrig.layer; } }



        #endregion



        #region Outputs (Layered)


        /// <summary>
        /// Gets the dlayer.
        /// </summary>
        /// <value>
        /// The dlayer.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 10000.0)]
        [Units("mm")]
        public double[] dlayer
        { get { return SoilObject.dlayer; } }



        //ARRAYS IN MILLIMETERS

        /// <summary>
        /// Gets the sa TMM.
        /// </summary>
        /// <value>
        /// The sa TMM.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] SATmm
        { get { return SoilObject.sat_dep; } }

        /// <summary>
        /// Gets the du LMM.
        /// </summary>
        /// <value>
        /// The du LMM.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] DULmm
        { get { return SoilObject.dul_dep; } }

        /// <summary>
        /// Gets or sets the s WMM.
        /// </summary>
        /// <value>
        /// The s WMM.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] SWmm
            { 
            get {
                if (SoilObject != null)
                    return SoilObject.sw_dep;
                else
                    return Soil.InitialWaterVolumetric;
                 }
            set { SetWater_mm(value); }  //TODO: remove this later, have manager scripts directly use SoilWater.SetWater_mm(amount) instead
            }

        /// <summary>
        /// Gets the l L15MM.
        /// </summary>
        /// <value>
        /// The l L15MM.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] LL15mm
        { get { return SoilObject.ll15_dep; } }

        /// <summary>
        /// Gets the airdr ymm.
        /// </summary>
        /// <value>
        /// The airdr ymm.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] AIRDRYmm
        { get { return SoilObject.air_dry_dep; } }




        //ARRAYS AS FRACTIONS

        /// <summary>
        /// Gets the sat.
        /// </summary>
        /// <value>
        /// The sat.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] SAT
        { get { return SoilObject.sat; } }

        /// <summary>
        /// Gets the dul.
        /// </summary>
        /// <value>
        /// The dul.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] DUL
        { get { return SoilObject.dul; } }

        /// <summary>
        /// Gets or sets the sw.
        /// </summary>
        /// <value>
        /// The sw.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] SW
            { 
            get { return SoilObject.sw; }
            set { SetWater_frac(value); } //TODO: remove this later, have manager scripts directly use SoilWater.SetWater_frac(amount) instead
            }

        /// <summary>
        /// Gets the l L15.
        /// </summary>
        /// <value>
        /// The l L15.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] LL15
        { get { return SoilObject.ll15; } }

        /// <summary>
        /// Gets the airdry.
        /// </summary>
        /// <value>
        /// The airdry.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] AIRDRY
        { get { return SoilObject.air_dry; } }



        /// <summary>
        /// Gets the flow.
        /// </summary>
        /// <value>
        /// The flow.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] flow
        { get { return SoilObject.flow; } }

        /// <summary>
        /// Gets the flux.
        /// </summary>
        /// <value>
        /// The flux.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] flux
        { get { return SoilObject.flux; } }

        /// <summary>
        /// Gets the outflow_lat.
        /// </summary>
        /// <value>
        /// The outflow_lat.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] outflow_lat
        { get { return SoilObject.outflow_lat; } }




        //DELTA ARRAY FOR A SOLUTE

        /// <summary>
        /// Gets the flow_no3.
        /// </summary>
        /// <value>
        /// The flow_no3.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] flow_no3
        { get { return SoilObject.GetFlowArrayForASolute("NO3"); } }

        /// <summary>
        /// Gets the flow_nh4.
        /// </summary>
        /// <value>
        /// The flow_nh4.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] flow_nh4
        { get { return SoilObject.GetFlowArrayForASolute("NH4"); } }

        /// <summary>
        /// Gets the flow_urea.
        /// </summary>
        /// <value>
        /// The flow_urea.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] flow_urea
        { get { return SoilObject.GetFlowArrayForASolute("urea"); } }



        #endregion






        //MANAGER COMMANDS



        #region Reset

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
            {
            Summary.WriteMessage(this, "Resetting Soil Water Balance");
            SoilObject.ResetSoil(constants, Soil);          //reset the soil
            surface = surfaceFactory.GetSurface(SoilObject, Clock); //reset the surface
            }


        #endregion






        #region Tillage


        /// <summary>
        /// Tillages the specified default tillage name.
        /// </summary>
        /// <param name="DefaultTillageName">Default name of the tillage.</param>
        /// <exception cref="ApsimXException"></exception>
        public void Tillage(string DefaultTillageName)
            {
            //****************
            //soilwat2_tillage
            //**************** <8-97 dms&pdv>
            //Mark Littleboy's tillage effect on runoff (used in PERFECT v2.0)
            //Littleboy, Cogle, Smith, Yule & Rao (1996)  Soil management and production
            //of alfisols in the SAT's I. Modelling the effects of soil management on runoff
            //and erosion.  Aust. J. Soil Res. 34: 91-102.

            //Tillage types that appear here must also be in residue2.ini under
            //[standard.residue2.tillage] !

            //Tillage reduces runoff potential (CN2) by g_tillage_cn_red (1st column below).
            //After tillage, CN2 increases linearly with cumulative rain since tillage until
            //g_tillage_cn_rain (column 2 below) has occured, ie roughness is smoothed out by rain.
            //Example parameter values (Littleboy et al) are given below for shallow & deep tillage.
            //Also, Nelsen et al (in press) had to reduce CN2 by 30 for ~ 30 days after tillage
            //during the wet season in wet tropical Philipines to mimic measured runoff.
            //-->


            SurfaceOrganicMatter.TillageTypesList defaultTypes = new SurfaceOrganicMatter.TillageTypesList();

            TillageType data = defaultTypes.GetTillageData(DefaultTillageName);

            if (data == null)
                {
                //! We have an unspecified tillage type

                string message = "Cannot find info for tillage:- " + data.Name;
                throw new ApsimXException(this, message);
                }



            switch (surface.SurfaceType)
                {
                case Surfaces.NormalSurface:
                    NormalSurface normal = (NormalSurface)surface;
                    normal.UpdateTillageCnRedVars(data.cn_rain, data.cn_red);
                    normal.ResetCumWaterSinceTillage();
                    WriteTillageToSummaryFile(data.Name, data.cn_red, data.cn_rain);
                    return;

                case Surfaces.PondSurface:
                    PondSurface pond = (PondSurface)surface;
                    pond.UpdateTillageCnRedVars(data.cn_rain, data.cn_red);
                    pond.ResetCumWaterSinceTillage();
                    WriteTillageToSummaryFile(data.Name, data.cn_red, data.cn_rain);
                    return;

                default:
                    return;

                }


            }





        /// <summary>
        /// Tillages the specified data.
        /// </summary>
        /// <param name="Data">The data.</param>
        /// <exception cref="ApsimXException"></exception>
        public void Tillage(TillageType Data)
            {
            //*     ===========================================================
            //      subroutine soilwat2_tillage ()
            //*     ===========================================================
            //*+  Purpose
            //*     Set up for CN reduction after tillage operation

            //*+  Notes
            //*       This code is borrowed from residue module.



            //*- Implementation Section ----------------------------------

            // cn_red is the reduction in the cn value, and cn_rain is the amount of rainfall after the tillage event that the reduction ceases to occur.

            //the event always gives us at least the type of tillage. Even if it does not give the cn_red and cn_rain.
            //if the event does not give us cn_red and cn_rain then use the type name to look up the values in the sim file (ini file).
            // ez - departs slightly from the Fortran version, where cn_red and cn_rain were optional arguments
            // They are always present here, but if the user sets the value to a negative number, we'll then
            // try to read the values from the initialisation data.



            //If the TillageData is not valid
            if ((Data.cn_red <= 0) || (Data.cn_rain <= 0))
                {

                string message = "tillage:- " + Data.Name + " has incorrect values for " + Environment.NewLine +
                    "CN reduction = " + Data.cn_red + Environment.NewLine + "Acc rain     = " + Data.cn_red;

                throw new ApsimXException(this, message);
                }



            double reduction;

            //! Ensure cn equation won't go silly
            reduction = constants.bound(Data.cn_red, 0.0, Soil.SoilWater.CN2Bare);


            switch (surface.SurfaceType)
                {
                case Surfaces.NormalSurface:
                    NormalSurface normal = (NormalSurface)surface;
                    normal.UpdateTillageCnRedVars(Data.cn_rain, reduction);
                    normal.ResetCumWaterSinceTillage();
                    WriteTillageToSummaryFile("a User Specified TillageType", reduction, Data.cn_rain);
                    return;

                case Surfaces.PondSurface:
                    PondSurface pond = (PondSurface)surface;
                    pond.UpdateTillageCnRedVars(Data.cn_rain, reduction);
                    pond.ResetCumWaterSinceTillage();
                    WriteTillageToSummaryFile("a User Specified TillageType", reduction, Data.cn_rain);
                    return;

                default:
                    return;

                }


            }



        /// <summary>
        /// Writes the tillage to summary file.
        /// </summary>
        /// <param name="TillageName">Name of the tillage.</param>
        /// <param name="CnReduction">The cn reduction.</param>
        /// <param name="CnCumWater">The cn cum water.</param>
        private void WriteTillageToSummaryFile(string TillageName, double CnReduction, double CnCumWater)
            {
            //sv- write what we are doing to the summary file.
            string line;
            line = String.Format("{0} {1} {2}                                        {3} {4:F} {5}                                        {6} {7:F}",
                                 "Soil tilled using ", TillageName, Environment.NewLine, "CN reduction = ", CnReduction, Environment.NewLine, "Acc rain     = ", CnCumWater);
            Summary.WriteMessage(this, line);
            }


        #endregion



        #region Irrigation

        /// <summary>
        /// Called when [irrigated].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="IrrigationData">The irrigation data.</param>
        [EventSubscribe("Irrigated")]
        private void OnIrrigated(object sender, Models.Soils.IrrigationApplicationType IrrigationData)
            {


            irrig.amount = IrrigationData.Amount;  

            irrig.willRunoff = IrrigationData.will_runoff;

            //check if will_runoff value conflicts with subsurface irrigation.
            if ((IrrigationData.will_runoff) && (IrrigationData.Depth > 0.0))
                {
                irrig.willRunoff = false;
                String warningText;
                warningText = "Irrigation Apply method parameter, willRunoff was reset from true to false." 
                + "In the Irrigation 'Apply' command, 'willRunoff' was set to true" + "\n"
                + " and irrigation depth was set to > 0 (mm)." + "\n"
                 + "Subsurface irrigation can not runoff like rain does. (Only surface irrigation can)" + "\n"
                 + "nb. Subsurface irrigations can still cause runoff if ponding occurs though.";
             
                constants.IssueWarning(warningText);
                }


            if (IrrigationData.Depth == 0.0)
                irrig.isSubSurface = false;
            else
                irrig.isSubSurface = true;
    

            irrig.layer = SoilObject.FindLayerNo(IrrigationData.Depth); //subsurface irrigation. (top layer is 1 not zero)


            //Solute amount in irrigation water.
            irrig.NO3 = IrrigationData.NO3;
            irrig.NH4 = IrrigationData.NH4;
            irrig.CL = IrrigationData.CL;

            }




        #endregion



        #region Set Max Pond (change depth of pond during a simulation)


        /// <summary>
        /// Sets the maximum pond.
        /// </summary>
        /// <param name="NewDepth">The new depth.</param>
        public void SetMaxPond(double NewDepth)
            {
            SoilObject.max_pond = NewDepth;

            //if the user changes max_pond after the OnSimulationCommencing event
            //you may need to change the surface either to or from a ponding surface.
            surface = surfaceFactory.GetSurface(SoilObject, Clock);
            }


        #endregion



        #region Set WaterTable


        /// <summary>
        /// Sets the water table.
        /// </summary>
        /// <param name="InitialDepth">The initial depth.</param>
        public void SetWaterTable(double InitialDepth)
            {
            SoilObject.SetWaterTable(InitialDepth);
            }


        #endregion



        #region Add / Remove Water from the Soil



        /// <summary>
        /// Sets the water_mm.
        /// </summary>
        /// <param name="New_SW_dep">The new_ s w_dep.</param>
        public void SetWater_mm(double[] New_SW_dep)
            {
            SoilObject.SetWater_mm(New_SW_dep); 
            }

        /// <summary>
        /// Sets the s WMM.
        /// </summary>
        /// <param name="Layer">Zero Based Layer Number</param>
        /// <param name="NewSWmm">New value for SWmm for the specified Layer</param>
        public void SetSWmm(int Layer, double NewSWmm)
            {
            SoilObject.SetWater_mm(Layer, NewSWmm);
            }


        /// <summary>
        /// Sets the water_frac.
        /// </summary>
        /// <param name="New_SW">The new_ sw.</param>
        public void SetWater_frac(double[] New_SW)
            {
            SoilObject.SetWater_frac(New_SW); 
            }


        //TODO: Change these delta properties into methods and change the manager scripts to call the methods instead.
        /// <summary>
        /// Sets the dlt_sw_dep.
        /// </summary>
        /// <value>
        /// The dlt_sw_dep.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] dlt_sw_dep
            {
            set
                {
                SoilObject.DeltaWater_mm(value);
                }
            }

        /// <summary>
        /// Sets the DLT_SW.
        /// </summary>
        /// <value>
        /// The DLT_SW.
        /// </value>
        [XmlIgnore]
        [Units("0-1")]
        public double[] dlt_sw
            {
            set
                {
                SoilObject.DeltaWater_frac(value);
                }
            }



        /// <summary>
        /// Called when [water changed].
        /// </summary>
        /// <param name="WaterChanged">The water changed.</param>
        [EventSubscribe("WaterChanged")]
        private void OnWaterChanged(WaterChangedType WaterChanged)
            {

            //This event is Only used by Plant2 and AgPasture.
            //This event was added so that the Plant2 module could extract water via its roots from the SoilWater module.
            //At the time Plant2 was not advanced enough to be able to do a "Set" on another modules variables.
            //Plant2 still uses this method to extract water using its roots.

            //*+  Purpose
            //*     Another module wants to change our water


            foreach (Layer lyr in SoilObject.TopToX(WaterChanged.DeltaWater.Length))
                {
                lyr.sw_dep = lyr.sw_dep + WaterChanged.DeltaWater[lyr.number-1];
                SoilObject.CheckLayerForErrors(lyr.number);
                }

            }



        #endregion






        //LOCAL VARIABLES

        #region Local Variables



        //Constants
        /// <summary>
        /// The constants
        /// </summary>
        private Constants constants;


        //Surface
        /// <summary>
        /// The surface factory
        /// </summary>
        private SurfaceFactory surfaceFactory;
        /// <summary>
        /// The surface
        /// </summary>
        private Surface surface;


        //Soil
        /// <summary>
        /// The soil object
        /// </summary>
        private SoilWaterSoil SoilObject;


        #endregion









        //DAILY INPUTS FROM OTHER MODULES


        #region Daily Input (Data Variables)

        //Met 
        /// <summary>
        /// The met
        /// </summary>
        private MetData met;


        //Irrigation
        /// <summary>
        /// The irrig
        /// </summary>
        private IrrigData irrig;


        //Canopy data from all the Crops in this Paddock.
        /// <summary>
        /// The canopy
        /// </summary>
        private CanopyData canopy;


        //SurfaceCover
        /// <summary>
        /// The surface cover
        /// </summary>
        private SurfaceCoverData surfaceCover;



        #endregion 



        #region Optional Daily Input (Data Variables)


        //Runon can be specified in a met file or sparse data file or manager script
        //[Input(IsOptional = true)]
        /// <summary>
        /// The runon
        /// </summary>
        [Units("mm/d")]
        private double runon;      //! external run-on of H2O (mm/d)


        //inflow_lat can be specified in a met file or sparse data file or manager script
        //[Input(IsOptional = true)]
        /// <summary>
        /// The inflow_lat
        /// </summary>
        [Units("mm")]
        private double[] inflow_lat;       //! inflowing lateral water



        //interception can be specified in micromet aka microclimate module (not implemented yet)
        //used in runoff and in infilitration calculation

        //[Input(IsOptional = true)]
        /// <summary>
        /// The interception
        /// </summary>
        [Units("mm")]
        private double interception;      //! canopy interception loss (mm)



        //residueinterception can be specified in surface organic matter module (not implemented yet)
        //used in runoff and in infilitration calculation

        //residueinterception is only used in ResiduesTrial.apsimx
        //[Input(IsOptional = true)]
        /// <summary>
        /// The residueinterception
        /// </summary>
        [Units("mm")]
        public double residueinterception;     //residue interception loss (mm)



        #endregion



        #region Get Variables from other Modules


        /// <summary>
        /// Gets the todays optional variables.
        /// </summary>
        private void GetTodaysOptionalVariables()
            {
            //zero to get rid of yesterdays value in case there isn't one none today.
            runon = 0.0;
            //interception = 0.0;
            //residueinterception = 0.0;


            object objectRunon = Apsim.Get(this, "runon");
            if (objectRunon != null)
                runon = (double)objectRunon;

            object objectInflow = Apsim.Get(this, "inflow_lat");
            if (objectInflow != null)
                inflow_lat = objectInflow as double[];
            else
                inflow_lat = new double[SoilObject.num_layers];

            //object objectIntercept = Apsim.Get(this, "interception");
            //if (objectIntercept != null)
            //    interception = (double)objectIntercept;

            //object objectResIntercept = Apsim.Get(this, "residueinterception");
            //if (objectResIntercept != null)
            //    residueinterception = (double)objectResIntercept;

            }



        /// <summary>
        /// Gets the todays canopy data.
        /// </summary>
        private void GetTodaysCanopyData()
            {
            //private void soilwat2_get_crop_variables()
            //{

            canopy.ZeroCanopyData();  //make all the arrays (from Yesterday) zero length, ready for the resize below (for Today).

            //Get an array of models that are crop models.
            List<IModel> models = Apsim.FindAll(paddock, typeof(ICanopy));

            //foreach ICanopy model in the simulation
            foreach (Model m in models)
                {
                    //add an extra element to each of the canopy arrays.
                    Array.Resize(ref canopy.cover_green, canopy.NumberOfCrops + 1);
                    Array.Resize(ref canopy.cover_tot, canopy.NumberOfCrops + 1);
                    Array.Resize(ref canopy.canopy_height, canopy.NumberOfCrops + 1);

                    //Cast the model to a ICanopy Model.
                    ICanopy Canopy = m as ICanopy;

                    //if this crop model has Canopy Data
                    //assign the data from this crop model to the extra element in the array you added.
                    canopy.cover_green[canopy.NumberOfCrops] = Canopy.CoverGreen;
                    canopy.cover_tot[canopy.NumberOfCrops] = Canopy.CoverTotal;
                    canopy.canopy_height[canopy.NumberOfCrops] = Canopy.Height;

                    canopy.NumberOfCrops += 1;     //increment number of crops ready for next array resize in next iteration.
                }


            //foreach crop2 model in the simulation
            List<IModel> models2 = Apsim.FindAll(paddock, typeof(ICrop2));

            foreach (Model m in models2)
                {
                Array.Resize(ref canopy.cover_green, canopy.NumberOfCrops + 1);
                Array.Resize(ref canopy.cover_tot, canopy.NumberOfCrops + 1);
                Array.Resize(ref canopy.canopy_height, canopy.NumberOfCrops + 1);

                ICrop2 Crop2 = m as ICrop2;
                if (Crop2.CanopyProperties != null)
                    {
                    canopy.cover_green[canopy.NumberOfCrops] = Crop2.CanopyProperties.CoverGreen;
                    canopy.cover_tot[canopy.NumberOfCrops] = Crop2.CanopyProperties.CoverTot;
                    canopy.canopy_height[canopy.NumberOfCrops] = Crop2.CanopyProperties.CanopyHeight;
                    }
                else
                    {
                    canopy.cover_green[canopy.NumberOfCrops] = 0;
                    canopy.cover_tot[canopy.NumberOfCrops] = 0;
                    canopy.canopy_height[canopy.NumberOfCrops] = 0;
                    }
                canopy.NumberOfCrops += 1;
                }

            canopy.interception = interception;
         }




        /// <summary>
        /// Gets the todays surface cover.
        /// </summary>
        private void GetTodaysSurfaceCover()
            {
            surfaceCover.ZeroSurfaceCover();

            surfaceCover.surfaceom_cover = SurfaceOM.Cover;
            surfaceCover.residueinterception = residueinterception;
            }




        /// <summary>
        /// Gets the todays solute amounts.
        /// </summary>
        private void GetTodaysSoluteAmounts()
            {
            //private void soilwat2_get_solute_variables()
            //    {

            string propName;
            double[] value;
      
            //for the number of solutes that was read in by OnNewSolute event handler)
            foreach (SoluteInLayer sol in SoilObject.GetAllSolutesInALayer())
                {
                if (sol.ownerName != "")
                    propName = sol.ownerName + "." + sol.name;
                else
                    propName = sol.name;

                object objValue = Apsim.Get(this, propName);  //Get the amount array for the solute by asking the System for it. 

                if (objValue != null)
                    {
                   value = objValue as double[];
                   SoilObject.UpdateSoluteAmounts(sol.name, value);
                    }

                }

            }





        #endregion









        //EVENT HANDLERS




        #region NewSolute Event Handler



        //ToDo: Need to work out what the NewSolute event will be.

        /// <summary>
        /// Called when [new solute].
        /// </summary>
        /// <param name="NewSolutes">The new solutes.</param>
        /// <exception cref="ApsimXException">No solute mobility information for  + name +  , please specify as mobile or immobile in the SoilWater ini file.</exception>
        [EventSubscribe("NewSolute")]
        private void OnNewSolute(NewSoluteType NewSolutes)
        {

            //*     ===========================================================
            //      subroutine soilwat2_on_new_solute ()
            //*     ===========================================================

            //"On New Solute" simply tells modules the name of a new solute, what module owns the new solute, and whether it is mobile or immobile.
            //       It alerts you at any given point in a simulation when a new solute is added. 
            //       It does NOT tell you the amount of the new solute in each of the layers. You have to ask the module owner for this separately.


            int counter;
            int numvals;             //! number of values returned

            string name;
            string ownerName;
            bool isMobile, isImmobile;


            //*- Implementation Section ----------------------------------
            numvals = NewSolutes.solutes.Length;

            for (counter = 0; counter < numvals; counter++)
            {
                name = NewSolutes.solutes[counter];
                ownerName = NewSolutes.OwnerFullPath;

                isMobile = (PositionInCharArray(name, mobile_solutes) >= 0);
                isImmobile = (PositionInCharArray(name, immobile_solutes) >= 0);

                if ( !isMobile && !isImmobile)
                    throw new ApsimXException(this, "No solute mobility information for " + name + " , please specify as mobile or immobile in the SoilWater ini file.");


                //Add the solute to each layer of the Soil
                foreach (Layer lyr in SoilObject)
                    {
                    SoluteInLayer newSolute = new SoluteInLayer(name, ownerName, isMobile);
                    if (lyr.GetASolute(name) == null)
                        {
                        lyr.AddSolute(newSolute);
                        }
                    }
            }
        }

        /// <summary>
        /// Positions the in character array.
        /// </summary>
        /// <param name="Name">The name.</param>
        /// <param name="NameList">The name list.</param>
        /// <returns></returns>
        private int PositionInCharArray(string Name, string[] NameList)
        {
            //!+ Purpose
            //!     returns the index number of the first occurrence of specified value

            for (int i = 0; i < NameList.Length; i++)
                if (NameList[i].ToLower() == Name.ToLower())
                    return i;
            return -1;  // Not found
        }



        #endregion




        #region Clock Event Handlers


        /// <summary>
        /// Saves the module constants.
        /// </summary>
        private void SaveModuleConstants()
            {
            constants = new Constants();

            constants.Summary = (Summary)Summary;
            constants.thismodel = this;

            constants.min_crit_temp                  = min_crit_temp;          
            constants.max_crit_temp                  = max_crit_temp;          
            constants.max_albedo                     = max_albedo;             
            constants.A_to_evap_fact                 = A_to_evap_fact;         
            constants.canopy_eos_coef                = canopy_eos_coef;        
            constants.sw_top_crit                    = sw_top_crit;            
            constants.sumes1_max                     = sumes1_max;             
            constants.sumes2_max                     = sumes2_max;             
            constants.solute_flow_eff                = solute_flow_eff;        
            constants.solute_flux_eff                = solute_flux_eff;        
            constants.gravity_gradient               = gravity_gradient;       
            constants.specific_bd                    = specific_bd;            
            constants.hydrol_effective_depth         = hydrol_effective_depth; 
            constants.mobile_solutes                 = mobile_solutes;    
            constants.immobile_solutes               = immobile_solutes;  
            constants.canopy_fact                    = canopy_fact;       
            constants.canopy_fact_height             = canopy_fact_height;
            constants.canopy_fact_default            = canopy_fact_default;    
            constants.act_evap_method                = act_evap_method;           

            }



        /// <summary>
        /// Called when [loaded].
        /// </summary>
        [EventSubscribe("Loaded")]
        private void OnLoaded()
        {

        }


        /// <summary>
        /// Called when [simulation commencing].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">
        /// SoilWater module has detected that the Soil has no layers.
        /// </exception>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {

            SaveModuleConstants();

            //daily inputs
            met = new MetData();
            irrig = new IrrigData();
            canopy = new CanopyData();
            surfaceCover = new SurfaceCoverData();

            //optional daily inputs
            runon = 0.0;      
            interception = 0.0;      
            residueinterception = 0.0; 


            if (Soil.Thickness != null)
                {
                try
                    {
                    SoilObject = new SoilWaterSoil(constants, Soil);  //constructor can throw an Exception
                    surfaceFactory = new SurfaceFactory();
                    surface = surfaceFactory.GetSurface(SoilObject, Clock);  //constructor can throw an Exception (Evap class constructor too)

                //optional inputs (array)
                inflow_lat = null; 
                }
                catch (Exception Ex)
                    {
                    throw new ApsimXException(this, Ex.Message);  //catch any constructor Exceptions and rethrow as an ApsimXException.
                    }
                }
            else
                {
                throw new ApsimXException(this, "SoilWater module has detected that the Soil has no layers.");
                }

        }



        /// <summary>
        /// Called when DoDailyInitialisation invoked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {

            met.radn = Weather.Radn;
            met.maxt = Weather.MaxT;
            met.mint = Weather.MinT;
            met.rain = Weather.Rain;

            constants.bound_check_real_var(met.radn, 0.0, 60.0, "radn");
            constants.bound_check_real_var(met.maxt, -50.0, 60.0, "maxt");
            constants.bound_check_real_var(met.mint, -50.0, 50.0, "mint");
            constants.bound_check_real_var(met.rain, 0.0, 5000.0, "rain");

        }




        /// <summary>
        /// Called when [do soil water movement].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilWaterMovement")]
        private void OnDoSoilWaterMovement(object sender, EventArgs e)
        {


            //*     ===========================================================
            //      subroutine soilwat2_prepare
            //*     ===========================================================


            SoilObject.ZeroOutputs();

            GetTodaysOptionalVariables();

            GetTodaysCanopyData();
            
            GetTodaysSurfaceCover();

            GetTodaysSoluteAmounts();



            //*     ===========================================================
            //      subroutine soilwat2_process
            //*     ===========================================================

 
            double backedup;          //! water backed up from flux calculations that was unable to enter profile


            //*- Implementation Section ----------------------------------


            //Give the surface new information for today.
            surface.Clock = Clock;
            surface.Met = met;
            surface.Runon = runon;
            surface.Irrig = irrig;
            surface.Canopy = canopy;                //interception is in here
            surface.SurfaceCover = surfaceCover;    //residueinterception is in here
            surface.SoilObject = SoilObject;        //give it the current state of the soil.



            // LATERAL FLOW
            SoilObject.Do_Lateral_Flow(inflow_lat);  
            //Lateral flow does not move solutes (comming in with inflow_lat, and out with outflow_lat). We should add this feature one day.



            // RUNOFF
            surface.CalcRunoff();



            // INFILTRATION
           
            surface.CalcInfiltration();

            surface.AddInfiltrationToSoil(ref SoilObject); //this includes the surface irrigation



            // IRRIGATION

            //subsurface irrigation
            SoilObject.AddSubSurfaceIrrigToSoil(irrig);


            //add solutes from irrigation
            SoilObject.AddSolutesDueToIrrigation(irrig);
            
            /*
            //! receive any solutes from rainfall
            soilwat2_rainfall_solute();
            */
           
            //now that we have applied the irrigation 
            //(either through surface irrigation or straight into the soil via subsurface), zero it.
            irrig.ZeroIrrigation(); 




            // SATURATED FLOW & ADD BACKED UP TO SURFACE

            //calculate saturated flow
            backedup = SoilObject.Calc_Saturated_Flow();

            if (backedup > 0.0)
                surface.AddBackedUpWaterToSurface(backedup, ref SoilObject);

            //! move water down     
            SoilObject.Do_Saturated_Flow();   //also calculate drainage out the bottom layer.




            // SOLUTE MOVEMENT - SATURATED FLOW 
            SoilObject.Do_Solutes_SatFlow();



            
            // EVAPORATION

            surface.CalcEvaporation();

            surface.RemoveEvaporationFromSoil(ref SoilObject);




            // UNSATURATED FLOW 

            SoilObject.Calc_Unsaturated_Flow();

            //! move water up         
            SoilObject.Do_Unsaturated_Flow();

            SoilObject.CheckSoilForErrors();




            // WATER TABLE
            SoilObject.Calc_DepthToWaterTable();



            // SOLUTE MOVEMENT - UNSATURATED FLOW 
            SoilObject.Do_Solutes_UnsatFlow();




            // SEND EVENTS OUT
            SendNitrogenChangedEvent();


            //zero this here so it is not used tomorrow. 
            residueinterception = 0.0;

        }

        #endregion











        //EVENTS - SENDING



        #region Send Nitrogen Changed Event


        /// <summary>
        /// Occurs when [nitrogen changed].
        /// </summary>
        public event NitrogenChangedDelegate NitrogenChanged;


        /// <summary>
        /// Sends the nitrogen changed event.
        /// </summary>
        private void SendNitrogenChangedEvent()
            {

            NitrogenChangedType NitrogenDeltas = new NitrogenChangedType();
            NitrogenDeltas.Sender = "SoilWater";
            NitrogenDeltas.SenderType = "WaterModule";

            NitrogenDeltas.DeltaUrea = SoilObject.GetDeltaArrayForASolute("urea");
            NitrogenDeltas.DeltaNH4 = SoilObject.GetDeltaArrayForASolute("NH4");
            NitrogenDeltas.DeltaNO3 = SoilObject.GetDeltaArrayForASolute("NO3");

            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NitrogenDeltas);


            }



        #endregion





    }

  }