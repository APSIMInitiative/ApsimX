using System;
using Models.Core;
using Newtonsoft.Json;

/// <summary>
/// Sugar constants
/// </summary>
[Serializable]
public class SugarcaneSpeciesConstants
{

    /// <summary>
    /// The max_table
    /// </summary>
    const int max_table = 10;   //! Maximum size_of of tables
    /// <summary>
    /// The max_stage
    /// </summary>
    const int max_stage = 6;    //! number of growth stages
    /// <summary>
    /// The max_part
    /// </summary>
    const int max_part = 5;     //! number of plant parts


    //*     ===========================================================
    //      subroutine sugar_read_crop_constants (section_name)
    //*     ===========================================================



    //!    sugar_get_cultivar_params

    //! full names of stages for reporting
    //[Param]
    /// <summary>
    /// Gets or sets the stage_names.
    /// </summary>
    /// <value>
    /// The stage_names.
    /// </value>
    public string[] stage_names { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1000.0, Name = "stage_code")]
    /// <summary>
    /// Gets or sets the stage_code_list.
    /// </summary>
    /// <value>
    /// The stage_code_list.
    /// </value>
    public double[] stage_code_list { get; set; }

    //! radiation use efficiency (g dm/mj)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the rue.
    /// </summary>
    /// <value>
    /// The rue.
    /// </value>
    [Units("g dm/mj")]
    public double[] rue { get; set; }

    //! root growth rate potential (mm depth/day)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the root_depth_rate.
    /// </summary>
    /// <value>
    /// The root_depth_rate.
    /// </value>
    [Units("mm")]
    public double[] root_depth_rate { get; set; }

    //! root:shoot ratio of new dm ()
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the ratio_root_shoot.
    /// </summary>
    /// <value>
    /// The ratio_root_shoot.
    /// </value>
    public double[] ratio_root_shoot { get; set; }

    //! transpiration efficiency coefficient to convert vpd to transpiration efficiency (kpa)
    //! although this is expressed as a pressure it is really in the form kpa*g carbo per m^2 / g water per m^2
    //! and this can be converted to kpa*g carbo per m^2 / mm water
    //! because 1g water = 1 cm^3 water
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the transp_eff_cf.
    /// </summary>
    /// <value>
    /// The transp_eff_cf.
    /// </value>
    public double[] transp_eff_cf { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the n_fix_rate.
    /// </summary>
    /// <value>
    /// The n_fix_rate.
    /// </value>
    public double[] n_fix_rate { get; set; }

    //! radiation extinction coefficient ()
    //[Param(MinVal = 0.0, MaxVal = 10.0)]
    /// <summary>
    /// Gets or sets the extinction_coef.
    /// </summary>
    /// <value>
    /// The extinction_coef.
    /// </value>
    public double extinction_coef { get; set; }

    //! radiation extinction coefficient () of dead leaves
    //[Param(MinVal = 0.0, MaxVal = 10.0)]
    /// <summary>
    /// Gets or sets the extinction_coef_dead.
    /// </summary>
    /// <value>
    /// The extinction_coef_dead.
    /// </value>
    public double extinction_coef_dead { get; set; }



    //! crop failure

    //! critical number of leaves below which portion of the crop maydie due to water stress
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the leaf_no_crit.
    /// </summary>
    /// <value>
    /// The leaf_no_crit.
    /// </value>
    public double leaf_no_crit { get; set; }

    //! maximum degree days allowed foremergence to take place (deg day)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the tt_emerg_limit.
    /// </summary>
    /// <value>
    /// The tt_emerg_limit.
    /// </value>
    [Units("oC")]
    public double tt_emerg_limit { get; set; }

    //! maximum days allowed after sowing for germination to take place (days)
    //[Param(MinVal = 0.0, MaxVal = 365.0)]
    /// <summary>
    /// Gets or sets the days_germ_limit.
    /// </summary>
    /// <value>
    /// The days_germ_limit.
    /// </value>
    [Units("days")]
    public double days_germ_limit { get; set; }

    //! critical cumulative phenology water stress above which the cropfails (unitless)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the swdf_pheno_limit.
    /// </summary>
    /// <value>
    /// The swdf_pheno_limit.
    /// </value>
    public double swdf_pheno_limit { get; set; }

    //! critical cumulative photosynthesis water stress above which the crop partly fails (unitless)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the swdf_photo_limit.
    /// </summary>
    /// <value>
    /// The swdf_photo_limit.
    /// </value>
    public double swdf_photo_limit { get; set; }

    //! rate of plant reduction with photosynthesis water stress
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the swdf_photo_rate.
    /// </summary>
    /// <value>
    /// The swdf_photo_rate.
    /// </value>
    public double swdf_photo_rate { get; set; }




    //!    sugar_root_depth

    //! initial depth of roots (mm)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the initial_root_depth.
    /// </summary>
    /// <value>
    /// The initial_root_depth.
    /// </value>
    [Units("mm")]
    public double initial_root_depth { get; set; }

    //! length of root per unit wt (mm/g)
    //[Param(MinVal = 0.0, MaxVal = 50000.0)]
    /// <summary>
    /// Gets or sets the specific_root_length.
    /// </summary>
    /// <value>
    /// The specific_root_length.
    /// </value>
    [Units("mm")]
    public double specific_root_length { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 0.1)]
    /// <summary>
    /// Gets or sets the x_plant_rld.
    /// </summary>
    /// <value>
    /// The x_plant_rld.
    /// </value>
    [Units("mm/mm3/plant")]
    public double[] x_plant_rld { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the y_rel_root_rate.
    /// </summary>
    /// <value>
    /// The y_rel_root_rate.
    /// </value>
    [Units("0-1")]
    public double[] y_rel_root_rate { get; set; }



    //! fraction of roots that dies back at harvest (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the root_die_back_fr.
    /// </summary>
    /// <value>
    /// The root_die_back_fr.
    /// </value>
    [Units("0-1")]
    public double root_die_back_fr { get; set; }



    //!    sugar_leaf_area_init

    //! initial plant leaf area (mm^2)
    //[Param(MinVal = 0.0, MaxVal = 100000.0)]
    /// <summary>
    /// Gets or sets the initial_tpla.
    /// </summary>
    /// <value>
    /// The initial_tpla.
    /// </value>
    [Units("mm^2")]
    public double initial_tpla { get; set; }



    //!    sugar_leaf_area_devel

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the sla_lfno.
    /// </summary>
    /// <value>
    /// The sla_lfno.
    /// </value>
    public double[] sla_lfno { get; set; }


    //! maximum specific leaf area for new leaf area (mm^2/g)
    //[Param(MinVal = 0.0, MaxVal = 50000.0)]
    /// <summary>
    /// Gets or sets the sla_max.
    /// </summary>
    /// <value>
    /// The sla_max.
    /// </value>
    [Units("mm^2/g")]
    public double[] sla_max { get; set; }

    //! minimum specific leaf area for new leaf area (mm^2/g)
    //[Param(MinVal = 0.0, MaxVal = 50000.0)]
    /// <summary>
    /// Gets or sets the sla_min.
    /// </summary>
    /// <value>
    /// The sla_min.
    /// </value>
    [Units("mm^2/g")]
    public double[] sla_min { get; set; }



    //!    sugar_height

    //[Param(MinVal = 0.0, MaxVal = 10000.0)]
    /// <summary>
    /// Gets or sets the x_stem_wt.
    /// </summary>
    /// <value>
    /// The x_stem_wt.
    /// </value>
    [Units("g/plant")]
    public double[] x_stem_wt { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 10000.0)]
    /// <summary>
    /// Gets or sets the y_height.
    /// </summary>
    /// <value>
    /// The y_height.
    /// </value>
    [Units("mm")]
    public double[] y_height { get; set; }




    //!    sugar_transp_eff

    //! fraction of distance between svp at min temp and svp at max temp where average svp during transpiration lies. (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the svp_fract.
    /// </summary>
    /// <value>
    /// The svp_fract.
    /// </value>
    public double svp_fract { get; set; }



    //!    cproc_sw_demand_bound

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the eo_crop_factor_default.
    /// </summary>
    /// <value>
    /// The eo_crop_factor_default.
    /// </value>
    public double eo_crop_factor_default { get; set; }



    //!    sugar_germination

    //! plant extractable soil water in seedling layer inadequate for germination (mm/mm)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the pesw_germ.
    /// </summary>
    /// <value>
    /// The pesw_germ.
    /// </value>
    [Units("mm/mm")]
    public double pesw_germ { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the fasw_emerg.
    /// </summary>
    /// <value>
    /// The fasw_emerg.
    /// </value>
    [Units("0-1")]
    public double[] fasw_emerg { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the rel_emerg_rate.
    /// </summary>
    /// <value>
    /// The rel_emerg_rate.
    /// </value>
    [Units("0-1")]
    public double[] rel_emerg_rate { get; set; }




    //!    sugar_leaf_appearance

    //! leaf number at emergence ()
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the leaf_no_at_emerg.
    /// </summary>
    /// <value>
    /// The leaf_no_at_emerg.
    /// </value>
    public double leaf_no_at_emerg { get; set; }



    //!    sugar_phenology_init

    //! minimum growing degree days for germination (deg days)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the shoot_lag.
    /// </summary>
    /// <value>
    /// The shoot_lag.
    /// </value>
    [Units("oC")]
    public double shoot_lag { get; set; }

    //! growing deg day increase with depth for germination (deg day/mm depth)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the shoot_rate.
    /// </summary>
    /// <value>
    /// The shoot_rate.
    /// </value>
    [Units("oC/mm")]
    public double shoot_rate { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the x_node_no_app.
    /// </summary>
    /// <value>
    /// The x_node_no_app.
    /// </value>
    [Units("oC")]
    public double[] x_node_no_app { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the y_node_app_rate.
    /// </summary>
    /// <value>
    /// The y_node_app_rate.
    /// </value>
    [Units("oC")]
    public double[] y_node_app_rate { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the x_node_no_leaf.
    /// </summary>
    /// <value>
    /// The x_node_no_leaf.
    /// </value>
    [Units("oC")]
    public double[] x_node_no_leaf { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the y_leaves_per_node.
    /// </summary>
    /// <value>
    /// The y_leaves_per_node.
    /// </value>
    [Units("oC")]
    public double[] y_leaves_per_node { get; set; }




    //!    sugar_dm_init

    //! root growth before emergence (g/plant)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the dm_root_init.
    /// </summary>
    /// <value>
    /// The dm_root_init.
    /// </value>
    [Units("g/plant")]
    public double dm_root_init { get; set; }

    //! stem growth before emergence (g/plant)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the dm_sstem_init.
    /// </summary>
    /// <value>
    /// The dm_sstem_init.
    /// </value>
    [Units("g/plant")]
    public double dm_sstem_init { get; set; }

    //! leaf growth before emergence (g/plant)
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the dm_leaf_init.
    /// </summary>
    /// <value>
    /// The dm_leaf_init.
    /// </value>
    [Units("g/plant")]
    public double dm_leaf_init { get; set; }

    //! cabbage "    "        "        "
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the dm_cabbage_init.
    /// </summary>
    /// <value>
    /// The dm_cabbage_init.
    /// </value>
    [Units("g/plant")]
    public double dm_cabbage_init { get; set; }

    //! sucrose "    "        "        "
    //[Param(MinVal = 0.0, MaxVal = 1000.0)]
    /// <summary>
    /// Gets or sets the dm_sucrose_init.
    /// </summary>
    /// <value>
    /// The dm_sucrose_init.
    /// </value>
    [Units("g/plant")]
    public double dm_sucrose_init { get; set; }

    //! ratio of leaf wt to cabbage wt ()
    //[Param(MinVal = 0.0, MaxVal = 10.0)]
    /// <summary>
    /// Gets or sets the leaf_cabbage_ratio.
    /// </summary>
    /// <value>
    /// The leaf_cabbage_ratio.
    /// </value>
    public double leaf_cabbage_ratio { get; set; }

    //! fraction of cabbage that is leaf sheath(0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the cabbage_sheath_fr.
    /// </summary>
    /// <value>
    /// The cabbage_sheath_fr.
    /// </value>
    public double cabbage_sheath_fr { get; set; }



    //!    sugar_dm_senescence

    //! fraction of root dry matter senescing each day (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the dm_root_sen_frac.
    /// </summary>
    /// <value>
    /// The dm_root_sen_frac.
    /// </value>
    public double dm_root_sen_frac { get; set; }



    //!    sugar_dm_dead_detachment

    //! fraction of dead plant parts detaching each day (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the dead_detach_frac.
    /// </summary>
    /// <value>
    /// The dead_detach_frac.
    /// </value>
    public double[] dead_detach_frac { get; set; }

    //! fraction of senesced dry matter detaching from live plant each day (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the sen_detach_frac.
    /// </summary>
    /// <value>
    /// The sen_detach_frac.
    /// </value>
    public double[] sen_detach_frac { get; set; }



    //!    sugar_leaf_area_devel

    //! corrects for other growing leaves
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the leaf_no_correction.
    /// </summary>
    /// <value>
    /// The leaf_no_correction.
    /// </value>
    public double leaf_no_correction { get; set; }



    //!    sugar_leaf_area_sen_light

    //! critical lai above which light
    //[Param(MinVal = 0.0, MaxVal = 10.0)]
    /// <summary>
    /// Gets or sets the lai_sen_light.
    /// </summary>
    /// <value>
    /// The lai_sen_light.
    /// </value>
    [Units("m^2/m^2")]
    public double lai_sen_light { get; set; }

    //! slope of linear relationship between lai and light competition factor for determining leaf senesence rate.
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the sen_light_slope.
    /// </summary>
    /// <value>
    /// The sen_light_slope.
    /// </value>
    public double sen_light_slope { get; set; }



    //!    sugar_leaf_area_sen_frost

    //[Param(MinVal = -20.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the frost_temp.
    /// </summary>
    /// <value>
    /// The frost_temp.
    /// </value>
    [Units("oC")]
    public double[] frost_temp { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the frost_fraction.
    /// </summary>
    /// <value>
    /// The frost_fraction.
    /// </value>
    [Units("oC")]
    public double[] frost_fraction { get; set; }



    //!    sugar_leaf_area_sen_water

    //! slope in linear eqn relating soil water stress during photosynthesis to leaf senesense rate
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the sen_rate_water.
    /// </summary>
    /// <value>
    /// The sen_rate_water.
    /// </value>
    public double sen_rate_water { get; set; }



    //!    sugar_phenology_init

    //! twilight in angular distance between sunset and end of twilight - altitude of sun. (deg)
    //[Param(MinVal = -90.0, MaxVal = 90.0)]
    /// <summary>
    /// Gets or sets the twilight.
    /// </summary>
    /// <value>
    /// The twilight.
    /// </value>
    [Units("o")]
    public double twilight { get; set; }



    //!    sugar_N_conc_limits

    //! stage table for N concentrations(g N/g biomass)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the x_stage_code.
    /// </summary>
    /// <value>
    /// The x_stage_code.
    /// </value>
    public double[] x_stage_code { get; set; } //all 6 of the following y values use this same "x_stage_code" for their x values.

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// critical N concentration of leaf (g N/g biomass)
    /// </summary>
    /// <value>
    /// The y_n_conc_crit_leaf.
    /// </value>
    public double[] y_n_conc_crit_leaf { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// minimum N concentration of leaf (g N/g biomass)
    /// </summary>
    /// <value>
    /// The y_n_conc_min_leaf.
    /// </value>
    public double[] y_n_conc_min_leaf { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// critical N concentration of stem (g N/g biomass)
    /// </summary>
    /// <value>
    /// The y_n_conc_crit_cane.
    /// </value>
    public double[] y_n_conc_crit_cane { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// minimum N concentration of stem (g N/g biomass)
    /// </summary>
    /// <value>
    /// The y_n_conc_min_cane.
    /// </value>
    public double[] y_n_conc_min_cane { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// critical N concentration of flower(g N/g biomass)
    /// </summary>
    /// <value>
    /// The y_n_conc_crit_cabbage.
    /// </value>
    public double[] y_n_conc_crit_cabbage { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// minimum N concentration of flower (g N/g biomass)
    /// </summary>
    /// <value>
    /// The y_n_conc_min_cabbage.
    /// </value>
    public double[] y_n_conc_min_cabbage { get; set; }



    //! critical N concentration of root (g N/g biomass)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the n_conc_crit_root.
    /// </summary>
    /// <value>
    /// The n_conc_crit_root.
    /// </value>
    public double n_conc_crit_root { get; set; }

    //! minimum N concentration of root (g N/g biomass)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the n_conc_min_root.
    /// </summary>
    /// <value>
    /// The n_conc_min_root.
    /// </value>
    public double n_conc_min_root { get; set; }



    //!    sugar_N_init

    //! initial root N concentration (gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the n_root_init_conc.
    /// </summary>
    /// <value>
    /// The n_root_init_conc.
    /// </value>
    public double n_root_init_conc { get; set; }

    //! initial stem N concentration (gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the n_sstem_init_conc.
    /// </summary>
    /// <value>
    /// The n_sstem_init_conc.
    /// </value>
    public double n_sstem_init_conc { get; set; }

    //! initial leaf N concentration (gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the n_leaf_init_conc.
    /// </summary>
    /// <value>
    /// The n_leaf_init_conc.
    /// </value>
    public double n_leaf_init_conc { get; set; }

    //!    "   cabbage    "            "
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the n_cabbage_init_conc.
    /// </summary>
    /// <value>
    /// The n_cabbage_init_conc.
    /// </value>
    public double n_cabbage_init_conc { get; set; }



    //!    sugar_N_senescence

    //! N concentration of senesced root (gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the n_root_sen_conc.
    /// </summary>
    /// <value>
    /// The n_root_sen_conc.
    /// </value>
    public double n_root_sen_conc { get; set; }

    //! N concentration of senesced leaf (gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the n_leaf_sen_conc.
    /// </summary>
    /// <value>
    /// The n_leaf_sen_conc.
    /// </value>
    public double n_leaf_sen_conc { get; set; }

    //! N concentration of senesced cabbage(gN/gdm)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the n_cabbage_sen_conc.
    /// </summary>
    /// <value>
    /// The n_cabbage_sen_conc.
    /// </value>
    public double n_cabbage_sen_conc { get; set; }



    //!    sugar_rue_reduction

    //! critical temperatures for photosynthesis (oC)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the x_ave_temp.
    /// </summary>
    /// <value>
    /// The x_ave_temp.
    /// </value>
    [Units("oC")]
    public double[] x_ave_temp { get; set; }

    //! Factors for critical temperatures (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the y_stress_photo.
    /// </summary>
    /// <value>
    /// The y_stress_photo.
    /// </value>
    public double[] y_stress_photo { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the x_ave_temp_stalk.
    /// </summary>
    /// <value>
    /// The x_ave_temp_stalk.
    /// </value>
    [Units("oC")]
    public double[] x_ave_temp_stalk { get; set; }

    //! Factors for critical temperatures (0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the y_stress_stalk.
    /// </summary>
    /// <value>
    /// The y_stress_stalk.
    /// </value>
    public double[] y_stress_stalk { get; set; }





    //!    sugar_tt

    //! temperature table for photosynthesis (degree days)
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the x_temp.
    /// </summary>
    /// <value>
    /// The x_temp.
    /// </value>
    [Units("oC")]
    public double[] x_temp { get; set; }

    //! degree days
    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the y_tt.
    /// </summary>
    /// <value>
    /// The y_tt.
    /// </value>
    [Units("oC")]
    public double[] y_tt { get; set; }




    //!    sugar_swdef

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the x_sw_demand_ratio.
    /// </summary>
    /// <value>
    /// The x_sw_demand_ratio.
    /// </value>
    public double[] x_sw_demand_ratio { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the y_swdef_leaf.
    /// </summary>
    /// <value>
    /// The y_swdef_leaf.
    /// </value>
    public double[] y_swdef_leaf { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the x_demand_ratio_stalk.
    /// </summary>
    /// <value>
    /// The x_demand_ratio_stalk.
    /// </value>
    public double[] x_demand_ratio_stalk { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the y_swdef_stalk.
    /// </summary>
    /// <value>
    /// The y_swdef_stalk.
    /// </value>
    public double[] y_swdef_stalk { get; set; }




    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the x_sw_avail_ratio.
    /// </summary>
    /// <value>
    /// The x_sw_avail_ratio.
    /// </value>
    public double[] x_sw_avail_ratio { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the y_swdef_pheno.
    /// </summary>
    /// <value>
    /// The y_swdef_pheno.
    /// </value>
    public double[] y_swdef_pheno { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the x_sw_ratio.
    /// </summary>
    /// <value>
    /// The x_sw_ratio.
    /// </value>
    public double[] x_sw_ratio { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the y_sw_fac_root.
    /// </summary>
    /// <value>
    /// The y_sw_fac_root.
    /// </value>
    public double[] y_sw_fac_root { get; set; }




    //! Nitrogen Stress Factors
    //! -----------------------

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the k_nfact_photo.
    /// </summary>
    /// <value>
    /// The k_nfact_photo.
    /// </value>
    public double k_nfact_photo { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the k_nfact_expansion.
    /// </summary>
    /// <value>
    /// The k_nfact_expansion.
    /// </value>
    public double k_nfact_expansion { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the k_nfact_stalk.
    /// </summary>
    /// <value>
    /// The k_nfact_stalk.
    /// </value>
    public double k_nfact_stalk { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the k_nfact_pheno.
    /// </summary>
    /// <value>
    /// The k_nfact_pheno.
    /// </value>
    public double k_nfact_pheno { get; set; }



    //! Water logging functions
    //! -----------------------

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the oxdef_photo_rtfr.
    /// </summary>
    /// <value>
    /// The oxdef_photo_rtfr.
    /// </value>
    public double[] oxdef_photo_rtfr { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the oxdef_photo.
    /// </summary>
    /// <value>
    /// The oxdef_photo.
    /// </value>
    public double[] oxdef_photo { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 0.20)]
    /// <summary>
    /// Gets or sets the x_afps.
    /// </summary>
    /// <value>
    /// The x_afps.
    /// </value>
    public double[] x_afps { get; set; }

    //! lookup factors used for reducing effective root length due to reduced air filled pore space.
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the y_afps_fac.
    /// </summary>
    /// <value>
    /// The y_afps_fac.
    /// </value>
    public double[] y_afps_fac { get; set; }




    //! Plant Water Content function (dry matter function)
    //! ----------------------------

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the cane_dmf_max.
    /// </summary>
    /// <value>
    /// The cane_dmf_max.
    /// </value>
    public double[] cane_dmf_max { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the cane_dmf_min.
    /// </summary>
    /// <value>
    /// The cane_dmf_min.
    /// </value>
    public double[] cane_dmf_min { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 10000.0)]
    /// <summary>
    /// Gets or sets the cane_dmf_tt.
    /// </summary>
    /// <value>
    /// The cane_dmf_tt.
    /// </value>
    public double[] cane_dmf_tt { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the cane_dmf_rate.
    /// </summary>
    /// <value>
    /// The cane_dmf_rate.
    /// </value>
    public double cane_dmf_rate { get; set; }



    //! Death by Lodging Constants
    //! --------------------------

    //!(0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the stress_lodge.
    /// </summary>
    /// <value>
    /// The stress_lodge.
    /// </value>
    [Units("0-1")]
    public double[] stress_lodge { get; set; }

    //!(0-1)
    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the death_fr_lodge.
    /// </summary>
    /// <value>
    /// The death_fr_lodge.
    /// </value>
    [Units("0-1")]
    public double[] death_fr_lodge { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the lodge_redn_photo.
    /// </summary>
    /// <value>
    /// The lodge_redn_photo.
    /// </value>
    public double lodge_redn_photo { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the lodge_redn_sucrose.
    /// </summary>
    /// <value>
    /// The lodge_redn_sucrose.
    /// </value>
    public double lodge_redn_sucrose { get; set; }

    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the lodge_redn_green_leaf.
    /// </summary>
    /// <value>
    /// The lodge_redn_green_leaf.
    /// </value>
    public double lodge_redn_green_leaf { get; set; }





    //Sizes of arrays read in from the INI file (Actually the SIM file).
    //-----------------------------------------

    /// <summary>
    /// Gets the num_plant_rld.
    /// </summary>
    /// <value>
    /// The num_plant_rld.
    /// </value>
    [JsonIgnore]
    public int num_plant_rld
    {
        get { return y_rel_root_rate.Length; }
    }
    /// <summary>
    /// Gets the num_sla_lfno.
    /// </summary>
    /// <value>
    /// The num_sla_lfno.
    /// </value>
    [JsonIgnore]
    public int num_sla_lfno
    {
        get { return sla_lfno.Length; }
    }
    /// <summary>
    /// Gets the num_stem_wt.
    /// </summary>
    /// <value>
    /// The num_stem_wt.
    /// </value>
    [JsonIgnore]
    public int num_stem_wt
    {
        get { return y_height.Length; }
    }
    /// <summary>
    /// Gets the num_fasw_emerg.
    /// </summary>
    /// <value>
    /// The num_fasw_emerg.
    /// </value>
    [JsonIgnore]
    public int num_fasw_emerg
    {
        get { return rel_emerg_rate.Length; }
    }
    /// <summary>
    /// Gets the num_node_no_app.
    /// </summary>
    /// <value>
    /// The num_node_no_app.
    /// </value>
    [JsonIgnore]
    public int num_node_no_app
    {
        get { return y_node_app_rate.Length; }
    }
    /// <summary>
    /// Gets the num_node_no_leaf.
    /// </summary>
    /// <value>
    /// The num_node_no_leaf.
    /// </value>
    [JsonIgnore]
    public int num_node_no_leaf
    {
        get { return y_node_app_rate.Length; }
    }
    /// <summary>
    /// Gets the num_frost_temp.
    /// </summary>
    /// <value>
    /// The num_frost_temp.
    /// </value>
    [JsonIgnore]
    public int num_frost_temp
    {
        get { return frost_temp.Length; }
    }
    /// <summary>
    /// Gets the num_ n_conc_stage.
    /// </summary>
    /// <value>
    /// The num_ n_conc_stage.
    /// </value>
    [JsonIgnore]
    public int num_N_conc_stage   //! no of values in stage table
    {
        get { return y_n_conc_min_cabbage.Length; }
    }
    /// <summary>
    /// Gets the num_ave_temp.
    /// </summary>
    /// <value>
    /// The num_ave_temp.
    /// </value>
    [JsonIgnore]
    public int num_ave_temp       //! size_of of critical temperature table
    {
        get { return x_ave_temp.Length; }
    }
    /// <summary>
    /// Gets the num_ave_temp_stalk.
    /// </summary>
    /// <value>
    /// The num_ave_temp_stalk.
    /// </value>
    [JsonIgnore]
    public int num_ave_temp_stalk //! size_of of critical temperature table
    {
        get { return x_ave_temp_stalk.Length; }
    }
    /// <summary>
    /// Gets the num_temp.
    /// </summary>
    /// <value>
    /// The num_temp.
    /// </value>
    [JsonIgnore]
    public int num_temp           //! size_of of table
    {
        get { return y_tt.Length; }
    }
    /// <summary>
    /// Gets the num_sw_demand_ratio.
    /// </summary>
    /// <value>
    /// The num_sw_demand_ratio.
    /// </value>
    [JsonIgnore]
    public int num_sw_demand_ratio
    {
        get { return y_swdef_leaf.Length; }
    }
    /// <summary>
    /// Gets the num_demand_ratio_stalk.
    /// </summary>
    /// <value>
    /// The num_demand_ratio_stalk.
    /// </value>
    [JsonIgnore]
    public int num_demand_ratio_stalk
    {
        get { return y_swdef_stalk.Length; }
    }
    /// <summary>
    /// Gets the num_sw_avail_ratio.
    /// </summary>
    /// <value>
    /// The num_sw_avail_ratio.
    /// </value>
    [JsonIgnore]
    public int num_sw_avail_ratio
    {
        get { return y_swdef_pheno.Length; }
    }
    /// <summary>
    /// Gets the num_sw_ratio.
    /// </summary>
    /// <value>
    /// The num_sw_ratio.
    /// </value>
    [JsonIgnore]
    public int num_sw_ratio
    {
        get { return y_sw_fac_root.Length; }
    }
    /// <summary>
    /// Gets the num_oxdef_photo.
    /// </summary>
    /// <value>
    /// The num_oxdef_photo.
    /// </value>
    [JsonIgnore]
    public int num_oxdef_photo
    {
        get { return oxdef_photo.Length; }
    }
    /// <summary>
    /// Gets the num_afps.
    /// </summary>
    /// <value>
    /// The num_afps.
    /// </value>
    [JsonIgnore]
    public int num_afps
    {
        get { return y_afps_fac.Length; }
    }
    /// <summary>
    /// Gets the num_cane_dmf.
    /// </summary>
    /// <value>
    /// The num_cane_dmf.
    /// </value>
    [JsonIgnore]
    public int num_cane_dmf
    {
        get { return cane_dmf_tt.Length; }
    }
    /// <summary>
    /// Gets the num_stress_lodge.
    /// </summary>
    /// <value>
    /// The num_stress_lodge.
    /// </value>
    [JsonIgnore]
    public int num_stress_lodge
    {
        get { return death_fr_lodge.Length; }
    }



}

