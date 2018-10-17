namespace Models.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// Converts the .apsim file from one version to the next
    /// </summary>
    public class Converter
    {
        /// <summary>Gets the latest .apsimx file format version.</summary>
        public static int LatestVersion { get { return 47; } }

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
                if (root.ContainsKey("Version"))
                {
                    int fileVersion = (int)root["Version"];

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
                    return false;
            }
            else
            {
                XmlConverters.DoConvert(ref st, Math.Min(toVersion, 46), fileName);
                st = ConvertToJSON(st, fileName);
                return true;
            }
        }

        /// <summary>Upgrades to version 47 - the first JSON version.</summary>
        private static string ConvertToJSON(string st, string fileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(st);
            var model = XmlUtilities.Deserialise(doc.DocumentElement, Assembly.GetExecutingAssembly()) as IModel;
            if (model is Simulations)
                (model as Simulations).Version = LatestVersion;
            return FileFormat.WriteToString(model);
        }


    }
}

