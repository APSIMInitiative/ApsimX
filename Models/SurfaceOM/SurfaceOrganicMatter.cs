using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Models.Core;
using Models.PMF;
using Models.Soils;

namespace Models.SurfaceOM
{
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SurfaceOrganicMatter : Model
    {
        #region links to other components
        [Link]
        private Soil soil = null;

        [Link]
        private ISummary summary = null;

        [Link]
        private WeatherFile weather = null;
        #endregion

        //====================================================================
        //    SurfaceOM constants;
        //====================================================================

        static int max_residues = 100;                          //maximum number of residues in at once;
        static int MaxFr = 3;
        //    ================================================================

        //====================================================================

        // dsg 190803  The following two "types" have been defined within the code because,
        //             dean"s datatypes.f90 generator cannot yet properly generate the;
        //             type definition from datatypes.interface for a structures within a;
        //             structure.
        [Serializable]
        public class OMFractionType
        {
            public double amount;
            public double C;
            public double N;
            public double P;
            public double AshAlk;

            public OMFractionType()
            {
                amount = 0;
                C = 0;
                N = 0;
                P = 0;
                AshAlk = 0;
            }
        }
        [Serializable]
        class SurfOrganicMatterType
        {
            public string name;
            public string OrganicMatterType;
            public double PotDecompRate;
            public double no3;
            public double nh4;
            public double po4;
            public OMFractionType[] Standing;
            public OMFractionType[] Lying;

            public SurfOrganicMatterType()
            {
                name = null;
                OrganicMatterType = null;
                PotDecompRate = 0;
                no3 = 0; nh4 = 0; po4 = 0;
                Standing = new OMFractionType[MaxFr];
                Lying = new OMFractionType[MaxFr];

                for (int i = 0; i < MaxFr; i++)
                {
                    Lying[i] = new OMFractionType();
                    Standing[i] = new OMFractionType();
                }
            }

            public SurfOrganicMatterType(string name, string type)
                : this()
            {
                this.name = name;
                OrganicMatterType = type;
            }

        }

        private List<SurfOrganicMatterType> SurfOM;
        private Models.WeatherFile.NewMetType MetData;

        //TODO - Tidy this god-awful mess of fixed array sizes up, preferably use dictionary for holding SOMTypes
        private int num_surfom
        {
            get
            {
                return SurfOM == null ? 0 : SurfOM.Count;
            }
        }

        private double irrig;
        private double eos;
        private double cumeos;
        private double[] dlayer;
        private bool phosphorus_aware;
        private OMFractionType oldSOMState;
        private double DailyInitialC;
        private double DailyInitialN;


        public double[] _standing_fraction;	                //standing fraction array;
        public string report_additions { get; set; }
        public string report_removals { get; set; }

        [Serializable]
        private class SurfaceOMConstants
        {
            public int[] cf_contrib = new int[max_residues];  //determinant of whether a residue type;
            //contributes to the calculation of contact factor (1 or 0)
            public double[] C_fract = new double[max_residues];	//Fraction of Carbon in plant;
            //material (0-1)

            public double[,] fr_pool_C = new double[MaxFr, max_residues];	//carbohydrate fraction in fom C pool (0-1)
            public double[,] fr_pool_N = new double[MaxFr, max_residues];	//carbohydrate fraction in fom N pool (0-1)
            public double[,] fr_pool_P = new double[MaxFr, max_residues];	//carbohydrate fraction in fom P pool (0-1)

            public double[] nh4ppm = new double[max_residues];	//ammonium component of residue (ppm)
            public double[] no3ppm = new double[max_residues];	//nitrate component of residue (ppm)
            public double[] po4ppm = new double[max_residues];	//ammonium component of residue (ppm)
            public double[] specific_area = new double[max_residues];	//specific area of residue (ha/kg)


            public SurfaceOMConstants()
            {
                cf_contrib = new int[max_residues];

                C_fract = new double[max_residues];

                fr_pool_C = new double[MaxFr, max_residues];
                fr_pool_N = new double[MaxFr, max_residues];
                fr_pool_P = new double[MaxFr, max_residues];

                nh4ppm = new double[max_residues];
                no3ppm = new double[max_residues];
                po4ppm = new double[max_residues];
                specific_area = new double[max_residues];
            }
        }

        //instance variables.
        SurfaceOMConstants c;

        public SurfaceOrganicMatter()
            : base()
        {
        }

        #region exposed properties
        #region BensParams

        public ResidueTypes residueTypes { get; set; }
        public TillageTypes tillageTypes { get; set; }

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

        private double _crit_residue_wt = 2000;
        /// <summary>
        /// critical residue weight below which Thorburn"s cover factor equals one
        /// </summary>
        [Units("")]
        public double crit_residue_wt
        {
            get
            {
                return _crit_residue_wt;
            }
            set
            {
                _crit_residue_wt = value;
            }
        }

        private double _opt_temp = 20;
        /// <summary>
        /// temperature at which decomp reaches optimum (oC)
        /// </summary>
        [Units("")]
        public double opt_temp
        {
            get
            {
                return _opt_temp;
            }
            set
            {
                value = _opt_temp;
            }
        }

        private double _cum_eos_max = 20;
        /// <summary>
        /// cumeos at which decomp rate becomes zero. (mm H2O)
        /// </summary>
        [Units("")]
        public double cum_eos_max
        {
            get
            {
                return _cum_eos_max;
            }
            set
            {
                _cum_eos_max = value;
            }
        }

        private double _cnrf_coeff = 0.277;
        /// <summary>
        /// coeff for rate of change in decomp with C:N
        /// </summary>
        [Units("")]
        public double cnrf_coeff
        {
            get
            {
                return _cnrf_coeff;
            }
            set
            {
                _cnrf_coeff = value;
            }
        }

        private double _cnrf_optcn = 25;
        /// <summary>
        /// C:N above which decomp is slowed
        /// </summary>
        [Units("")]
        public double cnrf_optcn
        {
            get
            {
                return _cnrf_optcn;
            }
            set
            {
                _cnrf_optcn = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>

        [Units("")]
        public double c_fract = 0.4;

        private double _leach_rain_tot = 25;
        /// <summary>
        /// total amount of "leaching" rain to remove all soluble N from surfom
        /// </summary>
        [Units("")]
        public double leach_rain_tot
        {
            get
            {
                return _leach_rain_tot;
            }
            set
            {
                _leach_rain_tot = value;
            }
        }

        private double _min_rain_to_leach = 10;
        /// <summary>
        /// threshold rainfall amount for leaching to occur
        /// </summary>
        [Units("")]
        public double min_rain_to_leach
        {
            get
            {
                return _min_rain_to_leach;
            }
            set
            {
                _min_rain_to_leach = value;
            }
        }

        private double _crit_min_surfom_orgC = 0.004;
        /// <summary>
        /// critical minimum org C below which potential decomposition rate is 100% (to avoid numerical imprecision)
        /// </summary>
        [Units("")]
        public double crit_min_surfom_orgC
        {
            get
            {
                return _crit_min_surfom_orgC;
            }
            set
            {
                _crit_min_surfom_orgC = value;
            }
        }

        private double _default_cpr = 0.0;
        /// <summary>
        /// Default C:P ratio
        /// </summary>
        [Units("")]
        public double default_cpr
        {
            get
            {
                return _default_cpr;
            }
            set
            {
                _default_cpr = value;
            }
        }

        private double _standing_extinct_coeff = 0.5;
        /// <summary>
        /// extinction coefficient for standing residues
        /// </summary>
        [Units("")]
        public double standing_extinct_coeff
        {
            get
            {
                return _standing_extinct_coeff;
            }
            set
            {
                _standing_extinct_coeff = value;
            }
        }

        private double _fractionFaecesAdded = 0.5;
        /// <summary>
        /// fraction of incoming faeces to add
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 0.0)]
        [Units("0-1")]
        public double fractionFaecesAdded
        {
            get
            {
                return _fractionFaecesAdded;
            }
            set
            {
                _fractionFaecesAdded = value;

            }
        }

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
        #endregion

        #region Outputs

        ///<summary>
        ///Total mass of all surface organic materials
        ///</summary>

        [Units("kg/ha")]
        public double surfaceom_wt { get { return SumSurfOMStandingLying(SurfOM, x => x.amount); } }

        [Units("kg/ha")]
        public double carbonbalance { get { return 0 - (surfaceom_c - DailyInitialC); } }


        [Units("kg/ha")]
        public double nitrogenbalance { get { return 0 - (surfaceom_n - DailyInitialN); } }

        ///<summary>
        ///Total mass of all surface organic carbon
        ///</summary>
        [Summary]
        [Description("Carbon content")]
        [Units("kg/ha")]
        public double surfaceom_c { get { return SumSurfOMStandingLying(SurfOM, x => x.C); } }

        ///<summary>
        ///Total mass of all surface organic nitrogen
        ///</summary>
        [Summary]
        [Description("Nitrogen content")]
        [Units("kg/ha")]
        public double surfaceom_n { get { return SumSurfOMStandingLying(SurfOM, x => x.N); } }

        ///<summary>
        ///Total mass of all surface organic phosphor
        ///</summary>

        [Summary]
        [Description("Phosphorus content")]
        [Units("kg/ha")]
        public double surfaceom_p { get { return SumSurfOMStandingLying(SurfOM, x => x.P); } }


        [Units("")]
        public double surfaceom_ashalk { get { return SumSurfOMStandingLying(SurfOM, x => x.P); } }

        ///<summary>
        ///Total mass of nitrate
        ///</summary>

        [Units("kg/ha")]
        public double surfaceom_no3 { get { return SumSurfOM(SurfOM, x => x.no3); } }

        ///<summary>
        ///Total mass of ammonium
        ///</summary>

        [Units("kg/ha")]
        public double surfaceom_nh4 { get { return SumSurfOM(SurfOM, x => x.nh4); } }


        ///<summary>
        ///Total mass of labile phosphorus
        ///</summary>

        [Units("kg/ha")]
        public double surfaceom_labile_p { get { return SumSurfOM(SurfOM, x => x.po4); } }

        ///<summary>
        ///Fraction of ground covered by all surface OMs
        ///</summary>

        [Description("Fraction of ground covered by all surface OMs")]
        [Units("m^2/m^2")]
        public double surfaceom_cover { get { return CoverTotal(); } }

        ///<summary>
        ///Temperature factor for decomposition
        ///</summary>

        [Units("0-1")]
        public double tf { get { return TemperatureFactor(); } }

        ///<summary>
        ///Contact factor for decomposition
        ///</summary>

        [Units("0-1")]
        public double cf { get { return ContactFactor(); } }


        [Units("0-1")]
        public double wf { get { return MoistureFactor(); } }

        private double _leaching_fr;
        [Units("")]
        public double leaching_fr { get { return _leaching_fr; } }

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
            var SomType = SurfOM.Find(x => x.name.Equals(pool, StringComparison.CurrentCultureIgnoreCase));
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
                return SurfOM.Select<SurfOrganicMatterType, double>
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
                return SurfOM.Select<SurfOrganicMatterType, double>
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
                return SurfOM.Select<SurfOrganicMatterType, double>
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
                return SurfOM.Select<SurfOrganicMatterType, double>
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
                return SurfOM.Select<SurfOrganicMatterType, double>
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
                return SurfOM.Select<SurfOrganicMatterType, double>(x => x.no3).ToArray<double>();
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
                return SurfOM.Select<SurfOrganicMatterType, double>(x => x.nh4).ToArray<double>();
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
                return SurfOM.Select<SurfOrganicMatterType, double>(x => x.nh4).ToArray<double>();
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
                PotDecomp(out c, out n, out p);

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
                PotDecomp(out c, out n, out p);

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
                PotDecomp(out c, out n, out p);

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
                return SurfOM.Select<SurfOrganicMatterType, double>(x => Utility.Math.Divide(x.Standing[0].amount, x.Standing[0].amount + x.Lying[0].amount, 0)).ToArray<double>();
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
                double[] result = new double[SurfOM.Count];
                for (int i = 0; i < result.Length; i++)
                    result[i] = Cover(i);

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
                double[] result = new double[SurfOM.Count];
                for (int i = 0; i < result.Length; i++)
                    result[i] = CNRatioFactor(i);

                return result;
            }
        }

        #endregion

        #endregion

        #region supporting classes
        [Serializable]
        public class ResidueTypes
        {
            [XmlElement("ResidueType")]
            [XmlIgnore]
            public List<ResidueType> residues { get; set; }

            public ResidueTypes()
            {
                if (residues == null)
                    residues = new List<ResidueType>();

                residues.Clear();
                residues.Add(new ResidueType("wheat") { specific_area = 0.0005 });
                residues.Add(new ResidueType("lucerne") { specific_area = 0.0002 });
                residues.Add(new ResidueType("barley") { specific_area = 0.0005 });
                residues.Add(new ResidueType("tithonia") { specific_area = 0.0005 });
                residues.Add(new ResidueType("bambatsi") { specific_area = 0.0005 });
                residues.Add(new ResidueType("barley") { specific_area = 0.0005 });
                residues.Add(new ResidueType("broccoli") { specific_area = 0.0004 });
                residues.Add(new ResidueType("butterflypea") { specific_area = 0.0004 });
                residues.Add(new ResidueType("camaldulensis") { specific_area = 0.0002 });
                residues.Add(new ResidueType("canola") { specific_area = 0.0002 });
                residues.Add(new ResidueType("centro") { specific_area = 0.0004 });
                residues.Add(new ResidueType("chickpea") { specific_area = 0.0002 });
                residues.Add(new ResidueType("cowpea") { specific_area = 0.0002 });
                residues.Add(new ResidueType("danthonia") { specific_area = 0.0005 });
                residues.Add(new ResidueType("nativepasture") { specific_area = 0.0005 });
                residues.Add(new ResidueType("pasture") { specific_area = 0.0005 });
                residues.Add(new ResidueType("globulus") { specific_area = 0.0002 });
                residues.Add(new ResidueType("grandis") { specific_area = 0.0002 });
                residues.Add(new ResidueType("oilmallee") { specific_area = 0.0002 });
                residues.Add(new ResidueType("fababean") { specific_area = 0.0002 });
                residues.Add(new ResidueType("fieldpea") { specific_area = 0.0002 });
                residues.Add(new ResidueType("grass") { specific_area = 0.0004 });
                residues.Add(new ResidueType("lablab") { specific_area = 0.0002 });
                residues.Add(new ResidueType("lentil") { specific_area = 0.0002 });
                residues.Add(new ResidueType("lolium_rigidum") { specific_area = 0.0002 });
                residues.Add(new ResidueType("lucerne") { specific_area = 0.0002 });
                residues.Add(new ResidueType("lupin") { specific_area = 0.0002 });
                residues.Add(new ResidueType("maize") { specific_area = 0.0004 });
                residues.Add(new ResidueType("medic") { specific_area = 0.0002 });
                residues.Add(new ResidueType("millet") { specific_area = 0.0004 });
                residues.Add(new ResidueType("mucuna") { specific_area = 0.0002 });
                residues.Add(new ResidueType("mungbean") { specific_area = 0.0002 });
                residues.Add(new ResidueType("horsegram") { specific_area = 0.0002 });
                residues.Add(new ResidueType("navybean") { specific_area = 0.0002 });
                residues.Add(new ResidueType("frenchbean") { specific_area = 0.0002 });
                residues.Add(new ResidueType("cotton") { specific_area = 0.0002 });
                residues.Add(new ResidueType("oats") { specific_area = 0.0005 });
                residues.Add(new ResidueType("oilpalmunderstory") { specific_area = 0.0002 });
                residues.Add(new ResidueType("orobanche") { specific_area = 0.0002 });
                residues.Add(new ResidueType("peanut") { specific_area = 0.0002 });
                residues.Add(new ResidueType("pigeonpea") { specific_area = 0.0002 });
                residues.Add(new ResidueType("poppies") { specific_area = 0.0005 });
                residues.Add(new ResidueType("potato") { specific_area = 0.0005 });
                residues.Add(new ResidueType("raphanus_raphanistrum") { specific_area = 0.0002 });
                residues.Add(new ResidueType("rice") { specific_area = 0.0005 });
                residues.Add(new ResidueType("soybean") { specific_area = 0.0002 });
                residues.Add(new ResidueType("sorghum") { specific_area = 0.0004 });
                residues.Add(new ResidueType("stylo") { specific_area = 0.0002 });
                residues.Add(new ResidueType("sugar") { specific_area = 0.0007 });
                residues.Add(new ResidueType("sunflower") { specific_area = 0.0002 });
                residues.Add(new ResidueType("sweetcorn") { specific_area = 0.0004 });
                residues.Add(new ResidueType("sweetsorghum") { specific_area = 0.0004 });
                residues.Add(new ResidueType("vetch") { specific_area = 0.0002 });
                residues.Add(new ResidueType("weed") { specific_area = 0.0004 });
                residues.Add(new ResidueType("WF_Millet") { specific_area = 0.0004 });
                residues.Add(new ResidueType("wheat") { specific_area = 0.0005 });
                residues.Add(new ResidueType("inert") { pot_decomp_rate = 0.0 });
                residues.Add(new ResidueType("slurp") { specific_area = 0.0005 });
                residues.Add(new ResidueType("manure")
                {
                    fraction_C = 0.08,
                    pot_decomp_rate = 0.1,
                    fr_c = new double[] { 0.3, 0.3, 0.4 },
                    fr_n = new double[] { 0.3, 0.3, 0.4 },
                    fr_p = new double[] { 0.3, 0.3, 0.4 },
                    po4ppm = 10,
                    nh4ppm = 10,
                    no3ppm = 10,
                    specific_area = 0.0001
                });
                residues.Add(new ResidueType("RuminantDung_PastureFed")
                {
                    fraction_C = 0.4,
                    pot_decomp_rate = 0.1,
                    fr_c = new double[] { 0.3, 0.5, 0.2 },
                    fr_n = new double[] { 0.3, 0.5, 0.2 },
                    fr_p = new double[] { 0.3, 0.5, 0.2 },
                    po4ppm = 5,
                    nh4ppm = 1250,
                    no3ppm = 0,
                    specific_area = 0.0001
                });
                residues.Add(new ResidueType("algae")
                {
                    fraction_C = 0.4,
                    pot_decomp_rate = 0.1,
                    specific_area = 0.0005,
                    cf_contrib = 1
                });
                residues.Add(new ResidueType("fym")
                {
                    fraction_C = 0.8,
                    pot_decomp_rate = 0.1,
                    fr_c = new double[] { 0.3, 0.3, 0.4 },
                    fr_n = new double[] { 0.3, 0.3, 0.4 },
                    fr_p = new double[] { 0.3, 0.3, 0.4 },
                    po4ppm = 40,
                    nh4ppm = 10,
                    no3ppm = 10,
                    specific_area = 0.0001,
                    cf_contrib = 1
                });
                residues.Add(new ResidueType("goatmanure")
                {
                    fraction_C = 0.8,
                    pot_decomp_rate = 0.1,
                    fr_c = new double[] { 0.3, 0.6, 0.3 },
                    fr_n = new double[] { 0.3, 0.6, 0.3 },
                    fr_p = new double[] { 0.3, 0.6, 0.34 },
                    po4ppm = 5,
                    nh4ppm = 1307,
                    no3ppm = 481,
                    specific_area = 0.0001,
                    cf_contrib = 1
                });
                residues.Add(new ResidueType("cm")
                {
                    fraction_C = 0.277,
                    pot_decomp_rate = 0.1,
                    fr_c = new double[] { 0.0, 0.5, 0.5 },
                    fr_n = new double[] { 0.0, 0.5, 0.5 },
                    fr_p = new double[] { 0.0, 0.5, 0.5 },
                    po4ppm = 5,
                    nh4ppm = 2558,
                    no3ppm = 873,
                    specific_area = 0.0001,
                    cf_contrib = 1
                });
                residues.Add(new ResidueType("cmA")
                {
                    fraction_C = 0.374,
                    pot_decomp_rate = 0.1,
                    fr_c = new double[] { 0.0, 0.5, 0.5 },
                    fr_n = new double[] { 0.0, 0.5, 0.5 },
                    fr_p = new double[] { 0.0, 0.5, 0.5 },
                    po4ppm = 5,
                    nh4ppm = 1307,
                    no3ppm = 481,
                    specific_area = 0.0001,
                    cf_contrib = 1
                });
                residues.Add(new ResidueType("cmB")
                {
                    fraction_C = 0.24,
                    pot_decomp_rate = 0.1,
                    fr_c = new double[] { 0.0, 0.5, 0.5 },
                    fr_n = new double[] { 0.0, 0.5, 0.5 },
                    fr_p = new double[] { 0.0, 0.5, 0.5 },
                    po4ppm = 5,
                    nh4ppm = 3009,
                    no3ppm = 36,
                    specific_area = 0.0001,
                    cf_contrib = 1
                });
                residues.Add(new ResidueType("manB")
                {
                    fraction_C = 0.08,
                    pot_decomp_rate = 0.1,
                    fr_c = new double[] { 0.1, 0.01, 0.89 },
                    fr_n = new double[] { 0.1, 0.01, 0.89 },
                    fr_p = new double[] { 0.1, 0.01, 0.89 },
                    po4ppm = 5,
                    nh4ppm = 1307,
                    no3ppm = 481,
                    specific_area = 0.0001,
                    cf_contrib = 1
                });
                residues.Add(new ResidueType("oilpalm")
                {
                    fraction_C = 0.44,
                    pot_decomp_rate = 0.05,
                    specific_area = 0.0002,
                });
                residues.Add(new ResidueType("oilpalmstem")
                {
                    fraction_C = 0.44,
                    fr_c = new double[] { 0.2, 0.7, 0.1 },
                    fr_n = new double[] { 0.2, 0.7, 0.1 },
                    fr_p = new double[] { 0.2, 0.7, 0.1 },
                    specific_area = 0.000005,
                });
            }

            public ResidueType getResidue(string name)
            {
                if (residues != null)
                    foreach (ResidueType residueType in residues)
                    {
                        if (residueType.fom_type.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                            return residueType;
                    }

                throw new ApsimXException("SurfaceOrganicMatter", "Could not find residue name " + name);
            }
        }
        [Serializable]

        public class ResidueType : Model
        {
            public string fom_type { get; set; }
            public double fraction_C { get; set; }
            public double po4ppm { get; set; }
            public double nh4ppm { get; set; }
            public double no3ppm { get; set; }
            public double specific_area { get; set; }
            public int cf_contrib { get; set; }
            public double pot_decomp_rate { get; set; }
            public double[] fr_c { get; set; }
            public double[] fr_n { get; set; }
            public double[] fr_p { get; set; }

            public ResidueType()
            {
                fom_type = "inert";
                InitialiseWithDefaults();
            }

            public ResidueType(string fomType)
            {
                fom_type = fomType;
                InitialiseWithDefaults();
            }

            private void InitialiseWithDefaults()
            {
                fraction_C = 0.4;
                specific_area = 0.0005;
                cf_contrib = 1;
                pot_decomp_rate = 0.1;
                fr_c = new double[3] { 0.2, 0.7, 0.1 };
                fr_n = new double[3] { 0.2, 0.7, 0.1 };
                fr_p = new double[3] { 0.2, 0.7, 0.1 };
            }
        }

        [Serializable]
        public class TillageTypes : Model
        {
            public List<TillageType> TillageType { get; set; }

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

        const double acceptableErr = 1e-4;

        #region Math Operations

        int Bound(int tobound, int lower, int upper)
        {
            return Math.Max(Math.Min(tobound, upper), lower);
        }

        /// <summary>
        /// "cover1" and "cover2" are numbers between 0 and 1 which
        ///     indicate what fraction of sunlight is intercepted by the
        ///     foliage of plants.  This function returns a number between
        ///     0 and 1 indicating the fraction of sunlight intercepted
        ///     when "cover1" is combined with "cover2", i.e. both sets of
        ///     plants are present.
        /// </summary>
        /// <param name="cover1"></param>
        /// <param name="cover2"></param>
        /// <returns></returns>
        private double AddCover(double cover1, double cover2)
        {
            double bare = (1.0 - cover1) * (1.0 - cover2);
            return 1.0 - bare;
        }

        const string ApsimBoundWarningError =
    @"'{0}' out of bounds!
     {1} < {2} < {3} evaluates 'FALSE'";

        private void Bound_check_real_var(double value, double lower, double upper, string vname)
        {
            if (Utility.Math.IsLessThan(value, lower) || Utility.Math.IsGreaterThan(value, upper))
                summary.WriteWarning(FullPath, String.Format(ApsimBoundWarningError, vname, lower, value, upper));
        }

        private bool reals_are_equal(double first, double second)
        {
            return Math.Abs(first - second) < 2 * double.Epsilon;
        }

        /// <summary>
        /// <para>+ Purpose</para>
        /// <para>
        /// Find the first element of an array where a given value
        /// is contained with the cumulative sum_of of the elements.
        /// If sum_of is not reached by the end of the array, then it
        /// is ok to set it to the last element. This will take
        /// account of the case of the number of levels being 0.
        /// </para>
        /// <para>Definition</para>
        /// <para>
        /// Returns ndx where ndx is the smallest value in the range
        /// 0..array.Length such that the sum of "array"(j), j=0..ndx is
        /// greater than or equal to "cum_sum".  If there is no such
        /// value of ndx, then the index of the last element will be returned.
        /// <para>
        /// <para>Mission Statement</para>
        /// <para>
        /// Find index for cumulative %2 = %1
        /// </para>
        /// </summary>
        /// <param name="cum_sum">sum_of to be found</param>
        /// <param name="array">array to be searched</param>
        /// <returns>Index for a 0-based array</returns>
        private int GetCumulativeIndexReal(double cum_sum, double[] array)
        {
            int size_of = array.Length - 1;
            double cum = 0;
            for (int i = 0; i < size_of; i++)
                if ((cum += array[i]) >= cum_sum)
                    return i;
            return size_of;
        }

        #endregion

        T[] ToArray<T>(string str)
        {
            string[] temp;

            if (str == null || str == "")
                temp = new string[0];
            else
                temp = str.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

            MethodInfo parser = null;
            if (typeof(T) != typeof(string))
                parser = typeof(T).GetMethod("Parse", new Type[] { typeof(string) });

            T[] result = new T[temp.Length];

            for (int i = 0; i < result.Length; i++)
                result[i] = parser == null ? (T)(object)temp[i] : (T)parser.Invoke(null, new[] { temp[i] });

            return result;
        }

        double Sum2DArray(double[,] _2Darray)
        {
            double result = 0;
            foreach (double f in _2Darray)
            {
                result += f;
            }

            return result;
        }

        double SumSurfOMStandingLying(List<SurfOrganicMatterType> var, Func<OMFractionType, double> func)
        {
            return var.Sum<SurfOrganicMatterType>(x => SumSurfOMStandingLying(x, func));
        }

        double SumSurfOMStandingLying(SurfOrganicMatterType var, Func<OMFractionType, double> func)
        {
            return var.Lying.Sum<OMFractionType>(func) + var.Standing.Sum<OMFractionType>(func);
        }

        double SumSurfOM(List<SurfOrganicMatterType> var, Func<SurfOrganicMatterType, double> func)
        {
            return var.Sum<SurfOrganicMatterType>(func);
        }

        double SumOMFractionType(OMFractionType[] var, Func<OMFractionType, double> func)
        {
            return var.Sum<OMFractionType>(func);
        }

        double surfaceom_wt_(string type, Func<OMFractionType, double> func)
        {
            int SOMNo = GetSoluteNumber(type);
            if (SOMNo > 0)
                return SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying.Sum<OMFractionType>(func) + x.Standing.Sum<OMFractionType>(func));
            else
                throw new Exception("No organic matter called " + type + " present");
        }

        #endregion

        /// <summary>
        /// Initialise residue module
        /// </summary>
        private void SurfomReset()
        {
            //Save State;
            SaveState();
            ZeroVariables();
            ReadParam();

            //Change of State;
            DeltaState();
        }

        private void SaveState()
        {
            oldSOMState = SurfomTotalState();
        }

        private void DeltaState()
        {
            //  Local Variables;
            OMFractionType newSOMState = SurfomTotalState();
            Models.Soils.ExternalMassFlowType massBalanceChange = new Models.Soils.ExternalMassFlowType();

            //- Implementation Section ----------------------------------
            massBalanceChange.DM = newSOMState.amount - oldSOMState.amount;
            massBalanceChange.C = newSOMState.C - oldSOMState.C;
            massBalanceChange.N = newSOMState.N - oldSOMState.N;
            massBalanceChange.P = newSOMState.P - oldSOMState.P;
            massBalanceChange.SW = 0.0;

            if (massBalanceChange.DM >= 0.0)
            {
                massBalanceChange.FlowType = "gain";
            }
            else
            {
                massBalanceChange.FlowType = "loss";
            }

            surfom_ExternalMassFlow(massBalanceChange);

            return;
        }

        #region published events

        public event Models.Soils.SoilNitrogen.ExternalMassFlowDelegate ExternalMassFlow;
        private void PublishExternalMassFlow(ExternalMassFlowType massBalanceChange)
        {
            if (ExternalMassFlow != null)
                ExternalMassFlow.Invoke(massBalanceChange);
        }

        public delegate void SurfaceOrganicMatterDecompDelegate(SurfaceOrganicMatterDecompType Data);
        public event SurfaceOrganicMatterDecompDelegate PotentialResidueDecompositionCalculated;

        private void PublishSurfaceOrganicMatterDecomp(SurfaceOrganicMatterDecompType SOMDecomp)
        {
            if (PotentialResidueDecompositionCalculated != null)
                PotentialResidueDecompositionCalculated.Invoke(SOMDecomp);
        }

        public delegate void FOMPoolDelegate(FOMPoolType Data);
        public event FOMPoolDelegate IncorpFOMPool;
        private void publish_FOMPool(FOMPoolType data)
        {
            if (IncorpFOMPool != null)
                IncorpFOMPool.Invoke(data);
        }

        public class SurfaceOrganicMatterPoolType
        {
            public string Name = "";
            public string OrganicMatterType = "";
            public double PotDecompRate;
            public double no3;
            public double nh4;
            public double po4;
            public FOMType[] StandingFraction;
            public FOMType[] LyingFraction;
        }
        public class SurfaceOrganicMatterType
        {
            public SurfaceOrganicMatterPoolType[] Pool;
        }

        public delegate void SurfaceOrganicMatterDelegate(SurfaceOrganicMatterType Data);
        public event SurfaceOrganicMatterDelegate SurfaceOrganicMatterState;
        private void publish_SurfaceOrganicMatter(SurfaceOrganicMatterType SOM)
        {
            if (SurfaceOrganicMatterState != null)
                SurfaceOrganicMatterState.Invoke(SOM);
        }

        public class ResidueRemovedType
        {
            public string residue_removed_action = "";
            public double dlt_residue_fraction;
            public double[] residue_incorp_fraction;
        }

        public class ResidueAddedType
        {
            public string residue_type = "";
            public string dm_type = "";
            public double dlt_residue_wt;
            public double dlt_dm_n;
            public double dlt_dm_p;
        }
        public delegate void Residue_addedDelegate(ResidueAddedType Data);
        public event Residue_addedDelegate Residue_added;

        public delegate void Residue_removedDelegate(ResidueRemovedType Data);
        public event Residue_removedDelegate Residue_removed;

        public class SurfaceOMRemovedType
        {
            public string SurfaceOM_type = "";
            public string SurfaceOM_dm_type = "";
            public double dlt_SurfaceOM_wt;
            public double SurfaceOM_dlt_dm_n;
            public double SurfaceOM_dlt_dm_p;
        }

        public delegate void SurfaceOM_removedDelegate(SurfaceOMRemovedType Data);
        public event SurfaceOM_removedDelegate SurfaceOM_removed;

        public event NitrogenChangedDelegate NitrogenChanged;

        #endregion

        #region event handlers

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (initialised)
            {
                DailyInitialC = SumSurfOMStandingLying(SurfOM, x => x.C);
                DailyInitialN = SumSurfOMStandingLying(SurfOM, x => x.N);
            }
        }

        bool initialised = false;

        public void OnTillage(TillageType data)
        {
            Tillage(data);
        }

        public class Add_surfaceomType
        {
            public string name = "";
            public string type = "";
            public double mass;
            public double n;
            public double cnr;
            public double p;
            public double cpr;
        }
        public void Incorporate(double fraction, double depth)
        {
            TillageType data = new TillageType();
            data.f_incorp = fraction;
            data.tillage_depth = depth;
            data.Name = "User";
            Tillage(data);
        }

        public void Add(string type, double mass, double N, string name = null)
        {
            Add_surfaceomType data = new Add_surfaceomType();
            if (name == null)
                data.name = type;
            else
                data.name = name;
            data.type = type;
            data.mass = mass;
            data.n = N;


            AddSurfom(data);
        }

        public override void OnSimulationCommencing()
        {
            if (residueTypes == null)
                residueTypes = new ResidueTypes();
            c = new SurfaceOMConstants();
            SurfOM = null;
            MetData = new Models.WeatherFile.NewMetType();
            irrig = 0;
            eos = 0;
            cumeos = 0;
            dlayer = new double[0];
            _leaching_fr = 0;
            phosphorus_aware = false;
            pond_active = "no";
            oldSOMState = new OMFractionType();
            _standing_fraction = new double[max_residues];
            ZeroVariables();
            OnReset();
        }

        [EventSubscribe("Reset")]
        private void OnReset()
        {
            initialised = true; SurfomReset();
        }

        [EventSubscribe("RemoveSurfaceOM")]
        private void OnRemove_surfaceOM(SurfaceOrganicMatterType SOM)
        {
            RemoveSurfom(SOM);
        }

        [EventSubscribe("NewWeatherDataAvailable")]
        private void OnNewWeatherDataAvailable(object sender, EventArgs e)
        {
            MetData = weather.MetData;
        }

        [EventSubscribe("Irrigated")]
        /// <summary>
        /// Get irrigation information from an Irrigated event.
        /// </summary>
        private void OnIrrigated(object sender, IrrigationApplicationType data)
        {
            //now increment internal irrigation log;
            irrig += data.Amount;
        }

        public class CropChoppedType
        {
            public string crop_type = "";
            public string[] dm_type;
            public double[] dlt_crop_dm;
            public double[] dlt_dm_n;
            public double[] dlt_dm_p;
            public double[] fraction_to_residue;
        }


        [EventSubscribe("CropChopped")]
        /// <summary>
        /// Get information on surfom added from the crops
        /// </summary>
        private void OnCrop_chopped(CropChoppedType data)
        {
            double surfom_added = 0;	//amount of residue added (kg/ha)
            double surfom_N_added = 0;	//amount of residue N added (kg/ha)
            double surfom_P_added = 0;	//amount of residue N added (kg/ha)

            if (data.fraction_to_residue.Sum() != 0)
            {
                for (int i = 0; i < data.dlt_crop_dm.Length; i++)
                {
                    surfom_added += data.dlt_crop_dm[i] * data.fraction_to_residue[i];
                }

                if (surfom_added > 0.0)
                {
                    for (int i = 0; i < data.dlt_dm_n.Length; i++)
                    {
                        surfom_N_added += data.dlt_dm_n[i] * data.fraction_to_residue[i];
                    }

                    //Find the amount of P added in surfom today, if phosphorus aware;
                    if (phosphorus_aware)
                        for (int i = 0; i < data.dlt_dm_p.Length; i++)
                            surfom_P_added += data.dlt_dm_p[i] * data.fraction_to_residue[i];

                    AddSurfaceOM(surfom_added, surfom_N_added, surfom_P_added, data.crop_type);
                }
            }
        }

        [EventSubscribe("BiomassRemoved")]
        private void OnBiomassRemoved(BiomassRemovedType BiomassRemoved)
        {
            SurfOMOnBiomassRemoved(BiomassRemoved);
        }

        /// <summary>
        /// Return the potential residue decomposition for today.
        /// </summary>
        public SurfaceOrganicMatterDecompType PotentialDecomposition()
        {
            GetOtherVariables();
            return Process();
        }

        /// <summary>
        /// Actual surface organic matter decomposition. Calculated by SoilNitrogen.
        /// </summary>
        [XmlIgnore]
        public SurfaceOrganicMatterDecompType ActualSOMDecomp { get; set; }

        /// <summary>
        /// Do the daily residue decomposition for today.
        /// </summary>
        [EventSubscribe("DoSurfaceOrganicMatterDecomposition")]
        private void OnDoSurfaceOrganicMatterDecomposition(object sender, EventArgs args)
        {
            DecomposeSurfom(ActualSOMDecomp);
        }

        public class Prop_upType
        {
            public string name = "";
            public double standing_fract;
        }

        [EventSubscribe("PropUp")]
        private void OnPropUp(Prop_upType data) { PropUp(data); }

        public class AddFaecesType
        {
            public double Defaecations;
            public double VolumePerDefaecation;
            public double AreaPerDefaecation;
            public double Eccentricity;
            public double OMWeight;
            public double OMN;
            public double OMP;
            public double OMS;
            public double OMAshAlk;
            public double NO3N;
            public double NH4N;
            public double POXP;
            public double SO4S;
        }


        [EventSubscribe("AddFaeces")]
        private void OnAddFaeces(AddFaecesType data) { AddFaeces(data); }

        #endregion

        private void surfom_ExternalMassFlow(ExternalMassFlowType massBalanceChange)
        {
            massBalanceChange.PoolClass = "surface";
            PublishExternalMassFlow(massBalanceChange);
        }

        private OMFractionType SurfomTotalState()
        {
            OMFractionType SOMstate = new OMFractionType();

            if (SurfOM == null)
                return SOMstate;

            SOMstate.N = SurfOM.Sum<SurfOrganicMatterType>(x => x.no3 + x.nh4);
            SOMstate.P = SurfOM.Sum<SurfOrganicMatterType>(x => x.po4);

            for (int pool = 0; pool < MaxFr; pool++)
            {
                SOMstate.amount += SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying[pool].amount + x.Standing[pool].amount);
                SOMstate.C += SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying[pool].C + x.Standing[pool].C);
                SOMstate.N += SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying[pool].N + x.Standing[pool].N);
                SOMstate.P += SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying[pool].P + x.Standing[pool].P);
                SOMstate.AshAlk += SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying[pool].AshAlk + x.Standing[pool].AshAlk);
            }

            return SOMstate;
        }

        /// <summary>
        /// Set all variables in this module to zero.
        /// </summary>
        private void ZeroVariables()
        {
            cumeos = 0;
            irrig = 0;
            eos = 0;
            dlayer = new double[dlayer.Length];

            phosphorus_aware = false;
        }

        /// <summary>
        /// Get the values of variables from other modules
        /// </summary>
        private void GetOtherVariables()
        {
            eos = soil.SoilWater.eos;
            dlayer = soil.Thickness;

            CheckPond();
        }

        private void CheckPond()
        {
            if (pond_active == null || pond_active.Length < 1)
                pond_active = "no";
        }

        /// <summary>
        /// Read in all parameters from parameter file
        /// <para>
        /// This now just modifies the inputs and puts them into global structs, reading in is handled by .NET
        /// </para>
        /// </summary>
        private void ReadParam()
        {
            //  Local Variables;
            string[] temp_name = ToArray<string>(PoolName);	                    //temporary array for residue names;
            string[] temp_type = ToArray<string>(type);	                        //temporary array for residue types;
            double[] temp_wt = ToArray<double>(mass);
            double[] temp_residue_cnr = ToArray<double>(cnr);	                    //temporary residue_cnr array;
            double[] temp_residue_cpr = ToArray<double>(cpr);	                    //temporary residue_cpr array;
            double[] temp_standing_fraction = ToArray<double>(standing_fraction);

            double[] tot_c = new double[max_residues];	//total C in residue;
            double[] tot_n = new double[max_residues];	//total N in residue;
            double[] tot_p = new double[max_residues];	//total P in residue;

            //Read in residue type from parameter file;
            //        ------------
            if (temp_name.Length != temp_type.Length)
                throw new Exception("Residue types and names do not match");

            //Read in residue weight from parameter file;
            //        --------------
            if (temp_name.Length != temp_wt.Length)
            {
                throw new Exception("Number of residue names and weights do not match");
            }

            _standing_fraction = temp_standing_fraction;

            //ASSUMING that a value of 0 here means that no C:P ratio was set
            //ASSUMING that this will only be called with a single value in temp_residue_cpr (as was initially coded - the only reason we are using arrays is because that was how the FORTRAN did it)
            phosphorus_aware = temp_residue_cpr.Length > 0 && temp_residue_cpr[0] > 0;

            if (!phosphorus_aware || temp_residue_cpr.Length < temp_residue_cnr.Length)
            {
                double[] temp = new double[temp_residue_cnr.Length];
                temp_residue_cpr.CopyTo(temp, 0);
                for (int i = temp_residue_cpr.Length; i < temp_residue_cnr.Length; i++)
                    temp[i] = _default_cpr;
                temp_residue_cpr = temp;
            }

            if (report_additions == null || report_additions.Length == 0)
                report_additions = "no";


            if (report_removals == null || report_removals.Length == 0)
                report_removals = "no";

            //NOW, PUT ALL THIS INFO INTO THE "SurfaceOM" STRUCTURE;

            DailyInitialC = DailyInitialN = 0;

            SurfOM = new List<SurfOrganicMatterType>();

            for (int i = 0; i < temp_name.Length; i++)
            {
                double pot_decomp_rate;
                //collect relevant type-specific constants from the ini-file;
                ReadTypeSpecificConstants(temp_type[i], i, out pot_decomp_rate);

                SurfOM.Add(
                    new SurfOrganicMatterType()
                    {
                        name = temp_name[i],
                        OrganicMatterType = temp_type[i],

                        //convert the ppm figures into kg/ha;
                        no3 = Utility.Math.Divide(c.no3ppm[i], 1000000.0, 0.0) * temp_wt[i],
                        nh4 = Utility.Math.Divide(c.nh4ppm[i], 1000000.0, 0.0) * temp_wt[i],
                        po4 = Utility.Math.Divide(c.po4ppm[i], 1000000.0, 0.0) * temp_wt[i],

                        PotDecompRate = pot_decomp_rate

                        //these are set in constructor automatically, just here for reference
                        //Standing = new OMFractionType[MaxFr],
                        //Lying = new OMFractionType[MaxFr]
                    }
                );

                tot_c[i] = temp_wt[i] * c.C_fract[i];
                tot_n[i] = Utility.Math.Divide(tot_c[i], temp_residue_cnr[i], 0.0);
                tot_p[i] = Utility.Math.Divide(tot_c[i], temp_residue_cpr[i], 0.0);

                for (int j = 0; j < MaxFr; j++)
                {
                    SurfOM[i].Standing[j] = new OMFractionType()
                        {
                            amount = temp_wt[i] * c.fr_pool_C[j, i] * _standing_fraction[i],
                            C = tot_c[i] * c.fr_pool_C[j, i] * _standing_fraction[i],
                            N = tot_n[i] * c.fr_pool_N[j, i] * _standing_fraction[i],
                            P = tot_p[i] * c.fr_pool_P[j, i] * _standing_fraction[i],
                            AshAlk = 0.0
                        };

                    SurfOM[i].Lying[j] = new OMFractionType()
                        {
                            amount = temp_wt[i] * c.fr_pool_C[j, i] * (1.0 - _standing_fraction[i]),
                            C = tot_c[i] * c.fr_pool_C[j, i] * (1.0 - _standing_fraction[i]),
                            N = tot_n[i] * c.fr_pool_N[j, i] * (1.0 - _standing_fraction[i]),
                            P = tot_p[i] * c.fr_pool_P[j, i] * (1.0 - _standing_fraction[i]),
                            AshAlk = 0.0
                        };
                }

                DailyInitialC = tot_c.Sum();
                DailyInitialN = tot_n.Sum();
            }
        }


        /// <summary>
        /// Get the solutes number
        /// </summary>
        /// <param name="surfomname"></param>
        /// <returns></returns>
        private int GetSoluteNumber(string surfomname)
        {
            if (SurfOM == null)
                return -1;

            for (int i = 0; i < SurfOM.Count; i++)
                if (SurfOM[i].name.Equals(surfomname, StringComparison.CurrentCultureIgnoreCase))
                    return i;

            return -1;
        }

        //surfom_ONnewmet used to sit here but it's now defunct thanks to newer APSIM methods :)

        /// <summary>
        /// Performs manure decomposition taking into account environmental;
        ///  and manure factors (independant to soil N but N balance can modify;
        ///  actual decomposition rates if desired by N model - this is possible;
        ///  because pools are not updated until end of time step - see post routine)
        /// </summary>
        /// <returns>pool decompositions for all residues</returns>
        private void PotDecomp(out double[] c_pot_decomp, out double[] n_pot_decomp, out double[] p_pot_decomp)
        {

            //(these pools are not updated until end of time step - see post routine)
            c_pot_decomp = new double[num_surfom];
            n_pot_decomp = new double[num_surfom];
            p_pot_decomp = new double[num_surfom];

            //  Local Variables;

            double
                mf = MoistureFactor(),	    //moisture factor for decomp (0-1)
                tf = TemperatureFactor(),	    //temperature factor for decomp (0-1)
                cf = ContactFactor();	    //manure/soil contact factor for decomp (0-1)

            for (int residue = 0; residue < num_surfom; residue++)
            {
                double cnrf = CNRatioFactor(residue);    //C:N factor for decomp (0-1) for surfom under consideration

                double
                    Fdecomp = -1,       //decomposition fraction for the given surfom
                    sumC = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C);

                if (sumC < _crit_min_surfom_orgC)
                {
                    //Residue wt is sufficiently low to suggest decomposing all;
                    //material to avoid low numbers which can cause problems;
                    //with numerical precision;
                    Fdecomp = 1;
                }
                else
                {
                    //Calculate today"s decomposition  as a fraction of potential rate;
                    Fdecomp = SurfOM[residue].PotDecompRate * mf * tf * cnrf * cf;
                }

                //Now calculate pool decompositions for this residue;
                c_pot_decomp[residue] = Fdecomp * sumC;
                n_pot_decomp[residue] = Fdecomp * SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N); ;
                p_pot_decomp[residue] = Fdecomp * SurfOM[residue].Lying.Sum<OMFractionType>(x => x.P); ;
            }
        }

        /// <summary>
        /// Calculate temperature factor for manure decomposition (0-1).
        /// <para>
        ///  Notes;
        ///  The temperature factor is a simple function of the square of daily
        ///  average temperature.  The user only needs to give an optimum temperature
        ///  and the code will back calculate the necessary coefficient at compile time.
        /// </para>
        /// </summary>
        /// <returns>temperature factor</returns>
        private double TemperatureFactor()
        {
            double
                ave_temp = Utility.Math.Divide((MetData.Maxt + MetData.Mint), 2.0, 0.0);	//today"s average air temp (oC)
            //tf;	//temperature factor;

            if (ave_temp > 0.0)
                return Utility.Math.Bound(
                    (double)Math.Pow(Utility.Math.Divide(ave_temp, _opt_temp, 0.0), 2.0),
                    0.0,
                    1.0
                    );
            else
                return 0;
        }

        /// <summary>
        /// Calculate manure/soil contact factor for manure decomposition (0-1).
        /// </summary>
        /// <returns></returns>
        private double ContactFactor()
        {
            double eff_surfom_wt = 0;	//Total residue wt across all instances;

            //Sum the effective mass of surface residues considering lying fraction only.
            //The "effective" weight takes into account the haystack effect and is governed by the;
            //cf_contrib factor (ini file).  ie some residue types do not contribute to the haystack effect.

            for (int residue = 0; residue < num_surfom; residue++)
                eff_surfom_wt += SurfOM[residue].Lying.Sum<OMFractionType>(x => x.amount) * c.cf_contrib[residue];

            if (eff_surfom_wt <= _crit_residue_wt)
                return 1.0;
            else
                return Utility.Math.Bound(Utility.Math.Divide(_crit_residue_wt, eff_surfom_wt, 0.0), 0, 1);
        }

        /// <summary>
        /// Calculate C:N factor for decomposition (0-1).
        /// </summary>
        /// <param name="residue">residue number</param>
        /// <returns>C:N factor for decomposition(0-1)</returns>
        private double CNRatioFactor(int residue)
        {
            if (residue < 0)
                return 1;
            else
            {
                //Note: C:N ratio factor only based on lying fraction
                double
                    total_C = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C),    //organic C component of this residue (kg/ha)
                    total_N = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N),    //organic N component of this residue (kg/ha)
                    total_mineral_n = SurfOM[residue].no3 + SurfOM[residue].nh4,    //mineral N component of this surfom (no3 + nh4)(kg/ha)
                    cnr = Utility.Math.Divide(total_C, (total_N + total_mineral_n), 0.0);               //C:N for this residue  (unitless)

                //As C:N increases above optcn cnrf decreases exponentially toward zero;
                //As C:N decreases below optcn cnrf is constrained to one;

                if (_cnrf_optcn == 0)
                {
                    return 1;
                }
                else
                {
                    return Utility.Math.Bound(
                        (double)Math.Exp(-_cnrf_coeff * ((cnr - _cnrf_optcn) / _cnrf_optcn)),
                        0.0,
                        1.0
                    );
                }
            }
        }

        /// <summary>
        /// Calculate moisture factor for manure decomposition (0-1).
        /// </summary>
        /// <returns></returns>
        private double MoistureFactor()
        {
            /*
                double mf;	//moisture factor for decomp (0-1)

               if (pond_active=="yes") {

                  //mf will always be 0.5, to recognise that potential decomp is less under flooded conditions;

                 return 0.5;

               }else{
                  //not flooded conditions

                  //moisture factor decreases from 1. at start of cumeos and decreases;
                  //linearly to zero at cum_eos_max;

                 mf = 1.0 - Utility.Math.Divide (cumeos, c.cum_eos_max, 0.0);

                 mf = Utility.Math.Bound(mf, 0.0, 1.0);
            return mf;
             * }
                 */

            //optimisation of above code:
            if (pond_active == "yes")
            {
                return 0.5;
            }
            else
            {
                return Utility.Math.Bound(1.0 - Utility.Math.Divide(cumeos, _cum_eos_max, 0.0), 0.0, 1.0);
            }
        }

        /// <summary>
        /// Calculate total cover
        /// </summary>
        /// <returns></returns>
        private double CoverTotal()
        {
            /*
             * Old way of doing it (:S not sure why this had to be so complicated)
             *
            double cover1 = 0;	//fraction of ground covered by residue (0-1)
            double cover2;	//fraction of ground covered by residue (0-1)
            double combined_cover = 0;	//effective combined cover from covers 1 & 2 (0-1)

            for (int i = 0; i < num_surfom; i++)
            {
                cover2 = Cover(i);
                combined_cover = AddCover(cover1, cover2);
                cover1 = combined_cover;
            }

            return combined_cover;
            */

            double combined_cover = 0;	//effective combined cover(0-1)

            for (int i = 0; i < num_surfom; i++)
                combined_cover = AddCover(combined_cover, Cover(i));

            return combined_cover;
        }

        /// <summary>
        /// Perform actions for current day.
        /// </summary>
        private SurfaceOrganicMatterDecompType Process()
        {
            double leach_rain = 0;	//"leaching" rainfall (if rain>10mm)

            SetPhosphorusAware();

            SetVars(out cumeos, out leach_rain);

            if (leach_rain > 0.0)
            {
                Leach(leach_rain);
            }
            // else no mineral N or P is leached today;

            return SendPotDecompEvent();

        }


        /// <summary>
        ///  Calculates variables required for today"s decomposition and;
        ///leaching factors.
        /// </summary>
        /// <param name="cumeos"></param>
        /// <param name="leach_rain"></param>
        private void SetVars(out double cumeos, out double leach_rain)
        {
            double precip = MetData.Rain + irrig;	//daily precipitation (rain + irrigation) (mm H2O)

            if (precip > 4.0)
            {
                //reset cumulative to just today"s eos
                cumeos = eos - precip;
            }
            else
            {
                //keep accumulating eos
                cumeos = this.cumeos + eos - precip;
            }
            cumeos = Math.Max(cumeos, 0.0);

            if (precip >= _min_rain_to_leach)
            {
                leach_rain = precip;
            }
            else
            {
                leach_rain = 0.0;
            }

            //reset irrigation log now that we have used that information;
            irrig = 0.0;
        }


        /// <summary>
        ///Remove mineral N and P from surfom with leaching rainfall and;
        ///pass to Soil N and Soil P modules.
        /// </summary>
        /// <param name="leach_rain"></param>
        private void Leach(double leach_rain)
        {

            double nh4_incorp;
            double no3_incorp;
            double po4_incorp;

            // Calculate Leaching Fraction;
            _leaching_fr = Utility.Math.Bound(Utility.Math.Divide(leach_rain, _leach_rain_tot, 0.0), 0.0, 1.0);


            //Apply leaching fraction to all mineral pools;
            //Put all mineral NO3,NH4 and PO4 into top layer;
            no3_incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.no3) * _leaching_fr;
            nh4_incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.nh4) * _leaching_fr;
            po4_incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.po4) * _leaching_fr;


            //If neccessary, Send the mineral N & P leached to the Soil N&P modules;
            if (no3_incorp > 0.0 || nh4_incorp > 0.0 || po4_incorp > 0.0)
            {
                NitrogenChangedType NitrogenChanges = new NitrogenChangedType();
                NitrogenChanges.Sender = "SurfaceOrganicMatter";
                NitrogenChanges.SenderType = "SurfaceOrganicMatter";
                NitrogenChanges.DeltaNH4 = new double[dlayer.Length];
                NitrogenChanges.DeltaNO3 = new double[dlayer.Length];
                NitrogenChanges.DeltaUrea = new double[dlayer.Length];
                NitrogenChanges.DeltaNH4[0] = nh4_incorp;
                NitrogenChanges.DeltaNO3[0] = no3_incorp;

                NitrogenChanged.Invoke(NitrogenChanges);

                if (phosphorus_aware)
                {
                    throw new NotImplementedException();
                }

            }

            for (int i = 0; i < num_surfom; i++)
            {
                SurfOM[i].no3 = SurfOM[i].no3 * (1.0 - _leaching_fr);
                SurfOM[i].nh4 = SurfOM[i].nh4 * (1.0 - _leaching_fr);
                SurfOM[i].po4 = SurfOM[i].po4 * (1.0 - _leaching_fr);
            }

        }



        /// <summary>
        /// Notify other modules of the potential to decompose.
        /// </summary>
        private SurfaceOrganicMatterDecompType SendPotDecompEvent()
        {

            if (num_surfom <= 0)
                return null;

            SurfaceOrganicMatterDecompType SOMDecomp = new SurfaceOrganicMatterDecompType()
            {
                Pool = new SurfaceOrganicMatterDecompPoolType[num_surfom]
            };

            double[]
                c_pot_decomp,
                n_pot_decomp,
                p_pot_decomp;

            PotDecomp(out c_pot_decomp, out n_pot_decomp, out p_pot_decomp);

            for (int residue = 0; residue < num_surfom; residue++)
                SOMDecomp.Pool[residue] = new SurfaceOrganicMatterDecompPoolType()
                {
                    Name = SurfOM[residue].name,
                    OrganicMatterType = SurfOM[residue].OrganicMatterType,

                    FOM = new FOMType()
                    {
                        amount = Utility.Math.Divide(c_pot_decomp[residue], c.C_fract[residue], 0.0),
                        C = c_pot_decomp[residue],
                        N = n_pot_decomp[residue],
                        P = p_pot_decomp[residue],
                        AshAlk = 0.0
                    }
                };

            return SOMDecomp;
        }

        /// <summary>
        /// send current status.
        /// </summary>
        private SurfaceOrganicMatterType respond2get_SurfaceOrganicMatter()
        {

            //TODO - implement  SurfaceOrganicMatterType.Clone() and use that here instead
            SurfaceOrganicMatterType SOM = new SurfaceOrganicMatterType()
            {
                Pool = new SurfaceOrganicMatterPoolType[num_surfom]
            };

            for (int residue = 0; residue < num_surfom; residue++)
            {
                SOM.Pool[residue] = new SurfaceOrganicMatterPoolType()
                {
                    Name = SurfOM[residue].name,
                    OrganicMatterType = SurfOM[residue].OrganicMatterType,
                    PotDecompRate = SurfOM[residue].PotDecompRate,
                    no3 = SurfOM[residue].no3,
                    nh4 = SurfOM[residue].nh4,
                    po4 = SurfOM[residue].po4
                };

                SOM.Pool[residue].StandingFraction = new FOMType[MaxFr];
                SOM.Pool[residue].LyingFraction = new FOMType[MaxFr];

                for (int pool = 0; pool < MaxFr; pool++)
                {
                    SOM.Pool[residue].StandingFraction[pool] = new FOMType()
                    {
                        amount = SurfOM[residue].Standing[pool].amount,
                        C = SurfOM[residue].Standing[pool].C,
                        N = SurfOM[residue].Standing[pool].N,
                        P = SurfOM[residue].Standing[pool].P,
                        AshAlk = SurfOM[residue].Standing[pool].AshAlk
                    };

                    SOM.Pool[residue].LyingFraction[pool] = new FOMType()
                    {
                        amount = SurfOM[residue].Lying[pool].amount,
                        C = SurfOM[residue].Lying[pool].C,
                        N = SurfOM[residue].Lying[pool].N,
                        P = SurfOM[residue].Lying[pool].P,
                        AshAlk = SurfOM[residue].Lying[pool].AshAlk
                    };
                }
            }

            publish_SurfaceOrganicMatter(SOM);
            return SOM;
        }

        /// <summary>
        /// Calculates surfom removal as a result of remove_surfom message
        /// </summary>
        /// <param name="SOM"></param>
        private void RemoveSurfom(SurfaceOrganicMatterType SOM)
        {
            //- Implementation Section ----------------------------------

            for (int som_index = 0; som_index < num_surfom; som_index++)
            {
                //Determine which residue pool corresponds to this index in the array;
                int SOMNo = GetSoluteNumber(SOM.Pool[som_index].Name);

                if (SOMNo < 0)
                {
                    //This is an unknown type - error (<- original comment, not really an error as original code didn't throw one :S)
                    summary.WriteMessage(FullPath, "Attempting to remove Surface Organic Matter from unknown " + SOM.Pool[som_index].Name + " Surface Organic Matter name." + Environment.NewLine);
                }
                else
                {
                    //This type already exists;

                    //        SurfOM[SOMNo] = SurfOM[SOMNo] - SOM[SOMNo]

                    //          Check if too much removed ?
                    for (int pool = 0; pool < MaxFr; pool++)
                    {
                        if (SurfOM[SOMNo].Lying[pool].amount >= SOM.Pool[SOMNo].LyingFraction[pool].amount)
                            SurfOM[SOMNo].Lying[pool].amount -= SOM.Pool[SOMNo].LyingFraction[pool].amount;
                        else
                        {
                            throw new Exception(
                                "Attempting to remove more dm from " + SOM.Pool[som_index].Name + " lying Surface Organic Matter pool " + pool + " than available" + Environment.NewLine
                                + "Removing " + SOM.Pool[SOMNo].LyingFraction[pool].amount + " (kg/ha) " + "from " + SurfOM[SOMNo].Lying[pool].amount + " (kg/ha) available."
                            );
                        }

                        SurfOM[SOMNo].Lying[pool].C -= SOM.Pool[SOMNo].LyingFraction[pool].C;
                        SurfOM[SOMNo].Lying[pool].N -= SOM.Pool[SOMNo].LyingFraction[pool].N;
                        SurfOM[SOMNo].Lying[pool].P -= SOM.Pool[SOMNo].LyingFraction[pool].P;
                        SurfOM[SOMNo].Lying[pool].AshAlk -= SOM.Pool[SOMNo].LyingFraction[pool].AshAlk;

                        if (SurfOM[SOMNo].Standing[pool].amount >= SOM.Pool[SOMNo].StandingFraction[pool].amount)
                        {
                            SurfOM[SOMNo].Standing[pool].amount -= SOM.Pool[SOMNo].StandingFraction[pool].amount;
                        }
                        else
                        {
                            summary.WriteMessage(FullPath,
                                "Attempting to remove more dm from " + SOM.Pool[som_index].Name + " standing Surface Organic Matter pool " + pool + " than available" + Environment.NewLine
                                + "Removing " + SOM.Pool[SOMNo].LyingFraction[pool].amount + " (kg/ha) " + "from " + SurfOM[SOMNo].Lying[pool].amount + " (kg/ha) available."
                           );
                        }

                        SurfOM[SOMNo].Standing[pool].C -= SOM.Pool[SOMNo].StandingFraction[pool].C;
                        SurfOM[SOMNo].Standing[pool].N -= SOM.Pool[SOMNo].StandingFraction[pool].N;
                        SurfOM[SOMNo].Standing[pool].P -= SOM.Pool[SOMNo].StandingFraction[pool].P;
                        SurfOM[SOMNo].Standing[pool].AshAlk -= SOM.Pool[SOMNo].StandingFraction[pool].AshAlk;
                    }

                    SurfOM[SOMNo].no3 -= SOM.Pool[SOMNo].no3;
                    SurfOM[SOMNo].nh4 -= SOM.Pool[SOMNo].nh4;
                    SurfOM[SOMNo].po4 -= SOM.Pool[SOMNo].po4;
                }

                double
                    samount = SOM.Pool[SOMNo].StandingFraction.Sum<FOMType>(x => x.amount),
                    sN = SOM.Pool[SOMNo].StandingFraction.Sum<FOMType>(x => x.N),
                    sP = SOM.Pool[SOMNo].StandingFraction.Sum<FOMType>(x => x.P),
                    lamount = SOM.Pool[SOMNo].LyingFraction.Sum<FOMType>(x => x.amount),
                    lN = SOM.Pool[SOMNo].LyingFraction.Sum<FOMType>(x => x.N),
                    lP = SOM.Pool[SOMNo].LyingFraction.Sum<FOMType>(x => x.P);


                //Report Removals;
                if (report_removals == "yes")
                    summary.WriteMessage(FullPath, String.Format(
    @"Removed SurfaceOM
    SurfaceOM name  = {0}
    SurfaceOM Type  = {1}

    Amount Removed (kg/ha):
        Lying:
            Amount = {2:0.0##}
            N      = {3:0.0##}
            P      = {4:0.0##}
        Standing:
            Amount = {5:0.0##}
            N      = {6:0.0##}
            P      = {7:0.0##}", SOM.Pool[SOMNo].Name, SOM.Pool[SOMNo].OrganicMatterType, lamount, lN, lP, samount, sN, sP
                    ));
                // else the user has asked for no reports for removals of surfom;
                    //in the summary file.
                
                surfom_Send_SOM_removed_Event(
                    SOM.Pool[SOMNo].OrganicMatterType,
                    SOM.Pool[SOMNo].OrganicMatterType,
                    samount + lamount,
                    sN + lN,
                    sP + lP
                   );

            }
        }

        private void DecomposeSurfom(SurfaceOrganicMatterDecompType SOMDecomp)
        {
            //  Local Variables;
            int num_surfom = SOMDecomp.Pool.Length;             //local surfom counter from received event;
            int residue_no;                                     //Index into the global array;
            double[] c_pot_decomp = new double[num_surfom];	//pot amount of C to decompose (kg/ha)
            double[] n_pot_decomp = new double[num_surfom];	//pot amount of N to decompose (kg/ha)
            double[] p_pot_decomp = new double[num_surfom];	//pot amount of P to decompose (kg/ha)
            double tot_c_decomp;	//total amount of c to decompose;
            double tot_n_decomp;	//total amount of c to decompose;
            double tot_p_decomp;	//total amount of c to decompose;

            double SOMcnr = 0;
            double SOMc = 0;
            double SOMn = 0;

            //calculate potential decompostion of C, N, and P;
            PotDecomp(out c_pot_decomp, out n_pot_decomp, out p_pot_decomp);

            for (int counter = 0; counter < num_surfom; counter++)
            {

                //Determine which residue pool corresponds to this index in the array;
                residue_no = GetSoluteNumber(SOMDecomp.Pool[counter].Name);

                //Collect actual decompostion of C and N from supplying module (soiln2)
                tot_c_decomp = SOMDecomp.Pool[counter].FOM.C;
                tot_n_decomp = SOMDecomp.Pool[counter].FOM.N;

                Bound_check_real_var(tot_n_decomp, 0.0, n_pot_decomp[residue_no], "total n decomposition");

                SOMc = SurfOM[residue_no].Standing.Sum<OMFractionType>(x => x.C) + SurfOM[residue_no].Lying.Sum<OMFractionType>(x => x.C);
                SOMn = SurfOM[residue_no].Standing.Sum<OMFractionType>(x => x.N) + SurfOM[residue_no].Lying.Sum<OMFractionType>(x => x.N);

                SOMcnr = Utility.Math.Divide(SOMc, SOMn, 0.0);

                if (reals_are_equal(tot_c_decomp, 0.0) && reals_are_equal(tot_n_decomp, 0.0))
                {
                    //all OK - nothing happening;
                }
                else if (tot_c_decomp > c_pot_decomp[residue_no] + acceptableErr)
                {
                    throw new Exception("SurfaceOM - C decomposition exceeds potential rate");
                }
                else if (tot_n_decomp > n_pot_decomp[residue_no] + acceptableErr)
                {
                    throw new Exception("SurfaceOM - N decomposition exceeds potential rate");
                    //NIH - If both the following tests are empty then they can both be deleted.
                }
                else if (reals_are_equal(Utility.Math.Divide(tot_c_decomp, tot_n_decomp, 0.0), SOMcnr))
                {
                    //all ok - decomposition and residue pool have same C:N;
                }
                else
                {
                    //              call fatal_error (Err_internal,
                    //    :                 "C:N ratio of decomposed residues is not valid")
                }

                //calculate total p decomposition;
                tot_p_decomp = tot_c_decomp * Utility.Math.Divide(p_pot_decomp[residue_no], c_pot_decomp[residue_no], 0.0);

                //Do actual decomposing - update pools for C, N, and P;
                Decomp(tot_c_decomp, tot_n_decomp, tot_p_decomp, residue_no);
            }
        }

        /// <summary>
        /// Performs updating of pools due to surfom decomposition
        /// </summary>
        /// <param name="C_decomp">C to be decomposed</param>
        /// <param name="N_decomp">N to be decomposed</param>
        /// <param name="P_decomp">P to be decomposed</param>
        /// <param name="residue">residue number being dealt with</param>
        private void Decomp(double C_decomp, double N_decomp, double P_decomp, int residue)
        {

            double Fdecomp;  //decomposing fraction;

            //do C
            Fdecomp = Utility.Math.Bound(Utility.Math.Divide(C_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C), 0.0), 0.0, 1.0);
            for (int i = 0; i < MaxFr; i++)
            {
                SurfOM[residue].Lying[i].C = SurfOM[residue].Lying[i].C * (1.0 - Fdecomp);
                SurfOM[residue].Lying[i].amount = SurfOM[residue].Lying[i].amount * (1.0 - Fdecomp);
            }

            //do N
            Fdecomp = Utility.Math.Divide(N_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N), 0.0);
            for (int i = 0; i < MaxFr; i++)
                SurfOM[residue].Lying[i].N = SurfOM[residue].Lying[i].N * (1.0 - Fdecomp);

            //do P
            Fdecomp = Utility.Math.Divide(P_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.P), 0.0);
            for (int i = 0; i < MaxFr; i++)
                SurfOM[residue].Lying[i].P = SurfOM[residue].Lying[i].P * (1.0 - Fdecomp);
        }

        /// <summary>
        /// Calculates surfom incorporation as a result of tillage operations.
        /// </summary>
        private void Tillage(TillageType data)
        {
            /*
            double[] type_info;	//Array containing information about;

            //----------------------------------------------------------
            //      Get User defined tillage effects on residue;
            //----------------------------------------------------------
            //collect_char_var ("type", "()", till_type, numvals);
            //collect_real_var_optional ("f_incorp", "()", f_incorp, numvals_f, 0.0, 1.0);
            //collect_real_var_optional ("tillage_depth", "()", tillage_depth, numvals_t, 0.0, 1000.0);
            */

            //----------------------------------------------------------
            //   If no user defined characteristics then use the;
            //     lookup table compiled from expert knowledge;
            //----------------------------------------------------------
            if (data.f_incorp == 0 && data.tillage_depth == 0)
            {
                summary.WriteMessage(FullPath, "    - Reading default residue tillage info");

                data = tillageTypes.GetTillageData(data.Name);

                //If we still have no values then stop
                if (data == null)
                    //We have an unspecified tillage type;
                    throw new Exception("Cannot find info for tillage:- " + data.Name);
            }

            //----------------------------------------------------------
            //             Now incorporate the residues;
            //----------------------------------------------------------

            Incorp(data.Name, data.f_incorp, data.tillage_depth);

            summary.WriteMessage(FullPath, String.Format(
    @"Residue removed using {0}
    Fraction Incorporated = {1:0.0##}
    Incorporated Depth    = {2:0.0##}", data.Name, data.f_incorp, data.tillage_depth
                 ));
        }


        /// <summary>
        /// Calculate surfom incorporation as a result of tillage and update;
        ///  residue and N pools.
        ///  <para>
        ///  Notes;
        ///  I do not like updating the pools here but we need to be able to handle;
        ///  the case of multiple tillage events per day.</para>
        /// </summary>
        /// <param name="action_type"></param>
        /// <param name="F_incorp"></param>
        /// <param name="Tillage_depth"></param>
        private void Incorp(string action_type, double F_incorp, double Tillage_depth)
        //================================================================
        {
            double cum_depth;	//
            int deepest_Layer;         //
            int nLayers = dlayer.Length;
            double depth_to_go;	//
            double F_incorp_layer = 0;	//
            double[] residue_incorp_fraction = new double[nLayers];
            double layer_incorp_depth;	//
            double[,] C_pool = new double[MaxFr, nLayers];	//total C in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] N_pool = new double[MaxFr, nLayers];	//total N in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] P_pool = new double[MaxFr, nLayers];	//total P in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] AshAlk_pool = new double[MaxFr, nLayers];	//total AshAlk in each Om fraction and layer (from all surfOM"s) incorporated;
            double[] no3 = new double[nLayers];	//total no3 to go into each soil layer (from all surfOM"s)
            double[] nh4 = new double[nLayers];	//total nh4 to go into each soil layer (from all surfOM"s)
            double[] po4 = new double[nLayers];	//total po4 to go into each soil layer (from all surfOM"s)
            FOMPoolType FPoolProfile = new FOMPoolType();
            ExternalMassFlowType massBalanceChange = new ExternalMassFlowType();

            F_incorp = Utility.Math.Bound(F_incorp, 0.0, 1.0);

            deepest_Layer = GetCumulativeIndexReal(Tillage_depth, dlayer);

            cum_depth = 0.0;

            for (int layer = 0; layer <= deepest_Layer; layer++)
            {

                for (int residue = 0; residue < num_surfom; residue++)
                {

                    depth_to_go = Tillage_depth - cum_depth;
                    layer_incorp_depth = Math.Min(depth_to_go, dlayer[layer]);
                    F_incorp_layer = Utility.Math.Divide(layer_incorp_depth, Tillage_depth, 0.0);
                    for (int i = 0; i < MaxFr; i++)
                    {
                        C_pool[i, layer] += (SurfOM[residue].Lying[i].C + SurfOM[residue].Standing[i].C) * F_incorp * F_incorp_layer;
                        N_pool[i, layer] += (SurfOM[residue].Lying[i].N + SurfOM[residue].Standing[i].N) * F_incorp * F_incorp_layer;
                        P_pool[i, layer] += (SurfOM[residue].Lying[i].P + SurfOM[residue].Standing[i].P) * F_incorp * F_incorp_layer;
                        AshAlk_pool[i, layer] += (SurfOM[residue].Lying[i].AshAlk + SurfOM[residue].Standing[i].AshAlk) * F_incorp * F_incorp_layer;
                    }
                    no3[layer] += SurfOM[residue].no3 * F_incorp * F_incorp_layer;
                    nh4[layer] += SurfOM[residue].nh4 * F_incorp * F_incorp_layer;
                    po4[layer] += SurfOM[residue].po4 * F_incorp * F_incorp_layer;
                }

                cum_depth = cum_depth + dlayer[layer];

                //dsg 160104  Remove the following variable after Res_removed_Event is scrapped;
                residue_incorp_fraction[layer] = F_incorp_layer;
            }

            if (Sum2DArray(C_pool) > 0.0)
            {

                //Pack up the incorporation info and send to SOILN2 and SOILP as part of a;
                //IncorpFOMPool Event;

                FPoolProfile.Layer = new FOMPoolLayerType[deepest_Layer + 1];

                for (int layer = 0; layer <= deepest_Layer; layer++)
                {
                    FPoolProfile.Layer[layer] = new FOMPoolLayerType()
                    {
                        thickness = dlayer[layer],
                        no3 = no3[layer],
                        nh4 = nh4[layer],
                        po4 = po4[layer],
                        Pool = new FOMType[MaxFr]
                    };

                    for (int i = 0; i < MaxFr; i++)
                        FPoolProfile.Layer[layer].Pool[i] = new FOMType()
                        {
                            C = C_pool[i, layer],
                            N = N_pool[i, layer],
                            P = P_pool[i, layer],
                            AshAlk = AshAlk_pool[i, layer]
                        };
                }

                //APSIM THING
                publish_FOMPool(FPoolProfile);

                //dsg 160104  Keep this event for the time being - will be replaced by ResidueChanged;

                SendResRemovedEvent(action_type, F_incorp, residue_incorp_fraction, deepest_Layer);

            }
            // else no residue incorporated;

            if (Tillage_depth <= 0.000001)
            {
                //the OM is not incorporated and is lost from the system;

                massBalanceChange.PoolClass = "surface";
                massBalanceChange.FlowType = "loss";
                massBalanceChange.DM = 0.0;
                massBalanceChange.C = 0.0;
                massBalanceChange.N = 0.0;
                massBalanceChange.P = 0.0;
                massBalanceChange.SW = 0.0;

                for (int pool = 0; pool < MaxFr; pool++)
                {
                    massBalanceChange.DM += SurfOM.Sum<SurfOrganicMatterType>(x => x.Standing[pool].amount + x.Lying[pool].amount) * F_incorp;
                    massBalanceChange.C += SurfOM.Sum<SurfOrganicMatterType>(x => x.Standing[pool].C + x.Lying[pool].C) * F_incorp;
                    massBalanceChange.N += SurfOM.Sum<SurfOrganicMatterType>(x => x.Standing[pool].N + x.Lying[pool].N) * F_incorp;
                    massBalanceChange.P += SurfOM.Sum<SurfOrganicMatterType>(x => x.Standing[pool].P + x.Lying[pool].P) * F_incorp;
                }
                surfom_ExternalMassFlow(massBalanceChange);
            }

            //Now update globals.  They must be updated here because there is the possibility of;
            //more than one incorporation on any given day;

            for (int pool = 0; pool < MaxFr; pool++)
            {
                for (int i = 0; i < SurfOM.Count; i++)
                {
                    SurfOM[i].Lying[pool].amount = SurfOM[i].Lying[pool].amount * (1.0 - F_incorp);
                    SurfOM[i].Standing[pool].amount = SurfOM[i].Standing[pool].amount * (1.0 - F_incorp);

                    SurfOM[i].Lying[pool].C = SurfOM[i].Lying[pool].C * (1.0 - F_incorp);
                    SurfOM[i].Standing[pool].C = SurfOM[i].Standing[pool].C * (1.0 - F_incorp);

                    SurfOM[i].Lying[pool].N = SurfOM[i].Lying[pool].N * (1.0 - F_incorp);
                    SurfOM[i].Standing[pool].N = SurfOM[i].Standing[pool].N * (1.0 - F_incorp);

                    SurfOM[i].Lying[pool].P = SurfOM[i].Lying[pool].P * (1.0 - F_incorp);
                    SurfOM[i].Standing[pool].P = SurfOM[i].Standing[pool].P * (1.0 - F_incorp);

                    SurfOM[i].Lying[pool].AshAlk = SurfOM[i].Lying[pool].AshAlk * (1.0 - F_incorp);
                    SurfOM[i].Standing[pool].AshAlk = SurfOM[i].Standing[pool].AshAlk * (1.0 - F_incorp);
                }
            }

            for (int i = 0; i < SurfOM.Count; i++)
            {
                SurfOM[i].no3 *= (1.0 - F_incorp);
                SurfOM[i].nh4 *= (1.0 - F_incorp);
                SurfOM[i].po4 *= (1.0 - F_incorp);
            }
        }

        /// <summary>
        /// Calculates surfom addition as a result of add_surfom message
        /// </summary>
        private void AddSurfom(Add_surfaceomType data)
        {

            int SOMNo = 0;                 //specific system number for this residue name;

            string
                surfom_name = data.name,
                surfom_type = data.type;

            double
                surfom_mass_added = Utility.Math.Bound(data.mass, -100000, 100000),  //Mass of new surfom added (kg/ha)
                surfom_c_added = 0,	                                    //C added in new material (kg/ha)
                surfom_n_added = Utility.Math.Bound(data.n, -10000, 10000),	        //N added in new material (kg/ha)
                surfom_no3_added = 0,	                                //NO3 added in new material (kg/ha)
                surfom_nh4_added = 0,	                                //NH4 added in new material (kg/ha)
                surfom_cnr_added = Utility.Math.Bound(data.cnr, 0, 10000),	        //C:N ratio of new material;
                surfom_p_added = Utility.Math.Bound(data.p, -10000, 10000),          //P added in new material (kg/ha)
                surfom_po4_added = 0,	                                //PO4 added in new material (kg/ha)
                surfom_cpr_added = Utility.Math.Bound(data.cpr, 0, 10000),	        //C:P ratio of new material;
                tot_mass = 0,
                removed_from_standing = 0,
                removed_from_lying = 0;

            ExternalMassFlowType massBalanceChange = new ExternalMassFlowType();

            SOMNo = GetSoluteNumber(surfom_name);

            if (SOMNo < 0)
            {

                SurfOM.Add(new SurfOrganicMatterType(surfom_name, surfom_type));
                SOMNo = num_surfom - 1;

                ReadTypeSpecificConstants(SurfOM[SOMNo].OrganicMatterType, SOMNo, out SurfOM[SOMNo].PotDecompRate);
            }
            // else this type already exists;

            //Get Mass of material added;

            //APSIMT THING
            //collect_real_var ("mass", "(kg/ha)", surfom_added, numvals, -100000.0, 100000.0);
            surfom_c_added = surfom_mass_added * c.C_fract[SOMNo];

            if (surfom_mass_added > -10000.0)
            {
                //Get N content of material added;
                if (surfom_n_added == 0)
                {
                    //APSIM THING
                    //collect_real_var_optional ("cnr", "()", surfom_cnr_added, numval_cnr, 0.0, 10000.0);
                    surfom_n_added = Utility.Math.Divide((surfom_mass_added * c.C_fract[SOMNo]), surfom_cnr_added, 0.0);

                    //If no N info provided, and no cnr info provided then throw error
                    if (surfom_cnr_added == 0)
                        throw new Exception("SurfaceOM CN ratio not specified.");
                }

                //APSIM THING
                if (surfom_p_added == 0)
                    //If no P info provided, and no cpr info provided {
                    //use default cpr and throw warning error to notify user;
                    if (surfom_cpr_added == 0)
                    {
                        surfom_p_added = Utility.Math.Divide((surfom_mass_added * c.C_fract[SOMNo]), _default_cpr, 0.0);
                        summary.WriteMessage(FullPath, "SurfOM P or SurfaceOM C:P ratio not specified - Default value applied.");
                    }
                    else
                        surfom_p_added = Utility.Math.Divide((surfom_mass_added * c.C_fract[SOMNo]), surfom_cpr_added, 0.0);

                //convert the ppm figures into kg/ha;
                surfom_no3_added = Utility.Math.Divide(c.no3ppm[SOMNo], 1000000, 0) * surfom_mass_added;
                surfom_nh4_added = Utility.Math.Divide(c.nh4ppm[SOMNo], 1000000, 0) * surfom_mass_added;
                surfom_po4_added = Utility.Math.Divide(c.po4ppm[SOMNo], 1000000, 0) * surfom_mass_added;

                SurfOM[SOMNo].no3 += surfom_no3_added;
                SurfOM[SOMNo].nh4 += surfom_nh4_added;
                SurfOM[SOMNo].po4 += surfom_po4_added;

                if (surfom_mass_added > 0.0)
                    //Assume all residue added is in the LYING pool, ie No STANDING component;
                    for (int i = 0; i < MaxFr; i++)
                    {
                        SurfOM[SOMNo].Lying[i].amount += surfom_mass_added * c.fr_pool_C[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].C += surfom_mass_added * c.C_fract[SOMNo] * c.fr_pool_C[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].N += surfom_n_added * c.fr_pool_N[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].P += surfom_p_added * c.fr_pool_P[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].AshAlk = 0;
                    }
                else
                {
                    //if residue is being removed, remove residue from both standing and lying pools;
                    double
                        lying = SurfOM[SOMNo].Lying.Sum<OMFractionType>(x => x.amount),
                        standing = SurfOM[SOMNo].Standing.Sum<OMFractionType>(x => x.amount);

                    tot_mass = lying + standing;

                    removed_from_standing = surfom_mass_added * (Utility.Math.Divide(standing, tot_mass, 0.0));
                    removed_from_lying = surfom_mass_added - removed_from_standing;

                    for (int i = 0; i < MaxFr; i++)
                    {
                        SurfOM[SOMNo].Lying[i].amount = SurfOM[SOMNo].Lying[i].amount + removed_from_lying * c.fr_pool_C[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].C = SurfOM[SOMNo].Lying[i].C + removed_from_lying * c.C_fract[SOMNo] * c.fr_pool_C[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].N = SurfOM[SOMNo].Lying[i].N + surfom_n_added * (Utility.Math.Divide(removed_from_lying, surfom_mass_added, 0.0)) * c.fr_pool_N[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].P = SurfOM[SOMNo].Lying[i].P + surfom_p_added * (Utility.Math.Divide(removed_from_lying, surfom_mass_added, 0.0)) * c.fr_pool_P[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].AshAlk = 0.0;
                        SurfOM[SOMNo].Standing[i].amount = SurfOM[SOMNo].Standing[i].amount + removed_from_standing * c.fr_pool_C[i, SOMNo];
                        SurfOM[SOMNo].Standing[i].C = SurfOM[SOMNo].Standing[i].C + removed_from_standing * c.C_fract[SOMNo] * c.fr_pool_C[i, SOMNo];
                        SurfOM[SOMNo].Standing[i].N = SurfOM[SOMNo].Standing[i].N + surfom_n_added * (Utility.Math.Divide(removed_from_standing, surfom_mass_added, 0.0)) * c.fr_pool_N[i, SOMNo];
                        SurfOM[SOMNo].Standing[i].P = SurfOM[SOMNo].Standing[i].P + surfom_p_added * (Utility.Math.Divide(removed_from_standing, surfom_mass_added, 0.0)) * c.fr_pool_P[i, SOMNo];
                        SurfOM[SOMNo].Standing[i].AshAlk = 0.0;
                    }
                }
                //Report Additions;
                if (report_additions == "yes")
                    summary.WriteMessage(FullPath, String.Format(
    @"Added SurfaceOM
    SurfaceOM name       = {0}
    SurfaceOM Type       = {1}
    Amount Added [kg/ha] = {2:0.0##}", SurfOM[SOMNo].name.Trim(), SurfOM[SOMNo].OrganicMatterType.Trim(), surfom_mass_added
                                         ));

                SendResAddedEvent(SurfOM[SOMNo].OrganicMatterType, SurfOM[SOMNo].OrganicMatterType, surfom_mass_added, surfom_n_added, surfom_p_added);

                //assumption is this event comes only from the manager for applying from an external source.
                surfom_ExternalMassFlow(
                    new ExternalMassFlowType()
                    {
                        PoolClass = "surface",
                        FlowType = "gain",
                        DM = surfom_mass_added,
                        C = surfom_c_added,
                        N = surfom_n_added + surfom_no3_added + surfom_nh4_added,
                        P = surfom_p_added + surfom_po4_added,
                        SW = 0.0
                    }

                );
            }
        }

        /// <summary>
        /// Adds excreta in response to an AddFaeces event
        /// This is a still the minimalist version, providing
        /// an alternative to using add_surfaceom directly
        /// </summary>
        /// <param name="data">structure holding description of the added faeces</param>
        private void AddFaeces(AddFaecesType data)
        {
            string Manure = "manure";
            AddSurfaceOM((double)(data.OMWeight * _fractionFaecesAdded),
                         (double)(data.OMN * _fractionFaecesAdded),
                         (double)(data.OMP * _fractionFaecesAdded),
                         Manure);

            // We should also have added ash alkalinity, but AddSurfaceOM_
            // doesn't have a field for that.
            // So let's add a bit of logic here to handle it...
            // We don't have a "fr_pool_ashalk" either, so we'll assume
            // it follows the same pattern as P.
            int SOMNo = GetSoluteNumber(Manure);

            if (SOMNo >= 0) // Should always be OK, following creation in AddSurfom
            {
                for (int i = 0; i < MaxFr; i++)
                {
                    SurfOM[SOMNo].Lying[i].AshAlk += (double)(data.OMAshAlk * _fractionFaecesAdded * c.fr_pool_P[i, SOMNo]);
                }
            }

            // We should also handle sulphur someday....
        }

        /// <summary>
        /// Calculates surfom addition as a result of add_surfom message
        /// </summary>
        private void PropUp(Prop_upType data)
        {
            int SOMNo = 0;                      //surfaceom pool number;
            double

                old_standing = 0,	        //previous standing residue mass in specified pool;
                old_lying = 0,	            //previous lying residue mass in specified pool;
                tot_mass = 0,	            //total mass of specified residue pool;
                new_standing = 0,	        //new standing residue mass in specified pool;
                new_lying = 0,	            //new lying residue mass in specified pool;
                standing_change_fract = 0,	//fractional change to standing material in specified residue pool;
                lying_change_fract = 0;	    //fractional change to lying material in specified residue pool;

            SOMNo = GetSoluteNumber(data.name);
            if (SOMNo < 0)
                throw new Exception("SurfaceOM residue name unknown. Cannot Prop up");

            old_lying = SurfOM[SOMNo].Lying.Sum<OMFractionType>(x => x.amount);
            old_standing = SurfOM[SOMNo].Standing.Sum<OMFractionType>(x => x.amount);

            tot_mass = old_standing + old_lying;
            new_standing = tot_mass * data.standing_fract;
            new_lying = tot_mass - new_standing;

            if (old_standing > 0.0)
            {
                standing_change_fract = Utility.Math.Divide(new_standing, old_standing, 0.0);
                lying_change_fract = Utility.Math.Divide(new_lying, old_lying, 0.0);

                for (int i = 0; i < MaxFr; i++)
                {
                    SurfOM[SOMNo].Standing[i].amount = SurfOM[SOMNo].Standing[i].amount * standing_change_fract;
                    SurfOM[SOMNo].Standing[i].C = SurfOM[SOMNo].Standing[i].C * standing_change_fract;
                    SurfOM[SOMNo].Standing[i].N = SurfOM[SOMNo].Standing[i].N * standing_change_fract;
                    SurfOM[SOMNo].Standing[i].P = SurfOM[SOMNo].Standing[i].P * standing_change_fract;
                    SurfOM[SOMNo].Standing[i].AshAlk = SurfOM[SOMNo].Standing[i].AshAlk * standing_change_fract;
                    SurfOM[SOMNo].Lying[i].amount = SurfOM[SOMNo].Lying[i].amount * lying_change_fract;
                    SurfOM[SOMNo].Lying[i].C = SurfOM[SOMNo].Lying[i].C * lying_change_fract;
                    SurfOM[SOMNo].Lying[i].N = SurfOM[SOMNo].Lying[i].N * lying_change_fract;
                    SurfOM[SOMNo].Lying[i].P = SurfOM[SOMNo].Lying[i].P * lying_change_fract;
                    SurfOM[SOMNo].Lying[i].AshAlk = SurfOM[SOMNo].Lying[i].AshAlk * lying_change_fract;
                }

            }
            else
            {
                lying_change_fract = Utility.Math.Divide(new_lying, old_lying, 0.0);
                for (int i = 0; i < MaxFr; i++)
                {
                    SurfOM[SOMNo].Standing[i].amount = SurfOM[SOMNo].Lying[i].amount * (1.0 - lying_change_fract);
                    SurfOM[SOMNo].Standing[i].C = SurfOM[SOMNo].Lying[i].C * (1.0 - lying_change_fract);
                    SurfOM[SOMNo].Standing[i].N = SurfOM[SOMNo].Lying[i].N * (1.0 - lying_change_fract);
                    SurfOM[SOMNo].Standing[i].P = SurfOM[SOMNo].Lying[i].P * (1.0 - lying_change_fract);
                    SurfOM[SOMNo].Standing[i].AshAlk = SurfOM[SOMNo].Lying[i].AshAlk * (1.0 - lying_change_fract);
                    SurfOM[SOMNo].Lying[i].amount = SurfOM[SOMNo].Lying[i].amount * lying_change_fract;
                    SurfOM[SOMNo].Lying[i].C = SurfOM[SOMNo].Lying[i].C * lying_change_fract;
                    SurfOM[SOMNo].Lying[i].N = SurfOM[SOMNo].Lying[i].N * lying_change_fract;
                    SurfOM[SOMNo].Lying[i].P = SurfOM[SOMNo].Lying[i].P * lying_change_fract;
                    SurfOM[SOMNo].Lying[i].AshAlk = SurfOM[SOMNo].Lying[i].AshAlk * lying_change_fract;
                }
            }

            //Report Additions;
            if (report_additions == "yes")
                summary.WriteMessage(FullPath, String.Format(
    @"Propped-up SurfaceOM
    SurfaceOM name        = {0}
    SurfaceOM Type        = {1}
    New Standing Fraction = {2}", SurfOM[SOMNo].name.Trim(), SurfOM[SOMNo].OrganicMatterType.Trim(), data.standing_fract
                                    ));

        }

        /// <summary>
        /// Reads type-specific residue constants from ini-file and places them in c. constants;
        /// </summary>
        /// <param name="surfom_type"></param>
        /// <param name="i"></param>
        private void ReadTypeSpecificConstants(string surfom_type, int i, out double pot_decomp_rate)
        {
            ResidueType thistype = residueTypes.getResidue(surfom_type);
            if (thistype == null)
                throw new ApsimXException(FullPath, "Cannot find residue type description for '" + surfom_type + "'");

            c.C_fract[i] = Utility.Math.Bound(thistype.fraction_C, 0.0, 1.0);
            c.po4ppm[i] = Utility.Math.Bound(thistype.po4ppm, 0.0, 1000.0);
            c.nh4ppm[i] = Utility.Math.Bound(thistype.nh4ppm, 0.0, 2000.0);
            c.no3ppm[i] = Utility.Math.Bound(thistype.no3ppm, 0.0, 1000.0);
            c.specific_area[i] = Utility.Math.Bound(thistype.specific_area, 0.0, 0.01);
            c.cf_contrib[i] = Bound(thistype.cf_contrib, 0, 1);
            pot_decomp_rate = Utility.Math.Bound(thistype.pot_decomp_rate, 0.0, 1.0);

            if (thistype.fr_c.Length != thistype.fr_n.Length || thistype.fr_n.Length != thistype.fr_p.Length)
                throw new Exception("Error reading in fr_c/n/p values, inconsistent array lengths");

            for (int j = 0; j < thistype.fr_c.Length; j++)
            {
                c.fr_pool_C[j, i] = thistype.fr_c[j];
                c.fr_pool_N[j, i] = thistype.fr_n[j];
                c.fr_pool_P[j, i] = thistype.fr_p[j];
            }
        }

        /// <summary>
        /// <para>Purpose;</para>
        /// <para>
        /// This function returns the fraction of the soil surface covered by;
        /// residue according to the relationship from Gregory (1982).
        /// </para>
        /// <para>Notes;</para>
        /// <para>Gregory"s equation is of the form;</para>
        /// <para>        Fc = 1.0 - exp (- Am * M)   where Fc = Fraction covered;</para>
        /// <para>                                          Am = Specific Area (ha/kg)</para>
        /// <para>                                           M = Mulching rate (kg/ha)</para>
        /// <para>This residue model keeps track of the total residue area and so we can
        /// substitute this value (area residue/unit area) for the product_of Am * M.</para>
        /// </summary>
        /// <param name="SOMindex"></param>
        /// <returns></returns>
        private double Cover(int SOMindex)
        {
            double F_Cover;	            //Fraction of soil surface covered by residue (0-1)
            double Area_lying;	        //area of lying component;
            double Area_standing;	    //effective area of standing component (the 0.5 extinction coefficient in area calculation
            //  provides a random distribution in degree to which standing stubble is "lying over"

            //calculate fraction of cover and check bounds (0-1).  Bounds checking;
            //is required only for detecting internal rounding error.
            double sum_stand_amount = 0, sum_lying_amount = 0;
            for (int i = 0; i < MaxFr; i++)
            {
                sum_lying_amount += SurfOM[SOMindex].Lying[i].amount;
                sum_stand_amount += SurfOM[SOMindex].Standing[i].amount;
            }

            Area_lying = c.specific_area[SOMindex] * sum_lying_amount;
            Area_standing = c.specific_area[SOMindex] * sum_stand_amount;

            F_Cover = AddCover(1.0 - (double)Math.Exp(-Area_lying), 1.0 - (double)Math.Exp(-(_standing_extinct_coeff) * Area_standing));
            F_Cover = Utility.Math.Bound(F_Cover, 0.0, 1.0);

            return F_Cover;
        }

        /// <summary>
        /// Check that soil phosphorus is in system
        /// </summary>
        private void SetPhosphorusAware()
        {
            phosphorus_aware = labile_p != null;
        }

        /// <summary>
        /// Get information on surfom added from the crops
        /// </summary>
        /// <param name="BiomassRemoved"></param>
        private void SurfOMOnBiomassRemoved(BiomassRemovedType BiomassRemoved)
        {
            double
                surfom_added = 0,	//amount of residue added (kg/ha)
                surfom_N_added = 0,	//amount of residue N added (kg/ha)
                surfom_P_added = 0;	//amount of residue N added (kg/ha)

            if (BiomassRemoved.fraction_to_residue.Sum() != 0)
            {
                //Find the amount of surfom to be added today;
                for (int i = 0; i < BiomassRemoved.fraction_to_residue.Length; i++)
                    surfom_added += BiomassRemoved.dlt_crop_dm[i] * BiomassRemoved.fraction_to_residue[i];

                if (surfom_added > 0.0)
                {
                    //Find the amount of N & added in surfom today;
                    for (int i = 0; i < BiomassRemoved.dlt_dm_p.Length; i++)
                    {
                        surfom_P_added += BiomassRemoved.dlt_dm_p[i] * BiomassRemoved.fraction_to_residue[i];
                        surfom_N_added += BiomassRemoved.dlt_dm_n[i] * BiomassRemoved.fraction_to_residue[i];
                    }

                    AddSurfaceOM(surfom_added, surfom_N_added, surfom_P_added, BiomassRemoved.crop_type);
                }
            }
        }

        private void AddSurfaceOM(double surfom_added, double surfom_N_added, double surfom_P_added, string crop_type)
        {
            int SOMNo;      //system number of the surface organic matter added;

            //Report Additions;
            if (report_additions == "yes")
            {
                summary.WriteMessage(FullPath, String.Format(

    @"Added surfom
    SurfaceOM Type          = {0}
    Amount Added [kg/ha]    = {1}", crop_type.TrimEnd(), surfom_added));
            }

            //Assume the "crop_type" is the unique name.  Now check whether this unique "name" already exists in the system.
            SOMNo = GetSoluteNumber(crop_type);

            if (SOMNo < 0)
            {
                if (SurfOM == null)
                    SurfOM = new List<SurfOrganicMatterType>();

                SurfOM.Add(new SurfOrganicMatterType(crop_type, crop_type));
                SOMNo = num_surfom - 1;

                //NOW UPDATE ALL VARIABLES;
                ReadTypeSpecificConstants(SurfOM[SOMNo].OrganicMatterType, SOMNo, out SurfOM[SOMNo].PotDecompRate);
            }
            // else THIS ADDITION IS AN EXISTING COMPONENT OF THE SURFOM SYSTEM;

            //convert the ppm figures into kg/ha;
            SurfOM[SOMNo].no3 += Utility.Math.Divide(c.no3ppm[SOMNo], 1000000.0, 0.0) * surfom_added;
            SurfOM[SOMNo].nh4 += Utility.Math.Divide(c.nh4ppm[SOMNo], 1000000.0, 0.0) * surfom_added;
            SurfOM[SOMNo].po4 += Utility.Math.Divide(c.po4ppm[SOMNo], 1000000.0, 0.0) * surfom_added;

            //Assume all surfom added is in the LYING pool, ie No STANDING component;
            for (int i = 0; i < MaxFr; i++)
            {
                SurfOM[SOMNo].Lying[i].amount += surfom_added * c.fr_pool_C[i, SOMNo];
                SurfOM[SOMNo].Lying[i].C += surfom_added * c.C_fract[SOMNo] * c.fr_pool_C[i, SOMNo];
                SurfOM[SOMNo].Lying[i].N += surfom_N_added * c.fr_pool_N[i, SOMNo];
                SurfOM[SOMNo].Lying[i].P += surfom_P_added * c.fr_pool_P[i, SOMNo];
                SurfOM[SOMNo].Lying[i].AshAlk = 0.0;
            }

            SendResAddedEvent(SurfOM[SOMNo].OrganicMatterType, SurfOM[SOMNo].OrganicMatterType, surfom_added, surfom_N_added, surfom_P_added);
        }

        /// <summary>
        /// Notify other modules of residue added to residue pool.
        /// </summary>
        /// <param name="residue_type"></param>
        /// <param name="dm_type"></param>
        /// <param name="dlt_residue_wt"></param>
        /// <param name="dlt_residue_N_wt"></param>
        /// <param name="dlt_residue_P_wt"></param>
        private void SendResAddedEvent(string residue_type, string dm_type, double dlt_residue_wt, double dlt_residue_N_wt, double dlt_residue_P_wt)
        {
            if (Residue_added != null)
            {
                ResidueAddedType data = new ResidueAddedType()
                {
                    residue_type = residue_type,
                    dm_type = dm_type,
                    dlt_residue_wt = dlt_residue_wt,
                    dlt_dm_n = dlt_residue_N_wt,
                    dlt_dm_p = dlt_residue_P_wt
                };

                Residue_added.Invoke(data);
            }
        }

        /// <summary>
        /// Notify other modules of residue removed from residue pool
        /// </summary>
        /// <param name="residue_removed_action"></param>
        /// <param name="dlt_residue_fraction"></param>
        /// <param name="residue_incorp_fraction"></param>
        /// <param name="deepest_layer"></param>
        private void SendResRemovedEvent(string residue_removed_action, double dlt_residue_fraction, double[] residue_incorp_fraction, int deepest_layer)
        {
            if (Residue_removed != null)
            {
                ResidueRemovedType data = new ResidueRemovedType()
                {
                    residue_removed_action = residue_removed_action,
                    dlt_residue_fraction = dlt_residue_fraction,
                    residue_incorp_fraction = residue_incorp_fraction
                };

                Residue_removed.Invoke(data);
            }
        }

        /// <summary>
        /// Notify other modules of residue added to residue pool
        /// </summary>
        /// <param name="residue_type"></param>
        /// <param name="dm_type"></param>
        /// <param name="dlt_residue_wt"></param>
        /// <param name="dlt_residue_N_wt"></param>
        /// <param name="dlt_residue_P_wt"></param>
        private void surfom_Send_SOM_removed_Event(string residue_type, string dm_type, double dlt_residue_wt, double dlt_residue_N_wt, double dlt_residue_P_wt)
        {
            if (SurfaceOM_removed != null)
            {
                SurfaceOMRemovedType data = new SurfaceOMRemovedType()
                {
                    SurfaceOM_type = residue_type,
                    SurfaceOM_dm_type = dm_type,
                    dlt_SurfaceOM_wt = dlt_residue_wt,
                    SurfaceOM_dlt_dm_n = dlt_residue_N_wt,
                    SurfaceOM_dlt_dm_p = dlt_residue_P_wt
                };

                SurfaceOM_removed.Invoke(data);
            }
        }
    }
}