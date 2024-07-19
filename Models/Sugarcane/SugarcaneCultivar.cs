using System;
using Models.Core;
using Newtonsoft.Json;

/// <summary>
/// Sugarcane Cultivar class.
/// </summary>
[Serializable]
public class SugarcaneCultivar
{


    /// <summary>
    /// The max_table
    /// </summary>
    const int max_table = 10;   //! Maximum size_of of tables
    /// <summary>
    /// The max_leaf
    /// </summary>
    const int max_leaf = 200;   //! maximum number of plant leaves

    //*     ===========================================================
    //      subroutine sugar_read_cultivar_params (section_name)
    //*     ===========================================================

    //[Param]
    /// <summary>
    /// Gets or sets the cultivar_name.
    /// </summary>
    /// <value>
    /// The cultivar_name.
    /// </value>
    public string cultivar_name { get; set; }

    //!    sugar_leaf_size
    //[Param(MinVal = 1000.0, MaxVal = 100000.0)]
    /// <summary>
    /// Gets or sets the leaf_size.
    /// </summary>
    /// <value>
    /// The leaf_size.
    /// </value>
    public double[] leaf_size { get; set; }

    //[Param(MinVal = 0.0, MaxVal = max_leaf)]
    /// <summary>
    /// Gets or sets the leaf_size_no.
    /// </summary>
    /// <value>
    /// The leaf_size_no.
    /// </value>
    public double[] leaf_size_no { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the cane_fraction.
    /// </summary>
    /// <value>
    /// The cane_fraction.
    /// </value>
    public double cane_fraction { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the sucrose_fraction_stalk.
    /// </summary>
    /// <value>
    /// The sucrose_fraction_stalk.
    /// </value>
    public double[] sucrose_fraction_stalk { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    /// <summary>
    /// Gets or sets the stress_factor_stalk.
    /// </summary>
    /// <value>
    /// The stress_factor_stalk.
    /// </value>
    public double[] stress_factor_stalk { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 2000.0)]
    /// <summary>
    /// Gets or sets the sucrose_delay.
    /// </summary>
    /// <value>
    /// The sucrose_delay.
    /// </value>
    public double sucrose_delay { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 5000.0)]
    /// <summary>
    /// Gets or sets the min_sstem_sucrose.
    /// </summary>
    /// <value>
    /// The min_sstem_sucrose.
    /// </value>
    [Units("g/m2")]
    public double min_sstem_sucrose { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 5000.0)]
    /// <summary>
    /// Gets or sets the min_sstem_sucrose_redn.
    /// </summary>
    /// <value>
    /// The min_sstem_sucrose_redn.
    /// </value>
    [Units("g/m2")]
    public double min_sstem_sucrose_redn { get; set; }

    //TODO: Either find a way to do this or remove 'tt_emerg_to_begcane_ub' from the ini file.
    ////[Param(MinVal = 0.0, MaxVal = tt_emerg_to_begcane_ub)]
    //[Param(MinVal = 0.0, MaxVal = 2000.0)]
    /// <summary>
    /// Gets or sets the tt_emerg_to_begcane.
    /// </summary>
    /// <value>
    /// The tt_emerg_to_begcane.
    /// </value>
    public double tt_emerg_to_begcane { get; set; }


    //TODO: Either find a way to do this or remove 'tt_begcane_to_flowering_ub' from the ini file.
    ////[Param(MinVal = 0.0, MaxVal = tt_begcane_to_flowering_ub)]
    //[Param(MinVal = 0.0, MaxVal = 10000.0)]
    /// <summary>
    /// Gets or sets the tt_begcane_to_flowering.
    /// </summary>
    /// <value>
    /// The tt_begcane_to_flowering.
    /// </value>
    public double tt_begcane_to_flowering { get; set; }


    //TODO: Either find a way to do this or remove 'tt_flowering_to_crop_end_ub' from the ini file.
    ////[Param(MinVal = 0.0, MaxVal = tt_flowering_to_crop_end_ub)]
    //[Param(MinVal = 0.0, MaxVal = 5000.0)]
    /// <summary>
    /// Gets or sets the tt_flowering_to_crop_end.
    /// </summary>
    /// <value>
    /// The tt_flowering_to_crop_end.
    /// </value>
    public double tt_flowering_to_crop_end { get; set; }



    //!    sugar_leaf_death

    //[Param(MinVal = 0.0, MaxVal = max_leaf)]
    /// <summary>
    /// Gets or sets the green_leaf_no.
    /// </summary>
    /// <value>
    /// The green_leaf_no.
    /// </value>
    public double green_leaf_no { get; set; }


    //!    sugar_leaf_size

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    /// <summary>
    /// Gets or sets the tillerf_leaf_size.
    /// </summary>
    /// <value>
    /// The tillerf_leaf_size.
    /// </value>
    public double[] tillerf_leaf_size { get; set; }

    //[Param(MinVal = 0.0, MaxVal = max_leaf)]
    /// <summary>
    /// Gets or sets the tillerf_leaf_size_no.
    /// </summary>
    /// <value>
    /// The tillerf_leaf_size_no.
    /// </value>
    public double[] tillerf_leaf_size_no { get; set; }


    /// <summary>
    /// Constructor. Sets the vaules to default so they can be overwritten by the cultivar.
    /// Defaults are based off Q117.
    /// </summary>
    public SugarcaneCultivar(bool ratoon) {
        leaf_size = new double[] {1500,55000,55000};
        leaf_size_no = new double[] {1,14,20};
        cane_fraction = 0.7;
        sucrose_fraction_stalk = new double[] {1,0.55};
        stress_factor_stalk = new double[] {0.2,1};
        sucrose_delay = 0;
        min_sstem_sucrose = 800;
        min_sstem_sucrose_redn = 10;
        tt_emerg_to_begcane = 1900;
        tt_begcane_to_flowering = 6000;
        tt_flowering_to_crop_end = 2000;
        green_leaf_no = 13;
        if (ratoon) { //Q117_ratoon
            tillerf_leaf_size = new double[] {1.5,1.5,1.5,1,1};
            tillerf_leaf_size_no = new double[] {1,4,10,12,26};
        } else {      //Q117
            tillerf_leaf_size = new double[] {1,1.5,1.5,1,1};
            tillerf_leaf_size_no = new double[] {1,6,10,12,26};
        }
    }

    //Sizes of arrays read in from the INI file (Actually the SIM file).
    //-----------------------------------------

    /// <summary>
    /// Gets the num_leaf_size.
    /// </summary>
    /// <value>
    /// The num_leaf_size.
    /// </value>
    [JsonIgnore]
    public int num_leaf_size
    {
        get { return leaf_size_no.Length; }
    }
    /// <summary>
    /// Gets the num_stress_factor_stalk.
    /// </summary>
    /// <value>
    /// The num_stress_factor_stalk.
    /// </value>
    [JsonIgnore]
    public int num_stress_factor_stalk
    {
        get { return stress_factor_stalk.Length; }
    }
    /// <summary>
    /// Gets the num_tillerf_leaf_size.
    /// </summary>
    /// <value>
    /// The num_tillerf_leaf_size.
    /// </value>
    [JsonIgnore]
    public int num_tillerf_leaf_size
    {
        get { return tillerf_leaf_size_no.Length; }
    }



}

