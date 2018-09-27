namespace Models.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Linq;
    using Newtonsoft.Json;
    using System.Xml;

    /// <summary>
    /// A class for reading and writing the .apsimx file format.
    /// </summary>
    public class Format
    {
        /// <summary>
        /// Convert a string (json or xml) to a model
        /// </summary>
        /// <param name="st">The string to convert</param>
        public static T StringToModel<T>(string st)
        {
            // Run the converter.
            T modelToReturn;

            int offset = st.TakeWhile(c => char.IsWhiteSpace(c)).Count();
            char firstNonBlankChar = st[offset];

            if (firstNonBlankChar == '{')
            {
                JsonSerializer serializer = new JsonSerializer()
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };
                using (var sw = new StringReader(st))
                using (var reader = new JsonTextReader(sw))
                {
                    modelToReturn = serializer.Deserialize<T>(reader);
                }
            }
            else
            {
                //using (Stream inStream = Converter.ConvertToLatestVersion(FileName))
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(st);
                modelToReturn = (T) XmlUtilities.Deserialise(doc.DocumentElement, Assembly.GetExecutingAssembly());
            }

            //if (simulations.Version > ApsimFile.Converter.LatestVersion)
            //    throw new Exception("This file has previously been opened with a more recent version of Apsim. Please upgrade to a newer version to open this file.");

            return modelToReturn;
        }
    }
}
