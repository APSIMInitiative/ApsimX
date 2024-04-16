using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Models.Factorial;

namespace Models.Core
{

    /// <summary>
    /// Implements APSIMs scoping rules.
    /// </summary>
    public class ScopingRules
    {
        private Dictionary<IModel, List<IModel>> cache = new Dictionary<IModel, List<IModel>>();

        /// <summary>
        /// Return a list of models in scope to the one specified.
        /// </summary>
        /// <param name="relativeTo">The model to base scoping rules on</param>
        public IModel[] FindAll(IModel relativeTo)
        {
            IModel scopedParent = FindScopedParentModel(relativeTo);
            if (scopedParent == null)
                throw new Exception("No scoping model found relative to: " + relativeTo.FullPath);

            // Try the cache first.
            List<IModel> modelsInScope;
            if (cache.TryGetValue(scopedParent, out modelsInScope))
                return modelsInScope.ToArray();

            // The algorithm is to find the parent scoped model of the specified model.
            // Then return all descendants of the scoped model and then recursively
            // the direct children of the parents of the scoped model. For any direct
            // child of the parents of the scoped model, we also return its descendants
            // if it is not a scoped model.

            // Return all models in zone and all direct children of zones parent.
            modelsInScope = new List<IModel>();
            modelsInScope.Add(scopedParent);
            modelsInScope.AddRange(scopedParent.FindAllDescendants());
            IModel m = scopedParent;
            while (m.Parent != null)
            {
                m = m.Parent;
                modelsInScope.Add(m);
                foreach (IModel child in m.Children)
                {
                    if (!modelsInScope.Contains(child))
                    {
                        modelsInScope.Add(child);

                        // Return the child's descendants if it is not a scoped model.
                        // This ensures that a soil's water node will be in scope of
                        // a manager inside a folder inside a zone.
                        if (!IsScopedModel(child))
                            modelsInScope.AddRange(child.FindAllDescendants());
                    }
                }
            }

            if (!modelsInScope.Contains(m))
                modelsInScope.Add(m); // top level simulation

            //scope may not work for models under experiment (that need to link back to the actual sim)
            //so first we find models that are in scope (aka, also under the factor), then also return
            //the descendants of the simulation
            Experiment exp = relativeTo.FindAncestor<Experiment>();
            if (exp != null)
            {
                Simulation sim = exp.FindChild<Simulation>();

                IEnumerable<IModel> descendants = sim.FindAllDescendants();
                foreach (IModel result in descendants)
                    modelsInScope.Add(result);
            }

            // add to cache for next time.
            cache.Add(scopedParent, modelsInScope);
            return modelsInScope.ToArray();
        }

        /*
        /// <summary>
        /// Return a list of models in scope to the one specified.
        /// </summary>
        /// <param name="relativeTo">The model to base scoping rules on</param>
        public IEnumerable<IModel> FindAll(IModel relativeTo)
        {
            string relativeToFullPath = relativeTo.FullPath;
            // Try the cache first.
            List<IModel> modelsInScope;
            if (cache.TryGetValue(relativeToFullPath, out modelsInScope))
                foreach (IModel result in modelsInScope)
                    yield return result;

            // The algorithm is to find the parent scoped model of the specified model.
            // Then return all descendants of the scoped model and then recursively
            // the direct children of the parents of the scoped model. For any direct
            // child of the parents of the scoped model, we also return its descendants
            // if it is not a scoped model.

            IModel scopedParent = relativeTo.Ancestors().FirstOrDefault(a => IsScopedModel(a));
            if (scopedParent == null)
                throw new Exception("No scoping model found relative to: " + relativeTo.FullPath);

            // Return all models in zone and all direct children of zones parent.
            modelsInScope = new List<IModel>();

            modelsInScope.Add(scopedParent);
            yield return scopedParent;

            foreach (IModel descendant in scopedParent.Descendants())
            {
                modelsInScope.Add(descendant);
                yield return descendant;
            }

            foreach (IModel ancestor in scopedParent.Ancestors())
            {
                modelsInScope.Add(ancestor);
                yield return ancestor;

                foreach (IModel cousin in ancestor.Children)
                {
                    if (!modelsInScope.Contains(cousin))
                    {
                        modelsInScope.Add(cousin);
                        yield return cousin;

                        if (!IsScopedModel(cousin))
                        {
                            foreach (IModel descendant in cousin.Descendants())
                            {
                                modelsInScope.Add(descendant);
                                yield return descendant;
                            }
                        }
                    }
                }
            }

            // add to cache for next time.
            cache.Add(relativeToFullPath, modelsInScope);
        }
        */

        /// <summary>
        /// Find a parent of 'relativeTo' that has a [ScopedModel] attribute. 
        /// Returns null if non found.
        /// </summary>
        /// <param name="relativeTo">The model to use as a base.</param>
        public static IModel FindScopedParentModel(IModel relativeTo)
        {
            //if (IsScopedModel(relativeTo))
            //    return relativeTo;
            //
            //return relativeTo.Ancestors().FirstOrDefault(a => IsScopedModel(a));

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
