using NUnit.Framework;
using System.IO;
using System.Reflection;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Newtonsoft.Json.Linq;

namespace UnitTests.Resources
{
    /// <summary>
    /// Basic tests for consistency among the released models.
    /// </summary>
    public class ResourceTests
    {
        /// <summary>
        /// Checks all resources files for released models (all resources 
        /// under Models.Resources). Ensures that all released modlels have
        /// a top-level simulations node and that they are converted to the
        /// latest version when read.
        /// </summary>
        [Test]
        public void EnsureResourcesUpToDate()
        {
            Assembly models = typeof(IModel).Assembly;
            foreach (string resourceName in models.GetManifestResourceNames())
            {
                if (resourceName.StartsWith("Models.Resources.") && resourceName.EndsWith(".json"))
                {
                    string resource;
                    using (Stream stream = models.GetManifestResourceStream(resourceName))
                        using (StreamReader reader = new StreamReader(stream))
                            resource = reader.ReadToEnd();

                    // Assume resource is json.
                    JObject root = JObject.Parse(resource);
                    Assert.That(root.ContainsKey("Version"), $"Resource '{resourceName}' does not contain a version number at the root level node.");

                    int version = (int)root["Version"];
                    Assert.That(version == Converter.LatestVersion, $"Resource '{resourceName}' is not up to date - version is {version} but latest version is {Converter.LatestVersion}.");

                    IModel model = FileFormat.ReadFromString<IModel>(resource, out _);
                    Assert.That(model is Simulations, $"Resource '{resourceName}' does not contain a top-level simulations node.");

                    int simulationsVersion = (model as Simulations).Version;
                    Assert.That(simulationsVersion == Converter.LatestVersion, $"Resource '{resourceName}' does not get converted to latest version when opened.");
                }
            }
        }
    }
}
