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
        [NonSerialized]
        private Dictionary<string, Utility.IVariable> VariableCache = new Dictionary<string, Utility.IVariable>();

        /// <summary>
        /// Clear the variable cache.
        /// </summary>
        public void ClearCache()
        {
            VariableCache.Clear();
        }


        /// <summary>
        /// Return a variable using the specified NamePath. Returns null if not found.
        /// </summary>
        public Utility.IVariable Get(Model relativeTo, string namePath)
        {
            // Look in cache first.
            string cacheKey = GetSimulationName(relativeTo) + namePath;

            if (VariableCache.ContainsKey(cacheKey))
                return VariableCache[cacheKey];

            string absolutePath = ToAbsolute(namePath, relativeTo);

            Model rootModel = GetRootModelOf(relativeTo);

            // Make the absolute path relative to the simulation.
            if (!absolutePath.StartsWith(rootModel.FullPath))
                throw new ApsimXException(relativeTo.FullPath, "Invalid path found while trying to do a get. Path is '" + absolutePath + "'");
            string pathRelativeToSimulations = absolutePath.Remove(0, rootModel.FullPath.Length);

            object obj = rootModel;
            PropertyInfo propertyInfo = null;

            // Now look through all paths which are separated by a '.'
            string[] namePathBits = pathRelativeToSimulations.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string pathBit in namePathBits)
            {
                if (propertyInfo != null)
                {
                    obj = propertyInfo.GetValue(obj, null);
                    propertyInfo = null;
                }

                object localObj = null;
                ModelCollection modelCollecion = obj as ModelCollection;
                if (modelCollecion != null)
                    localObj = modelCollecion.Models.FirstOrDefault(m => m.Name == pathBit);

                if (localObj != null)
                    obj = localObj;
                else
                {
                    propertyInfo = obj.GetType().GetProperty(pathBit);
                    if (propertyInfo == null)
                        return null;
                }
            }

            // Now we can create our return variable.
            Utility.IVariable variable;
            if (propertyInfo == null)
                variable = new Utility.VariableObject(obj);
            else
                variable = new Utility.VariableProperty(obj, propertyInfo);

            // Add to our cache.
            VariableCache[cacheKey] = variable;
            return variable;
        }



        /// <summary>
        /// Return the specified 'namePath' as an absolute one.
        /// </summary>
        private string ToAbsolute(string namePath, Model relativeTo)
        {
            if (namePath.StartsWith("."))
            {
                // Absolute path
                return namePath;
            }
            else if (namePath.StartsWith("[", StringComparison.CurrentCulture) && namePath.Contains(']'))
            {
                // namePath has a [type] at its beginning.
                int pos = namePath.IndexOf("]", StringComparison.CurrentCulture);
                string typeName = namePath.Substring(1, pos - 1);
                Type t = Utility.Reflection.GetTypeFromUnqualifiedName(typeName);
                Model modelInScope;
                if (t == null)
                    modelInScope = Model.Scope.Find(relativeTo, typeName);
                else
                    modelInScope = Model.Scope.Find(relativeTo, t);

                if (modelInScope == null)
                    throw new ApsimXException("Simulation.Variables", "Cannot find type: " + typeName + " while doing a get for: " + namePath);

                return modelInScope.FullPath + namePath.Substring(pos + 1);
            }
            else
                return relativeTo.FullPath + "." + namePath;
        }


        /// <summary>
        /// Locate the parent with the specified type. Returns null if not found.
        /// </summary>
        private string GetSimulationName(Model relativeTo)
        {
            Model m = relativeTo;
            while (m != null && m.Parent != null && !(relativeTo is Simulation))
                m = m.Parent;

            if (m == null || !(m is Simulation))
                return "";
            else
                return m.FullPath;
        }


        /// <summary>
        /// Locate the parent simulation Returns null if not found.
        /// </summary>
        protected Model GetRootModelOf(Model relativeTo)
        {
            while (relativeTo != null && relativeTo.Parent != null)
                relativeTo = relativeTo.Parent;

            if (relativeTo == null)
                throw new ApsimXException(relativeTo.FullPath, "Cannot find root model.");
            return relativeTo;
        }
    }
}
