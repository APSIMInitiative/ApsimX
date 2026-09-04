using APSIM.Core;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using Models.Core;
using Newtonsoft.Json.Linq;
using Models.PMF;
using System.Linq;
using Models.Functions;
using System;
using APSIM.Shared.Utilities;

namespace UnitTests.APSIM.Core.Tests;

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

                IModel model = FileFormat.ReadFromString<Simulations>(resource).Model as IModel;
                Assert.That(model is Simulations, $"Resource '{resourceName}' does not contain a top-level simulations node.");

                int simulationsVersion = (model as Simulations).Version;
                Assert.That(simulationsVersion == Converter.LatestVersion, $"Resource '{resourceName}' does not get converted to latest version when opened.");
            }
        }
    }

    /// <summary>Ensure released models are loaded from resource.</summary>
    [Test]
    public void EnsureReleasedModelsAreLoadedFromResource()
    {
        var wheat = new Plant()
        {
            Name = "Wheat",
            ResourceName = "Wheat"
        };

        Node node = Node.Create(wheat);
        Assert.That(node.Children.Count(), Is.GreaterThan(1));
    }

    /// <summary>Ensure released models are not written to .apsimx file. Reproduces bug #4694.</summary>
    [Test]
    public void EnsureReleasedModelsAreNotSerialised()
    {
        var wheat = new Plant()
        {
            Name = "Wheat",
            ResourceName = "Wheat"
        };

        Node node = Node.Create(wheat);
        JObject root = JObject.Parse(node.ToJSONString());

        Assert.That(root, Is.Not.Null);
        Assert.That(JsonUtilities.Children(root).Count, Is.EqualTo(0));
    }

    /// <summary>
    /// Checks all resources file in the Validation directory within Tests for released models. 
    /// Ensures that all variable references in the resource files are resolved when the resource is read. 
    /// </summary>
    [Test]
    public void EnsureResourceVariableReferencesAreResolved()
    {
        string modelResourcesPath = PathUtilities.GetApsimXDirectory() + "/" + "Models/Resources";
        var validationPaths = Directory.GetFiles(modelResourcesPath, $"*.json", SearchOption.AllDirectories);
        foreach (string path in validationPaths)
        {
            string resourceName = Path.GetFileNameWithoutExtension(path);
            string resourceContent = File.ReadAllText(path);
            Simulations resourceAsSimulations = FileFormat.ReadFromString<Simulations>(resourceContent).Model as Simulations;
            // Get the resource and put it into another simulation with models that can be linked to.
            INodeModel resource = resourceAsSimulations.GetChildren().First();
            Simulations newSim = Utilities.GetRunnableSimWithExtras(true);
            Zone zone = newSim.Node.FindChild<Zone>("Zone", recurse: true);
            Models.Soils.Soil soil = newSim.Node.FindChild<Models.Soils.Soil>("Soil", recurse: true);
            if (resourceName == "WaterBalance")
                soil.AddChild(resource);
            else zone.AddChild(resource);
            Node.Create(newSim);

            foreach (var variableRef in newSim.Node.FindChildren<VariableReference>(recurse: true))
            {
                try
                {
                    if (!variableRef.Enabled)
                        continue;
                    var obj = newSim.Node.Locator.GetObject(variableRef.Node, variableRef.VariableName);
                    if (obj == null)
                        obj = newSim.Node.Locator.GetObject(newSim.Node, variableRef.VariableName);
                        if (obj == null)
                            Assert.Fail($"Variable reference '{variableRef.VariableName}' in file '{path}' could not be resolved." + 
                                $" Apsim Path to model containing variable reference is '{variableRef.FullPath}'. Actual variable reference value: {variableRef.VariableName}");
                }
                catch (Exception)
                {
                    // do nothing.
                }
            }
        }
    }
}
