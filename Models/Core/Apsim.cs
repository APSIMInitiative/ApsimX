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
        /// Gets the value of a variable or model.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="namePath">The name of the object to return</param>
        /// <param name="ignoreCase">If true, ignore case when searching for the object or property</param>
        /// <returns>The found object or null if not found</returns>
        public static object Get(IModel model, string namePath, bool ignoreCase = false)
        {
            return Locator(model).Get(namePath, model as Model, ignoreCase);
        }

        /// <summary>
        /// Get the underlying variable object for the given path.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        public static IVariable GetVariableObject(IModel model, string namePath)
        {
            return Locator(model).GetInternal(namePath, model as Model);
        }

        /// <summary>
        /// Sets the value of a variable. Will throw if variable doesn't exist.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public static void Set(IModel model, string namePath, object value)
        {
            Locator(model).Set(namePath, model as Model, value);
        }

        /// <summary>
        /// Returns the full path of the specified model.
        /// </summary>
        /// <param name="model">The model to return the full path for</param>
        /// <returns>The path</returns>
        public static string FullPath(IModel model)
        {
            string fullPath = "." + model.Name;
            IModel parent = model.Parent;
            while (parent != null)
            {
                fullPath = fullPath.Insert(0, "." + parent.Name);
                parent = parent.Parent;
            }

            return fullPath;
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
        /// Returns the closest ancestor to a node of the specified type.
        /// Returns null if not found.
        /// </summary>
        /// <typeparam name="T">Type of model to search for.</typeparam>
        /// <param name="model">The reference model.</param>
        /// <returns></returns>
        public static T Ancestor<T>(IModel model)
        {
            IModel obj = model == null ? null : model.Parent;
            while (obj != null && !(obj is T))
                obj = obj.Parent;
            if (obj == null)
                return default(T);
            return (T)obj;
        }

        /// <summary>
        /// Locates and returns a model with the specified name that is in scope.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="namePath">The name of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public static IModel Find(IModel model, string namePath)
        {
            List<IModel> matches = FindAll(model);
            return matches.Find(match => StringUtilities.StringsAreEqual(match.Name, namePath));
        }

        /// <summary>
        /// Locates and returns a model with the specified type that is in scope.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="type">The type of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public static IModel Find(IModel model, Type type)
        {
            List<IModel> matches = FindAll(model, type);
            if (matches.Count > 0)
                return matches[0];
            else
                return null;
        }

        /// <summary>
        /// Locates and returns all models in scope.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public static List<IModel> FindAll(IModel model)
        {
            var simulation = Apsim.Parent(model, typeof(Simulation)) as Simulation;
            if (simulation == null)
            {
                ScopingRules scope = new ScopingRules();
                List<IModel>result = scope.FindAll(model).ToList();
                scope.Clear();
                return result;
            }
            return simulation.Scope.FindAll(model).ToList();
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
            List<IModel> matches = FindAll(model);
            matches.RemoveAll(match => !typeFilter.IsAssignableFrom(match.GetType()));
            return matches;
        }

        /// <summary>
        /// Perform a deep Copy of the this model.
        /// </summary>
        /// <param name="model">The model to clone</param>
        /// <returns>The clone of the model</returns>
        public static IModel Clone(IModel model)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, model);

                stream.Seek(0, SeekOrigin.Begin);
                IModel newModel =  (IModel)formatter.Deserialize(stream);

                ParentAllChildren(newModel);
                return newModel;
            }
        }

        /// <summary>
        /// Perform a deep serialise of the model.
        /// </summary>
        /// <param name="model">The model to clone</param>
        /// <returns>The model serialised to a stream.</returns>
        public static Stream SerialiseToStream(IModel model)
        {
            // Get rid of our parent temporarily as we don't want to serialise that.
            IModel parent = model.Parent;
            model.Parent = null;
            Stream stream = new MemoryStream();
            try
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, model);
            }
            finally
            {
                model.Parent = parent;
            }
            return stream;
        }

        /// <summary>
        /// Deserialise a model from a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialise from.</param>
        /// <returns>The newly created model</returns>
        public static IModel DeserialiseFromStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            IFormatter formatter = new BinaryFormatter();
            IModel model = (IModel)formatter.Deserialize(stream);
            return model;
        }



        /// <summary>Deletes the specified model.</summary>
        /// <param name="model">The model.</param>
        public static bool Delete(IModel model)
        {
            Locator(model.Parent).Clear();
            Apsim.ClearCaches(model);
            return model.Parent.Children.Remove(model as Model);
        }

        /// <summary>Clears the cache</summary>
        public static void ClearCache(IModel model)
        {
            Locator(model as Model).Clear();
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
            return model.Children.Find(m => typeFilter.IsAssignableFrom(m.GetType()));
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
            return model.Children.Find(m => m.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
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
            return model.Children.FindAll(m => typeFilter.IsAssignableFrom(m.GetType())).ToList<IModel>();
        }
        
        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursively(IModel model)
        {            
            List<IModel> models = new List<IModel>();
            foreach (Model child in model.Children)
            {                
                models.Add(child);                
                models.AddRange(ChildrenRecursively(child));
            }
            return models;
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
            return ChildrenRecursively(model).FindAll(m => typeFilter.IsAssignableFrom(m.GetType()));
        }
        
        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursivelyVisible(IModel model)
        {
            return ChildrenRecursively(model).FindAll(m => !m.IsHidden);
        }



        /// <summary>
        /// Return all siblings of the specified model.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <returns>The found siblings or an empty array if not found.</returns>
        public static List<IModel> Siblings(IModel model)
        {
            if (model != null && model.Parent != null)
            {
                return model.Parent.Children.FindAll(m => m != model).ToList<IModel>();
            }
            else
            {
                return new List<IModel>();
            }
        }

        /// <summary>
        /// Parent all children of 'model'.
        /// </summary>
        /// <param name="model">The model to parent</param>
        public static void ParentAllChildren(IModel model)
        {
            foreach (IModel child in model.Children)
            {
                child.Parent = model;
                ParentAllChildren(child);
            }
        }

        /// <summary>
        /// Parent all children of 'model' and call 'OnCreated' in each child.
        /// </summary>
        /// <param name="model">The model to parent</param>
        public static void InitialiseModel(IModel model)
        {
            ParentAllChildren(model);
            model.OnCreated();
            foreach (var child in Apsim.ChildrenRecursively(model))
                child.OnCreated();
        }

        /// <summary>
        /// Parent all children of 'model'.
        /// </summary>
        /// <param name="model">The model to parent</param>
        public static void UnparentAllChildren(IModel model)
        {
            foreach (IModel child in model.Children)
            {
                child.Parent = null;
                UnparentAllChildren(child);
            }
        }

        /// <summary>
        /// Subscribe to an event. Will throw if namePath doesn't point to a event publisher.
        /// </summary>
        /// <param name="model">The model containing the handler</param>
        /// <param name="eventNameAndPath">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        public static void Subscribe(IModel model, string eventNameAndPath, EventHandler handler)
        {
            // Get the name of the component and event.
            string componentName = StringUtilities.ParentName(eventNameAndPath, '.');
            if (componentName == null)
                throw new Exception("Invalid syntax for event: " + eventNameAndPath);
            string eventName = StringUtilities.ChildName(eventNameAndPath, '.');

            // Get the component.
            object component = Apsim.Get(model, componentName);
            if (component == null)
                throw new Exception(Apsim.FullPath(model) + " can not find the component: " + componentName);

            // Get the EventInfo for the published event.
            EventInfo componentEvent = component.GetType().GetEvent(eventName);
            if (componentEvent == null)
                throw new Exception("Cannot find event: " + eventName + " in model: " + componentName);

            // Subscribe to the event.
            componentEvent.AddEventHandler(component, handler);
        }

        /// <summary>
        /// Unsubscribe an event. Throws if not found.
        /// </summary>
        /// <param name="model">The model containing the handler</param>
        /// <param name="eventNameAndPath">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        public static void Unsubscribe(IModel model, string eventNameAndPath, EventHandler handler)
        {
            // Get the name of the component and event.
            string componentName = StringUtilities.ParentName(eventNameAndPath, '.');
            if (componentName == null)
                throw new Exception("Invalid syntax for event: " + eventNameAndPath);
            string eventName = StringUtilities.ChildName(eventNameAndPath, '.');

            // Get the component.
            object component = Apsim.Get(model, componentName);
            if (component == null)
                throw new Exception(Apsim.FullPath(model) + " can not find the component: " + componentName);

            // Get the EventInfo for the published event.
            EventInfo componentEvent = component.GetType().GetEvent(eventName);
            if (componentEvent == null)
                throw new Exception("Cannot find event: " + eventName + " in model: " + componentName);

            // Unsubscribe to the event.
            componentEvent.RemoveEventHandler(component, handler);
        }

        /// <summary>
        /// Return a list of all parameters (that are not references to child models). Never returns null. Can
        /// return an empty array. A parameter is a class property that is public and read/write
        /// </summary>
        /// <param name="model">The model to search</param>
        /// <param name="flags">The reflection tags to use in the search</param>
        /// <returns>The array of variables.</returns>
        public static IVariable[] FieldsAndProperties(object model, BindingFlags flags)
        {
            List<IVariable> allProperties = new List<IVariable>();
            foreach (PropertyInfo property in model.GetType().UnderlyingSystemType.GetProperties(flags))
            {
                if (property.CanRead)
                    allProperties.Add(new VariableProperty(model, property));
            }
            foreach (FieldInfo field in model.GetType().UnderlyingSystemType.GetFields(flags))
                allProperties.Add(new VariableField(model, field));
            return allProperties.ToArray();
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

        /// <summary>Get a list of allowable child functions for the specified parent.</summary>
        /// <param name="parent">The parent model.</param>
        /// <returns>A list of allowable child functions.</returns>
        public static List<Type> GetAllowableChildFunctions(object parent)
        {
            // For now, we allow all functions to be added anywhere
            List<Type> allowableFunctions = new List<Type>();
            foreach (Type t in ReflectionUtilities.GetTypesThatHaveInterface(typeof(IFunction)))
            {
                allowableFunctions.Add(t);
            }

            allowableFunctions.Sort(new ReflectionUtilities.TypeComparer());
            return allowableFunctions;
        }

        /// <summary>
        /// Gets the locater model for the specified model.
        /// </summary>
        /// <param name="model">The model to find the locator for</param>
        /// <returns>The an instance of a locater class for the specified model. Never returns null.</returns>
        private static Locater Locator(IModel model)
        {
            var simulation = Apsim.Parent(model, typeof(Simulation)) as Simulation;
            if (simulation == null)
            {
                // Simulation can be null if this model is not under a simulation e.g. DataStore.
                return new Locater();
            }
            else
            {
                return simulation.Locater;
            }
        }

        /// <summary>Encapsulates a model that can be added to another model.</summary>
        public class ModelDescription : IComparable<ModelDescription>
        {
            /// <summary>Name of resource.</summary>
            public string ResourceString {get; private set; }

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
