using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Models.Core
{
    /// <summary>
    /// This class provides the scoping rules for APSIMx
    /// </summary>
    public class Scope
    {
        /// <summary>
        /// Cache to speed up scope lookups.
        /// </summary>
        [NonSerialized]
        Dictionary<string, Model[]> Cache = new Dictionary<string, Model[]>();

        /// <summary>
        /// Clear the scope cache.
        /// </summary>
        public void ClearCache()
        {
            Cache.Clear();
        }

        /// <summary>
        /// Return a model with the specified name is in scope. Returns null if none found.
        /// </summary>
        public Model Find(Model relativeTo, string modelNameToFind)
        {
            // If it is in the cache then return it.
            string cacheKey = "ONE:" + relativeTo.FullPath + ":" + modelNameToFind;
            if (Cache.ContainsKey(cacheKey))
                return Cache[cacheKey][0];

            // Not in cache so look for it and add to cache.
            List<Model> modelsInScope = new List<Model>();
            Walk(relativeTo, new Comparer(modelNameToFind), true, modelsInScope, null);

            foreach (Model modelInScope in modelsInScope)
            {
                Cache.Add(cacheKey, new Model[1] { modelInScope });
                return modelInScope;
            }
            return null;
        }

        /// <summary>
        /// Return a model of the specified type that is in scope. Returns null if none found.
        /// </summary>
        public Model Find(Model relativeTo, Type modelType)
        {
            // If it is in the cache then return it.
            string cacheKey = "ONE:" + relativeTo.FullPath + ":" + modelType.Name;
            if (Cache.ContainsKey(cacheKey))
                return Cache[cacheKey][0];
            
            // Not in cache so look for it and add to cache.
            List<Model> modelsInScope = new List<Model>();
            Walk(relativeTo, new Comparer(modelType), true, modelsInScope, null);

            if (modelsInScope.Count >= 1)
                return modelsInScope[0];
            return null;
        }

        /// <summary>
        /// Return a list of all models in scope. If a Type is specified then only those models
        /// of that type will be returned. Never returns null. May return an empty array. Does not
        /// return models outside of a simulation.
        /// </summary>
        public Model[] FindAll(Model relativeTo, Type modelType = null)
        {
            // If it is in the cache then return it.
            string cacheKey = relativeTo.FullPath;
            if (modelType != null)
                cacheKey += ":" + modelType.Name;
            if (Cache.ContainsKey(cacheKey))
                return Cache[cacheKey];

            // Not in cache so look for matching models.
            List<Model> modelsInScope = new List<Model>();
            Walk(relativeTo, new Comparer(modelType), false, modelsInScope, null);

            // Add matching models to cache and return.
            if (modelsInScope.Count >= 1)
                Cache.Add(cacheKey, modelsInScope.ToArray());

            return modelsInScope.ToArray();
        }


        private class Comparer
        {
            private Type TypeToMatch = null;
            private string NameToMatch = null;

            public Comparer(Type type) { TypeToMatch = type; }
            public Comparer(string name) { NameToMatch = name; }

            public bool DoesMatch(Model model)
            {
                if (NameToMatch != null)
                    return model.Name == NameToMatch;
                else if (TypeToMatch != null)
                    return TypeToMatch.IsAssignableFrom(model.GetType());
                else
                    return true;                    
            }
        }

        /// <summary>
        /// This is the main scope walking method. Looks at children first then children of parent models etc
        /// until a Zone is reached. Once a zone is reached then it won't recursively go looking
        /// at child models.
        /// </summary>
        private void Walk(Model relativeTo, Comparer comparer, bool firstOnly, List<Model> matches, Model excludeChild)
        {
            if (comparer.DoesMatch(relativeTo))
                matches.Add(relativeTo);

            if (relativeTo is ModelCollection)
            {
                WalkChildren(relativeTo, comparer, firstOnly, matches, excludeChild);
            }
         
            bool haveFinished = firstOnly && matches.Count > 0;
            if (!haveFinished && relativeTo.Parent != null)
            {
                if (relativeTo is Simulation)
                {
                    // Don't go beyond simulation.
                }

                else if (relativeTo is Zone)
                {
                    // Walk the parent but not all child models recursively.
                    ModelCollection parent = relativeTo.Parent;
                    while (parent != null && !(parent is Simulations))
                    {
                        WalkParent(parent, comparer, firstOnly, matches, relativeTo);
                        parent = parent.Parent;
                    }
                }
                else
                {
                    // When walking the parent, tell it to exclude 'relativeTo' as we've already done it.
                    Walk(relativeTo.Parent, comparer, firstOnly, matches, relativeTo);
                }
            }
        }

        private void WalkChildren(Model relativeTo, Comparer comparer, bool firstOnly, List<Model> matches, Model excludeChild)
        {
            foreach (Model child in (relativeTo as ModelCollection).Models)
            {
                if (excludeChild == null || child != excludeChild)
                {
                    if (comparer.DoesMatch(child))
                        matches.Add(child);

                    if (child is ModelCollection)
                        WalkChildren(child, comparer, firstOnly, matches, null);
                }
                if (firstOnly && matches.Count > 0)
                    return;
            }
        }

        /// <summary>
        /// Walk the specified modelCollection but don't recursively walk down the child hierarchy.
        /// </summary>
        private void WalkParent(ModelCollection relativeTo, Comparer comparer, bool firstOnly, List<Model> matches, Model excludeChild)
        {
            foreach (Model child in relativeTo.Models)
            {
                if (excludeChild == null || child != excludeChild)
                {
                    if (comparer.DoesMatch(child))
                        matches.Add(child);
                }
                if (firstOnly && matches.Count > 0)
                    return;
            }

            if (comparer.DoesMatch(relativeTo))
                matches.Add(relativeTo);
        }



        /// <summary>
        /// Find the parent zone and return it. Will throw if not found.
        /// </summary>
        private Zone ParentZone(Model relativeTo)
        {
            // Go looking for a zone or Simulation.
            ModelCollection parentZone = relativeTo.Parent;
            while (parentZone != null && !typeof(Zone).IsAssignableFrom(parentZone.GetType()))
                parentZone = parentZone.Parent;
            if (parentZone == null || !(parentZone is Zone))
                throw new ApsimXException(relativeTo.FullPath, "Cannot find a parent zone for model '" + relativeTo.FullPath + "'");
            return parentZone as Zone;
        }

    }
}
