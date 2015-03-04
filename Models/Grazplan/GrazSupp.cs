// -----------------------------------------------------------------------
// <copyright file="Supplement.cs" company="CSIRO">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Models.Grazplan
{
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Utility;

    /// <summary>
    /// Class containing some common routine for dealing with parameter sets
    /// </summary>
    public class GrazParam
    {
        /// <summary>
        /// magic string to serve as a wildcard for all locales
        /// </summary>
        public const string ALL_LOCALES = "#all#";
        /// <summary>
        /// Registry key for Grazplan configuration information
        /// </summary>
        public const string PARAM_KEY = "Software\\CSIRO\\Common\\Parameters";

        static private string UILang = "";

        /// <summary>
        /// Determine whether a locale name is included in a list of locale names
        /// </summary>
        /// <param name="locale">Locale name</param>
        /// <param name="localeList">semicolon delimited list of locale names</param>
        /// <returns></returns>
        public static bool InLocale(string locale, string localeList)
        {
            if (locale == ALL_LOCALES)
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
        /// <returns>The 2-letter language code</returns>
        public static string GetUILang()
        {
            if (string.IsNullOrEmpty(UILang))
            {
                UILang = Environment.GetEnvironmentVariable("LANG");
                if (string.IsNullOrEmpty(UILang))
                    UILang = System.Globalization.CultureInfo.CurrentCulture.Name;
            }
            return UILang.Substring(0, 2);
        }

        /// <summary>
        /// Force use of a language code, rather than determining it from system settings
        /// </summary>
        /// <param name="lang">2-letter language code to be used</param>
        public static void SetUILang(string lang)
        {
            UILang = lang;
        }

        /// <summary>
        /// Common locale for use across models and programs 
        /// * The locale is a two-character country code that is stored in the registry.
        /// * If there is no entry in the registry, 'au' is returned.
        /// </summary>
        /// <returns>A 2-character country code</returns>
        public static string DefaultLocale()
        {
            string loc = null;
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(PARAM_KEY);
            if (regKey != null)
                loc = (string)regKey.GetValue("locale");
            if (string.IsNullOrEmpty(loc))
                loc = "au";
            return loc.ToLower();
        }
    }

    public class SuppInfo
    {
        /// <summary>Gets or sets whether the supplement is a roughage.</summary>
        /// <value>True if the supplement is a roughage</value>
        public bool IsRoughage;
        /// <summary>Gets or sets the dry matter content of the supplement (kg/kg FW).</summary>
        /// <value>Dry matter content of the supplement (kg/kg)</value>
        public double DMContent;
        /// <summary>Gets or sets the dry matter digestibility of the supplement (kg/kg DM).</summary>
        /// <value>Dry matter digestibiility of the supplement (kg/kg)</value>
        public double DMD;
        /// <summary>Gets or sets the metabolizable energy content of the supplement (MJ/kg).</summary>
        /// <value>Metabolizable energy content of the supplement (MJ/kg)</value>
        public double MEContent;
        /// <summary>Gets or sets the crude protein content of the supplement (kg/kg DM).</summary>
        /// <value>Crude protein content of the supplement (kg/kg)</value>
        public double CPConc;
        /// <summary>Gets or sets the degradability of the protein of the supplement (kg/kg CP).</summary>
        /// <value>Degradability of the protein of the supplement (kg/kg)</value>
        public double ProtDg;
        /// <summary>Gets or sets the phosphorus content of the supplement (kg/kg DM).</summary>
        /// <value>Phosphorus content of the supplement (kg/kg)</value>
        public double PConc;
        /// <summary>Gets or sets the sulfur content of the supplement (kg/kg DM).</summary>
        /// <value>Sulfur content of the supplement (kg/kg)</value>
        public double SConc;
        /// <summary>Gets or sets the ether-extractable content of the supplement (kg/kg DM).</summary>
        /// <value>Ether-extractable content of the supplement (kg/kg)</value>
        public double EEConc;
        /// <summary>Gets or sets the ratio of acid detergent insoluble protein to CP for the supplement (kg/kg CP).</summary>
        /// <value>Ratio of acid detergent insoluble protein to CP for the supplement (kg/kg)</value>
        public double ADIP2CP;
        /// <summary>Gets or sets the ash alkalinity of the supplement (mol/kg DM).</summary>
        /// <value>Ash alkalinity of the supplement (mol/kg)</value>
        public double AshAlk;
        /// <summary>Gets or sets the maximum passage rate of the supplement (0-1).</summary>
        /// <value>Maximum passage rate of the supplement (kg/kg)</value>
        public double MaxPassage;
    }

    /// <summary>
    /// TSupplement encapsulates the attributes of a single supplement.
    /// </summary>
    [Serializable]
    public class TSupplement
    {
        /// <summary>
        /// Enumeration of the chemical properites of a supplement
        /// </summary>
        [Serializable]
        public enum TSuppAttribute
        {
            spaDMP,
            spaDMD,
            spaMEDM,
            spaEE,
            spaCP,
            spaDG,
            spaADIP,
            spaPH,
            spaSU,
            spaAA,
            spaMaxP
        };

        [Serializable]
        internal struct TTranslation
        {
            internal string sLang;
            internal string sText;
        };

        internal string sName;
        internal TTranslation[] FTranslations = new TTranslation[0];
        public bool IsRoughage { get; set; }

        // The following are all on a 0-1 scale:

        /// <summary>Proportion of dry matter by weight</summary>
        internal double _DM_Propn;
        public double DM_Propn { get { return _DM_Propn; } set { _DM_Propn = value; } }

        /// <summary>
        /// Digestibility of dry matter
        /// </summary>
        internal double _DM_Digestibility;
        public double DM_Digestibility { get { return _DM_Digestibility; } set { _DM_Digestibility = value; } }

        /// <summary>
        /// Metabolizable energy:DM, MJ/kg
        /// </summary>
        internal double _ME_2_DM;
        public double ME_2_DM { get { return _ME_2_DM; } set { _ME_2_DM = value; } }

        /// <summary>
        /// Ether-extractable fraction
        /// </summary>
        internal double _EtherExtract;
        public double EtherExtract { get { return _EtherExtract; } set { _EtherExtract = value; } }

        /// <summary>
        /// Proportion which is crude protein
        /// </summary>
        internal double _CrudeProt;
        public double CrudeProt { get { return _CrudeProt; } set { _CrudeProt = value; } }

        /// <summary>
        /// Proportion of protein that is rumen-degradable
        /// </summary>
        internal double _DgProt;
        public double DgProt { get { return _DgProt; } set { _DgProt = value; } }

        /// <summary>
        /// Acid detergent insoluble protein:CP
        /// </summary>
        internal double _ADIP_2_CP;  
        public double ADIP_2_CP { get { return _ADIP_2_CP; } set { _ADIP_2_CP = value; } }

        /// <summary>
        /// Phosphorus content (P:DM)
        /// </summary>
        internal double _Phosphorus;
        public double Phosphorus { get { return _Phosphorus; } set { _Phosphorus = value; } }

        /// <summary>
        /// Sulphur content (S:DM)
        /// </summary>
        internal double _Sulphur;
        public double Sulphur { get { return _Sulphur; } set { _Sulphur = value; } }

        /// <summary>
        /// Ash alkalinity (mol/kg)
        /// </summary>
        internal double _AshAlkalinity;
        public double AshAlkalinity { get { return _AshAlkalinity; } set { _AshAlkalinity = value; } }

        /// <summary>
        /// Max. proportion passing through gut (used with whole grains)
        /// </summary>
        internal double _MaxPassage;
        public double MaxPassage { get { return _MaxPassage; } set { _MaxPassage = value; } }

        /// <summary>
        /// Indexer to allow easy access of attributes of a supplement
        /// </summary>
        /// <param name="attr">attibute to be retrieved or set</param>
        public double this[TSuppAttribute attr]
        {
            get
            {
                switch (attr)
                {
                    case TSuppAttribute.spaDMP: return _DM_Propn;
                    case TSuppAttribute.spaDMD: return _DM_Digestibility;
                    case TSuppAttribute.spaMEDM: return _ME_2_DM;
                    case TSuppAttribute.spaEE: return _EtherExtract;
                    case TSuppAttribute.spaCP: return _CrudeProt;
                    case TSuppAttribute.spaDG: return _DgProt;
                    case TSuppAttribute.spaADIP: return _ADIP_2_CP;
                    case TSuppAttribute.spaPH: return _Phosphorus;
                    case TSuppAttribute.spaSU: return _Sulphur;
                    case TSuppAttribute.spaAA: return _AshAlkalinity;
                    case TSuppAttribute.spaMaxP: return _MaxPassage;
                    default: return 0.0;
                }
            }

            set
            {
                switch (attr)
                {
                    case TSuppAttribute.spaDMP: _DM_Propn = value; break;
                    case TSuppAttribute.spaDMD: _DM_Digestibility = value; break;
                    case TSuppAttribute.spaMEDM: _ME_2_DM = value; break;
                    case TSuppAttribute.spaEE: _EtherExtract = value; break;
                    case TSuppAttribute.spaCP: _CrudeProt = value; break;
                    case TSuppAttribute.spaDG: _DgProt = value; break;
                    case TSuppAttribute.spaADIP: _ADIP_2_CP = value; break;
                    case TSuppAttribute.spaPH: _Phosphorus = value; break;
                    case TSuppAttribute.spaSU: _Sulphur = value; break;
                    case TSuppAttribute.spaAA: _AshAlkalinity = value; break;
                    case TSuppAttribute.spaMaxP: _MaxPassage = value; break;
                }
            }
        }

        private const double Rghg_MEDM_Intcpt = -1.707;
        private const double Rghg_MEDM_DMD = 17.2;
        private const double Conc_MEDM_Intcpt = 1.3;
        private const double Conc_MEDM_DMD = 13.3;
        private const double Conc_MEDM_EE = 23.4;
        public const double N2PROTEIN = 6.25;

        // ConvertDMD_To_ME2DM                                                          
        // ConvertME2DM_To_DMD                                                          
        // Routines for default conversions between M/D and DMD                         
        //   fDMD       Dry matter digestibility  (0-1)                                 
        //   fME2DM     M/D ratio                 (MJ/kg)                               
        //   fEE        Ether-extractable content (0-1)                                 

        /// <summary>
        /// Routine for default conversion from DMD to M/D
        /// </summary>
        /// <param name="fDMD">Dry matter digestibility  (0-1)</param>
        /// <param name="isRoughage">True if the supplement is a roughage</param>
        /// <param name="fEE">Ether-extractable content (0-1)</param>
        /// <returns>M/D ratio (MJ/kg)</returns>
        public static double ConvertDMD_To_ME2DM(double fDMD, bool isRoughage, double fEE)
        {
            if (isRoughage)
                return System.Math.Max(0.0, Rghg_MEDM_Intcpt + Rghg_MEDM_DMD * fDMD);
            else
                return System.Math.Max(0.0, Conc_MEDM_Intcpt + Conc_MEDM_DMD * fDMD + Conc_MEDM_EE * fEE);
        }

        /// <summary>
        /// Routine for default conversion from M/D to DMD
        /// </summary>
        /// <param name="fME2DM">M/D ratio (MJ/kg)</param>
        /// <param name="isRoughage">True if the supplement is a roughage</param>
        /// <param name="fEE">Ether-extractable content (0-1)</param>
        /// <returns>Dry matter digestibility  (0-1)</returns>
        public static double ConvertME2DM_To_DMD(double fME2DM, bool isRoughage, double fEE)
        {
            double result;
            if (isRoughage)
                result = (fME2DM - Rghg_MEDM_Intcpt) / Rghg_MEDM_DMD;
            else
                result = (fME2DM - (Conc_MEDM_EE * fEE + Conc_MEDM_Intcpt)) / Conc_MEDM_DMD;
            return System.Math.Max(0.0, System.Math.Min(0.9999, result));
        }

        /// <summary>
        /// Mix two supplements together and store in Self                               
        /// Will work if Supp1=this or Supp2=this
        /// This method is only exact if the passage rates of the two supplements are equal                                                                      
        /// </summary>
        public void Mix(TSupplement Supp1, TSupplement Supp2, double propn1)
        {
            if (propn1 >= 0.50)
                IsRoughage = Supp1.IsRoughage;
            else
                IsRoughage = Supp2.IsRoughage;
            double propn2 = 1.0 - propn1;                                  // Proportion of suppt 2 on a FW basis
            double DMpropn1 = Utility.Math.Divide(propn1 * Supp1.DM_Propn, // Proportion of suppt 1 on a DM basis
                      propn1 * Supp1.DM_Propn + propn2 * Supp2.DM_Propn, 0.0);
            double DMpropn2 = 1.0 - DMpropn1;                              // Proportion of suppt 2 on a DM basis 

            double CPpropn1;                                               // Proportion of suppt 1 on a total CP basis
            if (propn1 * Supp1.DM_Propn * Supp1.CrudeProt + propn2 * Supp2.DM_Propn * Supp2.CrudeProt > 0.0)
                CPpropn1 = propn1 * Supp1.DM_Propn * Supp1.CrudeProt
                   / (propn1 * Supp1.DM_Propn * Supp1.CrudeProt + propn2 * Supp2.DM_Propn * Supp2.CrudeProt);
            else
                CPpropn1 = propn1;
            double CPpropn2 = 1.0 - CPpropn1;                             // Proportion of suppt 1 on a total CP basis

            DM_Propn = propn1 * Supp1.DM_Propn + propn2 * Supp2.DM_Propn;
            DM_Digestibility = DMpropn1 * Supp1.DM_Digestibility + DMpropn2 * Supp2.DM_Digestibility;
            ME_2_DM = DMpropn1 * Supp1.ME_2_DM + DMpropn2 * Supp2.ME_2_DM;
            EtherExtract = DMpropn1 * Supp1.EtherExtract + DMpropn2 * Supp2.EtherExtract;
            CrudeProt = DMpropn1 * Supp1.CrudeProt + DMpropn2 * Supp2.CrudeProt;
            DgProt = CPpropn1 * Supp1.DgProt + CPpropn2 * Supp2.DgProt;
            ADIP_2_CP = CPpropn1 * Supp1.ADIP_2_CP + CPpropn2 * Supp2.ADIP_2_CP;
            Phosphorus = DMpropn1 * Supp1.Phosphorus + DMpropn2 * Supp2.Phosphorus;
            Sulphur = DMpropn1 * Supp1.Sulphur + DMpropn2 * Supp2.Sulphur;
            AshAlkalinity = DMpropn1 * Supp1.AshAlkalinity + DMpropn2 * Supp2.AshAlkalinity;
            MaxPassage = DMpropn1 * Supp1.MaxPassage + DMpropn2 * Supp2.MaxPassage;
        }

        public void MixMany(TSupplement[] supps, double[] amounts)
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

        public void MixMany(TSupplementItem[] supps)
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

        public void Assign(TSupplement srcSupp)
        {
            if (srcSupp != null)
            {
                sName = srcSupp.sName;
                IsRoughage = srcSupp.IsRoughage;
                DM_Propn = srcSupp.DM_Propn;
                DM_Digestibility = srcSupp.DM_Digestibility;
                ME_2_DM = srcSupp.ME_2_DM;
                EtherExtract = srcSupp.EtherExtract;
                CrudeProt = srcSupp.CrudeProt;
                DgProt = srcSupp.DgProt;
                ADIP_2_CP = srcSupp.ADIP_2_CP;
                Phosphorus = srcSupp.Phosphorus;
                Sulphur = srcSupp.Sulphur;
                AshAlkalinity = srcSupp.AshAlkalinity;
                MaxPassage = srcSupp.MaxPassage;
                Array.Resize(ref FTranslations, srcSupp.FTranslations.Length);
                Array.Copy(srcSupp.FTranslations, FTranslations, srcSupp.FTranslations.Length);
            }
        }

        /// <summary>
        /// constructor
        /// </summary>
        public TSupplement()
        {
            if (TSupplementLibrary.DefaultSuppConsts != null && TSupplementLibrary.DefaultSuppConsts.Count > 0)
            {
                Assign(TSupplementLibrary.DefaultSuppConsts[0]);
                FTranslations = new TTranslation[0];
            }
        }

        /// <summary>
        /// constructor with text argument
        /// </summary>
        public TSupplement(string suppSt)
        {
            ParseText(suppSt, false);
        }

        /// <summary>
        /// copy consructor
        /// </summary>
        public TSupplement(TSupplement src)
        {
            Assign(src);
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
        private bool ParseKeyword(ref string suppSt, string token, string units,
                                  double scalar, ref double value)
        {
            bool result = Utility.String.MatchToken(ref suppSt, token) &&
                          Utility.String.TokenDouble(ref suppSt, ref value) &&
                          Utility.String.MatchToken(ref suppSt, units);
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
        /// <param name="SuppSt">string to parse</param>
        /// <param name="bNameOnly"></param>
        public void ParseText(string suppSt, bool bNameOnly)
        {
            suppSt = suppSt.Trim();
            int suppNo = -1;
            int iMatchLen = 0;
            string nameStr;

            // Search for a matching name
            for (int idx = 0; idx < TSupplementLibrary.DefaultSuppConsts.Count; idx++)
            {
                string sSuppName = TSupplementLibrary.DefaultSuppConsts[idx].sName.ToUpper();
                if (sSuppName.StartsWith(suppSt.ToUpper()) && sSuppName.Length > iMatchLen)
                {
                    suppNo = idx;
                    iMatchLen = sSuppName.Length;
                }
            }

            if (suppNo >= 0) // If we have a match, initialise to it
                Assign(TSupplementLibrary.DefaultSuppConsts[suppNo]);
            else             // ... otherwise set to an arbitrary concentrate and use the given name
            {
                sName = suppSt;
                Utility.String.TextToken(ref suppSt, out nameStr);
                if (!bNameOnly && suppSt != "")
                    sName = nameStr;
            }

            bool Continue = !bNameOnly;       //  Now parse the rest of the string as keyword/value/unit combinations.
            bool DMDSet = false;              // TRUE once digestibility has been read in
            bool MEDMSet = false;             // TRUE once ME:DM has been read in

            while (Continue)                  // Any breakdown results in the rest of the string being ignored
            {
                if (ParseKeyword(ref suppSt, "DM_PC", "%", 0.01, ref _DM_Propn)
                   || ParseKeyword(ref suppSt, "CP", "%", 0.01, ref _CrudeProt)
                   || ParseKeyword(ref suppSt, "DG", "%", 0.01, ref _DgProt))
                    Continue = true;
                else if (ParseKeyword(ref suppSt, "DMD", "%", 0.01, ref _DM_Digestibility))
                {
                    Continue = true;
                    DMDSet = true;
                }
                else if (ParseKeyword(ref suppSt, "ME2DM", "MJ", 1.0, ref _ME_2_DM))
                {
                    Continue = true;
                    MEDMSet = true;
                }
                else
                    Continue = false;
            }

            if (DMDSet && !MEDMSet)
                ME_2_DM = DefaultME2DM();
            else if (!DMDSet && MEDMSet)
                DM_Digestibility = DefaultDMD();
        }

        public void AddTranslation(string lang, string text)
        {
            bool found = false;
            for (int idx = 0; idx < FTranslations.Length; idx++)
            {
                if (FTranslations[idx].sLang.Equals(text, StringComparison.OrdinalIgnoreCase))
                {
                    FTranslations[idx].sText = text;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Array.Resize(ref FTranslations, FTranslations.Length + 1);
                FTranslations[FTranslations.Length - 1] = new TTranslation();
                FTranslations[FTranslations.Length - 1].sLang = lang;
                FTranslations[FTranslations.Length - 1].sText = text;
            }
            if (lang.Equals("en", StringComparison.OrdinalIgnoreCase) || sName == "")
                sName = text;
        }

        public void DefaultFromName()
        {
            ParseText(sName, true);
        }

        /// <summary>
        /// Computes a default value for DM digestibility from a (known) M/D ratio 
        /// </summary>
        public double DefaultDMD()
        {
            return ConvertME2DM_To_DMD(ME_2_DM, IsRoughage, EtherExtract);
        }

        /// <summary>
        /// Computes a default value for M/D from a (known) DM digestibility          }
        /// </summary>
        public double DefaultME2DM()
        {
            return ConvertDMD_To_ME2DM(DM_Digestibility, IsRoughage, EtherExtract);
        }

        /// <summary>
        /// Calculates the default acid-detergent insoluble protein : crude protein 
        /// ratio for user defined supplements.                                      
        /// </summary>
        public double DefaultADIP_2_CP()
        {
            double result;
            if (IsRoughage)
                result = 0.19 * (1.0 - DgProt);
            else
                result = System.Math.Max(0.03, 0.87 - 1.09 * DgProt);

            // Post-Condition
            if ((result < 0.0) || (result > 1.0))
                throw new Exception(string.Format("Post condition failed in TSupplement.DefaultADIP_2_CP: Result={0}", result));
            return result;
        }

        /// <summary>
        /// Calculates the default phosphorus content for user defined supplements.
        /// </summary>                                                                       
        public double DefaultPhosphorus()
        {
            double result;
            if (IsRoughage)
                result = 0.0062 * DM_Digestibility - 0.0016;
            else
                result = 0.051 * DgProt * CrudeProt;

            // Post-Condition
            if ((result < 0.0) || (result > 1.0))
                throw new Exception(string.Format("Post condition failed in TSupplement.DefaultPhosphorus: Result={0}", result));
            return result;
        }

        /// <summary>
        /// Calculates the default sulphur content for user defined supplements.
        /// </summary>
        public double DefaultSulphur()
        {
            double result;
            if (IsRoughage)
                result = 0.0095 * CrudeProt + 0.0011;
            else
                result = 0.0126 * CrudeProt;

            // Post-Condition
            if ((result < 0.0) || (result > 1.0))
                throw new Exception(string.Format("Post condition failed in TSupplement.DefaultSulphur: Result={0}", result));
            return result;
        }

        public bool isSameAs(TSupplement otherSupp)
        {
            return (sName == otherSupp.sName)
            && (IsRoughage == otherSupp.IsRoughage)
            && (DM_Propn == otherSupp.DM_Propn)
            && (DM_Digestibility == otherSupp.DM_Digestibility)
            && (ME_2_DM == otherSupp.ME_2_DM)
            && (EtherExtract == otherSupp.EtherExtract)
            && (CrudeProt == otherSupp.CrudeProt)
            && (DgProt == otherSupp.DgProt)
            && (ADIP_2_CP == otherSupp.ADIP_2_CP)
            && (Phosphorus == otherSupp.Phosphorus)
            && (Sulphur == otherSupp.Sulphur)
            && (AshAlkalinity == otherSupp.AshAlkalinity)
            && (MaxPassage == otherSupp.MaxPassage);
        }
    }

    /// <summary>
    /// A record to allow us to hold amount and cost information along
    /// with the TSupplement information
    /// In TSupplementItem, the "amount" should be read as kg of supplement fresh
    /// weight. and the cost should be per kg fresh weight.
    /// </summary>
    [Serializable]
    public class TSupplementItem : TSupplement
    {
        public double Amount { get; set; }
        public double Cost { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public TSupplementItem() : base()
        {
        }

        /// <summary>
        /// Constructor
        /// Note that it makes a copy of the TSupplement
        /// </summary>
        public TSupplementItem(TSupplement src, double amt = 0.0, double cst = 0.0) : base(src)
        {
            Amount = amt;
            Cost = cst;
        }

        public void Assign(TSupplementItem srcSupp)
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
    ///  TSupplementRation encapsulates zero or more supplements mixed together.
    ///  In essence, it is a list of TSupplementItem.
    ///  This is the class used for specifying the supplement fed to a group of
    ///  animals in AnimGrp.pas 
    ///                                     
    /// Apart from the usual read/write properties and list-handling methods, the
    /// class has the following special methods:
    /// * AverageSuppt      computes the composition of a supplement mixture in
    ///                     proportions given by the fAmount values.
    /// * AverageCost       computes the cost of a supplement mixture in
    ///                     proportions given by the fAmount values.
    /// </summary>
    [Serializable]
    public class TSupplementRation
    {
        [Serializable]
        public enum TRationChoice
        {
            rcStandard,    //standard mix as specified
            rcOnlyStored,  //use only stored fodder while it lasts
            rcIncStored    //use stored fodder as first ingredient
        };

        protected TSupplementItem[] fSuppts = new TSupplementItem[0];

        TRationChoice rationChoice = TRationChoice.rcStandard;

        public int Count
        {
            get
            {
                return fSuppts.Length;
            }
        }

        public double TotalAmount
        {
            get
            {
                double totAmt = 0.0;
                for (int i = 0; i < fSuppts.Length; i++)
                    totAmt += fSuppts[i].Amount;
                return totAmt;
            }
            set
            {
                double fScale = 0.0;
                double totAmt = TotalAmount;
                if (totAmt > 0.0)
                    fScale = value / totAmt;
                else if (fSuppts.Length > 0)
                    fScale = value / fSuppts.Length;

                for (int i = 0; i < fSuppts.Length; i++)
                    fSuppts[i].Amount *= fScale;
            }
        }

        public TSupplementItem this[int idx]
        {
            get
            {
                return fSuppts[idx];
            }
            set
            {
                // Note: Assigning to the Suppt property copies the attributes of the source
                // TSupplement, not the TSupplement instance itself.
                if (idx >= fSuppts.Length)
                {
                    Array.Resize(ref fSuppts, (int)idx + 1);
                    fSuppts[idx] = new TSupplementItem(value);
                }
            }
        }

        public void Assign(TSupplementRation srcRation)
        {
            Array.Resize(ref fSuppts, srcRation.Count);
            for (int idx = 0; idx < srcRation.Count; idx++)
            {
                if (fSuppts[idx] == null)
                    fSuppts[idx] = new TSupplementItem();
                fSuppts[idx].Assign(srcRation[idx]);
            }
            rationChoice = srcRation.rationChoice;
        }

        public double getFWFract(int idx)
        {
            return fSuppts[idx].Amount >= 1e-7 ? fSuppts[idx].Amount / TotalAmount : 0.0;
        }

        public int Add(TSupplement supp, double amt = 0.0, double cost = 0.0)
        {
            int idx = fSuppts.Length;
            Insert(idx, supp, amt, cost);
            return idx;
        }

        public int Add(TSupplementItem suppItem)
        {
            return Add(suppItem, suppItem.Amount, suppItem.Cost);
        }

        public void Insert(int idx, TSupplement supp, double amt = 0.0, double cost = 0.0)
        {
            Array.Resize(ref fSuppts, fSuppts.Length + 1);
            for (int jdx = fSuppts.Length - 1; jdx > idx; jdx--)
                fSuppts[jdx] = fSuppts[jdx - 1];
            fSuppts[idx] = new TSupplementItem(supp, amt, cost);
        }

        public void Delete(int idx)
        {
            for (int jdx = idx + 1; jdx < fSuppts.Length; jdx++)
                fSuppts[jdx - 1] = fSuppts[jdx];
            Array.Resize(ref fSuppts, fSuppts.Length - 1);
        }

        public void Clear()
        {
            Array.Resize(ref fSuppts, 0);
        }

        public int IndexOf(string sName, bool checkTrans = false)
        {
            int result = -1;
            for (int idx = 0; idx < fSuppts.Length; idx++)
            {
                if (fSuppts[idx].sName.Equals(sName, StringComparison.OrdinalIgnoreCase))
                {
                    result = idx;
                    break;
                }
            }
            if (result < 0 && checkTrans)
            {
                for (int idx = 0; idx < fSuppts.Length; idx++)
                    for (int itr = 0; itr < fSuppts[idx].FTranslations.Length; itr++)
                        if (fSuppts[idx].FTranslations[itr].sText.Equals(sName, StringComparison.OrdinalIgnoreCase))
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
        /// <param name="aveSupp">receives the average supplement composition</param>
        public void AverageSuppt(out TSupplement aveSupp)
        {
            aveSupp = new TSupplement();
            if (TotalAmount > 0.0)
                aveSupp.MixMany(fSuppts);
        }

        /// <summary>
        /// Weighted average cost of a supplement
        /// </summary>
        /// <returns>The weighted average cost in the same units as TSupplementItem.cost</returns>
        public double AverageCost()
        {
            if (TotalAmount < 1e-7)
                return 0.0;
            double totCost = 0.0;
            for (int idx = 0; idx < fSuppts.Length; idx++)
                totCost += fSuppts[idx].Amount * fSuppts[idx].Cost;
            return totCost / TotalAmount;
        }

        static TSupplement.TSuppAttribute[] PROPN_ATTRS = { TSupplement.TSuppAttribute.spaDMP, 
                                                            TSupplement.TSuppAttribute.spaDMD, 
                                                            TSupplement.TSuppAttribute.spaEE, 
                                                            TSupplement.TSuppAttribute.spaCP, 
                                                            TSupplement.TSuppAttribute.spaDG, 
                                                            TSupplement.TSuppAttribute.spaADIP, 
                                                            TSupplement.TSuppAttribute.spaPH, 
                                                            TSupplement.TSuppAttribute.spaSU, 
                                                            TSupplement.TSuppAttribute.spaMaxP };
        /// <summary>
        /// Scales the attributes of the members of the supplement so that the weighted
        /// average attributes match those of aveSupp. Ensures that fractional values
        /// remain within the range 0-1
        /// * Assumes that all values are non-negative
        /// </summary>
        /// <param name="scaleToSupp"></param>
        /// <param name="attrs"></param>
        public void RescaleRation(TSupplement scaleToSupp, IList<TSupplement.TSuppAttribute> attrs)
        {
            Array attribs = Enum.GetValues(typeof(TSupplement.TSuppAttribute));
            foreach (TSupplement.TSuppAttribute attr in attribs)  // NB this only works becuase of the way the supplement attributes are ordered, i.e. DM proportion first and CP before dg and ADIP:CP                         }
            {                                                     // i.e. DM proportion first and CP before dg and ADIP:CP
                if (attrs.Contains(attr))
                {
                    double newWtMean = scaleToSupp[attr];

                    if (fSuppts.Length == 1)
                        fSuppts[0][attr] = newWtMean;
                    else
                    {
                        double oldWtMean = 0.0;
                        double totalWeight = 0.0;
                        double fWeight = 0.0;
                        for (int idx = 0; idx < fSuppts.Length; idx++)
                        {
                            switch (attr)
                            {
                                case TSupplement.TSuppAttribute.spaDMP:
                                    fWeight = getFWFract(idx);
                                    break;
                                case TSupplement.TSuppAttribute.spaDG:
                                case TSupplement.TSuppAttribute.spaADIP:
                                    fWeight = getFWFract(idx) * fSuppts[idx].DM_Propn * fSuppts[idx].CrudeProt;
                                    break;
                                default:
                                    fWeight = getFWFract(idx) * fSuppts[idx].DM_Propn;
                                    break;
                            }
                            oldWtMean += fWeight * fSuppts[idx][attr];
                            totalWeight += fWeight;
                        }
                        if (totalWeight > 0.0)
                            oldWtMean /= totalWeight;

                        for (int idx = 0; idx < fSuppts.Length; idx++)
                        {
                            if (totalWeight == 0.0)
                                fSuppts[idx][attr] = newWtMean;
                            else if ((newWtMean < oldWtMean) || (!PROPN_ATTRS.Contains(attr)))
                                fSuppts[idx][attr] *= newWtMean / oldWtMean;
                            else
                                fSuppts[idx][attr] += (1.0 - fSuppts[idx][attr]) * (newWtMean - oldWtMean)
                                                        / (1.0 - oldWtMean);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// TSupplementLibrary is a TSupplementRation descendant that is intended for
    /// use in manipulating lists of supplements within GUIs.
    /// For TSupplementLibrary, the "amounts" may be read in relative or absolute
    /// terms, depending on the application. 
    ///
    /// Apart from the usual read/write properties and list-handling methods, the
    /// class has the following special methods:
    /// * the Add and Insert methods have variants that allow the user to set up a
    ///   supplement by using its name; other attributes are looked up from the
    ///   DefaultSuppConsts library.
    /// * PopulateDefaults     sets the library up to contain the complete set of
    ///                        default supplement compositions.
    /// * CopyFrom             adds either the entire contents of another library,
    ///                        or else a nominated subset of supplements from the
    ///                        other library.
    /// * ReadFromRegistry     Populates the library from a set of formatted strings
    ///                        contained in a file pointed to by SUPP_LIB_KEY
    /// * WriteToRegistry      Write a formatted set of strings that can be read by
    ///                        ReadFromStrings to the file pointed to by
    ///                        in a file pointed to by SUPP_LIB_KEY
    /// </summary>
    [Serializable]
    public class TSupplementLibrary : TSupplementRation
    {
        public void Add(string sName, double amount = 0.0, double cost = 0.0)
        {
            Insert(Count, sName, amount, cost);
        }

        public void Insert(int idx, string sName, double amount = 0.0, double cost = 0.0)
        {
            TSupplementItem defSupp = new TSupplementItem();
            GetDefaultSupp(sName, ref defSupp);
            Insert(idx, defSupp, amount, cost);
        }

        /// <summary>
        /// Locates a supplement by name in the DefaultSupptCosts array and returns it
        /// </summary>
        public bool GetDefaultSupp(string sName, ref TSupplementItem suppt)
        {
            int idx = DefaultSuppConsts.IndexOf(sName);
            bool result = idx >= 0;

            if (result)
            {
                if (suppt == null)
                    suppt = new TSupplementItem();
                suppt.Assign(DefaultSuppConsts[idx]);
            }
            return result;
        }

        public void PopulateDefaults()
        {
            Assign(DefaultSuppConsts);
        }

        public void RevertToDefault(int idx)
        {
            GetDefaultSupp(fSuppts[idx].sName, ref fSuppts[idx]);
        }

        public void CopyFrom(TSupplementLibrary srcLibrary, string[] copyNames = null)
        {
            for (int idx = 0; idx < srcLibrary.Count; idx++)
            {
                if (copyNames == null || Array.IndexOf(copyNames, srcLibrary[idx].sName) >= 0)
                    Add(new TSupplementItem(srcLibrary[idx], srcLibrary[idx].Amount, srcLibrary[idx].Cost));
            }
        }

        private const string sATTR_HEADER = "R    DM    DMD    M/D     EE     CP     dg    ADIP     P        S       AA    MaxP Locales";

        public void ReadFromStrings(string locale, string[] strings)
        {
            if (strings == null || strings.Length == 0)
                throw new Exception("Error reading supplement library - must contain a header line");

            int iAttrPosn = strings[0].IndexOf('|');       // Every line must have a | character in
            // this column
            string sHdrStr = "Name";
            sHdrStr = sHdrStr.PadRight(iAttrPosn, ' ') + "|" + sATTR_HEADER;

            if (strings[0] != sHdrStr)
                throw new Exception("Error reading supplement library - header line is invalid");

            Clear();

            string sNameStr = "";
            try
            {
                for (int idx = 1; idx < strings.Length; idx++)
                {
                    if (strings[idx].Length < iAttrPosn)
                        continue;
                    sNameStr = strings[idx].Substring(0, iAttrPosn - 1).Trim();
                    string sAttrStr = strings[idx].Substring(iAttrPosn + 1);

                    TSupplement newSupp = new TSupplement();
                    newSupp.sName = sNameStr;
                    newSupp.IsRoughage = sAttrStr[0] == 'Y';
                    sAttrStr = sAttrStr.Remove(0, 1);

                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._DM_Propn);
                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._DM_Digestibility);
                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._ME_2_DM);
                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._EtherExtract);
                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._CrudeProt);
                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._DgProt);
                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._ADIP_2_CP);
                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._Phosphorus);
                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._Sulphur);
                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._AshAlkalinity);
                    Utility.String.TokenDouble(ref sAttrStr, ref newSupp._MaxPassage);

                    sAttrStr = sAttrStr.Trim();
                    string sTransStr;
                    string sTransName;
                    string sLocStr;
                    string sLang;
                    int iBlank = sAttrStr.IndexOf(' ');
                    if (iBlank < 0)
                    {
                        sLocStr = sAttrStr;
                        sTransStr = "";
                    }
                    else
                    {
                        sLocStr = sAttrStr.Substring(0, iBlank);
                        sTransStr = sAttrStr.Substring(iBlank + 1).Trim();
                    }

                    while (sTransStr != "")
                    {
                        Utility.String.TextToken(ref sTransStr, out sLang);
                        if (sTransStr[0] == ':')
                        {
                            sTransStr = sTransStr.Substring(1);
                            Utility.String.TextToken(ref sTransStr, out sTransName, true);
                            newSupp.AddTranslation(sLang, sTransName);
                        }
                        if (sTransStr.Length > 0 && sTransStr[0] == ';')
                            sTransStr = sTransStr.Substring(1);
                    }

                    if (sLocStr == "" || GrazParam.InLocale(locale, sLocStr))
                        Add(newSupp, 0.0, 0.0);
                }
            }
            catch (Exception)
            {
                throw new Exception("Error reading supplement library - line for " + sNameStr + " is invalid");
            }
        }

        public static TSupplementLibrary DefaultSuppConsts
        {
            get
            {
                if (GDefSupp == null)
                {
                    GDefSupp = new TSupplementLibrary();
                    SetupDefaultSupplements();
                }
                return GDefSupp;
            }
        }

        public static void SetupDefaultSupplements()
        {
            if (!DefaultSuppConsts.ReadFromRegistryFile(GrazParam.DefaultLocale()))
                DefaultSuppConsts.ReadFromResource(GrazParam.DefaultLocale());
        }

        public bool ReadFromRegistryFile(string locale)
        {
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(GrazParam.PARAM_KEY);
            if (regKey != null)
            {
                string suppFile = (string)regKey.GetValue("supplib");
                if (!string.IsNullOrEmpty(suppFile) && System.IO.File.Exists(suppFile))
                {
                    string[] suppStrings = System.IO.File.ReadAllLines(suppFile);
                    ReadFromStrings(locale, suppStrings);
                    return true;
                }
            }
            return false;
        }

        public void ReadFromResource(string locale)
        {
            string suppData = Properties.Resources.ResourceManager.GetString("Supplement");
            string[] suppStrings = suppData.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            ReadFromStrings(locale, suppStrings);
        }

        internal static TSupplementLibrary GDefSupp = null;

    }

}
