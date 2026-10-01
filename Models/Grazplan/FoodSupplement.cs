// -----------------------------------------------------------------------
// The GrazPlan Supplement objects
// -----------------------------------------------------------------------
using System;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Newtonsoft.Json;

namespace Models.GrazPlan
{
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
}
