using System.Reflection;
using APSIM.Shared.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        Type typeToCreate = ModelNameToType(modelNameToCreate)
                            ?? throw new Exception($"Unknown model type {modelNameToCreate}");

        var model = (INodeModel)Activator.CreateInstance(typeToCreate, true)
                    ?? throw new Exception($"Cannot create a model of type {typeToCreate.Name}");
        return model;
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