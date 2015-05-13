// -----------------------------------------------------------------------
// <copyright file="Locater.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// This class is responsible for the location and retrieval of variables or models 
    /// given a path.
    /// Path example syntax:
    ///    .Clock.Start                         ABSOLUTE PATH
    ///    [PotatoSowingRule].Script.SowDate    RELATIVE TO A MODEL IN SCOPE
    ///    Leaf.LAI                             RELATIVE CHILD MODEL.
    /// </summary>
    public class Locater
    {
        private class CacheForModel
        {
            /// <summary>
            /// A cache for speeding up look ups. The object can be either 
            /// Model[] or an IVariable.
            /// </summary>
            public Dictionary<string, object> cache = new Dictionary<string, object>();

            /// <summary>
            /// Get a value for the specified key or null if not in cache.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public object GetValueForKey(string key)
            {
                object value;
                if (this.cache.TryGetValue(key, out value))
                    return value;
                else
                    return null;
            }
        }

        /// <summary>
        /// A cache for speeding up look ups. The object can be either 
        /// Model[] or an IVariable.
        /// </summary>
        private Dictionary<IModel, CacheForModel> cache = new Dictionary<IModel, CacheForModel>();

        /// <summary>
        /// Clear the cache
        /// </summary>
        public void Clear()
        {
            this.cache.Clear();
        }

        /// <summary>
        /// Get the value of a variable or model.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <param name="ignoreCase">If true, ignore case when searching for the object or property</param>
        /// <returns>The found object or null if not found</returns>
        public object Get(string namePath, Model relativeTo, bool ignoreCase = false)
        {
            IVariable variable = this.GetInternal(namePath, relativeTo, ignoreCase);
            if (variable == null)
            {
                return variable;
            }
            else
            {
                return variable.Value;
            }
        }

        /// <summary>
        /// Set the value of a variable. Will throw if variable doesn't exist.
        /// </summary>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <param name="value">The value to set the property to</param>
        public void Set(string namePath, Model relativeTo, object value)
        {
            IVariable variable = this.GetInternal(namePath, relativeTo);
            if (variable == null)
            {
                throw new ApsimXException(relativeTo, "Cannot set the value of variable '" + namePath + "'. Variable doesn't exist");
            }
            else
            {
                variable.Value = value;
            }
        }

        /// <summary>
        /// Get the value of a variable or model.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <param name="ignoreCase">If true, ignore case when searching for the object or property</param>
        /// <returns>The found object or null if not found</returns>
        public IVariable GetInternal(string namePath, Model relativeTo, bool ignoreCase = false)
        {
            Model relativeToModel = relativeTo;
            string cacheKey = namePath;
            StringComparison compareType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            // Look in cache first.
            object value = GetFromCache(cacheKey, relativeToModel);
            if (value != null)
            {
                return value as IVariable;
            }

            IVariable returnVariable = null;
            if (namePath == null || namePath.Length == 0)
            {
                return null;
            }
            else if (namePath[0] != '.' && namePath[0] != '.' &&
                     namePath.IndexOfAny("(+*/".ToCharArray()) != -1)
            {
                // expression - need a better way of detecting an expression
                returnVariable = new VariableExpression(namePath, relativeToModel);
            }
            else
            {
                // Remove a square bracketed model name and change our relativeTo model to 
                // the referenced model.
                if (namePath.StartsWith("["))
                {
                    int posCloseBracket = namePath.IndexOf(']');
                    if (posCloseBracket == -1)
                    {
                        return null;
                    }
                    string modelName = namePath.Substring(1, posCloseBracket - 1);
                    namePath = namePath.Remove(0, posCloseBracket + 1);
                    Model foundModel = this.Find(modelName, relativeToModel);
                    if (foundModel == null)
                    {
                        // Didn't find a model with a name matching the square bracketed string so
                        // now try and look for a model with a type matching the square bracketed string.
                        Type[] modelTypes = GetTypeWithoutNameSpace(modelName);
                        if (modelTypes.Length == 1)
                            foundModel = this.Find(modelTypes[0], relativeToModel);
                    }
                    if (foundModel == null)
                        return null;
                    else
                        relativeToModel = foundModel;
                }
                else if (namePath.StartsWith("."))
                {
                    // Absolute path
                    Model root = relativeToModel;
                    while (root.Parent != null)
                    {
                        root = root.Parent as Model;
                    }
                    relativeToModel = root;

                    int posPeriod = namePath.IndexOf('.', 1);
                    if (posPeriod == -1)
                    {
                        posPeriod = namePath.Length;
                    }

                    namePath = namePath.Remove(0, posPeriod);
                    if (namePath.StartsWith("."))
                    {
                        namePath.Remove(1);
                    }
                }

                // Now walk the series of '.' separated path bits, assuming the path bits
                // are child models. Stop when we can't find the child model.
                string[] namePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                int i;
                for (i = 0; i < namePathBits.Length; i++)
                {
                    IModel localModel = relativeToModel.Children.FirstOrDefault(m => m.Name.Equals(namePathBits[i], compareType));
                    if (localModel == null)
                    {
                        break;
                    }
                    else
                    {
                        relativeToModel = localModel as Model;
                    }
                }

                // At this point there are only 2 possibilities. We have encountered a 
                // PropertyInfo or the path is invalid.
                // We now need to loop through the remaining path bits and keep track of each
                // section of the path as each section will have to be evaulated everytime a
                // a get is done for this path. 
                // The variable 'i' will point to the name path that cannot be found as a model.
                object relativeToObject = relativeToModel;
                List<IVariable> properties = new List<IVariable>();
                properties.Add(new VariableObject(relativeToModel));
                for (int j = i; j < namePathBits.Length; j++)
                {
                    // look for an array specifier e.g. sw[2]
                    string arraySpecifier = null;
                    if (namePathBits[j].Contains("["))
                    {
                        arraySpecifier = StringUtilities.SplitOffBracketedValue(ref namePathBits[j], '[', ']');
                    }

                    // Look for either a property or a child model.
                    IModel localModel = null;
                    PropertyInfo propertyInfo = relativeToObject.GetType().GetProperty(namePathBits[j]);
                    if (propertyInfo == null && ignoreCase) // If not found, try using a case-insensitive search
                        propertyInfo = relativeToObject.GetType().GetProperty(namePathBits[j], BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (propertyInfo == null && relativeToObject is Model)
                    {
                        // Not a property, may be a child model.
                        localModel = (relativeToObject as IModel).Children.FirstOrDefault(m => m.Name.Equals(namePathBits[i], compareType));
                        if (localModel == null)
                        {
                            return null;
                        }

                        properties.Add(new VariableObject(localModel));
                        relativeToObject = localModel;
                    }
                    else if (propertyInfo != null)
                    {
                        VariableProperty property = new VariableProperty(relativeToObject, propertyInfo, arraySpecifier);
                        properties.Add(property);
                        relativeToObject = property.Value;
                    }
                    else
                    {
                        return null;
                    }
                }

                // We now have a list of IVariable instances that can be evaluated to 
                // produce a return value for the given path. Wrap the list into an IVariable.
                returnVariable = new VariableComposite(namePath, properties);
            }

            // Add variable to cache.
            AddToCache(cacheKey, relativeTo, returnVariable);

            return returnVariable;
        }

        /// <summary>
        /// Gets all Type instances matching the specified class name with no namespace qualified class name.
        /// Will not throw. May return empty array.
        /// </summary>
        private static Type[] GetTypeWithoutNameSpace(string className)
        {
            List<Type> returnVal = new List<Type>();

            Type[] assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
            for (int j = 0; j < assemblyTypes.Length; j++)
            {
                if (assemblyTypes[j].Name == className)
                {
                    returnVal.Add(assemblyTypes[j]);
                }
            }

            return returnVal.ToArray();
        }

        /// <summary>
        /// Add the specified object to the cache.
        /// </summary>
        /// <param name="key">cache key</param>
        /// <param name="relativeTo">Model for which the object is relative to</param>
        /// <param name="obj">The object to store.</param>
        private void AddToCache(string key, Model relativeTo, object obj)
        {
            if (obj != null)
            {
                CacheForModel cacheForModel = null;
                if (this.cache.ContainsKey(relativeTo))
                    cacheForModel = this.cache[relativeTo];
                else
                {
                    cacheForModel = new CacheForModel();
                    this.cache.Add(relativeTo, cacheForModel);
                }
                cacheForModel.cache.Add(key, obj);
            }
        }

        /// <summary>
        /// Get an object from the cache.
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="relativeTo">The model the object is relative to</param>
        /// <returns>The object or null if not found.</returns>
        private object GetFromCache(string key, Model relativeTo)
        {
            if (cache.ContainsKey(relativeTo))
            {
                object value = this.cache[relativeTo].GetValueForKey(key);
                if (value != null)
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>
        /// Return a model with the specified name is in scope. Returns null if none found.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <returns>The found model or null if not found</returns>
        public Model Find(string namePath, Model relativeTo)
        {
            // Look in cache first.
            string cacheKey = "INSCOPENAME|" + namePath;
            object value = GetFromCache(cacheKey, relativeTo);
            if (value != null)
            {
                return (value as IVariable).Value as Model;
            }

            // Not in cache - get all in scope and return the one matching namePath.
            foreach (Model model in this.FindAll(relativeTo))
            {
                if (model.Name.Equals(namePath, StringComparison.CurrentCultureIgnoreCase))
                {
                    AddToCache(cacheKey, relativeTo, new VariableObject(model));
                    return model;
                }
            }

            return null;
        }

        /// <summary>
        /// Return a model with the specified type is in scope. Returns null if none found.
        /// </summary>
        /// <param name="type">The type of the object to return</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <returns>The found model or null if not found</returns>
        public Model Find(Type type, Model relativeTo)
        {
            // Look in cache first.
            string cacheKey = "INSCOPETYPE|" + type.Name;
            object value = GetFromCache(cacheKey, relativeTo);
            if (value != null)
            {
                return (value as IVariable).Value as Model;
            }

            // Not in cache - get all in scope and return the one matching namePath.
            foreach (Model model in this.FindAll(relativeTo))
            {
                if (type.IsAssignableFrom(model.GetType()))
                {
                    AddToCache(cacheKey, relativeTo, new VariableObject(model));
                    return model;
                }
            }

            return null;
        }

        /// <summary>
        /// Return all models within scope of the specified relative model.
        /// </summary>
        /// <param name="relativeTo">The model calling this method</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public Model[] FindAll(Model relativeTo)
        {
            // Look in cache first.
            string cacheKey = "ALLINSCOPE";
            object value = GetFromCache(cacheKey, relativeTo);
            if (value != null)
            {
                return value as Model[];
            }

            // Get all children first.
            List<Model> modelsInScope = new List<Model>();
            foreach (Model child in Apsim.ChildrenRecursively(relativeTo))
                modelsInScope.Add(child);

            // Add relativeTo.
            //modelsInScope.Add(relativeTo);

            // Get siblings and parents siblings and parents, parents siblings etc
            // until we reach a Simulations or Simulation model.
            if (!(relativeTo is Simulations))
            {
                Model relativeToParent = relativeTo;
                do
                {
                    foreach (IModel model in Apsim.Siblings(relativeToParent))
                        modelsInScope.Add(model as Model);
                    relativeToParent = relativeToParent.Parent as Model;

                    // Add in the top level model that we stopped on.
                    if (relativeToParent != null)
                    {
                        modelsInScope.Add(relativeToParent);
                    }
                }
                while (relativeToParent != null && !(relativeToParent is Simulation) && !(relativeToParent is Simulations));
            }

            // Add the in scope models to the cache and return them
            AddToCache(cacheKey, relativeTo, modelsInScope.ToArray());
            return modelsInScope.ToArray();
        }

        /// <summary>
        /// Return all models of the specified type within scope of the specified relative model.
        /// </summary>
        /// <param name="type">The type of the models to return</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public Model[] FindAll(Type type, Model relativeTo)
        {
            // Look in cache first.
            string cacheKey = "ALLINSCOPEOFTYPE|" + type.Name;
            object value = GetFromCache(cacheKey, relativeTo);
            if (value != null)
            {
                return value as Model[];
            }

            List<Model> modelsToReturn = new List<Model>();
            foreach (Model model in this.FindAll(relativeTo))
            {
                if (type.IsAssignableFrom(model.GetType()))
                {
                    modelsToReturn.Add(model);
                }
                else
                {
                    if (model is Manager)
                    {
                        Manager manager = model as Manager;
                        if (type.IsAssignableFrom(manager.Script.GetType()))
                        {
                            if (manager.Script != relativeTo)
                                modelsToReturn.Add(manager.Script);
                        }
                    }
                }
            }

            AddToCache(cacheKey, relativeTo, modelsToReturn.ToArray());
            return modelsToReturn.ToArray();
        }
    }
}
