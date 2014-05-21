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
        /// Clear the variable cache.
        /// </summary>
        public static void ClearCache(Model relativeTo)
        {
            Simulation simulation = GetSimulation(relativeTo);
            if (simulation != null)
                simulation.VariableCache.Clear();
        }


        /// <summary>
        /// Return a variable using the specified NamePath. Returns null if not found.
        /// </summary>
        public static IVariable Get(Model relativeTo, string namePath)
        {
            // Look in cache first.
            Simulation simulation = GetSimulation(relativeTo);

            // If this is a simulation variable then try and use the cache.
            bool useCache = simulation != null;

            string absolutePath = ToAbsolute(namePath, relativeTo);
            string cacheKey = null;
            if (useCache)
            {
                cacheKey = absolutePath;
                if (simulation.VariableCache.ContainsKey(cacheKey))
                    return simulation.VariableCache[cacheKey];
            }

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
            IVariable variable;
            if (propertyInfo == null)
                variable = new VariableObject(obj);
            else
                variable = new VariableProperty(obj, propertyInfo);

            // Add to our cache.
            if (useCache)
                simulation.VariableCache[cacheKey] = variable;
            return variable;
        }

    

        /// <summary>
        /// Return the specified 'namePath' as an absolute one.
        /// </summary>
        private static string ToAbsolute(string namePath, Model relativeTo)
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

                string typeName = namePath.Substring(1, pos - 1);
                Model modelInScope = Scope.Find(relativeTo, typeName);

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
        private static Simulation GetSimulation(Model relativeTo)
        {
            Model m = relativeTo;
            while (m != null && m.Parent != null && !(m is Simulation))
                m = m.Parent;

            if (m == null || !(m is Simulation))
                return null;
            else
                return m as Simulation;
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
