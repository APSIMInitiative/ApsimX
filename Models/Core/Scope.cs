using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Models.Core
{
    /// <summary>
    /// This class provides the scoping rules for APSIMx. If a model is sitting under a
    /// simulation then scope lookups will be cached for runtime speed efficiency.
    /// </summary>
    public class Scope
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
        public Scope(Model relativeTo)
        {
            RelativeTo = relativeTo;
            Simulation = relativeTo.ParentOfType(typeof(Simulation)) as Simulation;
        }

        /// <summary>
        /// Clear the scope cache if 'relativeTo' is sitting under a simulation.
        /// </summary>
        public void ClearCache()
        {
            Simulation.ScopeCache.Clear();
        }

        /// <summary>
        /// Return a model with the specified name is in scope. Returns null if none found.
        /// </summary>
        public Model Find(string modelNameToFind)
        {
            // If it is in the cache then return it.
            string cacheKey = null;
            cacheKey = GetCacheKey(Simulation, RelativeTo, modelNameToFind, singleMatch: true);
            if (cacheKey != null)
            {
                Model[] matches;
                if (Simulation.ScopeCache.TryGetValue(cacheKey, out matches))
                    return matches[0];
            }

            // Not in cache so look for it and add to cache.
            List<Model> modelsInScope = new List<Model>();
            Walk(RelativeTo, new Comparer(modelNameToFind), true, modelsInScope, null);

            if (modelsInScope.Count >= 1)
            {
                if (Simulation != null)
                    AddToCache(Simulation, cacheKey, new Model[1] { modelsInScope[0] });
                return modelsInScope[0];
            }
            return null;
        }


        /// <summary>
        /// Return a model of the specified type that is in scope. Returns null if none found.
        /// </summary>
        public Model Find(Type modelType)
        {
            // If it is in the cache then return it.
            string cacheKey = null;
            cacheKey = GetCacheKey(Simulation, RelativeTo, modelType.Name, singleMatch: true);
            if (cacheKey != null)
            {
                Model[] matches;
                if (Simulation.ScopeCache.TryGetValue(cacheKey, out matches))
                    return matches[0];
            }

            // Not in cache so look for it and add to cache.
            List<Model> modelsInScope = new List<Model>();
            Walk(RelativeTo, new Comparer(modelType), true, modelsInScope, null);

            if (modelsInScope.Count >= 1)
            {
                if (Simulation != null)
                    AddToCache(Simulation, cacheKey, modelsInScope.ToArray());
                return modelsInScope[0];
            }
            return null;
        }

        /// <summary>
        /// Return a list of all models in scope. If a Type is specified then only those models
        /// of that type will be returned. Never returns null. May return an empty array. Does not
        /// return models outside of a simulation.
        /// </summary>
        public Model[] FindAll(Type modelType = null)
        {
            // If it is in the cache then return it.
            string cacheKey = null;
            if (modelType == null)
                cacheKey = GetCacheKey(Simulation, RelativeTo, null, singleMatch: false);
            else
                cacheKey = GetCacheKey(Simulation, RelativeTo, modelType.Name, singleMatch: false);
            if (cacheKey != null && Simulation.ScopeCache.ContainsKey(cacheKey))
                return Simulation.ScopeCache[cacheKey];

            // Not in cache so look for matching models.
            List<Model> modelsInScope = new List<Model>();
            Walk(RelativeTo, new Comparer(modelType), false, modelsInScope, null);

            // Add matching models to cache and return.
            if (modelsInScope.Count >= 1 && Simulation != null)
            {
                AddToCache(Simulation, cacheKey, modelsInScope.ToArray());
            }
            return modelsInScope.ToArray();
        }

        /// <summary>
        /// Return a unique cache key or null if cache shouldn't be used.
        /// </summary>
        private static string GetCacheKey(Simulation simulation, Model relativeTo, string modelName, bool singleMatch)
        {
            // If this is a simulation variable then try and use the cache.
            bool useCache = simulation != null;
 
            if (!useCache) 
                return null;

            if (singleMatch)
                if (modelName != null)
                    return relativeTo.FullPath + modelName;
                else
                    return relativeTo.FullPath;
            else
                if (modelName != null)
                    return relativeTo.FullPath + "|MANY|" + modelName;
                else
                    return relativeTo.FullPath + "|MANY";
        }

        /// <summary>
        /// Add the specified list of models to the cache.
        /// </summary>
        private static void AddToCache(Simulation simulation, string cacheKey, Model[] models)
        {

            if (simulation != null && cacheKey != null)
            {
                if (simulation.ScopeCache.ContainsKey(cacheKey))
                    simulation.ScopeCache[cacheKey] = models;
                else
                    simulation.ScopeCache.Add(cacheKey, models);
            }
        }

        /// <summary>
        /// A comparer clas that is used during the model walk.
        /// </summary>
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
        private static void Walk(Model relativeTo, Comparer comparer, bool firstOnly, List<Model> matches, Model excludeChild)
        {
            if (comparer.DoesMatch(relativeTo) && !matches.Contains(relativeTo))
                matches.Add(relativeTo);

            WalkChildren(relativeTo, comparer, firstOnly, matches, excludeChild);
         
            bool haveFinished = firstOnly && matches.Count > 0;
            if (!haveFinished && relativeTo.Parent != null)
            {
                if (relativeTo is Simulation || relativeTo is Factorial.Experiment)
                {
                    // Don't go beyond simulation.
                }

                else
                {
                    // Walk the parent but not all child models recursively.
                    Model parent = relativeTo.Parent;
                    while (parent != null)
                    {
                        WalkParent(parent, comparer, firstOnly, matches, relativeTo);
                        if (parent is Simulation)
                            break;
                        else if (parent is Models.Factorial.Experiment)
                            Walk(parent, comparer, firstOnly, matches, relativeTo);
                        parent = parent.Parent;
                    }
                }
            }
        }

        /// <summary>
        /// Walk the children of 'relativeTo' recursively
        /// </summary>
        private static void WalkChildren(Model relativeTo, Comparer comparer, bool firstOnly, List<Model> matches, Model excludeChild)
        {
            foreach (Model child in relativeTo.Children.All)
            {
                if (excludeChild == null || child != excludeChild)
                {
                    if (comparer.DoesMatch(child) && !matches.Contains(child))
                        matches.Add(child);
                    WalkChildren(child, comparer, firstOnly, matches, null);
                }
                if (firstOnly && matches.Count > 0)
                    return;
            }
        }

        /// <summary>
        /// Walk the specified modelCollection but don't recursively walk down the child hierarchy.
        /// </summary>
        private static void WalkParent(Model relativeTo, Comparer comparer, bool firstOnly, List<Model> matches, Model excludeChild)
        {
            foreach (Model child in relativeTo.Models)
            {
                if (excludeChild == null || child != excludeChild)
                {
                    if (comparer.DoesMatch(child) && !matches.Contains(child))
                        matches.Add(child);
                }
                if (firstOnly && matches.Count > 0)
                    return;
            }

            if (comparer.DoesMatch(relativeTo) && !matches.Contains(relativeTo))
                matches.Add(relativeTo);
        }

    }
}
