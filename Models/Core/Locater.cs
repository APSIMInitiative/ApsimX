// -----------------------------------------------------------------------
// <copyright file="Locater.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// This class is responsible for the location and retrieval of variables or models 
    /// given a path.
    /// Path example syntax:
    ///    .Clock.Start                         ABSOLUTE PATH
    ///    [PotatoSowingRule].Script.SowDate    RELATIVE TO A MODEL IN SCOPE
    ///    Leaf.LAI                             RELATIVE CHILD MODEL.
    /// </summary>
    public class Locater
    {
        /// <summary>
        /// A cache for speeding up look ups. The object can be either 
        /// Model[] or an IVariable.
        /// </summary>
        private Dictionary<string, object> cache = new Dictionary<string, object>();

        /// <summary>
        /// Clear the cache
        /// </summary>
        public void Clear()
        {
            this.cache.Clear();
        }

        /// <summary>
        /// Get the value of a variable or model.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <returns>The found object or null if not found</returns>
        public object Get(string namePath, Model relativeTo)
        {
            IVariable variable = this.GetInternal(namePath, relativeTo);
            if (variable == null)
            {
                return variable;
            }
            else
            {
                return variable.Value;
            }
        }

        /// <summary>
        /// Set the value of a variable. Will throw if variable doesn't exist.
        /// </summary>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <param name="value">The value to set the property to</param>
        public void Set(string namePath, Model relativeTo, object value)
        {
            IVariable variable = this.GetInternal(namePath, relativeTo);
            if (variable == null)
            {
                throw new ApsimXException(relativeTo.FullPath, "Cannot set the value of variable '" + namePath + "'. Variable doesn't exist");
            }
            else
            {
                variable.Value = value;
            }
        }

        /// <summary>
        /// Get the value of a variable or model.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <returns>The found object or null if not found</returns>
        public IVariable GetInternal(string namePath, Model relativeTo)
        {
            // Look in cache first.
            string cacheKey = relativeTo.FullPath + "|" + namePath;
            object value;
            if (this.cache.TryGetValue(cacheKey, out value))
            {
                return value as IVariable;
            }

            IVariable returnVariable = null;
            if (namePath == null || namePath.Length == 0)
            {
                return null;
            }
            else if (namePath.StartsWith("evaluate", StringComparison.CurrentCultureIgnoreCase))
            {
                returnVariable = new VariableExpression(namePath.Remove(0, 8), relativeTo);
            }
            else
            {
                // Remove a square bracketed model name and change our relativeTo model to 
                // the referenced model.
                if (namePath.StartsWith("["))
                {
                    int posCloseBracket = namePath.IndexOf(']');
                    if (posCloseBracket == -1)
                    {
                        return null;
                    }
                    string modelName = namePath.Substring(1, posCloseBracket - 1);
                    namePath = namePath.Remove(0, posCloseBracket + 1);
                    relativeTo = this.Find(modelName, relativeTo);
                    if (relativeTo == null)
                    {
                        throw new Exception("Cannot find model: " + modelName);
                    }
                }
                else if (namePath.StartsWith("."))
                {
                    // Absolute path
                    Model root = relativeTo;
                    while (root.Parent != null)
                    {
                        root = root.Parent;
                    }
                    relativeTo = root;

                    int posPeriod = namePath.IndexOf('.', 1);
                    if (posPeriod == -1)
                    {
                        posPeriod = namePath.Length;
                    }

                    namePath = namePath.Remove(0, posPeriod);
                    if (namePath.StartsWith("."))
                    {
                        namePath.Remove(1);
                    }
                }

                // Now walk the series of '.' separated path bits, assuming the path bits
                // are child models. Stop when we can't find the child model.
                string[] namePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                int i;
                for (i = 0; i < namePathBits.Length; i++)
                {
                    Model localModel = relativeTo.Children.All.FirstOrDefault(m => m.Name == namePathBits[i]);
                    if (localModel == null)
                    {
                        break;
                    }
                    else
                    {
                        relativeTo = localModel;
                    }
                }

                // At this point there are only 2 possibilities. We have encountered a 
                // PropertyInfo or the path is invalid.
                // We now need to loop through the remaining path bits and keep track of each
                // section of the path as each section will have to be evaulated everytime a
                // a get is done for this path. 
                // The variable 'i' will point to the name path that cannot be found as a model.
                object relativeToObject = relativeTo;
                List<IVariable> properties = new List<IVariable>();
                properties.Add(new VariableObject(relativeTo));
                for (int j = i; j < namePathBits.Length; j++)
                {
                    // look for an array specifier e.g. sw[2]
                    string arraySpecifier = null;
                    if (namePathBits[j].Contains("["))
                    {
                        arraySpecifier = Utility.String.SplitOffBracketedValue(ref namePathBits[j], '[', ']');
                    }

                    // Look for either a property or a child model.
                    Model localModel = null;
                    PropertyInfo propertyInfo = relativeToObject.GetType().GetProperty(namePathBits[j]);
                    if (propertyInfo == null && relativeToObject is Model)
                    {
                        // Not a property, may be a child model.
                        localModel = (relativeToObject as Model).Children.All.FirstOrDefault(m => m.Name == namePathBits[i]);
                        if (localModel == null)
                        {
                            return null;
                        }

                        properties.Add(new VariableObject(localModel));
                        relativeToObject = localModel;
                    }
                    else if (propertyInfo != null)
                    {
                        VariableProperty property = new VariableProperty(relativeToObject, propertyInfo, arraySpecifier);
                        properties.Add(property);
                        relativeToObject = property.Value;
                    }
                    else
                    {
                        return null;
                    }
                }

                // We now have a list of IVariable instances that can be evaluated to 
                // produce a return value for the given path. Wrap the list into an IVariable.
                returnVariable = new VariableComposite(namePath, properties);
            }

            // Add varible to cache.
            if (returnVariable != null)
            {
                this.cache.Add(cacheKey, returnVariable);
            }

            return returnVariable;
        }

        /// <summary>
        /// Return a model with the specified name is in scope. Returns null if none found.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <returns>The found model or null if not found</returns>
        public Model Find(string namePath, Model relativeTo)
        {
            // Look in cache first.
            string cacheKey = relativeTo.FullPath + "|INSCOPENAME|" + namePath;
            if (this.cache.ContainsKey(cacheKey))
            {
                return (this.cache[cacheKey] as IVariable).Value as Model;
            }

            // Not in cache - get all in scope and return the one matching namePath.
            foreach (Model model in this.FindAll(relativeTo))
            {
                if (model.Name.Equals(namePath, StringComparison.CurrentCultureIgnoreCase))
                {
                    this.cache.Add(cacheKey, new VariableObject(model));
                    return model;
                }
            }

            return null;
        }

        /// <summary>
        /// Return a model with the specified type is in scope. Returns null if none found.
        /// </summary>
        /// <param name="type">The type of the object to return</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <returns>The found model or null if not found</returns>
        public Model Find(Type type, Model relativeTo)
        {
            // Look in cache first.
            string cacheKey = relativeTo.FullPath + "|INSCOPETYPE|" + type.Name;
            if (this.cache.ContainsKey(cacheKey))
            {
                return (this.cache[cacheKey] as IVariable).Value as Model;
            }

            // Not in cache - get all in scope and return the one matching namePath.
            foreach (Model model in this.FindAll(relativeTo))
            {
                if (type.IsAssignableFrom(model.GetType()))
                {
                    this.cache.Add(cacheKey, new VariableObject(model));
                    return model;
                }
            }

            return null;
        }

        /// <summary>
        /// Return all models within scope of the specified relative model.
        /// </summary>
        /// <param name="relativeTo">The model calling this method</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public Model[] FindAll(Model relativeTo)
        {
            // Look in cache first.
            string cacheKey = relativeTo.FullPath + "|ALLINSCOPE";
            if (this.cache.ContainsKey(cacheKey))
            {
                return this.cache[cacheKey] as Model[];
            }

            // Get all children first.
            List<Model> modelsInScope = new List<Model>();
            modelsInScope.AddRange(relativeTo.Children.AllRecursively);

            // Add relativeTo.
            modelsInScope.Add(relativeTo);

            // Get siblings and parents siblings and parents, parents siblings etc
            // until we reach a Simulations or Simulation model.
            if (!(relativeTo is Simulations))
            {
                do
                {
                    modelsInScope.AddRange(Siblings(relativeTo));
                    relativeTo = relativeTo.Parent;

                    // Add in the top level model that we stopped on.
                    if (relativeTo != null)
                    {
                        modelsInScope.Add(relativeTo);
                    }
                }
                while (relativeTo != null && !(relativeTo is Simulation) && !(relativeTo is Simulations));
            }
            // Add the in scope models to the cache and return them
            this.cache.Add(cacheKey, modelsInScope.ToArray());
            return modelsInScope.ToArray();
        }

        /// <summary>
        /// Return all models of the specified type within scope of the specified relative model.
        /// </summary>
        /// <param name="type">The type of the models to return</param>
        /// <param name="relativeTo">The model calling this method</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public Model[] FindAll(Type type, Model relativeTo)
        {
            // Look in cache first.
            string cacheKey = relativeTo.FullPath + "|ALLINSCOPEOFTYPE|" + type.Name;
            if (this.cache.ContainsKey(cacheKey))
            {
                return this.cache[cacheKey] as Model[];
            }

            List<Model> modelsToReturn = new List<Model>();
            foreach (Model model in this.FindAll(relativeTo))
            {
                if (type.IsAssignableFrom(model.GetType()))
                {
                    modelsToReturn.Add(model);
                }
            }

            this.cache.Add(cacheKey, modelsToReturn.ToArray());
            return modelsToReturn.ToArray();
        }

        /// <summary>
        /// Return all siblings of the specified model.
        /// </summary>
        /// <param name="relativeTo">The model for which siblings are to be found</param>
        /// <returns>The found siblings or an empty array if not found.</returns>
        private static Model[] Siblings(Model relativeTo)
        {
            if (relativeTo.Parent == null)
            {
                return new Model[0];
            }

            List<Model> siblings = new List<Model>();
            foreach (Model child in relativeTo.Parent.Children.All)
            {
                if (child != relativeTo)
                {
                    siblings.Add(child);
                }
            }

            return siblings.ToArray();
        }
    }
}
