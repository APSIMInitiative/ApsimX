namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
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
            if (string.IsNullOrEmpty(namePath))
                throw new Exception($"Unable to find variable with null variable name");
            else if (namePath[0] != '.' &&
                     (namePath.Replace("()", "").IndexOfAny("+*/".ToCharArray()) != -1
                     | (namePath.IndexOfAny("(".ToCharArray()) >= 0 && namePath.Substring(0, (namePath.IndexOf('(')>=0? namePath.IndexOf('(') : 0)).IndexOfAny("[.".ToCharArray()) == -1)))
            {
                // expression - need a better way of detecting an expression
                returnVariable = new VariableExpression(namePath, relativeTo as Model);
            }
            else
            {
                namePath = namePath.Replace("Value()", "Value().");
                // Remove a square bracketed model name and change our relativeTo model to 
                // the referenced model.
                if (namePath.StartsWith("["))
                {
                    int posCloseBracket = namePath.IndexOf(']');
                    if (posCloseBracket == -1)
                        throw new Exception($"No closing square bracket in variable name '{namePath}'.");

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
                        throw new Exception($"Unable to find any model with name or type {modelName} in scope of {relativeTo.Name}");
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
                string[] namePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                int i;
                for (i = 0; i < namePathBits.Length; i++)
                {
                    IModel localModel = relativeTo.Children.FirstOrDefault(m => m.Name.Equals(namePathBits[i], compareType) && m.Enabled);
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
                // PropertyInfo/MethodInfo or the path is invalid.
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
                        throw new Exception($"Unable to locate model {namePath}");

                    // Check property info
                    PropertyInfo propertyInfo = relativeToObject.GetType().GetProperty(namePathBits[j]);
                    if (propertyInfo == null && ignoreCase) // If not found, try using a case-insensitive search
                    {
                        propertyInfo = relativeToObject.GetType().GetProperty(namePathBits[j], BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    }

                    // If not property info
                    // Check method info
                    MethodInfo methodInfo = null;
                    List<object> argumentsList = null;
                    if (propertyInfo == null)
                    {
                        if (namePathBits[j].IndexOf('(') > 0)
                        {
                            relativeToObject.GetType().GetMethod(namePathBits[j].Substring(0, namePathBits[j].IndexOf('(')));
                            if (methodInfo == null && ignoreCase) // If not found, try using a case-insensitive search
                            {
                                methodInfo = relativeToObject.GetType().GetMethod(namePathBits[j].Substring(0, namePathBits[j].IndexOf('(')), BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
                            }
                        }
                        if (methodInfo != null)
                        {
                            // get arguments and store in VariableMethod
                            string args = namePathBits[j].Substring(namePathBits[j].IndexOf('('));
                            args = args.Substring(0,args.IndexOf(')'));
                            args = args.Replace("(", "").Replace(")", "");
                            if (args.Length > 0)
                            {
                                argumentsList = new List<object>();
                                args = args.Trim('(').Trim(')');
                                var argList = args.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                // get arguments
                                ParameterInfo[] pars = methodInfo.GetParameters();

                                for (int argid = 0; argid < argList.Length; argid++)
                                {
                                    var cleanArg = argList[argid].Trim(' ').Trim(new char[] { '(', ')' }).Trim(' ').Trim('"');
                                    switch (Type.GetTypeCode(pars[argid].ParameterType))
                                    {
                                        case TypeCode.Double:
                                            argumentsList.Add(Convert.ToDouble(cleanArg, CultureInfo.InvariantCulture));
                                            break;
                                        case TypeCode.Int32:
                                            argumentsList.Add(Convert.ToInt32(cleanArg, CultureInfo.InvariantCulture));
                                            break;
                                        case TypeCode.String:
                                            argumentsList.Add(cleanArg);
                                            break;
                                        case TypeCode.Boolean:
                                            argumentsList.Add(Convert.ToBoolean(cleanArg, CultureInfo.InvariantCulture));
                                            break;
                                        default:
                                            throw new ApsimXException(relativeToModel, "The type of argument (" + Type.GetTypeCode(pars[argid].ParameterType) + ") is not currently supported in Report methods");
                                    }
                                }
                            }
                        }
                    }

                    //if (relativeToObject is IFunction && namePathBits[j] == "Value()")
                    //{
                    //    MethodInfo method = relativeTo.GetType().GetMethod("Value");
                    //    properties.Add(new VariableMethod(relativeTo, method));
                    //}
                    //                    else if (propertyInfo == null && methodInfo == null && relativeToObject is Model)

                    if (propertyInfo == null && methodInfo == null && relativeToObject is IModel model)
                    {
                        // Not a property, may be an unchecked method or a child model.
                        localModel = model.Children.FirstOrDefault(m => m.Name.Equals(namePathBits[j], compareType));
                        if (localModel == null)
                        {
                            // Not a model
                            throw new Exception($"While locating model {namePath}: {namePathBits[j]} is not a child of {model.Name}");
                        }
                        else
                        {
                            properties.Add(new VariableObject(localModel));
                            relativeToObject = localModel;
                        }
                    }
                    else if (propertyInfo != null)
                    {
                        VariableProperty property = new VariableProperty(relativeToObject, propertyInfo, arraySpecifier);
                        properties.Add(property);
                        relativeToObject = property.Value;
                        if (relativeToObject == null)
                            return null;
                    }
                    else if (methodInfo != null)
                    {
                        VariableMethod method = null;
                        if (argumentsList != null)
                        {
                            method = new VariableMethod(relativeToObject, methodInfo, argumentsList.ToArray<object>());
                        }
                        else
                        {
                            method = new VariableMethod(relativeToObject, methodInfo, null);
                        }
                        //                        VariableProperty property = new VariableProperty(relativeToObject, propertyInfo, arraySpecifier);
                        properties.Add(method);
                        relativeToObject = method.Value;
                        if (relativeToObject == null)
                            return null;
                    }
                    else if (relativeToObject is IList)
                    {
                        // Special case: we are trying to get a property of an array(IList). In this case
                        // we want to return the property value for all items in the array.
                        VariableProperty property = new VariableProperty(relativeToObject, namePathBits[j]);
                        properties.Add(property);
                        relativeToObject = property.Value;
                        if (relativeToObject == null)
                            return null;
                    }
                    else
                    {
                        throw new Exception($"While locating model {namePath}: unknown model or property specification {namePathBits[j]}");
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

        ///// <summary>
        ///// Get the value of a variable or model.
        ///// </summary>
        ///// <param name="namePath">The name of the object to return</param>
        ///// <param name="ignoreCase">If true, ignore case when searching for the object or property</param>
        ///// <returns>The found object or null if not found</returns>
        //private IVariable GetInternal(string namePath, bool ignoreCase = true)
        //{
        //    IModel relativeTo = relativeToModel;
        //    string cacheKey = namePath;
        //    StringComparison compareType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        //    // Look in cache first.
        //    object value = null;
        //    if (cache.ContainsKey(cacheKey))
        //        value = cache[cacheKey];
        //    if (value != null)
        //        return value as IVariable;

        //    IVariable returnVariable = null;
        //    if (namePath == null || namePath.Length == 0)
        //    {
        //        return null;
        //    }
        //    else if (namePath[0] != '.' &&
        //             namePath.Replace("()", "").IndexOfAny("+*/".ToCharArray()) != -1)
        //    {
        //        // expression - need a better way of detecting an expression
        //        returnVariable = new VariableExpression(namePath, relativeTo as Model);
        //    }
        //    else
        //    {
        //        // Remove a square bracketed model name and change our relativeTo model to 
        //        // the referenced model.
        //        if (namePath.StartsWith("["))
        //        {
        //            int posCloseBracket = namePath.IndexOf(']');
        //            if (posCloseBracket == -1)
        //            {
        //                return null;
        //            }
        //            string modelName = namePath.Substring(1, posCloseBracket - 1);
        //            namePath = namePath.Remove(0, posCloseBracket + 1);
        //            Model foundModel = Apsim.Find(relativeTo, modelName) as Model;
        //            if (foundModel == null)
        //            {
        //                // Didn't find a model with a name matching the square bracketed string so
        //                // now try and look for a model with a type matching the square bracketed string.
        //                Type[] modelTypes = GetTypeWithoutNameSpace(modelName);
        //                if (modelTypes.Length == 1)
        //                    foundModel = Apsim.Find(relativeTo, modelTypes[0]) as Model;
        //            }
        //            if (foundModel == null)
        //                return null;
        //            else
        //                relativeTo = foundModel;
        //        }
        //        else if (namePath.StartsWith("."))
        //        {
        //            // Absolute path
        //            IModel root = relativeTo;
        //            while (root.Parent != null)
        //            {
        //                root = root.Parent as Model;
        //            }
        //            relativeTo = root;

        //            int posPeriod = namePath.IndexOf('.', 1);
        //            if (posPeriod == -1)
        //            {
        //                posPeriod = namePath.Length;
        //            }

        //            namePath = namePath.Remove(0, posPeriod);
        //            if (namePath.StartsWith("."))
        //            {
        //                namePath.Remove(1);
        //            }
        //        }

        //        // Now walk the series of '.' separated path bits, assuming the path bits
        //        // are child models. Stop when we can't find the child model.
        //        string[] namePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        //        int i;
        //        for (i = 0; i < namePathBits.Length; i++)
        //        {
        //            IModel localModel = relativeTo.Children.FirstOrDefault(m => m.Name.Equals(namePathBits[i], compareType));
        //            if (localModel == null)
        //            {
        //                break;
        //            }
        //            else
        //            {
        //                relativeTo = localModel as Model;
        //            }
        //        }

        //        // At this point there are only 2 possibilities. We have encountered a 
        //        // PropertyInfo/MethodInfo or the path is invalid.
        //        // We now need to loop through the remaining path bits and keep track of each
        //        // section of the path as each section will have to be evaulated everytime a
        //        // a get is done for this path. 
        //        // The variable 'i' will point to the name path that cannot be found as a model.
        //        object relativeToObject = relativeTo;
        //        List<IVariable> properties = new List<IVariable>();
        //        properties.Add(new VariableObject(relativeTo));
        //        for (int j = i; j < namePathBits.Length; j++)
        //        {
        //            // look for an array specifier e.g. sw[2]
        //            string arraySpecifier = null;
        //            if (namePathBits[j].Contains("["))
        //            {
        //                arraySpecifier = StringUtilities.SplitOffBracketedValue(ref namePathBits[j], '[', ']');
        //            }

        //            // Look for either a property or a child model.
        //            IModel localModel = null;
        //            if (relativeToObject == null)
        //                return null;
        //            PropertyInfo propertyInfo = relativeToObject.GetType().GetProperty(namePathBits[j]);
        //            if (propertyInfo == null && ignoreCase) // If not found, try using a case-insensitive search
        //                propertyInfo = relativeToObject.GetType().GetProperty(namePathBits[j], BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
        //            if (relativeToObject is IFunction && namePathBits[j] == "Value()")
        //            {
        //                MethodInfo method = relativeTo.GetType().GetMethod("Value");
        //                properties.Add(new VariableMethod(relativeTo, method));
        //            }
        //            else if (propertyInfo == null && relativeToObject is Model)
        //            {
        //                // Not a property, may be an unchecked method or a child model.
        //                localModel = (relativeToObject as IModel).Children.FirstOrDefault(m => m.Name.Equals(namePathBits[j], compareType));
        //                if (localModel == null)
        //                {
        //                    // not a child model so check that it still isn't an unchecked method with arguments
        //                    MethodInfo methodInfo = null;
        //                    if(namePathBits[j].IndexOf('(')>0)
        //                    { 
        //                        relativeToObject.GetType().GetMethod(namePathBits[j].Substring(0, namePathBits[j].IndexOf('(')));
        //                        if (methodInfo == null && ignoreCase) // If not found, try using a case-insensitive search
        //                        {
        //                            methodInfo = relativeToObject.GetType().GetMethod(namePathBits[j].Substring(0, namePathBits[j].IndexOf('(')), BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
        //                        }
        //                    }
        //                    if (methodInfo != null)
        //                    {
        //                        // get arguments and store in VariableMethod
        //                        List<object> argumentsList = new List<object>();
        //                        string args = namePathBits[j].Substring(namePathBits[j].IndexOf('('));
        //                        if (args.Length > 0)
        //                        {
        //                            args = args.Trim('(').Trim(')');
        //                            var argList = args.Split(',');
        //                            // get arguments
        //                            ParameterInfo[] pars = methodInfo.GetParameters();

        //                            for (int argid = 0; argid < argList.Length; argid++)
        //                            {
        //                                var cleanArg = argList[argid].Trim(' ').Trim(new char[] { '(', ')' }).Trim(' ').Trim('"');
        //                                switch (Type.GetTypeCode(pars[argid].ParameterType))
        //                                {
        //                                    case TypeCode.Double:
        //                                        argumentsList.Add(Convert.ToDouble(cleanArg, CultureInfo.InvariantCulture));
        //                                        break;
        //                                    case TypeCode.Int32:
        //                                        argumentsList.Add(Convert.ToInt32(cleanArg));
        //                                        break;
        //                                    case TypeCode.String:
        //                                        argumentsList.Add(cleanArg);
        //                                        break;
        //                                    default:
        //                                        break;
        //                                }
        //                            }
        //                            properties.Add(new VariableMethod(relativeTo, methodInfo, argumentsList.ToArray<object>()));
        //                        }
        //                        else
        //                        {
        //                            properties.Add(new VariableMethod(relativeTo, methodInfo, null));
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // Not a model or method
        //                        return null;
        //                    }
        //                }
        //                else
        //                {
        //                    properties.Add(new VariableObject(localModel));
        //                    relativeToObject = localModel;
        //                }
        //            }
        //            else if (propertyInfo != null)
        //            {
        //                VariableProperty property = new VariableProperty(relativeToObject, propertyInfo, arraySpecifier);
        //                properties.Add(property);
        //                relativeToObject = property.Value;
        //            }
        //            else if (relativeToObject is IList)
        //            {
        //                // Special case: we are trying to get a property of an array(IList). In this case
        //                // we want to return the property value for all items in the array.
        //                VariableProperty property = new VariableProperty(relativeToObject, namePathBits[j]);
        //                properties.Add(property);
        //                relativeToObject = property.Value;
        //            }
        //            else
        //            {
        //                return null;
        //            }
        //        }

        //        // We now have a list of IVariable instances that can be evaluated to 
        //        // produce a return value for the given path. Wrap the list into an IVariable.
        //        returnVariable = new VariableComposite(namePath, properties);
        //    }

        //    // Add variable to cache.
        //    cache.Add(cacheKey, returnVariable);

        //    return returnVariable;
        //}

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
