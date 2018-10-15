namespace Models.Core.ApsimFile
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Converts the .apsim file from one version to the next
    /// </summary>
    public class Converter
    {
        /// <summary>Gets the latest .apsimx file format version.</summary>
        public static int LatestVersion { get { return 46; } }

        /// <summary>Converts a .apsimx string to the latest version.</summary>
        /// <param name="st">XML or JSON string to convert.</param>
        /// <param name="toVersion">The optional version to convert to.</param>
        /// <param name="fileName">The optional filename where the string came from.</param>
        /// <returns>Returns true if something was changed.</returns>
        public static bool DoConvert(ref string st, int toVersion = -1, string fileName = null)
        {
            if (toVersion == -1)
                toVersion = LatestVersion;

            int offset = st.TakeWhile(c => char.IsWhiteSpace(c)).Count();
            char firstNonBlankChar = st[offset];

            if (firstNonBlankChar == '{')
            {
                // json
                JObject root = JObject.Parse(st);
                int fileVersion = (int) root["Version"];

                // Update the xml if not at the latest version.
                bool changed = false;
                while (fileVersion < toVersion)
                {
                    changed = true;

                    // Find the method to call to upgrade the file by one version.
                    int versionFunction = fileVersion + 1;
                    MethodInfo method = typeof(Converter).GetMethod("UpgradeToVersion" + versionFunction, BindingFlags.NonPublic | BindingFlags.Static);
                    if (method == null)
                        throw new Exception("Cannot find converter to go to version " + versionFunction);

                    // Found converter method so call it.
                    method.Invoke(null, new object[] { root, fileName });

                    fileVersion++;
                }

                if (changed)
                {
                    root["Version"] = fileVersion;
                    st = root.ToString();
                }

                return true;
            }
            else
                return XmlConverters.DoConvert(ref st, toVersion, fileName);
        }

    }
}

