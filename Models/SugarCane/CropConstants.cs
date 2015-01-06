
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;


[Serializable]
public class CropConstants
    {

    const int max_table = 10;   //! Maximum size_of of tables
    const int max_stage = 6;    //! number of growth stages
    const int max_part = 5;     //! number of plant parts


    //*     ===========================================================
    //      subroutine sugar_read_crop_constants (section_name)
    //*     ===========================================================



    //!    sugar_get_cultivar_params

    //! full names of stages for reporting
    //[Param]
    public string[] stage_names { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1000.0, Name = "stage_code")]
    public double[] stage_code_list { get; set; }

    //! radiation use efficiency (g dm/mj)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("g dm/mj")]
    public double[] rue { get; set; }

    //! root growth rate potential (mm depth/day)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("mm")]
    public double[] root_depth_rate { get; set; }

    //! root:shoot ratio of new dm ()
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    public double[] ratio_root_shoot { get; set; }

    //! transpiration efficiency coefficient to convert vpd to transpiration efficiency (kpa)
    //! although this is expressed as a pressure it is really in the form kpa*g carbo per m^2 / g water per m^2
    //! and this can be converted to kpa*g carbo per m^2 / mm water
    //! because 1g water = 1 cm^3 water
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] transp_eff_cf { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] n_fix_rate { get; set; }

    //! radiation extinction coefficient ()
    //[Param(MinVal = 0.0, MaxVal = 10.0)]
    public double extinction_coef { get; set; }

    //! radiation extinction coefficient () of dead leaves
    //[Param(MinVal = 0.0, MaxVal = 10.0)]
    public double extinction_coef_dead { get; set; }



    //! crop failure

    //! critical number of leaves below which portion of the crop maydie due to water stress
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double leaf_no_crit { get; set; }

    //! maximum degree days allowed foremergence to take place (deg day)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("oC")]
    public double tt_emerg_limit { get; set; }

    //! maximum days allowed after sowing for germination to take place (days)
    //[Param(MinVal = 0.0, MaxVal = 365.0)]
    [Units("days")]
    public double days_germ_limit { get; set; }

    //! critical cumulative phenology water stress above which the cropfails (unitless)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    public double swdf_pheno_limit { get; set; }

    //! critical cumulative photosynthesis water stress above which the crop partly fails (unitless)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    public double swdf_photo_limit { get; set; }

    //! rate of plant reduction with photosynthesis water stress
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double swdf_photo_rate { get; set; }




    //!    sugar_root_depth

    //! initial depth of roots (mm)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("mm")]
    public double initial_root_depth { get; set; }

    //! length of root per unit wt (mm/g)
    //[Param(MinVal = 0.0, MaxVal = 50000.0)]
    [Units("mm")]
    public double specific_root_length { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 0.1)]
    [Units("mm/mm3/plant")]
    public double[] x_plant_rld { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    [Units("0-1")]
    public double[] y_rel_root_rate { get; set; }



    //! fraction of roots that dies back at harvest (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    [Units("0-1")]
    public double root_die_back_fr { get; set; }



    //!    sugar_leaf_area_init

    //! initial plant leaf area (mm^2)
    //[Param(MinVal = 0.0, MaxVal = 100000.0)]
    [Units("mm^2")]
    public double initial_tpla { get; set; }



    //!    sugar_leaf_area_devel

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] sla_lfno { get; set; }


    //! maximum specific leaf area for new leaf area (mm^2/g)
    //[Param(MinVal = 0.0, MaxVal = 50000.0)]
    [Units("mm^2/g")]
    public double[] sla_max { get; set; }

    //! minimum specific leaf area for new leaf area (mm^2/g)
    //[Param(MinVal = 0.0, MaxVal = 50000.0)]
    [Units("mm^2/g")]
    public double[] sla_min { get; set; }



    //!    sugar_height

    //[Param(MinVal = 0.0, MaxVal = 10000.0)]
    [Units("g/plant")]
    public double[] x_stem_wt { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 10000.0)]
    [Units("mm")]
    public double[] y_height { get; set; }




    //!    sugar_transp_eff

    //! fraction of distance between svp at min temp and svp at max temp where average svp during transpiration lies. (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double svp_fract { get; set; }



    //!    cproc_sw_demand_bound

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double eo_crop_factor_default { get; set; }



    //!    sugar_germination

    //! plant extractable soil water in seedling layer inadequate for germination (mm/mm)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    [Units("mm/mm")]
    public double pesw_germ { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    [Units("0-1")]
    public double[] fasw_emerg { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    [Units("0-1")]
    public double[] rel_emerg_rate { get; set; }




    //!    sugar_leaf_appearance

    //! leaf number at emergence ()
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double leaf_no_at_emerg { get; set; }



    //!    sugar_phenology_init

    //! minimum growing degree days for germination (deg days)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("oC")]
    public double shoot_lag { get; set; }

    //! growing deg day increase with depth for germination (deg day/mm depth)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    [Units("oC/mm")]
    public double shoot_rate { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("oC")]
    public double[] x_node_no_app { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("oC")]
    public double[] y_node_app_rate { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("oC")]
    public double[] x_node_no_leaf { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("oC")]
    public double[] y_leaves_per_node { get; set; }




    //!    sugar_dm_init

    //! root growth before emergence (g/plant)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("g/plant")]
    public double dm_root_init { get; set; }

    //! stem growth before emergence (g/plant)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("g/plant")]
    public double dm_sstem_init { get; set; }

    //! leaf growth before emergence (g/plant)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("g/plant")]
    public double dm_leaf_init { get; set; }

    //! cabbage "    "        "        "
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("g/plant")]
    public double dm_cabbage_init { get; set; }

    //! sucrose "    "        "        "
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    [Units("g/plant")]
    public double dm_sucrose_init { get; set; }

    //! ratio of leaf wt to cabbage wt ()
    //[Param(MinVal = 0.0, MaxVal = 10.0)]
    public double leaf_cabbage_ratio { get; set; }

    //! fraction of cabbage that is leaf sheath(0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double cabbage_sheath_fr { get; set; }



    //!    sugar_dm_senescence

    //! fraction of root dry matter senescing each day (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double dm_root_sen_frac { get; set; }



    //!    sugar_dm_dead_detachment

    //! fraction of dead plant parts detaching each day (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] dead_detach_frac { get; set; }

    //! fraction of senesced dry matter detaching from live plant each day (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] sen_detach_frac { get; set; }



    //!    sugar_leaf_area_devel

    //! corrects for other growing leaves
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double leaf_no_correction { get; set; }



    //!    sugar_leaf_area_sen_light

    //! critical lai above which light
    //[Param(MinVal = 0.0, MaxVal = 10.0)]
    [Units("m^2/m^2")]
    public double lai_sen_light { get; set; }

    //! slope of linear relationship between lai and light competition factor for determining leaf senesence rate.
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double sen_light_slope { get; set; }



    //!    sugar_leaf_area_sen_frost

    //[Param(MinVal = -20.0, MaxVal = 100.0)]
    [Units("oC")]
    public double[] frost_temp { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    [Units("oC")]
    public double[] frost_fraction { get; set; }



    //!    sugar_leaf_area_sen_water

    //! slope in linear eqn relating soil water stress during photosynthesis to leaf senesense rate
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double sen_rate_water { get; set; }



    //!    sugar_phenology_init

    //! twilight in angular distance between sunset and end of twilight - altitude of sun. (deg)
    //[Param(MinVal = -90.0, MaxVal = 90.0)]
    [Units("o")]
    public double twilight { get; set; }



    //!    sugar_N_conc_limits

    //! stage table for N concentrations(g N/g biomass)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] x_stage_code { get; set; } //all 6 of the following y values use this same "x_stage_code" for their x values.

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// critical N concentration of leaf (g N/g biomass) 
    /// </summary>
    public double[] y_n_conc_crit_leaf { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// minimum N concentration of leaf (g N/g biomass)
    /// </summary>
    public double[] y_n_conc_min_leaf { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// critical N concentration of stem (g N/g biomass)
    /// </summary>
    public double[] y_n_conc_crit_cane { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// minimum N concentration of stem (g N/g biomass)
    /// </summary>
    public double[] y_n_conc_min_cane { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// critical N concentration of flower(g N/g biomass)
    /// </summary>
    public double[] y_n_conc_crit_cabbage { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// minimum N concentration of flower (g N/g biomass)
    /// </summary>
    public double[] y_n_conc_min_cabbage { get; set; }



    //! critical N concentration of root (g N/g biomass)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double n_conc_crit_root { get; set; }

    //! minimum N concentration of root (g N/g biomass)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double n_conc_min_root { get; set; }



    //!    sugar_N_init

    //! initial root N concentration (gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double n_root_init_conc { get; set; }

    //! initial stem N concentration (gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double n_sstem_init_conc { get; set; }

    //! initial leaf N concentration (gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double n_leaf_init_conc { get; set; }

    //!    "   cabbage    "            "
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double n_cabbage_init_conc { get; set; }



    //!    sugar_N_senescence

    //! N concentration of senesced root (gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double n_root_sen_conc { get; set; }

    //! N concentration of senesced leaf (gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double n_leaf_sen_conc { get; set; }

    //! N concentration of senesced cabbage(gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double n_cabbage_sen_conc { get; set; }



    //!    sugar_rue_reduction

    //! critical temperatures for photosynthesis (oC)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    [Units("oC")]
    public double[] x_ave_temp { get; set; }

    //! Factors for critical temperatures (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] y_stress_photo { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    [Units("oC")]
    public double[] x_ave_temp_stalk { get; set; }

    //! Factors for critical temperatures (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] y_stress_stalk { get; set; }





    //!    sugar_tt

    //! temperature table for photosynthesis (degree days)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    [Units("oC")]
    public double[] x_temp { get; set; }

    //! degree days
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    [Units("oC")]
    public double[] y_tt { get; set; }




    //!    sugar_swdef

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] x_sw_demand_ratio { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] y_swdef_leaf { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] x_demand_ratio_stalk { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] y_swdef_stalk { get; set; }




    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] x_sw_avail_ratio { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] y_swdef_pheno { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] x_sw_ratio { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] y_sw_fac_root { get; set; }




    //! Nitrogen Stress Factors
    //! -----------------------

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double k_nfact_photo { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double k_nfact_expansion { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double k_nfact_stalk { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double k_nfact_pheno { get; set; }



    //! Water logging functions
    //! -----------------------

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] oxdef_photo_rtfr { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] oxdef_photo { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 0.20)]
    public double[] x_afps { get; set; }

    //! lookup factors used for reducing effective root length due to reduced air filled pore space.
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] y_afps_fac { get; set; }




    //! Plant Water Content function (dry matter function)
    //! ----------------------------

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] cane_dmf_max { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] cane_dmf_min { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 10000.0)]
    public double[] cane_dmf_tt { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double cane_dmf_rate { get; set; }



    //! Death by Lodging Constants
    //! --------------------------

    //!(0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    [Units("0-1")]
    public double[] stress_lodge { get; set; }

    //!(0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    [Units("0-1")]
    public double[] death_fr_lodge { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double lodge_redn_photo { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double lodge_redn_sucrose { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double lodge_redn_green_leaf { get; set; }





    //Sizes of arrays read in from the INI file (Actually the SIM file).
    //-----------------------------------------

    [XmlIgnore]
    public int num_plant_rld
        {
        get { return y_rel_root_rate.Length;}
        }
    [XmlIgnore]
    public int num_sla_lfno
        {
        get { return sla_lfno.Length; }
        }
    [XmlIgnore]
    public int num_stem_wt
        {
        get { return y_height.Length; }
        }
    [XmlIgnore]
    public int num_fasw_emerg
        {
        get { return rel_emerg_rate.Length; }
        }
    [XmlIgnore]
    public int num_node_no_app
        {
        get { return y_node_app_rate.Length; }
        }
    [XmlIgnore]
    public int num_node_no_leaf
        {
        get { return y_node_app_rate.Length; }
        }
    [XmlIgnore]
    public int num_frost_temp
        {
        get { return frost_temp.Length; }
        }
    [XmlIgnore]
    public int num_N_conc_stage   //! no of values in stage table
        {
        get { return y_n_conc_min_cabbage.Length; }
        }
    [XmlIgnore]
    public int num_ave_temp       //! size_of of critical temperature table
        {
        get { return x_ave_temp.Length; }
        }
    [XmlIgnore]
    public int num_ave_temp_stalk //! size_of of critical temperature table
        {
        get { return x_ave_temp_stalk.Length; }
        }
    [XmlIgnore]
    public int num_temp           //! size_of of table
        {
        get { return y_tt.Length; }
        }
    [XmlIgnore]
    public int num_sw_demand_ratio
        {
        get { return y_swdef_leaf.Length; }
        }
    [XmlIgnore]
    public int num_demand_ratio_stalk
        {
        get { return y_swdef_stalk.Length; }
        }
    [XmlIgnore]
    public int num_sw_avail_ratio
        {
        get { return y_swdef_pheno.Length; }
        }
    [XmlIgnore]
    public int num_sw_ratio
        {
        get { return y_sw_fac_root.Length; }
        }
    [XmlIgnore]
    public int num_oxdef_photo
        {
        get { return oxdef_photo.Length; }
        }
    [XmlIgnore]
    public int num_afps
        {
        get { return y_afps_fac.Length; }
        }
    [XmlIgnore]
    public int num_cane_dmf
        {
        get { return cane_dmf_tt.Length; }
        }
    [XmlIgnore]
    public int num_stress_lodge
        {
        get { return death_fr_lodge.Length; }
        }



    }

