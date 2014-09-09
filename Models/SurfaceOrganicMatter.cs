namespace Models.SurfaceOM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;
    using Models.Core;
    using Models.PMF;
    using Models.Soils;

    /// <summary>
    /// The surface organic matter model, ported by Ben Jolley from the
    /// Fortran version.
    /// Tidied up (somewhat) for ApsimX by Eric Zurcher, August 2014.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SurfaceOrganicMatter : Model
    {
        #region links to other components
        /// <summary>
        /// Link to the soil component.
        /// Used to retrieve values for eos and soil layer thicknesses.
        /// </summary>
        [Link]
        private Soil soil = null;

        /// <summary>
        /// Link to the summary component.
        /// Provides access to WriteWarning and WriteMessage.
        /// </summary>
        [Link]
        private ISummary summary = null;

        /// <summary>
        /// Link to the weather component.
        /// Provides access to temperature and rainfall values.
        /// </summary>
        [Link]
        private WeatherFile weather = null;
        #endregion

        // ====================================================================
        //    SurfaceOM constants;
        // ====================================================================

        /// <summary>
        /// Number of pools into which carbon is grouped.
        /// Currently there are three, indexed as follows: 
        ///  0 = carbohydrate
        ///  1 = cellulose
        ///  2 = lignin.
        /// </summary>
        private static int maxFr = 3;
        /// ================================================================

        /// ====================================================================

        
        // dsg 190803  The following two "types" have been defined within the code because,
        //             dean"s datatypes.f90 generator cannot yet properly generate the;
        //             type definition from datatypes.interface for a structures within a;
        //             structure.
        /// <summary>
        /// Type carrying information about the CNP composition of an organic matter fraction
        /// </summary>
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
                no3 = 0; 
                nh4 = 0; 
                po4 = 0;
                Standing = new OMFractionType[maxFr];
                Lying = new OMFractionType[maxFr];

                for (int i = 0; i < maxFr; i++)
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

        private int numSurfom = 0;

        private double irrig;
        private double eos;
        private double cumeos;
        private double[] dlayer;
        private bool phosphorusAware;
        private OMFractionType oldSOMState;
        private double DailyInitialC;
        private double DailyInitialN;

        /// <summary>
        /// standing fraction array
        /// </summary>
        public double[] _standing_fraction;
        public string ReportAdditions { get; set; }
        public string ReportRemovals { get; set; }

        public SurfaceOrganicMatter()
            : base()
        {
            // Set default values for some properties
            CriticalResidueWeight = 2000;
            OptimumDecompTemp = 20;
            MaxCumulativeEOS = 20;
            CNRatioDecompCoeff = 0.277;
            CNRatioDecompThreshold = 25;
            TotalLeachRain = 25;
            MinRainToLeach = 10;
            CriticalMinimumOrganicC = 0.004;
            DefaultCPRatio = 0.0;
            StandingExtinctCoeff = 0.5;
            FractionFaecesAdded = 0.5;
            ResidueTypes = new ResidueTypesList();
            Pools = new PoolTypesList();
        }

        #region exposed properties
        #region BensParams

        public ResidueTypesList ResidueTypes { get; set; }
        public TillageTypesList TillageTypes { get; set; }
        private PoolTypesList Pools { get; set; }

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
        public double CriticalResidueWeight { get; set; }

        /// <summary>
        /// temperature at which decomp reaches optimum (oC)
        /// </summary>
        [Units("oC")]
        public double OptimumDecompTemp { get; set; }

        /// <summary>
        /// cumeos at which decomp rate becomes zero. (mm H2O)
        /// </summary>
        [Units("")]
        public double MaxCumulativeEOS { get; set; }

        /// <summary>
        /// Coefficient to determine the magnitude of C:N effects on decomposition of residue
        /// </summary>
        [Units("")]
        public double CNRatioDecompCoeff { get; set; }

        /// <summary>
        /// C:N above which decomposition rate of residue declines
        /// </summary>
        [Units("")]
        public double CNRatioDecompThreshold { get; set; }

        /// <summary>
        /// total amount of "leaching" rain to remove all soluble N from surfom
        /// </summary>
        [Units("")]
        public double TotalLeachRain { get; set; }

        /// <summary>
        /// threshold rainfall amount for leaching to occur
        /// </summary>
        [Units("")]
        public double MinRainToLeach { get; set; }

        /// <summary>
        /// critical minimum org C below which potential decomposition rate is 100% (to avoid numerical imprecision)
        /// </summary>
        [Units("")]
        public double CriticalMinimumOrganicC { get; set; }

        /// <summary>
        /// Default C:P ratio
        /// </summary>
        [Units("")]
        public double DefaultCPRatio { get; set; }

        /// <summary>
        /// extinction coefficient for standing residues
        /// </summary>
        [Units("")]
        public double StandingExtinctCoeff { get; set; }

        /// <summary>
        /// fraction of incoming faeces to add
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 0.0)]
        [Units("0-1")]
        public double FractionFaecesAdded { get; set; }

        private int[] cf_contrib = new int[0];               // determinant of whether a residue type contributes to the calculation of contact factor (1 or 0)

        private double[] C_fract = new double[0];            // Fraction of Carbon in plant material (0-1)

        private double[,] frPoolC = new double[maxFr, 0];  // carbohydrate fraction in fom C pool (0-1)
        private double[,] frPoolN = new double[maxFr, 0];  // carbohydrate fraction in fom N pool (0-1)
        private double[,] frPoolP = new double[maxFr, 0];  // carbohydrate fraction in fom P pool (0-1)

        private double[] nh4ppm = new double[0];             // ammonium component of residue (ppm)
        private double[] no3ppm = new double[0];             // nitrate component of residue (ppm)
        private double[] po4ppm = new double[0];             // ammonium component of residue (ppm)
        private double[] specific_area = new double[0];      // specific area of residue (ha/kg)

        #endregion

        #region Inputs

        // [Units("mm")]
        // double eos = double.NaN;
        [Units("")]
        public string PondActive = null;

        [Units("")]
        public double[] labile_p = null;
        #endregion

        #region Outputs

        /// <summary>
        /// Total mass of all surface organic materials
        /// </summary>
        [Units("kg/ha")]
        public double surfaceom_wt { get { return SumSurfOMStandingLying(SurfOM, x => x.amount); } }

        [Units("kg/ha")]
        public double carbonbalance { get { return 0 - (surfaceom_c - DailyInitialC); } }

        [Units("kg/ha")]
        public double nitrogenbalance { get { return 0 - (surfaceom_n - DailyInitialN); } }

        /// <summary>
        /// Total mass of all surface organic carbon
        /// </summary>
        [Summary]
        [Description("Carbon content")]
        [Units("kg/ha")]
        public double surfaceom_c { get { return SumSurfOMStandingLying(SurfOM, x => x.C); } }

        /// <summary>
        /// Total mass of all surface organic nitrogen
        /// </summary>
        [Summary]
        [Description("Nitrogen content")]
        [Units("kg/ha")]
        public double surfaceom_n { get { return SumSurfOMStandingLying(SurfOM, x => x.N); } }

        /// <summary>
        /// Total mass of all surface organic phosphor
        /// </summary>
        [Summary]
        [Description("Phosphorus content")]
        [Units("kg/ha")]
        public double surfaceom_p { get { return SumSurfOMStandingLying(SurfOM, x => x.P); } }

        [Units("")]
        public double surfaceom_ashalk { get { return SumSurfOMStandingLying(SurfOM, x => x.P); } }

        /// <summary>
        /// Total mass of nitrate
        /// </summary>
        [Units("kg/ha")]
        public double surfaceom_no3 { get { return SumSurfOM(SurfOM, x => x.no3); } }

        /// <summary>
        /// Total mass of ammonium
        /// </summary>
        [Units("kg/ha")]
        public double surfaceom_nh4 { get { return SumSurfOM(SurfOM, x => x.nh4); } }

        /// <summary>
        /// Total mass of labile phosphorus
        /// </summary>
        [Units("kg/ha")]
        public double surfaceom_labile_p { get { return SumSurfOM(SurfOM, x => x.po4); } }

        /// <summary>
        /// Fraction of ground covered by all surface OMs
        /// </summary>
        [Description("Fraction of ground covered by all surface OMs")]
        [Units("m^2/m^2")]
        public double surfaceom_cover { get { return CoverTotal(); } }

        /// <summary>
        /// Temperature factor for decomposition
        /// </summary>
        [Units("0-1")]
        public double tf { get { return TemperatureFactor(); } }

        /// <summary>
        /// Contact factor for decomposition
        /// </summary>
        [Units("0-1")]
        public double cf { get { return ContactFactor(); } }

        [Units("0-1")]
        public double wf { get { return MoistureFactor(); } }

        private double _leaching_fr;
        [Units("")]
        public double leaching_fr { get { return _leaching_fr; } }

        [Units("")]
        public int surface_organic_matter 
        { 
            get 
            { 
                respond2get_SurfaceOrganicMatter(); 
                return 0; 
            } 
        }

        /// <summary>
        /// Mass of organic matter named wheat
        /// </summary>
        [Units("")]
        public double surfaceom_wt_rice { get { return ResidueMass("rice", x => x.amount); } }

        /// <summary>
        /// Mass of organic matter named algae
        /// </summary>
        [Units("")]
        public double surfaceom_wt_algae { get { return ResidueMass("algae", x => x.amount); } }

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

        /// <summary>
        /// Mass of organic matter in all pools
        /// </summary>
        [Units("kg/ha")]
        public double[] surfaceom_wt_all
        {
            get
            {
                return SurfOM.Select<SurfOrganicMatterType, double>(x =>
                        SumOMFractionType(x.Standing, y => y.amount) +
                        SumOMFractionType(x.Lying, y => y.amount)).ToArray<double>();
            }
        }

        /// <summary>
        /// Mass of organic carbon in all pools
        /// </summary>
        [Units("kg/ha")]
        public double[] surfaceom_c_all
        {
            get
            {
                return SurfOM.Select<SurfOrganicMatterType, double>(x =>
                        SumOMFractionType(x.Standing, y => y.C) +
                        SumOMFractionType(x.Lying, y => y.C)).ToArray<double>();
            }
        }

        /// <summary>
        /// Mass of organic nitrogen in all pools
        /// </summary>
        [Units("kg/ha")]
        public double[] surfaceom_n_all
        {
            get
            {
                return SurfOM.Select<SurfOrganicMatterType, double>(x =>
                        SumOMFractionType(x.Standing, y => y.N) +
                        SumOMFractionType(x.Lying, y => y.N)).ToArray<double>();
            }
        }

        /// <summary>
        /// Mass of organic phosphor in all pools
        /// </summary>
        [Units("kg/ha")]
        public double[] surfaceom_p_all
        {
            get
            {
                return SurfOM.Select<SurfOrganicMatterType, double>(x =>
                        SumOMFractionType(x.Standing, y => y.P) +
                        SumOMFractionType(x.Lying, y => y.P)).ToArray<double>();
            }
        }

        [Units("")]
        public double[] surfaceom_ashalk_all
        {
            get
            {
                return SurfOM.Select<SurfOrganicMatterType, double>(x =>
                        SumOMFractionType(x.Standing, y => y.AshAlk) +
                        SumOMFractionType(x.Lying, y => y.AshAlk)).ToArray<double>();
            }
        }

        /// <summary>
        /// Mass of nitrate in all pools
        /// </summary>
        [Units("")]
        public double[] surfaceom_no3_all
        {
            get
            {
                return SurfOM.Select<SurfOrganicMatterType, double>(x => x.no3).ToArray<double>();
            }
        }

        /// <summary>
        /// Mass of ammonium in all pools
        /// </summary>
        [Units("")]
        public double[] surfaceom_nh4_all
        {
            get
            {
                return SurfOM.Select<SurfOrganicMatterType, double>(x => x.nh4).ToArray<double>();
            }
        }

        /// <summary>
        /// Mass of labile phosphorus in all pools
        /// </summary>
        [Units("")]
        public double[] surfaceom_labile_p_all
        {
            get
            {
                return SurfOM.Select<SurfOrganicMatterType, double>(x => x.nh4).ToArray<double>();
            }
        }

        /// <summary>
        /// Potential organic C decomposition in all pools
        /// </summary>
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

        /// <summary>
        /// Potential organic N decomposition in all pools
        /// </summary>
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

        /// <summary>
        /// Potential organic P decomposition in all pools
        /// </summary>
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

        /// <summary>
        /// Fraction of all pools which is inert, ie not in contact with the ground
        /// </summary>
        [Units("")]
        public double[] standing_fr_all
        {
            get
            {
                return SurfOM.Select<SurfOrganicMatterType, double>(x => Utility.Math.Divide(x.Standing[0].amount, x.Standing[0].amount + x.Lying[0].amount, 0)).ToArray<double>();
            }
        }

        /// <summary>
        /// Fraction of ground covered by all pools
        /// </summary>
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

        /// <summary>
        /// C:N ratio factor for decomposition for all pools
        /// </summary>
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

        public override void OnDeserialised(bool xmlSerialisation)
        {
            if (xmlSerialisation)
            {
                if (ResidueTypes == null)
                    ResidueTypes = new ResidueTypesList();
                if (!string.IsNullOrEmpty(ResidueTypes.LoadFromResource))
                {
                    string xml = Properties.Resources.ResourceManager.GetString(ResidueTypes.LoadFromResource);
                    if (xml != null)
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xml);
                        ResidueTypesList ValuesFromResource = Utility.Xml.Deserialise(doc.DocumentElement) as ResidueTypesList;
                        if (ValuesFromResource != null)
                        {
                            foreach (ResidueType residueType in ValuesFromResource.residues)
                            {
                                residueType.IsHidden = true;
                                ResidueTypes.residues.Add(residueType);
                            }
                        }
                    }
                }
                ResidueTypes.FillAllDerived();
            }
        }

        private ResidueType[] savedResidues;

        /// <summary>
        /// We're about to be serialised. Remove residue types from out list
        /// that were obtained from the XML resource so they are not included
        /// </summary>
        public override void OnSerialising(bool xmlSerialisation)
        {
            if (xmlSerialisation)
            {
                savedResidues = ResidueTypes.residues.ToArray();
                for (int i = ResidueTypes.residues.Count - 1; i >= 0; i--)
                {
                    if (ResidueTypes.residues[i].IsHidden)
                        ResidueTypes.residues.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Serialisation has completed. Reinstate the full list of 
        /// residue types
        /// </summary>
        public override void OnSerialised(bool xmlSerialisation)
        {
            if (xmlSerialisation && savedResidues != null)
            {
                ResidueTypes.residues = savedResidues.ToList();
                savedResidues = null;
            }
        }

        #region supporting classes

        [Serializable]
        public class ResidueType : Model
        {
            public string fom_type { get; set; }
            public string derived_from { get; set; } // No logic for this implemented currently
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
                InitialiseWithNulls();
            }

            public ResidueType(string fomType)
            {
                fom_type = fomType;
                InitialiseWithNulls();
            }

            private void InitialiseWithNulls()
            {
                fraction_C = double.NaN;
                po4ppm = double.NaN;
                nh4ppm = double.NaN;
                no3ppm = double.NaN;
                specific_area = double.NaN;
                cf_contrib = -1;
                pot_decomp_rate = double.NaN;
                fr_c = null;
                fr_n = null;
                fr_p = null;
            }
        }

        [Serializable]
        public class ResidueTypesList : Model
        {
            [XmlElement("ResidueType")]
            public List<ResidueType> residues { get; set; }

            public string LoadFromResource { get; set; }

            public void FillAllDerived()
            {
                for (int i = 0; i < residues.Count; i++)
                {
                    FillDerived(i);
                }
            }

            /// <summary>
            /// Fills in field values for a residue type from a base type
            /// </summary>
            /// <param name="i">List index of the residue type to be filled in</param>
            /// </param>
            public void FillDerived(int i)
            {
                ResidueType residue = residues[i];
                ResidueType refResidue = null;
                if (!string.IsNullOrEmpty(residue.derived_from))
                {
                    for (int j = 0; j < residues.Count; j++)
                    {
                        if (residues[j].fom_type.Equals(residue.derived_from, StringComparison.CurrentCultureIgnoreCase))
                        {
                            FillDerived(j); // Make sure the template has itself been filled
                            refResidue = residues[j];
                            break;
                        }
                    }
                }
                if (double.IsNaN(residue.fraction_C))
                {
                    residue.fraction_C = refResidue != null ? refResidue.fraction_C : 0.4;
                }
                if (double.IsNaN(residue.po4ppm))
                {
                    residue.po4ppm = refResidue != null ? refResidue.po4ppm : 0.0;
                }
                if (double.IsNaN(residue.nh4ppm))
                {
                    residue.nh4ppm = refResidue != null ? refResidue.nh4ppm : 0.0;
                }
                if (double.IsNaN(residue.no3ppm))
                {
                    residue.no3ppm = refResidue != null ? refResidue.no3ppm : 0.0;
                }
                if (double.IsNaN(residue.specific_area))
                {
                    residue.specific_area = refResidue != null ? refResidue.specific_area : 0.0005;
                }
                if (double.IsNaN(residue.pot_decomp_rate))
                {
                    residue.pot_decomp_rate = refResidue != null ? refResidue.pot_decomp_rate : 0.1;
                }
                if (residue.cf_contrib < 0)
                {
                    residue.cf_contrib = refResidue != null ? refResidue.cf_contrib : 1;
                }
                if (residue.fr_c == null)
                {
                    residue.fr_c = refResidue != null ? (double[])refResidue.fr_c.Clone() : new double[3] { 0.2, 0.7, 0.1 };
                }
                if (residue.fr_n == null)
                {
                    residue.fr_n = refResidue != null ? (double[])refResidue.fr_n.Clone() : new double[3] { 0.2, 0.7, 0.1 };
                }
                if (residue.fr_p == null)
                {
                    residue.fr_p = refResidue != null ? (double[])refResidue.fr_p.Clone() : new double[3] { 0.2, 0.7, 0.1 };
                }
            }

            public ResidueTypesList()
            {
                if (residues == null)
                    residues = new List<ResidueType>();

                residues.Clear();
                LoadFromResource = "ResidueTypes";
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
        public class TillageTypesList : Model
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

        [Serializable]
        public class PoolType : Model
        {
            public string PoolName { get; set; }
            public string ResidueType { get; set; }
            public double Mass { get; set; }
            public double CNRatio { get; set; }
            public double CPRatio { get; set; }
            public double StandingFraction { get; set; }
        }

        [Serializable]
        public class PoolTypesList : Model
        {
            public List<PoolType> Pool { get; set; }
        }

        private const double acceptableErr = 1e-4;

        #region Math Operations

        private int Bound(int tobound, int lower, int upper)
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

        private const string ApsimBoundWarningError =
    @"'{0}' out of bounds!
     {1} < {2} < {3} evaluates 'FALSE'";

        private void Bound_check_real_var(double value, double lower, double upper, string vname)
        {
            if (Utility.Math.IsLessThan(value, lower) || Utility.Math.IsGreaterThan(value, upper))
                summary.WriteWarning(FullPath, string.Format(ApsimBoundWarningError, vname, lower, value, upper));
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
            int sizeOf = array.Length - 1;
            double cum = 0;
            for (int i = 0; i < sizeOf; i++)
                if ((cum += array[i]) >= cum_sum)
                    return i;
            return sizeOf;
        }

        #endregion

        protected T[] ToArray<T>(string str)
        {
            string[] temp;

            if (str == null || str == string.Empty)
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

        double ResidueMass(string type, Func<OMFractionType, double> func)
        {
            int SOMNo = GetResidueNumber(type);
            if (SOMNo > 0)
                return SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying.Sum<OMFractionType>(func) + x.Standing.Sum<OMFractionType>(func));
            else
                throw new ApsimXException(FullPath, "No organic matter called " + type + " present");
        }

        #endregion

        /// <summary>
        /// Initialise residue module
        /// </summary>
        private void SurfomReset()
        {
            // Save State;
            SaveState();
            ZeroVariables();
            ReadParam();

            // Change of State;
            DeltaState();
        }

        private void SaveState()
        {
            oldSOMState = SurfomTotalState();
        }

        private void DeltaState()
        {
            // Local Variables;
            OMFractionType newSOMState = SurfomTotalState();
            Models.Soils.ExternalMassFlowType massBalanceChange = new Models.Soils.ExternalMassFlowType();

            // - Implementation Section ----------------------------------
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

            SendExternalMassFlow(massBalanceChange);

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

        private void PublishFOMPool(FOMPoolType data)
        {
            if (IncorpFOMPool != null)
                IncorpFOMPool.Invoke(data);
        }

        public class SurfaceOrganicMatterPoolType
        {
            public string Name = string.Empty;
            public string OrganicMatterType = string.Empty;
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
            public string residue_removed_action = string.Empty;
            public double dlt_residue_fraction;
            public double[] residue_incorp_fraction;
        }

        public class ResidueAddedType
        {
            public string residue_type = string.Empty;
            public string dm_type = string.Empty;
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
            public string SurfaceOM_type = string.Empty;
            public string SurfaceOM_dm_type = string.Empty;
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
            public string name = string.Empty;
            public string type = string.Empty;
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
            if (ResidueTypes == null)
                ResidueTypes = new ResidueTypesList();
            if (TillageTypes == null)
                TillageTypes = new TillageTypesList();
            SurfOM = null;
            MetData = new Models.WeatherFile.NewMetType();
            irrig = 0;
            eos = 0;
            cumeos = 0;
            dlayer = new double[0];
            _leaching_fr = 0;
            phosphorusAware = false;
            PondActive = "no";
            oldSOMState = new OMFractionType();
            ZeroVariables();
            OnReset();
        }

        [EventSubscribe("Reset")]
        private void OnReset()
        {
            initialised = true; 
            SurfomReset();
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
            // now increment internal irrigation log;
            irrig += data.Amount;
        }

        public class CropChoppedType
        {
            public string crop_type = string.Empty;
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
            double surfom_added = 0;    // amount of residue added (kg/ha)
            double surfom_N_added = 0;  // amount of residue N added (kg/ha)
            double surfom_P_added = 0;  // amount of residue N added (kg/ha)

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

                    // Find the amount of P added in surfom today, if phosphorus aware;
                    if (phosphorusAware)
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

        private void SendExternalMassFlow(ExternalMassFlowType massBalanceChange)
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

            for (int pool = 0; pool < maxFr; pool++)
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

            phosphorusAware = false;
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
            if (PondActive == null || PondActive.Length < 1)
                PondActive = "no";
        }

        /// <summary>
        /// Read in all parameters from parameter file
        /// <para>
        /// This now just modifies the inputs and puts them into global structs, reading in is handled by .NET
        /// </para>
        /// </summary>
        private void ReadParam()
        {
            // Local Variables;
            string[] tempName = ToArray<string>(PoolName);   // temporary array for residue names;
            string[] tempType = ToArray<string>(type);       // temporary array for residue types;
            double[] tempMass = ToArray<double>(mass);
            double[] tempResidueCNr = ToArray<double>(cnr); // temporary residue_cnr array;
            double[] tempResidueCPr = ToArray<double>(cpr); // temporary residue_cpr array;
            double[] tempStandingFraction = ToArray<double>(standing_fraction);

            // Read in residue type from parameter file;
            //        ------------
            if (tempName.Length != tempType.Length)
                throw new ApsimXException(FullPath, "Residue types and names do not match");

            // Read in residue weight from parameter file;
            //        --------------
            if (tempName.Length != tempMass.Length)
            {
                throw new ApsimXException(FullPath, "Number of residue names and weights do not match");
            }

            double[] totC = new double[tempName.Length];  // total C in residue;
            double[] totN = new double[tempName.Length];  // total N in residue;
            double[] totP = new double[tempName.Length];  // total P in residue;

            _standing_fraction = tempStandingFraction;

            // ASSUMING that a value of 0 here means that no C:P ratio was set
            // ASSUMING that this will only be called with a single value in tempResidueCPr (as was initially coded - the only reason we are using arrays is because that was how the FORTRAN did it)
            phosphorusAware = tempResidueCPr.Length > 0 && tempResidueCPr[0] > 0;

            if (!phosphorusAware || tempResidueCPr.Length < tempResidueCNr.Length)
            {
                double[] temp = new double[tempResidueCNr.Length];
                tempResidueCPr.CopyTo(temp, 0);
                for (int i = tempResidueCPr.Length; i < tempResidueCNr.Length; i++)
                    temp[i] = DefaultCPRatio;
                tempResidueCPr = temp;
            }

            if (ReportAdditions == null || ReportAdditions.Length == 0)
                ReportAdditions = "no";


            if (ReportRemovals == null || ReportRemovals.Length == 0)
                ReportRemovals = "no";

            // NOW, PUT ALL THIS INFO INTO THE "SurfaceOM" STRUCTURE;

            DailyInitialC = DailyInitialN = 0;

            // SurfOM = new List<SurfOrganicMatterType>();

            for (int i = 0; i < tempName.Length; i++)
            {
                int SOMNo = AddNewSurfOM(tempName[i], tempType[i]);

                // convert the ppm figures into kg/ha;
                SurfOM[SOMNo].no3 = Utility.Math.Divide(no3ppm[i], 1000000.0, 0.0) * tempMass[i];
                SurfOM[SOMNo].nh4 = Utility.Math.Divide(nh4ppm[i], 1000000.0, 0.0) * tempMass[i];
                SurfOM[SOMNo].po4 = Utility.Math.Divide(po4ppm[i], 1000000.0, 0.0) * tempMass[i];

                totC[i] = tempMass[i] * C_fract[i];
                totN[i] = Utility.Math.Divide(totC[i], tempResidueCNr[i], 0.0);
                totP[i] = Utility.Math.Divide(totC[i], tempResidueCPr[i], 0.0);

                for (int j = 0; j < maxFr; j++)
                {
                    SurfOM[i].Standing[j] = new OMFractionType()
                        {
                            amount = tempMass[i] * frPoolC[j, i] * _standing_fraction[i],
                            C = totC[i] * frPoolC[j, i] * _standing_fraction[i],
                            N = totN[i] * frPoolN[j, i] * _standing_fraction[i],
                            P = totP[i] * frPoolP[j, i] * _standing_fraction[i],
                            AshAlk = 0.0
                        };

                    SurfOM[i].Lying[j] = new OMFractionType()
                        {
                            amount = tempMass[i] * frPoolC[j, i] * (1.0 - _standing_fraction[i]),
                            C = totC[i] * frPoolC[j, i] * (1.0 - _standing_fraction[i]),
                            N = totN[i] * frPoolN[j, i] * (1.0 - _standing_fraction[i]),
                            P = totP[i] * frPoolP[j, i] * (1.0 - _standing_fraction[i]),
                            AshAlk = 0.0
                        };
                }

                DailyInitialC = totC.Sum();
                DailyInitialN = totN.Sum();
            }
        }


        /// <summary>
        /// Get the solutes number
        /// </summary>
        /// <param name="surfomname"></param>
        /// <returns></returns>
        private int GetResidueNumber(string surfomname)
        {
            if (SurfOM == null)
                return -1;

            for (int i = 0; i < SurfOM.Count; i++)
                if (SurfOM[i].name.Equals(surfomname, StringComparison.CurrentCultureIgnoreCase))
                    return i;

            return -1;
        }

        // surfom_ONnewmet used to sit here but it's now defunct thanks to newer APSIM methods :)

        /// <summary>
        /// Performs manure decomposition taking into account environmental;
        ///  and manure factors (independant to soil N but N balance can modify;
        ///  actual decomposition rates if desired by N model - this is possible;
        ///  because pools are not updated until end of time step - see post routine)
        /// </summary>
        /// <returns>pool decompositions for all residues</returns>
        private void PotDecomp(out double[] c_pot_decomp, out double[] n_pot_decomp, out double[] p_pot_decomp)
        {
            // (these pools are not updated until end of time step - see post routine)
            c_pot_decomp = new double[numSurfom];
            n_pot_decomp = new double[numSurfom];
            p_pot_decomp = new double[numSurfom];

            // Local Variables;

            double
                mf = MoistureFactor(),     // moisture factor for decomp (0-1)
                tf = TemperatureFactor(),  // temperature factor for decomp (0-1)
                cf = ContactFactor();      // manure/soil contact factor for decomp (0-1)

            for (int residue = 0; residue < numSurfom; residue++)
            {
                double cnrf = CNRatioFactor(residue);    // C:N factor for decomp (0-1) for surfom under consideration

                double
                    Fdecomp = -1,       // decomposition fraction for the given surfom
                    sumC = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C);

                if (sumC < CriticalMinimumOrganicC)
                {
                    // Residue wt is sufficiently low to suggest decomposing all;
                    // material to avoid low numbers which can cause problems;
                    // with numerical precision;
                    Fdecomp = 1;
                }
                else
                {
                    // Calculate today"s decomposition  as a fraction of potential rate;
                    Fdecomp = SurfOM[residue].PotDecompRate * mf * tf * cnrf * cf;
                }

                // Now calculate pool decompositions for this residue;
                c_pot_decomp[residue] = Fdecomp * sumC;
                n_pot_decomp[residue] = Fdecomp * SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N);
                p_pot_decomp[residue] = Fdecomp * SurfOM[residue].Lying.Sum<OMFractionType>(x => x.P);
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
                ave_temp = Utility.Math.Divide((MetData.Maxt + MetData.Mint), 2.0, 0.0);  // today"s average air temp (oC)
            // tf;  // temperature factor;

            if (ave_temp > 0.0)
                return Utility.Math.Bound(
                    (double)Math.Pow(Utility.Math.Divide(ave_temp, OptimumDecompTemp, 0.0), 2.0),
                    0.0,
                    1.0);
            else
                return 0;
        }

        /// <summary>
        /// Calculate manure/soil contact factor for manure decomposition (0-1).
        /// </summary>
        /// <returns></returns>
        private double ContactFactor()
        {
            double effSurfomMass = 0;  // Total residue wt across all instances;

            // Sum the effective mass of surface residues considering lying fraction only.
            // The "effective" weight takes into account the haystack effect and is governed by the;
            // cf_contrib factor (ini file).  ie some residue types do not contribute to the haystack effect.

            for (int residue = 0; residue < numSurfom; residue++)
                effSurfomMass += SurfOM[residue].Lying.Sum<OMFractionType>(x => x.amount) * cf_contrib[residue];

            if (effSurfomMass <= CriticalResidueWeight)
                return 1.0;
            else
                return Utility.Math.Bound(Utility.Math.Divide(CriticalResidueWeight, effSurfomMass, 0.0), 0, 1);
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
                // Note: C:N ratio factor only based on lying fraction
                double
                    total_C = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C),        // organic C component of this residue (kg/ha)
                    total_N = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N),        // organic N component of this residue (kg/ha)
                    total_mineral_n = SurfOM[residue].no3 + SurfOM[residue].nh4,          // mineral N component of this surfom (no3 + nh4)(kg/ha)
                    cnr = Utility.Math.Divide(total_C, (total_N + total_mineral_n), 0.0); // C:N for this residue  (unitless)

                // As C:N increases above optcn cnrf decreases exponentially toward zero;
                // As C:N decreases below optcn cnrf is constrained to one;

                if (CNRatioDecompThreshold == 0)
                {
                    return 1;
                }
                else
                {
                    return Utility.Math.Bound(
                        (double)Math.Exp(-CNRatioDecompCoeff * ((cnr - CNRatioDecompThreshold) / CNRatioDecompThreshold)),
                        0.0,
                        1.0);
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

               if (PondActive=="yes") {

                  //mf will always be 0.5, to recognise that potential decomp is less under flooded conditions;

                 return 0.5;

               }else{
                  //not flooded conditions

                  //moisture factor decreases from 1. at start of cumeos and decreases;
                  //linearly to zero at MaxCumulativeEOS;

                 mf = 1.0 - Utility.Math.Divide (cumeos, c.MaxCumulativeEOS, 0.0);

                 mf = Utility.Math.Bound(mf, 0.0, 1.0);
            return mf;
             * }
                 */

            // optimisation of above code:
            if (PondActive == "yes")
            {
                return 0.5;
            }
            else
            {
                return Utility.Math.Bound(1.0 - Utility.Math.Divide(cumeos, MaxCumulativeEOS, 0.0), 0.0, 1.0);
            }
        }

        /// <summary>
        /// Calculate total cover
        /// </summary>
        /// <returns></returns>
        private double CoverTotal()
        {
            double combinedCover = 0;  // effective combined cover(0-1)

            for (int i = 0; i < numSurfom; i++)
                combinedCover = AddCover(combinedCover, Cover(i));

            return combinedCover;
        }

        /// <summary>
        /// Perform actions for current day.
        /// </summary>
        private SurfaceOrganicMatterDecompType Process()
        {
            double leachRain = 0; // "leaching" rainfall (if rain>10mm)

            SetPhosphorusAware();

            SetVars(out cumeos, out leachRain);

            if (leachRain > 0.0)
            {
                Leach(leachRain);
            }
            // else no mineral N or P is leached today;

            return SendPotDecompEvent();

        }


        /// <summary>
        /// Calculates variables required for today"s decomposition and;
        /// leaching factors.
        /// </summary>
        /// <param name="cumeos"></param>
        /// <param name="leachRain"></param>
        private void SetVars(out double cumeos, out double leachRain)
        {
            double precip = MetData.Rain + irrig;  // daily precipitation (rain + irrigation) (mm H2O)

            if (precip > 4.0)
            {
                // reset cumulative to just today"s eos
                cumeos = eos - precip;
            }
            else
            {
                // keep accumulating eos
                cumeos = this.cumeos + eos - precip;
            }
            cumeos = Math.Max(cumeos, 0.0);

            if (precip >= MinRainToLeach)
            {
                leachRain = precip;
            }
            else
            {
                leachRain = 0.0;
            }

            // reset irrigation log now that we have used that information;
            irrig = 0.0;
        }


        /// <summary>
        /// Remove mineral N and P from surfom with leaching rainfall and;
        /// pass to Soil N and Soil P modules.
        /// </summary>
        /// <param name="leachRain"></param>
        private void Leach(double leachRain)
        {

            double nh4Incorp;
            double no3Incorp;
            double po4Incorp;

            // Calculate Leaching Fraction;
            _leaching_fr = Utility.Math.Bound(Utility.Math.Divide(leachRain, TotalLeachRain, 0.0), 0.0, 1.0);


            // Apply leaching fraction to all mineral pools;
            // Put all mineral NO3,NH4 and PO4 into top layer;
            no3Incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.no3) * _leaching_fr;
            nh4Incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.nh4) * _leaching_fr;
            po4Incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.po4) * _leaching_fr;


            // If neccessary, Send the mineral N & P leached to the Soil N&P modules;
            if (no3Incorp > 0.0 || nh4Incorp > 0.0 || po4Incorp > 0.0)
            {
                NitrogenChangedType NitrogenChanges = new NitrogenChangedType();
                NitrogenChanges.Sender = "SurfaceOrganicMatter";
                NitrogenChanges.SenderType = "SurfaceOrganicMatter";
                NitrogenChanges.DeltaNH4 = new double[dlayer.Length];
                NitrogenChanges.DeltaNO3 = new double[dlayer.Length];
                NitrogenChanges.DeltaUrea = new double[dlayer.Length];
                NitrogenChanges.DeltaNH4[0] = nh4Incorp;
                NitrogenChanges.DeltaNO3[0] = no3Incorp;

                NitrogenChanged.Invoke(NitrogenChanges);

                if (phosphorusAware)
                {
                    throw new NotImplementedException();
                }

            }

            for (int i = 0; i < numSurfom; i++)
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

            if (numSurfom <= 0)
                return null;

            SurfaceOrganicMatterDecompType SOMDecomp = new SurfaceOrganicMatterDecompType()
            {
                Pool = new SurfaceOrganicMatterDecompPoolType[numSurfom]
            };

            double[]
                c_pot_decomp,
                n_pot_decomp,
                p_pot_decomp;

            PotDecomp(out c_pot_decomp, out n_pot_decomp, out p_pot_decomp);

            for (int residue = 0; residue < numSurfom; residue++)
                SOMDecomp.Pool[residue] = new SurfaceOrganicMatterDecompPoolType()
                {
                    Name = SurfOM[residue].name,
                    OrganicMatterType = SurfOM[residue].OrganicMatterType,

                    FOM = new FOMType()
                    {
                        amount = Utility.Math.Divide(c_pot_decomp[residue], C_fract[residue], 0.0),
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

            // TODO - implement  SurfaceOrganicMatterType.Clone() and use that here instead
            SurfaceOrganicMatterType SOM = new SurfaceOrganicMatterType()
            {
                Pool = new SurfaceOrganicMatterPoolType[numSurfom]
            };

            for (int residue = 0; residue < numSurfom; residue++)
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

                SOM.Pool[residue].StandingFraction = new FOMType[maxFr];
                SOM.Pool[residue].LyingFraction = new FOMType[maxFr];

                for (int pool = 0; pool < maxFr; pool++)
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
            // - Implementation Section ----------------------------------

            for (int som_index = 0; som_index < numSurfom; som_index++)
            {
                // Determine which residue pool corresponds to this index in the array;
                int SOMNo = GetResidueNumber(SOM.Pool[som_index].Name);

                if (SOMNo < 0)
                {
                    // This is an unknown type - error (<- original comment, not really an error as original code didn't throw one :S)
                    summary.WriteMessage(FullPath, "Attempting to remove Surface Organic Matter from unknown " + SOM.Pool[som_index].Name + " Surface Organic Matter name." + Environment.NewLine);
                }
                else
                {
                    // This type already exists;

                    // SurfOM[SOMNo] = SurfOM[SOMNo] - SOM[SOMNo]

                    // Check if too much removed ?
                    for (int pool = 0; pool < maxFr; pool++)
                    {
                        if (SurfOM[SOMNo].Lying[pool].amount >= SOM.Pool[SOMNo].LyingFraction[pool].amount)
                            SurfOM[SOMNo].Lying[pool].amount -= SOM.Pool[SOMNo].LyingFraction[pool].amount;
                        else
                        {
                            throw new ApsimXException(FullPath,
                                "Attempting to remove more dm from " + SOM.Pool[som_index].Name + " lying Surface Organic Matter pool " + pool + " than available" + Environment.NewLine
                                + "Removing " + SOM.Pool[SOMNo].LyingFraction[pool].amount + " (kg/ha) " + "from " + SurfOM[SOMNo].Lying[pool].amount + " (kg/ha) available.");
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
                                + "Removing " + SOM.Pool[SOMNo].LyingFraction[pool].amount + " (kg/ha) " + "from " + SurfOM[SOMNo].Lying[pool].amount + " (kg/ha) available.");
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


                // Report Removals;
                if (ReportRemovals == "yes")
                    summary.WriteMessage(FullPath, string.Format(
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
            P      = {7:0.0##}", SOM.Pool[SOMNo].Name, SOM.Pool[SOMNo].OrganicMatterType, lamount, lN, lP, samount, sN, sP));
                // else the user has asked for no reports for removals of surfom;
                // in the summary file.

                SendSOMRemovedEvent(
                    SOM.Pool[SOMNo].OrganicMatterType,
                    SOM.Pool[SOMNo].OrganicMatterType,
                    samount + lamount,
                    sN + lN,
                    sP + lP);

            }
        }

        private void DecomposeSurfom(SurfaceOrganicMatterDecompType SOMDecomp)
        {
            // Local Variables;
            int numSurfom = SOMDecomp.Pool.Length;          // local surfom counter from received event;
            int residue_no;                                 // Index into the global array;
            double[] cPotDecomp = new double[numSurfom];  // pot amount of C to decompose (kg/ha)
            double[] nPotDecomp = new double[numSurfom];  // pot amount of N to decompose (kg/ha)
            double[] pTotDecomp = new double[numSurfom];  // pot amount of P to decompose (kg/ha)
            double totCDecomp;    // total amount of c to decompose;
            double totNDecomp;    // total amount of c to decompose;
            double totPDecomp;    // total amount of c to decompose;

            double SOMcnr = 0;
            double SOMc = 0;
            double SOMn = 0;

            // calculate potential decompostion of C, N, and P;
            PotDecomp(out cPotDecomp, out nPotDecomp, out pTotDecomp);

            for (int counter = 0; counter < numSurfom; counter++)
            {

                // Determine which residue pool corresponds to this index in the array;
                residue_no = GetResidueNumber(SOMDecomp.Pool[counter].Name);

                // Collect actual decompostion of C and N from supplying module (soiln2)
                totCDecomp = SOMDecomp.Pool[counter].FOM.C;
                totNDecomp = SOMDecomp.Pool[counter].FOM.N;

                Bound_check_real_var(totNDecomp, 0.0, nPotDecomp[residue_no], "total n decomposition");

                SOMc = SurfOM[residue_no].Standing.Sum<OMFractionType>(x => x.C) + SurfOM[residue_no].Lying.Sum<OMFractionType>(x => x.C);
                SOMn = SurfOM[residue_no].Standing.Sum<OMFractionType>(x => x.N) + SurfOM[residue_no].Lying.Sum<OMFractionType>(x => x.N);

                SOMcnr = Utility.Math.Divide(SOMc, SOMn, 0.0);

                if (reals_are_equal(totCDecomp, 0.0) && reals_are_equal(totNDecomp, 0.0))
                {
                    // all OK - nothing happening;
                }
                else if (totCDecomp > cPotDecomp[residue_no] + acceptableErr)
                {
                    throw new ApsimXException(FullPath, "SurfaceOM - C decomposition exceeds potential rate");
                }
                else if (totNDecomp > nPotDecomp[residue_no] + acceptableErr)
                {
                    throw new ApsimXException(FullPath, "SurfaceOM - N decomposition exceeds potential rate");
                    // NIH - If both the following tests are empty then they can both be deleted.
                }
                else if (reals_are_equal(Utility.Math.Divide(totCDecomp, totNDecomp, 0.0), SOMcnr))
                {
                    // all ok - decomposition and residue pool have same C:N;
                }
                else
                {
                    // call fatal_error (Err_internal,
                    // :                 "C:N ratio of decomposed residues is not valid")
                }

                // calculate total p decomposition;
                totPDecomp = totCDecomp * Utility.Math.Divide(pTotDecomp[residue_no], cPotDecomp[residue_no], 0.0);

                // Do actual decomposing - update pools for C, N, and P;
                Decomp(totCDecomp, totNDecomp, totPDecomp, residue_no);
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

            double Fdecomp;  // decomposing fraction;

            // do C
            Fdecomp = Utility.Math.Bound(Utility.Math.Divide(C_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C), 0.0), 0.0, 1.0);
            for (int i = 0; i < maxFr; i++)
            {
                SurfOM[residue].Lying[i].C = SurfOM[residue].Lying[i].C * (1.0 - Fdecomp);
                SurfOM[residue].Lying[i].amount = SurfOM[residue].Lying[i].amount * (1.0 - Fdecomp);
            }

            // do N
            Fdecomp = Utility.Math.Divide(N_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N), 0.0);
            for (int i = 0; i < maxFr; i++)
                SurfOM[residue].Lying[i].N = SurfOM[residue].Lying[i].N * (1.0 - Fdecomp);

            // do P
            Fdecomp = Utility.Math.Divide(P_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.P), 0.0);
            for (int i = 0; i < maxFr; i++)
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

            // ----------------------------------------------------------
            //   If no user defined characteristics then use the;
            //     lookup table compiled from expert knowledge;
            // ----------------------------------------------------------
            if (data.f_incorp == 0 && data.tillage_depth == 0)
            {
                summary.WriteMessage(FullPath, "    - Reading default residue tillage info");

                data = TillageTypes.GetTillageData(data.Name);

                // If we still have no values then stop
                if (data == null)
                    // We have an unspecified tillage type;
                    throw new ApsimXException(FullPath, "Cannot find info for tillage:- " + data.Name);
            }

            // ----------------------------------------------------------
            //             Now incorporate the residues;
            // ----------------------------------------------------------

            Incorp(data.Name, data.f_incorp, data.tillage_depth);

            summary.WriteMessage(FullPath, string.Format(
    @"Residue removed using {0}
    Fraction Incorporated = {1:0.0##}
    Incorporated Depth    = {2:0.0##}", data.Name, data.f_incorp, data.tillage_depth));
        }

        /// <summary>
        /// Calculate surfom incorporation as a result of tillage and update;
        ///  residue and N pools.
        ///  <para>
        ///  Notes;
        ///  I do not like updating the pools here but we need to be able to handle;
        ///  the case of multiple tillage events per day.</para>
        /// </summary>
        /// <param name="actionType"></param>
        /// <param name="fIncorp"></param>
        /// <param name="tillageDepth"></param>
        private void Incorp(string actionType, double fIncorp, double tillageDepth)
        // ================================================================
        {
            double cumDepth;
            int deepestLayer;
            int nLayers = dlayer.Length;
            double depthToGo;
            double F_incorp_layer = 0;
            double[] residueIncorpFraction = new double[nLayers];
            double layerIncorpDepth;
            double[,] CPool = new double[maxFr, nLayers];  // total C in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] NPool = new double[maxFr, nLayers];  // total N in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] PPool = new double[maxFr, nLayers];  // total P in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] AshAlkPool = new double[maxFr, nLayers]; // total AshAlk in each Om fraction and layer (from all surfOM"s) incorporated;
            double[] no3 = new double[nLayers]; // total no3 to go into each soil layer (from all surfOM"s)
            double[] nh4 = new double[nLayers]; // total nh4 to go into each soil layer (from all surfOM"s)
            double[] po4 = new double[nLayers]; // total po4 to go into each soil layer (from all surfOM"s)
            FOMPoolType FPoolProfile = new FOMPoolType();
            ExternalMassFlowType massBalanceChange = new ExternalMassFlowType();

            fIncorp = Utility.Math.Bound(fIncorp, 0.0, 1.0);

            deepestLayer = GetCumulativeIndexReal(tillageDepth, dlayer);

            cumDepth = 0.0;

            for (int layer = 0; layer <= deepestLayer; layer++)
            {
                for (int residue = 0; residue < numSurfom; residue++)
                {
                    depthToGo = tillageDepth - cumDepth;
                    layerIncorpDepth = Math.Min(depthToGo, dlayer[layer]);
                    F_incorp_layer = Utility.Math.Divide(layerIncorpDepth, tillageDepth, 0.0);
                    for (int i = 0; i < maxFr; i++)
                    {
                        CPool[i, layer] += (SurfOM[residue].Lying[i].C + SurfOM[residue].Standing[i].C) * fIncorp * F_incorp_layer;
                        NPool[i, layer] += (SurfOM[residue].Lying[i].N + SurfOM[residue].Standing[i].N) * fIncorp * F_incorp_layer;
                        PPool[i, layer] += (SurfOM[residue].Lying[i].P + SurfOM[residue].Standing[i].P) * fIncorp * F_incorp_layer;
                        AshAlkPool[i, layer] += (SurfOM[residue].Lying[i].AshAlk + SurfOM[residue].Standing[i].AshAlk) * fIncorp * F_incorp_layer;
                    }
                    no3[layer] += SurfOM[residue].no3 * fIncorp * F_incorp_layer;
                    nh4[layer] += SurfOM[residue].nh4 * fIncorp * F_incorp_layer;
                    po4[layer] += SurfOM[residue].po4 * fIncorp * F_incorp_layer;
                }

                cumDepth = cumDepth + dlayer[layer];

                // dsg 160104  Remove the following variable after Res_removed_Event is scrapped;
                residueIncorpFraction[layer] = F_incorp_layer;
            }

            if (Sum2DArray(CPool) > 0.0)
            {

                // Pack up the incorporation info and send to SOILN2 and SOILP as part of a;
                // IncorpFOMPool Event;

                FPoolProfile.Layer = new FOMPoolLayerType[deepestLayer + 1];

                for (int layer = 0; layer <= deepestLayer; layer++)
                {
                    FPoolProfile.Layer[layer] = new FOMPoolLayerType()
                    {
                        thickness = dlayer[layer],
                        no3 = no3[layer],
                        nh4 = nh4[layer],
                        po4 = po4[layer],
                        Pool = new FOMType[maxFr]
                    };

                    for (int i = 0; i < maxFr; i++)
                        FPoolProfile.Layer[layer].Pool[i] = new FOMType()
                        {
                            C = CPool[i, layer],
                            N = NPool[i, layer],
                            P = PPool[i, layer],
                            AshAlk = AshAlkPool[i, layer]
                        };
                }

                // APSIM THING
                PublishFOMPool(FPoolProfile);

                // dsg 160104  Keep this event for the time being - will be replaced by ResidueChanged;

                SendResRemovedEvent(actionType, fIncorp, residueIncorpFraction, deepestLayer);

            }
            // else no residue incorporated;

            if (tillageDepth <= 0.000001)
            {
                // the OM is not incorporated and is lost from the system;

                massBalanceChange.PoolClass = "surface";
                massBalanceChange.FlowType = "loss";
                massBalanceChange.DM = 0.0;
                massBalanceChange.C = 0.0;
                massBalanceChange.N = 0.0;
                massBalanceChange.P = 0.0;
                massBalanceChange.SW = 0.0;

                for (int pool = 0; pool < maxFr; pool++)
                {
                    massBalanceChange.DM += SurfOM.Sum<SurfOrganicMatterType>(x => x.Standing[pool].amount + x.Lying[pool].amount) * fIncorp;
                    massBalanceChange.C += SurfOM.Sum<SurfOrganicMatterType>(x => x.Standing[pool].C + x.Lying[pool].C) * fIncorp;
                    massBalanceChange.N += SurfOM.Sum<SurfOrganicMatterType>(x => x.Standing[pool].N + x.Lying[pool].N) * fIncorp;
                    massBalanceChange.P += SurfOM.Sum<SurfOrganicMatterType>(x => x.Standing[pool].P + x.Lying[pool].P) * fIncorp;
                }
                SendExternalMassFlow(massBalanceChange);
            }

            // Now update globals.  They must be updated here because there is the possibility of;
            // more than one incorporation on any given day;

            for (int pool = 0; pool < maxFr; pool++)
            {
                for (int i = 0; i < SurfOM.Count; i++)
                {
                    SurfOM[i].Lying[pool].amount = SurfOM[i].Lying[pool].amount * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].amount = SurfOM[i].Standing[pool].amount * (1.0 - fIncorp);

                    SurfOM[i].Lying[pool].C = SurfOM[i].Lying[pool].C * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].C = SurfOM[i].Standing[pool].C * (1.0 - fIncorp);

                    SurfOM[i].Lying[pool].N = SurfOM[i].Lying[pool].N * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].N = SurfOM[i].Standing[pool].N * (1.0 - fIncorp);

                    SurfOM[i].Lying[pool].P = SurfOM[i].Lying[pool].P * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].P = SurfOM[i].Standing[pool].P * (1.0 - fIncorp);

                    SurfOM[i].Lying[pool].AshAlk = SurfOM[i].Lying[pool].AshAlk * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].AshAlk = SurfOM[i].Standing[pool].AshAlk * (1.0 - fIncorp);
                }
            }

            for (int i = 0; i < SurfOM.Count; i++)
            {
                SurfOM[i].no3 *= (1.0 - fIncorp);
                SurfOM[i].nh4 *= (1.0 - fIncorp);
                SurfOM[i].po4 *= (1.0 - fIncorp);
            }
        }

        /// <summary
        /// Adds a new element to the SurfOM list, resizing the relevant arrays
        /// <summary>
        private int AddNewSurfOM(string newName, string newType)
        {
            if (SurfOM == null)
                SurfOM = new List<SurfOrganicMatterType>();

            SurfOM.Add(new SurfOrganicMatterType(newName, newType));
            numSurfom = SurfOM.Count;
            Array.Resize(ref cf_contrib, numSurfom);
            Array.Resize(ref C_fract, numSurfom);
            Array.Resize(ref nh4ppm, numSurfom);
            Array.Resize(ref no3ppm, numSurfom);
            Array.Resize(ref po4ppm, numSurfom);
            Array.Resize(ref specific_area, numSurfom);
            frPoolC = Resize2DArray<double>(frPoolC, maxFr, numSurfom);
            frPoolN = Resize2DArray<double>(frPoolN, maxFr, numSurfom);
            frPoolP = Resize2DArray<double>(frPoolP, maxFr, numSurfom);

            int SOMNo = numSurfom - 1;

            ReadTypeSpecificConstants(SurfOM[SOMNo].OrganicMatterType, SOMNo, out SurfOM[SOMNo].PotDecompRate);

            return SOMNo;
        }
         
        /// Calculates surfom addition as a result of add_surfom message
        /// </summary>
        private void AddSurfom(Add_surfaceomType data)
        {

            int SOMNo = 0;                 // specific system number for this residue name;

            string
                surfomName = data.name,
                surfomType = data.type;

            double
                surfomMassAdded = Utility.Math.Bound(data.mass, -100000, 100000), // Mass of new surfom added (kg/ha)
                surfomCAdded = 0,                                         // C added in new material (kg/ha)
                surfomNAdded = Utility.Math.Bound(data.n, -10000, 10000), // N added in new material (kg/ha)
                surfomNO3Added = 0,                                       // NO3 added in new material (kg/ha)
                surfomNH4Added = 0,                                       // NH4 added in new material (kg/ha)
                surfomCNrAdded = Utility.Math.Bound(data.cnr, 0, 10000),  // C:N ratio of new material;
                surfomPAdded = Utility.Math.Bound(data.p, -10000, 10000), // P added in new material (kg/ha)
                surfomPO4Added = 0,                                       // PO4 added in new material (kg/ha)
                surfomCPrAdded = Utility.Math.Bound(data.cpr, 0, 10000),  // C:P ratio of new material;
                totMass = 0,
                removedFromStanding = 0,
                removedFromLying = 0;

            ExternalMassFlowType massBalanceChange = new ExternalMassFlowType();

            SOMNo = GetResidueNumber(surfomName);

            if (SOMNo < 0)
            {
                SOMNo = AddNewSurfOM(surfomName, surfomType);
            }
            // else this type already exists;

            // Get Mass of material added;

            // APSIM THING
            // collect_real_var ("mass", "(kg/ha)", surfomAdded, numvals, -100000.0, 100000.0);
            surfomCAdded = surfomMassAdded * C_fract[SOMNo];

            if (surfomMassAdded > -10000.0)
            {
                // Get N content of material added;
                if (surfomNAdded == 0)
                {
                    // APSIM THING
                    // collect_real_var_optional ("cnr", "()", surfomCNrAdded, numval_cnr, 0.0, 10000.0);

                        surfomNAdded = Utility.Math.Divide(surfomMassAdded * C_fract[SOMNo], surfomCNrAdded, 0.0);

                    // RCichota, disabled this error for now. WE have to see whether it is fair to add C and no N (I think so) or throw exception...
                        // If no N info provided, and no cnr info provided then throw error
                        //if (surfomCNrAdded == 0)
                            //throw new ApsimXException(FullPath, "SurfaceOM CN ratio not specified.");

                }

                // APSIM THING
                if (surfomPAdded == 0)
                    /// If no P info provided, and no cpr info provided {
                    /// use default cpr and throw warning error to notify user;
                    if (surfomCPrAdded == 0)
                    {
                        surfomPAdded = Utility.Math.Divide(surfomMassAdded * C_fract[SOMNo], DefaultCPRatio, 0.0);
                        summary.WriteMessage(FullPath, "SurfOM P or SurfaceOM C:P ratio not specified - Default value applied.");
                    }
                    else
                        surfomPAdded = Utility.Math.Divide(surfomMassAdded * C_fract[SOMNo], surfomCPrAdded, 0.0);

                // convert the ppm figures into kg/ha;
                surfomNO3Added = Utility.Math.Divide(no3ppm[SOMNo], 1000000, 0) * surfomMassAdded;
                surfomNH4Added = Utility.Math.Divide(nh4ppm[SOMNo], 1000000, 0) * surfomMassAdded;
                surfomPO4Added = Utility.Math.Divide(po4ppm[SOMNo], 1000000, 0) * surfomMassAdded;

                SurfOM[SOMNo].no3 += surfomNO3Added;
                SurfOM[SOMNo].nh4 += surfomNH4Added;
                SurfOM[SOMNo].po4 += surfomPO4Added;

                if (surfomMassAdded > 0.0)
                    /// Assume all residue added is in the LYING pool, ie No STANDING component;
                    for (int i = 0; i < maxFr; i++)
                    {
                        SurfOM[SOMNo].Lying[i].amount += surfomMassAdded * frPoolC[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].C += surfomMassAdded * C_fract[SOMNo] * frPoolC[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].N += surfomNAdded * frPoolN[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].P += surfomPAdded * frPoolP[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].AshAlk = 0;
                    }
                else
                {
                    // if residue is being removed, remove residue from both standing and lying pools;
                    double
                        lying = SurfOM[SOMNo].Lying.Sum<OMFractionType>(x => x.amount),
                        standing = SurfOM[SOMNo].Standing.Sum<OMFractionType>(x => x.amount);

                    totMass = lying + standing;

                    removedFromStanding = surfomMassAdded * Utility.Math.Divide(standing, totMass, 0.0);
                    removedFromLying = surfomMassAdded - removedFromStanding;

                    for (int i = 0; i < maxFr; i++)
                    {
                        SurfOM[SOMNo].Lying[i].amount = SurfOM[SOMNo].Lying[i].amount + removedFromLying * frPoolC[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].C = SurfOM[SOMNo].Lying[i].C + removedFromLying * C_fract[SOMNo] * frPoolC[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].N = SurfOM[SOMNo].Lying[i].N + surfomNAdded * Utility.Math.Divide(removedFromLying, surfomMassAdded, 0.0) * frPoolN[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].P = SurfOM[SOMNo].Lying[i].P + surfomPAdded * Utility.Math.Divide(removedFromLying, surfomMassAdded, 0.0) * frPoolP[i, SOMNo];
                        SurfOM[SOMNo].Lying[i].AshAlk = 0.0;
                        SurfOM[SOMNo].Standing[i].amount = SurfOM[SOMNo].Standing[i].amount + removedFromStanding * frPoolC[i, SOMNo];
                        SurfOM[SOMNo].Standing[i].C = SurfOM[SOMNo].Standing[i].C + removedFromStanding * C_fract[SOMNo] * frPoolC[i, SOMNo];
                        SurfOM[SOMNo].Standing[i].N = SurfOM[SOMNo].Standing[i].N + surfomNAdded * Utility.Math.Divide(removedFromStanding, surfomMassAdded, 0.0) * frPoolN[i, SOMNo];
                        SurfOM[SOMNo].Standing[i].P = SurfOM[SOMNo].Standing[i].P + surfomPAdded * Utility.Math.Divide(removedFromStanding, surfomMassAdded, 0.0) * frPoolP[i, SOMNo];
                        SurfOM[SOMNo].Standing[i].AshAlk = 0.0;
                    }
                }
                /// Report Additions;
                if (ReportAdditions == "yes")
                    summary.WriteMessage(FullPath, string.Format(
    @"Added SurfaceOM
    SurfaceOM name       = {0}
    SurfaceOM Type       = {1}
    Amount Added [kg/ha] = {2:0.0##}", SurfOM[SOMNo].name.Trim(), SurfOM[SOMNo].OrganicMatterType.Trim(), surfomMassAdded));

                SendResAddedEvent(SurfOM[SOMNo].OrganicMatterType, SurfOM[SOMNo].OrganicMatterType, surfomMassAdded, surfomNAdded, surfomPAdded);

                // assumption is this event comes only from the manager for applying from an external source.
                SendExternalMassFlow(
                    new ExternalMassFlowType()
                    {
                        PoolClass = "surface",
                        FlowType = "gain",
                        DM = surfomMassAdded,
                        C = surfomCAdded,
                        N = surfomNAdded + surfomNO3Added + surfomNH4Added,
                        P = surfomPAdded + surfomPO4Added,
                        SW = 0.0
                    });
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
            AddSurfaceOM((double)(data.OMWeight * FractionFaecesAdded),
                         (double)(data.OMN * FractionFaecesAdded),
                         (double)(data.OMP * FractionFaecesAdded),
                         Manure);

            // We should also have added ash alkalinity, but AddSurfaceOM_
            // doesn't have a field for that.
            // So let's add a bit of logic here to handle it...
            // We don't have a "fr_pool_ashalk" either, so we'll assume
            // it follows the same pattern as P.
            int SOMNo = GetResidueNumber(Manure);

            if (SOMNo >= 0) // Should always be OK, following creation in AddSurfom
            {
                for (int i = 0; i < maxFr; i++)
                {
                    SurfOM[SOMNo].Lying[i].AshAlk += (double)(data.OMAshAlk * FractionFaecesAdded * frPoolP[i, SOMNo]);
                }
            }

            // We should also handle sulphur someday....
        }

        /// <summary>
        /// Reads type-specific residue constants from ini-file and places them in c. constants;
        /// </summary>
        /// <param name="surfomType"></param>
        /// <param name="i"></param>
        private void ReadTypeSpecificConstants(string surfom_type, int i, out double pot_decomp_rate)
        {
            ResidueType thistype = ResidueTypes.getResidue(surfom_type);
            if (thistype == null)
                throw new ApsimXException(FullPath, "Cannot find residue type description for '" + surfom_type + "'");

            C_fract[i] = Utility.Math.Bound(thistype.fraction_C, 0.0, 1.0);
            po4ppm[i] = Utility.Math.Bound(thistype.po4ppm, 0.0, 1000.0);
            nh4ppm[i] = Utility.Math.Bound(thistype.nh4ppm, 0.0, 2000.0);
            no3ppm[i] = Utility.Math.Bound(thistype.no3ppm, 0.0, 1000.0);
            specific_area[i] = Utility.Math.Bound(thistype.specific_area, 0.0, 0.01);
            cf_contrib[i] = Bound(thistype.cf_contrib, 0, 1);
            pot_decomp_rate = Utility.Math.Bound(thistype.pot_decomp_rate, 0.0, 1.0);

            if (thistype.fr_c.Length != thistype.fr_n.Length || thistype.fr_n.Length != thistype.fr_p.Length)
                throw new ApsimXException(FullPath, "Error reading in fr_c/n/p values, inconsistent array lengths");

            for (int j = 0; j < thistype.fr_c.Length; j++)
            {
                frPoolC[j, i] = thistype.fr_c[j];
                frPoolN[j, i] = thistype.fr_n[j];
                frPoolP[j, i] = thistype.fr_p[j];
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
            double F_Cover;             // Fraction of soil surface covered by residue (0-1)
            double areaLying;          // area of lying component;
            double areaStanding;       // effective area of standing component (the 0.5 extinction coefficient in area calculation
            // provides a random distribution in degree to which standing stubble is "lying over"

            // calculate fraction of cover and check bounds (0-1).  Bounds checking;
            // is required only for detecting internal rounding error.
            double sumStandAmount = 0;
            double sumLyingAmount = 0;
            for (int i = 0; i < maxFr; i++)
            {
                sumLyingAmount += SurfOM[SOMindex].Lying[i].amount;
                sumStandAmount += SurfOM[SOMindex].Standing[i].amount;
            }

            areaLying = specific_area[SOMindex] * sumLyingAmount;
            areaStanding = specific_area[SOMindex] * sumStandAmount;

            F_Cover = AddCover(1.0 - (double)Math.Exp(-areaLying), 1.0 - (double)Math.Exp(-(StandingExtinctCoeff) * areaStanding));
            F_Cover = Utility.Math.Bound(F_Cover, 0.0, 1.0);

            return F_Cover;
        }

        /// <summary>
        /// Check that soil phosphorus is in system
        /// </summary>
        private void SetPhosphorusAware()
        {
            phosphorusAware = labile_p != null;
        }

        /// <summary>
        /// Get information on surfom added from the crops
        /// </summary>
        /// <param name="BiomassRemoved"></param>
        private void SurfOMOnBiomassRemoved(BiomassRemovedType BiomassRemoved)
        {
            double
                surfomAdded = 0,   // amount of residue added (kg/ha)
                surfomNAdded = 0, // amount of residue N added (kg/ha)
                surfomPAdded = 0; // amount of residue N added (kg/ha)

            if (BiomassRemoved.fraction_to_residue.Sum() != 0)
            {
                // Find the amount of surfom to be added today;
                for (int i = 0; i < BiomassRemoved.fraction_to_residue.Length; i++)
                    surfomAdded += BiomassRemoved.dlt_crop_dm[i] * BiomassRemoved.fraction_to_residue[i];

                if (surfomAdded > 0.0)
                {
                    // Find the amount of N & added in surfom today;
                    for (int i = 0; i < BiomassRemoved.dlt_dm_p.Length; i++)
                    {
                        surfomPAdded += BiomassRemoved.dlt_dm_p[i] * BiomassRemoved.fraction_to_residue[i];
                        surfomNAdded += BiomassRemoved.dlt_dm_n[i] * BiomassRemoved.fraction_to_residue[i];
                    }

                    AddSurfaceOM(surfomAdded, surfomNAdded, surfomPAdded, BiomassRemoved.crop_type);
                }
            }
        }

        private void AddSurfaceOM(double surfomAdded, double surfomNAdded, double surfomPAdded, string cropType)
        {
            int SOMNo;      // system number of the surface organic matter added;

            // Report Additions;
            if (ReportAdditions == "yes")
            {
                summary.WriteMessage(FullPath, string.Format(

    @"Added surfom
    SurfaceOM Type          = {0}
    Amount Added [kg/ha]    = {1}", cropType.TrimEnd(), surfomAdded));
            }

            // Assume the "cropType" is the unique name.  Now check whether this unique "name" already exists in the system.
            SOMNo = GetResidueNumber(cropType);

            if (SOMNo < 0)
            {
                SOMNo = AddNewSurfOM(cropType, cropType);
            }
            /// else THIS ADDITION IS AN EXISTING COMPONENT OF THE SURFOM SYSTEM;

            // convert the ppm figures into kg/ha;
            SurfOM[SOMNo].no3 += Utility.Math.Divide(no3ppm[SOMNo], 1000000.0, 0.0) * surfomAdded;
            SurfOM[SOMNo].nh4 += Utility.Math.Divide(nh4ppm[SOMNo], 1000000.0, 0.0) * surfomAdded;
            SurfOM[SOMNo].po4 += Utility.Math.Divide(po4ppm[SOMNo], 1000000.0, 0.0) * surfomAdded;

            // Assume all surfom added is in the LYING pool, ie No STANDING component;
            for (int i = 0; i < maxFr; i++)
            {
                SurfOM[SOMNo].Lying[i].amount += surfomAdded * frPoolC[i, SOMNo];
                SurfOM[SOMNo].Lying[i].C += surfomAdded * C_fract[SOMNo] * frPoolC[i, SOMNo];
                SurfOM[SOMNo].Lying[i].N += surfomNAdded * frPoolN[i, SOMNo];
                SurfOM[SOMNo].Lying[i].P += surfomPAdded * frPoolP[i, SOMNo];
                SurfOM[SOMNo].Lying[i].AshAlk = 0.0;
            }

            SendResAddedEvent(SurfOM[SOMNo].OrganicMatterType, SurfOM[SOMNo].OrganicMatterType, surfomAdded, surfomNAdded, surfomPAdded);
        }

        /// <summary>
        /// Notify other modules of residue added to residue pool.
        /// </summary>
        /// <param name="residueType"></param>
        /// <param name="dmType"></param>
        /// <param name="dltResidueWt"></param>
        /// <param name="dltResidueNWt"></param>
        /// <param name="dltResiduePWt"></param>
        private void SendResAddedEvent(string residueType, string dmType, double dltResidueWt, double dltResidueNWt, double dltResiduePWt)
        {
            if (Residue_added != null)
            {
                ResidueAddedType data = new ResidueAddedType()
                {
                    residue_type = residueType,
                    dm_type = dmType,
                    dlt_residue_wt = dltResidueWt,
                    dlt_dm_n = dltResidueNWt,
                    dlt_dm_p = dltResiduePWt
                };

                Residue_added.Invoke(data);
            }
        }

        /// <summary>
        /// Notify other modules of residue removed from residue pool
        /// </summary>
        /// <param name="residueRemovedAction"></param>
        /// <param name="dltResidueFraction"></param>
        /// <param name="residueIncorpFraction"></param>
        /// <param name="deepestLayer"></param>
        private void SendResRemovedEvent(string residueRemovedAction, double dltResidueFraction, double[] residueIncorpFraction, int deepestLayer)
        {
            if (Residue_removed != null)
            {
                ResidueRemovedType data = new ResidueRemovedType()
                {
                    residue_removed_action = residueRemovedAction,
                    dlt_residue_fraction = dltResidueFraction,
                    residue_incorp_fraction = residueIncorpFraction
                };

                Residue_removed.Invoke(data);
            }
        }

        /// <summary>
        /// Notify other modules of residue added to residue pool
        /// </summary>
        /// <param name="residueType"></param>
        /// <param name="dmType"></param>
        /// <param name="dltResidueWt"></param>
        /// <param name="dltResidueNWt"></param>
        /// <param name="dltResiduePWt"></param>
        private void SendSOMRemovedEvent(string residueType, string dmType, double dltResidueWt, double dltResidueNWt, double dltResiduePWt)
        {
            if (SurfaceOM_removed != null)
            {
                SurfaceOMRemovedType data = new SurfaceOMRemovedType()
                {
                    SurfaceOM_type = residueType,
                    SurfaceOM_dm_type = dmType,
                    dlt_SurfaceOM_wt = dltResidueWt,
                    SurfaceOM_dlt_dm_n = dltResidueNWt,
                    SurfaceOM_dlt_dm_p = dltResiduePWt
                };

                SurfaceOM_removed.Invoke(data);
            }
        }

        protected T[,] Resize2DArray<T>(T[,] original, int x, int y)
        {
            T[,] newArray = new T[x, y];
            int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
            int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

            for (int i = 0; i < minY; ++i)
            {
                Array.Copy(original, i * original.GetLength(0), newArray, i * newArray.GetLength(0), minX);
            }

            return newArray;
        }
    }
}