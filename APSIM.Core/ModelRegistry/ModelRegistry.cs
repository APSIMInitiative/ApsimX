using System.Reflection;
using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>
/// Uses reflection to create a registry of models that can be discovered and created.
/// </summary>
internal class ModelRegistry
{
    private static Assembly modelsAssembly;

    private static readonly object lockObject = new object();

    /// <summary>
    /// Convert a model name to a .NET type. Will throw if not found.
    /// </summary>
    /// <param name="modelNameToCreate">Name of model.</param>
    /// <returns>The .NET type.</returns>
    internal static Type ModelNameToType(string modelNameToCreate)
    {
        DiscoverModels();
        var modelTypes = ReflectionUtilities.GetTypeWithoutNameSpace(modelNameToCreate, modelsAssembly);
        if (modelTypes.Length != 1)
            return null;
        Type typeToCreate = modelTypes.First();
        return typeToCreate;
    }

    /// <summary>
    /// Create an instance of a model. Will throw if not found.
    /// </summary>
    /// <param name="modelNameToCreate">The name of the model to create.</param>
    /// <returns>The newly created instance.</returns>
    internal static INodeModel CreateModel(string modelNameToCreate)
    {
        DiscoverModels();
        Type typeToCreate = ModelNameToType(modelNameToCreate);
        if (typeToCreate == null)
        {
            // Find and return a resource.
            var resourceName = modelsAssembly.GetManifestResourceNames().
                FirstOrDefault(r => r.Contains(modelNameToCreate, StringComparison.InvariantCultureIgnoreCase));
            if (resourceName != null)
                return CreateResourceModelFromName(modelNameToCreate, resourceName);
        }
        if (typeToCreate == null)
            throw new Exception($"Cannot find a model with name: {modelNameToCreate}");
                            


        var model = (INodeModel)Activator.CreateInstance(typeToCreate, true)
                    ?? throw new Exception($"Cannot create a model of type {typeToCreate.Name}");
        return model;
    }

    /// <summary>
    /// Create a model from an embedded resource.
    /// </summary>
    /// <param name="modelNameToCreate">The name of the model to create.</param>
    /// <param name="resourceName">The name of the embedded resource.</param>
    /// <returns>The newly created instance.</returns>
    private static INodeModel CreateResourceModelFromName(string modelNameToCreate, string resourceName)
    {
        using var stream = modelsAssembly.GetManifestResourceStream(resourceName)
            ?? throw new Exception($"Could not find model or resource with the name: {resourceName}");
        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();
        INodeModel resource = FileFormat.ReadFromString<INodeModel>(json).Model.GetChildren().FirstOrDefault();
        resource.ResourceName = modelNameToCreate;
        List<INodeModel> children = resource.GetChildren().ToList();
        foreach (var child in children)
            resource.RemoveChild(child);
        return resource;
    }

    /// <summary>
    /// Discover all models by looking in modelsAssembly.
    /// </summary>
    private static void DiscoverModels()
    {
        if (modelsAssembly == null)
        {
            lock (lockObject)
            {
                if (modelsAssembly == null)
                {
                    string binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    if (string.IsNullOrEmpty(binPath))
                        throw new InvalidOperationException("Could not determine the directory of the executing assembly. Cannot locate Models.dll.");
                    modelsAssembly = Assembly.LoadFrom(Path.Combine(binPath, "Models.dll"));
                }
            }
        }
    }

}