namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using Models.Core.Interfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    /// <summary>This class loads a model from a resource</summary>
    [Serializable]
    public class ModelCollectionFromResource : Model, IOptionallySerialiseChildren
    {
        /// <summary>Gets or sets the name of the resource.</summary>
        public string ResourceName { get; set; }

        /// <summary>Allow children to be serialised?</summary>
        [JsonIgnore]
        public bool DoSerialiseChildren
        {
            get
            {
                if (string.IsNullOrEmpty(ResourceName))
                    return true;

                if (ChildrenToSerialize != null && ChildrenToSerialize.Count > 0)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Gets all child models which are not part of the 'official' model resource.
        /// Generally speaking, this is all models which have been added by the user
        /// (e.g. cultivars).
        /// </summary>
        /// <remarks>
        /// This returns all child models which do not have a matching model in the
        /// resource model's children. A match is defined as having the same name and
        /// type.
        /// </remarks>
        public List<Model> ChildrenToSerialize
        {
            get
            {
                if (string.IsNullOrEmpty(ResourceName))
                    return Children;

                List<Model> officialChildren = GetResourceModel()?.Children;
                if (officialChildren == null)
                    return Children;

                List<Model> toReturn = new List<Model>();
                foreach (Model child in Children)
                    if (!officialChildren.Any(m => m.GetType() == child.GetType() && string.Equals(m.Name, child.Name, StringComparison.InvariantCultureIgnoreCase)))
                        toReturn.Add(child);
                return toReturn;
            }
        }

        /// <summary>
        /// We have just been deserialised. If from XML then load our model
        /// from resource.
        /// </summary>
        public override void OnCreated()
        {
            // lookup the resource get the xml and then deserialise to a model.
            if (!string.IsNullOrEmpty(ResourceName))
            {
                string contents = ReflectionUtilities.GetResourceAsString("Models.Resources." + ResourceName + ".json");
                if (contents != null)
                {
                    Model modelFromResource = GetResourceModel();
                    modelFromResource.Enabled = Enabled;
                    
                    Children.RemoveAll(c => modelFromResource.Children.Contains(c, new ModelComparer()));
                    Children.InsertRange(0, modelFromResource.Children);

                    CopyPropertiesFrom(modelFromResource);
                    SetNotVisible(modelFromResource);
                    Apsim.ParentAllChildren(this);
                }
            }
        }

        /// <summary>
        /// Get a list of parameter names for this model.
        /// </summary>
        /// <returns></returns>
        public List<string> GetModelParameterNames()
        {
            if (ResourceName != null && ResourceName != "")
            {
                string contents = ReflectionUtilities.GetResourceAsString("Models.Resources." + ResourceName + ".json");
                if (contents != null)
                {
                    var parameterNames = new List<string>();

                    var json = JObject.Parse(contents);
                    var children = json["Children"] as JArray;
                    var simulations = children[0];
                    foreach (var parameter in simulations.Children())
                    {
                        if (parameter is JProperty)
                        {
                            var property = parameter as JProperty;
                            if (property.Name != "$type")
                            {
                                parameterNames.Add(property.Name);
                            }
                        }
                    }
                    return parameterNames;
                }
            }

            return null;
        }

        private Model GetResourceModel()
        {
            if (string.IsNullOrEmpty(ResourceName))
                return null;

            string contents = ReflectionUtilities.GetResourceAsString($"Models.Resources.{ResourceName}.json");
            if (string.IsNullOrEmpty(contents))
                return null;

            Model modelFromResource = ApsimFile.FileFormat.ReadFromString<Model>(contents, out List<Exception> errors);
            if (errors != null && errors.Count > 0)
                throw errors[0];

            if (this.GetType() != modelFromResource.GetType())
            {
                // Top-level model may be a simulations node. Search for a child of the correct type.
                Model child = Apsim.Child(modelFromResource, this.GetType()) as Model;
                if (child != null)
                    modelFromResource = child;
            }

            return modelFromResource;
        }

        /// <summary>
        /// Copy all properties from the specified resource.
        /// </summary>
        /// <param name="from">Model to copy from</param>
        private void CopyPropertiesFrom(Model from)
        {
            foreach (PropertyInfo property in from.GetType().GetProperties())
            {
                if (property.CanWrite &&
                    property.Name != "Name" &&
                    property.Name != "Parent" &&
                    property.Name != "Children" &&
                    property.Name != "IncludeInDocumentation" &&
                    property.Name != "ResourceName")
                {
                    var description = property.GetCustomAttribute(typeof(DescriptionAttribute));
                    if (description == null)
                    {
                        object fromValue = property.GetValue(from);
                        bool doSetPropertyValue;
                        if (fromValue is double)
                            doSetPropertyValue = Convert.ToDouble(fromValue, CultureInfo.InvariantCulture) != 0;
                        else
                            doSetPropertyValue = fromValue != null;

                        if (doSetPropertyValue)
                            property.SetValue(this, fromValue);
                    }
                }
            }
        }

        /// <summary>Sets the not visible.</summary>
        /// <param name="ModelFromResource">The model from resource.</param>
        private static void SetNotVisible(Model ModelFromResource)
        {
            foreach (Model child in ModelFromResource.Children)
            {
                child.IsHidden = true;
                child.ReadOnly = true;
                SetNotVisible(child);
            }
        }

        /// <summary>
        /// Class used to compare models. The models are considered equal iff they have
        /// the same name and type.
        /// </summary>
        private class ModelComparer : IEqualityComparer<Model>
        {
            public bool Equals(Model x, Model y)
            {
                return x.GetType() == y.GetType() && string.Equals(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(Model obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
