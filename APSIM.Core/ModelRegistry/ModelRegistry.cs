using System.Reflection;
using APSIM.Shared.Utilities;
using Newtonsoft.Json.Linq;

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
        Type[] modelTypes = ReflectionUtilities.GetTypeWithoutNameSpace(modelNameToCreate, modelsAssembly);
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
        if (typeToCreate != null)
        {
            INodeModel model = (INodeModel)Activator.CreateInstance(typeToCreate, true);
            if (model == null)
                throw new Exception($"A {typeToCreate.Name} model could not be found or could not be created.");
            return model;
        }
        else
        {
            // Try and see if this is a resource.
            string[] names = modelsAssembly.GetManifestResourceNames();
            string resourceName = names.FirstOrDefault(r => r.Equals($"Models.Resources.{modelNameToCreate}.json", StringComparison.InvariantCultureIgnoreCase));
            if (resourceName != null)
                return CreateResourceModelFromName(modelNameToCreate, resourceName);
            else
                throw new Exception($"A {typeToCreate.Name} model or resource could not be found.");
        }
    }

    /// <summary>
    /// Create a model from an embedded resource.
    /// </summary>
    /// <param name="modelNameToCreate">The name of the model to create.</param>
    /// <param name="resourceName">The name of the embedded resource.</param>
    /// <returns>The newly created instance.</returns>
    private static INodeModel CreateResourceModelFromName(string modelNameToCreate, string resourceName)
    {
        using Stream stream = modelsAssembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new Exception($"Could not find model or resource with the name: {resourceName}");
        
        using StreamReader reader = new StreamReader(stream);
        string json = reader.ReadToEnd();
        
        // Inject the ResourceName into the JSON before deserializing
        JObject jObject = JObject.Parse(json);
        // Get the first child of the root object and set its ResourceName property.
        // The actual resource always has a simulations Parent, so the first child of the root object is always the resource.
        jObject["Children"][0]["ResourceName"] = modelNameToCreate;
        
        INodeModel resource = FileFormat.ReadFromString<INodeModel>(jObject.ToString()).Model.GetChildren().FirstOrDefault();
        
        List<INodeModel> children = resource.GetChildren().ToList();
        foreach (INodeModel child in children)
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