using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;

namespace Models.Core
{

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
        /// Remove a single entry from the cache. 
        /// Should be called if the old path may become invalid.
        /// </summary>
        /// <param name="path"></param>
        public void ClearEntry(string path)
        {
            cache.Remove(path);
        }

        /// <summary>
        /// Get the value of a variable or model.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <param name="flags"><see cref="LocatorFlags"/> controlling the search</param>
        /// <returns>The found object or null if not found</returns>
        public object Get(string namePath, LocatorFlags flags = LocatorFlags.None)
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
            return relativeToModel.FindAllInScope().FirstOrDefault(m => typeToMatch.IsAssignableFrom(m.GetType()));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="namePath"></param>
        /// <param name="flags">LocatorFlags controlling the search</param>
        /// <returns></returns>
        public IVariable GetObject(string namePath, LocatorFlags flags = LocatorFlags.None)
        {
            return GetInternal(namePath, flags);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="namePath"></param>
        /// <param name="flags">LocatorFlags controlling the search</param>
        /// <returns>Information about the named variable, but without its current data values</returns>
        public IVariable GetObjectProperties(string namePath, LocatorFlags flags = LocatorFlags.PropertiesOnly)
        {
            return GetInternal(namePath, flags | LocatorFlags.PropertiesOnly);
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
        /// Test whether a name appears to represent an Expression
        /// Probably need a better way of detecting an expression
        /// </summary>
        /// <param name="path">The string to be tested</param>
        /// <returns>True if this appears to be an expression</returns>
        private bool IsExpression(string path)
        {
            //-- Remove all white spaces from the string --
            path = path.Replace(" ", "").Replace("()", "");
            if (path.Length == 0 || path[0] == '.')
                return false;
            if (path.IndexOfAny("+*/^".ToCharArray()) >= 0) // operators indicate an expression
                return true;
            int openingParen = path.IndexOf('(');
            if (openingParen >= 0 && path.Substring(0, openingParen).IndexOfAny("[.".ToCharArray()) == -1)
                return true;
            return false;
        }

        /// <summary>
        /// Get the value of a variable or model.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <param name="flags"><see cref="LocatorFlags"/> controlling the search</param>
        /// <returns>The found object or null if not found</returns>
        //        /// <param name="ignoreCase">If true, ignore case when searching for the object or property</param>
        //        /// <param name="propertiesOnly">If true, fetch only property information, but not the value</param>
        //        /// <param name="includeDisabled">If true, include disabled models in the search</param>
        private IVariable GetInternal(string namePath, LocatorFlags flags = LocatorFlags.None)
        {
            IModel relativeTo = relativeToModel;
            string cacheKey = namePath;
            bool ignoreCase = (flags & LocatorFlags.CaseSensitive) != LocatorFlags.CaseSensitive;
            StringComparison compareType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            bool throwOnError = (flags & LocatorFlags.ThrowOnError) == LocatorFlags.ThrowOnError;

            IVariable returnVariable = null;
            if (string.IsNullOrEmpty(namePath))
            {
                if (throwOnError)
                    throw new Exception($"Unable to find variable with null variable name");
                else
                    return null;
            }

            // Look in cache first.
            object value = null;
            if (cache.ContainsKey(cacheKey))
                value = cache[cacheKey];
            if (value != null)
                return value as IVariable;

            if (IsExpression(namePath))
            {
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
                    {
                        if (throwOnError)
                            throw new Exception($"No closing square bracket in variable name '{namePath}'.");
                        else
                            return null;
                    }
                    string modelName = namePath.Substring(1, posCloseBracket - 1);
                    namePath = namePath.Remove(0, posCloseBracket + 1);
                    Model foundModel = relativeTo.FindInScope(modelName) as Model;
                    if (foundModel == null)
                    {
                        // Didn't find a model with a name matching the square bracketed string so
                        // now try and look for a model with a type matching the square bracketed string.
                        Type[] modelTypes = ReflectionUtilities.GetTypeWithoutNameSpace(modelName, Assembly.GetExecutingAssembly());
                        if (modelTypes.Length == 1)
                            foundModel = relativeTo.FindAllInScope().FirstOrDefault(m => modelTypes[0].IsAssignableFrom(m.GetType())) as Model;
                    }
                    if (foundModel == null)
                    {
                        if (throwOnError)
                            throw new Exception($"Unable to find any model with name or type {modelName} in scope of {relativeTo.Name}");
                        else
                            return null;
                    }
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

                    namePath = namePath.Remove(0, 1);
                    int posPeriod = namePath.IndexOf('.');
                    if (posPeriod == -1)
                    {
                        posPeriod = namePath.Length;
                    }
                    string rootName = namePath.Substring(0, posPeriod);
                    if (relativeTo.Name.Equals(rootName, compareType))
                        namePath = namePath.Remove(0, posPeriod);
                    else
                    {
                        if (throwOnError)
                            throw new Exception($"Incorrect root name in absolute path '.{namePath}'");
                        else
                            return null;
                    }
                    if (namePath.StartsWith("."))
                    {
                        namePath = namePath.Remove(0, 1);
                    }
                }
                else if ((flags & LocatorFlags.IncludeReportVars) == LocatorFlags.IncludeReportVars)
                {
                    // Try a report column.
                    foreach (Report report in relativeTo.FindAllInScope<Report>())
                    {
                        IReportColumn column = report.Columns?.Find(c => c.Name == namePath);
                        if (column != null && !((column is ReportColumn) && (column as ReportColumn).possibleRecursion))
                        {
                            // Things can get nasty here. The call below to column.GetValue(0) has the
                            // potential to call this routine recursively. Consider as an example
                            // a Report column with the contents "n + 1 as n". This is hard to catch
                            // and handle, since it leads to a StackOverflowException which cannot be
                            // caught. One way to prevent this problem is to check whether the
                            // stack has grown unreasonably large, then throw our own Exception.
                            //
                            // This possiblity of recursion is also one reason why the "throwOnError" option
                            // is not handled by exclosing the whole routine in a try block, then deciding
                            // in the catch section whether to rethrow the exception or return null.
                            int nFrames = new System.Diagnostics.StackTrace().FrameCount;
                            if (nFrames > 1000)
                            {
                                if (throwOnError)
                                    throw new Exception("Recursion error");
                                else
                                    return null;
                            }
                            return new VariableObject(column.GetValue(0));
                        }
                    }
                }

                // Now walk the series of '.' separated path bits, assuming the path bits
                // are child models. Stop when we can't find the child model.
                string[] namePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                if (namePathBits.Length == 0 && !string.IsNullOrEmpty(namePath))
                {
                    if (throwOnError)
                        throw new Exception($"Invalid variable name: '{cacheKey}'");
                    else
                        return null;
                }
                int i;
                bool includeDisabled = (flags & LocatorFlags.IncludeDisabled) == LocatorFlags.IncludeDisabled;
                for (i = 0; i < namePathBits.Length; i++)
                {
                    IModel localModel = relativeTo.Children.FirstOrDefault(m => m.Name.Trim().Equals(namePathBits[i], compareType) && (includeDisabled || m.Enabled));
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
                    {
                        if (throwOnError)
                            throw new Exception($"Unable to locate model {namePath}");
                        else
                            return null;
                    }
                    // Check property info
                    Type declaringType;
                    if (properties.Any())
                        declaringType = properties.Last().DataType;
                    else
                        declaringType = relativeToObject.GetType();
                    PropertyInfo propertyInfo = declaringType.GetProperty(namePathBits[j]);
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
                            // before trying to access the method we need to identify the types of arguments to identify overloaded methods.
                            // assume: presence of quotes is string
                            // assume: tolower = false or true is boolean
                            // assume: presence of . and tryparse is double
                            // assume: tryparse is int32
                            List<Type> argumentsTypes = new List<Type>();
                            argumentsList = new List<object>();
                            // get arguments and store in VariableMethod
                            string args = namePathBits[j].Substring(namePathBits[j].IndexOf('('));
                            args = args.Substring(0, args.IndexOf(')'));
                            args = args.Replace("(", "").Replace(")", "");

                            if (args.Length > 0)
                            {
                                args = args.Trim('(').Trim(')');
                                var argList = args.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim(' ')).ToArray();

                                for (int argid = 0; argid < argList.Length; argid++)
                                {
                                    var trimmedarg = argList[argid].Trim(' ').Trim(new char[] { '(', ')' }).Trim(' ');
                                    if (trimmedarg.Contains('"'))
                                    {
                                        argumentsTypes.Add(typeof(string));
                                        argumentsList.Add(trimmedarg.Trim('\"'));
                                    }
                                    else if (trimmedarg.ToLower() == "false" || trimmedarg.ToLower() == "true")
                                    {
                                        argumentsTypes.Add(typeof(bool));
                                        argumentsList.Add(Convert.ToBoolean(trimmedarg, CultureInfo.InvariantCulture));
                                    }
                                    else if (trimmedarg.Contains('.') && double.TryParse(trimmedarg, out _))
                                    {
                                        argumentsTypes.Add(typeof(double));
                                        argumentsList.Add(Convert.ToDouble(trimmedarg, CultureInfo.InvariantCulture));
                                    }
                                    else if (Int32.TryParse(trimmedarg, out _))
                                    {
                                        argumentsTypes.Add(typeof(Int32));
                                        argumentsList.Add(Convert.ToInt32(trimmedarg, CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        if (throwOnError)
                                            throw new ApsimXException(relativeToModel, $"Unable to determine the type of argument ({trimmedarg}) in Report method");
                                        else
                                            return null;
                                    }
                                }
                            }

                            // try get the method with identified arguments
                            methodInfo = relativeToObject.GetType().GetMethod(namePathBits[j].Substring(0, namePathBits[j].IndexOf('(')), argumentsTypes.ToArray<Type>());
                            if (methodInfo == null && ignoreCase) // If not found, try using a case-insensitive search
                            {
                                BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase;
                                methodInfo = relativeToObject.GetType().GetMethod(namePathBits[j].Substring(0, namePathBits[j].IndexOf('(')), argumentsTypes.Count(), bindingFlags, null, argumentsTypes.ToArray<Type>(), null);
                            }
                        }
                    }

                    bool propertiesOnly = (flags & LocatorFlags.PropertiesOnly) == LocatorFlags.PropertiesOnly;

                    if (relativeToObject is Models.Functions.IFunction && namePathBits[j] == "Value()")
                    {
                        MethodInfo method = relativeTo.GetType().GetMethod("Value");
                        properties.Add(new VariableMethod(relativeTo, method));
                    }
                    else if (propertyInfo == null && methodInfo == null && relativeToObject is IModel model)
                    {
                        // Not a property, may be an unchecked method or a child model.
                        localModel = model.Children.FirstOrDefault(m => m.Name.Equals(namePathBits[j], compareType));
                        if (localModel == null)
                        {
                            // Not a model
                            if (throwOnError)
                                throw new Exception($"While locating model {namePath}: {namePathBits[j]} is not a child of {model.Name}");
                            else
                                return null;
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
                        if (propertiesOnly && j == namePathBits.Length - 1)
                            break;
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
                        properties.Add(method);
                        if (propertiesOnly && j == namePathBits.Length - 1)
                            break;
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
                        if (propertiesOnly && j == namePathBits.Length - 1)
                            break;
                        relativeToObject = property.Value;
                        if (relativeToObject == null)
                            return null;
                    }
                    else
                    {
                        if (throwOnError)
                            throw new Exception($"While locating model {namePath}: unknown model or property specification {namePathBits[j]}");
                        else
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
    }
}
