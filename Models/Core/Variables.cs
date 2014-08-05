using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Models.Core
{
    /// <summary>
    /// This class is responsible for the retrieval of variables given a path.
    /// Path example syntax:
    ///    .Clock.Startday                     <- ABSOLUTE PATH
    ///    [PotatoSowingRule].Script.SowDate   <- RELATIVE TO A MODEL IN SCOPE
    ///    Leaf.LAI                            <- RELATIVE CHILD MODEL.
    /// </summary>
    public class Variables
    {
        /// <summary>
        /// A reference to the simulation holding the variable cache.
        /// </summary>
        private Simulation Simulation;

        /// <summary>
        /// The model that we are working for.
        /// </summary>
        private Model RelativeTo;

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        public Variables(Model relativeTo)
        {
            RelativeTo = relativeTo;
            Simulation = relativeTo.ParentOfType(typeof(Simulation)) as Simulation;
        }

        /// <summary>
        /// Return a model or variable using the specified NamePath. Returns null if not found.
        /// </summary>
        public object Get(string namePath)
        {
            IVariable variable = GetInternal(namePath);
            if (variable == null)
                return null;
            else
                return variable.Value;
        }

        /// <summary>
        /// Set the value of a variable. Will throw if variable doesn't exist.
        /// </summary>
        public void Set(string namePath, object value)
        {
            IVariable variable = GetInternal(namePath); 
            if (variable == null)
                throw new ApsimXException(RelativeTo.FullPath, "Cannot set the value of variable '" + namePath + "'. Variable doesn't exist");
            else
                variable.Value = value;
        }

        /// <summary>
        /// Return a variable using the specified NamePath. Returns null if not found.
        /// </summary>
        private IVariable GetInternal(string namePath)
        {
            // Look in cache first.
            string absolutePath = ToAbsolute(Simulation, namePath, RelativeTo);
            string cacheKey = null;
            cacheKey = absolutePath;
            if (Simulation != null)
            {
                IVariable returnVariable;
                if (Simulation.VariableCache.TryGetValue(cacheKey, out returnVariable))
                    return returnVariable;
            }
            IVariable variable;

            // Look for an expression.
            if (namePath.StartsWith("("))
            {
                variable = new VariableExpression(namePath, this);
            }
            else
            {
                // Not found in cache so go find variable.
                Model rootModel = GetRootModelOf(RelativeTo);

                // Make the absolute path relative to the simulation.
                if (!absolutePath.StartsWith(rootModel.FullPath))
                    throw new ApsimXException(RelativeTo.FullPath, "Invalid path found while trying to do a get. Path is '" + absolutePath + "'");
                string pathRelativeToSimulations = absolutePath.Remove(0, rootModel.FullPath.Length);

                object obj = rootModel;
                PropertyInfo propertyInfo = null;
                string arraySpecifier = null;
                    
                // Now look through all paths which are separated by a '.'
                string[] namePathBits = pathRelativeToSimulations.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string st in namePathBits)
                {
                    string pathBit = st;
                    if (propertyInfo != null)
                    {
                        obj = propertyInfo.GetValue(obj, null);
                        propertyInfo = null;
                    }

                    // look for an array specifier e.g. sw[2]
                    if (pathBit.Contains("["))
                    {
                        arraySpecifier = Utility.String.SplitOffBracketedValue(ref pathBit, '[', ']');
                    }

                    object localObj = null;
                    Model model = obj as Model;
                    if (model != null)
                        localObj = model.Children.All.FirstOrDefault(m => m.Name == pathBit);

                    if (localObj != null)
                        obj = localObj;
                    else
                    {
                        propertyInfo = obj.GetType().GetProperty(pathBit);
                        if (propertyInfo == null)
                        {
                            if (Simulation != null)
                                Simulation.VariableCache[cacheKey] = new VariableObject(null);
                            return null;
                        }
                    }
                }

                // Now we can create our return variable.
                if (propertyInfo == null)
                    variable = new VariableObject(obj);
                else
                    variable = new VariableProperty(obj, propertyInfo, arraySpecifier);
            }

            // Add to our cache.
            if (Simulation != null)
                Simulation.VariableCache[cacheKey] = variable;
            return variable;
        }

        /// <summary>
        /// Return the specified 'namePath' as an absolute one.
        /// </summary>
        private static string ToAbsolute(Simulation simulation, string namePath, Model relativeTo)
        {
            if (namePath.Length == 0)
                return namePath;

            if (namePath[0] == '.')
            {
                // Absolute path
                return namePath;
            }
            else if (namePath[0] == '[')
            {
                // namePath has a [type] at its beginning.
                int pos = namePath.IndexOf(']');
                if (pos == -1)
                    throw new ApsimXException(relativeTo.FullPath, "Invalid path found: " + namePath);

                // Do a model name search first.
                string typeName = namePath.Substring(1, pos - 1);
                Model modelInScope = relativeTo.Scope.Find(typeName);

                // If none found then assume typeName is a type and do a type search.
                if (modelInScope == null)
                {
                    Type t = Utility.Reflection.GetTypeFromUnqualifiedName(typeName);
                    if (t != null)
                    {
                        modelInScope = relativeTo.Scope.Find(t);
                    }
                }

                if (modelInScope == null)
                    throw new ApsimXException("Simulation.Variables", "Cannot find type: " + typeName + " while doing a get for: " + namePath);

                return modelInScope.FullPath + namePath.Substring(pos + 1);
            }
            else
                return relativeTo.FullPath + "." + namePath;
        }

        /// <summary>
        /// Locate the parent simulation Returns null if not found.
        /// </summary>
        private static Model GetRootModelOf(Model relativeTo)
        {
            while (relativeTo != null && relativeTo.Parent != null)
                relativeTo = relativeTo.Parent;

            if (relativeTo == null)
                throw new ApsimXException(relativeTo.FullPath, "Cannot find root model.");
            return relativeTo;
        }
    }
}
