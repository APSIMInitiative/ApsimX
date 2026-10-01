// -----------------------------------------------------------------------
// The GrazPlan Supplement objects
// -----------------------------------------------------------------------
using System;

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
            if (OperatingSystem.IsWindows())
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
}
