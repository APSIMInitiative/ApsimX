using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APSIM.Shared.Utilities;
using System.Xml;
using System.Globalization;
using System.Reflection;
using System.Xml.Serialization;

namespace Utility
{
    public class ConfigurationConverter
    {
        public const int LatestVersion = 2;

        public static Configuration DoConvert(string fileName)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(File.ReadAllText(fileName));

            XmlElement versionElement = document["Configuration"]["Version"];
            int version;
            if (versionElement == null)
                version = 0;
            else
                version = int.Parse(versionElement.InnerText, CultureInfo.InvariantCulture);

            while (version < LatestVersion)
            {
                version++;

                MethodInfo upgrader = typeof(ConfigurationConverter).GetMethod($"UpgradeToVersion{version}", BindingFlags.Static | BindingFlags.NonPublic);
                if (upgrader == null)
                    throw new Exception($"Unable to find configuration file converter for version {version}.");

                upgrader.Invoke(null, new object[] { document["Configuration"] });
            }

            if (document["Configuration"]["Version"] == null)
            {
                XmlNode versionNode = document.CreateNode(XmlNodeType.Element, "Version", "");
                document["Configuration"].AppendChild(versionNode);
            }

            document["Configuration"]["Version"].InnerText = version.ToString();

            XmlSerializer serializer = new XmlSerializer(typeof(Configuration), typeof(Configuration).GetNestedTypes());
            using (XmlReader reader = new XmlNodeReader(document))
            {
                Configuration configurationSettings = serializer.Deserialize(reader) as Configuration;
                // reset to false as this should be false at the beginning of every APSIM launch.
                configurationSettings.ThemeRestartRequired = false;
                return configurationSettings;
            }
        }

        /// <summary>
        /// Upgrades to version 1. Changes MRUList from a list of
        /// strings to a list of type ApsimFileMetadata.
        /// </summary>
        /// <param name="rootNode"></param>
        private static void UpgradeToVersion1(XmlNode rootNode)
        {
            XmlNode mruList = rootNode["MruList"];
            for (int i = 0; i < mruList.ChildNodes.Count; i++)
            {
                XmlNode child = mruList.ChildNodes[i];

                // Create a new ApsimFileMetadata object containing the filename.
                ApsimFileMetadata file = new ApsimFileMetadata(child.InnerText);

                // Serialize the ApsimFileMetadata.
                string xml = XmlUtilities.Serialise(file, false);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                // Import the serialized ApsimFileMetadata into this document,
                // replacing the existing string filename.
                XmlNode newChild = doc[typeof(ApsimFileMetadata).Name];
                newChild = mruList.OwnerDocument.ImportNode(newChild, true);
                mruList.ReplaceChild(newChild, child);
            }
        }

        private static void UpgradeToVersion2(XmlNode rootNode)
        {
            XmlNode fontNode = rootNode["Font"];
            if (fontNode != null)
            {
                string family = fontNode["Family"].InnerText;
                int size = int.Parse(fontNode["Size"].InnerText) / 1024;
                string gravity = fontNode["Gravity"].InnerText;
                string stretch = fontNode["Stretch"].InnerText;
                string style = fontNode["Style"].InnerText;
                string variant = fontNode["Variant"].InnerText;
                string weight = fontNode["Weight"].InnerText;

                string fontString = $"{family} {style} {variant} {weight} {stretch} {gravity} {size}";

                rootNode.RemoveChild(fontNode);
                XmlNode newFontNode = rootNode.OwnerDocument.CreateElement("FontName");
                newFontNode.InnerText = fontString;
                rootNode.AppendChild(newFontNode);
            }
        }
    }
}
