namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using Functions;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

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
            else if (namePath[0] != '.' &&
                     namePath.Replace("()", "").IndexOfAny("(+*/".ToCharArray()) != -1)
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
                    Model foundModel = relativeToModel.FindInScope(modelName) as Model;
                    if (foundModel == null)
                    {
                        // Didn't find a model with a name matching the square bracketed string so
                        // now try and look for a model with a type matching the square bracketed string.
                        Type[] modelTypes = GetTypeWithoutNameSpace(modelName);
                        if (modelTypes.Length == 1)
                            foundModel = relativeToModel.FindAllInScope().FirstOrDefault(m => modelTypes[0].IsAssignableFrom(m.GetType())) as Model;
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
                        // Path starts with a . and only contains a single period.
                        // If name matches then no problem. Otherwise we need to return null.
                        posPeriod = namePath.IndexOf('.');
                        if (namePath.Remove(0, posPeriod) == relativeToModel.Name)
                            posPeriod = namePath.Length;
                    }

                    namePath = namePath.Remove(0, posPeriod);
                    if (namePath.StartsWith("."))
                    {
                        namePath = namePath.Remove(0, 1);
                    }
                }
                else
                {
                    // Try a report column.
                    foreach (Report report in relativeTo.FindAllInScope<Report>())
                    {
                        IReportColumn column = report.Columns?.Find(c => c.Name == namePath);
                        if (column != null)
                            return new VariableObject(column.GetValue(0));
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
                        // Allow for the possibility that the first path element may point to
                        // the starting parent model, rather than to a child within that model
                        if ((i == 0) && relativeToModel.Name.Equals(namePathBits[i], compareType))
                            continue;
                        else
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
                    if (relativeToObject == null)
                        return null;
                    PropertyInfo propertyInfo = relativeToObject.GetType().GetProperty(namePathBits[j]);
                    if (propertyInfo == null && ignoreCase) // If not found, try using a case-insensitive search
                        propertyInfo = relativeToObject.GetType().GetProperty(namePathBits[j], BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (relativeToObject is IFunction && namePathBits[j] == "Value()")
                    {
                        MethodInfo method = relativeToModel.GetType().GetMethod("Value");
                        properties.Add(new VariableMethod(relativeToModel, method));
                    }
                    else if (propertyInfo == null && relativeToObject is Model)
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
                    else if (relativeToObject is IList)
                    {
                        // Special case: we are trying to get a property of an array(IList). In this case
                        // we want to return the property value for all items in the array.
                        VariableProperty property = new VariableProperty(relativeToObject, namePathBits[j], arraySpecifier);
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
    }
}
