// -----------------------------------------------------------------------
// <copyright file="Scope.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Implements APSIMs scoping rules.
    /// </summary>
    public class ScopingRules
    {
        private Dictionary<string, List<IModel>> cache = new Dictionary<string, List<IModel>>();

        /// <summary>
        /// Return a list of models in scope to the one specified.
        /// </summary>
        /// <param name="relativeTo">The model to base scoping rules on</param>
        public IModel[] FindAll(IModel relativeTo)
        {
            string relativeToFullPath = Apsim.FullPath(relativeTo);
            // Try the cache first.
            List<IModel> modelsInScope;
            if (cache.TryGetValue(relativeToFullPath, out modelsInScope))
                return modelsInScope.ToArray();

            // The algorithm is to find the parent Zone of the specified model.
            // Then return all children of this zone recursively and then recursively 
            // the direct children of the parents of the zone.
            IModel parentZone = FindScopedParentModel(relativeTo);
            if (parentZone == null)
                throw new Exception("No scoping model found relative to: " + Apsim.FullPath(relativeTo));

            // return all models in zone and all direct children of zones parent.
            modelsInScope = new List<IModel>();
            modelsInScope.Add(parentZone);
            modelsInScope.AddRange(Apsim.ChildrenRecursively(parentZone));
            while (parentZone.Parent != null)
            {
                parentZone = parentZone.Parent;
                modelsInScope.Add(parentZone);
                foreach (IModel child in parentZone.Children)
                {
                    if (!modelsInScope.Contains(child))
                    {
                        modelsInScope.Add(child);
                        if (!IsScopedModel(child))
                            modelsInScope.AddRange(Apsim.ChildrenRecursively(child));
                    }
                }
            }

            if (!modelsInScope.Contains(parentZone))
                modelsInScope.Add(parentZone); // top level simulation

            // add to cache for next time.
            cache.Add(relativeToFullPath, modelsInScope);
            return modelsInScope.ToArray();
        }

        /// <summary>
        /// Find a parent of 'relativeTo' that has a [ScopedModel] attribute. 
        /// Returns null if non found.
        /// </summary>
        /// <param name="relativeTo">The model to use as a base.</param>
        private static IModel FindScopedParentModel(IModel relativeTo)
        {
            do
            {
                if (IsScopedModel(relativeTo))
                    return relativeTo;
                if (relativeTo.Parent == null)
                    return relativeTo;
                relativeTo = relativeTo.Parent;
            }
            while (relativeTo != null);

            return null;
        }

        /// <summary>
        /// Return true if model is a scoped model
        /// </summary>
        /// <param name="relativeTo"></param>
        /// <returns></returns>
        public static bool IsScopedModel(IModel relativeTo)
        {
            return relativeTo.GetType().GetCustomAttribute(typeof(ScopedModelAttribute), true) as ScopedModelAttribute != null;
        }

        /// <summary>
        /// Clear the current cache
        /// </summary>
        public void Clear()
        {
            this.cache.Clear();
        }
    }
}
