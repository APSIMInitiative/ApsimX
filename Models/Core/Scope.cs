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
        /// Clear the scope cache if 'relativeTo' is sitting under a simulation.
        /// </summary>
        public static void ClearCache(Model relativeTo)
        {
            Simulation simulation = GetSimulation(relativeTo);
            if (simulation != null)
                simulation.ScopeCache.Clear();
        }

        /// <summary>
        /// Return a model with the specified name is in scope. Returns null if none found.
        /// </summary>
        public static Model Find(Model relativeTo, string modelNameToFind)
        {
            // Get the simulation
            Simulation simulation = GetSimulation(relativeTo);

            // If it is in the cache then return it.
            string cacheKey = null;
            if (simulation != null)
            {
                cacheKey = GetCacheKey(relativeTo, modelNameToFind, singleMatch: true);
                if (cacheKey != null && simulation.ScopeCache.ContainsKey(cacheKey))
                    return simulation.ScopeCache[cacheKey][0];
            }

            // Not in cache so look for it and add to cache.
            List<Model> modelsInScope = new List<Model>();
            Walk(relativeTo, new Comparer(modelNameToFind), true, modelsInScope, null);

            if (modelsInScope.Count >= 1)
            {
                if (simulation != null)
                    AddToCache(simulation, cacheKey, new Model[1] { modelsInScope[0] });
                return modelsInScope[0];
            }
            return null;
        }


        /// <summary>
        /// Return a model of the specified type that is in scope. Returns null if none found.
        /// </summary>
        public static Model Find(Model relativeTo, Type modelType)
        {
            // Get the simulation
            Simulation simulation = GetSimulation(relativeTo);

            // If it is in the cache then return it.
            string cacheKey = null;
            if (simulation != null)
            {
                cacheKey = GetCacheKey(relativeTo, modelType.Name, singleMatch: true);
                if (cacheKey != null && simulation.ScopeCache.ContainsKey(cacheKey))
                    return simulation.ScopeCache[cacheKey][0];
            }

            // Not in cache so look for it and add to cache.
            List<Model> modelsInScope = new List<Model>();
            Walk(relativeTo, new Comparer(modelType), true, modelsInScope, null);

            if (modelsInScope.Count >= 1)
            {
                if (simulation != null) 
                    AddToCache(simulation, cacheKey, modelsInScope.ToArray());
                return modelsInScope[0];
            }
            return null;
        }

        /// <summary>
        /// Return a list of all models in scope. If a Type is specified then only those models
        /// of that type will be returned. Never returns null. May return an empty array. Does not
        /// return models outside of a simulation.
        /// </summary>
        public static Model[] FindAll(Model relativeTo, Type modelType = null)
        {
            // Get the simulation
            Simulation simulation = GetSimulation(relativeTo);

            // If it is in the cache then return it.
            string cacheKey = null;
            if (simulation != null)
            {
                if (modelType == null)
                    cacheKey = GetCacheKey(relativeTo, null, singleMatch: false);
                else
                    cacheKey = GetCacheKey(relativeTo, modelType.Name, singleMatch: false);
                if (cacheKey != null && simulation.ScopeCache.ContainsKey(cacheKey))
                    return simulation.ScopeCache[cacheKey];
            }
            // Not in cache so look for matching models.
            List<Model> modelsInScope = new List<Model>();
            Walk(relativeTo, new Comparer(modelType), false, modelsInScope, null);

            // Add matching models to cache and return.
            if (modelsInScope.Count >= 1 && simulation != null)
            {
                AddToCache(simulation, cacheKey, modelsInScope.ToArray());
            }
            return modelsInScope.ToArray();
        }

        /// <summary>
        /// Return a unique cache key or null if cache shouldn't be used.
        /// </summary>
        private static string GetCacheKey(Model relativeTo, string modelName, bool singleMatch)
        {
            // Look in cache first.
            Simulation simulation = GetSimulation(relativeTo);

            // If this is a simulation variable then try and use the cache.
            bool useCache = simulation != null;
 
            if (!useCache) 
                return null;

            string cacheKey = null;
            cacheKey = relativeTo.FullPath;
            if (singleMatch)
                cacheKey += "|SINGLE";
            else
                cacheKey += "|MANY";
            if (modelName != null)
                cacheKey += "|" + modelName;
            return cacheKey;
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

            if (relativeTo is ModelCollection)
            {
                WalkChildren(relativeTo, comparer, firstOnly, matches, excludeChild);
            }
         
            bool haveFinished = firstOnly && matches.Count > 0;
            if (!haveFinished && relativeTo.Parent != null)
            {
                if (relativeTo is Simulation || relativeTo is Factorial.Experiment)
                {
                    // Don't go beyond simulation.
                }

                else// if (relativeTo is Zone)
                {
                    // Walk the parent but not all child models recursively.
                    ModelCollection parent = relativeTo.Parent;
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
                //else
                //{
                //    // When walking the parent, tell it to exclude 'relativeTo' as we've already done it.
                //    Walk(relativeTo.Parent, comparer, firstOnly, matches, relativeTo);
                //}
            }
        }

        private static void WalkChildren(Model relativeTo, Comparer comparer, bool firstOnly, List<Model> matches, Model excludeChild)
        {
            foreach (Model child in (relativeTo as ModelCollection).Models)
            {
                if (excludeChild == null || child != excludeChild)
                {
                    if (comparer.DoesMatch(child) && !matches.Contains(child))
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
        private static void WalkParent(ModelCollection relativeTo, Comparer comparer, bool firstOnly, List<Model> matches, Model excludeChild)
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
    }
}
