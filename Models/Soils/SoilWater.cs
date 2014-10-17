

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


namespace Models.Soils
{

    ///<summary>
    /// .NET port of the Fortran SoilWat model
    /// Ported by Shaun Verrall Mar 2011
    /// Extended by Eric Zurcher Mar 2012
    ///</summary>
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
        [Description("Date for start of summer evaporation (dd-mmm)")]
        public string SummerDate { get; set; }

        [Bounds(Lower = 0.0, Upper = 40.0)]
        [Units("mm")]
        [Description("Upper limit of stage 1 soil evaporation during summer (U)")]
        public double SummerU { get; set; }

        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Description("Stage 2 drying coefficient during summer (Cona)")]
        public double SummerCona { get; set; }




        //winter

        [Description("Date for start of winter evaporation (dd-mmm)")]
        public string WinterDate { get; set; }

        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm")]
        [Description("Upper limit of stage 1 soil evaporation during winter (U)")]
        public double WinterU { get; set; }

        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Description("Stage 2 drying coefficient during winter (Cona)")]
        public double WinterCona { get; set; }





        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Description("Diffusivity constant for soil texture")]
        public double DiffusConst { get; set; }

        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Description("Slope for diffusivity/soil water content relationship")]
        public double DiffusSlope { get; set; }

        [Description("Bare soil albedo")]
        public double Salb { get; set; }



        //Runoff


        [Bounds(Lower = 1.0, Upper = 100.0)]
        [Units("0-100")]
        [Description("Runoff Curve Number of bare soil with average moisture (cn2_bare)")]
        public double CN2Bare { get; set; }

        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Units("0-100")]
        [Description("Maximum reduction in cn2_bare due to cover (cn_red)")]
        public double CNRed { get; set; }

        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Cover at which cn_red occurs (cn_cov)")]
        public double CNCov { get; set; }



        //Lateral flow

        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Slope")]
        public double slope { get; set; }

        [Bounds(Lower = 0.0, Upper = 1.0e8F)]     //1.0e8F = 100000000
        [Units("m")]
        [Description("Basal width of discharge area")]
        public double discharge_width { get; set; }

        [Bounds(Lower = 0.0, Upper = 1.0e8F)]     //1.0e8F = 100000000
        [Units("m^2")]
        [Description("Area over which lateral flow is occuring")]
        public double catchment_area { get; set; }



        //Ponding

        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        [Description("Maximum water storage on soil surface")]
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
        [Units("mm")]
        [Description("Thicknesses specified in SoilWater node of GUI")]
        public double[] Thickness { get; set; }



        /// <summary>
        /// This "Depth" variable is only needed for the "Depth" column in the "SoilWater" node of the GUI.
        /// Just converts back and forth between "Depth" (in cm as string) AND "Thickness" (in mm as double).
        /// </summary>
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
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/d")]
        [Description("SWCON")]
        public double[] SWCON { get; set; }


        /// <summary>
        /// Lateral flow soil water conductivity constant for each soil layer.
        /// At thicknesses specified in "SoilWater" node of GUI.
        /// Use Soil.KLAT for KLAT in standard thickness
        /// </summary>
        [Bounds(Lower = 0, Upper = 1.0e3F)] //1.0e3F = 1000
        [Units("mm/d")]
        [Description("KLAT")]
        public double[] KLAT { get; set; }



        #endregion






        //MODEL
        //*****


        #region Links

        /// <summary>The clock</summary>
        [Link]
        private Clock Clock = null;


        /// <summary>The weather</summary>
        [Link]
        Weather Weather;


        ////needed for "interception"
        //[Link]
        //MicroClimate MicroClimate;


        /// <summary>The soil</summary>
        [Link]
        private Soil Soil = null;


        ////Gives you access to the "Water" tab of the GUI. Don't use this, Use top level Soil to get values in the Default layer structure.
        //[Link]
        //private Water Water = null; 


        //Paddock is needed to find all the Crops so you can work out Canopy data.
        /// <summary>The paddock</summary>
        [Link]
        Simulation paddock;


        //Needed for SurfaceCover
        /// <summary>The surface om</summary>
        [Link]
        SurfaceOrganicMatter SurfaceOM = null;


        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;



        #endregion



        #region Module Constants (Default Values) (NOT specified in GUI)

        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("oC")]
        [Description("Temperature below which eeq decreases")]
        public double min_crit_temp {get; set;}           


        [Bounds(Lower = 0.0, Upper = 50.0)]
        [Units("oC")]
        [Description("Temperature above which eeq increases")]
        public double max_crit_temp {get; set;}            


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Maximum bare ground soil albedo")]
        public double max_albedo {get; set;}           


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Factor to convert 'A' to coefficient in Adam's type residue effect on Eos")]
        public double A_to_evap_fact {get; set;}      


        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("0-10")]
        [Description("Coefficient in cover Eos reduction equation")]
        public double canopy_eos_coef {get; set;}  


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Critical sw ratio in top layer below which stage 2 evaporation occurs")]
        public double sw_top_crit {get; set;}  


        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        [Description("Upper limit of sumes1")]
        public double sumes1_max {get; set;}  


        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        [Description("Upper limit of sumes2")]
        public double sumes2_max {get; set;}  


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Efficiency of moving solute with unsaturated flow")]
        public double[] solute_flow_eff {get; set;}  


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Efficiency of moving solute with flux (saturated flow)")]
        public double[] solute_flux_eff {get; set;}   


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Gradient due to hydraulic differentials")]
        public double gravity_gradient {get; set;}  


        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("g/cm^3")]
        [Description("Specific bulk density")]
        public double specific_bd {get; set;}  


        [Bounds(Lower = 1.0, Upper = 1000.0)]
        [Units("mm")]
        [Description("Hydrologically effective depth for runoff")]
        public double hydrol_effective_depth {get; set;}  


        [Description("Names of all possible mobile solutes")]
        public string[] mobile_solutes {get; set;}  


        [Description("Names of all possible immobile solutes")]
        public string[] immobile_solutes {get; set;}  


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Canopy factors for cover runoff effect")]
        public double[] canopy_fact {get; set;}  


        [Bounds(Lower = 0.0, Upper = 100000.0)]
        [Units("mm")]
        [Description("Heights for canopy factors")]
        public double[] canopy_fact_height {get; set;}  


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Default canopy factor in absence of height")]
        public double canopy_fact_default {get; set;}  


        [Description("Actual soil evaporation model being used")]
        public string act_evap_method {get; set;}  
 


        #endregion



        #region Manager Variables (Default Values) (can be altered in Manager Code) 


        //TODO: put obsrunoff_name and eo_source into "Optional Daily Inputs" secion

        //Two below, store the Apsim variable names that you ask the System to provide by doing a Variables.Get();
        //The response can come from a Met file or Input File or Manager Script.

        [Description("System variable name of external observed runoff source")]
        public string obsrunoff_name {get; set;}   

        [Description("System variable name of external eo source")]
        public string eo_source { get; set; }    



        //TODO: turn these into MANAGER COMMANDS by turning them into Set methods. Just like SetMaxPond
        //maybe get rid of the set; and just leave a get; so they are still available as outputs

        //Irrigation Command
        //******************

        [Bounds(Lower = 0, Upper = 100)]
        [Description("Irrigation will runoff (0 no runoff [default], 1 runoff like rain")]
        public bool irrigation_will_runoff {get; set;}

        [Bounds(Lower = 0, Upper = 100)]
        [Description("Number of soil layer to which irrigation water is applied (where top layer == 1)")]
        public int irrigation_layer {get; set;}   



        #endregion



        #region Constructor



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

            obsrunoff_name = "";
            eo_source = "";

            irrigation_will_runoff = false;
            irrigation_layer = 1;  //layer 1 is surface irrigation 


            }




        #endregion



        #region Ouputs (NOT Layered)



        /// <summary>
        /// Effective potential evapotranspiration
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double Eo { get{return surface.Eo;} }                     

        /// <summary>
        /// Pot evap after modification for green cover & residue wt
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double Eos { get{return surface.Eos;} }


        /// <summary>
        /// Total es
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double Es { get { return surface.Es; } }

        /// <summary>
        /// time after 2nd-stage soil evaporation begins (d)
        /// </summary>
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
        /// New cn2 after modification for crop cover & residue cover
        /// </summary>
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
        [XmlIgnore]
        [Units("mm")]
        public double Drainage { get{return SoilObject.Drainage;} }



        /// <summary>Drainage rate from bottom layer</summary>
        /// <value>The leach n o3.</value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double LeachNO3 { get { return SoilObject.LeachNO3; } }         //! Leaching from bottom layer (kg/ha) // 

        /// <summary>Drainage rate from bottom layer</summary>
        /// <value>The leach n h4.</value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double LeachNH4 { get { return SoilObject.LeachNH4; } }         //! Leaching from bottom layer (kg/ha) // 

        /// <summary>Drainage rate from bottom layer</summary>
        /// <value>The leach urea.</value>
        [XmlIgnore]
        [Units("kg/ha")]
        public double LeachUrea { get { return SoilObject.LeachUrea; } }         //! Leaching from bottom layer (kg/ha) // 




        /// <summary>
        /// Infiltration into top layer
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double Infiltration { get{return surface.Infiltration;} }     

        /// <summary>
        /// Runoff from top layer
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double Runoff { get{return surface.Runoff;} }           
        
        /// <summary>
        /// Evaporation from the surface of the pond
        /// </summary>
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
        [XmlIgnore]
        [Units("mm")]
        public double ESW
        { get { return SoilObject.esw; } }



        #endregion



        #region Outputs (Layered)


        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 10000.0)]
        [Units("mm")]
        public double[] dlayer
        { get { return SoilObject.dlayer; } }



        //ARRAYS IN MILLIMETERS

        [XmlIgnore]
        [Units("mm")]
        public double[] sat_dep
        { get { return SoilObject.sat_dep; } }

        [XmlIgnore]
        [Units("mm")]
        public double[] DULmm
        { get { return SoilObject.dul_dep; } }

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

        [XmlIgnore]
        [Units("mm")]
        public double[] LL15mm
        { get { return SoilObject.ll15_dep; } }

        [XmlIgnore]
        [Units("mm")]
        public double[] air_dry_dep
        { get { return SoilObject.air_dry_dep; } }




        //ARRAYS AS FRACTIONS

        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] sat
        { get { return SoilObject.sat; } }

        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] DUL
        { get { return SoilObject.dul; } }

        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] SW
            { 
            get { return SoilObject.sw; }
            set { SetWater_frac(value); } //TODO: remove this later, have manager scripts directly use SoilWater.SetWater_frac(amount) instead
            }

        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] LL15
        { get { return SoilObject.ll15; } }

        [XmlIgnore]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] air_dry
        { get { return SoilObject.air_dry; } }



        [XmlIgnore]
        [Units("mm")]
        public double[] flow
        { get { return SoilObject.flow; } }

        [XmlIgnore]
        [Units("mm")]
        public double[] flux
        { get { return SoilObject.flux; } }

        [XmlIgnore]
        [Units("mm")]
        public double[] outflow_lat
        { get { return SoilObject.outflow_lat; } }




        //DELTA ARRAY FOR A SOLUTE

        [XmlIgnore]
        [Units("kg/ha")]
        public double[] flow_no3
        { get { return SoilObject.GetFlowArrayForASolute("NO3"); } }

        [XmlIgnore]
        [Units("kg/ha")]
        public double[] flow_nh4
        { get { return SoilObject.GetFlowArrayForASolute("NH4"); } }

        [XmlIgnore]
        [Units("kg/ha")]
        public double[] flow_urea
        { get { return SoilObject.GetFlowArrayForASolute("urea"); } }



        #endregion






        //MANAGER COMMANDS



        #region Reset

        public void Reset()
            {
            Summary.WriteMessage(this, "Resetting Soil Water Balance");
            SoilObject.ResetSoil(constants, Soil);          //reset the soil
            surface = surfaceFactory.GetSurface(SoilObject); //reset the surface
            }


        #endregion






        #region Tillage


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
                throw new Exception(message);
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

                throw new Exception(message);
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

        [EventSubscribe("Irrigated")]
        private void OnIrrigated(object sender, Models.Soils.IrrigationApplicationType IrrigationData)
            {


            //TODO: need to zero the irrig object each day. 

            //see OnProcess event handler for where this irrigation is added to the soil water 
            irrig.irrigation = IrrigationData.Amount;  //! amount of irrigation (mm)    

            //Added on 5 Dec 2012 to allow irrigation to runoff like rain. 
            irrig.irrigation_will_runoff = IrrigationData.will_runoff;

            if ((IrrigationData.will_runoff) && (IrrigationData.Depth > 0.0))
                {
                irrig.irrigation_will_runoff = false;
                String warningText;
                warningText = "In the irrigation 'apply' command, 'will_runoff' was set to true" + "\n"
                + "If you specify irrigation depth > 0 (mm), " + "\n"
                 + "then you can not choose to have irrigation runoff like rain as well. ('will_runoff = true')" + "\n"
                 + "ie. Subsurface irrigations can not runoff like rain does. (Only surface irrigation can)" + "\n"
                 + "nb. Subsurface irrigations will cause runoff if ponding occurs though.";
                constants.IssueWarning(warningText);
                }


            //sv- added on 26 Nov 2012. Needed for subsurface irrigation. 
            //    Manager module sends "apply" command specifying depth as a argument to irrigation module.
            //    irrigation module sends "Irrigated" event with the depth. 
            //    Now need to turn depth into the specific subsurface layer that the irrigation is to go into.
            irrig.irrigation_layer = SoilObject.FindLayerNo(IrrigationData.Depth);


            //Solute amount in irrigation water.

            irrig.NO3 = IrrigationData.NO3;
            irrig.NH4 = IrrigationData.NH4;
            irrig.CL = IrrigationData.CL;

            }




        #endregion



        #region Set Max Pond (change depth of pond during a simulation)


        public void SetMaxPond(double NewDepth)
            {
            SoilObject.max_pond = NewDepth;

            //if the user changes max_pond after the OnSimulationCommencing event
            //you may need to change the surface either to or from a ponding surface.
            surface = surfaceFactory.GetSurface(SoilObject);
            }


        #endregion



        #region Set WaterTable


        public void SetWaterTable(double InitialDepth)
            {
            SoilObject.SetWaterTable(InitialDepth);
            }


        #endregion



        #region Add / Remove Water from the Soil



        public void SetWater_mm(double[] New_SW_dep)
            {
            SoilObject.SetWater_mm(New_SW_dep); 
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Layer">Zero Based Layer Number</param>
        /// <param name="NewSWmm">New value for SWmm for the specified Layer</param>
        public void SetSWmm(int Layer, double NewSWmm)
            {
            SoilObject.SetWater_mm(Layer, NewSWmm);
            }


        public void SetWater_frac(double[] New_SW)
            {
            SoilObject.SetWater_frac(New_SW); 
            }


        //TODO: Change these delta properties into methods and change the manager scripts to call the methods instead.
        [XmlIgnore]
        [Units("mm")]
        public double[] dlt_sw_dep
            {
            set
                {
                SoilObject.DeltaWater_mm(value);
                }
            }

        [XmlIgnore]
        [Units("0-1")]
        public double[] dlt_sw
            {
            set
                {
                SoilObject.DeltaWater_frac(value);
                }
            }



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
        private Constants constants;


        //Surface
        private SurfaceFactory surfaceFactory;
        private Surface surface;


        //Soil
        private SoilWaterSoil SoilObject;


        #endregion









        //DAILY INPUTS FROM OTHER MODULES


        #region Daily Input (Data Variables)

        //Met 
        private MetData met;


        //Irrigation
        private IrrigData irrig;


        //Canopy data from all the Crops in this Paddock.
        private CanopyData canopy;


        //SurfaceCover
        private SurfaceCoverData surfaceCover;



        #endregion 



        #region Optional Daily Input (Data Variables)


        //Runon can be specified in a met file or sparse data file or manager script
        //[Input(IsOptional = true)]
        [Units("mm/d")]
        private double runon;      //! external run-on of H2O (mm/d)


        //inflow_lat can be specified in a met file or sparse data file or manager script
        //[Input(IsOptional = true)]
        [Units("mm")]
        private double[] inflow_lat;       //! inflowing lateral water



        //interception can be specified in micromet aka microclimate module (not implemented yet)
        //used in runoff and in infilitration calculation

        //[Input(IsOptional = true)]
        [Units("mm")]
        private double interception;      //! canopy interception loss (mm)



        //residueinterception can be specified in surface organic matter module (not implemented yet)
        //used in runoff and in infilitration calculation

        //residueinterception is only used in ResiduesTrial.apsimx
        //[Input(IsOptional = true)]
        [Units("mm")]
        public double residueinterception;     //residue interception loss (mm)



        #endregion



        #region Get Variables from other Modules


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



        private void GetTodaysCanopyData()
            {
            //private void soilwat2_get_crop_variables()
            //{

            canopy.ZeroCanopyData();  //make all the arrays (from Yesterday) zero length, ready for the resize below (for Today).

            //Get an array of models that are crop models.
            List<IModel> models = Apsim.FindAll(paddock, typeof(ICrop));

            //foreach crop model in the simulation
            foreach (Model m in models)
                {
                    //add an extra element to each of the canopy arrays.
                    Array.Resize(ref canopy.cover_green, canopy.NumberOfCrops + 1);
                    Array.Resize(ref canopy.cover_tot, canopy.NumberOfCrops + 1);
                    Array.Resize(ref canopy.canopy_height, canopy.NumberOfCrops + 1);

                    //Cast the model to a Crop Model.
                    ICrop Crop = m as ICrop;

                    //if this crop model has Canopy Data
                    if (Crop.CanopyData != null)
                    {
                        //assign the data from this crop model to the extra element in the array you added.
                        canopy.cover_green[canopy.NumberOfCrops] = Crop.CanopyData.cover;
                        canopy.cover_tot[canopy.NumberOfCrops] = Crop.CanopyData.cover_tot;
                        canopy.canopy_height[canopy.NumberOfCrops] = Crop.CanopyData.height;
                    }
                    //if this crop model does not have any Canopy Data
                    else
                    {
                        //assign zero the extra element in the array you added.
                        canopy.cover_green[canopy.NumberOfCrops] = 0;
                        canopy.cover_tot[canopy.NumberOfCrops] = 0;
                        canopy.canopy_height[canopy.NumberOfCrops] = 0;
                    }
                    canopy.NumberOfCrops += 1;     //increment number of crops ready for next array resize in next iteration.
                }


            //foreach crop2 model in the simulation
            List<IModel> models2 = Apsim.FindAll(paddock, typeof(ICrop2));

            foreach (Model m in models2)
                {
                canopy.NumberOfCrops = 0;
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




        private void GetTodaysSurfaceCover()
            {
            surfaceCover.ZeroSurfaceCover();

            surfaceCover.surfaceom_cover = SurfaceOM.surfaceom_cover;
            surfaceCover.residueinterception = residueinterception;
            }




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
                    throw new Exception("No solute mobility information for " + name + " , please specify as mobile or immobile in the SoilWater ini file.");


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



        [EventSubscribe("Loaded")]
        private void OnLoaded()
        {

        }


        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {

            SaveModuleConstants();

            //daily inputs
            met = new MetData();
            irrig = new IrrigData(irrigation_will_runoff, irrigation_layer);
            canopy = new CanopyData();
            surfaceCover = new SurfaceCoverData();

            //optional daily inputs
            runon = 0.0;      
            interception = 0.0;      
            residueinterception = 0.0; 


            if (Soil.Thickness != null)
                {
                SoilObject = new SoilWaterSoil(constants, Soil);
                surfaceFactory = new SurfaceFactory();
                surface = surfaceFactory.GetSurface(SoilObject);

                //optional inputs (array)
                inflow_lat = null; 
                }
            else
                {
                throw new Exception("SoilWater module has detected that the Soil has no layers.");
                }

        }



        [EventSubscribe("NewWeatherDataAvailable")]
        private void OnNewWeatherDataAvailable(object sender, EventArgs e)
        {

            met.radn = Weather.MetData.Radn;
            met.maxt = Weather.MetData.Maxt;
            met.mint = Weather.MetData.Mint;
            met.rain = Weather.MetData.Rain;

            constants.bound_check_real_var(met.radn, 0.0, 60.0, "radn");
            constants.bound_check_real_var(met.maxt, -50.0, 60.0, "maxt");
            constants.bound_check_real_var(met.mint, -50.0, 50.0, "mint");
            constants.bound_check_real_var(met.rain, 0.0, 5000.0, "rain");

        }




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



            // RUNOFF

            surface.CalcRunoff();



            // INFILTRATION
           
            surface.CalcInfiltration();

            //add infiltration to soil
            surface.AddInfiltrationToSoil(ref SoilObject);



            // SUBSURFACE IRRIGATION
            if (irrig.irrigation_layer > 1)
                SoilObject.AddSubSurfaceIrrig(irrig);


            //! save solutes from irrigation
            SoilObject.AddSolutesDueToIrrigation(irrig);
            /*
                  //! receive any solutes from rainfall
                  soilwat2_rainfall_solute();
            */
            //! NIH 180895
            //! in order to continue capturing irrigation information we zero
            //! the value here.  If we zero the value at the beginning of the day
            //! we may zero it after irrigation has already been specified and the
            //! information would be lost.  The safest way is to hold onto the
            //! information until it is used then reset the record.

            irrig.ZeroIrrigation(irrigation_will_runoff, irrigation_layer); 



            // SATURATED FLOW

            //calculate saturated flow
            backedup = SoilObject.Calc_Saturated_Flow();

            if (backedup > 0.0)
                surface.AddBackedUpWaterToSurface(backedup, ref SoilObject);

            //! move water down     (Saturated Flow - alter sw_dep values using flux calculation)
            SoilObject.Do_Saturated_Flow();         //also calculate drainage out the bottom layer.




            // SATURATED FLOW SOLUTE MOVEMENT

            //! now move the solutes with flux  
            //! flux -  flow > dul
            SoilObject.Do_Solutes_SatFlow();



            
            // EVAPORATION

            surface.CalcEvaporation();

            //! ** take away evaporation
            surface.RemoveEvaporationFromSoil(ref SoilObject);




            // UNSATURATED FLOW 

            //calculate unsaturated flow   
            SoilObject.Calc_Unsaturated_Flow();

            //! move water up          (Unsaturated Flow - alter sw_dep values using flow calculation)
            SoilObject.Do_Unsaturated_Flow();

            //! now check that the soil water is not silly
            SoilObject.CheckSoilForErrors();




            // WATER TABLE

            SoilObject.Calc_DepthToWaterTable();



            // UNSATURATED FLOW SOLUTE MOVEMENT

            //! now move the solutes with flow  
            SoilObject.Do_Solutes_UnsatFlow();




            // SEND EVENTS OUT

            SendNitrogenChangedEvent();


            //zero this here so it is not used tomorrow. 
            residueinterception = 0.0;

        }

        #endregion











        //EVENTS - SENDING



        #region Send Nitrogen Changed Event


        public event NitrogenChangedDelegate NitrogenChanged;


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