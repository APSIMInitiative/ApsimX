// -----------------------------------------------------------------------
// The GrazPlan Supplement objects
// -----------------------------------------------------------------------
using System;
using APSIM.Shared.Utilities;

namespace Models.GrazPlan
{
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
