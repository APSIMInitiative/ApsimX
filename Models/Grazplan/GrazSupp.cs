// -----------------------------------------------------------------------
// The GrazPlan Supplement objects
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Newtonsoft.Json;

namespace Models.GrazPlan
{

    /// <summary>
    /// Class containing some common routine for dealing with parameter sets
    /// </summary>
    public class GrazParam
    {
        /// <summary>
        /// magic string to serve as a wildcard for all locales
        /// </summary>
        public const string ALLLOCALES = "#all#";

        /// <summary>
        /// Registry key for Grazplan configuration information
        /// </summary>
        public const string PARAMKEY = "Software\\CSIRO\\Common\\Parameters";

        /// <summary>
        /// The UI language
        /// </summary>
        private static string userInterfaceLang = string.Empty;

        /// <summary>
        /// Determine whether a locale name is included in a list of locale names
        /// </summary>
        /// <param name="locale">Locale name</param>
        /// <param name="localeList">semicolon delimited list of locale names</param>
        /// <returns>True if the locale is in the list</returns>
        public static bool InLocale(string locale, string localeList)
        {
            if (locale == ALLLOCALES)
                return true;
            else
            {
                string temp = ";" + localeList + ";";
                return temp.Contains(";" + locale + ";");
            }
        }

        /// <summary>
        /// Returns the 2-letter ISO 639 language code (e.g, 'en')
        /// </summary>
        /// <returns>
        /// The 2-letter language code
        /// </returns>
        public static string GetUILang()
        {
            if (string.IsNullOrEmpty(userInterfaceLang))
            {
                userInterfaceLang = Environment.GetEnvironmentVariable("LANG");
                if (string.IsNullOrEmpty(userInterfaceLang))
                    userInterfaceLang = System.Globalization.CultureInfo.CurrentCulture.Name;
            }
            return userInterfaceLang.Substring(0, 2);
        }

        /// <summary>
        /// Force use of a language code, rather than determining it from system settings
        /// </summary>
        /// <param name="lang">2-letter language code to be used</param>
        public static void SetUILang(string lang)
        {
            userInterfaceLang = lang;
        }

        /// <summary>
        /// Common locale for use across models and programs
        /// * The locale is a two-character country code that is stored in the registry.
        /// * If there is no entry in the registry, 'au' is returned.
        /// </summary>
        /// <returns>
        /// A 2-character country code
        /// </returns>
        public static string DefaultLocale()
        {
            string loc = null;
#if NET6_0_OR_GREATER
            if (OperatingSystem.IsWindows())
#else
            if (ProcessUtilities.CurrentOS.IsWindows)
#endif
            {
                Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(PARAMKEY);
                if (regKey != null)
                    loc = (string)regKey.GetValue("locale");
            }
            if (string.IsNullOrEmpty(loc))
                loc = "au";
            return loc.ToLower();
        }
    }

    /// <summary>
    /// Supplement information
    /// </summary>
    [Serializable]
    public class SuppInfo
    {
        /// <summary>
        /// Gets or sets a value indicating whether the supplement is a roughage.
        /// </summary>
        /// <value>True if the supplement is a roughage</value>
        public bool IsRoughage { get; set; }

        /// <summary>
        /// Gets or sets the dry matter content of the supplement (kg/kg FW).
        /// </summary>
        /// <value>Dry matter content of the supplement (kg/kg)</value>
        public double DMContent { get; set; }

        /// <summary>
        /// Gets or sets the dry matter digestibility of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Dry matter digestibiility of the supplement (kg/kg)</value>
        public double DMD { get; set; }

        /// <summary>
        /// Gets or sets the metabolizable energy content of the supplement (MJ/kg).
        /// </summary>
        /// <value>Metabolizable energy content of the supplement (MJ/kg)</value>
        public double MEContent { get; set; }

        /// <summary>
        /// Gets or sets the crude protein content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Crude protein content of the supplement (kg/kg)</value>
        public double CPConc { get; set; }

        /// <summary>
        /// Gets or sets the degradability of the protein of the supplement (kg/kg CP).
        /// </summary>
        /// <value>Degradability of the protein of the supplement (kg/kg)</value>
        public double ProtDg { get; set; }

        /// <summary>
        /// Gets or sets the phosphorus content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Phosphorus content of the supplement (kg/kg)</value>
        public double PConc { get; set; }

        /// <summary>
        /// Gets or sets the sulfur content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Sulfur content of the supplement (kg/kg)</value>
        public double SConc { get; set; }

        /// <summary>
        /// Gets or sets the ether-extractable content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Ether-extractable content of the supplement (kg/kg)</value>
        public double EEConc { get; set; }

        /// <summary>
        /// Gets or sets the ratio of acid detergent insoluble protein to CP for the supplement (kg/kg CP).
        /// </summary>
        /// <value>Ratio of acid detergent insoluble protein to CP for the supplement (kg/kg)</value>
        public double ADIP2CP { get; set; }

        /// <summary>
        /// Gets or sets the ash alkalinity of the supplement (mol/kg DM).
        /// </summary>
        /// <value>Ash alkalinity of the supplement (mol/kg)</value>
        public double AshAlk { get; set; }

        /// <summary>
        /// Gets or sets the maximum passage rate of the supplement (0-1).
        /// </summary>
        /// <value>Maximum passage rate of the supplement (kg/kg)</value>
        public double MaxPassage { get; set; }
    }

    /// <summary>
    /// Supplement encapsulates the attributes of a single supplement.
    /// </summary>
    [Serializable]
    public class FoodSupplement
    {
        /// <summary>
        /// The translations array
        /// </summary>
        internal Translation[] Translations = new Translation[0];

        /// <summary>
        /// Max. proportion passing through gut (used with whole grains)
        /// </summary>
        internal double maxPassage;

        /// <summary>
        /// Ash alkalinity (mol/kg)
        /// </summary>
        internal double ashAlkalinity;

        /// <summary>
        /// Proportion of dry matter by weight
        /// </summary>
        internal double dmPropn;

        /// <summary>
        /// Digestibility of dry matter
        /// </summary>
        internal double dmDigestibility;

        /// <summary>
        /// Metabolizable energy:DM, MJ/kg
        /// </summary>
        internal double me2dm;

        /// <summary>
        /// Ether-extractable fraction
        /// </summary>
        internal double etherExtract;

        /// <summary>
        /// Proportion which is crude protein
        /// </summary>
        internal double crudeProt;

        /// <summary>
        /// Proportion of protein that is rumen-degradable
        /// </summary>
        internal double degProt;

        /// <summary>
        /// Acid detergent insoluble protein:CP
        /// </summary>
        internal double adip2cp;

        /// <summary>
        /// The Rghg_MEDM_Intcpt
        /// </summary>
        private const double Rghg_MEDM_Intcpt = -1.707;

        /// <summary>
        /// The Rghg_MEDM_DMD
        /// </summary>
        private const double Rghg_MEDM_DMD = 17.2;

        /// <summary>
        /// The Conc_MEDM_Intcpt
        /// </summary>
        private const double Conc_MEDM_Intcpt = 1.3;

        /// <summary>
        /// The Conc_MEDM_DMD
        /// </summary>
        private const double Conc_MEDM_DMD = 13.3;

        /// <summary>
        /// The Conc_MEDM_EE
        /// </summary>
        private const double Conc_MEDM_EE = 23.4;

        /// <summary>
        /// The n2 protein
        /// </summary>
        public const double N2PROTEIN = 6.25;

        /// <summary>
        /// FoodSupplement constructor
        /// </summary>
        public FoodSupplement()
        {
            if (SupplementLibrary.DefaultSuppConsts != null && SupplementLibrary.DefaultSuppConsts.Count > 0)
            {
                Assign(SupplementLibrary.DefaultSuppConsts[0]);
                Translations = new Translation[0];
            }
        }

        /// <summary>
        /// constructor with text argument
        /// </summary>
        /// <param name="suppSt">The supplement name</param>
        public FoodSupplement(string suppSt)
        {
            ParseText(suppSt, false);
        }

        /// <summary>
        /// copy consructor
        /// </summary>
        /// <param name="src">The source.</param>
        public FoodSupplement(FoodSupplement src)
        {
            Assign(src);
        }

        /// <summary>
        /// Enumeration of the chemical properites of a supplement
        /// </summary>
        [Serializable]
        public enum SuppAttribute
        {
            /// <summary>
            /// The attribute DMP
            /// </summary>
            spaDMP,

            /// <summary>
            /// The attribute DMD
            /// </summary>
            spaDMD,

            /// <summary>
            /// The attribute medm
            /// </summary>
            spaMEDM,

            /// <summary>
            /// The attribute ee
            /// </summary>
            spaEE,

            /// <summary>
            /// The attribute cp
            /// </summary>
            spaCP,

            /// <summary>
            /// The attribute dg
            /// </summary>
            spaDG,

            /// <summary>
            /// The attribute adip
            /// </summary>
            spaADIP,

            /// <summary>
            /// The attribute ph
            /// </summary>
            spaPH,

            /// <summary>
            /// The attribute su
            /// </summary>
            spaSU,

            /// <summary>
            /// The attribute aa
            /// </summary>
            spaAA,

            /// <summary>
            /// The attribute maximum p
            /// </summary>
            spaMaxP
        }

        /// <summary>
        /// Gets or sets the name of the supplement
        /// </summary>
        [Units("-")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is roughage.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is roughage; otherwise, <c>false</c>.
        /// </value>
        [Units("-")]
        public bool IsRoughage { get; set; }

        // The following are all on a 0-1 scale:

        /// <summary>
        /// Gets or sets the d m_ propn.
        /// </summary>
        /// <value>
        /// The d m_ propn.
        /// </value>
        [Units("0-1.0")]
        public double DMPropn
        {
            get { return dmPropn; }
            set { dmPropn = value; }
        }

        /// <summary>
        /// Gets or sets the dry matter digestibility.
        /// </summary>
        /// <value>
        /// The dmDigestibility value.
        /// </value>
        [Units("0-1.0")]
        public double DMDigestibility
        {
            get { return dmDigestibility; }
            set { dmDigestibility = value; }
        }

        /// <summary>
        /// Gets or sets the metabolizable energy:DM (MJ/kg).
        /// </summary>
        /// <value>
        /// The me2dm value.
        /// </value>
        [Units("0-1.0")]
        public double ME2DM
        {
            get { return me2dm; }
            set { me2dm = value; }
        }

        /// <summary>
        /// Gets or sets the ether extractable fraction.
        /// </summary>
        /// <value>
        /// The ether extract.
        /// </value>
        [Units("0-1.0")]
        public double EtherExtract
        {
            get { return etherExtract; }
            set { etherExtract = value; }
        }

        /// <summary>
        /// Gets or sets the proportion that is crude protein.
        /// </summary>
        /// <value>
        /// The crude prot.
        /// </value>
        [Units("0-1.0")]
        public double CrudeProt
        {
            get { return crudeProt; }
            set { crudeProt = value; }
        }

        /// <summary>
        /// Gets or sets the protein proportion that is rumen degradeable.
        /// </summary>
        /// <value>
        /// The degProt value.
        /// </value>
        [Units("0-1.0")]
        public double DegProt
        {
            get { return degProt; }
            set { degProt = value; }
        }

        /// <summary>
        /// Gets or sets the acid detergent insoluble protein:CP.
        /// </summary>
        /// <value>The adip2cp.</value>
        public double ADIP2CP
        {
            get { return adip2cp; }
            set { adip2cp = value; }
        }

        /// <summary>
        /// Phosphorus content (P:DM)
        /// </summary>
        internal double phosphorus;

        /// <summary>
        /// Gets or sets the phosphorus (P:DM).
        /// </summary>
        /// <value>
        /// The phosphorus.
        /// </value>
        [Units("0-1.0")]
        public double Phosphorus
        {
            get { return phosphorus; }
            set { phosphorus = value; }
        }

        /// <summary>
        /// Sulphur content (S:DM)
        /// </summary>
        internal double sulphur;

        /// <summary>
        /// Gets or sets the sulphur content (S:DM).
        /// </summary>
        /// <value>
        /// The sulphur.
        /// </value>
        [Units("0-1.0")]
        public double Sulphur
        {
            get { return sulphur; }
            set { sulphur = value; }
        }

        /// <summary>
        /// Gets or sets the ash alkalinity.
        /// </summary>
        /// <value>
        /// The ash alkalinity.
        /// </value>
        [Units("mol/kg")]
        public double AshAlkalinity
        {
            get { return ashAlkalinity; }
            set { ashAlkalinity = value; }
        }

        /// <summary>
        /// Gets or sets the maximum proportion passing through the gut (used with whole grains).
        /// </summary>
        /// <value>
        /// The maximum passage.
        /// </value>
        [Units("")]
        public double MaxPassage
        {
            get { return maxPassage; }
            set { maxPassage = value; }
        }

        /// <summary>
        /// Indexer to allow easy access of attributes of a supplement
        /// </summary>
        /// <value>
        /// The <see cref="System.Double"/>.
        /// </value>
        /// <param name="attr">attibute to be retrieved or set</param>
        /// <returns>The value of the attribute chosen</returns>
        [JsonIgnore]
        public double this[SuppAttribute attr]
        {
            get
            {
                switch (attr)
                {
                    case SuppAttribute.spaDMP:
                        return this.dmPropn;
                    case SuppAttribute.spaDMD:
                        return this.dmDigestibility;
                    case SuppAttribute.spaMEDM:
                        return this.me2dm;
                    case SuppAttribute.spaEE:
                        return this.etherExtract;
                    case SuppAttribute.spaCP:
                        return this.crudeProt;
                    case SuppAttribute.spaDG:
                        return this.degProt;
                    case SuppAttribute.spaADIP:
                        return this.adip2cp;
                    case SuppAttribute.spaPH:
                        return this.phosphorus;
                    case SuppAttribute.spaSU:
                        return this.sulphur;
                    case SuppAttribute.spaAA:
                        return this.ashAlkalinity;
                    case SuppAttribute.spaMaxP:
                        return this.maxPassage;
                    default: return 0.0;
                }
            }

            set
            {
                switch (attr)
                {
                    case SuppAttribute.spaDMP:
                        this.dmPropn = value;
                        break;
                    case SuppAttribute.spaDMD:
                        this.dmDigestibility = value;
                        break;
                    case SuppAttribute.spaMEDM:
                        this.me2dm = value;
                        break;
                    case SuppAttribute.spaEE:
                        this.etherExtract = value;
                        break;
                    case SuppAttribute.spaCP:
                        this.crudeProt = value;
                        break;
                    case SuppAttribute.spaDG:
                        this.degProt = value;
                        break;
                    case SuppAttribute.spaADIP:
                        this.adip2cp = value;
                        break;
                    case SuppAttribute.spaPH:
                        this.phosphorus = value;
                        break;
                    case SuppAttribute.spaSU:
                        this.sulphur = value;
                        break;
                    case SuppAttribute.spaAA:
                        this.ashAlkalinity = value;
                        break;
                    case SuppAttribute.spaMaxP:
                        this.maxPassage = value;
                        break;
                }
            }
        }

        // ConvertDMD_To_ME2DM
        // ConvertME2DM_To_DMD
        // Routines for default conversions between M/D and DMD
        //   fDMD       Dry matter digestibility  (0-1)
        //   fME2DM     M/D ratio                 (MJ/kg)
        //   fEE        Ether-extractable content (0-1)

        /// <summary>
        /// Routine for default conversion from DMD to M/D
        /// </summary>
        /// <param name="dmd">Dry matter digestibility  (0-1)</param>
        /// <param name="isRoughage">True if the supplement is a roughage</param>
        /// <param name="fEE">Ether-extractable content (0-1)</param>
        /// <returns>M/D ratio (MJ/kg)</returns>
        public static double ConvertDMDToME2DM(double dmd, bool isRoughage, double fEE)
        {
            if (isRoughage)
                return System.Math.Max(0.0, Rghg_MEDM_Intcpt + (Rghg_MEDM_DMD * dmd));
            else
                return System.Math.Max(0.0, Conc_MEDM_Intcpt + (Conc_MEDM_DMD * dmd) + (Conc_MEDM_EE * fEE));
        }

        /// <summary>
        /// Routine for default conversion from M/D to DMD
        /// </summary>
        /// <param name="me2dm">M/D ratio (MJ/kg)</param>
        /// <param name="isRoughage">True if the supplement is a roughage</param>
        /// <param name="fEE">Ether-extractable content (0-1)</param>
        /// <returns>Dry matter digestibility  (0-1)</returns>
        public static double ConvertME2DMToDMD(double me2dm, bool isRoughage, double fEE)
        {
            double result;
            if (isRoughage)
                result = (me2dm - Rghg_MEDM_Intcpt) / Rghg_MEDM_DMD;
            else
                result = (me2dm - ((Conc_MEDM_EE * fEE) + Conc_MEDM_Intcpt)) / Conc_MEDM_DMD;
            return System.Math.Max(0.0, System.Math.Min(0.9999, result));
        }

        /// <summary>
        /// Mix two supplements together and store in Self
        /// Will work if Supp1=this or Supp2=this
        /// This method is only exact if the passage rates of the two supplements are equal
        /// </summary>
        /// <param name="supp1">The supp1.</param>
        /// <param name="supp2">The supp2.</param>
        /// <param name="propn1">The propn1.</param>
        public void Mix(FoodSupplement supp1, FoodSupplement supp2, double propn1)
        {
            if (propn1 >= 0.50)
                IsRoughage = supp1.IsRoughage;
            else
                IsRoughage = supp2.IsRoughage;
            double propn2 = 1.0 - propn1;                                  // Proportion of suppt 2 on a FW basis
            double dmpropn1 = MathUtilities.Divide(propn1 * supp1.DMPropn, (propn1 * supp1.DMPropn) + (propn2 * supp2.DMPropn), 0.0);  // Proportion of suppt 1 on a DM basis
            double dmpropn2 = 1.0 - dmpropn1;                              // Proportion of suppt 2 on a DM basis

            double CPpropn1;                                               // Proportion of suppt 1 on a total CP basis
            if ((propn1 * supp1.DMPropn * supp1.CrudeProt) + (propn2 * supp2.DMPropn * supp2.CrudeProt) > 0.0)
            {
                CPpropn1 = (propn1 * supp1.DMPropn * supp1.CrudeProt) / ((propn1 * supp1.DMPropn * supp1.CrudeProt) + (propn2 * supp2.DMPropn * supp2.CrudeProt));
            }
            else
                CPpropn1 = propn1;
            double CPpropn2 = 1.0 - CPpropn1;                             // Proportion of suppt 1 on a total CP basis

            DMPropn = (propn1 * supp1.DMPropn) + (propn2 * supp2.DMPropn);
            DMDigestibility = (dmpropn1 * supp1.DMDigestibility) + (dmpropn2 * supp2.DMDigestibility);
            ME2DM = (dmpropn1 * supp1.ME2DM) + (dmpropn2 * supp2.ME2DM);
            EtherExtract = (dmpropn1 * supp1.EtherExtract) + (dmpropn2 * supp2.EtherExtract);
            CrudeProt = (dmpropn1 * supp1.CrudeProt) + (dmpropn2 * supp2.CrudeProt);
            DegProt = (CPpropn1 * supp1.DegProt) + (CPpropn2 * supp2.DegProt);
            ADIP2CP = (CPpropn1 * supp1.ADIP2CP) + (CPpropn2 * supp2.ADIP2CP);
            Phosphorus = (dmpropn1 * supp1.Phosphorus) + (dmpropn2 * supp2.Phosphorus);
            Sulphur = (dmpropn1 * supp1.Sulphur) + (dmpropn2 * supp2.Sulphur);
            AshAlkalinity = (dmpropn1 * supp1.AshAlkalinity) + (dmpropn2 * supp2.AshAlkalinity);
            MaxPassage = (dmpropn1 * supp1.MaxPassage) + (dmpropn2 * supp2.MaxPassage);
        }

        /// <summary>
        /// Mixes the many supplements
        /// </summary>
        /// <param name="supps">The supplements</param>
        /// <param name="amounts">The amounts.</param>
        public void MixMany(FoodSupplement[] supps, double[] amounts)
        {
            double amountSum = 0.0;
            for (int idx = 0; idx < supps.Length; idx++)
            {
                if (idx < amounts.Length && amounts[idx] > 0.0)
                {
                    Mix(supps[idx], this, amounts[idx] / (amountSum + amounts[idx]));
                    amountSum += amounts[idx];
                }
            }
        }

        /// <summary>
        /// Mixes the many supplements
        /// </summary>
        /// <param name="supps">The supplements</param>
        public void MixMany(SupplementItem[] supps)
        {
            double amountSum = 0.0;
            for (int idx = 0; idx < supps.Length; idx++)
            {
                if (supps[idx].Amount > 0.0)
                {
                    Mix(supps[idx], this, supps[idx].Amount / (amountSum + supps[idx].Amount));
                    amountSum += supps[idx].Amount;
                }
            }
        }

        /// <summary>
        /// Assigns the specified source supplement.
        /// </summary>
        /// <param name="srcSupp">The source supplement.</param>
        public void Assign(FoodSupplement srcSupp)
        {
            if (srcSupp != null)
            {
                this.Name = srcSupp.Name;
                IsRoughage = srcSupp.IsRoughage;
                DMPropn = srcSupp.DMPropn;
                DMDigestibility = srcSupp.DMDigestibility;
                ME2DM = srcSupp.ME2DM;
                EtherExtract = srcSupp.EtherExtract;
                CrudeProt = srcSupp.CrudeProt;
                DegProt = srcSupp.DegProt;
                ADIP2CP = srcSupp.ADIP2CP;
                Phosphorus = srcSupp.Phosphorus;
                Sulphur = srcSupp.Sulphur;
                AshAlkalinity = srcSupp.AshAlkalinity;
                MaxPassage = srcSupp.MaxPassage;
                Array.Resize(ref Translations, srcSupp.Translations.Length);
                Array.Copy(srcSupp.Translations, Translations, srcSupp.Translations.Length);
            }
        }

        /// <summary>
        /// This function looks for "token value units" at the head of SuppSt and
        /// if it finds it, scales the value which has been read in
        /// </summary>
        /// <param name="suppSt">String to parse</param>
        /// <param name="token">The expected token string</param>
        /// <param name="units">The expected units string</param>
        /// <param name="scalar">Multiplier for value field</param>
        /// <param name="value">Receives the value which was read</param>
        /// <returns>The scaled value</returns>
        private bool ParseKeyword(ref string suppSt, string token, string units, double scalar, ref double value)
        {
            bool result = StringUtilities.MatchToken(ref suppSt, token) &&
                          StringUtilities.TokenDouble(ref suppSt, ref value) &&
                          StringUtilities.MatchToken(ref suppSt, units);
            if (result)
                value *= scalar;
            return result;
        }

        /// <summary>
        /// The CreateText method is fairly general. The layout of the string is:
        /// (Name) [ (keyword) (value) (unit)[(keyword)...] ]
        /// If (Name) is found in SuppTokens, then the supplement is initialised to
        /// the corresponding supplement.  Otherwise it is initialised to supplement
        /// number 1 (the first concentrate).  Any keywords then modify the
        /// composition.  Keywords are:
        /// DM_PC (%)  DMD (%) CP (%) DG (%) ME2DM (MJ)
        /// Finally, if only one of DMD and ME2DM was found, the regression equation
        /// on ether extract is used to estimate the other.
        /// </summary>
        /// <param name="suppSt">The supp st.</param>
        /// <param name="nameOnly">if set to <c>true</c> [b name only].</param>
        public void ParseText(string suppSt, bool nameOnly)
        {
            suppSt = suppSt.Trim();
            int suppNo = -1;
            int matchLen = 0;
            string nameStr;

            // Search for a matching name
            for (int idx = 0; idx < SupplementLibrary.DefaultSuppConsts.Count; idx++)
            {
                string suppName = SupplementLibrary.DefaultSuppConsts[idx].Name.ToUpper();
                if (suppName.StartsWith(suppSt.ToUpper()) && suppName.Length > matchLen)
                {
                    suppNo = idx;
                    matchLen = suppName.Length;
                }
            }

            if (suppNo >= 0)
            {
                // If we have a match, initialise to it
                Assign(SupplementLibrary.DefaultSuppConsts[suppNo]);
            }
            else
            {
                // ... otherwise set to an arbitrary concentrate and use the given name
                this.Name = suppSt;
                StringUtilities.TextToken(ref suppSt, out nameStr);
                if (!nameOnly && suppSt != string.Empty)
                    this.Name = nameStr;
            }

            bool continueLoop = !nameOnly;    // Now parse the rest of the string as keyword/value/unit combinations.
            bool dmdSet = false;              // TRUE once digestibility has been read in
            bool medmSet = false;             // TRUE once ME:DM has been read in

            while (continueLoop)
            {
                // Any breakdown results in the rest of the string being ignored
                if (ParseKeyword(ref suppSt, "DM_PC", "%", 0.01, ref dmPropn)
                   || ParseKeyword(ref suppSt, "CP", "%", 0.01, ref crudeProt)
                   || ParseKeyword(ref suppSt, "DG", "%", 0.01, ref degProt))
                    continueLoop = true;
                else if (ParseKeyword(ref suppSt, "DMD", "%", 0.01, ref dmDigestibility))
                {
                    continueLoop = true;
                    dmdSet = true;
                }
                else if (ParseKeyword(ref suppSt, "ME2DM", "MJ", 1.0, ref me2dm))
                {
                    continueLoop = true;
                    medmSet = true;
                }
                else
                    continueLoop = false;
            }

            if (dmdSet && !medmSet)
                ME2DM = DefaultME2DM();
            else if (!dmdSet && medmSet)
                DMDigestibility = DefaultDMD();
        }

        /// <summary>
        /// Adds the translation.
        /// </summary>
        /// <param name="lang">The language.</param>
        /// <param name="text">The text.</param>
        public void AddTranslation(string lang, string text)
        {
            bool found = false;
            for (int idx = 0; idx < Translations.Length; idx++)
            {
                if (Translations[idx].Lang.Equals(text, StringComparison.OrdinalIgnoreCase))
                {
                    Translations[idx].Text = text;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Array.Resize(ref Translations, Translations.Length + 1);
                Translations[Translations.Length - 1] = new Translation();
                Translations[Translations.Length - 1].Lang = lang;
                Translations[Translations.Length - 1].Text = text;
            }
            if (lang.Equals("en", StringComparison.OrdinalIgnoreCase) || this.Name == string.Empty)
                this.Name = text;
        }

        /// <summary>
        /// Defaults from name.
        /// </summary>
        public void DefaultFromName()
        {
            ParseText(this.Name, true);
        }

        /// <summary>
        /// Computes a default value for DM digestibility from a (known) M/D ratio
        /// </summary>
        /// <returns>The digestibility</returns>
        public double DefaultDMD()
        {
            return ConvertME2DMToDMD(ME2DM, IsRoughage, EtherExtract);
        }

        /// <summary>
        /// Computes a default value for M/D from a (known) DM digestibility          }
        /// </summary>
        /// <returns>The M/D value</returns>
        public double DefaultME2DM()
        {
            return ConvertDMDToME2DM(DMDigestibility, IsRoughage, EtherExtract);
        }

        /// <summary>
        /// Calculates the default acid-detergent insoluble protein : crude protein
        /// ratio for user defined supplements.
        /// </summary>
        /// <returns>The default acid-detergent insoluble protein : crude protein ratio</returns>
        /// <exception cref="System.Exception">result is out of range</exception>
        public double DefaultADIP2CP()
        {
            double result;
            if (IsRoughage)
                result = 0.19 * (1.0 - DegProt);
            else
                result = System.Math.Max(0.03, 0.87 - (1.09 * DegProt));

            // Post-Condition
            if ((result < 0.0) || (result > 1.0))
                throw new Exception(string.Format("Post condition failed in FoodSupplement.DefaultADIP_2_CP: Result={0}", result));
            return result;
        }

        /// <summary>
        /// Calculates the default phosphorus content for user defined supplements.
        /// </summary>
        /// <returns>The default phosphorus content</returns>
        /// <exception cref="System.Exception">the result is out of range</exception>
        public double DefaultPhosphorus()
        {
            double result;
            if (IsRoughage)
                result = (0.0062 * DMDigestibility) - 0.0016;
            else
                result = 0.051 * DegProt * CrudeProt;

            // Post-Condition
            if ((result < 0.0) || (result > 1.0))
                throw new Exception(string.Format("Post condition failed in Supplement.DefaultPhosphorus: Result={0}", result));
            return result;
        }

        /// <summary>
        /// Calculates the default sulphur content for user defined supplements.
        /// </summary>
        /// <returns>The default sulphur value for the supplement type</returns>
        /// <exception cref="System.Exception">The result is out of range</exception>
        public double DefaultSulphur()
        {
            double result;
            if (IsRoughage)
                result = (0.0095 * CrudeProt) + 0.0011;
            else
                result = 0.0126 * CrudeProt;

            // Post-Condition
            if ((result < 0.0) || (result > 1.0))
                throw new Exception(string.Format("Post condition failed in Supplement.DefaultSulphur: Result={0}", result));
            return result;
        }

        /// <summary>
        /// Determines whether [is same as] [the specified other supp].
        /// </summary>
        /// <param name="otherSupp">The other supp.</param>
        /// <returns>True is the supplements are the same</returns>
        public bool IsSameAs(FoodSupplement otherSupp)
        {
            return (this.Name == otherSupp.Name)
            && (IsRoughage == otherSupp.IsRoughage)
            && (DMPropn == otherSupp.DMPropn)
            && (DMDigestibility == otherSupp.DMDigestibility)
            && (ME2DM == otherSupp.ME2DM)
            && (EtherExtract == otherSupp.EtherExtract)
            && (CrudeProt == otherSupp.CrudeProt)
            && (DegProt == otherSupp.DegProt)
            && (ADIP2CP == otherSupp.ADIP2CP)
            && (Phosphorus == otherSupp.Phosphorus)
            && (Sulphur == otherSupp.Sulphur)
            && (AshAlkalinity == otherSupp.AshAlkalinity)
            && (MaxPassage == otherSupp.MaxPassage);
        }

        /// <summary>
        /// Populates fields of this FoodSupplement from a SuppToStockType
        /// </summary>
        /// <param name="value">The supp to stock value</param>
        public void SetSuppAttrs(SuppToStockType value)
        {
            this.IsRoughage = value.IsRoughage;         // "roughage"
            this.DMPropn = value.DMContent;             // "dm_content"
            this.DMDigestibility = value.DMD;           // "dmd"
            this.ME2DM = value.MEContent;               // "me_content"
            this.CrudeProt = value.CPConc;              // "cp_conc"
            this.DegProt = value.ProtDg;                // "prot_dg"
            this.Phosphorus = value.PConc;              // "p_conc"
            this.Sulphur = value.SConc;                 // "s_conc"
            this.EtherExtract = value.EEConc;           // "ee_conc"
            this.ADIP2CP = value.ADIP2CP;               // "adip2cp"
            this.AshAlkalinity = value.AshAlk;          // "ash_alk"
            this.MaxPassage = value.MaxPassage;         // "max_passage"
        }

        /// <summary>
        /// The translation specification
        /// </summary>
        [Serializable]
        internal struct Translation
        {
            /// <summary>
            /// The supplement base language
            /// </summary>
            internal string Lang;

            /// <summary>
            /// The s text
            /// </summary>
            internal string Text;
        }
    }

    /// <summary>
    /// A record to allow us to hold amount and cost information along
    /// with the FoodSupplement information
    /// In FoodSupplementItem, the "amount" should be read as kg of supplement fresh
    /// weight. and the cost should be per kg fresh weight.
    /// </summary>
    [Serializable]
    public class SupplementItem : FoodSupplement
    {
        /// <summary>
        /// SupplementItem constructor
        /// </summary>
        public SupplementItem() : base()
        {
        }

        /// <summary>
        /// Constructor
        /// Note that it makes a copy of the FoodSupplement
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="amt">The amt.</param>
        /// <param name="cst">The CST.</param>
        public SupplementItem(FoodSupplement src, double amt = 0.0, double cst = 0.0) : base(src)
        {
            Amount = amt;
            Cost = cst;
        }

        /// <summary>
        /// Gets or sets the amount in kg.
        /// </summary>
        /// <value>
        /// The amount.
        /// </value>
        [Units("kg")]
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets the cost.
        /// </summary>
        /// <value>
        /// The cost.
        /// </value>
        [Units("-")]
        public double Cost { get; set; }

        /// <summary>
        /// Assigns the specified source supp.
        /// </summary>
        /// <param name="srcSupp">The source supp.</param>
        public void Assign(SupplementItem srcSupp)
        {
            if (srcSupp != null)
            {
                base.Assign(srcSupp);
                Amount = srcSupp.Amount;
                Cost = srcSupp.Cost;
            }
        }
    }

    /// <summary>
    /// SupplementRation encapsulates zero or more supplements mixed together.
    /// In essence, it is a list of SupplementItem.
    /// This is the class used for specifying the supplement fed to a group of
    /// animals in AnimGrp.pas
    /// Apart from the usual read/write properties and list-handling methods, the
    /// class has the following special methods:
    /// * AverageSuppt      computes the composition of a supplement mixture in
    /// proportions given by the fAmount values.
    /// * AverageCost       computes the cost of a supplement mixture in
    /// proportions given by the fAmount values.
    /// </summary>
    [Serializable]
    public class SupplementRation
    {
        /// <summary>
        /// The supplements array
        /// </summary>
        internal SupplementItem[] SuppArray = new SupplementItem[0];

        /// <summary>
        /// The ration choice
        /// </summary>
        private RationChoice rationChoice = RationChoice.rcStandard;

        /// <summary>
        /// The ration choice type
        /// </summary>
        [Serializable]
        public enum RationChoice
        {
            /// <summary>
            /// The rc standard mix as specified
            /// </summary>
            rcStandard,

            /// <summary>
            /// The rc only stored
            /// use only stored fodder while it lasts
            /// </summary>
            rcOnlyStored,

            /// <summary>
            /// The rc inc stored
            /// use stored fodder as first ingredient
            /// </summary>
            rcIncStored
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count</value>
        [Units("-")]
        public int Count
        {
            get
            {
                return SuppArray.Length;
            }
        }

        /// <summary>
        /// Gets or sets the total amount.
        /// </summary>
        /// <value>
        /// The total amount.
        /// </value>
        [Units("kg")]
        public double TotalAmount
        {
            get
            {
                double totAmt = 0.0;
                for (int i = 0; i < SuppArray.Length; i++)
                    totAmt += SuppArray[i].Amount;
                return totAmt;
            }

            set
            {
                double scale = 0.0;
                double totAmt = TotalAmount;
                if (totAmt > 0.0)
                    scale = value / totAmt;
                else if (SuppArray.Length > 0)
                    scale = value / SuppArray.Length;

                for (int i = 0; i < SuppArray.Length; i++)
                    SuppArray[i].Amount *= scale;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="SupplementItem"/> with the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="SupplementItem"/>.
        /// </value>
        /// <param name="idx">The index.</param>
        /// <returns>The supplement object</returns>
        [JsonIgnore]
        public SupplementItem this[int idx]
        {
            get
            {
                return SuppArray[idx];
            }

            set
            {
                // Note: Assigning to the Suppt property copies the attributes of the source
                // FoodSupplement, not the FoodSupplement instance itself.
                if (idx >= SuppArray.Length)
                {
                    Array.Resize(ref SuppArray, (int)idx + 1);
                    SuppArray[idx] = new SupplementItem(value);
                }
            }
        }

        /// <summary>
        /// Assigns the specified source ration.
        /// </summary>
        /// <param name="srcRation">The source ration.</param>
        public void Assign(SupplementRation srcRation)
        {
            Array.Resize(ref SuppArray, srcRation.Count);
            for (int idx = 0; idx < srcRation.Count; idx++)
            {
                if (SuppArray[idx] == null)
                    SuppArray[idx] = new SupplementItem();
                SuppArray[idx].Assign(srcRation[idx]);
            }
            rationChoice = srcRation.rationChoice;
        }

        /// <summary>
        /// Gets the fresh weight fraction.
        /// </summary>
        /// <param name="idx">The index of the supplement.</param>
        /// <returns>The fresh weight fraction for the supplement at idx</returns>
        public double GetFWFract(int idx)
        {
            return SuppArray[idx].Amount >= 1e-7 ? SuppArray[idx].Amount / TotalAmount : 0.0;
        }

        /// <summary>
        /// Adds the specified supp.
        /// </summary>
        /// <param name="supp">The supp.</param>
        /// <param name="amt">The amt.</param>
        /// <param name="cost">The cost.</param>
        /// <returns>The array index of the new supplement</returns>
        public int Add(FoodSupplement supp, double amt = 0.0, double cost = 0.0)
        {
            int idx = SuppArray.Length;
            Insert(idx, supp, amt, cost);
            return idx;
        }

        /// <summary>
        /// Adds the specified supp item.
        /// </summary>
        /// <param name="suppItem">The supp item.</param>
        /// <returns>The array index of the new supplement</returns>
        public int Add(SupplementItem suppItem)
        {
            return Add(suppItem, suppItem.Amount, suppItem.Cost);
        }

        /// <summary>
        /// Inserts the specified index.
        /// </summary>
        /// <param name="idx">The index.</param>
        /// <param name="supp">The supp.</param>
        /// <param name="amt">The amt.</param>
        /// <param name="cost">The cost.</param>
        public void Insert(int idx, FoodSupplement supp, double amt = 0.0, double cost = 0.0)
        {
            Array.Resize(ref SuppArray, SuppArray.Length + 1);
            for (int jdx = SuppArray.Length - 1; jdx > idx; jdx--)
                SuppArray[jdx] = SuppArray[jdx - 1];
            SuppArray[idx] = new SupplementItem(supp, amt, cost);
        }

        /// <summary>
        /// Deletes the specified index.
        /// </summary>
        /// <param name="idx">The index.</param>
        public void Delete(int idx)
        {
            for (int jdx = idx + 1; jdx < SuppArray.Length; jdx++)
                SuppArray[jdx - 1] = SuppArray[jdx];
            Array.Resize(ref SuppArray, SuppArray.Length - 1);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            Array.Resize(ref SuppArray, 0);
        }

        /// <summary>
        /// Get the index of the supplement in the supplements array
        /// </summary>
        /// <param name="name">Name of the supplement.</param>
        /// <param name="checkTrans">if set to <c>true</c> [check trans].</param>
        /// <returns>The array index of the supplement or -1 if not found</returns>
        public int IndexOf(string name, bool checkTrans = false)
        {
            int result = -1;
            for (int idx = 0; idx < SuppArray.Length; idx++)
            {
                if (SuppArray[idx].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    result = idx;
                    break;
                }
            }

            if (result < 0 && checkTrans)
            {
                for (int idx = 0; idx < SuppArray.Length; idx++)
                    for (int itr = 0; itr < SuppArray[idx].Translations.Length; itr++)
                        if (SuppArray[idx].Translations[itr].Text.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            result = idx;
                            break;
                        }
            }
            return result;
        }

        /// <summary>
        /// Computes a weighted average supplement composition
        /// </summary>
        public FoodSupplement AverageSuppt()
        {
            var aveSupp = new FoodSupplement();
            if (TotalAmount > 0.0)
                aveSupp.MixMany(SuppArray);
            return aveSupp;
        }

        /// <summary>
        /// Weighted average cost of a supplement
        /// </summary>
        /// <returns>
        /// The weighted average cost in the same units as SupplementItem.cost
        /// </returns>
        public double AverageCost()
        {
            if (this.TotalAmount < 1e-7)
                return 0.0;
            double totCost = 0.0;
            for (int idx = 0; idx < this.SuppArray.Length; idx++)
                totCost += this.SuppArray[idx].Amount * this.SuppArray[idx].Cost;
            return totCost / this.TotalAmount;
        }

        /// <summary>
        /// The property n_ attrs
        /// </summary>
        private static FoodSupplement.SuppAttribute[] PROPNATTRS =
                                                            {
                                                            FoodSupplement.SuppAttribute.spaDMP,
                                                            FoodSupplement.SuppAttribute.spaDMD,
                                                            FoodSupplement.SuppAttribute.spaEE,
                                                            FoodSupplement.SuppAttribute.spaCP,
                                                            FoodSupplement.SuppAttribute.spaDG,
                                                            FoodSupplement.SuppAttribute.spaADIP,
                                                            FoodSupplement.SuppAttribute.spaPH,
                                                            FoodSupplement.SuppAttribute.spaSU,
                                                            FoodSupplement.SuppAttribute.spaMaxP
                                                            };

        /// <summary>
        /// Scales the attributes of the members of the supplement so that the weighted
        /// average attributes match those of aveSupp. Ensures that fractional values
        /// remain within the range 0-1
        /// * Assumes that all values are non-negative
        /// </summary>
        /// <param name="scaleToSupp">The scale to supp.</param>
        /// <param name="attrs">The attrs.</param>
        public void RescaleRation(FoodSupplement scaleToSupp, IList<FoodSupplement.SuppAttribute> attrs)
        {
            Array attribs = Enum.GetValues(typeof(FoodSupplement.SuppAttribute));

            // NB this only works because of the way the supplement attributes are ordered
            // i.e. DM proportion first and CP before dg and ADIP:CP
            foreach (FoodSupplement.SuppAttribute attr in attribs)
            {
                if (attrs.Contains(attr))
                {
                    double newWtMean = scaleToSupp[attr];

                    if (this.SuppArray.Length == 1)
                        this.SuppArray[0][attr] = newWtMean;
                    else
                    {
                        double oldWtMean = 0.0;
                        double totalWeight = 0.0;
                        double weight = 0.0;
                        for (int idx = 0; idx < SuppArray.Length; idx++)
                        {
                            switch (attr)
                            {
                                case FoodSupplement.SuppAttribute.spaDMP:
                                    weight = GetFWFract(idx);
                                    break;
                                case FoodSupplement.SuppAttribute.spaDG:
                                case FoodSupplement.SuppAttribute.spaADIP:
                                    weight = GetFWFract(idx) * SuppArray[idx].DMPropn * SuppArray[idx].CrudeProt;
                                    break;
                                default:
                                    weight = GetFWFract(idx) * SuppArray[idx].DMPropn;
                                    break;
                            }
                            oldWtMean += weight * this.SuppArray[idx][attr];
                            totalWeight += weight;
                        }
                        if (totalWeight > 0.0)
                            oldWtMean /= totalWeight;

                        for (int idx = 0; idx < this.SuppArray.Length; idx++)
                        {
                            if (totalWeight == 0.0)
                                this.SuppArray[idx][attr] = newWtMean;
                            else if ((newWtMean < oldWtMean) || (!PROPNATTRS.Contains(attr)))
                                this.SuppArray[idx][attr] *= newWtMean / oldWtMean;
                            else
                                this.SuppArray[idx][attr] += (1.0 - this.SuppArray[idx][attr]) * (newWtMean - oldWtMean)
                                                        / (1.0 - oldWtMean);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// SupplementLibrary is a SupplementRation descendant that is intended for
    /// use in manipulating lists of supplements within GUIs.
    /// For SupplementLibrary, the "amounts" may be read in relative or absolute
    /// terms, depending on the application.
    /// Apart from the usual read/write properties and list-handling methods, the
    /// class has the following special methods:
    /// * the Add and Insert methods have variants that allow the user to set up a
    /// supplement by using its name; other attributes are looked up from the
    /// DefaultSuppConsts library.
    /// * PopulateDefaults     sets the library up to contain the complete set of
    /// default supplement compositions.
    /// * CopyFrom             adds either the entire contents of another library,
    /// or else a nominated subset of supplements from the
    /// other library.
    /// * ReadFromRegistry     Populates the library from a set of formatted strings
    /// contained in a file pointed to by SUPP_LIB_KEY
    /// * WriteToRegistry      Write a formatted set of strings that can be read by
    /// ReadFromStrings to the file pointed to by
    /// in a file pointed to by SUPP_LIB_KEY
    /// </summary>
    [Serializable]
    public class SupplementLibrary : SupplementRation
    {
        /// <summary>
        /// The g definition supp
        /// </summary>
        internal static SupplementLibrary GDefSupp = null;

        /// <summary>
        /// The s att r_ header
        /// </summary>
        private const string ATTRHEADER = "R    DM    DMD    M/D     EE     CP     dg    ADIP     P        S       AA    MaxP Locales";

        /// <summary>
        /// Lock object controlling access to GDefSupp
        /// </summary>
        protected readonly static object defSuppLock = new object();

        /// <summary>
        /// Gets the default supp consts.
        /// </summary>
        /// <value>
        /// The default supp consts.
        /// </value>
        public static SupplementLibrary DefaultSuppConsts
        {
            get
            {
                lock (defSuppLock)
                {
                    if (GDefSupp == null)
                    {
                        GDefSupp = new SupplementLibrary();
                        SetupDefaultSupplements();
                    }
                    return GDefSupp;
                }
            }
        }

        /// <summary>
        /// Setups the default supplements.
        /// </summary>
        public static void SetupDefaultSupplements()
        {
            if (!DefaultSuppConsts.ReadFromRegistryFile(GrazParam.DefaultLocale()))
                DefaultSuppConsts.ReadFromResource(GrazParam.DefaultLocale());
        }

        /// <summary>
        /// Adds the specified s name.
        /// </summary>
        /// <param name="name">Name of the supplement.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="cost">The cost.</param>
        public void Add(string name, double amount = 0.0, double cost = 0.0)
        {
            Insert(Count, name, amount, cost);
        }

        /// <summary>
        /// Inserts the specified index.
        /// </summary>
        /// <param name="idx">The index.</param>
        /// <param name="suppName">Name of the supplement.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="cost">The cost.</param>
        public void Insert(int idx, string suppName, double amount = 0.0, double cost = 0.0)
        {
            SupplementItem defSupp = new SupplementItem();
            this.GetDefaultSupp(suppName, ref defSupp);
            Insert(idx, defSupp, amount, cost);
        }

        /// <summary>
        /// Locates a supplement by name in the DefaultSupptCosts array and returns it
        /// </summary>
        /// <param name="suppName">Name of the supplement.</param>
        /// <param name="suppt">The supplement.</param>
        /// <returns>The supplement object</returns>
        public bool GetDefaultSupp(string suppName, ref SupplementItem suppt)
        {
            int idx = DefaultSuppConsts.IndexOf(suppName);
            bool result = idx >= 0;

            if (result)
            {
                if (suppt == null)
                    suppt = new SupplementItem();
                suppt.Assign(DefaultSuppConsts[idx]);
            }
            return result;
        }

        /// <summary>
        /// Populates the defaults.
        /// </summary>
        public void PopulateDefaults()
        {
            Assign(DefaultSuppConsts);
        }

        /// <summary>
        /// Reverts to default.
        /// </summary>
        /// <param name="idx">The index.</param>
        public void RevertToDefault(int idx)
        {
            this.GetDefaultSupp(SuppArray[idx].Name, ref this.SuppArray[idx]);
        }

        /// <summary>
        /// Copies from.
        /// </summary>
        /// <param name="srcLibrary">The source library.</param>
        /// <param name="copyNames">The copy names.</param>
        public void CopyFrom(SupplementLibrary srcLibrary, string[] copyNames = null)
        {
            for (int idx = 0; idx < srcLibrary.Count; idx++)
            {
                if (copyNames == null || Array.IndexOf(copyNames, srcLibrary[idx].Name) >= 0)
                    this.Add(new SupplementItem(srcLibrary[idx], srcLibrary[idx].Amount, srcLibrary[idx].Cost));
            }
        }

        /// <summary>
        /// Reads from strings.
        /// </summary>
        /// <param name="locale">The locale.</param>
        /// <param name="strings">The strings.</param>
        /// <exception cref="System.Exception">
        /// Error reading supplement library - must contain a header line
        /// or
        /// Error reading supplement library - header line is invalid
        /// or
        /// Error reading supplement library - line for  + sNameStr +  is invalid
        /// </exception>
        public void ReadFromStrings(string locale, string[] strings)
        {
            if (strings == null || strings.Length == 0)
                throw new Exception("Error reading supplement library - must contain a header line");

            int attrPosn = strings[0].IndexOf('|');       // Every line must have a | character in
            // this column
            string hdrStr = "Name";
            hdrStr = hdrStr.PadRight(attrPosn, ' ') + "|" + ATTRHEADER;

            if (strings[0] != hdrStr)
                throw new Exception("Error reading supplement library - header line is invalid");

            this.Clear();

            string nameStr = string.Empty;
            try
            {
                for (int idx = 1; idx < strings.Length; idx++)
                {
                    if (strings[idx].Length < attrPosn)
                        continue;
                    nameStr = strings[idx].Substring(0, attrPosn - 1).Trim();
                    string attrStr = strings[idx].Substring(attrPosn + 1);

                    FoodSupplement newSupp = new FoodSupplement();
                    newSupp.Name = nameStr;
                    newSupp.IsRoughage = attrStr[0] == 'Y';
                    attrStr = attrStr.Remove(0, 1);

                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.dmPropn);
                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.dmDigestibility);
                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.me2dm);
                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.etherExtract);
                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.crudeProt);
                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.degProt);
                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.adip2cp);
                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.phosphorus);
                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.sulphur);
                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.ashAlkalinity);
                    StringUtilities.TokenDouble(ref attrStr, ref newSupp.maxPassage);

                    attrStr = attrStr.Trim();
                    string transStr;
                    string transName;
                    string locStr;
                    string language;
                    int indexBlank = attrStr.IndexOf(' ');
                    if (indexBlank < 0)
                    {
                        locStr = attrStr;
                        transStr = string.Empty;
                    }
                    else
                    {
                        locStr = attrStr.Substring(0, indexBlank);
                        transStr = attrStr.Substring(indexBlank + 1).Trim();
                    }

                    while (transStr != string.Empty)
                    {
                        StringUtilities.TextToken(ref transStr, out language);
                        if (transStr.Length > 0 && transStr[0] == ':')
                        {
                            transStr = transStr.Substring(1);
                            StringUtilities.TextToken(ref transStr, out transName, true);
                            newSupp.AddTranslation(language, transName);
                        }
                        if (transStr.Length > 0 && transStr[0] == ';')
                            transStr = transStr.Substring(1);
                    }

                    if (locStr == string.Empty || GrazParam.InLocale(locale, locStr))
                        this.Add(newSupp, 0.0, 0.0);
                }
            }
            catch (Exception)
            {
                throw new Exception("Error reading supplement library - line for " + nameStr + " is invalid");
            }
        }

        /// <summary>
        /// Reads from registry file.
        /// </summary>
        /// <param name="locale">The locale.</param>
        /// <returns>True if this locale is found</returns>
        public bool ReadFromRegistryFile(string locale)
        {
#if NET6_0_OR_GREATER
            if (OperatingSystem.IsWindows())
#else
            if (ProcessUtilities.CurrentOS.IsWindows)
#endif
            {
                Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(GrazParam.PARAMKEY);
                if (regKey != null)
                {
                    string suppFile = (string)regKey.GetValue("supplib");
                    if (!string.IsNullOrEmpty(suppFile) && System.IO.File.Exists(suppFile))
                    {
                        string[] suppStrings = System.IO.File.ReadAllLines(suppFile);
                        this.ReadFromStrings(locale, suppStrings);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Reads from resource.
        /// </summary>
        /// <param name="locale">The locale.</param>
        public void ReadFromResource(string locale)
        {
            string suppData = ReflectionUtilities.GetResourceAsString("Models.Resources.Supplement.txt");
            string[] suppStrings = suppData.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            this.ReadFromStrings(locale, suppStrings);
        }

        /// <summary>
        /// Returns the index of FoodSupplement in the array of supplements
        /// </summary>
        /// <param name="item">The supplement item</param>
        /// <returns>The array index, or -1 if not found</returns>
        public int IndexOf(SupplementItem item)
        {
            return Array.IndexOf(this.SuppArray, item);
        }
    }
}
