using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using Models.Core;

namespace Models.SurfaceOM
{

    public partial class SurfaceOrganicMatter
    {
        #region BensParams

        public ResidueTypes ResidueTypes { get; set;}
        public TillageTypes TillageTypes { get; set; }

        [Summary]
        [Description("Pool name")]
        [Units("")]
        public string PoolName { get; set; }

        [Summary]
        [Description("Pool type")]
        [Units("")]
        public string type { get; set; }

        [Summary]
        [Description("Mass")]
        [Units("kg/ha")]
        public string mass { get; set; }

        [Summary]
        [Description("Standing fraction")]
        [Units("0-1")]
        public string standing_fraction { get; set; }

        [Summary]
        [Description("CPR")]
        [Units("")]
        public string cpr { get; set; }

        [Summary]
        [Description("CNR")]
        [Units("")]
        public string cnr { get; set; }

        #endregion

        #region Params

        /// <summary>
        /// critical residue weight below which Thorburn"s cover factor equals one
        /// </summary>
        [Units("")]
        public double crit_residue_wt = 2000;

        /// <summary>
        /// temperature at which decomp reaches optimum (oC)
        /// </summary>
        [Units("")]
        public double opt_temp = 20;

        /// <summary>
        /// cumeos at which decomp rate becomes zero. (mm H2O)
        /// </summary>
        [Units("")]
        public double cum_eos_max = 20;

        /// <summary>
        /// coeff for rate of change in decomp with C:N
        /// </summary>
        [Units("")]
        public double cnrf_coeff = 0.277;

        /// <summary>
        /// C:N above which decomp is slowed
        /// </summary>

        [Units("")]
        public double cnrf_optcn = 25;

        /// <summary>
        /// 
        /// </summary>

        [Units("")]
        public double c_fract = 0.4;

        /// <summary>
        /// total amount of "leaching" rain to remove all soluble N from surfom
        /// </summary>

        [Units("")]
        public double leach_rain_tot = 25;

        /// <summary>
        /// threshold rainfall amount for leaching to occur
        /// </summary>

        [Units("")]
        public double min_rain_to_leach = 10;

        /// <summary>
        /// critical minimum org C below which potential decomposition rate is 100% (to avoid numerical imprecision)
        /// </summary>

        [Units("")]
        public double crit_min_surfom_orgC = 0.004;

        /// <summary>
        /// Default C:P ratio
        /// </summary>

        [Units("")]
        public double default_cpr = 0.0;

        /// <summary>
        /// Default fraction of residue isolated from soil (standing up)
        /// </summary>

        [Units("")]
        public double default_standing_fraction = 0.0;

        /// <summary>
        /// extinction coefficient for standing residues
        /// </summary>

        [Units("")]
        public double standing_extinct_coeff = 0.5;

        /// <summary>
        /// fraction of incoming faeces to add
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 0.0)]
        [Units("0-1")]
        public double fractionFaecesAdded = 0.5;

        private int[] cf_contrib = new int[max_residues];            //determinant of whether a residue type contributes to the calculation of contact factor (1 or 0)

        private double[] C_fract = new double[max_residues];	        //Fraction of Carbon in plant material (0-1)

        private double[,] fr_pool_C = new double[MaxFr, max_residues];	//carbohydrate fraction in fom C pool (0-1)
        private double[,] fr_pool_N = new double[MaxFr, max_residues];	//carbohydrate fraction in fom N pool (0-1)
        private double[,] fr_pool_P = new double[MaxFr, max_residues];	//carbohydrate fraction in fom P pool (0-1)

        private double[] nh4ppm = new double[max_residues];	        //ammonium component of residue (ppm)
        private double[] no3ppm = new double[max_residues];	        //nitrate component of residue (ppm)
        private double[] po4ppm = new double[max_residues];	        //ammonium component of residue (ppm)
        private double[] specific_area = new double[max_residues];	    //specific area of residue (ha/kg)

        #endregion

        #region Inputs

        //[Units("mm")]
        //double eos = double.NaN;

        [Units("")]
        string pond_active = null;

        [Units("")]
        double[] labile_p = null;

        /*
        [Input(true)]
        [Units("")]
        double f_incorp { get { return f_incorp_val; } set { f_incorp_val = value; f_incorp_written = true; } }
        double f_incorp_val = double.NaN;

        [Input(true)]
        [Units("")]
        double tillage_depth { get { return tillage_depth_val; } set { tillage_depth_val = value; tillage_depth_written = true; } }
        double tillage_depth_val = double.NaN;
        */


        //[Input()]
        //[Units("")]
        //double Crit_residue_wt;

        #endregion

        #region Outputs

        ///<summary>
        ///Total mass of all surface organic materials
        ///</summary>

        [Units("kg/ha")]
        public double surfaceom_wt { get { return SumSurfOMStandingLying(g.SurfOM, x => x.amount); } }

        [Units("kg/ha")]
        public double carbonbalance { get { return 0 - (surfaceom_c - g.DailyInitialC); } }


        [Units("kg/ha")]
        public double nitrogenbalance { get { return 0 - (surfaceom_n - g.DailyInitialN); } }

        ///<summary>
        ///Total mass of all surface organic carbon
        ///</summary>
        [Summary]
        [Description("Carbon content")]
        [Units("kg/ha")]
        public double surfaceom_c { get { return SumSurfOMStandingLying(g.SurfOM, x => x.C); } }

        ///<summary>
        ///Total mass of all surface organic nitrogen
        ///</summary>
        [Summary]
        [Description("Nitrogen content")]
        [Units("kg/ha")]
        public double surfaceom_n { get { return SumSurfOMStandingLying(g.SurfOM, x => x.N); } }

        ///<summary>
        ///Total mass of all surface organic phosphor
        ///</summary>

        [Summary]
        [Description("Phosphorus content")]
        [Units("kg/ha")]
        public double surfaceom_p { get { return SumSurfOMStandingLying(g.SurfOM, x => x.P); } }


        [Units("")]
        public double surfaceom_ashalk { get { return SumSurfOMStandingLying(g.SurfOM, x => x.P); } }

        ///<summary>
        ///Total mass of nitrate
        ///</summary>

        [Units("kg/ha")]
        public double surfaceom_no3 { get { return SumSurfOM(g.SurfOM, x => x.no3); } }

        ///<summary>
        ///Total mass of ammonium
        ///</summary>

        [Units("kg/ha")]
        public double surfaceom_nh4 { get { return SumSurfOM(g.SurfOM, x => x.nh4); } }


        ///<summary>
        ///Total mass of labile phosphorus
        ///</summary>

        [Units("kg/ha")]
        public double surfaceom_labile_p { get { return SumSurfOM(g.SurfOM, x => x.po4); } }

        ///<summary>
        ///Fraction of ground covered by all surface OMs
        ///</summary>

        [Description("Fraction of ground covered by all surface OMs")]
        [Units("m^2/m^2")]
        public double surfaceom_cover { get { return surfom_cover_total(); } }

        ///<summary>
        ///Temperature factor for decomposition
        ///</summary>

        [Units("0-1")]
        public double tf { get { return surfom_tf(); } }

        ///<summary>
        ///Contact factor for decomposition
        ///</summary>

        [Units("0-1")]
        public double cf { get { return surfom_cf(); } }


        [Units("0-1")]
        public double wf { get { return surfom_wf(); } }


        [Units("")]
        public double leaching_fr { get { return g.leaching_fr; } }


        [Units("")]
        public int surface_organic_matter { get { respond2get_SurfaceOrganicMatter(); return 0; } }

        ///<summary>
        ///Mass of organic matter named wheat
        ///</summary>

        [Units("")]
        public double surfaceom_wt_rice { get { return surfaceom_wt_("rice", x => x.amount); } }

        ///<summary>
        ///Mass of organic matter named algae
        ///</summary>

        [Units("")]
        public double surfaceom_wt_algae { get { return surfaceom_wt_("algae", x => x.amount); } }
       
        /// <summary>
        /// Get the weight of the given SOM pool
        /// </summary>
        /// <param name="pool">Name of the pool to get the weight from.</param>
        /// <returns>The weight of the given pool</returns>
        public double GetWeightFromPool(string pool)
        {
             var SomType = g.SurfOM.Find(x => x.name.Equals(pool, StringComparison.CurrentCultureIgnoreCase));
             return SumOMFractionType(SomType.Standing, y => y.amount) +
                 SumOMFractionType(SomType.Lying, y => y.amount);
        }

        ///<summary>
        ///Mass of organic matter in all pools
        ///</summary>
        [Units("kg/ha")]
        public double[] surfaceom_wt_all
        {
            get
            {
                return g.SurfOM.Select<SurfOrganicMatterType, double>
                    (
                    x =>
                        SumOMFractionType(x.Standing, y => y.amount) +
                        SumOMFractionType(x.Lying, y => y.amount)
                    ).ToArray<double>();
            }
        }

        ///<summary>
        ///Mass of organic carbon in all pools
        ///</summary>

        [Units("kg/ha")]
        public double[] surfaceom_c_all
        {
            get
            {
                return g.SurfOM.Select<SurfOrganicMatterType, double>
                    (
                    x =>
                        SumOMFractionType(x.Standing, y => y.C) +
                        SumOMFractionType(x.Lying, y => y.C)
                    ).ToArray<double>();
            }
        }

        ///<summary>
        ///Mass of organic nitrogen in all pools
        ///</summary>

        [Units("kg/ha")]
        public double[] surfaceom_n_all
        {
            get
            {
                return g.SurfOM.Select<SurfOrganicMatterType, double>
                    (
                    x =>
                        SumOMFractionType(x.Standing, y => y.N) +
                        SumOMFractionType(x.Lying, y => y.N)
                    ).ToArray<double>();
            }
        }

        ///<summary>
        ///Mass of organic phosphor in all pools
        ///</summary>

        [Units("kg/ha")]
        public double[] surfaceom_p_all
        {
            get
            {
                return g.SurfOM.Select<SurfOrganicMatterType, double>
                    (
                    x =>
                        SumOMFractionType(x.Standing, y => y.P) +
                        SumOMFractionType(x.Lying, y => y.P)
                    ).ToArray<double>();
            }
        }


        [Units("")]
        public double[] surfaceom_ashalk_all
        {
            get
            {
                return g.SurfOM.Select<SurfOrganicMatterType, double>
                    (
                    x =>
                        SumOMFractionType(x.Standing, y => y.AshAlk) +
                        SumOMFractionType(x.Lying, y => y.AshAlk)
                    ).ToArray<double>();
            }
        }

        ///<summary>
        ///Mass of nitrate in all pools
        ///</summary>

        [Units("")]
        public double[] surfaceom_no3_all
        {
            get
            {
                return g.SurfOM.Select<SurfOrganicMatterType, double>(x => x.no3).ToArray<double>();
            }
        }

        ///<summary>
        ///Mass of ammonium in all pools
        ///</summary>

        [Units("")]
        public double[] surfaceom_nh4_all
        {
            get
            {
                return g.SurfOM.Select<SurfOrganicMatterType, double>(x => x.nh4).ToArray<double>();
            }
        }

        ///<summary>
        ///Mass of labile phosphorus in all pools
        ///</summary>

        [Units("")]
        public double[] surfaceom_labile_p_all
        {
            get
            {
                return g.SurfOM.Select<SurfOrganicMatterType, double>(x => x.nh4).ToArray<double>();
            }
        }



        ///<summary>
        ///Potential organic C decomposition in all pools
        ///</summary>

        [Units("")]
        public double[] pot_c_decomp_all
        {
            get
            {
                double[] c, n, p;
                surfom_Pot_Decomp(out c, out n, out p);

                return c;
            }
        }

        ///<summary>
        ///Potential organic N decomposition in all pools
        ///</summary>

        [Units("")]
        public double[] pot_n_decomp_all
        {
            get
            {
                double[] c, n, p;
                surfom_Pot_Decomp(out c, out n, out p);

                return n;
            }
        }

        ///<summary>
        ///Potential organic P decomposition in all pools
        ///</summary>

        [Units("")]
        public double[] pot_p_decomp_all
        {
            get
            {
                double[] c, n, p;
                surfom_Pot_Decomp(out c, out n, out p);

                return p;
            }
        }

        ///<summary>
        ///Fraction of all pools which is inert, ie not in contact with the ground
        ///</summary>

        [Units("")]
        public double[] standing_fr_all
        {
            get
            {
                return g.SurfOM.Select<SurfOrganicMatterType, double>(x => divide(x.Standing[0].amount, x.Standing[0].amount + x.Lying[0].amount, 0)).ToArray<double>();
            }
        }

        ///<summary>
        ///Fraction of ground covered by all pools
        ///</summary>

        [Units("m^2/m^2")]
        public double[] surfaceom_cover_all
        {
            get
            {
                double[] result = new double[g.SurfOM.Count];
                for (int i = 0; i < result.Length; i++)
                    result[i] = surfom_cover(i);

                return result;
            }
        }

        ///<summary>
        ///C:N ratio factor for decomposition for all pools
        ///</summary>

        [Units("")]
        public double[] cnrf_all
        {
            get
            {
                double[] result = new double[g.SurfOM.Count];
                for (int i = 0; i < result.Length; i++)
                    result[i] = surfom_cnrf(i);

                return result;
            }
        }

        //
        //[Units("")]
        //public double[] dlt_no3;// { get; private set; }

        //
        //[Units("")]
        //public double[] dlt_nh4;// { get; private set; }

        //
        //[Units("")]
        //public double[] dlt_labile_p;// { get; private set; }


        #endregion
    }
}