using NUnit.Framework;
using System.IO;
using System.Reflection;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;

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

                    IModel model = FileFormat.ReadFromString<IModel>(resource, e => throw e, false).NewModel as IModel;
                    Assert.That(model is Simulations, $"Resource '{resourceName}' does not contain a top-level simulations node.");

                    int simulationsVersion = (model as Simulations).Version;
                    Assert.That(simulationsVersion == Converter.LatestVersion, $"Resource '{resourceName}' does not get converted to latest version when opened.");
                }
            }
        }

        /// <summary>
        /// This test will open a file containing the wheat model and will save the file.
        /// The wheat model should *not* be serialized - what should be serialized is a
        /// reference to the released model. Reproduces bug #4694.
        /// </summary>
        [Test]
        public void EnsureReleasedModelsAreNotSaved()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.WheatModel.apsimx");
            IModel topLevel = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as IModel;
            string serialized = FileFormat.WriteToString(topLevel);

            JObject root = JObject.Parse(serialized);

            // This file contains 2 wheat models - one under replacements, and one under a folder.
            JObject fullWheat = JsonUtilities.ChildWithName(JsonUtilities.ChildWithName(root, "Replacements"), "Wheat");
            JObject wheat = JsonUtilities.ChildWithName(JsonUtilities.ChildWithName(root, "Folder"), "Wheat");

            // The wheat model under replacements should have its full
            // model structure (properties + children) serialized.
            Assert.That(fullWheat, Is.Not.Null);
            Assert.That(JsonUtilities.Children(fullWheat).Count, Is.Not.EqualTo(0));

            // The wheat model under the folder should *not* have its
            // full model structure (properties + children) serialized.
            Assert.That(wheat, Is.Not.Null);
            Assert.That(JsonUtilities.Children(wheat).Count, Is.EqualTo(0));
        }
    }
}
