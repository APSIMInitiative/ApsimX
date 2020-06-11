namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using APSIM.Shared.Utilities;
    using Functions;
    using Factorial;

    /// <summary>
    /// The API for models to discover other models, get and set variables in
    /// other models and send events and subscribe to events in other models.
    /// </summary>
    public static class Apsim
    {
        /// <summary>
        /// Sets the value of a variable. Will throw if variable doesn't exist.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public static void Set(IModel model, string namePath, object value)
        {
            model.FindByPath(namePath).Value = value;
        }

        /// <summary>
        /// Return a parent node of the specified type 'typeFilter'. Will throw if not found.
        /// </summary>
        /// <param name="model">The model to get the parent for</param>
        /// <param name="typeFilter">The name of the parent model to return</param>
        /// <returns>The parent of the specified type.</returns>
        public static IModel Parent(IModel model, Type typeFilter)
        {
            IModel obj = model;
            while (obj.Parent != null && !typeFilter.IsAssignableFrom(obj.GetType()))
            {
                obj = obj.Parent as IModel;
            }

            if (obj == null)
            {
                throw new ApsimXException(model, "Cannot find a parent of type: " + typeFilter.Name);
            }

            return obj;
        }

        /// <summary>
        /// Locates and returns a model with the specified name that is in scope.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="namePath">The name of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public static IModel Find(IModel model, string namePath)
        {
            return model?.FindInScope(namePath);
        }

        /// <summary>
        /// Locates and returns a model with the specified type that is in scope.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="type">The type of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public static IModel Find(IModel model, Type type)
        {
            if (model == null)
                return null;

            MethodInfo[] methods = model.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            MethodInfo find = methods.FirstOrDefault(m => m.Name == "FindInScope" && m.IsGenericMethod);
            if (find == null)
                throw new Exception($"Unable to find find method");
            return find.MakeGenericMethod(type).Invoke(model, null) as IModel;
        }

        /// <summary>
        /// Locates and returns all models in scope.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public static List<IModel> FindAll(IModel model)
        {
            if (model == null)
                return new List<IModel>();

            MethodInfo[] methods = model.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            MethodInfo find = methods.FirstOrDefault(m => m.Name == "FindAllInScope" && !m.IsGenericMethod && m.GetParameters().Length == 0);
            if (find == null)
                throw new Exception($"Unable to find find method");

            return (find.Invoke(model, null) as IEnumerable<IModel>).ToList();
        }

        /// <summary>
        /// Clears the cached scoping values for the simulation 
        /// We need to do this when models have been added or deleted,
        /// as the cache will then be incorrect
        /// </summary>
        /// <param name="model"></param>
        public static void ClearCaches(IModel model)
        {
            var simulation = Apsim.Parent(model, typeof(Simulation)) as Simulation;
            if (simulation != null && simulation.Scope != null)
            {
                simulation.ClearCaches();
            }
            else
            {
                // If the model didn't have a Simulation object as an ancestor, then it's likely to 
                // have a Simulations object as one. If so, the Simulations links may need to be updated.
                var simulations = Apsim.Parent(model, typeof(Simulations)) as Simulations;
                if (simulations != null)
                {
                    simulations.ClearLinks();
                }
            }
        }

        /// <summary>
        /// Locates and returns all models in scope of the specified type.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="typeFilter">The type of the models to return</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public static List<IModel> FindAll(IModel model, Type typeFilter)
        {
            if (model == null)
                return null;

            MethodInfo[] methods = model.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            MethodInfo find = methods.FirstOrDefault(m => m.Name == "FindAllInScope" && m.IsGenericMethod);
            if (find == null)
                throw new Exception($"Unable to find find method");
            return (find.MakeGenericMethod(typeFilter).Invoke(model, null) as IEnumerable<object>).OfType<IModel>().ToList();
        }

        /// <summary>
        /// Perform a deep Copy of the this model.
        /// </summary>
        /// <param name="model">The model to clone</param>
        /// <returns>The clone of the model</returns>
        public static T Clone<T>(this T model) where T : IModel
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, model);

                stream.Seek(0, SeekOrigin.Begin);
                T newModel =  (T)formatter.Deserialize(stream);

                newModel.ParentAllDescendants();

                return newModel;
            }
        }

        /// <summary>
        /// Return a child model that matches the specified 'modelType'. Returns 
        /// an empty list if not found.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <param name="typeFilter">The type of children to return</param>
        /// <returns>A list of all children</returns>
        public static IModel Child(IModel model, Type typeFilter)
        {
            if (model == null)
                return null;

            MethodInfo[] methods = model.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            MethodInfo find = methods.FirstOrDefault(m => m.Name == "FindChild" && m.IsGenericMethod && m.GetParameters().Count() == 0);
            if (find == null)
                throw new Exception($"Unable to find find method");
            return find.MakeGenericMethod(typeFilter).Invoke(model, null) as IModel;
        }

        /// <summary>
        /// Return a child model that matches the specified 'name'. Returns 
        /// null if not found.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <param name="name">The name of the child to return</param>
        /// <returns>A list of all children</returns>
        public static IModel Child(IModel model, string name)
        {
            return model.FindChild(name);
        }
        
        /// <summary>
        /// Return children that match the specified 'typeFilter'. Never returns 
        /// null. Can return empty List.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <param name="typeFilter">The type of children to return</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> Children(IModel model, Type typeFilter)
        {
            if (model == null)
                return null;

            MethodInfo[] methods = model.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            MethodInfo find = methods.FirstOrDefault(m => m.Name == "FindAllChildren" && m.IsGenericMethod && m.GetParameters().Count() == 0);
            if (find == null)
                throw new Exception($"Unable to find find method");
            return (find.MakeGenericMethod(typeFilter).Invoke(model, null) as IEnumerable<object>).OfType<IModel>().ToList();
        }
        
        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursively(IModel model)
        {
            return model.FindAllDescendants().ToList();
        }

        /// <summary>
        /// Return a list of all child models recursively. Only models of 
        /// the specified 'typeFilter' will be returned. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <param name="typeFilter">The type of children to return</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursively(IModel model, Type typeFilter)
        {
            if (model == null)
                return new List<IModel>();

            MethodInfo[] methods = model.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            MethodInfo find = methods.FirstOrDefault(m => m.Name == "FindAllDescendants" && m.IsGenericMethod && m.GetParameters().Length == 0);
            if (find == null)
                throw new Exception($"Unable to find find method");

            return (find.MakeGenericMethod(typeFilter).Invoke(model, null) as IEnumerable<object>).OfType<IModel>().ToList();
        }
        
        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursivelyVisible(IModel model)
        {
            return model.FindAllDescendants().Where(m => !m.IsHidden).ToList();
        }

        /// <summary>
        /// Return all siblings of the specified model.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <returns>The found siblings or an empty array if not found.</returns>
        public static List<IModel> Siblings(IModel model)
        {
            return model.FindAllSiblings().ToList();
        }

        /// <summary>
        /// Parent all children of 'model'.
        /// </summary>
        /// <param name="model">The model to parent</param>
        public static void ParentAllChildren(IModel model)
        {
            model.ParentAllDescendants();
        }

        /// <summary>Return true if the child can be added to the parent.</summary>
        /// <param name="parent">The parent model.</param>
        /// <param name="childType">The child type.</param>
        /// <returns>True if child can be added.</returns>
        public static bool IsChildAllowable(object parent, Type childType)
        {
            if (childType == typeof(Simulations))
                return false;

            if (parent.GetType() == typeof(Folder) ||
                parent.GetType() == typeof(Factor) ||
                parent.GetType() == typeof(CompositeFactor) ||
                parent.GetType() == typeof(Replacements))
                return true;

            // Functions are currently allowable anywhere
            if (childType.GetInterface("IFunction") != null)
                return true;

            // Is allowable if one of the valid parents of this type (t) matches the parent type.
            foreach (ValidParentAttribute validParent in ReflectionUtilities.GetAttributes(childType, typeof(ValidParentAttribute), true))
            {
                if (validParent != null)
                {
                    if (validParent.DropAnywhere)
                        return true;

                    if (validParent.ParentType.IsAssignableFrom(parent.GetType()))
                        return true;
                }
            }
            return false;
        }

        /// <summary>Get a list of allowable child models for the specified parent.</summary>
        /// <param name="parent">The parent model.</param>
        /// <returns>A list of allowable child models.</returns>
        public static IEnumerable<ModelDescription> GetAllowableChildModels(object parent)
        {
            var allowableModels = new SortedSet<ModelDescription>();

            // Add in all types that implement the IModel interface.
            foreach (Type t in ReflectionUtilities.GetTypesThatHaveInterface(typeof(IModel)))
                allowableModels.Add(new ModelDescription(t));

            // Add in resources.
            var thisAssembly = Assembly.GetExecutingAssembly();
            foreach (var resourceName in thisAssembly.GetManifestResourceNames())
            {
                if (resourceName.Contains(".json"))
                {
                    // Get the full model type name from the resource.
                    string modelTypeFullName = null;
                    var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                    using (StreamReader reader = new StreamReader(resStream))
                    {
                        // Need to get the second '$type' line from the resource. The 
                        // first is assumed to be 
                        //    "$type": "Models.Core.Simulations, Models"
                        // The second is assumed to be the model we're looking for.
                        int count = 0;
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains("\"$type\""))
                            {
                                count++;
                                if (count == 2)
                                {
                                    modelTypeFullName = StringUtilities.SplitOffAfterDelimiter(ref line, ":");
                                    modelTypeFullName = modelTypeFullName.Replace("\"", "");
                                    modelTypeFullName = modelTypeFullName.Replace(", Models,", "");
                                    break;
                                }
                            }
                        }
                    }

                    if (modelTypeFullName != null)
                    {
                        // Assume the resource name is the model name.
                        var resourceNameWithoutExtension = resourceName.Replace(".json", "");
                        var resourceWords = resourceNameWithoutExtension.Split(".".ToCharArray()).ToList();
                        var modelName = resourceWords.Last();

                        var modelType = thisAssembly.ExportedTypes.FirstOrDefault(t => t.FullName == modelTypeFullName);
                        if (modelType != null)
                            allowableModels.Add(new ModelDescription(modelType, modelName, resourceName));
                    }
                }
            }

            // Remove models that cannot be added to parent.
            allowableModels.RemoveWhere(t => !IsChildAllowable(parent, t.ModelType));
            
            //allowableModels.Sort(new ReflectionUtilities.TypeComparer());
            return allowableModels;
        }

        /// <summary>Encapsulates a model that can be added to another model.</summary>
        public class ModelDescription : IComparable<ModelDescription>
        {
            /// <summary>Name of resource.</summary>
            public string ResourceString {get; set; }

            /// <summary>Constructor.</summary>
            public ModelDescription(Type t) 
            { 
                ModelType = t;
                ModelName = ModelType.Name;
            }

            /// <summary>Constructor.</summary>
            public ModelDescription(Type t, string name, string resourceName) 
            { 
                ModelType = t;
                ModelName = name;
                ResourceString = resourceName; 
            }

            /// <summary>Type of model.</summary>
            public Type ModelType { get; }

            /// <summary>Name of model.</summary>
            public string ModelName { get; }

            /// <summary>Comparison method.</summary>
            /// <param name="other">The other instance to compare this one to.</param>
            public int CompareTo(ModelDescription other)
            {
                int comparison = ModelType.FullName.CompareTo(other.ModelType.FullName);
                if (comparison == 0)
                    comparison = ModelName.CompareTo(other.ModelName);
                return comparison;
            }
        }
    }
}
