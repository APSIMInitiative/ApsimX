namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>
    /// Base class for all models
    /// </summary>
    [Serializable]
    public class Model : IModel
    {
        [NonSerialized]
        private IModel modelParent;

        /// <summary>
        /// Initializes a new instance of the <see cref="Model" /> class.
        /// </summary>
        public Model()
        {
            this.Name = GetType().Name;
            this.IsHidden = false;
            this.Children = new List<IModel>();
            IncludeInDocumentation = true;
            Enabled = true;
        }

        /// <summary>
        /// Gets or sets the name of the model
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a list of child models.   
        /// </summary>
        public List<IModel> Children { get; set; }

        /// <summary>
        /// Gets or sets the parent of the model.
        /// </summary>
        [XmlIgnore]
        public IModel Parent { get { return modelParent; } set { modelParent = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether a model is hidden from the user.
        /// </summary>
        [XmlIgnore]
        public bool IsHidden { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the graph should be included in the auto-doc documentation.
        /// </summary>
        public bool IncludeInDocumentation { get; set; }

        /// <summary>
        /// A cleanup routine, in which we clear our child list recursively
        /// </summary>
        public void ClearChildLists()
        {
            foreach (Model child in Children)
                child.ClearChildLists();
            Children.Clear();
        }

        /// <summary>
        /// Gets or sets whether the model is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Controls whether the model can be modified.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Full path to the model.
        /// </summary>
        public string FullPath
        {
            get
            {
                string fullPath = "." + Name;
                IModel parent = Parent;
                while (parent != null)
                {
                    fullPath = fullPath.Insert(0, "." + parent.Name);
                    parent = parent.Parent;
                }

                return fullPath;
            }
        }

        /// <summary>
        /// Find a sibling with a given name.
        /// </summary>
        /// <param name="name">Name of the sibling.</param>
        public IModel Sibling(string name)
        {
            return Siblings(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a descendant with a given name.
        /// </summary>
        /// <param name="name">Name of the descendant.</param>
        public IModel Descendant(string name)
        {
            return Descendants(name).FirstOrDefault();
        }

        /// <summary>
        /// Find an ancestor with a given name.
        /// </summary>
        /// <param name="name">Name of the ancestor.</param>
        public IModel Ancestor(string name)
        {
            return Ancestors(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a model in scope with a given name.
        /// </summary>
        /// <param name="name">Name of the model.</param>
        public IModel InScope(string name)
        {
            return InScopeAll(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a sibling of a given type.
        /// </summary>
        /// <typeparam name="T">Type of the sibling.</typeparam>
        public T Sibling<T>() where T : IModel
        {
            return Siblings<T>().FirstOrDefault();
        }

        /// <summary>
        /// Performs a depth-first search for a descendant of a given type.
        /// </summary>
        /// <typeparam name="T">Type of the descendant.</typeparam>
        public T Descendant<T>() where T : IModel
        {
            return Descendants<T>().FirstOrDefault();
        }

        /// <summary>
        /// Find an ancestor of a given type.
        /// </summary>
        /// <typeparam name="T">Type of the ancestor.</typeparam>
        public T Ancestor<T>() where T : IModel
        {
            return Ancestors<T>().FirstOrDefault();
        }

        /// <summary>
        /// Find a model of a given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of model to find.</typeparam>
        public T InScope<T>() where T : IModel
        {
            return InScopeAll<T>().FirstOrDefault();
        }

        /// <summary>
        /// Find a sibling with a given type and name.
        /// </summary>
        /// <param name="name">Name of the sibling.</param>
        /// <typeparam name="T">Type of the sibling.</typeparam>
        public T Sibling<T>(string name) where T : IModel
        {
            return Siblings<T>(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a descendant model with a given type and name.
        /// </summary>
        /// <param name="name">Name of the descendant.</param>
        /// <typeparam name="T">Type of the descendant.</typeparam>
        public T Descendant<T>(string name) where T : IModel
        {
            return Descendants<T>(name).FirstOrDefault();
        }

        /// <summary>
        /// Find an ancestor with a given type and name.
        /// </summary>
        /// <param name="name">Name of the ancestor.</param>
        /// <typeparam name="T">Type of the ancestor.</typeparam>
        public T Ancestor<T>(string name) where T : IModel
        {
            return Ancestors<T>(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a model in scope with a given type and name.
        /// </summary>
        /// <param name="name">Name of the model.</param>
        /// <typeparam name="T">Type of model to find.</typeparam>
        public T InScope<T>(string name) where T : IModel
        {
            return InScopeAll<T>(name).FirstOrDefault();
        }

        /// <summary>
        /// Find all ancestors of the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public IEnumerable<T> Ancestors<T>() where T : IModel
        {
            return Ancestors().OfType<T>();
        }

        /// <summary>
        /// Find all descendants of the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of descendants to return.</typeparam>
        public IEnumerable<T> Descendants<T>() where T : IModel
        {
            return Descendants().OfType<T>();
        }

        /// <summary>
        /// Find all siblings of the given type.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        public IEnumerable<T> Siblings<T>() where T : IModel
        {
            return Siblings().OfType<T>();
        }

        /// <summary>
        /// Find all models of a given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of models to find.</typeparam>
        public IEnumerable<T> InScopeAll<T>() where T : IModel
        {
            return InScopeAll().OfType<T>();
        }

        /// <summary>
        /// Find all siblings with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        /// <param name="name">Name of the siblings.</param>
        public IEnumerable<T> Siblings<T>(string name) where T : IModel
        {
            return Siblings(name).OfType<T>();
        }

        /// <summary>
        /// Find all descendants with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of descendants to return.</typeparam>
        /// <param name="name">Name of the descendants.</param>
        public IEnumerable<T> Descendants<T>(string name) where T : IModel
        {
            return Descendants(name).OfType<T>();
        }

        /// <summary>
        /// Find all ancestors of the given type.
        /// </summary>
        /// <typeparam name="T">Type of ancestors to return.</typeparam>
        /// <param name="name">Name of the ancestors.</param>
        public IEnumerable<T> Ancestors<T>(string name) where T : IModel
        {
            return Ancestors(name).OfType<T>();
        }

        /// <summary>
        /// Find all models of a given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of models to find.</typeparam>
        /// <param name="name">Name of the models.</param>
        public IEnumerable<T> InScopeAll<T>(string name) where T : IModel
        {
            return InScopeAll(name).OfType<T>();
        }

        /// <summary>
        /// Find all siblings with a given name.
        /// </summary>
        /// <param name="name">Name of the siblings.</param>
        public IEnumerable<IModel> Siblings(string name)
        {
            return Siblings().Where(s => s.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Find all descendants with a given name.
        /// </summary>
        /// <param name="name">Name of the descendants.</param>
        public IEnumerable<IModel> Descendants(string name)
        {
            return Descendants().Where(d => d.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Find all ancestors with a given name.
        /// </summary>
        /// <param name="name">Name of the ancestors.</param>
        public IEnumerable<IModel> Ancestors(string name)
        {
            return Ancestors().Where(a => a.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Find all model in scope with a given name.
        /// </summary>
        /// <param name="name">Name of the models.</param>
        public IEnumerable<IModel> InScopeAll(string name)
        {
            return InScopeAll().Where(m => m.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Returns all ancestor models.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IModel> Ancestors()
        {
            IModel parent = Parent;
            while (parent != null)
            {
                yield return parent;
                parent = parent.Parent;
            }
        }

        /// <summary>
        /// Returns all descendant models.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IModel> Descendants()
        {
            foreach (IModel child in Children)
            {
                yield return child;

                foreach (IModel descendant in child.Descendants())
                    yield return descendant;
            }
        }

        /// <summary>
        /// Returns all sibling models.
        /// </summary>
        public IEnumerable<IModel> Siblings()
        {
            if (Parent == null || Parent.Children == null)
                yield break;

            foreach (IModel sibling in Parent.Children)
                if (sibling != this)
                    yield return sibling;
        }

        /// <summary>
        /// Returns all models which are in scope.
        /// </summary>
        public IEnumerable<IModel> InScopeAll()
        {
            Simulation sim = Ancestor<Simulation>();
            ScopingRules scope = sim?.Scope ?? new ScopingRules();
            foreach (IModel result in scope.FindAll(this))
                yield return result;
        }

        /// <summary>
        /// Called when the model has been newly created in memory whether from 
        /// cloning or deserialisation.
        /// </summary>
        public virtual void OnCreated() { }
    }
}
