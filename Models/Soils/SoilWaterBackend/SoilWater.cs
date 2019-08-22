using Models.Core;
using Models.Interfaces;
using Models.PMF;
using Models.Soils.SoilWaterBackend;
using Models.Surface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

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
    [ValidParent(ParentType=typeof(Soil))]
    public class SoilWater : Model, ISoilWater
    {

        private List<IModel> canopyModels;


        //GUI
        //***

        // Needed for the GUI's "SoilWater" node (DO NOT USE THESE VARIABLES DIRECTLY)


        //SOIL "Properties" (NOT LAYERED)

        //To use these variables you should use Soil.SoilWater.VariableName (eg. Soil.SoilWater.SummerCona)


        //Evaporation

        //different evap for summer and winter
        //summer

        /// <summary>
        /// Start date for switch to summer parameters for soil water evaporation (dd-mmm)
        /// </summary>
        /// <value>
        /// The summer date.
        /// </value>
        [Units("dd-mmm")]
        [Caption("Summer date")]
        [Description("Start date for switch to summer parameters for soil water evaporation")]
        public string SummerDate { get; set; }

        /// <summary>
        /// Cummulative soil water evaporation to reach the end of stage 1 soil water evaporation in summer (a.k.a. U) (
        /// </summary>
        /// <value>
        /// The summer u.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 40.0)]
        [Units("mm")]
        [Caption("Summer U")]
        [Description("Cummulative soil water evaporation to reach the end of stage 1 soil water evaporation in summer (a.k.a. U)")]
        public double SummerU { get; set; }

        /// <summary>
        /// Drying coefficient for stage 2 soil water evaporation in summer (a.k.a. ConA) 
        /// </summary>
        /// <value>
        /// The summer cona.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Caption("Summer ConA")]
        [Description("Drying coefficient for stage 2 soil water evaporation in summer (a.k.a. ConA)")]
        public double SummerCona { get; set; }

        //winter

        /// <summary>
        /// Start date for switch to winter parameters for soil water evaporation (dd-mmm)
        /// </summary>
        /// <value>
        /// The winter date.
        /// </value>
        [Units("dd-mmm")]
        [Caption("Winter date")]
        [Description("Start date for switch to winter parameters for soil water evaporation")]
        public string WinterDate { get; set; }

        /// <summary>
        /// Cummulative soil water evaporation to reach the end of stage 1 soil water evaporation in winter (a.k.a. U) 
        /// </summary>
        /// <value>
        /// The winter u.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm")]
        [Caption("Winter U")]
        [Description("Cummulative soil water evaporation to reach the end of stage 1 soil water evaporation in winter (a.k.a. U).")]
        public double WinterU { get; set; }

        /// <summary>
        /// Drying coefficient for stage 2 soil water evaporation in winter (a.k.a. ConA) 
        /// </summary>
        /// <value>
        /// The winter cona.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Caption("Winter ConA")]
        [Description("Drying coefficient for stage 2 soil water evaporation in winter (a.k.a. ConA)")]
        public double WinterCona { get; set; }

        /// <summary>
        /// Constant in the soil water diffusivity calculation (mm2/day)
        /// </summary>
        /// <value>
        /// The diffus constant.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm2/day")]
        [Caption("Diffusivity constant")]
        [Description("Constant in the soil water diffusivity calculation")]
        public double DiffusConst { get; set; }

        /// <summary>
        /// Effect of soil water storage above the lower limit on soil water diffusivity (/mm)
        /// </summary>
        /// <value>
        /// The diffus slope.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Units("/mm")]
        [Caption("Diffusivity slope")]
        [Description("Effect of soil water storage above the lower limit on soil water diffusivity")]
        public double DiffusSlope { get; set; }

        /// <summary>
        /// Fraction of incoming radiation reflected from bare soil
        /// </summary>
        /// <value>
        /// The salb.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Caption("Albedo")]
        [Description("Fraction of incoming radiation reflected from bare soil")]
        public double Salb { get; set; }

        //Runoff

        /// <summary>
        /// Runoff Curve Number (CN) for bare soil with average moisture
        /// </summary>
        /// <value>
        /// The c n2 bare.
        /// </value>
        [Bounds(Lower = 1.0, Upper = 100.0)]
        [Caption("CN bare")]
        [Description("Runoff Curve Number (CN) for bare soil with average moisture")]
        public double CN2Bare { get; set; }

        /// <summary>
        /// Maximum reduction in runoff Curve Number due to cover 
        /// </summary>
        /// <value>
        /// The cn red.
        /// </value>
        [Bounds(Lower = 1.0, Upper = 100.0)]
        [Caption("CN reduction")]
        [Description("Maximum reduction in runoff Curve Number due to cover")]
        public double CNRed { get; set; }

        /// <summary>
        /// Fractional cover at which maximum reduction of Curve Number occurs
        /// </summary>
        /// <value>
        /// The cn cov.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]        
        [Caption("CN cover")]
        [Description("Fractional cover at which maximum reduction of Curve Number occurs")]
        public double CNCov { get; set; }

        //Lateral flow

        /// <summary>
        /// Slope of the catchment area for lateral flow calculations
        /// </summary>
        /// <remarks>
        /// DSG: The units of slope are metres/metre.  Hence a slope = 0 means horizontal soil layers, and no lateral flows will occur.
        /// A slope = 1 means basically a 45 degree angle slope, which we thought would be the most anyone would be wanting to simulate.  Hence the bounds 0-1.  I still think this is fine.
        /// </remarks>
        /// <value>
        /// The slope.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Caption("Slope")]
        [Description("Slope of the catchment area for lateral flow calculations")]
        public double slope { get; set; }

        /// <summary>
        /// Basal width of the downslope boundary of the catchment for lateral flow calculations (m)
        /// </summary>
        /// <value>
        /// The discharge_width.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0e8F)]     //1.0e8F = 100000000
        [Units("m")]
        [Caption("Basal width")]
        [Description("Basal width of the downslope boundary of the catchment for lateral flow calculations")]
        public double discharge_width { get; set; }

        /// <summary>
        /// Catchment area for later flow calculations (m2)
        /// </summary>
        /// <value>
        /// The catchment_area.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0e8F)]     //1.0e8F = 100000000
        [Units("m2")]
        [Caption("Catchment")]
        [Description("Catchment area for lateral flow calculations")]
        public double catchment_area { get; set; }


        //Ponding

        /// <summary>
        /// Maximum ponding depth of water (e.g. of a rice paddy) (mm)
        /// </summary>
        /// <value>
        /// The max_pond.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        [Caption("Max pond")]
        [Description("Maximum ponding depth of water (e.g. of a rice paddy)")]
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
        /// Soil layer thickness for each layer (mm)
        /// </summary>
        /// <remarks>
        /// Thicknesses specified in "SoilWater" node of GUI (in mm as double).
        /// This is the NON standard layer thickness.
        /// </remarks>
        /// <value>
        /// The thickness.
        /// </value>
        [Description("Depth (mm)")]
        public double[] Thickness { get; set; }

        /// <summary>
        /// Fractional amount of water above DUL that can drain under gravity per day
        /// </summary>
        /// <remarks>
        /// Between (SAT and DUL) soil water conductivity constant for each soil layer.
        /// At thicknesses specified in "SoilWater" node of GUI.
        /// Use Soil.SWCON for SWCON in standard thickness
        /// </remarks>
        /// <value>
        /// The swcon.
        /// </value>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/d")]
        [Caption("SWCON")]
        [Description("Fractional amount of water above DUL that can drain under gravity per day (SWCON)")]
        public double[] SWCON { get; set; }


        /// <summary>
        /// Lateral saturated hydraulic conductivity (KLAT)
        /// </summary>
        /// <remarks>
        /// Lateral flow soil water conductivity constant for each soil layer.
        /// At thicknesses specified in "SoilWater" node of GUI.
        /// Use Soil.KLAT for KLAT in standard thickness
        /// </remarks>
        /// <value>
        /// The klat.
        /// </value>
        [Bounds(Lower = 0, Upper = 1.0e3F)] //1.0e3F = 1000
        [Units("mm/d")]
        [Caption("Klat")]
        [Description("Lateral saturated hydraulic conductivity (KLAT)")]
        public double[] KLAT { get; set; }

        //MODEL
        //*****

        // Links

        /// <summary>
        /// The clock
        /// </summary>
        [Link]
        private IClock Clock = null;

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
        private Zone paddock = null;

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

        /// <summary>Link to NO3.</summary>
        [Link]
        private List<ISolute> solutes = null;

        /// <summary>
        /// Constant in the calculation of the effect of surface residues on potential evapotranspiration
        /// </summary>
        /// <remarks>
        /// Factor to convert 'A' to coefficient in Adam's type residue effect on Eos
        /// </remarks>
        /// <value>
        /// The a_to_evap_fact.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double A_to_evap_fact { get; set; }

        /// <summary>
        /// Constant in the calculation of the effect of canopy cover on potential evapotranspiration
        /// </summary>
        /// <remarks>
        /// Coefficient in cover Eos reduction equation
        /// </remarks>
        /// <value>
        /// The canopy_eos_coef.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        public double canopy_eos_coef { get; set; }

        /// <summary>
        /// Critical soil water ratio in top layer below which stage 2 evaporation occurs
        /// </summary>
        /// <value>
        /// The sw_top_crit.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double sw_top_crit { get; set; }

        /// <summary>
        /// The value of the sum of soil water evaporation after which stage 1 evaporation concludes (mm)
        /// </summary>
        /// <remarks>
        /// Upper limit of sumes1
        /// </remarks>
        /// 
        /// 
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        public double sumes1_max { get; set; }

        /// <summary>
        /// The value of the sum of soil water evaporation after which stage 2 evaporation concludes (mm)
        /// </summary>
        /// <remarks>
        /// Upper limit of sumes2
        /// </remarks>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        public double sumes2_max { get; set; }

        /// <summary>
        /// Efficiency of moving solute with water flow below DUL (unsaturated flow)
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] solute_flow_eff { get; set; }

        /// <summary>
        /// Efficiency of moving solute with water flow above DUL (saturated flow)
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] solute_flux_eff { get; set; }

        /// <summary>
        /// Multiplier that can be used to reduce the effects of gravity on soil water movement, 0 = a horizontal soil, 1 = a vertial soil
        /// </summary>
        /// <remarks>
        /// Gradient due to hydraulic differentials
        /// </remarks>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double gravity_gradient { get; set; }

        /// <summary>
        /// Soil dry bulk density (g/cm3)
        /// </summary>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("g/cm3")]
        public double specific_bd { get; set; }

        /// <summary>
        /// Effective depth of ponded water that must be built up on the soil surface before runoff will occur
        /// </summary>
        /// <remarks>
        /// Hydrologically effective depth for runoff
        /// </remarks>
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
        /// Values of canopy effect for lookup (Y) in the relationship between crop height and effect of canopy cover on runoff
        /// </summary>
        /// <remarks>
        /// Canopy factors for cover runoff effect
        /// </remarks>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] canopy_fact { get; set; }

        /// <summary>
        /// Heights values for lookup (X) in the relationship between crop height and effect of canopy cover on runoff
        /// </summary>
        /// <remarks>
        /// Heights for canopy factors
        /// </remarks>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 100000.0)]
        [Units("mm")]
        public double[] canopy_fact_height { get; set; }

        /// <summary>
        /// Default canopy factor for runoff to use in absence of the canopy effect function, 0-1
        /// </summary>
        /// <remarks>
        /// Default canopy factor in absence of height
        /// </remarks>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double canopy_fact_default { get; set; }

        /// <summary>
        /// Name of soil water evaporation model being used
        /// </summary>
        [XmlIgnore]
        public string act_evap_method { get; set; }  

        // Manager Variables (Default Values) (can be altered in Manager Code) 


        //TODO: put obsrunoff_name and eo_source into "Optional Daily Inputs" secion

        //Two below, store the Apsim variable names that you ask the System to provide by doing a Variables.Get();
        //The response can come from a Met file or Input File or Manager Script.

        //[Description("System variable name of external eo source")]
        //public string eo_source { get; set; }    

        //TODO: turn these into MANAGER COMMANDS by turning them into Set methods. Just like SetMaxPond
        //maybe get rid of the set; and just leave a get; so they are still available as outputs  

        // Constructor

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
            mobile_solutes = new string[] { "NO3", "urea", "Chloride", "br", "org_n", "org_c_pool1", "org_c_pool2", "org_c_pool3" };
            immobile_solutes = new string[] { "NH4", "PlantAvailableNH4", "PlantAvailableNO3" };
            canopy_fact = new double[] { 1, 1, 0, 0 };
            canopy_fact_height = new double[] { 0, 600, 1800, 30000 };
            canopy_fact_default = 0.5;
            act_evap_method = "ritchie";


            //Manager Variables

            //eo_source = ""; 

            }

        // Ouputs (NOT Layered)

        /// <summary>
        /// Potential evapotranspiration of the whole soil-plant system
        /// </summary>
        /// <value>
        /// The eo.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Eo
        {
            get {return surface != null ? surface.Eo : Double.NaN;}
            set { surface.Eo = value; }
        }

        /// <summary>
        /// Potential evaporation of water from the soil (after accounting for the effects of cover and residues)
        /// </summary>
        /// <value>
        /// The eos.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Eos { get{return surface != null ? surface.Eos : Double.NaN;} }

        /// <summary>
        /// Actual (realised) soil water evaporation
        /// </summary>
        /// <value>
        /// The es.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Es { get { return surface != null ? surface.Es : Double.NaN; } }

        /// <summary>
        /// Number of days since the beginning of 2nd-stage soil water evaporation
        /// </summary>
        /// <remarks>
        /// time after 2nd-stage soil evaporation begins (d)
        /// </remarks>
        /// <value>
        /// The t.
        /// </value>
        [XmlIgnore]
        [Units("d")]
        public double t
            {
            get
                {
                if (surface == null)
                    return Double.NaN;
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
        /// Actual curve number (CN) after taking into account the effects of crop and residue cover
        /// </summary>
        /// <value>
        /// The cn2_new.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 100.0)]
        public double cn2_new
            {
            get {
                if (surface == null)
                    return Double.NaN;
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
        /// Drainage of water exiting the deepest soil layer (mm /day)
        /// </summary>
        /// <value>
        /// The drainage.
        /// </value>
        [XmlIgnore]
        [Units("mm/day")]
        public double Drainage { get{return SoilObject != null ? SoilObject.Drainage : Double.NaN;} }

        /// <summary>
        /// Amount of N leaching as NO3-N from the deepest soil layer (kg /ha)
        /// </summary>
        /// <value>
        /// The leach n o3.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double LeachNO3 { get { return SoilObject != null ? SoilObject.LeachNO3 : Double.NaN; } }         //! Leaching from bottom layer (kg/ha) // 

        /// <summary>
        /// Amount of N leaching as NH4-N from the deepest soil layer (kg /ha)
        /// </summary>
        /// <value>
        /// The leach n h4.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double LeachNH4 { get { return SoilObject != null ? SoilObject.LeachNH4 : Double.NaN; } }         //! Leaching from bottom layer (kg/ha) // 

        /// <summary>
        /// Amount of N leaching as urea-N  from the deepest soil layer (kg /ha)
        /// </summary>
        /// <value>
        /// The leach urea.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double LeachUrea { get { return SoilObject != null ? SoilObject.LeachUrea : Double.NaN; } }         //! Leaching from bottom layer (kg/ha) // 

        /// <summary>
        /// Depth of water infiltration into the soil (mm)
        /// </summary>
        /// <value>
        /// The infiltration.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Infiltration { get{return surface != null ? surface.Infiltration : Double.NaN;} }

        /// <summary>
        /// Amount of water runoff (mm)
        /// </summary>
        /// <value>
        /// The runoff.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double Runoff { get{return surface != null ? surface.Runoff : Double.NaN;} }

        /// <summary>
        /// Evaporation of water from the surface of the pond (mm)
        /// </summary>
        /// <value>
        /// The pond_evap.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double pond_evap
            {
            get {
                if (surface == null)
                    return Double.NaN;
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
        /// Surface water ponding depth (mm)
        /// </summary>
        /// <value>
        /// The pond.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double pond
            {
            get {
                if (surface == null)
                    return Double.NaN;
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
        /// Depth below the soil surface of the first saturated layer (mm)
        /// </summary>
        /// <remarks>
        /// Water table depth
        /// (depth below the ground surface of the first saturated layer)
        /// </remarks>
        /// <value>
        /// The water table.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double WaterTable
            { 
            get{return SoilObject != null ? SoilObject.DepthToWaterTable : Double.NaN;}
            set { SetWaterTable(value); } //TODO: remove this later, have manager scripts directly use SoilWater.SetWaterTable(amount) instead
            }

        /// <summary>
        /// Extractable soil water (above LL15) (mm)
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] ESW
        { get { return SoilObject != null ? SoilObject.esw : new double[0]; } }

        // Outputs (Layered)

        /// <summary>
        /// Current soil water content for each soil layer expressed as a depth of water (mm)
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
                else if (Soil != null)
                    return Soil.InitialWaterVolumetric;
                else
                    return new double[0];
                 }
            set { SetWater_mm(value); }  //TODO: remove this later, have manager scripts directly use SoilWater.SetWater_mm(amount) instead
            }

        /// <summary>
        /// Current soil water content for each soil layer expressed as a volumetric water content
        /// </summary>
        /// <value>
        /// The sw.
        /// </value>
        [XmlIgnore]
        [Units("mm/mm")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] SW
            { 
            get { return SoilObject != null ? SoilObject.sw : new double[0]; }
            set { SetWater_frac(value); } //TODO: remove this later, have manager scripts directly use SoilWater.SetWater_frac(amount) instead
            }

        /// <summary>
        /// Depth of water moving upward from each soil layer during unsaturated flow (negative value means downward movement) (mm)
        /// </summary>
        /// <value>
        /// The flow.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] Flow
        { get { return SoilObject != null ? SoilObject.flow : new double[0]; } }

        /// <summary>
        /// Depth of water moving downward out of each soil layer due to gravity drainage (above DUL) (mm)
        /// </summary>
        /// <value>
        /// The flux.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] Flux
        { get { return SoilObject != null ? SoilObject.flux : new double[0]; } }

        /// <summary>
        /// Amount of water moving laterally out of the profile (mm)
        /// </summary>
        /// <value>
        /// The outflow_lat.
        /// </value>
        [XmlIgnore]
        [Units("mm")]
        public double[] LateralOutflow
        { get { return SoilObject != null ? SoilObject.outflow_lat : new double[0] ; } }

        //DELTA ARRAY FOR A SOLUTE

        /// <summary>
        /// Amount of N leaching as NO3 from each soil layer (kg /ha)
        /// </summary>
        /// <value>
        /// The flow_no3.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] FlowNO3
        { get { return SoilObject != null ? SoilObject.GetFlowArrayForASolute("NO3") : new double[0]; } }

        /// <summary>
        /// Amount of N leaching as NH4 from each soil layer (kg /ha)
        /// </summary>
        /// <value>
        /// The flow_nh4.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] FlowNH4
        { get { return SoilObject != null ? SoilObject.GetFlowArrayForASolute("NH4") : new double[0]; } }

        /// <summary>
        /// Amount of N leaching as urea from each soil layer (kg /ha)
        /// </summary>
        /// <value>
        /// The flow_urea.
        /// </value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] flow_urea
        { get { return SoilObject != null ? SoilObject.GetFlowArrayForASolute("urea") : new double[0]; } }

        //MANAGER COMMANDS

        // Reset

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
            {
            Summary.WriteMessage(this, "Resetting Soil Water Balance");
            SoilObject.ResetSoil(constants, Soil);          //reset the soil
            surface = surfaceFactory.GetSurface(SoilObject, Clock); //reset the surface
            }

        // Tillage

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class TillageTypesList : Model
        {
            /// <summary>Gets or sets the type of the tillage.</summary>
            /// <value>The type of the tillage.</value>
            public List<TillageType> TillageType { get; set; }

            /// <summary>Gets the tillage data.</summary>
            /// <param name="Name">The name.</param>
            /// <returns></returns>
            public TillageType GetTillageData(string Name)
            {
                foreach (TillageType tillageType in TillageType)
                {
                    if (tillageType.Name == Name)
                        return tillageType;
                }
                return null;
            }
        }

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


            TillageTypesList defaultTypes = new TillageTypesList();

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
            reduction = constants.bound(Data.cn_red, 0.0, CN2Bare);

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

        // Irrigation

        /// <summary>
        /// Called when [irrigated].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="IrrigationData">The irrigation data.</param>
        [EventSubscribe("Irrigated")]
        private void OnIrrigated(object sender, Models.Soils.IrrigationApplicationType IrrigationData)
            {
            IrrigData irrigData = new IrrigData();
            irrigData.amount = IrrigationData.Amount;  

            irrigData.willRunoff = IrrigationData.WillRunoff;

            //check if will_runoff value conflicts with subsurface irrigation.
            if ((IrrigationData.WillRunoff) && (IrrigationData.Depth > 0.0))
                {
                irrigData.willRunoff = false;
                String warningText;
                warningText = "Irrigation Apply method parameter, willRunoff was reset from true to false." 
                + "In the Irrigation 'Apply' command, 'willRunoff' was set to true" + "\n"
                + " and irrigation depth was set to > 0 (mm)." + "\n"
                 + "Subsurface irrigation can not runoff like rain does. (Only surface irrigation can)" + "\n"
                 + "nb. Subsurface irrigations can still cause runoff if ponding occurs though.";
             
                constants.IssueWarning(warningText);
                }


            if (IrrigationData.Depth == 0.0)
                irrigData.isSubSurface = false;
            else
                irrigData.isSubSurface = true;
    

            irrigData.layer = SoilObject.FindLayerNo(IrrigationData.Depth); //subsurface irrigation. (top layer is 1 not zero)


            //Solute amount in irrigation water.
            irrigData.NO3 = IrrigationData.NO3;
            irrigData.NH4 = IrrigationData.NH4;
            irrigData.CL = IrrigationData.CL;

            irrig.Add(irrigData);
            }

        // Set Max Pond (change depth of pond during a simulation)

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

        // Set WaterTable

        /// <summary>
        /// Sets the water table.
        /// </summary>
        /// <param name="InitialDepth">The initial depth.</param>
        public void SetWaterTable(double InitialDepth)
            {
            SoilObject.SetWaterTable(InitialDepth);
            }

        // Add / Remove Water from the Soil

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
        private void SetWater_frac(double[] New_SW)
            {
            SoilObject.SetWater_frac(New_SW); 
            }

        ///<summary>Remove water from the profile</summary>
        public void RemoveWater(double[] value)
        {
            SoilObject.DeltaWater_mm(value);
        }

        /// <summary>
        /// Change in volumetric soil water content in each soil layer
        /// </summary>
        /// <value>
        /// The DLT_SW.
        /// </value>
        [XmlIgnore]
        [Bounds(Lower = -1.0, Upper = 1.0)]
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

        //LOCAL VARIABLES

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
        private Models.Soils.SoilWaterBackend.Surface surface;

        //Soil
        /// <summary>
        /// The soil object
        /// </summary>
        private SoilWaterSoil SoilObject;

        //DAILY INPUTS FROM OTHER MODULES

        //Met 
        /// <summary>
        /// The met
        /// </summary>
        private MetData met;

        //Irrigation
        /// <summary>
        /// A list of irrigation applications for the day
        /// </summary>
        private List<IrrigData> irrig;

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

        // Optional Daily Input (Data Variables)

        //Runon can be specified in a met file or sparse data file or manager script (mm/d)
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

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double PotentialInfiltration
        {
            get
            { return canopy.PotentialInfiltration;}
            set
            { canopy.PotentialInfiltration = value;}
        }

        // Get Variables from other Modules

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

            //foreach ICanopy model in the simulation
            foreach (Model m in canopyModels)
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

         }

        /// <summary>
        /// Gets the todays surface cover.
        /// </summary>
        private void GetTodaysSurfaceCover()
            {
            surfaceCover.ZeroSurfaceCover();

            surfaceCover.surfaceom_cover = SurfaceOM.Cover;
            }

        /// <summary>
        /// Gets the todays solute amounts.
        /// </summary>
        private void GetTodaysSoluteAmounts()
        {
            foreach (var solute in solutes)
                SoilObject.UpdateSoluteAmounts(solute.Name, solute.kgha);
        }

        //EVENT HANDLERS

        // NewSolute Event Handler

        /// <summary>
        /// Called to find all solutes.
        /// </summary>
        private void FindSolutes()
        {
            foreach (var solute in solutes)
            {
                bool isMobile = (PositionInCharArray(solute.Name, mobile_solutes) >= 0);
                bool isImmobile = (PositionInCharArray(solute.Name, immobile_solutes) >= 0);
                if (!isMobile && !isImmobile)
                    throw new ApsimXException(this, "No solute mobility information for " + solute.Name + " , please specify as mobile or immobile in the SoilWater ini file.");

                //Add the solute to each layer of the Soil
                foreach (Layer lyr in SoilObject)
                {
                    SoluteInLayer newSolute = new SoluteInLayer(solute.Name, null, isMobile);
                    if (lyr.GetASolute(solute.Name) == null)
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
                if (String.Equals(NameList[i], Name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;  // Not found
        }

        // Clock Event Handlers

        /// <summary>
        /// Saves the module constants.
        /// </summary>
        private void SaveModuleConstants()
            {
            constants = new Constants();

            constants.Summary = Summary;
            constants.thismodel = this;

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
        /// Called when [simulation commencing].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">
        /// SoilWater module has detected that the Soil has no layers.
        /// </exception>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {

            SaveModuleConstants();

            //daily inputs
            met = new MetData();
            irrig = new List<IrrigData>();
            canopy = new CanopyData();
            surfaceCover = new SurfaceCoverData();

            //optional daily inputs
            runon = 0.0;       

            if (Soil.Thickness != null)
                {
                SoilObject = new SoilWaterSoil(constants, Soil);  //constructor can throw an Exception
                surfaceFactory = new SurfaceFactory();
                surface = surfaceFactory.GetSurface(SoilObject, Clock);  //constructor can throw an Exception (Evap class constructor too)

                //optional inputs (array)
                inflow_lat = null; 
                }
            else
                {
                throw new ApsimXException(this, "SoilWater module has detected that the Soil has no layers.");
                }
            FindSolutes();

            canopyModels = Apsim.FindAll(paddock, typeof(ICanopy));
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

            foreach (IrrigData irrData in irrig)
            {
                //subsurface irrigation
                SoilObject.AddSubSurfaceIrrigToSoil(irrData);

                //add solutes from irrigation
                SoilObject.AddSolutesDueToIrrigation(irrData);
            }
            
            /*
            //! receive any solutes from rainfall
            soilwat2_rainfall_solute();
            */
           
            //now that we have applied the irrigation 
            //(either through surface irrigation or straight into the soil via subsurface), zero it.
            irrig.Clear(); 

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

            surface.CalcEvaporation(Eo);

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

        }

        //EVENTS - SENDING

        // Send Nitrogen Changed Event

        /// <summary>
        /// Sends the nitrogen changed event.
        /// </summary>
        private void SendNitrogenChangedEvent()
        {
            foreach (var solute in solutes)
            {
                bool isMobile = (PositionInCharArray(solute.Name, mobile_solutes) >= 0);
                if (isMobile)
                    solute.AddKgHaDelta(SoluteSetterType.Soil, SoilObject.GetDeltaArrayForASolute(solute.Name));
            }
        }

    }

  }