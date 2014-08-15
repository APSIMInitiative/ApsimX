using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Reflection;
using System.IO;

namespace Models.Core
{
    /// <summary>
    /// Encapsulates a collection of models.
    /// </summary>
    [Serializable]
    public class ModelCollection
    {
        /// <summary>
        /// The model that contains the list of child models that
        /// we are to work with.
        /// </summary>
        private Model Model;

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        public ModelCollection(Model model)
        {
            Model = model;
        } 

        /// <summary>
        /// Return a list of child models.
        /// </summary>
        public List<Model> All { get { return Model.Models; } }

        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        public List<Model> AllRecursively
        {
            get
            {
                List<Model> models = new List<Model>();

                if (Model.Models != null)
                {
                    foreach (Model child in Model.Models)
                    {
                        models.Add(child);
                        models.AddRange(child.Children.AllRecursively);
                    }
                }
                return models;
            }
        }

        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        public List<Model> AllRecursivelyVisible
        {
            get
            {
                List<Model> models = new List<Model>();

                if (Model.Models != null)
                {
                    foreach (Model child in Model.Models)
                    {
                        if (!child.IsHidden)
                        {
                            models.Add(child);
                            models.AddRange(child.Children.AllRecursivelyVisible);
                        }
                    }
                }
                return models;
            }
        }

        /// <summary>
        /// Return a list of all child models recursively. Only models of 
        /// the specified 'typeFilter' will be returned. Never returns
        /// null. Can return an empty list.
        /// </summary>
        public List<Model> AllRecursivelyMatching(Type typeFilter)
        {
            List<Model> models = new List<Model>();

            if (Model.Models != null)
            {
                foreach (Model child in Model.Models)
                {
                    if (child.GetType() == typeFilter)
                    {
                        models.Add(child);
                    }

                    models.AddRange(child.Children.AllRecursivelyMatching(typeFilter));
                }
            }
            return models;
        }

        /// <summary>
        /// Return a child model that matches the specified 'modelType'. Returns 
        /// null if not found.
        /// </summary>
        public Model Matching(Type modelType)
        {
            foreach (Model child in Model.Models)
                if (modelType.IsAssignableFrom(child.GetType()))
                    return child;
            return null;
        }

        /// <summary>
        /// Return a child model that matches the specified 'name'. Returns 
        /// null if not found.
        /// </summary>
        public Model Matching(string name)
        {
            foreach (Model child in Model.Models)
                if (child.Name == name)
                    return child;
            return null;
        }

        /// <summary>
        /// Return children that match the specified 'modelType'. Never returns 
        /// null. Can return emply Array.
        /// </summary>
        public Model[] MatchingMultiple(Type modelType)
        {
            List<Model> matches = new List<Model>();
            foreach (Model child in Model.Models)
                if (modelType.IsAssignableFrom(child.GetType()))
                    matches.Add(child);
            return matches.ToArray();
        }

        /// <summary>
        /// Add a model to the collection. Will throw if model cannot be added.
        /// </summary>
        public void Add(Model model)
        {
            EnsureNameIsUnique(model);
            Model.Models.Add(model);
            ClearCache();

            if (model.Models == null)
                model.Models = new List<Core.Model>();

            model.Parent = Model;
            Model.OnLoaded();

            // Call the model's (and all children recursively) OnLoaded method
            model.OnLoaded();
            ParentAllChildren(model);

            Simulation simulation = Model.ParentOfType(typeof(Simulation)) as Simulation;
            if (simulation != null && simulation.IsRunning)
            {
                model.Events.Connect();
                model.ResolveLinks();
            }
        }

        /// <summary>
        /// Replace the specified 'modelToReplace' with the specified 'newModel'. Return
        /// true if successful.
        /// </summary>
        public bool Replace(Model modelToReplace, Model newModel)
        {
            // Find the model.
            int index = Model.Models.IndexOf(modelToReplace);
            if (index != -1)
            {
                Model oldModel = Model.Models[index];

                oldModel.Events.Disconnect();
                oldModel.UnResolveLinks();

                // remove the existing model.
                Model.Models.RemoveAt(index);

                // Name and parent the model we're adding.
                newModel.Name = modelToReplace.Name;
                newModel.Parent = modelToReplace.Parent;
                EnsureNameIsUnique(newModel);

                // insert the new model.
                Model.Models.Insert(index, newModel);

                // clear caches.
                ClearCache();

                oldModel.Parent = null;

                // Connect our new child.
                Simulation simulation = Model.ParentOfType(typeof(Simulation)) as Simulation;
                if (simulation != null && simulation.IsRunning)
                {
                    newModel.Events.Connect();
                    newModel.ResolveLinks();
                }
                newModel.OnLoaded();

                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a model from the Models collection. Returns true if model was removed.
        /// </summary>
        public bool Remove(Model modelToRemove)
        {
            // Find the model.
            int index = Model.Models.IndexOf(modelToRemove);
            if (index != -1)
            {
                Model oldModel = Model.Models[index];

                // remove the existing model.
                Model.Models.RemoveAt(index);

                // clear caches.
                ClearCache();

                oldModel.Events.Disconnect();
                oldModel.UnResolveLinks();

                return true;
            }
            return false;
        }

        /// <summary>
        /// Give the specified model a unique name
        /// </summary>
        private string EnsureNameIsUnique(Model modelToCheck)
        {
            if (modelToCheck.Models == null)
                return modelToCheck.Name;
            string originalName = modelToCheck.Name;
            string NewName = originalName;
            int Counter = 0;
            Model child = Model.Models.FirstOrDefault(m => m.Name == NewName);
            while (child != null && child != modelToCheck && Counter < 10000)
            {
                Counter++;
                NewName = originalName + Counter.ToString();
                child = Model.Models.FirstOrDefault(m => m.Name == NewName);
            }
            if (Counter == 1000)
                throw new Exception("Cannot create a unique name for model: " + originalName);
            Utility.Reflection.SetName(modelToCheck, NewName);
            return NewName;
        }

        /// <summary>
        /// Clear the variable cache.
        /// </summary>
        private void ClearCache()
        {
            Simulation simulation = Model.ParentOfType(typeof(Simulation)) as Simulation;
            if (simulation != null)
            {
                simulation.Locater.Clear();
            }
        }

        /// <summary>
        /// Parent all children of 'model' correctly and call their OnLoaded.
        /// </summary>
        private void ParentAllChildren(Model model)
        {
            foreach (Model child in model.Children.All)
            {
                child.Parent = model;
                child.OnLoaded();
                ParentAllChildren(child);
            }
        }
    }
}
