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
    using System.Collections;
    using Functions;

    /// <summary>
    /// This class is responsible for the location and retrieval of variables or models 
    /// given a path.
    /// Path example syntax:
    ///    .Clock.Start                         ABSOLUTE PATH
    ///    [PotatoSowingRule].Script.SowDate    RELATIVE TO A MODEL IN SCOPE
    ///    Leaf.LAI                             RELATIVE CHILD MODEL.
    /// </summary>
    [Serializable]
    public class Locator : ILocator
    {
        /// <summary>The model this locator is relative to</summary>
        private IModel relativeToModel;

        /// <summary>
        /// A cache for speeding up look ups. The object can be either 
        /// Model[] or an IVariable.
        /// </summary>
        private Dictionary<string, object> cache = new Dictionary<string, object>();

        /// <summary>Constructor</summary>
        /// <param name="relativeTo">Model locator is relative to</param>
        internal Locator(IModel relativeTo)
        {
            relativeToModel = relativeTo;
        }

        /// <summary>Clear the cache</summary>
        public void Clear()
        {
            cache.Clear();
        }

        /// <summary>
        /// Get the value of a variable or model.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <returns>The found object or null if not found</returns>
        public object Get(string namePath)
        {
            IVariable variable = GetInternal(namePath);
            if (variable == null)
                return variable;
            else
                return variable.Value;
        }

        /// <summary>Gets a model in scope of the specified type</summary>
        /// <param name="typeToMatch">The type of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public IModel Get(Type typeToMatch)
        {
            return Apsim.Find(relativeToModel, typeToMatch);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="namePath"></param>
        /// <returns></returns>
        public IVariable GetObject(string namePath)
        {
            return GetInternal(namePath);
        }

        /// <summary>
        /// Set the value of a variable. Will throw if variable doesn't exist.
        /// </summary>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public void Set(string namePath, object value)
        {
            IVariable variable = GetInternal(namePath);
            if (variable == null)
                throw new Exception("Cannot set the value of variable '" + namePath + "'. Variable doesn't exist");
            else
                variable.Value = value;
        }

        /// <summary>
        /// Get the value of a variable or model.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <param name="ignoreCase">If true, ignore case when searching for the object or property</param>
        /// <returns>The found object or null if not found</returns>
        private IVariable GetInternal(string namePath, bool ignoreCase = true)
        {
            IModel relativeTo = relativeToModel;
            string cacheKey = namePath;
            StringComparison compareType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            // Look in cache first.
            object value = null;
            if (cache.ContainsKey(cacheKey))
                value = cache[cacheKey];
            if (value != null)
                return value as IVariable;

            IVariable returnVariable = null;
            if (namePath == null || namePath.Length == 0)
            {
                return null;
            }
            else if (namePath[0] != '.' &&
                     namePath.Replace("()", "").IndexOfAny("(+*/".ToCharArray()) != -1)
            {
                // expression - need a better way of detecting an expression
                returnVariable = new VariableExpression(namePath, relativeTo as Model);
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
                    Model foundModel = Apsim.Find(relativeTo, modelName) as Model;
                    if (foundModel == null)
                    {
                        // Didn't find a model with a name matching the square bracketed string so
                        // now try and look for a model with a type matching the square bracketed string.
                        Type[] modelTypes = GetTypeWithoutNameSpace(modelName);
                        if (modelTypes.Length == 1)
                            foundModel = Apsim.Find(relativeTo, modelTypes[0]) as Model;
                    }
                    if (foundModel == null)
                        return null;
                    else
                        relativeTo = foundModel;
                }
                else if (namePath.StartsWith("."))
                {
                    // Absolute path
                    IModel root = relativeTo;
                    while (root.Parent != null)
                    {
                        root = root.Parent as Model;
                    }
                    relativeTo = root;

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
                    IModel localModel = relativeTo.Children.FirstOrDefault(m => m.Name.Equals(namePathBits[i], compareType));
                    if (localModel == null)
                    {
                        break;
                    }
                    else
                    {
                        relativeTo = localModel as Model;
                    }
                }

                // At this point there are only 2 possibilities. We have encountered a 
                // PropertyInfo or the path is invalid.
                // We now need to loop through the remaining path bits and keep track of each
                // section of the path as each section will have to be evaulated everytime a
                // a get is done for this path. 
                // The variable 'i' will point to the name path that cannot be found as a model.
                object relativeToObject = relativeTo;
                List<IVariable> properties = new List<IVariable>();
                properties.Add(new VariableObject(relativeTo));
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
                        MethodInfo method = relativeTo.GetType().GetMethod("Value");
                        properties.Add(new VariableMethod(relativeTo, method));
                    }
                    else if (propertyInfo == null && relativeToObject is Model)
                    {
                        // Not a property, may be a child model.
                        localModel = (relativeToObject as IModel).Children.FirstOrDefault(m => m.Name.Equals(namePathBits[j], compareType));
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
                        VariableProperty property = new VariableProperty(relativeToObject, namePathBits[j]);
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
            cache.Add(cacheKey, returnVariable);

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

    }
}
