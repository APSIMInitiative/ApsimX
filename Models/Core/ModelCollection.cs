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
        /// Return a list of all child models recursively. If a 'typeFilter' is 
        /// specified then only models of that type will be returned. Never returns
        /// null. Can return an empty list.
        /// </summary>
        public List<Model> AllRecursively(Type typeFilter = null)
        {
            List<Model> models = new List<Model>();

            if (Model.Models != null)
            {
                foreach (Model child in Model.Models)
                {
                    models.Add(child);
                    models.AddRange(child.Children.AllRecursively(typeFilter));
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
            model.Parent = Model;
            Model.Scope.ClearCache();
            Model.Variables.ClearCache();

            if (Model.IsConnected)
            {
                if (model.Models == null)
                    model.Models = new List<Core.Model>();
                model.IsConnected = true;
                model.Connect();
            }

            // Call the model's OnLoaded method.
            Model.OnLoaded();
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

                if (oldModel.IsConnected)
                    oldModel.Disconnect();

                // remove the existing model.
                Model.Models.RemoveAt(index);

                // Name and parent the model we're adding.
                newModel.Name = modelToReplace.Name;
                newModel.Parent = modelToReplace.Parent;
                EnsureNameIsUnique(newModel);

                // insert the new model.
                Model.Models.Insert(index, newModel);

                // clear caches.
                Model.Scope.ClearCache();
                Model.Variables.ClearCache();

                oldModel.Parent = null;

                // If we are connected then connect our new child.
                if (Model.IsConnected)
                {
                    newModel.IsConnected = true;
                    newModel.Connect();
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
                Model.Scope.ClearCache();
                Model.Variables.ClearCache();

                if (oldModel.IsConnected)
                    oldModel.Disconnect();

                return true;
            }
            return false;
        }

        /// <summary>
        /// Give the specified model a unique name
        /// </summary>
        private string EnsureNameIsUnique(Model Model)
        {
            if (Model.Models == null)
                return Model.Name;
            string originalName = Model.Name;
            string NewName = originalName;
            int Counter = 0;
            Model child = Model.Models.FirstOrDefault(m => m.Name == NewName);
            while (child != null && child != Model && Counter < 10000)
            {
                Counter++;
                NewName = originalName + Counter.ToString();
                child = Model.Models.FirstOrDefault(m => m.Name == NewName);
            }
            if (Counter == 1000)
                throw new Exception("Cannot create a unique name for model: " + originalName);
            Utility.Reflection.SetName(Model, NewName);
            return NewName;
        }

    }
}
