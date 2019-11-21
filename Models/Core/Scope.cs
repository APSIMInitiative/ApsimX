namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;

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

            // The algorithm is to find the parent scoped model of the specified model.
            // Then return all descendants of the scoped model and then recursively
            // the direct children of the parents of the scoped model. For any direct
            // child of the parents of the scoped model, we also return its descendants
            // if it is not a scoped model.

            IModel scopedParent = FindScopedParentModel(relativeTo);
            if (scopedParent == null)
                throw new Exception("No scoping model found relative to: " + Apsim.FullPath(relativeTo));

            // Return all models in zone and all direct children of zones parent.
            modelsInScope = new List<IModel>();
            modelsInScope.Add(scopedParent);
            modelsInScope.AddRange(Apsim.ChildrenRecursively(scopedParent));
            while (scopedParent.Parent != null)
            {
                scopedParent = scopedParent.Parent;
                modelsInScope.Add(scopedParent);
                foreach (IModel child in scopedParent.Children)
                {
                    if (!modelsInScope.Contains(child))
                    {
                        modelsInScope.Add(child);

                        // Return the child's descendants if it is not a scoped model.
                        // This ensures that a soil's water node will be in scope of
                        // a manager inside a folder inside a zone.
                        if (!IsScopedModel(child))
                            modelsInScope.AddRange(Apsim.ChildrenRecursively(child));
                    }
                }
            }

            if (!modelsInScope.Contains(scopedParent))
                modelsInScope.Add(scopedParent); // top level simulation

            // add to cache for next time.
            cache.Add(relativeToFullPath, modelsInScope);
            return modelsInScope.ToArray();
        }

        /// <summary>
        /// Find a parent of 'relativeTo' that has a [ScopedModel] attribute. 
        /// Returns null if non found.
        /// </summary>
        /// <param name="relativeTo">The model to use as a base.</param>
        public static IModel FindScopedParentModel(IModel relativeTo)
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
        /// Returns true iff model x is in scope of model y.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public bool InScopeOf(IModel x, IModel y)
        {
            return FindAll(y).Contains(x);
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
