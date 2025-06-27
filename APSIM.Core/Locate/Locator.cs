using System.Collections;
using System.Globalization;
using System.Reflection;
using APSIM.Shared.Utilities;

namespace APSIM.Core;


/// <summary>
/// This class is responsible for the location and retrieval of variables or models
/// given a path.
/// Path example syntax:
///    .Clock.Start                         ABSOLUTE PATH
///    [PotatoSowingRule].Script.SowDate    RELATIVE TO A MODEL IN SCOPE
///    Leaf.LAI                             RELATIVE CHILD MODEL.
/// </summary>
internal class Locator
{
    /// <summary>Cache for speeding up look ups.</summary>
    private Dictionary<(object relativeTo, string path), VariableComposite> cache = new();

    /// <summary>Constructor</summary>
    internal Locator()
    {
    }

    /// <summary>Clear the cache</summary>
    public void Clear() => cache.Clear();

    /// <summary>
    /// Remove a single entry from the cache.
    /// Should be called if the old path may become invalid.
    /// </summary>
    /// <param name="path"></param>
    public void ClearEntry(Node relativeTo, string path)
    {
        cache.Remove((relativeTo, path));
    }

    /// <summary>
    /// Get the value of a variable or model.
    /// </summary>
    /// <param name="namePath">The name of the object to return</param>
    /// <param name="flags"><see cref="LocatorFlags"/> controlling the search</param>
    /// <returns>The found object or null if not found</returns>
    public object Get(Node relativeTo, string namePath, LocatorFlags flags = LocatorFlags.None)
    {
        var variable = GetObject(relativeTo, namePath, flags);
        if (variable == null)
            return null;
        else
            return variable.Value;
    }

    /// <summary>
    /// Returns a Variable from the given path and flags
    /// </summary>
    /// <param name="namePath"></param>
    /// <param name="flags">LocatorFlags controlling the search</param>
    /// <returns></returns>
    public VariableComposite GetObject(Node relativeTo, string namePath, LocatorFlags flags = LocatorFlags.None)
    {
        return GetInternal(relativeTo, namePath, flags);
    }

    /// <summary>
    /// Set the value of a variable. Will throw if variable doesn't exist.
    /// </summary>
    /// <param name="namePath">The name of the object to set</param>
    /// <param name="value">The value to set the property to</param>
    public void Set(Node relativeTo, string namePath, object value)
    {
        VariableComposite variable = GetInternal(relativeTo, namePath);
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
    private VariableComposite GetInternal(Node relativeTo, string namePath, LocatorFlags flags = LocatorFlags.None)
    {
        var cacheKey = (relativeTo, namePath);
        bool ignoreCase = (flags & LocatorFlags.CaseSensitive) != LocatorFlags.CaseSensitive;
        StringComparison compareType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        bool throwOnError = (flags & LocatorFlags.ThrowOnError) == LocatorFlags.ThrowOnError;
        bool onlyModelChildren = (flags & LocatorFlags.ModelsOnly) == LocatorFlags.ModelsOnly;
        bool includeReportVars = (flags & LocatorFlags.IncludeReportVars) == LocatorFlags.IncludeReportVars;

        //check if a path was given
        if (string.IsNullOrEmpty(namePath))
        {
            if (throwOnError)
                throw new Exception($"Unable to find variable with null variable name");
            else
                return null;
        }

        // check if path is in cache
        VariableComposite value = null;
        if (cache.ContainsKey(cacheKey) && !onlyModelChildren)
            value = cache[cacheKey];
        if (value != null)
            return value;

        //check if path is actually an expression
        if (IsExpression(namePath))
        {
            var returnVariable = new VariableComposite(namePath);
            returnVariable.AddExpression(relativeTo.Model, namePath);
            cache.Add(cacheKey, returnVariable);
            return returnVariable;
        }

        namePath = namePath.Replace("Value()", "Value().");

        //If our name starts with [ or . we need to handle that formatting and figure out where that model is
        if (namePath.StartsWith("[") || namePath.StartsWith("."))
        {
            relativeTo = GetInternalRelativeTo(relativeTo.Model, namePath, compareType, throwOnError, out string filteredNamePath);
            namePath = filteredNamePath;
        }
        //if it doesn't start with those characters, it might be a report variable, so we need to search for that in the report columns
        else if (includeReportVars)
        {
            throw new NotImplementedException();
            /* TODO
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
            */
        }

        //exit here early if we don't have a starting point
        if (relativeTo == null)
        {
            if (throwOnError)
                throw new Exception($"Unable to locate model {namePath}");
            else
                return null;
        }

        //chop the name path up into pieces that can each be searched for
        string[] namePathBits = GetInternalNameBits(namePath, throwOnError);

        // We now need to loop through the remaining path bits and keep track of each
        // section of the path as each section will have to be evaulated everytime a
        // a get is done for this path.
        object relativeToObject = relativeTo is Node n ? n.Model : relativeTo;  // can be any class instance or variable, but not a Node.
        var composite = new VariableComposite(namePath);
        composite.AddInstance(relativeTo.Model);
        for (int j = 0; j < namePathBits.Length; j++)
        {
            // look for an array specifier e.g. sw[2]
            //need to do this first as the [ ] will screw up matching to a property
            string arraySpecifier = null;
            if (!onlyModelChildren && namePathBits[j].Contains("["))
                arraySpecifier = StringUtilities.SplitOffBracketedValue(ref namePathBits[j], '[', ']');

            object objectInfo = GetInternalObjectInfo(relativeToObject, namePathBits[j], composite, namePathBits.Length-j-1, ignoreCase, throwOnError, onlyModelChildren, out List<object> argumentsList);

            //Depending on the type we found, handle it
            bool propertiesOnly = (flags & LocatorFlags.PropertiesOnly) == LocatorFlags.PropertiesOnly;

            if (objectInfo is PropertyInfo propertyInfo)
            {
                composite.AddProperty(relativeToObject, propertyInfo, arraySpecifier);
                if (propertiesOnly && j == namePathBits.Length - 1)
                    break;
                relativeToObject = composite.Value;
                if (relativeToObject == null)
                    return null;
            }
            else if ((objectInfo as MethodInfo) != null)
            {
                MethodInfo methodInfo = objectInfo as MethodInfo;
                composite.AddMethod(relativeToObject, methodInfo, argumentsList?.ToArray<object>());
                if (propertiesOnly && j == namePathBits.Length - 1)
                    break;
                relativeToObject = composite.Value;
                if (relativeToObject == null)
                    return null;
            }
            else if (relativeToObject is IList)
            {
                // Special case: we are trying to get a property of an array(IList). In this case
                // we want to return the property value for all items in the array.
                composite.AddProperty(relativeToObject, namePathBits[j]);
                if (propertiesOnly && j == namePathBits.Length - 1)
                    break;
                relativeToObject = composite.Value;
                if (relativeToObject == null)
                    return null;
            }
            else if (objectInfo is INodeModel)
            {
                // No need to keep existing models. It is more more efficient to replace them with new model.
                composite.Clear();
                composite.AddInstance(objectInfo);
                relativeToObject = objectInfo;
            }
            else
            {
                if (throwOnError)
                    throw new Exception($"While locating model {namePath}: unknown model or property specification {namePathBits[j]}");
                else
                    return null;
            }
        }

        // Add variable to cache.
        if (!onlyModelChildren) //don't add this to the cache if it's been found by skipping properties/methods
            cache.Add(cacheKey, composite);
        return composite;
    }

    private Node GetInternalRelativeTo(INodeModel relativeTo, string namePath, StringComparison compareType, bool throwOnError, out string namePathFiltered)
    {
        string path = namePath;
        namePathFiltered = "";

        // Remove a square bracketed model name and change our relativeTo model to
        // the referenced model.
        if (path.StartsWith("["))
        {
            int posCloseBracket = path.IndexOf(']');
            if (posCloseBracket == -1)
            {
                if (throwOnError)
                    throw new Exception($"No closing square bracket in variable name '{path}'.");
                else
                    return null;
            }
            string modelName = path.Substring(1, posCloseBracket - 1);
            path = path.Remove(0, posCloseBracket + 1);
            Node foundModel = relativeTo.Node.FindInScope(modelName);
            if (foundModel == null)
            {
                // Didn't find a model with a name matching the square bracketed string so
                // now try and look for a model with a type matching the square bracketed string.
                Type[] modelTypes = ReflectionUtilities.GetTypeWithoutNameSpace(modelName, Assembly.GetExecutingAssembly());
                if (modelTypes.Length == 1)
                    foundModel = relativeTo.Node.WalkScoped().FirstOrDefault(m => modelTypes[0].IsAssignableFrom(m.GetType()));
            }
            if (foundModel == null)
            {
                if (throwOnError)
                    throw new Exception($"Unable to find any model with name or type {modelName} in scope of {relativeTo.Name}");
                else
                    return null;
            }
            else
            {
                namePathFiltered = path;
                return foundModel;
            }

        }
        else if (path.StartsWith("."))
        {
            // Absolute path
            Node root;
            if (relativeTo.Node.Parent == null)
                root = relativeTo.Node;
            else
                root = relativeTo.Node.WalkParents()?.Last();

            path = path.Remove(0, 1);
            int posPeriod = path.IndexOf('.');
            if (posPeriod == -1)
            {
                posPeriod = path.Length;
            }
            string rootName = path.Substring(0, posPeriod);
            if (root.Name.Equals(rootName, compareType))
                path = path.Remove(0, posPeriod);
            else
            {
                if (throwOnError)
                    throw new Exception($"Incorrect root name in absolute path '.{path}'");
                else
                    return null;
            }
            if (path.StartsWith("."))
            {
                path = path.Remove(0, 1);
            }
            namePathFiltered = path;
            return root;
        }
        else
        {
            if (throwOnError)
                throw new Exception($"Path does not start with . or [ '{path}'");
            else
                return null;
        }
    }
    private string[] GetInternalNameBits(string namePath, bool throwOnError)
    {
        // Now walk the series of '.' separated path bits, assuming the path bits
        // are child models. Stop when we can't find the child model.
        string[] bits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        List<string> bitsTrimmed = new List<string>();
        for(int i = 0; i < bits.Length; i++)
        {
            bits[i] = bits[i].Trim();
            if (bits[i].Length > 0)
                bitsTrimmed.Add(bits[i]);
        }
        string[] namePathBits = bitsTrimmed.ToArray();

        if (namePathBits.Length == 0 && !string.IsNullOrEmpty(namePath))
        {
            if (throwOnError)
                throw new Exception($"Invalid variable name: '{namePath}'");
            else
                return null;
        }

        return namePathBits;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="relativeToObject">Can be any class instance or variable but not a Node.</param>
    /// <param name="name"></param>
    /// <param name="composite"></param>
    /// <param name="remainingNames"></param>
    /// <param name="ignoreCase"></param>
    /// <param name="throwOnError"></param>
    /// <param name="onlyModelChildren"></param>
    /// <param name="argumentsList"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private object GetInternalObjectInfo(object relativeToObject, string name, VariableComposite composite, int remainingNames, bool ignoreCase, bool throwOnError, bool onlyModelChildren, out List<object> argumentsList)
    {

        argumentsList = null;
        PropertyInfo propertyInfo = null;
        MethodInfo methodInfo = null;
        INodeModel modelInfo = null;

        if (!onlyModelChildren)
        {

            // Check if property
            Type declaringType;
            if (composite.Any())
            {
                declaringType = composite.DataType;
                //Make sure we get the runtime created type of a manager script
                if ((relativeToObject.GetType().Name == "Manager" && name.Equals("Script")) ||
                     relativeToObject.GetType().GetInterfaces().FirstOrDefault(i => i.Name == "IScript") != null)
                    declaringType = composite.Value.GetType();
            }
            else
                declaringType = relativeToObject.GetType();
            propertyInfo = declaringType.GetProperty(name);
            if (propertyInfo == null && ignoreCase) // If not found, try using a case-insensitive search
            {
                propertyInfo = relativeToObject.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }
        }

        if (!onlyModelChildren && propertyInfo == null)
        {
            if (name.IndexOf('(') > 0)
            {
                // before trying to access the method we need to identify the types of arguments to identify overloaded methods.
                // assume: presence of quotes is string
                // assume: tolower = false or true is boolean
                // assume: presence of . and tryparse is double
                // assume: tryparse is int32
                List<Type> argumentsTypes = new List<Type>();
                argumentsList = new List<object>();
                // get arguments and store in VariableMethod
                string args = name.Substring(name.IndexOf('('));
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
                            argumentsList.Add(System.Convert.ToBoolean(trimmedarg, CultureInfo.InvariantCulture));
                        }
                        else if (trimmedarg.Contains('.') && double.TryParse(trimmedarg, out _))
                        {
                            argumentsTypes.Add(typeof(double));
                            argumentsList.Add(System.Convert.ToDouble(trimmedarg, CultureInfo.InvariantCulture));
                        }
                        else if (Int32.TryParse(trimmedarg, out _))
                        {
                            argumentsTypes.Add(typeof(Int32));
                            argumentsList.Add(System.Convert.ToInt32(trimmedarg, CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            if (throwOnError)
                                throw new Exception($"Unable to determine the type of argument ({trimmedarg}) in Report method");
                            else
                                return null;
                        }
                    }
                }


                string functionName = name.Substring(0, name.IndexOf('('));
                methodInfo = relativeToObject.GetType().GetMethod(functionName, argumentsTypes.ToArray<Type>());
                if (methodInfo == null && ignoreCase) // If not found, try using a case-insensitive search
                {
                    // try to get the method with identified arguments
                    BindingFlags bindingFlags = BindingFlags.Default | BindingFlags.IgnoreCase;
                    methodInfo = relativeToObject.GetType().GetMethod(functionName, argumentsTypes.Count(), bindingFlags, null, argumentsTypes.ToArray<Type>(), null);
                }
                if (methodInfo == null) // If not found, try searching without parameters in case they are optional and none were provided
                {
                    methodInfo = relativeToObject.GetType().GetMethod(functionName);
                    if (methodInfo != null) //if we found it, add missing parameters in for the optional ones missing
                    {
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        while (argumentsList.Count < parameters.Length)
                        {
                            argumentsTypes.Add(typeof(object));
                            argumentsList.Add(Type.Missing);
                        }
                    }
                }
            }
        }

        // Not a property or method, may be a child model.
        if (relativeToObject is INodeModel model)
        {
            StringComparison compareType = StringComparison.Ordinal;
            if (ignoreCase)
                compareType = StringComparison.OrdinalIgnoreCase;

            modelInfo = model.GetChildren().FirstOrDefault(m => m.Name.Equals(name, compareType));
        }

        if (methodInfo != null) //if we found a method, return it
        {
            return methodInfo;
        }
        else if (propertyInfo != null && modelInfo == null) //if only propertyInfo was found, return it
        {
            return propertyInfo;
        }
        else if (propertyInfo == null && modelInfo != null) //if only a child model was found, return it
        {
            return modelInfo;
        }
        else if (propertyInfo != null && modelInfo != null) //if a child model and a property were both found, we need to handle is
        {
            //if the property is a primitive type, but we have more names to dig through, return the child instead

            if ((propertyInfo.PropertyType.IsPrimitive && remainingNames > 0) && remainingNames > 0)
            {
                return modelInfo;
            }
            //This was put in place to stop Graph.Series returning the list of series instead of the commonly named child Series.
            else if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType) && modelInfo.GetType().Name == "Series")
            {
                return modelInfo;
            }
            //Special case for Nutrient which has a CNRF child and an array property. The check below isn't returning a value for the array
            else if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType) && propertyInfo.Name == "CNRF" && relativeToObject.GetType().Name.Equals("Nutrient"))
            {
                return propertyInfo;
            }
            else
            {
                //get the value of the property and see if it can be evaluated, if it can't return the child instead
                try
                {
                    //I hate this still throws when in debug mode, but it's the only way to actually check if the property is actually a getter
                    // that will throw if it's run.
                    object val = propertyInfo.GetValue(relativeToObject);
                    if (val != null)
                        return propertyInfo;
                    else
                        return modelInfo;
                }
                catch
                {
                    return modelInfo;
                }
            }
        }
        else
        {
            return null;
        }
    }
}
