

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Models.Core;
using Models;
using System.Xml.Serialization;

namespace Models.Soils
{
    public class WaterChangedType
    {
        public double[] DeltaWater;
    }
    public class NewProfileType
    {
        public double[] dlayer;
        public double[] air_dry_dep;
        public double[] ll15_dep;
        public double[] dul_dep;
        public double[] sat_dep;
        public double[] sw_dep;
        public double[] bd;
    }
    public delegate void NewProfileDelegate(NewProfileType Data);


    ///<summary>
    /// .NET port of the Fortran SoilWat model
    /// Ported by Shaun Verrall Mar 2011
    /// Extended by Eric Zurcher Mar 2012
    ///</summary>
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class SoilWater : Model
    {
        #region Links
        [Link]
        private Clock Clock = null;

        //[Link]
        //private Zone Paddock = null;

        //[Link]
        private SoilWatTillageType SoilWatTillageType = null;

        [Link]
        private Soil Soil = null;

        #endregion

        #region Constants

        private const double precision_sw_dep = 1.0e-3; //!Precision for sw dep (mm)
        private const int ritchie_method = 1;
        private const double mm2m = 1.0 / 1000.0;      //! conversion of mm to m
        private const double sm2smm = 1000000.0;       //! conversion of square metres to square mm
        private const double error_margin = 0.0001;

        #endregion

        #region Module Constants (from SIM file but it gets from INI file)

        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("oC")]
        [Description("Temperature below which eeq decreases")]
        public double min_crit_temp = 5.0;             //! temperature below which eeq decreases (oC)


        [Bounds(Lower = 0.0, Upper = 50.0)]
        [Units("oC")]
        [Description("Temperature above which eeq increases")]
        public double max_crit_temp = 35.0;             //! temperature above which eeq increases (oC)


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Maximum bare ground soil albedo")]
        public double max_albedo = 0.23;                //! maximum bare ground soil albedo (0-1)


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Factor to convert 'A' to coefficient in Adam's type residue effect on Eos")]
        public double A_to_evap_fact = 0.44;            //! factor to convert "A" to coefficient in Adam's type residue effect on Eos


        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("0-10")]
        [Description("Coefficient in cover Eos reduction equation")]
        public double canopy_eos_coef = 1.7;           //! coef in cover Eos reduction eqn


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Critical sw ratio in top layer below which stage 2 evaporation occurs")]
        public double sw_top_crit = 0.9;               //! critical sw ratio in top layer below which stage 2 evaporation occurs


        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        [Description("Upper limit of sumes1")]
        public double sumes1_max = 100;                //! upper limit of sumes1


        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        [Description("Upper limit of sumes2")]
        public double sumes2_max = 25;                //! upper limit of sumes2


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Efficiency of moving solute with unsaturated flow")]
        public double[] solute_flow_eff = new double[] { 1.0 };          //sv- Unsaturated Flow   //! efficiency of moving solute with flow (0-1)
        private int num_solute_flow;   //bound_check_real_array() gives this a value in soilwat2_read_constants()


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Efficiency of moving solute with flux (saturated flow)")]
        public double[] solute_flux_eff = new double[] { 1.0 };         //sv- Drainage (Saturated Flow)   //! efficiency of moving solute with flux (0-1) 
        private int num_solute_flux; //bound_check_real_array() gives this a value in soilwat2_read_constants()


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Gradient due to hydraulic differentials")]
        public double gravity_gradient = 0.00002;          //! gradient due to hydraulic differentials (0-1)


        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("g/cm^3")]
        [Description("Specific bulk density")]
        public double specific_bd = 2.65;               //! specific bulk density (g/cc)


        [Bounds(Lower = 1.0, Upper = 1000.0)]
        [Units("mm")]
        [Description("Hydrologically effective depth for runoff")]
        public double hydrol_effective_depth = 450;    //! hydrologically effective depth for runoff (mm)


        [Description("Names of all possible mobile solutes")]
        public string[] mobile_solutes = new string[] {"no3", "urea", "cl", "br", "org_n", "org_c_pool1", "org_c_pool2", 
                                                   "org_c_pool3"};     //! names of all possible mobile solutes


        [Description("Names of all possible immobile solutes")]
        public string[] immobile_solutes = new string[] { "nh4" };   //! names of all possible immobile solutes


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Canopy factors for cover runoff effect")]
        public double[] canopy_fact = new double[] { 1, 1, 0, 0 };        //! canopy factors for cover runoff effect ()


        [Bounds(Lower = 0.0, Upper = 100000.0)]
        [Units("mm")]
        [Description("Heights for canopy factors")]
        public double[] canopy_fact_height = new double[] { 0, 600, 1800, 30000 }; //! heights for canopy factors (mm)


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Default canopy factor in absence of height")]
        public double canopy_fact_default = 0.5;       //! default canopy factor in absence of height ()


        [Description("Actual soil evaporation model being used")]
        public string act_evap_method = "ritchie";           //! actual soil evaporation model being used //sv- hard wired to "ritchie" in the init event handler. 
        private int evap_method;               //sv- integer representation of act_evap_method   


        //On SendIrrigated Event
        //**********************
        //Irrigation Runoff
        [Bounds(Lower = 0, Upper = 100)]
        [Description("Irrigation will runoff (0 no runoff [default], 1 runoff like rain")]
        public int irrigation_will_runoff = 0;  //0 means no runoff (default), 1 means allow the irrigation to runoff just like rain. (in future perhaps have other runoff methods)

        //Irrigation Layer
        [Bounds(Lower = 0, Upper = 100)]
        [Description("Number of soil layer to which irrigation water is applied (where top layer == 1)")]
        public int irrigation_layer = 0;      //! number of soil layer to which irrigation water is applied

        #endregion


        #region Soil "Property" (NOT layered): (Constants & Starting Values from SIM file), and the Outputs

        [Description("System variable name of external observed runoff source")]
        public string obsrunoff_name = "";    //! system name of observed runoff

        private string _eo_source = "";

        [Description("System variable name of external eo source")]
        public string eo_source      //! system variable name of external eo source
        {
            get { return _eo_source; }
            set
            {
                _eo_source = value;
                Console.WriteLine("Eo source: " + _eo_source);
            }
        }

        //sv- end of initial sw section


 

        private double _max_pond = 0.0;

        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        [Description("Maximum surface storage capacity of soil")]
        public double max_pond         //! maximum surface storage capacity of soil  //sv- used to store water from runoff on the surface.
        {
            get { return _max_pond; }
            set
            {
                //* made settable to allow for erosion 'max_pond'
                //*** dsg 280103  Added re-settable 'max-pond' for Shaun Lisson to simulate dam-break in rice cropping
                if (!initDone)
                {
                    //if we are reading in the [Param], because this variable is "settable" it can be changed from this value, 
                    //therefore store a copy so if there is a Reset event we can set it back to this value.
                    reset_max_pond = value;
                }
                _max_pond = value;
            }
        }

        [Bounds(Lower = 0.0001, Upper = 1.0)]
        [Units("0-1")]


        //Extra parameters for evaporation models (this module only has Ritchie Evaporation)  
        //(see soilwat2_init() for which u and cona is used)

        //same evap for summer and winter
        private double _u = 6.0;

        [Bounds(Lower = 0.0, Upper = 40.0)]
        [Units("mm")]
        [Description("Upper limit of stage 1 soil evaporation")]
        private double u            //! upper limit of stage 1 soil evaporation (mm)
        {
            get { return _u; }
            set { _u = value; }
        }

        private double _cona = 3.0;

        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Description("Stage 2 drying coefficient")]
        private double cona         //! stage 2 drying coefficient
        {
            get { return _cona; }
            set { _cona = value; }
        }

        //different evap for summer and winter
        //summer

        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Description("Stage 2 drying coefficient during summer")]
        public double SummerCona = Double.NaN;

        [Bounds(Lower = 0.0, Upper = 40.0)]
        [Units("mm")]
        [Description("Upper limit of stage 1 soil evaporation during summer")]
        public double SummerU = Double.NaN;

        [Description("Date for start of summer evaporation (dd-mmm)")]
        public string SummerDate = "not_read";       //! Date for start of summer evaporation (dd-mmm)

        //winter

        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Description("Stage 2 drying coefficient during winter")]
        public double WinterCona = Double.NaN;

        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm")]
        [Description("Upper limit of stage 1 soil evaporation during winter")]
        public double WinterU = Double.NaN;

        [Description("Date for start of winter evaporation (dd-mmm)")]
        public string WinterDate = "not_read";       //! Date for start of winter evaporation (dd-mmm)

        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Description("Diffusivity constant for soil testure")]
        public double DiffusConst = 40.0;     //! diffusivity constant for soil testure

        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Description("Slope for diffusivity/soil water content relationship")]
        public double DiffusSlope = 16.0;     //! slope for diffusivity/soil water content relationship

        [Description("Bare soil albedo")]
        public double Salb;           //! bare soil albedo (unitless)

        private double _cn2_bare = 73.0;

        [Bounds(Lower = 1.0, Upper = 100.0)]
        [Description("Curve number input used to calculate daily runoff")]
        public double CN2Bare         //! curve number input used to calculate daily runoff
        {
            get { return _cn2_bare; }
            set
            {
                if (!initDone)
                {
                    //if we are reading in the [Param], because this variable is "settable" it can be changed from this value, 
                    //therefore store a copy so if there is a Reset event we can set it back to this value.
                    reset_cn2_bare = value;
                }
                _cn2_bare = value;
            }
        }

        private double _cn_red = 20.0;

        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Description("Maximum reduction in cn2_bare due to cover")]
        public double CNRed           //! maximum reduction in cn2_bare due to cover
        {
            get { return _cn_red; }
            set
            {
                if (!initDone)
                {
                    //if we are reading in the [Param], because this variable is "settable" it can be changed from this value, 
                    //therefore store a copy so if there is a Reset event we can set it back to this value.
                    reset_cn_red = value;
                }
                _cn_red = value;
            }
        }

        private double _cn_cov = 0.8;

        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Cover at which cn_red occurs")]
        public double CNCov           //! cover at which cn_red occurs
        {
            get { return _cn_cov; }
            set
            {
                if (!initDone)
                {
                    //if we are reading in the [Param], because this variable is "settable" it can be changed from this value, 
                    //therefore store a copy so if there is a Reset event we can set it back to this value.
                    reset_cn_cov = value;
                }
                _cn_cov = value;
            }
        }


        //end of Extra parameters for evaporation models


        //sv- Lateral flow properties  //sv- also from Lateral_read_param()

        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Slope")]
        public double slope = Double.NaN;

        [Bounds(Lower = 0.0, Upper = 1.0e8F)]     //1.0e8F = 100000000
        [Units("m")]
        [Description("Basal width of discharge area")]
        public double discharge_width = Double.NaN;  //! basal width of discharge area (m)

        [Bounds(Lower = 0.0, Upper = 1.0e8F)]     //1.0e8F = 100000000
        [Units("m^2")]
        [Description("Area over which lateral flow is occuring")]
        public double catchment_area = Double.NaN;   //! area over which lateral flow is occuring (m2)

        //sv- end of Lateral flow properties



        //sv- PURE OUTPUTS


        [Units("mm")]
        [Description("Total es")]
        private double es                      //! total es
        { get { return Utility.Math.Sum(es_layers); } }


        [Units("mm")]
        [Description("Daily effective rainfall")]
        private double eff_rain                  //! daily effective rainfall (mm)
        { get { return rain + runon - runoff - drain; } }

        [Units("mm")]
        [Description("Potential extractable sw in profile")]
        private double esw                       //! potential extractable sw in profile  
        {
            get
            {
                if (_dlayer == null)
                    return 0;
                int num_layers = _dlayer.Length;
                double result = 0.0;
                for (int layer = 0; layer < num_layers; layer++)
                    result += Math.Max(_sw_dep[layer] - _ll15_dep[layer], 0.0);
                return result;
            }
        }


        [Description("Effective total cover")]
        private double cover_surface_runoff;     //! effective total cover (0-1)   //residue cover + cover from any crops (tall or short)


        [Units("d")]
        [Description("time after which 2nd-stage soil evaporation begins")]
        private double t;                        //! time after 2nd-stage soil evaporation begins (d)


        [Units("mm")]
        [Description("Effective potential evapotranspiration")]
        private double eo;                       //! effective potential evapotranspiration (mm)


        [Units("mm")]
        [Description("Pot sevap after modification for green cover & residue wt")]
        public double eos;                      //! pot sevap after modification for green cover & residue wt


        [Description("New cn2 after modification for crop cover & residue cover")]
        private double cn2_new;                  //! New cn2  after modification for crop cover & residue cover


        [Units("mm")]
        [Description("Drainage rate from bottom layer")]
        private double drain;            //! drainage rate from bottom layer (cm/d) // I think this is in mm, not cm....


        [Units("mm")]
        [Description("Infiltration")]
        private double infiltration;     //! infiltration (mm)


        [Units("mm")]
        [Description("Runoff")]
        private double runoff;           //! runoff (mm)


        [Units("mm")]
        [Description("Evaporation from the surface of the pond")]
        private double pond_evap;      //! evaporation from the surface of the pond (mm)


        [Units("mm")]
        [Description("Surface water ponding depth")]
        private double pond;           //! surface water ponding depth

        //Soilwat2Globals



        //taken from soilwat2_set_my_variable()


        //nb. water_table is both an input and an output. 
        //It is always is an output because a water table can always build up. (See soilwat_water_table())
        //Sometimes it is an input when the user specifies a set command in a manager because they want to set the water_table at a specific height on a given day. (see SetWaterTable())

        private double _water_table = Double.NaN;

        [Units("mm")]
        [Description("Water table depth (depth below the ground surface of the first saturated layer)")]
        private double water_table     //! water table depth (depth below the ground surface of the first saturated layer)
        {
            get { return _water_table; }
            set { SetWaterTable(value); }
        }


        //end of soilwat2_set_my_variable()
        private double WaterTableInitial = double.NaN;
        public double WaterTable
        {
            get
            {
                return WaterTableInitial;
            }
            set
            {
                WaterTableInitial = value;
                water_table = WaterTableInitial;
            }
        }





        #endregion


        #region Soil "Profile" (layered): (Constants & Starting Values from SIM file), and the Outputs

        //Has the soilwat_init() been done? If so, let the fractional soil arrays (eg. sw, sat, dul etc) check the profile
        //layers when a "set" occurs. If not, save reset values so they can be applied if a reset event is sent.
        bool initDone = false;
        //If doing a reset, we don't want to check profile layer data until ALL the various values have been reset. This flag
        //tells us whether we're doing a reset; if so, we can skip checking.
        bool inReset = false;

        //SIM file gets them from .APSIM file

        //Soilwat2Parameters   //sv- also from soilwat2_soil_profile_param()


        private double[] _dlayer = null;
        [Bounds(Lower = 0.0, Upper = 10000.0)]
        [Units("mm")]
        [Description("Thickness of soil layer")]
        private double[] dlayer    //! thickness of soil layer (mm)
        {
            get { return _dlayer; }
            set
            {
                if (!initDone)
                {
                    //if we are reading in the [Param], because this variable is "settable" it can be changed from this value, 
                    //therefore store a copy so if there is a Reset event we can set it back to this value.
                    reset_dlayer = new double[value.Length];
                    Array.Copy(value, reset_dlayer, value.Length);
                }

                int num_layers = value.Length;
                //resize all the arrays if they changed the number of layers
                if (_dlayer == null || num_layers != _dlayer.Length)
                {
                    Array.Resize(ref _air_dry_dep, num_layers);
                    Array.Resize(ref _dul_dep, num_layers);
                    Array.Resize(ref _ll15_dep, num_layers);
                    Array.Resize(ref _sat_dep, num_layers);
                    Array.Resize(ref _sw_dep, num_layers);
                    Array.Resize(ref _dlayer, num_layers);
                    Array.Resize(ref bd, num_layers);
                    Array.Resize(ref es_layers, num_layers);
                    Array.Resize(ref flow, num_layers);
                    Array.Resize(ref flux, num_layers);
                    Array.Resize(ref outflow_lat, num_layers);
                    //also resize for all solutes in this simulation.
                    for (int solnum = 0; solnum < num_solutes; solnum++)
                    {
                        Array.Resize(ref solutes[solnum].amount, num_layers);
                        Array.Resize(ref solutes[solnum].leach, num_layers);
                        Array.Resize(ref solutes[solnum].up, num_layers);
                        Array.Resize(ref solutes[solnum].delta, num_layers);
                    }
                }

                for (int layer = 0; layer < _dlayer.Length; layer++)
                {
                    //If you change the depths of the layer then you need to modify the water "_dep" variables by the same amount. (they are in mm too)
                    //If you don't do this, you will have the same amount of water that is now in a shallower layer, 
                    //therefore you will have a different fraction equivalent variables, the ones without the "_dep" eg. sw, dul.  
                    double fract = Utility.Math.Divide(value[layer], _dlayer[layer], 0.0);
                    _air_dry_dep[layer] = _air_dry_dep[layer] * fract;
                    _dul_dep[layer] = _dul_dep[layer] * fract;
                    _ll15_dep[layer] = _ll15_dep[layer] * fract;
                    _sat_dep[layer] = _sat_dep[layer] * fract;
                    _sw_dep[layer] = _sw_dep[layer] * fract;

                    //_dlayer[layer] = value[layer];

                    soilwat2_check_profile(layer);
                }

                if (initDone)
                    soilwat2_New_Profile_Event();

                Array.Copy(value, _dlayer, num_layers);
            }
        }


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Saturated water content for layer")]
        private double[] sat       //! saturated water content for layer  
        {
            get
            {
                int num_layers = _dlayer.Length;
                double[] _sat = new double[num_layers];
                for (int layer = 0; layer < num_layers; layer++)
                    _sat[layer] = Utility.Math.Divide(_sat_dep[layer], _dlayer[layer], 0.0);
                return _sat;
            }
            set
            {
                if (!initDone)
                {
                    //if we are reading in the [Param], because this variable is "settable" it can be changed from this value, 
                    //therefore store a copy so if there is a Reset event we can set it back to this value.
                    reset_sat = new double[value.Length];
                    Array.Copy(value, reset_sat, value.Length);
                }
                //* made settable to allow for erosion
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    _sat_dep[layer] = value[layer] * _dlayer[layer];   //change sat_dep NOT sat. The sat variable is just for inputting and outputting and is immediately converted to sw_dep.
                    soilwat2_check_profile(layer);
                }
            }
        }


        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Drained upper limit soil water content for each soil layer")]
        private double[] dul       //! drained upper limit soil water content for each soil layer 
        {
            get
            {
                int num_layers = _dlayer.Length;
                double[] _dul = new double[num_layers];
                //* made settable to allow for erosion
                for (int layer = 0; layer < num_layers; layer++)
                    _dul[layer] = Utility.Math.Divide(_dul_dep[layer], _dlayer[layer], 0.0);
                return _dul;
            }
            set
            {
                if (!initDone)
                {
                    //if we are reading in the [Param], because this variable is "settable" it can be changed from this value, 
                    //therefore store a copy so if there is a Reset event we can set it back to this value.
                    reset_dul = new double[value.Length];
                    Array.Copy(value, reset_dul, value.Length);
                }
                //* made settable to allow for erosion
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    _dul_dep[layer] = value[layer] * _dlayer[layer];   //change dul_dep NOT dul. The dul variable is just for inputting and outputting and is immediately converted to sw_dep.
                    soilwat2_check_profile(layer);
                }
            }
        }

        private int numvals_sw = 0;                        //! number of values returned for sw 
        [Bounds(Lower = 0.0, Upper = 1.0)]
#if COMPARISON
    [Units("mm/mm")]
#else
        [Units("0-1")]
#endif

        [XmlIgnore]
        [UserInterfaceIgnore]
        [Description("Soil water content of layer")]
        public double[] sw        //! soil water content of layer
        {
            get
            {
                if (_dlayer != null)
                {
                    int num_layers = _dlayer.Length;
                    double[] _sw = new double[num_layers];
                    for (int layer = 0; layer < num_layers; layer++)
                        _sw[layer] = Utility.Math.Divide(_sw_dep[layer], _dlayer[layer], 0.0);
                    return _sw;
                }
                return null;
            }
            set
            {
                if (!initDone)
                {
                    //if we are reading in the [Param], because this variable is "settable" it can be changed from this value, 
                    //therefore store a copy so if there is a Reset event we can set it back to this value.
                    reset_numvals_sw = value.Length;
                    reset_sw = new double[value.Length];
                    Array.Copy(value, reset_sw, value.Length);
                }

                double[] sw_dep_old;
                double sw_dep_lyr, sw_dep_delta_sum;
                sw_dep_old = _sw_dep;
                soilwat2_zero_default_variables();
                int num_layers = _dlayer.Length;
                sw_dep_delta_sum = 0.0;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    sw_dep_lyr = value[layer] * _dlayer[layer];   //sw_dep = sw * dlayer
                    sw_dep_delta_sum = sw_dep_delta_sum + (sw_dep_lyr - sw_dep_old[layer]);   //accumulate the change in the entire soil profile.
                    _sw_dep[layer] = sw_dep_lyr;  //change sw_dep NOT sw. The sw variable is just for inputting and outputting and is immediately converted to sw_dep.    
                    soilwat2_check_profile(layer);
                }
                if (initDone)
                    soilwat2_ExternalMassFlow(sw_dep_delta_sum);     //tell the "System Balance" module (if there is one) that the user has changed the water by this amount.
                numvals_sw = value.Length;          //used in soilwat2_set_default()
            }
        }

        [Bounds(Lower = 0.0, Upper = 1.0)]
#if COMPARISON
    [Units("mm/mm")]
#else
        [Units("0-1")]
#endif
        [Description("15 bar lower limit of extractable soil water for each soil layer")]
        private double[] ll15      //! 15 bar lower limit of extractable soil water for each soil layer
        {
            get
            {
                int num_layers = _dlayer.Length;
                double[] _ll15 = new double[num_layers];
                for (int layer = 0; layer < num_layers; layer++)
                    _ll15[layer] = Utility.Math.Divide(_ll15_dep[layer], _dlayer[layer], 0.0);
                return _ll15;
            }
            set
            {
                if (!initDone)
                {
                    //if we are reading in the [Param], because this variable is "settable" it can be changed from this value, 
                    //therefore store a copy so if there is a Reset event we can set it back to this value.
                    reset_ll15 = new double[value.Length];
                    Array.Copy(value, reset_ll15, value.Length);
                }
                //* made settable to allow for erosion
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    _ll15_dep[layer] = value[layer] * _dlayer[layer];   //change ll15_dep NOT dul. The dll15 variable is just for inputting and outputting and is immediately converted to sw_dep.
                    soilwat2_check_profile(layer);
                }
            }
        }

        [Bounds(Lower = 0.0, Upper = 1.0)]
#if COMPARISON
    [Units("mm/mm")]
#else
        [Units("0-1")]
#endif
        [Description("Air dry soil water content")]
        private double[] air_dry   //! air dry soil water content
        {
            get
            {
                int num_layers = _dlayer.Length;
                double[] _air_dry = new double[num_layers];
                for (int layer = 0; layer < num_layers; layer++)
                    _air_dry[layer] = Utility.Math.Divide(_air_dry_dep[layer], _dlayer[layer], 0.0);
                return _air_dry;
            }
            set
            {
                if (!initDone)
                {
                    //if we are reading in the [Param], because this variable is "settable" it can be changed from this value, 
                    //therefore store a copy so if there is a Reset event we can set it back to this value.
                    reset_air_dry = new double[value.Length];
                    Array.Copy(value, reset_air_dry, value.Length);
                }

                //* made settable to allow for erosion  
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    _air_dry_dep[layer] = value[layer] * _dlayer[layer];   //change air_dry_dep NOT dul. The air_dry variable is just for inputting and outputting and is immediately converted to sw_dep.
                    soilwat2_check_profile(layer);
                }
            }
        }

        #region User interface variables.
        // While the user specifies these variables in the user interface, this SoilWater model shouldn't use them
        // while it is running. Instead it should ask Soil for the value of SWCON, MWCON etc as they will then
        // be mapped into a standardised layer structure. The 4 variables below (Thickness, SWCON, MWCON and KLAT) 
        // may be in a different layer structure.

        [UserInterfaceIgnore]
        public double[] Thickness { get; set; }     //! soil water conductivity constant (1/d) //! ie day**-1 for each soil layer

        [XmlIgnore]
        [Units("cm")]
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

        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/d")]
        [Description("Soil water conductivity constant")]
        public double[] SWCON { get; set; }     //! soil water conductivity constant (1/d) //! ie day**-1 for each soil layer

        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("Impermeable soil layer indicator")]
        public double[] MWCON { get; set; }     //! impermeable soil layer indicator

        [Bounds(Lower = 0, Upper = 1.0e3F)] //1.0e3F = 1000
        [Units("mm/d")]
        public double[] KLAT { get; set; }

        #endregion

        [Description("Flag to determine if Ks has been chosen for use")]
        private bool using_ks;       //! flag to determine if Ks has been chosen for use. //sv- set in soilwat2_init() by checking if mwcon exists

        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm/d")]
        [Description("Saturated conductivity")]
        private double[] ks = null;        //! saturated conductivity (mm/d)

        [Bounds(Lower = 0.01, Upper = 3.0)]
        [Units("g/cm^3")]
        [Description("Bulk density of soil")]
        private double[] bd;      //! moist bulk density of soil (g/cm^3) // ??? Is this "moist" or "dry"; how moist?


        //sv- Lateral Flow profile   //sv- also from Lateral_read_param()




        private double[] _sat_dep;
        [Units("mm")]
        [Description("Sat * dlayer")]
        private double[] sat_dep   // sat * dlayer //see soilwat2_init() for initialisation
        {
            get { return _sat_dep; }
            set
            {
                //* made settable to allow for erosion
                _sat_dep = new double[value.Length];
                Array.Copy(value, _sat_dep, value.Length);
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    soilwat2_check_profile(layer);
                }
            }
        }

        private double[] _dul_dep;
        [Units("mm")]
        [Description("dul * dlayer")]
        private double[] dul_dep   // dul * dlayer  //see soilwat2_init() for initialisation
        {
            get { return _dul_dep; }
            set
            {
                //* made settable to allow for erosion
                _dul_dep = new double[value.Length];
                Array.Copy(value, _dul_dep, value.Length);
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    soilwat2_check_profile(layer);
                }
            }
        }

        private double[] _sw_dep;

        [XmlIgnore]
        [UserInterfaceIgnore]
        [Units("mm")]
        [Description("sw * dlayer")]
        public double[] sw_dep    // sw * dlayer //see soilwat2_init() for initialisation
        {
            get { return _sw_dep; }
            set
            {
                soilwat2_zero_default_variables();
                numvals_sw = value.Length;          //used in soilwat2_set_default()
                _sw_dep = new double[value.Length];
                Array.Copy(value, _sw_dep, value.Length);
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    soilwat2_check_profile(layer);
                }

                //TODO: External Mass Flow event should be triggered just like for sw. Same should go for dlt_sw and dlt_sw_dep.
            }
        }


        private double[] _ll15_dep;
        [Units("mm")]
        [Description("ll15 * dlayer")]
        private double[] ll15_dep  // ll15 * dlayer //see soilwat2_init() for initialisation
        {
            get { return _ll15_dep; }
            set
            {
                //* made settable to allow for erosion
                _ll15_dep = new double[value.Length];
                Array.Copy(value, _ll15_dep, value.Length);
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    soilwat2_check_profile(layer);
                }
            }
        }


        private double[] _air_dry_dep;
        [Units("mm")]
        [Description("air_dry * dlayer")]
        private double[] air_dry_dep  // air_dry * dlayer //see soilwat2_init() for initialisation
        {
            get { return _air_dry_dep; }
            set
            {
                //* made settable to allow for erosion
                _air_dry_dep = new double[value.Length];
                Array.Copy(value, _air_dry_dep, value.Length);
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    soilwat2_check_profile(layer);
                }
            }
        }


        [Units("mm/mm")]
        [Description("Soil water content of layer")]
        private double[] sws       //TODO: this appears to just be an output variable and is identical to sw. I think it should be removed.   //! temporary soil water array used in water_table calculation
        {
            get
            {
                int num_layers = _dlayer.Length;
                double[] result = new double[num_layers];
                for (int layer = 0; layer < num_layers; layer++)
                    result[layer] = Utility.Math.Divide(_sw_dep[layer], _dlayer[layer], 0.0);
                return result;
            }
        }


        [Units("mm")]
        [Description("Depth of water moving from layer i+1 into layer i because of unsaturated flow; (positive value indicates upward movement into layer i) (negative value indicates downward movement (mm) out of layer i)")]
        private double[] flow;        //sv- Unsaturated Flow //! depth of water moving from layer i+1 into layer i because of unsaturated flow; (positive value indicates upward movement into layer i) (negative value indicates downward movement (mm) out of layer i)


        [Units("mm")]
        [Description("Initially, water moving downward into layer i (mm), then water moving downward out of layer i (saturated flow)")]
        private double[] flux;       //sv- Drainage (Saturated Flow) //! initially, water moving downward into layer i (mm), then water moving downward out of layer i (mm)


        [Units("mm")]
        [Description("flow_water[layer] = flux[layer] - flow[layer]")]
        private double[] flow_water         //flow_water[layer] = flux[layer] - flow[layer] 
        {
            get
            {
                int num_layers = flow.Length;
                double[] water_flow = new double[num_layers];
                for (int layer = 0; layer < num_layers; layer++)
                    water_flow[layer] = flux[layer] - flow[layer];
                return water_flow;
            }
        }


        //Soilwat2Globals

        //soilwat2_on_new_solute event handler

        //Lateral Flow profile     //sv- also from Lateral_Send_my_variable()


        [Units("mm")]
        [Description("Lateral outflow")]
        private double[] outflow_lat;   //! outflowing lateral water   //lateral outflow
        //end


        #endregion


        #region Set My Variables (Let other modules change me) (these are sort of like a [Param]'s but after the start of the simulation)

        //These are the sets for ficticious variables that actually set other variables.


        [Units("mm")]
        public double[] dlt_dlayer
        {
            set
            {
                int num_layers = value.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    //If you change the depths of the layer then you need to modify the water "_dep" variables by the same amount. (they are in mm too)
                    //If you don't do this, you will have the same amount of water that is now in a shallower layer, 
                    //therefore you will have a different fraction equivalent variables, the ones without the "_dep" eg. sw, dul.  
                    double fract = Utility.Math.Divide((_dlayer[layer] + value[layer]), _dlayer[layer], 0.0);
                    _air_dry_dep[layer] = _air_dry_dep[layer] * fract;
                    _dul_dep[layer] = _dul_dep[layer] * fract;
                    _ll15_dep[layer] = _ll15_dep[layer] * fract;
                    _sat_dep[layer] = _sat_dep[layer] * fract;
                    _sw_dep[layer] = _sw_dep[layer] * fract;

                    _dlayer[layer] = _dlayer[layer] + value[layer];

                    soilwat2_check_profile(layer);
                }

                //resize all the arrays if they changed the number of layers
                if (num_layers != _dlayer.Length)
                {
                    Array.Resize(ref _air_dry_dep, num_layers);
                    Array.Resize(ref _dul_dep, num_layers);
                    Array.Resize(ref _ll15_dep, num_layers);
                    Array.Resize(ref _sat_dep, num_layers);
                    Array.Resize(ref _sw_dep, num_layers);
                    Array.Resize(ref _dlayer, num_layers);
                    Array.Resize(ref bd, num_layers);
                    Array.Resize(ref es_layers, num_layers);
                    Array.Resize(ref flow, num_layers);
                    Array.Resize(ref flux, num_layers);
                    Array.Resize(ref outflow_lat, num_layers);
                    //also resize for all solutes in this simulation.
                    for (int solnum = 0; solnum < num_solutes; solnum++)
                    {
                        Array.Resize(ref solutes[solnum].amount, num_layers);
                        Array.Resize(ref solutes[solnum].leach, num_layers);
                        Array.Resize(ref solutes[solnum].up, num_layers);
                        Array.Resize(ref solutes[solnum].delta, num_layers);
                    }
                }

                soilwat2_New_Profile_Event();
            }
        }


        [Units("mm")]
        public double[] dlt_sw
        {
            set
            {
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    //sw_dep = sw * dlayer
                    _sw_dep[layer] = _sw_dep[layer] + (value[layer] * _dlayer[layer]);      //change sw_dep NOT sw. The sw variable is just for inputting and outputting and is immediately converted to sw_dep.
                    soilwat2_check_profile(layer);
                }
            }
        }


        [Units("mm")]
        public double[] dlt_sw_dep
        {
            set
            {
                int num_layers = _dlayer.Length;
                for (int layer = 0; layer < num_layers; layer++)
                {
                    _sw_dep[layer] = _sw_dep[layer] + value[layer];
                    soilwat2_check_profile(layer);
                }
            }
        }


        #endregion


        //DAILY INPUTS FROM OTHER MODULES

        #region [INPUTS]

        //taken from soilwat2_get_residue_variables()

        //[Input(IsOptional = true)]
        [Units("0-1")]
        private double surfaceom_cover = 0.0;

        //end of soilwat2_get_residue_variables()

        //taken from soilwat2_get_environ_variables()

        //from met module
        //Runon is specified in a met file or sparse data file
        //[Input(IsOptional = true)]
        [Units("mm/d")]
        private double runon = 0.0;      //! external run-on of H2O (mm/d)

        ////[Input(IsOptional = true)]
        //[Units("mm")]
        //private double snow = 0.0;      //! water content of snow falling during the day

        //from crop modules  
        //used in runoff(as part of TotalInterception parameter) and in infilitration

        //[Input(IsOptional = true)]
        [Units("mm")]
        private double interception = 0.0;      //! canopy interception loss (mm)

        //from surface organic matter module
        //used in runoff(as part of TotalInterception parameter) and in infilitration

        //[Input(IsOptional = true)]
        [Units("mm")]
        private double residueinterception = 0.0;     //residue interception loss (mm)

        //end of soilwat2_get_environ_variables()


        //taken from Lateral_process()

        //from met module
        //Inflow is specified in a met file or sparse data file
        //[Input(IsOptional = true)]
        [Units("mm")]
        private double[] inflow_lat;       //! inflowing lateral water

        //end of Lateral_process()


        #endregion


        #region Get Variables from other Modules (if need to do stuff AFTER inputting)


        private void soilwat2_get_crop_variables()
        {
            //also called in prepare event as well

            //*+  Purpose
            //*      get the value/s of a variable/array.

            //*+  Mission Statement
            //*     Get crop Variables

#if (APSIMX == false)
        Double coverLive;
        Double coverTotal;
        Double height;

        bool foundCL;
        bool foundCT;
        bool foundH;

        int i = 0;
        foreach (Component Comp in MyPaddock.Crops)
        {
            foundCL = MyPaddock.Get(Comp.FullName + ".cover_green", out coverLive);
            foundCT = MyPaddock.Get(Comp.FullName + ".cover_tot", out coverTotal);
            foundH = MyPaddock.Get(Comp.FullName + ".Height", out height);

            ////must have at least these three variables to be considered a "crop" component.
            if (foundCL && foundCT && foundH)
                {
                num_crops = i + 1;
                Array.Resize(ref cover_green, num_crops);
                Array.Resize(ref cover_tot, num_crops);
                Array.Resize(ref canopy_height, num_crops);
                cover_green[i] = coverLive;
                cover_tot[i] = coverTotal;
                canopy_height[i] = height;
                i++;
                }
            else
                {
                throw new Exception("Crop Module: " +  Comp.FullName  + 
                        " is missing one/or more of the following 3 output variables (cover_green, cover_tot, height) " + Environment.NewLine +
                        "These 3 output variables are needed by the SoilWater module (for evaporation, runoff etc.");
                }
        }
#endif

        }


        private void soilwat2_get_solute_variables()
        {
            //for the number of solutes that was read in by OnNewSolute event handler)
            for (int solnum = 0; solnum < num_solutes; solnum++)
            {
                double[] Value;
                string propName;
                if (solutes[solnum].ownerName != "")
                    propName = solutes[solnum].ownerName + "." + solutes[solnum].name;
                else
                    propName = solutes[solnum].name;
                object objValue = this.Get(propName);
                if (objValue != null)
                {
                    Value = objValue as double[];
                    // Should check array size here to be sure it matches...
                    Array.Copy(Value, solutes[solnum].amount, Math.Min(Value.Length, solutes[solnum].amount.Length));
                }
            }
        }

        //this is called in the On Process event handler
        //it just calls all the methods above.
        private void soilwat2_get_other_variables()
        {

            soilwat2_get_crop_variables();

            soilwat2_get_solute_variables();

        }

        #endregion


        #region Set Variables in other Modules (Solute model mainly)

        private float[] ToFloatArray(double[] D)
        {
            float[] f = new float[D.Length];
            for (int i = 0; i < D.Length; i++)
                f[i] = (float)D[i];
            return f;
        }


        private void SetModuleSolutes()
        {
            //taken from soilwat2_set_other_variables()

            NitrogenChangedType NitrogenChanges = new NitrogenChangedType();
            NitrogenChanges.Sender = "SoilWater";
            NitrogenChanges.SenderType = "WateModule";
            NitrogenChanges.DeltaUrea = new double[dlayer.Length];
            NitrogenChanges.DeltaNH4 = new double[dlayer.Length];
            NitrogenChanges.DeltaNO3 = new double[dlayer.Length];

            //for all solutes in this simulation.
            for (int solnum = 0; solnum < num_solutes; solnum++)
            {
                //convert to float array
                float[] temp_dlt_solute = ToFloatArray(solutes[solnum].delta);

                //set the change in solutes for the modules
                if (solutes[solnum].name == "urea")
                    NitrogenChanges.DeltaUrea = solutes[solnum].delta;
                else if (solutes[solnum].name == "nh4")
                    NitrogenChanges.DeltaNH4 = solutes[solnum].delta;
                else if (solutes[solnum].name == "no3")
                    NitrogenChanges.DeltaNO3 = solutes[solnum].delta;
                else
                {
                    throw new NotImplementedException("Cannot do a set for solute: " + solutes[solnum].name);

                    //string propName;
                    //if (solutes[solnum].ownerName != "")
                    //    propName = solutes[solnum].ownerName + ".dlt_" + solutes[solnum].name;
                    //else
                    //    propName = "dlt_" + solutes[solnum].name;
                    //MyPaddock.Set(propName, temp_dlt_solute);
                }
            }
            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NitrogenChanges);
        }

        //this is called in the On Process event handler
        private void soilwat2_set_other_variables()
        {

            SetModuleSolutes();

            //! Send a runoff event to the system
            if (runoff > 0.0)
            {
                RunoffEventType r = new RunoffEventType(); //! structure holding runoff event
                r.runoff = (float)runoff;
                if (Runoff != null)
                    Runoff.Invoke(r);
            }

        }


        #endregion



        //LOCAL VARIABLES

        #region Local Variables

        //! ====================================================================
        //!     soilwat2 constants
        //! ====================================================================

        //Soilwat2Globals


        //MET
        //sv- These met variables get assigned by the OnNewMet Event Handler
        private double rain;         //! precipitation (mm/d)
        private double radn;         //! solar radiation (mj/m^2/day)
        private double mint;         //! minimum air temperature (oC)
        private double maxt;         //! maximum air temperature (oC)

        //RUNOFF
        //r double      cover_surface_runoff;
        //r double runoff;
        //who put this in? double      eff_rain; 
        private double runoff_pot;       //! potential runoff with no pond(mm)
        //private double obsrunoff;         //! observed runoff (mm)

        //GET CROP VARIABLES
        //private int[]       crop_module = new int[max_crops];             //! list of modules replying 
        private double[] cover_tot = null;     //! total canopy cover of crops (0-1)
        private double[] cover_green = null;   //! green canopy cover of crops (0-1)
        private double[] canopy_height = null; //! canopy heights of each crop (mm)
        private int num_crops = 0;                //! number of crops ()

        //TILLAGE EVENT
        private double tillage_cn_red;   //! reduction in CN due to tillage ()   //can either come from the manager module or from the sim file
        private double tillage_cn_rain;  //! cumulative rainfall below which tillage reduces CN (mm) //can either come from the manager module orh the sim file
        private double tillage_rain_sum; //! cumulative rainfall for tillage CN reduction (mm)

        //EVAPORATION
        private int year;         //! year
        private int day;          //! day of year
        private double sumes1;       //! cumulative soil evaporation in stage 1 (mm)
        private double sumes2;       //! cumulative soil evaporation in stage 2 (mm)
        //r double      t;
        //private double eo_system;         //! eo from somewhere else in the system //sv- see eo_source
        //r double eo;
        private double real_eo;                  //! potential evapotranspiration (mm) 
        //r double eos;
        private double[] es_layers = null;     //! actual soil evaporation (mm)


        //r double drain;
        //r double infiltration;


        //SOLUTES
        //OnNewSolute
        private class Solute
        {
            public string name = "";        // Name of the solute
            public string ownerName = "";    // FQN of the component handling this solute
            public bool mobility = false;      // Is the solute mobile?
            public double[] amount;    // amount of solute in each layer (kg/ha)
            public double[] leach;     // amount leached from each layer (kg/ha)
            public double[] up;        // amount "upped" from each layer (kg/ha)
            public double[] delta;     // change in solute in each layer (kg/ha)
            public double rain_conc;   // concentration entering via rainfall (ppm)
            public double irrigation;  // amount of solute in irrigation water (kg/ha)
            public int get_flow_id = 0;    // registration ID for getting flow values
            public int get_leach_id = 0;    // registration ID for getting leach value
        };

        private Solute[] solutes = null;
        int num_solutes = 0;

        //IRRIGATION
        private double irrigation;       //! irrigation (mm)                                                 

        //r double pond_evap;
        //r double pond;
        //r double water_table;
        //r double[] sws;

        private double oldSWDep;


        //end Soilwat2Globals

        //The following are used for doing a Reset Event. 
        //They are used to store the original values read in by the [Param] tags.
        //They only apply to [Param] tags that are also "Settable" properties which can be changed by the user.
        //Settable [Param]'s need to have their original values stored because the user can alter them and if a reset is done we need to set them back 
        //to what was originally read in. The .NET infrastructure of APSIM does not really let you do this. You only get to read in from the .sim file once at the
        //start of the simulation. So we use these variables to compensate.

        //Soil Property
        //initial starting soil water 
        //runoff
        double reset_cn2_bare, reset_cn_red, reset_cn_cov;
        //ponding 
        double reset_max_pond;

        //Soil Profile
        int reset_numvals_sw;         //used in soilwat2_set_default()
        double[] reset_dlayer, reset_sat, reset_dul, reset_sw, reset_ll15, reset_air_dry;



        #endregion


        //MODEL

        #region Functions to Zero Variables


        private void soilwat2_zero_variables()
        {

            //You only really want to zero, 
            // Ouputs, Local Variables, 
            // and Settable Params (which you are using reset variables to store the original value in)
            //You do not want to zero non Settable Params because there is no way to reread them back in again. 
            //Plus they don't change during the simulation so why bother.  
            //By definition you don't want to reset the module constants. ( except the ones changed in soilwat2_read_constants() )


            //Settable Params
            //! ie day**-1 for each soil layer
            _cn2_bare = 0.0;                         //! curve number input used to calculate daily g_runoff
            _cn_cov = 0.0;                           //! cover at which c_cn_red occurs
            _cn_red = 0.0;                           //! maximum reduction in p_cn2_bare due to cover

            _max_pond = 0.0;                         //! maximum allowable surface storage (ponding) mm

            numvals_sw = 0;                         //! number of values returned for sw

            ZeroArray(ref _dlayer);                   //! thickness of soil layer i (mm)
            ZeroArray(ref _sat_dep);                  //! saturated water content for layer l (mm water)
            ZeroArray(ref _dul_dep);                  //! drained upper limit soil water content for each soil layer (mm water)
            ZeroArray(ref _sw_dep);                   //! soil water content of layer l (mm)
            ZeroArray(ref _ll15_dep);                 //! 15 bar lower limit of extractable soil water for each soil layer(mm water)
            ZeroArray(ref _air_dry_dep);              //! air dry soil water content (mm water)

            _water_table = 0.0;                      //! water table depth (mm)

            //Outputs
            drain = 0.0;                            //! drainage rate from bottom layer (cm/d)
            infiltration = 0.0;                     //! infiltration (mm)
            runoff = 0.0;                           //! runoff (mm)

            pond = 0.0;                             //! surface ponding depth (mm)
            pond_evap = 0.0;                        //! evaporation from the pond surface (mm)
            eo = 0.0;                               //! potential evapotranspiration (mm)
            eos = 0.0;                              //! pot sevap after modification for green cover & residue wt
            t = 0.0;                                //! time after 2nd-stage soil evaporation begins (d)
            cn2_new = 0.0;                          //! New cn2  after modification for crop cover & residue cover
            cover_surface_runoff = 0.0;             //! effective total cover (0-1)
            ZeroArray(ref flow);                     //! depth of water moving from layer l+1
            //! into layer l because of unsaturated
            //! flow; positive value indicates upward
            //! movement into layer l, negative value
            //! indicates downward movement (mm) out of layer l
            ZeroArray(ref flux);                     //! initially, water moving downward into layer l (mm), 
            //then water moving downward out of layer l (mm)
            ZeroArray(ref es_layers);                //! actual soil evaporation (mm)

            ZeroArray(ref outflow_lat);

            //Local Variables

            cover_tot = null;                //! total canopy cover of crops (0-1)
            cover_green = null;              //! green canopy cover of crops (0-1)
            canopy_height = null;            //! canopy heights of each crop (mm)
            num_crops = 0;                          //! number of crops ()
            sumes1 = 0.0;                           //! cumulative soil evaporation in stage 1 (mm)
            sumes2 = 0.0;                           //! cumulative soil evaporation in stage 2 (mm)

            for (int sol = 0; sol < num_solutes; sol++)
            {
                ZeroArray(ref solutes[sol].amount);
                ZeroArray(ref solutes[sol].delta);
                ZeroArray(ref solutes[sol].leach);  //! amount of solute leached from each layer (kg/ha)
                ZeroArray(ref solutes[sol].up);     //! amount of solute upped from each layer (kg/ha)
                solutes[sol].rain_conc = 0.0;
                solutes[sol].irrigation = 0.0;
            }

            runoff_pot = 0.0;                       //! potential runoff with no pond(mm)  
            irrigation = 0.0;                       //! irrigation (mm)

            //obsrunoff = 0.0;                        //! observed runoff (mm)
            tillage_cn_red = 0.0;                   //! reduction in CN due to tillage ()
            tillage_cn_rain = 0.0;                  //! cumulative rainfall below which tillage reduces CN (mm)
            tillage_rain_sum = 0.0;                 //! cumulative rainfall for tillage CN reduction (mm)
            obsrunoff_name = "";                    //! system name of observed runoff

            //eo_system = 0.0;                        //! eo from somewhere else in the system
            _eo_source = "";                        //! system variable name of external eo source

            real_eo = 0.0;                          //! eo determined before any ponded water is evaporated (mm)

            irrigation_layer = 0;                   //! trickle irrigation input layer
        }


        /*
        //TODO: this is used by the soilwat2_set_my_variables(). This allows other modules to set soilwat's variables.
        // this is implememented in .NET by declaring a Property with Gets and Sets and making it an INPUT tag. Nb. that i think you have to use a local variable as a go between as well. See SoilNitrogen [Input] tags with get and set. Or maybet it is  tags.
        */
        private void soilwat2_zero_default_variables()
        {

            //*+  Mission Statement
            //*     zero default soil water initialisation parameters      

            numvals_sw = 0;
            ZeroArray(ref _sw_dep);
        }


        private void soilwat2_zero_daily_variables()
        {

            //sv- this is exectued in the Prepare event.

            ZeroArray(ref flow);
            ZeroArray(ref flux);
            ZeroArray(ref es_layers);
            cover_tot = null;
            cover_green = null;
            canopy_height = null;
            //ZeroArray(ref crop_module, max_crops);

            eo = 0.0;
            eos = 0.0;
            cn2_new = 0.0;
            drain = 0.0;
            infiltration = 0.0;
            runoff = 0.0;
            runoff_pot = 0.0;
            num_crops = 0;
            //obsrunoff = 0.0;
            pond_evap = 0.0;                    //! evaporation from the pond surface (mm)
            real_eo = 0.0;                      //! eo determined before any ponded water is evaporated (mm)


            //! initialise all solute information
            for (int solnum = 0; solnum < num_solutes; solnum++)
            {
                ZeroArray(ref solutes[solnum].amount);
                ZeroArray(ref solutes[solnum].leach);
                ZeroArray(ref solutes[solnum].up);
                ZeroArray(ref solutes[solnum].delta);
                solutes[solnum].rain_conc = 0.0;
            }

        }


        private void Lateral_zero_daily_variables()
        {
            ZeroArray(ref outflow_lat);
        }


        private void ZeroArray(ref double[] A)
        {
            if (A != null)
            {
                for (int i = 0; i < A.Length; i++)
                {
                    A[i] = 0.0;
                }
            }
        }

        #endregion


        #region Bounds checking and warning functions

#if (APSIMX == true)
        private void IssueWarning(string warningText)
        {
            Console.WriteLine(warningText);
        }
#else
    [Link]
    private Component My = null;  // Get access to "Warning" function
    private void IssueWarning(string warningText)
    {
    My.Warning(warningText);
    }
#endif


        private double bound(double A, double Lower, double Upper)
        {
            //force A to stay between the Lower and the Upper. Set A to the Upper or Lower if it exceeds them.
            if (Lower > Upper)
            {
                IssueWarning("Lower bound " + Lower + " is > upper bound " + Upper + "\n"
                                   + "        Variable is not constrained");
                return A;
            }
            else
                return Math.Max(Math.Min(A, Upper), Lower);
        }


        // Unlike u_bound and l_bound, this does not force the variable to be between the bounds. It just warns the user in the summary file.
        protected void bound_check_real_var(double Variable, double LowerBound, double UpperBound, string VariableName)
        {
            string warningMsg;
            if (Variable > UpperBound)
            {
                warningMsg = "The variable: /'" + VariableName + "/' is above the expected upper bound of: " + UpperBound;
                IssueWarning(warningMsg);
            }
            if (Variable < LowerBound)
            {
                warningMsg = "The variable: /'" + VariableName + "/' is below the expected lower bound of: " + LowerBound;
                IssueWarning(warningMsg);
            }
        }

        protected void bound_check_real_array(double[] A, double LowerBound, double UpperBound, string ArrayName, int ElementToStopChecking)
        {
            for (int i = 0; i < ElementToStopChecking; i++)
            {
                bound_check_real_var(A[i], LowerBound, UpperBound, ArrayName + "(" + i + 1 + ")");
            }
        }

        #endregion

        #region Functions to Set Intial SW and Error Check Soil Profile


        #region Set Initial SW values



        //sv- DEAN SAYS THAT THE GUI ALWAYS SPECIFIES A SET OF SW VALUES. THEREFORE YOU DON'T NEED ANY OF THIS CODE TO SET DEFAULTS ANYMORE. ALL OF THIS IS DONE IN THE GUI NOW AND YOU JUST GET GIVE THE SW VALUES FOR EACH LAYER. SO DON'T NEED THIS ANYMORE.
        //Had to uncomment this because it is called in the "set" for the "insoil" property. I don't think any simulation actually does a set on "insoil" though
        //so perhaps I can comment it out and turn "insoil" just into a normal variable that is a [Param].
        //TODO: see if I can comment out the soilwat2_set_default() as per the comments above.

        private double root_proportion(int Layer, double RootDepth)
        {

            //integer    layer                 ! (INPUT) layer to look at
            //real       root_depth            ! (INPUT) depth of roots

            //!+ Purpose
            //!       returns the proportion of layer that has roots in it (0-1).

            //!+  Definition
            //!     Each element of "dlayr" holds the height of  the
            //!     corresponding soil layer.  The height of the top layer is
            //!     held in "dlayr"(1), and the rest follow in sequence down
            //!     into the soil profile.  Given a root depth of "root_depth",
            //!     this function will return the proportion of "dlayr"("layer")
            //!     which has roots in it  (a value in the range 0..1).

            //!+  Mission Statement
            //!      proportion of layer %1 explored by roots

            double depth_to_layer_bottom;  //! depth to bottom of layer (mm)
            double depth_to_layer_top;     //! depth to top of layer (mm)
            double depth_to_root;          //! depth to root in layer (mm)
            double depth_of_root_in_layer; //! depth of root within layer (mm)

            depth_to_layer_bottom = Utility.Math.Sum(_dlayer, 0, Layer, 0.0);
            depth_to_layer_top = depth_to_layer_bottom - _dlayer[Layer - 1];
            depth_to_root = Math.Min(depth_to_layer_bottom, RootDepth);

            depth_of_root_in_layer = Math.Max(depth_to_root - depth_to_layer_top, 0.0);
            return Utility.Math.Divide(depth_of_root_in_layer, _dlayer[Layer - 1], 0.0);

        }


        //All the following function are used ONLY in soilwat2_init() no where else.

        private void soilwat2_read_constants()
        {

            //##################
            //Constants    --> soilwat2_read_constants()
            //##################


            num_solute_flow = solute_flow_eff.Length;
            num_solute_flux = solute_flux_eff.Length;


            if (canopy_fact.Length != canopy_fact_height.Length)
            {
                throw new Exception("No. of canopy_fact coeffs do not match the no. of canopy_fact_height coeffs.");
            }

            //sv- the following test is removed from soilwat2_read_constants() too
            switch (act_evap_method)
            {
                case "ritchie":
                    evap_method = ritchie_method;  //ritchie_method = 1
                    break;
                case "bs_a":
                    evap_method = 2;
                    break;
                case "bs_b":
                    evap_method = 3;
                    break;
                case "bs_acs_jd":
                    evap_method = 4;
                    break;
                case "rickert":
                    evap_method = 5;
                    break;
                case "rwc":
                    evap_method = 6;
                    break;
                default:
                    evap_method = ritchie_method;
                    break;
            }


            if (evap_method != ritchie_method)
            {
                evap_method = ritchie_method;
                IssueWarning("Your ini file is set to use an evaporation method other than ritchie(act_evap_method=1)." + "\n"
                                        + "This module: SoilWater can only use ritchie evaporation." + "\n"
                                        + "Your evaporation method has therefore been reset to ritchie(act_evap_method=1).");
            }


            //##################
            //End of Constants
            //##################

        }


        private void soilwat2_soil_property_param()
        {

            //##################
            //Soil Properties  --> soilwat2_soil_property_param()
            //##################

            //If this function has been called by a Reset Event
            //then reset (all the variables that are "Settable" by the user) back to the original values read in by [Param]  
            if (initDone)
            {
                //Soil Property


                //runoff
                _cn2_bare = reset_cn2_bare;
                _cn_red = reset_cn_red;
                _cn_cov = reset_cn_cov;

                //ponding
                _max_pond = reset_max_pond;

            }

            //sv- the following test is removed from soilwat2_soil_property_param()
            if (_cn_red >= _cn2_bare)
            {
                _cn_red = _cn2_bare - 0.00009;
            }


            //****************
            //U and Cona (used in Ritchie Evaporation)
            //*****************

            //sv- the following test is removed from soilwat2_soil_property_param()

            //u - can either use (one value for summer and winter) or two different values.
            //    (must also take into consideration where they enter two values [one for summer and one for winter] but they make them both the same)
            if (!initDone)
            {
                if (Double.IsNaN(_u))
                {
                    if ((Double.IsNaN(SummerU) || (Double.IsNaN(WinterU))))
                    {
                        throw new Exception("A single value for u OR BOTH values for summeru and winteru must be specified");
                    }
                    //if they entered two values but they made them the same
                    if (SummerU == WinterU)
                    {
                        _u = SummerU;      //u is now no longer null. As if the user had entered a value for u.
                    }
                }
                else
                {
                    SummerU = _u;
                    WinterU = _u;
                }

                //cona - can either use (one value for summer and winter) or two different values.
                //       (must also take into consideration where they enter two values [one for summer and one for winter] but they make them both the same)
                if (Double.IsNaN(_cona))
                {
                    if ((Double.IsNaN(SummerCona)) || (Double.IsNaN(WinterCona)))
                    {
                        throw new Exception("A single value for cona OR BOTH values for summercona and wintercona must be specified");
                    }
                    //if they entered two values but they made them the same.
                    if (SummerCona == WinterCona)
                    {
                        _cona = SummerCona;   //cona is now no longer null. As if the user had entered a value for cona.
                    }
                }
                else
                {
                    SummerCona = _cona;
                    WinterCona = _cona;
                }

                //summer and winter default dates.
                if (SummerDate == "not_read")
                {
                    SummerDate = "1-oct";
                }

                if (WinterDate == "not_read")
                {
                    WinterDate = "1-apr";
                }
            }

            //assign u and cona to either sumer or winter values
            // Need to add 12 hours to move from "midnight" to "noon", or this won't work as expected
            if (Utility.Date.WithinDates(WinterDate, Clock.Today, SummerDate))
            {
                _cona = WinterCona;
                _u = WinterU;
            }
            else
            {
                _cona = SummerCona;
                _u = SummerU;
            }


            //***************
            //end U and Cona
            //***************




            //##################
            //End of Soil Properties
            //##################


        }


        private void soilwat2_soil_profile_param()
        {

            //##################
            //Soil Profile  -->  soilwat2_soil_profile_param()
            //##################

#if COMPARISON
        Console.WriteLine("     ");
        Console.WriteLine("     - Reading constants");
        Console.WriteLine("     ");
        Console.WriteLine("     - Reading Soil Property Parameters");
        Console.WriteLine("     ");
#endif
            Console.WriteLine("   - Reading Soil Profile Parameters");


            //Initialise the Optional Array Parameters (if not read in).

            if (!initDone) // If this is actual initialisation, establish whether we will use ks
            {
                //sv- with mwcon: 0 is impermeable and 1 is permeable.
                //sv- if mwcon is not specified then set it to 1 and don't use ks. If it is specified then use mwcon and use ks. 
                //c dsg - if there is NO impermeable layer specified, then mwcon must be set to '1' in all layers by default.
                if (MWCON != null)
                {
                    IssueWarning("mwcon is being replaced with a saturated conductivity (ks). " + "\n"
                                        + "See documentation for details.");
                }

                //for (klat == null) see Lateral_init().


                if (ks == null)
                {
                    using_ks = false;
                    ks = new double[_dlayer.Length];
                    ZeroArray(ref ks);
                }
                else
                {
                    using_ks = true;
                }
            }
            else
            //If this function has been called by a Reset Event
            //then reset (all the variables that are "Settable" by the user) back to the original values read in by [Param]  
            {
                inReset = true; // Temporarily turn of profile checking, until we've reset all values
                //soil profile
                _dlayer = new double[reset_dlayer.Length];
                Array.Copy(reset_dlayer, _dlayer, reset_dlayer.Length);
                sat = reset_sat;
                dul = reset_dul;
                numvals_sw = reset_numvals_sw;  //used in soilwat2_set_default();
                sw = reset_sw;
                ll15 = reset_ll15;
                air_dry = reset_air_dry;
                inReset = false;
            }


            //sv-end

            //*****************
            //End of Initial SW  
            //*****************

            //##################
            //End of Soil Profile
            //##################

        }


        private void soilwat2_evap_init()
        {

            //##################
            //Evap Init   --> soilwat2_evap_init (), soilwat2_ritchie_init()
            //##################   

            if (evap_method == ritchie_method)
            {
                //soilwat2_ritchie_init();
                //*+  Mission Statement
                //*       Initialise ritchie evaporation model

                double swr_top;       //! stage 2 evaporation occurs ratio available sw potentially available sw in top layer

                //! set up evaporation stage
                swr_top = Utility.Math.Divide((_sw_dep[0] - ll15_dep[0]), (_dul_dep[0] - _ll15_dep[0]), 0.0);
                swr_top = bound(swr_top, 0.0, 1.0);

                //! are we in stage1 or stage2 evap?
                if (swr_top < sw_top_crit)
                {
                    //! stage 2 evap
                    sumes2 = sumes2_max - (sumes2_max * Utility.Math.Divide(swr_top, sw_top_crit, 0.0));
                    sumes1 = _u;
                    t = Utility.Math.Sqr(Utility.Math.Divide(sumes2, _cona, 0.0));
                }
                else
                {
                    //! stage 1 evap
                    sumes2 = 0.0;
                    sumes1 = sumes1_max - (sumes1_max * swr_top);
                    t = 0.0;
                }
            }
            else
            {
                throw new Exception("Tried to initialise unknown evaporation method");
            }

            //##################
            //End of Evap Init
            //##################


        }


        private void Lateral_init()
        {

            //##################
            //Lateral Init  --> lateral_init(lateral)
            //##################


            //sv- the following test is removed from Lateral_read_param()
            //sv- Lateral variables are all optional so zero them if not entered by user.
            //These are optional parameters and so they may have a default value of NaN(double vars) or null(array vars) if they were not read in.
            //So set them to zero.

            if (Double.IsNaN(slope))
                slope = 0.0;

            if (Double.IsNaN(discharge_width))
                discharge_width = 0.0;

            if (Double.IsNaN(catchment_area))
                catchment_area = 0.0;

            if (KLAT == null)
                KLAT = new double[_dlayer.Length];

            //taken from Lateral_zero_variables()
            ZeroArray(ref outflow_lat);

            //see Lateral_process() for where daily input inflow_lat[] is initialised if not read in.

            //##################
            //End of Lateral Init  
            //##################
        }


        #endregion



        #region Check a given layer for Errors


        private void soilwat2_layer_check(int layer)
        {

            //sv- this function is only ever used in the function soilwat2_check_profile(int layer)

            //*+  Purpose
            //*       checks that layer lies in range of 1 - num_layers

            //*+  Notes
            //*             reports error if layer < min_layer
            //*             or layer > num_layers

            //*+  Mission Statement
            //*     Check Soil Water Parameters for a given layer

            int min_layer = 1;      //! lowest value for a layer number

            string error_messg;
            int num_layers;

            num_layers = _dlayer.Length;

            if (layer < min_layer)
            {
                error_messg = String.Format("{0} {1} {2} {3}",
                                            " soil layer no. ", layer,
                                            " is below mimimum of ", min_layer);
                IssueWarning(error_messg);
            }
            else if (layer > num_layers)
            {
                error_messg = String.Format("{0} {1} {2} {3}",
                                            " soil layer no. ", layer,
                                            " is above maximum of ", num_layers);
                IssueWarning(error_messg);
            }
        }

        private void soilwat2_check_profile(int layer)
        {
            //*+  Purpose
            //*       checks validity of soil water parameters for a soil profile layer

            //*+  Notes
            //*           reports an error if
            //*           - g%ll15_dep, _dul_dep, and _sat_dep are not in ascending order
            //*           - ll15 is below min_sw
            //*           - sat is above max_sw
            //*           - sw > sat or sw < min_sw      

            if (inReset || !initDone)
                return;

            //Constant Values
            double min_sw_local = 0.0;
            double max_sw_margin = 0.01;

            string err_messg;           //! error message

            double dul_local;                 //! drained upper limit water content of layer (mm water/mm soil)
            double dul_errmargin;       //! rounding error margin for dulc
            double ll15_local;                //! lower limit at 15 bars water content of layer (mm water/mm soil)
            double ll15_errmargin;      //! rounding error margin for ll15c
            double air_dry_local;             //! lower limit at air dry water content of layer (mm water/mm soil)
            double air_dry_errmargin;   //! rounding error margin for air_dryc
            double sat_local;                 //! saturated water content of layer (mm water/mm soil)
            double sat_errmargin;       //! rounding error margin for satc
            double sw_local;                  //! soil water content of layer l (mm water/mm soil)
            double sw_errmargin;        //! rounding error margin for swc

            double max_sw_local;              //! largest acceptable value for sat (mm water/mm soil)

            max_sw_local = 1.0 - Utility.Math.Divide(bd[layer], specific_bd, 0.0);  //ie. Total Porosity

            sw_local = Utility.Math.Divide(_sw_dep[layer], _dlayer[layer], 0.0);
            sat_local = Utility.Math.Divide(_sat_dep[layer], _dlayer[layer], 0.0);
            dul_local = Utility.Math.Divide(_dul_dep[layer], _dlayer[layer], 0.0);
            ll15_local = Utility.Math.Divide(_ll15_dep[layer], _dlayer[layer], 0.0);
            air_dry_local = Utility.Math.Divide(_air_dry_dep[layer], _dlayer[layer], 0.0);

            //TODO: where do these error_margins come from?
            sw_errmargin = error_margin;
            sat_errmargin = error_margin;
            dul_errmargin = error_margin;
            ll15_errmargin = error_margin;
            air_dry_errmargin = error_margin;


            if ((air_dry_local + air_dry_errmargin) < min_sw_local)
            {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                           " Air dry lower limit of ",
                                           air_dry_local,
                                           " in layer ",
                                           layer,
                                           "\n",
                                           "         is below acceptable value of ",
                                           min_sw_local);
                IssueWarning(err_messg);
            }


            if ((ll15_local + ll15_errmargin) < (air_dry_local - air_dry_errmargin))
            {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                           " 15 bar lower limit of ",
                                           ll15_local,
                                           " in layer ",
                                           layer,
                                           "\n",
                                           "         is below air dry value of ",
                                           air_dry_local);
                IssueWarning(err_messg);
            }



            if ((dul_local + dul_errmargin) <= (ll15_local - ll15_errmargin))
            {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                           " drained upper limit of ",
                                           dul_local,
                                           " in layer ",
                                           layer,
                                           "\n",
                                           "         is at or below lower limit of ",
                                           ll15_local);
                IssueWarning(err_messg);
            }

            if ((sat_local + sat_errmargin) <= (dul_local - dul_errmargin))
            {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G})",
                                           " saturation of ",
                                           sat_local,
                                           " in layer ",
                                           layer,
                                           "\n",
                                           "         is at or below drained upper limit of ",
                                           dul_local);
                IssueWarning(err_messg);
            }

            if ((sat_local - sat_errmargin) > (max_sw_local + max_sw_margin))
            {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G} {7} {8} {9:G} {10} {11} {12:G})",
                                           " saturation of ",
                                           sat_local,
                                           " in layer ",
                                           layer,
                                           "\n",
                                           "         is above acceptable value of ",
                                           max_sw_local,
                                           "\n",
                                           "You must adjust bulk density (bd) to below ",
                                           (1.0 - sat_local) * specific_bd,
                                           "\n",
                                           "OR saturation (sat) to below ",
                                           max_sw_local);
                IssueWarning(err_messg);
            }


            if (sw_local - sw_errmargin > sat_local + sat_errmargin)
            {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G}",
                                           " soil water of ",
                                           sw_local,
                                           " in layer ",
                                           layer,
                                           "\n",
                                           "         is above saturation of ",
                                           sat_local);
                IssueWarning(err_messg);
            }

            if (sw_local + sw_errmargin < air_dry_local - air_dry_errmargin)
            {
                err_messg = String.Format("({0} {1:G}) {2} {3} {4} {5} {6:G}",
                                           " soil water of ",
                                           sw_local,
                                           " in layer ",
                                           layer,
                                           "\n",
                                           "         is below air-dry value of ",
                                           air_dry_local);
                IssueWarning(err_messg);
            }

        }


        #endregion


        #endregion


        #region Soil Science Functions

        private int FindLayerNo(double Depth)
        {
            // Find the soil layer in which the indicated depth is located
            // NOTE: The returned layer number is 0-based
            // If the depth is not reached, the last element is used
            double depth_cum = 0.0;
            for (int i = 0; i < _dlayer.Length; i++)
            {
                depth_cum = depth_cum + _dlayer[i];
                if (depth_cum >= Depth)
                    return i;
            }
            return _dlayer.Length - 1;
        }


        #region Runoff


        private void soilwat2_runoff(double Rain, double Runon, double TotalInterception, ref double Runoff)
        {
            Runoff = 0.0;  //zero the return parameter

            if ((Rain + Runon - TotalInterception) > 0.0)
            {
                if (obsrunoff_name == "")
                {
                    soilwat2_scs_runoff(Rain, Runon, TotalInterception, ref Runoff);
                }
                else
                {
                    //obsrunoff = Double.NaN;
                    //if (My.Get(obsrunoff_name, out obsrunoff) && !Double.IsNaN(obsrunoff))
                    //    runoff = obsrunoff;
                    //else
                    {
                        //          write (line, '(a,i4,a,i3,a)')
                        string line = String.Format("{0} {1} {2} {3} {4}",
                                                   "Year = ",
                                                   year,
                                                   ", day = ",
                                                   day,
                                                   ", Using predicted runoff for missing observation");

                        IssueWarning(line);
                        soilwat2_scs_runoff(Rain, Runon, TotalInterception, ref Runoff);
                    }
                }

                //The reduction in the runoff as a result of doing a tillage (tillage_cn_red) ceases after a set amount of rainfall (tillage_cn_rain).
                //this function works out the accumulated rainfall since last tillage event, and turns off the reduction if it is over the amount of rain specified.
                soilwat2_tillage_addrain(Rain, Runon, TotalInterception); //! Update rain since tillage accumulator. ! NB. this needs to be done _after_ cn calculation.
            }
        }



        private void soilwat2_scs_runoff(double Rain, double Runon, double TotalInterception, ref double Runoff)
        {
            double cn;                                 //! scs curve number
            double cn1;                                //! curve no. for dry soil (antecedant) moisture
            double cn3;                                //! curve no. for wet soil (antecedant) moisture
            double cover_fract;                        //! proportion of maximum cover effect on runoff (0-1)
            double cnpd;                               //! cn proportional in dry range (dul to ll15)
            int layer;                              //! layer counter
            int num_layers;                         //! number of layers
            double s;                                  //! potential max retention (surface ponding + infiltration)
            double xpb;                                //! intermedite variable for deriving runof
            double[] runoff_wf;                        //! weighting factor for depth for each la
            double dul_fraction;                       // if between (0-1) not above dul, if (1-infinity) above dul 
            double tillage_reduction;                  //! reduction in cn due to tillage

            num_layers = _dlayer.Length;
            runoff_wf = new double[num_layers];

            soilwat2_runoff_depth_factor(ref runoff_wf);

            cnpd = 0.0;
            for (layer = 0; layer < num_layers; layer++)
            {
                dul_fraction = Utility.Math.Divide((_sw_dep[layer] - _ll15_dep[layer]), (_dul_dep[layer] - _ll15_dep[layer]), 0.0);
                cnpd = cnpd + dul_fraction * runoff_wf[layer];
            }
            cnpd = bound(cnpd, 0.0, 1.0);


            //reduce cn2 for the day due to the cover effect
            //nb. cover_surface_runoff should really be a parameter to this function
            cover_fract = Utility.Math.Divide(cover_surface_runoff, _cn_cov, 0.0);
            cover_fract = bound(cover_fract, 0.0, 1.0);
            cn2_new = _cn2_bare - (_cn_red * cover_fract);


            //tillage reduction on cn
            //nb. tillage_cn_red, tillage_cn_rain, and tillage_rain_sum, should really be parameters to this function
            if (tillage_cn_rain > 0.0)
            {
                tillage_reduction = tillage_cn_red * (Utility.Math.Divide(tillage_rain_sum, tillage_cn_rain, 0.0) - 1.0);
                cn2_new = cn2_new + tillage_reduction;
            }
            else
            {
                //nothing
            }


            //! cut off response to cover at high covers if p%cn_red < 100.
            cn2_new = bound(cn2_new, 0.0, 100.0);

            cn1 = Utility.Math.Divide(cn2_new, (2.334 - 0.01334 * cn2_new), 0.0);
            cn3 = Utility.Math.Divide(cn2_new, (0.4036 + 0.005964 * cn2_new), 0.0);
            cn = cn1 + (cn3 - cn1) * cnpd;

            // ! curve number will be decided from scs curve number table ??dms
            s = 254.0 * (Utility.Math.Divide(100.0, cn, 1000000.0) - 1.0);
            xpb = (Rain + Runon - TotalInterception) - 0.2 * s;
            xpb = Math.Max(xpb, 0.0);

            //assign the output variable
            Runoff = Utility.Math.Divide(xpb * xpb, (Rain + Runon - TotalInterception + 0.8 * s), 0.0);

            //bound check the ouput variable
            bound_check_real_var(Runoff, 0.0, (Rain + Runon - TotalInterception), "runoff");
        }

        private double add_cover(double cover1, double cover2)
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



        private void soilwat2_cover_surface_runoff()
        {

            //This does NOT calculate runoff. It calculates an effective cover that is used for runoff.
            //In the process event this is called before the soilwat2_runoff.

            //*+  Purpose
            //*       calculate the effective runoff cover

            //*+  Assumptions
            //*       Assumes that if canopy height is negative it is missing.

            //*+  Mission Statement
            //*     Calculate the Effective Runoff surface Cover

            double canopyfact;             //! canopy factor (0-1)
            int crop;                   //! crop number
            double effective_crop_cover;   //! effective crop cover (0-1)
            double cover_surface_crop;     //! efective total cover (0-1)

            //! cover cn response from perfect   - ML  & dms 7-7-95
            //! nb. perfect assumed crop canopy was 1/2 effect of mulch
            //! This allows the taller canopies to have less effect on runoff
            //! and the cover close to ground to have full effect (jngh)

            //! weight effectiveness of crop canopies
            //!    0 (no effect) to 1 (full effect)

            cover_surface_crop = 0.0;
            for (crop = 0; crop < num_crops; crop++)
            {
                if (canopy_height[crop] >= 0.0)
                {
                    bool bDidInterpolate;
                    canopyfact = Utility.Math.LinearInterpReal(canopy_height[crop], canopy_fact_height, canopy_fact, out bDidInterpolate);
                }
                else
                {
                    canopyfact = canopy_fact_default;
                }

                effective_crop_cover = cover_tot[crop] * canopyfact;
                cover_surface_crop = add_cover(cover_surface_crop, effective_crop_cover);
            }

            //! add cover known to affect runoff
            //!    ie residue with canopy shading residue         
            cover_surface_runoff = add_cover(cover_surface_crop, surfaceom_cover);
        }



        private void soilwat2_runoff_depth_factor(ref double[] runoff_wf)
        {

            //runoff_wf -> ! (OUTPUT) weighting factor for runoff

            //*+  Purpose
            //*      Calculate the weighting factor hydraulic effectiveness used
            //*      to weight the effect of soil moisture on runoff.

            //*+  Mission Statement
            //*      Calculate soil moisture effect on runoff      

            double profile_depth;             //! current depth of soil profile - for when erosion turned on     
            double cum_depth;                 //! cumulative depth (mm)
            double hydrol_effective_depth_local;    //! hydrologically effective depth for runoff (mm)
            int hydrol_effective_layer;    //! layer number that the effective depth occurs in ()
            int layer;                     //! layer counter
            int num_layers;                //! number of layers
            double scale_fact;                //! scaling factor for wf function to sum to 1
            double wf_tot;                    //! total of wf ()
            double wx;                        //! depth weighting factor for current total depth. intermediate variable for deriving wf (total wfs to current layer)
            double xx;                        //! intermediate variable for deriving wf total wfs to previous layer

            xx = 0.0;
            cum_depth = 0.0;
            wf_tot = 0.0;
            num_layers = _dlayer.Length;
            runoff_wf = new double[num_layers];

            //! check if hydro_effective_depth applies for eroded profile.
            profile_depth = Utility.Math.Sum(_dlayer);
            hydrol_effective_depth_local = Math.Min(hydrol_effective_depth, profile_depth);

            scale_fact = 1.0 / (1.0 - Math.Exp(-4.16));
            hydrol_effective_layer = FindLayerNo(hydrol_effective_depth_local);

            for (layer = 0; layer <= hydrol_effective_layer; layer++)
            {
                cum_depth = cum_depth + _dlayer[layer];
                cum_depth = Math.Min(cum_depth, hydrol_effective_depth_local);

                //! assume water content to c%hydrol_effective_depth affects runoff
                //! sum of wf should = 1 - may need to be bounded? <dms 7-7-95>
                wx = scale_fact * (1.0 - Math.Exp(-4.16 * Utility.Math.Divide(cum_depth, hydrol_effective_depth_local, 0.0)));
                runoff_wf[layer] = wx - xx;
                xx = wx;

                wf_tot = wf_tot + runoff_wf[layer];
            }

            bound_check_real_var(wf_tot, 0.9999, 1.0001, "wf_tot");
        }

        #endregion


        #region Tillage

        private void soilwat2_tillage_addrain(double Rain, double Runon, double TotalInterception)
        {

            //The reduction in the runoff as a result of doing a tillage (tillage_cn_red) ceases after a set amount of rainfall (tillage_cn_rain).
            //This function works out the accumulated rainfall since last tillage event, and turns off the reduction if it is over the amount of rain specified.
            //This  soilwat2_tillage_addrain() is only called in soilwat2_runoff() 

            //sv- The Runoff is altered after a tillage event occurs.
            //sv- This code calculates how much it should be altered based on the accumulated rainfall since the last tillage event. 
            //sv- The zeroing of the tillage_rain_sum occurs in the tillage event.

            //*+  Mission Statement
            //*      Accumulate rainfall for tillage cn reduction 

            //rain         -> ! (INPUT) today's rainfall (mm)
            //runon        -> ! (INPUT) today's run on (mm)
            //interception -> ! (INPUT) todays interception loss (mm)

            string message;      //! message string

            tillage_rain_sum = tillage_rain_sum + Rain + Runon - TotalInterception;

            if ((tillage_cn_rain > 0.0) && (tillage_rain_sum > tillage_cn_rain))
            {
                //! This tillage has lost all effect on cn. CN reduction
                //!  due to tillage is off until the next tillage operation.
                tillage_cn_rain = 0.0;
                tillage_cn_red = 0.0;

                message = "Tillage CN reduction finished";
                Console.WriteLine(message);

            }

        }

        #endregion


        #region Infiltration

        private void soilwat2_infiltration()
        {

            //TODO: I think this should be a ref for infiltration parameter.

            //infiltration -> ! (OUTPUT) infiltration into top layer (mm)

            //*+  Purpose
            //*     infiltration into top layer after runoff.

            //*+  Mission Statement
            //*      Calculate infiltration into top layer

            double infiltration_1;      //! amount of infiltration from rain, irrigation - runoff
            double infiltration_2;      //! amount of infiltration from ponding

            //! DSG 041200
            //! with the addition of the ponding feature, infiltration is now
            //! considered as consisting of two components - that from the (rain +
            //! irrigation) and that from ponding.



            if (irrigation_layer <= 1)      //if this is surface irrigation
            {
                infiltration_1 = rain + irrigation + runon - runoff_pot - interception - residueinterception;
            }
            else
            {                           //if this is sub surface irrigation
                infiltration_1 = rain + runon - runoff_pot - interception - residueinterception;
            }



            infiltration_2 = pond;
            infiltration = infiltration_1 + infiltration_2;

            pond = 0.0;

        }


        #endregion


        #region Evaporation

        private void soilwat2_pot_evapotranspiration()
        {
            //*+  Purpose
            //*       calculate potential evapotranspiration (eo) or get from another module

            //*+  Notes
            //*       Eventually eo will be in a separate module entirely, and
            //*       will appear to soilwat when get_other_varaibles() runs.
            //*       But, for now we either retrieve it "manually", or use priestly-taylor.

            //eo_system = Double.NaN;
#if (APSIMX == false)
        if (_eo_source != "" && My.Get(_eo_source, out eo_system) && !Double.IsNaN(eo_system))
        {
            eo = eo_system;                     //! eo is provided by system
        }
        else
        {
            soilwat2_priestly_taylor();    //! eo from priestly taylor
        }
#else
            soilwat2_priestly_taylor();    //! eo from priestly taylor
#endif
        }

        private void soilwat2_pot_evapotranspiration_effective()
        {
            //*+  Notes
            //*       Eventually eo will be in a separate module entirely, and
            //*       will appear to soilwat when get_other_varaibles() runs.
            //*       But, for now we use either priestly-taylor, or whatever
            //*       the user specified.

            //! dsg 270502  check to see if there is any ponding.  If there is, evaporate any potential (g%eos) straight out of it and transfer
            //!             any remaining potential to the soil layer 1, as per usual.  Introduce new term g%pond_evap
            //!             which is the daily evaporation from the pond.

            if (pond > 0.0)
            {
                if (pond >= eos)
                {
                    pond = pond - eos;    //sv- the depth of water in the pond decreases by the amount of soil evaporation.
                    pond_evap = eos;
                    eos = 0.0;
                }
                else
                {
                    eos = eos - pond;
                    pond_evap = pond;
                    pond = 0.0;
                }
            }

        }

        private void soilwat2_priestly_taylor()
        {
            double albedo;           //! albedo taking into account plant material
            double cover_green_sum;  //! sum of crop green covers (0-1)
            double eeq;              //! equilibrium evaporation rate (mm)
            double wt_ave_temp;      //! weighted mean temperature for the day (oC)

            //*  ******* calculate potential evaporation from soil surface (eos) ******

            //                ! find equilibrium evap rate as a
            //                ! function of radiation, albedo, and temp.

            cover_green_sum = 0.0;
            for (int crop = 0; crop < num_crops; ++crop)
                cover_green_sum = 1.0 - (1.0 - cover_green_sum) * (1.0 - cover_green[crop]);

            albedo = max_albedo - (max_albedo - Salb) * (1.0 - cover_green_sum);

            // ! wt_ave_temp is mean temp, weighted towards max.
            wt_ave_temp = (0.60 * maxt) + (0.40 * mint);

            eeq = radn * 23.8846 * (0.000204 - 0.000183 * albedo) * (wt_ave_temp + 29.0);

            //! find potential evapotranspiration (eo) from equilibrium evap rate
            eo = eeq * soilwat2_eeq_fac();
        }



        private double soilwat2_eeq_fac()
        {
            //*+  Mission Statement
            //*     Calculate the Equilibrium Evaporation Rate

            if (maxt > max_crit_temp)
            {
                //! at very high max temps eo/eeq increases
                //! beyond its normal value of 1.1
                return ((maxt - max_crit_temp) * 0.05 + 1.1);
            }
            else
            {
                if (maxt < min_crit_temp)
                {
                    //! at very low max temperatures eo/eeq
                    //! decreases below its normal value of 1.1
                    //! note that there is a discontinuity at tmax = 5
                    //! it would be better at tmax = 6.1, or change the
                    //! .18 to .188 or change the 20 to 21.1
                    return (0.01 * Math.Exp(0.18 * (maxt + 20.0)));
                }
            }

            return 1.1;  //sv- normal value of eeq fac (eo/eeq)
        }


        private void soilwat2_evaporation()
        {
            //eos   -> ! (output) potential soil evap after modification for crop cover & residue_wt
            //esoil -> ! (output) actual soil evaporation (mm)

            double asw1;    //! available soil water in top layer for actual soil evaporation (mm)

            //1. get potential soil water evaporation
            soilwat2_pot_soil_evaporation();

            //2. get available soil water for evaporation
            //   ! NB. ritchie + b&s evaporate from layer 1, but rickert
            //   !     can evaporate from L1 + L2.
            asw1 = _sw_dep[0] - _air_dry_dep[0];
            asw1 = bound(asw1, 0.0, eo);

            //3. get actual soil water evaporation
            soilwat2_soil_evaporation(asw1);
        }


        private void soilwat2_pot_soil_evaporation()
        {
            //eos -> ! (output) potential soil evap after modification for crop cover & residue_w

            double cover_tot_sum;
            double eos_canopy_fract;      //! fraction of potential soil evaporation limited by crop canopy (mm)
            double eos_residue_fract;     //! fraction of potential soil evaporation limited by crop residue (mm)

            //! 1. get potential soil water evaporation

            //!---------------------------------------+
            //! reduce Eo to that under plant CANOPY                    <DMS June 95>
            //!---------------------------------------+

            //!  Based on Adams, Arkin & Ritchie (1976) Soil Sci. Soc. Am. J. 40:436-
            //!  Reduction in potential soil evaporation under a canopy is determined
            //!  the "% shade" (ie cover) of the crop canopy - this should include th
            //!  green & dead canopy ie. the total canopy cover (but NOT near/on-grou
            //!  residues).  From fig. 5 & eqn 2.                       <dms June 95>
            //!  Default value for c%canopy_eos_coef = 1.7
            //!              ...minimum reduction (at cover =0.0) is 1.0
            //!              ...maximum reduction (at cover =1.0) is 0.183.

            cover_tot_sum = 0.0;
            for (int i = 0; i < num_crops; i++)
                cover_tot_sum = 1.0 - (1.0 - cover_tot_sum) * (1.0 - cover_tot[i]);
            eos_canopy_fract = Math.Exp(-1 * canopy_eos_coef * cover_tot_sum);

            //   !-----------------------------------------------+
            //   ! reduce Eo under canopy to that under mulch            <DMS June 95>
            //   !-----------------------------------------------+

            //   !1a. adjust potential soil evaporation to account for
            //   !    the effects of surface residue (Adams et al, 1975)
            //   !    as used in Perfect
            //   ! BUT taking into account that residue can be a mix of
            //   ! residues from various crop types <dms june 95>

            if (surfaceom_cover >= 1.0)
            {
                //! We test for 100% to avoid log function failure.
                //! The algorithm applied here approaches 0 as cover approaches
                //! 100% and so we use zero in this case.
                eos_residue_fract = 0.0;
            }
            else
            {
                //! Calculate coefficient of residue_wt effect on reducing first
                //! stage soil evaporation rate

                //!  estimate 1st stage soil evap reduction power of
                //!    mixed residues from the area of mixed residues.
                //!    [DM. Silburn unpublished data, June 95 ]
                //!    <temporary value - will reproduce Adams et al 75 effect>
                //!     c%A_to_evap_fact = 0.00022 / 0.0005 = 0.44
                eos_residue_fract = Math.Pow((1.0 - surfaceom_cover), A_to_evap_fact);
            }

            //! Reduce potential soil evap under canopy to that under residue (mulch)
            eos = eo * eos_canopy_fract * eos_residue_fract;
        }




        private void soilwat2_soil_evaporation(double Eos_max)
        {
            //es        -> ! (input) upper limit of soil evaporation (mm/day)
            //eos       -> ! (input) potential rate of evaporation (mm/day)
            //eos_max   -> ! (input) upper limit of soil evaporation (mm/day)

            //*+  Purpose
            //*     Wrapper for various evaporation models. Returns actual
            //*     evaporation from soil surface (es).

            //*+  Mission Statement
            //*     Soil Evaporation from Soil Surface

            //sv- Es is an array because some Evap methods do evaporation from every layer in the soil. 
            //    Most only do the surface but I think one of them does every layer so you had to make es layered to cope with this one method.
            //    That is why they created Esoil array.
            //    You will note however with Ritchie evaporation we only pass the top layer to Es[0] to soilwat2_ritchie_evaporation()
            //    so the ritchie method only alters the evaporation in the surface layer of this array.

            ZeroArray(ref es_layers);

            if (evap_method == ritchie_method)
            {
                soilwat2_ritchie_evaporation(Eos_max);
            }
            else
            {
                throw new Exception("Undefined evaporation method");
            }

        }

        private void soilwat2_ritchie_evaporation(double Eos_max)
        {
            //es        -> ! (output) actual evaporation (mm)
            //eos       -> ! (input) potential rate of evaporation (mm/day)
            //eos_max   -> ! (input) upper limit of soil evaporation (mm/day)

            //*+  Purpose
            //*          ****** calculate actual evaporation from soil surface (es) ******
            //*          most es takes place in two stages: the constant rate stage
            //*          and the falling rate stage (philip, 1957).  in the constant
            //*          rate stage (stage 1), the soil is sufficiently wet for water
            //*          be transported to the surface at a rate at least equal to the
            //*          evaporation potential (eos).
            //*          in the falling rate stage (stage 2), the surface soil water
            //*          content has decreased below a threshold value, so that es
            //*          depends on the flux of water through the upper layer of soil
            //*          to the evaporating site near the surface.

            //*+  Notes
            //*       This changes globals - sumes1/2 and t.


            double esoil1;     //! actual soil evap in stage 1
            double esoil2;     //! actual soil evap in stage 2
            double sumes1_max; //! upper limit of sumes1
            double w_inf;      //! infiltration into top layer (mm)



            // Need to add 12 hours to move from "midnight" to "noon", or this won't work as expected
            if (Utility.Date.WithinDates(WinterDate, Clock.Today, SummerDate))
            {
                _cona = WinterCona;
                _u = WinterU;
            }
            else
            {
                _cona = SummerCona;
                _u = SummerU;
            }

            sumes1_max = _u;
            w_inf = infiltration;

            //! if infiltration, reset sumes1
            //! reset sumes2 if infil exceeds sumes1      
            if (w_inf > 0.0)
            {
                sumes2 = Math.Max(0.0, (sumes2 - Math.Max(0.0, w_inf - sumes1)));
                sumes1 = Math.Max(0.0, sumes1 - w_inf);

                //! update t (incase sumes2 changed)
                t = Utility.Math.Sqr(Utility.Math.Divide(sumes2, _cona, 0.0));
            }
            else
            {
                //! no infiltration, no re-set.
            }

            //! are we in stage1 ?
            if (sumes1 < sumes1_max)
            {
                //! we are in stage1
                //! set esoil1 = potential, or limited by u.
                esoil1 = Math.Min(eos, sumes1_max - sumes1);

                if ((eos > esoil1) && (esoil1 < Eos_max))
                {
                    //*           !  eos not satisfied by 1st stage drying,
                    //*           !  & there is evaporative sw excess to air_dry, allowing for esoil1.
                    //*           !  need to calc. some stage 2 drying(esoil2).

                    //*  if g%sumes2.gt.0.0 then esoil2 =f(sqrt(time),p%cona,g%sumes2,g%eos-esoil1).
                    //*  if g%sumes2 is zero, then use ritchie's empirical transition constant (0.6).            

                    if (sumes2 > 0.0)
                    {
                        t = t + 1.0;
                        esoil2 = Math.Min((eos - esoil1), (_cona * Math.Pow(t, 0.5) - sumes2));
                    }
                    else
                    {
                        esoil2 = 0.6 * (eos - esoil1);
                    }
                }
                else
                {
                    //! no deficit (or esoil1.eq.eos_max,) no esoil2 on this day            
                    esoil2 = 0.0;
                }

                //! check any esoil2 with lower limit of evaporative sw.
                esoil2 = Math.Min(esoil2, Eos_max - esoil1);


                //!  update 1st and 2nd stage soil evaporation.     
                sumes1 = sumes1 + esoil1;
                sumes2 = sumes2 + esoil2;
                t = Utility.Math.Sqr(Utility.Math.Divide(sumes2, _cona, 0.0));
            }
            else
            {
                //! no 1st stage drying. calc. 2nd stage         
                esoil1 = 0.0;

                t = t + 1.0;
                esoil2 = Math.Min(eos, (_cona * Math.Pow(t, 0.5) - sumes2));

                //! check with lower limit of evaporative sw.
                esoil2 = Math.Min(esoil2, Eos_max);

                //!   update 2nd stage soil evaporation.
                sumes2 = sumes2 + esoil2;
            }

            es_layers[0] = esoil1 + esoil2;

            //! make sure we are within bounds      
            es_layers[0] = bound(es_layers[0], 0.0, eos);
            es_layers[0] = bound(es_layers[0], 0.0, Eos_max);
        }

        #endregion

        #region Drainage (Saturated Flow)

        private void soilwat2_drainage(ref double ExtraRunoff)
        {

            //*     ===========================================================
            //subroutine soilwat2_drainage (flux,extra_runoff)
            //*     ===========================================================


            //*+  Function Arguments
            //flux              //! (output) water moving out of
            //extra_runoff      //! (output) water to add to runoff layer (mm)

            //*+  Purpose       
            //calculate flux - drainage from each layer. 
            //sv- it just calculates. It does not change anything.

            //*+  Constant Values
            //character  my_name*(*);           //! name of subroutine
            //parameter (my_name = 'soilwat2_drainage');

            //*+  Local Variables

            double add;                   //! water to add to layer
            double backup;                //! water to backup
            double excess;                //! amount above saturation(overflow)(mm)
            double excess_down;           //! amount above saturation(overflow) that moves on down (mm)
            double[] new_sw_dep;            //! record of results of sw calculations ensure mass balance. (mm)
            int i;                     //! counter  //sv- this was "l" (as in the leter "L") but it looks too much like the number 1, so I changed it to "i". 
            int layer;                 //! counter for layer no.
            int num_layers;            //! number of layers
            double w_drain;               //! water draining by gravity (mm)
            double w_in;                  //! water coming into layer (mm)
            double w_out;                 //! water going out of layer (mm)
            double w_tot;                 //! total water in layer at start (mm)

            //*- Implementation Section ----------------------------------

            //! flux into layer 1 = infiltration (mm).

            w_in = 0.0;
            ExtraRunoff = 0.0;

            //! calculate drainage and water
            //! redistribution.

            num_layers = _dlayer.Length;
            flux = new double[num_layers];
            new_sw_dep = new double[num_layers];

            for (layer = 0; layer < num_layers; layer++)
            {
                //! get total water concentration in layer

                w_tot = _sw_dep[layer] + w_in;

                //! get excess water above saturation & then water left
                //! to drain between sat and dul.  Only this water is
                //! subject to swcon. The excess is not - treated as a
                //! bucket model. (mm)

                if (w_tot > _sat_dep[layer])
                {
                    excess = w_tot - _sat_dep[layer];
                    w_tot = _sat_dep[layer];
                }
                else
                {
                    excess = 0.0;
                }


                if (w_tot > _dul_dep[layer])
                {
                    w_drain = (w_tot - _dul_dep[layer]) * Soil.SWCON[layer];
                    //!w_drain = min(w_drain,p%Ks(layer))
                }
                else
                {
                    w_drain = 0.0;
                }

                //! get water draining out of layer (mm)

                if (excess > 0.0)
                {

                    //! Calculate amount of water to backup and push down
                    //! Firstly top up this layer (to saturation)
                    add = Math.Min(excess, w_drain);
                    excess = excess - add;
                    new_sw_dep[layer] = _sat_dep[layer] - w_drain + add;

                    //! partition between flow back up and flow down
                    excess_down = Math.Min(ks[layer] - w_drain, excess);
                    backup = excess - excess_down;

                    w_out = excess_down + w_drain;
                    flux[layer] = w_out;

                    //! now back up to saturation for this layer up out of the
                    //! backup water keeping account for reduction of actual
                    //! flow rates (flux) for N movement.

                    for (i = layer - 1; i >= 0; i--)
                    {
                        flux[i] = flux[i] - backup;
                        add = Math.Min(_sat_dep[i] - new_sw_dep[i], backup);
                        new_sw_dep[i] = new_sw_dep[i] + add;
                        backup = backup - add;
                    }
                    ExtraRunoff = ExtraRunoff + backup;
                }
                else
                {
                    //! there is no excess so do nothing
                    w_out = w_drain;
                    flux[layer] = w_out;
                    new_sw_dep[layer] = _sw_dep[layer] + w_in - w_out;

                }

                //! drainage out of this layer goes into next layer down
                w_in = w_out;
            }

            //call pop_routine (my_name);

        }


        private void soilwat2_drainage_old(ref double ExtraRunoff)
        {
            //flux         -> (output) water moving out of
            //extra_runoff -> (output) water to add to runoff layer (mm)

            //*+  Purpose
            //*       calculate flux - drainage from each layer

            //*+  Mission Statement
            //*     Calculate Drainage from each layer      

            double add;           //! water to add to layer
            double backup;        //! water to backup
            double excess;        //! amount above saturation(overflow)(mm)
            double[] new_sw_dep;    //! record of results of sw calculations ensure mass balance. (mm)
            int i;             //! counter //sv- this was "l" (as in the leter "L") but it looks too much like the number 1, so I changed it to "i". 
            int layer;         //! counter for layer no.
            int num_layers;    //! number of layers
            double w_drain;       //! water draining by gravity (mm)
            double w_in;          //! water coming into layer (mm)
            double w_out;         //! water going out of layer (mm)
            double w_tot;         //! total water in layer at start (mm)

            //! flux into layer 1 = infiltration (mm).
            w_in = 0.0;
            ExtraRunoff = 0.0;


            //! calculate drainage and water
            //! redistribution.
            num_layers = _dlayer.Length;
            flux = new double[num_layers];
            new_sw_dep = new double[num_layers];

            for (layer = 0; layer < num_layers; layer++)
            {
                //! get total water concentration in layer
                w_tot = _sw_dep[layer] + w_in;

                //! get excess water above saturation & then water left
                //! to drain between sat and dul.  Only this water is
                //! subject to swcon. The excess is not - treated as a
                //! bucket model. (mm)

                if (w_tot > _sat_dep[layer])
                {
                    excess = w_tot - _sat_dep[layer];
                    w_tot = _sat_dep[layer];
                }
                else
                {
                    excess = 0.0;
                }

                if (w_tot > _dul_dep[layer])
                {
                    w_drain = (w_tot - _dul_dep[layer]) * Soil.SWCON[layer];
                }
                else
                {
                    w_drain = 0.0;
                }

                //! get water draining out of layer (mm)
                if (excess > 0.0)
                {
                    if (Soil.MWCON == null || Soil.MWCON[layer] >= 1.0)
                    {
                        //! all this excess goes on down so do nothing
                        w_out = excess + w_drain;
                        new_sw_dep[layer] = _sw_dep[layer] + w_in - w_out;
                        flux[layer] = w_out;
                    }
                    else
                    {
                        //! Calculate amount of water to backup and push down
                        //! Firstly top up this layer (to saturation)
                        add = Math.Min(excess, w_drain);
                        excess = excess - add;
                        new_sw_dep[layer] = _sat_dep[layer] - w_drain + add;

                        //! partition between flow back up and flow down
                        backup = (1.0 - Soil.MWCON[layer]) * excess;
                        excess = Soil.MWCON[layer] * excess;

                        w_out = excess + w_drain;
                        flux[layer] = w_out;

                        //! now back up to saturation for this layer up out of the
                        //! backup water keeping account for reduction of actual
                        //! flow rates (flux) for N movement.         
                        for (i = layer - 1; i >= 0; i--)
                        {
                            flux[i] = flux[i] - backup;
                            add = Math.Min((_sat_dep[i] - new_sw_dep[i]), backup);
                            new_sw_dep[i] = new_sw_dep[i] + add;
                            backup = backup - add;
                        }

                        ExtraRunoff = ExtraRunoff + backup;
                    }
                }
                else
                {
                    //! there is no excess so do nothing
                    w_out = w_drain;
                    flux[layer] = w_out;
                    new_sw_dep[layer] = _sw_dep[layer] + w_in - w_out;
                }

                //! drainage out of this layer goes into next layer down
                w_in = w_out;

            }

        }

        #endregion


        #region Unsaturated Flow

        private void soilwat2_unsat_flow()
        {

            //*+  Purpose
            //*       calculate unsaturated flow below drained upper limit

            //*+  Mission Statement
            //*     Calculate Unsaturated Solute and Water Flow


            double esw_dep1;            //! extractable soil water in current layer (mm)
            double esw_dep2;            //! extractable soil water in next layer below (mm)
            double dbar;                //! average diffusivity used to calc unsaturated flow between layers
            int layer;               //! layer counter for current layer
            int second_last_layer;   //! last layer for flow
            int num_layers;          //! number of layers
            int next_layer;          //! layer counter for next lower layer
            double flow_max;            //! maximum flow to make gradient between layers equal zero
            double theta1;              //! sw content above ll15 for current layer (cm/cm)
            double theta2;              //! sw content above ll15 for next lower layer (cm/cm)
            double w_out;               //! water moving up out of this layer (mm)
            //! +ve = up to next layer
            //! -ve = down into this layer
            double this_layer_cap;      //! capacity of this layer to accept water from layer below (mm)
            double next_layer_cap;      //! capacity of nxt layer to accept water from layer above (mm)
            double sw1;                 //! sw for current layer (mm/mm)
            double sw2;                 //! sw for next lower layer (mm/mm)
            double gradient;            //! driving force for flow
            double sum_inverse_dlayer;
            double dlayer1;             //! depth of current layer (mm)
            double dlayer2;             //! depth of next lower layer (mm)
            double ave_dlayer;          //! average depth of current and next layers (mm)
            double sw_dep1;             //! soil water depth in current layer (mm)
            double sw_dep2;             //! soil water depth in next layer (mm)
            double ll15_dep1;           //! 15 bar lower limit sw depth in current layer (mm)
            double ll15_dep2;           //! 15 bar lower limit sw depth in next layer (mm)
            double sat_dep1;            //! saturated sw depth in current layer (mm)
            double sat_dep2;            //! saturated sw depth in next layer (mm)
            double dul_dep1;            //! drained upper limit in current layer (mm)
            double dul_dep2;            //! drained upper limit in next layer (mm)
            double swg;                 //! sw differential due to gravitational pressure head (mm)

            num_layers = _dlayer.Length;

            //! *** calculate unsaturated flow below drained upper limit (flow)***   
            flow = new double[num_layers];

            //! second_last_layer is bottom layer but 1.
            second_last_layer = num_layers - 1;


            w_out = 0.0;
            for (layer = 0; layer < second_last_layer; layer++)
            {
                next_layer = layer + 1;

                dlayer1 = _dlayer[layer];
                dlayer2 = _dlayer[next_layer];
                ave_dlayer = (dlayer1 + dlayer2) * 0.5;

                sw_dep1 = _sw_dep[layer];
                sw_dep2 = _sw_dep[next_layer];

                ll15_dep1 = _ll15_dep[layer];
                ll15_dep2 = _ll15_dep[next_layer];

                sat_dep1 = _sat_dep[layer];
                sat_dep2 = _sat_dep[next_layer];

                dul_dep1 = _dul_dep[layer];
                dul_dep2 = _dul_dep[next_layer];

                esw_dep1 = Math.Max((sw_dep1 - w_out) - ll15_dep1, 0.0);
                esw_dep2 = Math.Max(sw_dep2 - ll15_dep2, 0.0);

                //! theta1 is excess of water content above lower limit,
                //! theta2 is the same but for next layer down.
                theta1 = Utility.Math.Divide(esw_dep1, dlayer1, 0.0);
                theta2 = Utility.Math.Divide(esw_dep2, dlayer2, 0.0);

                //! find diffusivity, a function of mean thet.
                dbar = DiffusConst * Math.Exp(DiffusSlope * (theta1 + theta2) * 0.5);

                //! testing found that a limit of 10000 (as used in ceres-maize)
                //! for dbar limits instability for flow direction for consecutive
                //! days in some situations.

                dbar = bound(dbar, 0.0, 10000.0);

                sw1 = Utility.Math.Divide((sw_dep1 - w_out), dlayer1, 0.0);
                sw1 = Math.Max(sw1, 0.0);

                sw2 = Utility.Math.Divide(sw_dep2, dlayer2, 0.0);
                sw2 = Math.Max(sw2, 0.0);

                //    ! gradient is defined in terms of absolute sw content
                //cjh          subtract gravity gradient to prevent gradient being +ve when flow_max is -ve, resulting in sw > sat.
                gradient = Utility.Math.Divide((sw2 - sw1), ave_dlayer, 0.0) - gravity_gradient;


                //!  flow (positive up) = diffusivity * gradient in water content
                flow[layer] = dbar * gradient;

                //! flow will cease when the gradient, adjusted for gravitational
                //! effect, becomes zero.
                swg = gravity_gradient * ave_dlayer;

                //! calculate maximum flow
                sum_inverse_dlayer = Utility.Math.Divide(1.0, dlayer1, 0.0) + Utility.Math.Divide(1.0, dlayer2, 0.0);
                flow_max = Utility.Math.Divide((sw2 - sw1 - swg), sum_inverse_dlayer, 0.0);


                //c dsg 260202
                //c dsg    this code will stop a saturated layer difusing water into a partially saturated
                //c        layer above for Water_table height calculations
                if ((_sw_dep[layer] >= _dul_dep[layer]) && (_sw_dep[next_layer] >= _dul_dep[next_layer]))
                {
                    flow[layer] = 0.0;
                }

                //c dsg 260202
                //c dsg    this code will stop unsaturated flow downwards through an impermeable layer, but will allow flow up
                if ((Soil.MWCON != null) && (Soil.MWCON[layer] == 0) && (flow[layer] < 0.0))
                {
                    flow[layer] = 0.0;
                }


                if (flow[layer] < 0.0)
                {
                    //! flow is down to layer below
                    //! check capacity of layer below for holding water from this layer
                    //! and the ability of this layer to supply the water

                    //!    next_layer_cap = l_bound (sat_dep2 - sw_dep2, 0.0)
                    //!    dsg 150302   limit unsaturated downflow to a max of dul in next layer

                    next_layer_cap = Math.Max(dul_dep2 - sw_dep2, 0.0);
                    flow_max = Math.Max(flow_max, -1 * next_layer_cap);
                    flow_max = Math.Max(flow_max, -1 * esw_dep1);
                    flow[layer] = Math.Max(flow[layer], flow_max);
                }
                else
                {
                    if (flow[layer] > 0.0)
                    {
                        //! flow is up from layer below
                        //! check capacity of this layer for holding water from layer below
                        //! and the ability of the layer below to supply the water

                        //!            this_layer_cap = l_bound (sat_dep1 - (sw_dep1 - w_out), 0.0)
                        //!    dsg 150302   limit unsaturated upflow to a max of dul in this layer
                        this_layer_cap = Math.Max(dul_dep1 - (sw_dep1 - w_out), 0.0);
                        flow_max = Math.Min(flow_max, this_layer_cap);
                        flow_max = Math.Min(flow_max, esw_dep2);
                        flow[layer] = Math.Min(flow[layer], flow_max);
                    }
                    else
                    {
                        // no flow
                    }
                }


                //! For conservation of water, store amount of water moving
                //! between adjacent layers to use for next pair of layers in profile
                //! when calculating theta1 and sw1.
                w_out = flow[layer];
            }

        }

        #endregion


        #region Solute

        //sv- solute movement during Drainage (Saturated Flow)

        private void soilwat2_solute_flux(ref double[] solute_out, double[] solute_kg)
        {

            //solute_out   ->   ! (output) solute leaching out of each layer (kg/ha) 
            //solute_kg    ->   ! (input) solute in each layer (kg/ha)

            //*+  Purpose
            //*         calculate the downward movement of solute with percolating water

            //*+  Mission Statement
            //*     Calculate the Solute Movement with Saturated Water Flux

            double in_solute;        //! solute leaching into layer from above (kg/ha)
            int layer;            //! layer counter
            int num_layers;       //! number of layers in profile
            double out_max;          //! max. solute allowed to leach out of layer (kg/ha)
            double out_solute;       //! solute leaching out of layer (kg/ha)
            double out_w;            //! water draining out of layer (mm)
            double solute_kg_layer;  //! quantity of solute in layer (kg/ha)
            double water;            //! quantity of water in layer (mm)
            double solute_flux_eff_local;

            num_layers = _dlayer.Length;
            solute_out = new double[num_layers];
            in_solute = 0.0;

            for (layer = 0; layer < num_layers; layer++)
            {
                //! get water draining out of layer and n content of layer includes that leaching down         
                out_w = flux[layer];
                solute_kg_layer = solute_kg[layer] + in_solute;

                //! n leaching out of layer is proportional to the water draining out.
                if (num_solute_flux == 1)
                {
                    //single value was specified in ini file (still gets put in an array with just one element)
                    solute_flux_eff_local = solute_flux_eff[0];
                }
                else
                {
                    //array was specified in ini file
                    solute_flux_eff_local = solute_flux_eff[layer];
                }
                water = _sw_dep[layer] + out_w;
                out_solute = solute_kg_layer * Utility.Math.Divide(out_w, water, 0.0) * solute_flux_eff_local;

                //! don't allow the n to be reduced below a minimum level
                out_max = Math.Max(solute_kg_layer, 0.0);
                out_solute = bound(out_solute, 0.0, out_max);

                //! keep the leaching and set the input for the next layer
                solute_out[layer] = out_solute;
                in_solute = out_solute;
            }


        }

        //sv- solute movement during Unsaturated Flow

        private void soilwat2_solute_flow(ref double[] solute_up, double[] solute_kg)
        {

            //solute_up -> ! (output) solute moving upwards into each layer (kg/ha)
            //solute_kg -> ! (input/output) solute in each layer (kg/ha)

            //*+  Purpose
            //*       movement of solute in response to differences in
            //*       water content of adjacent soil layers when the soil water
            //*       content is < the drained upper limit (unsaturated flow)

            //*+  Notes
            //*       170895 nih The variable names and comments need to be cleaned
            //*                  up.  When this is done some references to no3 or
            //*                  nitrogen need to be changed to 'solute'

            //*+  Mission Statement
            //*     Calculate the Solute Movement with Unsaturated Water Flow

            double bottomw;             //! water movement to/from next layer (kg/ha)
            double in_solute;           //! solute moving into layer from above (kg/ha)
            int layer;               //! layer counter
            double[] solute_down;         //! solute moving downwards out of each layer (kg/ha)
            int num_layers;          //! number of layers
            double out_solute;          //! solute moving out of layer (kg/ha)
            double out_w;               //! water draining out of layer (mm)
            double[] remain;              //! n remaining in each layer between movement up (kg/ha)
            double solute_kg_layer;     //! quantity of solute in layer (kg/ha)
            double top_w;               //! water movement to/from above layer (kg/ha)
            double water;               //! quantity of water in layer (mm)
            double solute_flow_eff_local;

            //sv- initialise the local arrays declared above.

            //! flow  up from lower layer:  + up, - down
            //******************************************
            //******************************************


            //! + ve flow : upward movement. go from bottom to top layer   
            //**********************************************************

            num_layers = _dlayer.Length;
            solute_up = new double[num_layers];
            solute_down = new double[num_layers];
            remain = new double[num_layers];

            in_solute = 0.0;
            for (layer = num_layers - 1; layer > 0; layer--)
            {
                //! keep the nflow upwards
                solute_up[layer] = in_solute;

                //! get water moving up and out of layer to the one above
                out_w = flow[layer - 1];
                if (out_w <= 0.0)
                {
                    out_solute = 0.0;
                }
                else
                {
                    //! get water movement between this and next layer
                    bottomw = flow[layer];

                    //! get n content of layer includes that moving from other layer
                    solute_kg_layer = solute_kg[layer] + in_solute;
                    water = _sw_dep[layer] + out_w - bottomw;

                    //! n moving out of layer is proportional to the water moving out.
                    if (num_solute_flow == 1)
                    {
                        solute_flow_eff_local = solute_flow_eff[0];
                    }
                    else
                    {
                        solute_flow_eff_local = solute_flow_eff[layer];
                    }
                    out_solute = solute_kg_layer * Utility.Math.Divide(out_w, water, 0.0) * solute_flow_eff_local;

                    //! don't allow the n to be reduced below a minimum level
                    out_solute = bound(out_solute, 0.0, solute_kg_layer);
                }
                //! set the input for the next layer
                in_solute = out_solute;
            }

            solute_up[0] = in_solute;
            //! now get n remaining in each layer between movements
            //! this is needed to adjust the n in each layer before calculating
            //! downwards movement.  i think we shouldn't do this within a time
            //! step. i.e. there should be no movement within a time step. jngh
            remain[0] = solute_up[0];
            for (layer = 1; layer < num_layers; layer++)
            {
                remain[layer] = solute_up[layer] - solute_up[layer - 1];
            }




            //! -ve flow - downward movement
            //******************************

            in_solute = 0.0;
            top_w = 0.0;

            for (layer = 0; layer < num_layers; layer++)
            {
                //! get water moving out of layer
                out_w = -1 * flow[layer];
                if (out_w <= 0.0)
                {
                    out_solute = 0.0;
                }
                else
                {
                    //! get n content of layer includes that moving from other layer
                    solute_kg_layer = solute_kg[layer] + in_solute + remain[layer];
                    water = _sw_dep[layer] + out_w - top_w;

                    //! n moving out of layer is proportional to the water moving out.
                    if (num_solute_flow == 1)
                    {
                        solute_flow_eff_local = solute_flow_eff[0];
                    }
                    else
                    {
                        solute_flow_eff_local = solute_flow_eff[layer];
                    }

                    out_solute = solute_kg_layer * Utility.Math.Divide(out_w, water, 0.0) * solute_flow_eff_local;

                    //! don't allow the n to be reduced below a minimum level
                    out_solute = Utility.Math.RoundToZero(out_solute);
                    out_solute = bound(out_solute, 0.0, solute_kg_layer);
                }
                solute_down[layer] = out_solute;
                in_solute = out_solute;
                top_w = out_w;
            }

            for (layer = 0; layer < num_layers; layer++)
            {
                solute_up[layer] = solute_up[layer] - solute_down[layer];
            }

        }



        private void soilwat2_irrig_solute()
        {
            //*+  Mission Statement
            //*      Add solutes with irrigation

            int solnum;     //! solute number counter variable     
            int layer;      //! soil layer

            //sv- 11 Dec 2012. 
            //Since I have allowed irrigations to runoff just like rain (using argument "will_runoff = 1" in apply command)
            //I should really remove a proportion of the solutes that are lost due to some of the irrigation running off.
            //Perhaps something like (irrigation / (rain + irrigation)) * runoff 
            //to work out how much of the runoff is caused by irrigation and remove this proportion of solutes from the surface layer.
            //HOWEVER, when rain causes runoff we don't remove solutes from the surface layer of the soil. 
            //So why when irrigation causes runoff should we remove solutes.  

            if (irrigation_layer == 0)   //sv- if user did not enter an irrigation_layer
            {
                //!addition at surface
                layer = 0;
            }
            else
            {
                layer = irrigation_layer - 1;
            }

            for (solnum = 0; solnum < num_solutes; solnum++)
            {
                solutes[solnum].amount[layer] += solutes[solnum].irrigation;
                solutes[solnum].delta[layer] += solutes[solnum].irrigation;
            }

        }

        /*

           private void soilwat2_rainfall_solute()
              {
              //*+  Mission Statement
              //*      Add solutes from rainfall

              int      solnum;        //! solute number counter variable
              double   mass_rain;     //! mass of rainfall on this day (kg/ha)
              double   mass_solute;   //! mass of solute in this rainfall (kg/ha)

              //! 1mm of rain = 10000 kg/ha, therefore total mass of rainfall = g%rain * 10000 kg/ha
              mass_rain = rain * 10000.0;

              for(solnum=0; solnum<num_solutes; solnum++)
                 {
                 //!assume all rainfall goes into layer 1
                 //! therefore mass_solute = mass_rain * g%solute_conc_rain (in ppm) / 10^6
                 mass_solute = Utility.Math.Divide(mass_rain * solute_conc_rain[solnum], 1000000.0, 0.0);
                 solute[solnum,0]   = solute[solnum,0] + mass_solute;
                 dlt_solute[solnum,0] = dlt_solute[solnum,0] + mass_solute;
                 }

              }

        */

        private void MoveDownReal(double[] DownAmount, ref double[] A)
        {

            //!+ Sub-Program Arguments
            //   real       array (*)             ! (INPUT/OUTPUT) amounts currently in
            //                                    !   each layer
            //   real       down (*)              ! (INPUT) amounts to move into each
            //                                    !   layer from the one above

            //!+ Purpose
            //!     move amounts specified, downwards from one element to the next

            //!+  Definition
            //!     Each of the "nlayr" elements of "array" holds quantities
            //!     for a given soil layer.  "array"(1) corresponds to the
            //!     uppermost layer.   "array"(n) corresponds to the layer
            //!     (n-1) layers down from the uppermost layer.  "down"(n)
            //!     indicates a quantity to be moved from the layer
            //!     corresponding to "array"(n) down into the layer
            //!     corresponding to "array"(n+1).  This subroutine subtracts
            //!     "down"(n) from "array"(n) and adds it to "array"(n+1) for
            //!     n=1 .. ("nlayr"-1).  "down"("nlayr") is subtracted from
            //!     "array"("nlayr").

            //!+  Mission Statement
            //!      Move amounts of %1 down array %2

            //!+ Changes
            //!       031091  jngh changed variable movedn to down - cr157

            //!+ Local Variables
            int layer;  //! layer number
            double win;    //! amount moving from layer above to current layer
            double wout;   //! amount moving from current layer to the one below

            //!- Implementation Section ----------------------------------

            win = 0.0;
            for (layer = 0; layer < Math.Min(A.Length, DownAmount.Length); layer++)
            {
                wout = DownAmount[layer];
                A[layer] = A[layer] + win - wout;
                win = wout;
            }
        }

        private void soilwat2_move_solute_down()
        {

            //*+  Mission Statement
            //*      Calculate downward movement of solutes

            int num_layers;
            int solnum;              //! solute number counter variable

            num_layers = _dlayer.Length;

            for (solnum = 0; solnum < num_solutes; solnum++)
            {
                if (solutes[solnum].mobility)     //this boolean array is created in new solute event handler.
                {
                    ZeroArray(ref solutes[solnum].leach);
                    soilwat2_solute_flux(ref solutes[solnum].leach, solutes[solnum].amount);               //calc leaching
                    MoveDownReal(solutes[solnum].leach, ref solutes[solnum].amount);      //use leaching to set new solute values
                    MoveDownReal(solutes[solnum].leach, ref solutes[solnum].delta);       //use leaching to set new delta (change in) solute values
                }
            }
        }


        private void MoveUpReal(double[] UpAmount, ref double[] A)
        {
            //move_up_real(leach, temp_solute, num_layers);


            //!     ===========================================================
            //   subroutine Move_up_real (up, array, nlayr)
            //!     ===========================================================


            //!+ Sub-Program Arguments
            //eal        array (*)             //! (INPUT/OUTPUT) amounts currently in each layer
            //int         nlayr                 //! (INPUT) number of layers
            //real        up (*)                //! (INPUT) amounts to move into each layer from the one below

            //!+ Purpose
            //!       move amounts specified, upwards from one element to the next

            //!+  Definition
            //!     Each of the "nlayr" elements of "array" holds quantities
            //!     for a given soil layer.  "array"(1) corresponds to the
            //!     uppermost layer.   "array"(n) corresponds to the layer
            //!     (n-1) layers down from the uppermost layer.  "up"(n)
            //!     indicates a quantity to be moved from the layer
            //!     corresponding to "array"(n+1) up into the layer
            //!     corresponding to "array"(n).  This subroutine subtracts
            //!     "up"(n) from "array"(n+1) and adds it to "array"(n) for
            //!     n=1..("nlayr"-1).  "up"("nlayr") is added to "array"("nlayr").

            //!+  Mission Statement
            //!      Move amounts %1 up array %2

            //!+ Changes
            //!       031091  jngh renamed moveup to up - cr158
            //!                    included description of variables in parameter list
            //!                      - cr159
            //!                    corrected description - cr160

            //!+ Calls

            //!+ Local Variables
            int layer;                 //! layer number
            double win;                   //! amount moving from layer below to current layer
            double wout;                  //! amount moving from current layer to the one above

            //!- Implementation Section ----------------------------------

            wout = 0.0;
            for (layer = 0; layer < Math.Min(A.Length, UpAmount.Length); layer++)
            {
                win = UpAmount[layer];
                A[layer] = A[layer] + win - wout;
                wout = win;
            }
        }

        private void soilwat2_move_solute_up()
        {

            //*+  Mission Statement
            //*      Calculate upward movement of solutes

            int num_layers;          //! number of layers
            int solnum;              //! solute number counter variable

            num_layers = _dlayer.Length;

            for (solnum = 0; solnum < num_solutes; solnum++)
            {
                if (solutes[solnum].mobility)
                {
                    ZeroArray(ref solutes[solnum].up);
                    soilwat2_solute_flow(ref solutes[solnum].up, solutes[solnum].amount);
                    MoveUpReal(solutes[solnum].up, ref solutes[solnum].amount);
                    MoveUpReal(solutes[solnum].up, ref solutes[solnum].delta);
                }
            }
        }


        #endregion



        #region Water Table

        private double soilwat_water_table()
        {
            //*+  Purpose
            //*     Calculate the water table
            // water table is just the depth (in mm) below the ground surface of the first layer which is above saturation.

            int layer;
            int num_layers;
            int sat_layer;
            double margin;      //! dsg 110302  allowable looseness in definition of sat
            double saturated_fraction;
            double saturated_fraction_above;
            double drainable;
            double drainable_capacity;
            double bottom_depth;
            double saturated;
            bool layer_is_fully_saturated;
            bool layer_is_saturated;
            bool layer_above_is_saturated;


            //sv- C# has a problem with these values being initialised inside of the final else clause of an if statement. You have to give them a default value.
            sat_layer = -1;
            saturated_fraction_above = 0.0;
            layer_is_saturated = false;


            num_layers = _dlayer.Length;


            for (layer = 0; layer < num_layers; layer++)
            {
                margin = error_margin;

                //Find the first layer that is above saturation or really close to it. 
                //nb. sat_layer is a layer number not an index. Therefore it starts at 1 and not zero. So we need to convert it to a layer number from an index. "layer" variable is really an index not a layer number.
                if ((_sat_dep[layer] - _sw_dep[layer]) <= margin)
                {
                    sat_layer = layer + 1;
                    break;
                }
                //Or if mwcon is set to be impermeable for this layer and above sw is above dul then consider this layer as saturated.
                else if ((Soil.MWCON != null) && (Soil.MWCON[layer] < 1.0) && (_sw_dep[layer] > _dul_dep[layer]))
                {
                    //!  dsg 150302     also check whether impermeable layer is above dul. If so then consider it to be saturated
                    sat_layer = layer;
                    break;
                }
                else
                {
                    sat_layer = 0;   //if there is no saturated layer set it to 0
                }
            }

            //If you found a saturated layer in the profile, 
            if (sat_layer > 0)
            {
                //! saturated fraction of saturated layer
                //calculate the saturation_fraction of current layer incase,
                //there is no layer above
                //or incase mwcon was set to impermeable and sw was above dul (so there are layers above but no saturated layers, the impermeable layer is just above dul which is the watertable) 
                drainable = _sw_dep[sat_layer - 1] - _dul_dep[sat_layer - 1];
                drainable_capacity = _sat_dep[sat_layer - 1] - _dul_dep[sat_layer - 1];
                saturated_fraction = Utility.Math.Divide(drainable, drainable_capacity, 0.0);
                //if it is not the top layer that is saturated (ie. there is a layer above the saturated layer)
                //Then see if the layer above it is above dul and if so calculate the fraction so we can add this as extra millimeters to the water_table.
                if (sat_layer > 1)
                {
                    //! saturated fraction of layer above saturated layer
                    drainable = _sw_dep[sat_layer - 2] - _dul_dep[sat_layer - 2];
                    drainable_capacity = _sat_dep[sat_layer - 2] - _dul_dep[sat_layer - 2];
                    saturated_fraction_above = Utility.Math.Divide(drainable, drainable_capacity, 0.0);
                }
                else
                {
                    //! top layer fully saturated - no layer above it
                    saturated_fraction_above = 0.0;
                }
            }
            else
            {
                //! profile not saturated
                saturated_fraction = 0.0;
            }

            //set some boolean flags based on the saturated fraction calculated above.
            if (saturated_fraction >= 0.999999)
            {
                layer_is_fully_saturated = true;
                layer_above_is_saturated = true;
            }
            else if (saturated_fraction > 0.0)
            {
                layer_is_fully_saturated = false;
                layer_is_saturated = true;
            }
            else
            {
                layer_is_fully_saturated = false;
                layer_is_saturated = false;
            }


            if (saturated_fraction_above > 0.0)
            {
                layer_above_is_saturated = true;
            }
            else
            {
                layer_above_is_saturated = false;
            }


            //Do the calculation of the water_table      
            if (layer_is_fully_saturated && layer_above_is_saturated)
            {
                //! dsg 150302  saturated layer = layer, layer above is over dul
                bottom_depth = Utility.Math.Sum(_dlayer, 0, sat_layer - 1, 0.0);
                saturated = saturated_fraction_above * _dlayer[sat_layer - 2];
                return (bottom_depth - saturated);
            }
            else if (layer_is_saturated)
            {
                //! dsg 150302  saturated layer = layer, layer above not over dul
                bottom_depth = Utility.Math.Sum(_dlayer, 0, sat_layer, 0.0);
                saturated = saturated_fraction * _dlayer[sat_layer - 1];
                return (bottom_depth - saturated);
            }
            else
            {
                //! profile is not saturated
                bottom_depth = Utility.Math.Sum(_dlayer);
                return bottom_depth;
            }

        }


        private void SetWaterTable(double WaterTable)
        {
            if (!double.IsNaN(WaterTable))
            {
                int layer;
                int num_layers;
                double top;
                double bottom;
                double fraction;
                double drainable_porosity;

                num_layers = _dlayer.Length;
                top = 0.0;
                bottom = 0.0;

                for (layer = 0; layer < num_layers; layer++)
                {
                    top = bottom;
                    bottom = bottom + _dlayer[layer];
                    if (WaterTable >= bottom)
                    {
                        //do nothing;
                    }
                    else if (WaterTable > top)
                    {
                        //! top of water table is in this layer
                        fraction = (bottom - WaterTable) / (bottom - top);
                        drainable_porosity = _sat_dep[layer] - _dul_dep[layer];
                        _sw_dep[layer] = _dul_dep[layer] + fraction * drainable_porosity;
                    }
                    else
                    {
                        _sw_dep[layer] = _sat_dep[layer];
                    }
                }

                _water_table = WaterTable;
            }
        }



        #endregion






        #region Lateral Flow


        private void Lateral_process()
        {

            int layer;
            double d;  //depth of water table in a layer (mm)
            double max_flow;


            int num_layers = _dlayer.Length;


            //TODO: This initialisation section should be in soilwat2_set_my_variable() not really here. But this is how SoilWat does it, so leave it here for now.
            //inflow_lat is optional daily input so if it does not exist just create it and zero it.
            if (inflow_lat == null)
            {
                inflow_lat = new double[_dlayer.Length];
            }

            //The user does not have have specify a value for ALL the layers in the soil. Just can specify the layers from the top down to whatever layer they like.
            //Therefore we need to resize the array if they did not specify a value for every layer and then put in zero values for the layers they did not specify.
            if (inflow_lat.Length < _dlayer.Length)
            {
                int startZeroingFromHere = inflow_lat.Length;  //seems stupid but do this incase one day change back to 1 based array again.
                Array.Resize(ref inflow_lat, _dlayer.Length);
                //This following is probably not necessary as the resize probably zeros it, but do it just incase.
                for (int i = startZeroingFromHere; i < _dlayer.Length; i++)
                {
                    inflow_lat[i] = 0.0;
                }
            }



            for (layer = 0; layer < num_layers; layer++)
            {
                //! dsg 150302   add the inflowing lateral water
                _sw_dep[layer] = _sw_dep[layer] + inflow_lat[layer];
                d = _dlayer[layer] * Utility.Math.Divide((_sw_dep[layer] - _dul_dep[layer]), (_sat_dep[layer] - _dul_dep[layer]), 0.0);
                d = Math.Max(0.0, d);  //! water table depth in layer must be +ve

                double i, j;
                i = KLAT[layer] * d * (discharge_width / mm2m) * slope;
                j = (catchment_area * sm2smm) * (Math.Pow((1.0 + Math.Pow(slope, 2)), 0.5));
                outflow_lat[layer] = Utility.Math.Divide(i, j, 0.0);

                //! Cannot drop sw below dul
                max_flow = Math.Max(0.0, (_sw_dep[layer] - _dul_dep[layer]));

                outflow_lat[layer] = bound(outflow_lat[layer], 0.0, max_flow);

                _sw_dep[layer] = _sw_dep[layer] - outflow_lat[layer];
            }

        }


        #endregion





        #endregion





        //EVENT HANDLERS

        #region Functions used in Event Handlers (mainly in Init, Reset, UserInit, and Write Summary Report Event Handlers)

        //Summary Report & Init2
        private void soilwat2_sum_report()
        {

            //*+  Mission Statement
            //*      Report SoilWat module summary details

            double depth_layer_top;     //! depth to top of layer (mm)
            double depth_layer_bottom;  //! depth to bottom of layer (mm)
            int layer;               //! layer number
            int num_layers;          //! number of soil profile layers
            string line;                //! temp output record
            double[] runoff_wf;           //! weighting factor for runoff
            double[] usw;                 //! unavail. sw (mm)
            double[] asw;                 //! avail. sw (mm)
            double[] masw;                //! max unavail. sw (mm)
            double[] dsw;                 //! drainable sw (mm)

            num_layers = _dlayer.Length;
            runoff_wf = new double[num_layers];
            usw = new double[num_layers];
            asw = new double[num_layers];
            masw = new double[num_layers];
            dsw = new double[num_layers];

            Console.WriteLine();    //new line
#if COMPARISON
        Console.WriteLine();
        Console.WriteLine();
#endif

            line = "                 Soil Profile Properties";
            Console.WriteLine(line);

            line = "   ---------------------------------------------------------------------";
            Console.WriteLine(line);

            if (!using_ks)
            {

                line = "         Depth  Air_Dry  LL15   Dul    Sat     Sw     BD   Runoff  SWCON";
                Console.WriteLine(line);

                line = "           mm     mm/mm  mm/mm  mm/mm  mm/mm  mm/mm  g/cc    wf";
                Console.WriteLine(line);
            }
            else
            {
                line = "         Depth  Air_Dry  LL15   Dul    Sat     Sw     BD   Runoff  SWCON   Ks";
                Console.WriteLine(line);

                line = "           mm     mm/mm  mm/mm  mm/mm  mm/mm  mm/mm  g/cc    wf           mm/day";
                Console.WriteLine(line);
            }

            line = "   ---------------------------------------------------------------------";
            Console.WriteLine(line);

            depth_layer_top = 0.0;
            soilwat2_runoff_depth_factor(ref runoff_wf);

            for (layer = 0; layer < num_layers; layer++)
            {
                depth_layer_bottom = depth_layer_top + _dlayer[layer];

                if (!using_ks)
                {
                    line = String.Format("   {0,6:0.#} {1} {2,4:0.#} {3,6:0.000} {4,6:0.000} {5,6:0.000} {6,6:0.000} {7,6:0.000} {8,6:0.000} {9,6:0.000} {10,6:0.000}",
                                         depth_layer_top,
                                         "-",
                                         depth_layer_bottom,
                                         Utility.Math.Divide(_air_dry_dep[layer], _dlayer[layer], 0.0),
                                         Utility.Math.Divide(_ll15_dep[layer], _dlayer[layer], 0.0),
                                         Utility.Math.Divide(_dul_dep[layer], _dlayer[layer], 0.0),
                                         Utility.Math.Divide(_sat_dep[layer], _dlayer[layer], 0.0),
                                         Utility.Math.Divide(_sw_dep[layer], _dlayer[layer], 0.0),
                                         bd[layer],
                                         runoff_wf[layer],
                                         Soil.SWCON[layer]);
                }
                else
                {
                    line = String.Format("   {0,6:0.#} {1} {2,4:0.#} {3,6:0.000} {4,6:0.000} {5,6:0.000} {6,6:0.000} {7,6:0.000} {8,6:0.000} {9,6:0.000} {10,6:0.000} {11,6:0.000}",
                                         depth_layer_top,
                                         "-",
                                         depth_layer_bottom,
                                         Utility.Math.Divide(_air_dry_dep[layer], _dlayer[layer], 0.0),
                                         Utility.Math.Divide(_ll15_dep[layer], _dlayer[layer], 0.0),
                                         Utility.Math.Divide(_dul_dep[layer], _dlayer[layer], 0.0),
                                         Utility.Math.Divide(_sat_dep[layer], _dlayer[layer], 0.0),
                                         Utility.Math.Divide(_sw_dep[layer], _dlayer[layer], 0.0),
                                         bd[layer],
                                         runoff_wf[layer],
                                         Soil.SWCON[layer],
                                         ks[layer]);
                }
                Console.WriteLine(line);
                depth_layer_top = depth_layer_bottom;
            }

            line = "   ---------------------------------------------------------------------";
            Console.WriteLine(line);

            Console.WriteLine();
            Console.WriteLine();
#if COMPARISON
        Console.WriteLine();
#endif

            line = "             Soil Water Holding Capacity";
            Console.WriteLine(line);

            line = "     ---------------------------------------------------------";
            Console.WriteLine(line);

            line = "         Depth    Unavailable Available  Max Avail.  Drainable";
            Console.WriteLine(line);
            line = "                     (LL15)   (SW-LL15)  (DUL-LL15)  (SAT-DUL)";
            Console.WriteLine(line);

            line = "                       mm        mm          mm         mm";
            Console.WriteLine(line);

            line = "     ---------------------------------------------------------";
            Console.WriteLine(line);

            num_layers = _dlayer.Length;
            depth_layer_top = 0.0;

            for (layer = 0; layer < num_layers; layer++)
            {
                depth_layer_bottom = depth_layer_top + _dlayer[layer];
                usw[layer] = _ll15_dep[layer];
                asw[layer] = Math.Max((_sw_dep[layer] - _ll15_dep[layer]), 0.0);
                masw[layer] = _dul_dep[layer] - _ll15_dep[layer];
                dsw[layer] = _sat_dep[layer] - _dul_dep[layer];

                line = String.Format("   {0,6:0.#} {1} {2,4:0.#} {3,10:0.00} {4,10:0.00} {5,10:0.00} {6,10:0.00}",
                                     depth_layer_top,
                                     "-",
                                     depth_layer_bottom,
                                     usw[layer],
                                     asw[layer],
                                     masw[layer],
                                     dsw[layer]);

                Console.WriteLine(line);
                depth_layer_top = depth_layer_bottom;
            }

            line = "     ---------------------------------------------------------";
            Console.WriteLine(line);

            line = String.Format("           Totals{0,10:0.00} {1,10:0.00} {2,10:0.00} {3,10:0.00}",
                                 Utility.Math.Sum(usw),
                                 Utility.Math.Sum(asw),
                                 Utility.Math.Sum(masw),
                                 Utility.Math.Sum(dsw));

            Console.WriteLine(line);

            line = "     ---------------------------------------------------------";
            Console.WriteLine(line);


            //! echo sw parameters

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
#if COMPARISON
        Console.WriteLine();
        Console.WriteLine();
#endif

            line = "             Initial Soil Parameters";
            Console.WriteLine(line);

            line = "     ---------------------------------------------------------";
            Console.WriteLine(line);

            line = "            Salb     Dif_Con   Dif_Slope";
            Console.WriteLine(line);

            line = "     ---------------------------------------------------------";
            Console.WriteLine(line);

            line = String.Format("       {0,11:0.00} {1,11:0.00} {2,11:0.00}",
                                 Salb,
                                 DiffusConst,
                                 DiffusSlope);

            Console.WriteLine(line);

            line = "     ---------------------------------------------------------";
            Console.WriteLine(line);
            Console.WriteLine();
            Console.WriteLine();
#if COMPARISON
        Console.WriteLine();
#endif

            if (obsrunoff_name != "")
            {
                string obsrunoff_name_trunc;
                obsrunoff_name_trunc = obsrunoff_name.Trim();      //get rid of any whitespaces before and after the name.
                line = String.Format("      {0} {1} {2}",
                                     "             Observed runoff data ( ",
                                     obsrunoff_name_trunc,
                                     " ) is used in water balance");

                Console.WriteLine(line);
            }
            else
            {
                //! no observed data
                Console.WriteLine("             Runoff is predicted using scs curve number:");
                line = "           Cn2  Cn_Red  Cn_Cov   H_Eff_Depth ";
                Console.WriteLine(line);

                line = "                                      mm     ";
                Console.WriteLine(line);

                line = "     ---------------------------------------------------------";
                Console.WriteLine(line);

                line = String.Format("      {0,8:0.00} {1,7:0.00} {2,7:0.00} {3,11:0.00}",
                                     _cn2_bare,
                                     _cn_red,
                                     _cn_cov,
                                     hydrol_effective_depth);

                Console.WriteLine(line);

                line = "     ---------------------------------------------------------";
                Console.WriteLine(line);
            }


            Console.WriteLine();
            Console.WriteLine();
#if COMPARISON
        Console.WriteLine();
#endif


            if (evap_method == ritchie_method)
            {
                line = "      Using Ritchie evaporation model";
                Console.WriteLine(line);

                if (WinterU == SummerU)
                {
                    line = String.Format("       {0} {1,8:0.00} {2}",
                                         "Cuml evap (U):        ",
                                         _u,
                                         " (mm^0.5)");

                    Console.WriteLine(line);
                }
                else
                {
                    line = String.Format("        {0} {1,8:0.00} {2}        {3} {4,8:0.00} {5}",
                                         "Stage 1 Duration (U): Summer    ",
                                         SummerU,
                                         " (mm)" + Environment.NewLine,
                                         "                      Winter    ",
                                         WinterU,
                                         " (mm)");
                    Console.WriteLine(line);
                }

                if (WinterCona == SummerCona)
                {
                    line = String.Format("       {0} {1,8:0.00} {2}",
                                         "CONA:                 ",
                                         _cona,
                                         " ()");
                    Console.WriteLine(line);
                }
                else
                {
                    line = String.Format("        {0} {1,8:0.00} {2}        {3} {4,8:0.00} {5}",
                                         "Stage 2       (CONA): Summer    ",
                                         SummerCona,
                                         " (mm^0.5)" + Environment.NewLine,
                                         "                      Winter    ",
                                         WinterCona,
                                         " (mm^0.5)");
                    Console.WriteLine(line);
                }

                if ((WinterCona != SummerCona) || (WinterU != SummerU))
                {
                    Console.WriteLine("       Critical Dates:       Summer        " + SummerDate + Environment.NewLine +
                    "                             Winter        " + WinterDate);
                }
            }
            else
            {
                line = "     Using unknown evaporation method!";
                Console.WriteLine(line);
            }

#if (!COMPARISON)
            Console.WriteLine();
#endif


            if (_eo_source != "")
            {
                line = String.Format("      {0} {1}",
                                     "Eo source:             ",
                                     _eo_source);
                Console.WriteLine(line);
            }
            else
            {
                line = String.Format("       {0}",
                                     "Eo from priestly-taylor");
                Console.WriteLine(line);
            }

#if (!COMPARISON)
            Console.WriteLine();
#endif
        }

        //Init2, Reset, UserInit
        private void soilwat2_init()
        {
            //*+  Purpose
            //*       input initial values from soil water parameter files.

            //*+  Mission Statement
            //*       Initialise SoilWat module


            soilwat2_read_constants();

            soilwat2_soil_property_param();

            soilwat2_soil_profile_param();

            soilwat2_evap_init();

            Lateral_init();

            initDone = true;     //let the classes properties to now allow "sets"

            for (int layer = 0; layer < _dlayer.Length; layer++)
                soilwat2_check_profile(layer);

            //publish event saying there is a new soil profile.
            soilwat2_New_Profile_Event();

        }


        private void soilwat2_save_state()
        {
            oldSWDep = soilwat2_total_sw_dep();
        }


        private void soilwat2_delta_state()
        {
            double dltSWDep;
            double newSWDep;

            newSWDep = soilwat2_total_sw_dep();
            dltSWDep = newSWDep - oldSWDep;
            soilwat2_ExternalMassFlow(dltSWDep);       //tell the "System Balance" module (if there is one) that the user has changed the water by this amount (by doing a Reset).
        }


        private double soilwat2_total_sw_dep()
        {
            //only used above in save_state and delta_state
            return Utility.Math.Sum(_sw_dep);
        }


        #endregion



        #region Clock Event Handlers


        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e)
        {

            day = Clock.Today.DayOfYear;
            year = Clock.Today.Year;

            initDone = false;

            //ApsimFile.Soil Soil = (ApsimFile.Soil) Paddock.Get("Soil");
            //diffus_const = Soil.SoilWater.DiffusConst;
            //diffus_slope = Soil.SoilWater.DiffusSlope;
            //cn2_bare = Soil.SoilWater.CN2Bare;
            //cn_red = Soil.SoilWater.CNRed;
            //cn_cov = Soil.SoilWater.CNCov;
            //if (!double.IsNaN(Soil.SoilWater.MaxPond))
            //    max_pond = Soil.SoilWater.MaxPond;
            //salb = Soil.SoilWater.Salb;
            //summerdate = Soil.SoilWater.SummerDate;
            //summeru = Soil.SoilWater.SummerU;
            //summercona = Soil.SoilWater.SummerCona;
            //winterdate = Soil.SoilWater.WinterDate;
            //winteru = Soil.SoilWater.WinterU;
            //wintercona = Soil.SoilWater.WinterCona;
            //slope = Soil.SoilWater.Slope;
            //discharge_width = Soil.SoilWater.DischargeWidth;
            //catchment_area = Soil.SoilWater.CatchmentArea;

            dlayer = Soil.Thickness;
            sat = Soil.SAT;
            dul = Soil.DUL;
            sw = Soil.SW;
            ll15 = Soil.LL15;
            air_dry = Soil.AirDry;
            ks = Soil.Water.KS;
            bd = Soil.Water.BD;
            sw = Soil.SW;

            // some defaults.
            if (SWCON == null)
            {
                SWCON = new double[dlayer.Length];
                for (int i = 0; i < dlayer.Length; i++)
                    SWCON[i] = 0.3;
            }
            //Save State
            soilwat2_save_state();

            soilwat2_init();

            soilwat2_sum_report();

            //Change State
            soilwat2_delta_state();

            initDone = true;
        }

        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            //*     ===========================================================
            //      subroutine soilwat2_prepare
            //*     ===========================================================

            //*+  Purpose
            //*       Calculate potential evapotranspiration
            //*
            //*+  Mission Statement
            //*     Perform all APSIM Timestep calculations

            //*- Implementation Section ----------------------------------

            day = Clock.Today.DayOfYear;
            year = Clock.Today.Year;

            soilwat2_zero_daily_variables();
            Lateral_zero_daily_variables();     //sv- I added this from Lateral_prepare()
            soilwat2_get_crop_variables();

            //! potential: sevap + transpiration:
            soilwat2_pot_evapotranspiration();
            real_eo = eo;  //! store for reporting
        }


        [EventSubscribe("MiddleOfDay")]
        private void OnProcess(object sender, EventArgs e)
        {

            //Get variables from other modules
            //taken from Main() 
            soilwat2_get_other_variables();

            //sv- I added everything above



            //*     ===========================================================
            //      subroutine soilwat2_process
            //*     ===========================================================
            //*+  Purpose
            //*       simulates runoff, infiltration, flux (drainage), unsaturated flow,
            //*       evaporation, solute movement, transpiration.

            //*+  Local Variables

            int layer;                 //! layer number counter variable
            int num_layers;            //! number of layers
            double extra_runoff;          //! water backed up from flux calculations that was unable to enter profile

            //*- Implementation Section ----------------------------------


            num_layers = _dlayer.Length;

            // LATERAL FLOW

            Lateral_process();

            // RUNOFF

            soilwat2_cover_surface_runoff();

            //c dsg 070302 added runon
            //! NIH Need to consider if interception losses were already considered in runoff model calibration

            if (irrigation_will_runoff == 0)
            {
                soilwat2_runoff(rain, runon, (interception + residueinterception), ref runoff_pot);
            }
            else
            {
                //calculate runoff but allow irrigations to runoff just like rain.
                soilwat2_runoff((rain + irrigation), runon, (interception + residueinterception), ref runoff_pot);
            }


            //! DSG  041200
            //! g%runoff_pot is the runoff which would have occurred without
            //! ponding.  g%runoff is the ammended runoff after taking any
            //! ponding into account

            pond = pond + runoff_pot;
            runoff = Math.Max((pond - _max_pond), 0.0);
            pond = Math.Min(pond, _max_pond);

            // INFILTRATION

            soilwat2_infiltration();

            //! all infiltration and solutes(from irrigation)
            //! go into the top layer.

            _sw_dep[0] = _sw_dep[0] + infiltration;

            // IRRIGATION

            if (irrigation_layer > 1)    //if this is a sub surface irrigation
            {
                //add the irrigation
                _sw_dep[irrigation_layer - 1] = _sw_dep[irrigation_layer - 1] + irrigation;
            }

            //! save solutes from irrigation
            soilwat2_irrig_solute();
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

            irrigation = 0.0;
            for (int solnum = 0; solnum < num_solutes; solnum++)
                solutes[solnum].irrigation = 0.0;

            // SATURATED FLOW (flux calculation, aka Drainage) 

            //sv- I added this
            extra_runoff = 0.0;

            if (using_ks)
            {
                soilwat2_drainage(ref extra_runoff);    //sv- this returns flux[] and extra_runoff  //nb. this only calculates the flux it does not move it or change any sw values. That is done in move_down_real() 
            }
            else
            {
                soilwat2_drainage_old(ref extra_runoff); //sv- this returns flux[] and extra_runoff //nb. this only calculates the flux it does not move it or change any sw values. That is done in move_down_real()
            }

            //"runoff" is caused by permeability of top layer(cn2Bare). This permeability is modified cover(cnCov, cnRed) and moisture content.   
            //"extra_runoff" is caused by backing up of top layer due to inability of soil to drain. See soilwat2_drainage() above.

            //Any extra_runoff then it becomes a pond. 
            pond = Math.Min(extra_runoff, _max_pond);
            //If there is too much for the pond handle then add the excess (ie. extra_runoff-pond) to normal runoff.
            runoff = runoff + extra_runoff - pond;
            //Deduct the extra_runoff from the infiltration because it did not infiltrate (because it backed up).
            infiltration = infiltration - extra_runoff;

            _sw_dep[0] = _sw_dep[0] - extra_runoff;   //sv- actually add the extra runoff to _sw_dep

            //! move water down     (Saturated Flow - alter _sw_dep values using flux calculation)
            MoveDownReal(flux, ref _sw_dep);

            //! drainage out of bottom layer
            drain = flux[num_layers - 1];

            // SATURATED FLOW SOLUTE MOVEMENT

            //! now move the solutes with flux  
            //! flux -  flow > dul
            soilwat2_move_solute_down();

            // EVAPORATION

            //! actual soil evaporation:
            soilwat2_evaporation();

            //soilwat2_pot_evapotranspiration() is called in the prepare event. 
            //This "_effective calculation()" just takes ponding into account.
            //! potential: sevap + transpiration:
            soilwat2_pot_evapotranspiration_effective();

            //! ** take away evaporation
            for (layer = 0; layer < num_layers; layer++)
            {
                _sw_dep[layer] = _sw_dep[layer] - es_layers[layer];
            }

            // UNSATURATED FLOW (flow calculation)

            //! get unsaturated flow   
            soilwat2_unsat_flow();

            //! move water up          (Unsaturated Flow - alter _sw_dep values using flow calculation)
            MoveUpReal(flow, ref _sw_dep);

            //! now check that the soil water is not silly
            for (layer = 0; layer < num_layers; layer++)
            {
                soilwat2_check_profile(layer);
            }

            // WATER TABLE

            _water_table = soilwat_water_table();

            // UNSATURATED FLOW SOLUTE MOVEMENT

            //! now move the solutes with flow  
            soilwat2_move_solute_up();

            //sv- I added everything below.

            //Change the variables in other modules
            //taken from Main() 
            soilwat2_set_other_variables();

            if (WaterMovementCompleted != null)
                WaterMovementCompleted.Invoke(this, new EventArgs());
        }

        #endregion


        #region Met, Irrig, Solute, Plants Event Handlers

        [EventSubscribe("NewMet")]
        private void OnNewMet(WeatherFile.NewMetType NewMet)
        {
            //*     ===========================================================
            //      subroutine soilwat2_ONnewmet (variant)
            //*     ===========================================================

            //*+  Purpose
            //*     Get new met data

            //*+  Mission Statement
            //*     Get new met data

            //*- Implementation Section ----------------------------------

            radn = NewMet.radn;
            maxt = NewMet.maxt;
            mint = NewMet.mint;
            rain = NewMet.rain;

            bound_check_real_var(radn, 0.0, 60.0, "radn");
            bound_check_real_var(maxt, -50.0, 60.0, "maxt");
            bound_check_real_var(mint, -50.0, 50.0, "mint");
            bound_check_real_var(rain, 0.0, 5000.0, "rain");

        }

        //ToDo: Need to work out what the NewSolute event will be.

        [EventSubscribe("NewSolute")]
        private void OnNewSolute(NewSoluteType newsolute)
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

            //*- Implementation Section ----------------------------------
            numvals = newsolute.solutes.Length;

            Array.Resize(ref solutes, num_solutes + numvals);

            for (counter = 0; counter < numvals; counter++)
            {
                name = newsolute.solutes[counter].ToLower();
                solutes[num_solutes] = new Solute();
                solutes[num_solutes].name = name;
                solutes[num_solutes].ownerName = newsolute.OwnerFullPath;
                solutes[num_solutes].mobility = PositionInCharArray(name, mobile_solutes) >= 0;
                if (!solutes[num_solutes].mobility && PositionInCharArray(name, immobile_solutes) < 0)
                    throw new Exception("No solute mobility information for " + name + " , please specify as mobile or immobile in the SoilWater ini file.");
                int nLayers = _dlayer.Length;
                // Create layer arrays for the new solute
                solutes[num_solutes].amount = new double[nLayers];
                solutes[num_solutes].delta = new double[nLayers];
                solutes[num_solutes].leach = new double[nLayers];
                solutes[num_solutes].up = new double[nLayers];
                // Register new "flow" and "leach" outputs for these solutes
                // See "getPropertyValue" function for the callback used to actually retrieve the values
                num_solutes = num_solutes + 1;
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

        //public void OnIrrigated(IrrigationApplicationType Irrigated)
        //{

        //    //* ====================================================================
        //    //subroutine soilwat2_ONirrigated ()
        //    //* ====================================================================

        //    //*+  Mission Statement
        //    //*     Add Water

        //    //*+  Local Variables
        //    int solnum;           //! solute no. counter variable               
        //    double solute_amount = 0.0;

        //    //*- Implementation Section ----------------------------------


        //    //see OnProcess event handler for where this irrigation is added to the soil water 
        //    irrigation = Irrigated.Amount;  //! amount of irrigation (mm)    

        //    //Added on 5 Dec 2012 to allow irrigation to runoff like rain. 
        //    irrigation_will_runoff = Irrigated.will_runoff;

        //    if ((irrigation_will_runoff == 1) && (Irrigated.Depth > 0.0))
        //    {
        //        irrigation_will_runoff = 0;
        //        String warningText;
        //        warningText = "In the irrigation 'apply' command in the line above, 'will_runoff' was set to 0 not 1" + "\n"
        //        + "If irrigation depth > 0 (mm), " + "\n"
        //         + "then you can not choose to have irrigation runoff like rain as well. ('will_runoff = 1')" + "\n"
        //         + "ie. Subsurface irrigations can not runoff like rain does. (Only surface irrigation can)" + "\n"
        //         + "nb. Subsurface irrigations will cause runoff if ponding occurs though.";
        //        IssueWarning(warningText);
        //    }


        //    //sv- added on 26 Nov 2012. Needed for subsurface irrigation. 
        //    //    Manager module sends "apply" command specifying depth as a argument to irrigation module.
        //    //    irrigation module sends "Irrigated" event with the depth. 
        //    //    Now need to turn depth into the specific subsurface layer that the irrigation is to go into.
        //    irrigation_layer = FindLayerNo(Irrigated.Depth) + 1;    //irrigation_layer is 1 based layer number but FindLayerNo() returns zero based layer number, so add 1.


        //    //Solute amount in irrigation water.
        //    for (solnum = 0; solnum < num_solutes; solnum++)
        //    {
        //        switch (solutes[solnum].name)
        //        {
        //            case "no3":
        //                solute_amount = Irrigated.NO3;
        //                break;
        //            case "nh4":
        //                solute_amount = Irrigated.NH4;
        //                break;
        //            case "cl":
        //                solute_amount = Irrigated.CL;
        //                break;
        //            default:
        //                solute_amount = 0.0;
        //                break;
        //        }

        //        //this irrigation_solute is added to the the soil solutes (solute 2D array) when soilwat2_irrig_solute() is called from OnProcess event handler.
        //        solutes[solnum].irrigation += solute_amount;
        //    }
        //}

        [EventSubscribe("WaterChanged")]
        private void OnWaterChanged(WaterChangedType WaterChanged)
        {

            //This event is Only used by Plant2 and AgPasture.
            //This event was added so that the Plant2 module could extract water via its roots from the SoilWater module.
            //At the time Plant2 was not advanced enough to be able to do a "Set" on another modules variables.
            //Plant2 still uses this method to extract water using its roots.

            //*+  Purpose
            //*     Another module wants to change our water


            int layer;

            for (layer = 0; layer < WaterChanged.DeltaWater.Length; layer++)
            {
                _sw_dep[layer] = _sw_dep[layer] + WaterChanged.DeltaWater[layer];
                soilwat2_check_profile(layer);
            }

        }


        #endregion


        #region Manager Event Handlers

        public void Reset()
        {
            //nb. this is the same as OnUserInit Event

            //Save State
            soilwat2_save_state();
            soilwat2_zero_variables();
            soilwat2_get_other_variables();
            soilwat2_init();

            //Change State
            soilwat2_delta_state();
        }


        //OnUserInit is no longer supported. It has been replaced by the OnReset() above.


        public void Tillage(TillageType Tillage)
        {
            //*     ===========================================================
            //      subroutine soilwat2_tillage ()
            //*     ===========================================================
            //*+  Purpose
            //*     Set up for CN reduction after tillage operation

            //*+  Notes
            //*       This code is borrowed from residue module.

            //*+  Mission Statement
            //*       Calculate tillage effects

            //*+  Local Variables
            string message;             //! message string
            string tillage_type;             //! name of implement used for tillage//! 1. Find which implement was used. eg. disc, burn, etc.


            //*- Implementation Section ----------------------------------

            // cn_red is the reduction in the cn value, and cn_rain is the amount of rainfall after the tillage event that the reduction ceases to occur.

            //the event always gives us at least the type of tillage. Even if it does not give the cn_red and cn_rain.
            //if the event does not give us cn_red and cn_rain then use the type name to look up the values in the sim file (ini file).
            // ez - departs slightly from the Fortran version, where cn_red and cn_rain were optional arguments
            // They are always present here, but if the user sets the value to a negative number, we'll then
            // try to read the values from the initialisation data.

            tillage_type = Tillage.Name;       //sv - the event always gives us at least this.

            //sv- if the Tillage information did not come with the event.
            if ((Tillage.cn_red <= 0) || (Tillage.cn_rain <= 0))
            {
                Console.WriteLine();
                Console.WriteLine("    - Reading tillage CN info");

                TillageType data = SoilWatTillageType.GetTillageData(tillage_type);

                if (data == null)
                {
                    //sv- Event did not give us the tillage information and the sim file does not have the tillage information.
                    //! We have an unspecified tillage type
                    tillage_cn_red = 0.0;
                    tillage_cn_rain = 0.0;

                    message = "Cannot find info for tillage:- " + Tillage.Name;
                    throw new Exception(message);
                }
                else
                {
                    //sv- Get the values from the sim file.
                    tillage_type = "tillage specified in ini file.";
                    if (Tillage.cn_red >= 0)
                        tillage_cn_red = data.cn_red;

                    if (Tillage.cn_rain >= 0)
                        tillage_cn_rain = data.cn_rain;
                }
            }
            else
            {
                tillage_cn_red = Tillage.cn_red;
                tillage_cn_rain = Tillage.cn_rain;
            }

            //! Ensure cn equation won't go silly
            tillage_cn_red = bound(tillage_cn_red, 0.0, _cn2_bare);

            //sv- write what we are doing to the summary file.
            string line;
            line = String.Format("{0} {1} {2}                                        {3} {4:F} {5}                                        {6} {7:F}",
                                 "Soil tilled using ", tillage_type, Environment.NewLine, "CN reduction = ", tillage_cn_red, Environment.NewLine, "Acc rain     = ", tillage_cn_rain);
            Console.WriteLine(line);


            //! 3. Reset the accumulator
            tillage_rain_sum = 0.0;

        }

        #endregion


        //EVENTS - SENDING

        #region Functions used to Publish Events sent by this module


        private void soilwat2_New_Profile_Event()
        {
            //*+  Mission Statement
            //*     Advise other modules of new profile specification
            if (NewProfile != null)
            {
                NewProfileType newProfile = new NewProfileType();
                int nLayers = _dlayer.Length;
                // Convert array values from doubles to floats
                newProfile.air_dry_dep = _air_dry_dep;
                newProfile.bd = bd;
                newProfile.dlayer = _dlayer;
                newProfile.dul_dep = _dul_dep;
                newProfile.ll15_dep = _ll15_dep;
                newProfile.sat_dep = _sat_dep;
                newProfile.sw_dep = _sw_dep;
                if (NewProfile != null)
                    NewProfile.Invoke(newProfile);
            }
        }


        private void soilwat2_ExternalMassFlow(double sw_dep_delta_sum)
        {

        //    //*+  Mission Statement
        //    //*     Update internal time record and reset daily state variables.

        //    //External Mass Flow event is used for a model called "System Balance" which just keeps track of all the water, solutes etc in the model. 
        //    //To make sure it all balances out and no water is being lost from the system. It is used for debugging purposes.
        //    //Some times however the user will do something that will diliberately upset this, such as forcibly reseting a water content by doing a
        //    //Reset command in a manager or by Setting a variable in the manager manually. When this happens the "System Balance" module's balance 
        //    //no longer adds up. So when you do a Reset or Set a variable you must send an External Mass Flow Type event that alerts the "System Balance"
        //    //module that the user has forced a change and the amount by which they have changed it, so that the "System Balance" module can add this
        //    //amount to its balance so it's balance will work out correctly again. 

        //    ExternalMassFlowType massBalanceChange = new ExternalMassFlowType();


        //    if (sw_dep_delta_sum >= 0.0)
        //    {
        //        massBalanceChange.FlowType = "gain";
        //    }
        //    else
        //    {
        //        massBalanceChange.FlowType = "loss";
        //    }

        //    massBalanceChange.PoolClass = "soil";
        //    massBalanceChange.DM = 0.0F;
        //    massBalanceChange.C = 0.0F;
        //    massBalanceChange.N = 0.0F;
        //    massBalanceChange.P = 0.0F;
        //    massBalanceChange.SW = Math.Abs((float)sw_dep_delta_sum);

        //    if (ExternalMassFlow != null)
        //        ExternalMassFlow.Invoke(massBalanceChange);

        }

        #endregion


        #region Events sent by this Module

        //Events
        public event NewProfileDelegate NewProfile;
        //public event ExternalMassFlowDelegate ExternalMassFlow;
        public event RunoffEventDelegate Runoff;
        public event NitrogenChangedDelegate NitrogenChanged;
        public event EventHandler WaterMovementCompleted;
        #endregion


    }

    public class SoilWatTillageType
    {
        Dictionary<string, float[]> tillage_types;

        protected float[] strToArr(string str)
        {
            string[] temp = str.Split(new char[] { ' ', '\t', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            float[] result = new float[temp.Length];

            for (int i = 0; i < result.Length; i++)
                result[i] = float.Parse(temp[i]);

            return result;
        }

        public System.Xml.XmlNode xe = null;

        [XmlAnyElement]
        public System.Xml.XmlElement[] Nodes = null;

        public void OnInitialised()
        {
            tillage_types = new Dictionary<string, float[]>();

#if (APSIMX == true)
            foreach (System.Xml.XmlNode xnc in Nodes)
                if (xnc.NodeType == System.Xml.XmlNodeType.Element)
                    tillage_types.Add(xnc.Name, strToArr(xnc.FirstChild.Value));
#else
            foreach (System.Xml.XmlNode xnc in xe.ChildNodes)
                if (xnc.NodeType == System.Xml.XmlNodeType.Element)
                    tillage_types.Add(xnc.Name, strToArr(xnc.FirstChild.Value));
#endif
        }

        public TillageType GetTillageData(string name)
        {
            throw new NotImplementedException("SoilWatTillageType not implemented");
        //    return tillage_types.ContainsKey(name) ? new TillageType() { type = name, cn_red = tillage_types[name][0], cn_rain = tillage_types[name][1] } : null;
        }
    }

}