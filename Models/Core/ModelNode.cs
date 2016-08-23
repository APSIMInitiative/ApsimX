
namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using System.ComponentModel;
    using System.Threading;
    using System.Reflection;

    /// <summary>
    /// Wrapper class for a 'model'
    /// </summary>
    [Serializable]
    public class ModelWrapper
    {
        private int depth;

        // ---- Properties --------------------------------------------------------------

        /// <summary>Name</summary>
        public string Name { get; set; }

        /// <summary>Associated model.</summary>
        public object Model { get; set; }

        /// <summary>The children.</summary>
        [XmlElement("Child")]
        public List<ModelWrapper> Children { get; set; }

        /// <summary>Gets a list of all children recursively.</summary>
        [XmlIgnore]
        public List<ModelWrapper> ChildrenRecursively
        {
            get
            {
                List<ModelWrapper> models = new List<ModelWrapper>();

                foreach (ModelWrapper child in Children)
                {
                    models.Add(child);
                    models.AddRange(child.ChildrenRecursively);
                }
                return models;
            }
        }

        /// <summary>Gets a list of all child models AND this model.</summary>
        [XmlIgnore]
        public List<ModelWrapper> Models
        {
            get
            {
                List<ModelWrapper> models = ChildrenRecursively;
                models.Insert(0, this);
                return models;
            }
        }

        // ---- Methods --------------------------------------------------------------

        /// <summary>Constructor</summary>
        public ModelWrapper() { Children = new List<ModelWrapper>(); }

        /// <summary>Constructor</summary>
        public ModelWrapper(object model, string name = null)
            : base()
        {
            Children = new List<ModelWrapper>();
            Model = model;
            if (name == null)
                Name = model.GetType().Name;
            else
                Name = name;
        }

        /// <summary>A method for adding a model.</summary>
        public ModelWrapper Add(object model)
        {
            if (Model == null)
            {
                Model = model;
                Name = model.GetType().Name;
                depth = 1;
                return this;
            }
            else
            {
                ModelWrapper child = new ModelWrapper(model) { Name = model.GetType().Name };
                child.depth = depth + 1;
                Children.Add(child);
                return child;
            }
        }

        /// <summary>A method for removing a model.</summary>
        public void Remove(object node)
        {
            throw new NotImplementedException();
        }

        /// <summary>Return a list of all models in scope.</summary>
        /// <param name="allModels"></param>
        public List<ModelWrapper> FindModelsInScope(List<ModelWrapper> allModels)
        {
            return allModels.FindAll(m => m.depth <= depth || Children.Contains(m));
        }

        /// <summary>Get a property as specified by the path.</summary>
        /// <param name="namePath">The path to the property</param>
        /// <param name="ignoreCase">If true, then name comparison will ignore case sensitivity.</param>
        /// <returns>The property or null if not found.</returns>
        public object Get(string namePath, bool ignoreCase = false)
        {
            Property property = GetProperty(namePath, ignoreCase);
            if (property == null)
                return null;
            return property.Get();
        }

        /// <summary>Set a property value.</summary>
        /// <param name="namePath">The path to the property</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>Return true if value was set.</returns>
        public bool Set(string namePath, object value)
        {
            Property property = GetProperty(namePath);
            if (property == null)
                return false;
            property.Set(value);
            return true;
        }

        /// <summary>Get a property or model as specified by the path.</summary>
        /// <param name="namePath">The path to the property</param>
        /// <param name="ignoreCase">If true, then name comparison will ignore case sensitivity.</param>
        /// <returns>The property or null if not found.</returns>
        internal Property GetProperty(string namePath, bool ignoreCase = false)
        {
            // Set up some flags depending on case sensitivity.
            StringComparison compareType = StringComparison.Ordinal;
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            if (ignoreCase)
            {
                compareType = System.StringComparison.OrdinalIgnoreCase;
                bindingFlags |= BindingFlags.IgnoreCase;
            }

            Property relativeTo = new Property(this);

            // Walk the series of '.' separated path bits, assuming the path bits
            // are child models or properties. Stop when we can't find a path bit.
            string[] namePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //VariableEvaluator variables = new VariableEvaluator();
            foreach (string pathBit in namePathBits)
            {
                if (!relativeTo.SetToChildModel(pathBit, compareType))
                    if (!relativeTo.SetToProperty(pathBit, bindingFlags))
                        return null;
            }
                
            return relativeTo;
        }



    }
}
