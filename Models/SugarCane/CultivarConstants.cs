using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

[Serializable]
public class CultivarConstants
    {


    const int max_table = 10;   //! Maximum size_of of tables
    const int max_leaf = 200;   //! maximum number of plant leaves

    //*     ===========================================================
    //      subroutine sugar_read_cultivar_params (section_name)
    //*     ===========================================================

    //[Param]
    public string cultivar_name { get; set; }

    //!    sugar_leaf_size
    //[Param(MinVal = 1000.0, MaxVal = 100000.0)]
    public double[] leaf_size { get; set; }

    //[Param(MinVal = 0.0, MaxVal = max_leaf)]
    public double[] leaf_size_no { get; set; }



    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double cane_fraction { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] sucrose_fraction_stalk { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 1.0)]
    public double[] stress_factor_stalk { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 2000.0)]
    public double sucrose_delay { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 5000.0)]
    [Units("g/m2")]
    public double min_sstem_sucrose { get; set; }


    //[Param(MinVal = 0.0, MaxVal = 5000.0)]
    [Units("g/m2")]
    public double min_sstem_sucrose_redn { get; set; }

    //TODO: Either find a way to do this or remove 'tt_emerg_to_begcane_ub' from the ini file.
    ////[Param(MinVal = 0.0, MaxVal = tt_emerg_to_begcane_ub)]
    //[Param(MinVal = 0.0, MaxVal = 2000.0)]
    public double tt_emerg_to_begcane { get; set; }


    //TODO: Either find a way to do this or remove 'tt_begcane_to_flowering_ub' from the ini file.
    ////[Param(MinVal = 0.0, MaxVal = tt_begcane_to_flowering_ub)]
    //[Param(MinVal = 0.0, MaxVal = 10000.0)]
    public double tt_begcane_to_flowering { get; set; }


    //TODO: Either find a way to do this or remove 'tt_flowering_to_crop_end_ub' from the ini file.
    ////[Param(MinVal = 0.0, MaxVal = tt_flowering_to_crop_end_ub)]
    //[Param(MinVal = 0.0, MaxVal = 5000.0)]
    public double tt_flowering_to_crop_end { get; set; }



    //!    sugar_leaf_death

    //[Param(MinVal = 0.0, MaxVal = max_leaf)]
    public double green_leaf_no { get; set; }


    //!    sugar_leaf_size

    //[Param(MinVal = 0.0, MaxVal = 100.0)]
    public double[] tillerf_leaf_size { get; set; }

    //[Param(MinVal = 0.0, MaxVal = max_leaf)]
    public double[] tillerf_leaf_size_no { get; set; }




    //Sizes of arrays read in from the INI file (Actually the SIM file).
    //-----------------------------------------

    [XmlIgnore]
    public int num_leaf_size
        {
        get { return leaf_size_no.Length; }
        }
    [XmlIgnore]
    public int num_stress_factor_stalk
        {
        get { return stress_factor_stalk.Length; }
        }
    [XmlIgnore]
    public int num_tillerf_leaf_size
        {
        get { return tillerf_leaf_size_no.Length; }
        }



    }
 
