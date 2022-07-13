namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using Models.Core.ApsimFile;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// This class encapsulates an instruction to replace a model.
    /// </summary>
    public class Resource
    {
        /// <summary>The instance of resource.</summary>
        private static Resource instance = null;

        private readonly Dictionary<string, IModel> cache = new Dictionary<string, IModel>();

        /// <summary>Singleton instance of Resource</summary>
        public static Resource Instance
        {
            get
            {
                if (instance == null)
                    instance = new Resource();
                return instance;
            }
        }

        /// <summary>Replace all child models (of a parent) that have ResourceName with a new model loaded from a resource.</summary>
        /// <param name="parent">The parent model to search for children.</param>
        public void Replace(IModel parent)
        {
            foreach (var child in parent.FindAllDescendants()
                                        .Where(m => !string.IsNullOrEmpty(m.ResourceName))
                                        .ToArray()) // ToArray is necessary to stop 'Collection was modified' exception
            {
                IModel modelFromResource = GetModel(child.ResourceName);
                if (modelFromResource != null)
                {
                    modelFromResource.Enabled = parent.Enabled;

                    // Replace existing children that match (name and type) the children of modelFromResource.
                    child.Children.RemoveAll(mr =>
                    {
                        return mr.GetType() == child.GetType() && string.Equals(mr.Name, child.Name, StringComparison.InvariantCultureIgnoreCase);
                    });
                    child.Children.InsertRange(0, modelFromResource.Children);

                    CopyPropertiesFrom(modelFromResource, child);

                    // Make all descendents of 'child' hidden and readonly.
                    bool isHidden = parent.FindAncestor<Replacements>() == null;
                    foreach (Model descendant in child.FindAllDescendants())
                    {
                        descendant.IsHidden = isHidden;
                        descendant.ReadOnly = isHidden;
                    }

                    child.ParentAllDescendants();
                }
            }
        }

        /// <summary>Get a model from resource.</summary>
        /// <param name="resourceName">Name of model.</param>
        /// <returns>The newly created model. Throws if not found.</returns>
        public IModel GetModel(string resourceName)
        {
            if (!cache.TryGetValue(resourceName, out IModel modelFromResource))
            {
                string contents = GetString(resourceName);
                if (string.IsNullOrEmpty(contents))
                    return null;

                modelFromResource = FileFormat.ReadFromString<IModel>(contents, e => throw e, false);
                modelFromResource = modelFromResource.Children.First();
                cache.Add(resourceName, modelFromResource);
            }

            return modelFromResource.Clone();
        }

        /// <summary>Get a model resource as a string.</summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns>The model JSON string. Throws if not found.</returns>
        public static string GetString(string resourceName)
        {
            string fullResourceName = $"Models.Resources.{resourceName}.json";
            var contents = ReflectionUtilities.GetResourceAsString(fullResourceName);
            return contents;
        }

        /// <summary>Get a collection of all properties from the specified resource model.</summary>
        /// <param name="resourceName">Name of the resource.</param>
        public IEnumerable<PropertyInfo> GetPropertiesFromResourceModel(string resourceName)
        {
            string[] propertiesNotToCopy = { "$type", "Name", "Parent", "Children", "IncludeInDocumentation", "ResourceName", "Enabled", "ReadOnly" };

            var model = GetModel(resourceName);
            if (model != null)
            {
                var children = JObject.Parse(GetString(resourceName))["Children"] as JArray;
                if (children == null)
                    throw new Exception($"Invalid resource {resourceName}");

                var resourceToken = children[0] as JObject;
                var propertyTokens = resourceToken.Properties();
                foreach (var propertyName in resourceToken.Properties()
                                                          .Select(pt => pt.Name)
                                                          .Where(name => !propertiesNotToCopy.Contains(name)))
                {
                    var propertyInfo = model.GetType().GetProperty(propertyName);
                    if (propertyInfo != null &&
                        propertyInfo.CanWrite &&
                        propertyInfo.CanRead &&
                        propertyInfo.GetCustomAttribute(typeof(DescriptionAttribute)) == null &&
                        propertyInfo.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null)
                        yield return propertyInfo;
                }
            }
        }

        /// <summary>Default constructor (private)</summary>
        private Resource() { }

        /// <summary>Copy all public properties from the one model to another.</summary>
        /// <param name="from">Model to copy from.</param>
        /// <param name="to">Model to copy to.</param>
        private void CopyPropertiesFrom(IModel from, IModel to)
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
    }
}
