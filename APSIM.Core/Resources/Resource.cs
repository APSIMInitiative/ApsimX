using System.Globalization;
using System.Reflection;
using APSIM.Shared.Utilities;
using Newtonsoft.Json.Linq;

namespace APSIM.Core;

/// <summary>
/// Resources encapsulates methods for accessing model resources (JSON files stored in
/// the Models assembly).
/// </summary>
public class Resource
{
    /// <summary>The instance of resource.</summary>
    private static Resource instance = null;

    /// <summary>A cache of models from resource.</summary>
    private readonly Dictionary<string, ResourceModel> cache = new Dictionary<string, ResourceModel>();

    /// <summary>A lock for the cache.</summary>
    private readonly object cacheLock = new object();

    /// <summary>
    /// Get a collection of child models that are from a resource.
    /// </summary>
    /// <param name="parentModel">The parent model to search for.</param>
    public IEnumerable<INodeModel> GetChildModelsThatAreFromResource(INodeModel parentModel)
    {
        IEnumerable<INodeModel> childrenFromResource = null;

        if (!string.IsNullOrEmpty(parentModel.ResourceName))
        {
            INodeModel modelFromResource = GetModel(parentModel.ResourceName);
            if (modelFromResource != null)
            {
                childrenFromResource = parentModel.GetChildren().Where(mc =>
                {
                    return modelFromResource.GetChildren().Any(c => c.GetType() == mc.GetType() &&
                                                                    string.Equals(c.Name, mc.Name, StringComparison.InvariantCultureIgnoreCase));
                });
            }
        }

        return childrenFromResource;
    }

    /// <summary>Get a model from resource.</summary>
    /// <param name="resourceName">Name of model.</param>
    /// <returns>The newly created model. Throws if not found.</returns>
    public INodeModel GetModel(string resourceName)
    {
        var resourceModel = GetModelNoClone(resourceName);
        return ReflectionUtilities.Clone(resourceModel?.Root.Model) as INodeModel;
    }

    /// <summary>Get a model resource as a string.</summary>
    /// <param name="resourceName">Name of the resource.</param>
    /// <returns>The model JSON string. Throws if not found.</returns>
    public static string GetString(string resourceName)
    {
        string fullResourceName = $"Models.Resources.{resourceName}.json";
        Assembly modelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                                                         .First(assembly => assembly.GetName().Name == "Models");
        var contents = ReflectionUtilities.GetResourceAsString(modelsAssembly, fullResourceName);
        return contents;
    }


    /// <summary>Singleton instance of Resource</summary>
    internal static Resource Instance
    {
        get
        {
            if (instance == null)
                instance = new Resource();
            return instance;
        }
    }

    /// <summary>Replace a model or all its child models that have ResourceName
    /// with new models loaded from a resource.</summary>
    /// <param name="tree">The model node tree to scan.</param>
    internal IEnumerable<Node> Replace(NodeTree tree)
    {
        List<Node> nodesThatHaveChanged = new();
        foreach (var node in tree.Nodes.Where(node => !string.IsNullOrEmpty(node.Model.ResourceName)).ToArray())  // ToArray() to avoid collection was modified exception.
        {
            var model = node.Model;
            ReplaceModel(node, model.Enabled);
            nodesThatHaveChanged.Add(node);
        }
        return nodesThatHaveChanged;
    }

    /// <summary>Remove all children that are from a resource.</summary>
    /// <param name="model">The model to remove child models from.</param>
    internal IEnumerable<INodeModel> RemoveResourceChildren(INodeModel model)
    {
        if (string.IsNullOrEmpty(model.ResourceName))
            return model.GetChildren();
        else
        {
            var resourceModel = GetModelNoClone(model.ResourceName);
            return model.GetChildren().Where(m => !resourceModel.Root.Children.Any(rc => m.GetType() == rc.Model.GetType() &&
                                                                                         m.Name.Equals(rc.Name, StringComparison.InvariantCultureIgnoreCase)));
        }
    }

    /// <summary>Get a collection of all properties from the specified resource model.</summary>
    /// <param name="resourceName">Name of the resource.</param>
    internal IEnumerable<PropertyInfo> GetPropertiesFromResourceModel(string resourceName)
    {
        return GetModelNoClone(resourceName)?.Properties;
    }


    /// <summary>Default constructor (private)</summary>
    private Resource() { }

    /// <summary>
    /// Replace this model with one loaded from a resource
    /// </summary>
    /// <param name="model">The model to replace</param>
    /// <param name="enabled">Whether the model is enabled</param>
    private void ReplaceModel(Node node, bool enabled)
    {
        INodeModel modelFromResource = GetModel(node.Model.ResourceName);
        if (modelFromResource != null)
        {
            modelFromResource.Enabled = enabled;

            // Get children that need to be added from the resource model
            IEnumerable<INodeModel> childrenToAdd = modelFromResource.GetChildren().Where(mc =>
            {
                return !node.Children.Any(c => c.Model.GetType() == mc.GetType() &&
                                               string.Equals(c.Name, mc.Name, StringComparison.InvariantCultureIgnoreCase));
            });

            // Make all children that are about to be added from resource hidden and readonly.
            bool isHidden = true;
            foreach (var descendant in childrenToAdd)
                descendant.IsHidden = isHidden;

            int index = 0;
            foreach (var child in childrenToAdd)
            {
                node.InsertChild(index, child);
                index++;
            }

            CopyPropertiesFrom(modelFromResource, node.Model);
        }
    }

    /// <summary>Get a model from resource.</summary>
    /// <param name="resourceName">Name of model.</param>
    /// <returns>The newly created model. Throws if not found.</returns>
    private ResourceModel GetModelNoClone(string resourceName)
    {
        if (!cache.TryGetValue(resourceName, out ResourceModel modelFromResource))
        {
            lock (cacheLock)
            {
                if (!cache.TryGetValue(resourceName, out modelFromResource))
                {
                    string contents = GetString(resourceName);
                    if (string.IsNullOrEmpty(contents))
                        return null;

                    modelFromResource = new ResourceModel(contents);
                    cache.Add(resourceName, modelFromResource);
                }
            }
        }
        return modelFromResource;
    }

    /// <summary>Copy all public properties from the one model to another.</summary>
    /// <param name="from">Model to copy from.</param>
    /// <param name="to">Model to copy to.</param>
    private void CopyPropertiesFrom(INodeModel from, INodeModel to)
    {
        foreach (PropertyInfo property in GetPropertiesFromResourceModel(to.ResourceName))
        {
            try
            {
                object fromValue = property.GetValue(from);
                bool doSetPropertyValue;
                if (fromValue is double)
                    doSetPropertyValue = Convert.ToDouble(fromValue, CultureInfo.InvariantCulture) != 0;
                else
                    doSetPropertyValue = fromValue != null;

                if (doSetPropertyValue)
                    property.SetValue(to, fromValue);
            }
            catch (Exception)
            {
                // Couldn't set property - ignore error.
            }
        }
    }

    /// <summary>Encapsulates a model from resources.</summary>
    private class ResourceModel
    {
        private NodeTree tree;

        /// <summary>Constructor.</summary>
        /// <param name="resourceJson">The resource JSON.</param>
        public ResourceModel(string resourceJson)
        {
            tree = NodeTree.CreateFromString<object>(resourceJson, e => throw e, false);
            Properties = GetPropertiesFromResourceModel(resourceJson);
        }

        /// <summary>The root node deserialised from resource.</summary>
        public Node Root => tree.Root.Children.First();

        /// <summary>The properties of the model from resource.</summary>
        public IEnumerable<PropertyInfo> Properties { get; }

        /// <summary>Get a collection of all properties from the specified resource model.</summary>
        /// <param name="model">The model.</param>
        /// <param name="resourceJson">The resource JSON.</param>
        private IEnumerable<PropertyInfo> GetPropertiesFromResourceModel(string resourceJson)
        {
            string[] propertiesNotToCopy = { "$type", "Name", "Parent", "Children", "IncludeInDocumentation", "ResourceName", "Enabled", "ReadOnly" };

            List<PropertyInfo> properties = new List<PropertyInfo>();

            var children = JObject.Parse(resourceJson)["Children"] as JArray;
            if (children == null)
                throw new Exception($"Invalid resource {tree.Root.Name}");

            var resourceToken = children[0] as JObject;
            var propertyTokens = resourceToken.Properties();
            foreach (var propertyName in resourceToken.Properties()
                                                        .Select(pt => pt.Name)
                                                        .Where(name => !propertiesNotToCopy.Contains(name)))
            {
                var propertyInfo = Root.Model.GetType().GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    bool propertyHasDescription = propertyInfo.GetCustomAttributes().Any(a => a.GetType().Name == "DescriptionAttribute");
                    bool propertyHasJsonIgnore = propertyInfo.GetCustomAttributes().Any(a => a.GetType().Name == "JsonIgnoreAttribute");
                    if (propertyInfo != null &&
                        propertyInfo.CanWrite &&
                        propertyInfo.CanRead &&
                        !propertyHasDescription &&
                        !propertyHasJsonIgnore)
                        properties.Add(propertyInfo);
                }
            }

            return properties;
        }
    }
}
