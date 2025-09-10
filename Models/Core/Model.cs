using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Models.Factorial;
using Newtonsoft.Json;
using APSIM.Core;

namespace Models.Core
{

    /// <summary>
    /// Base class for all models
    /// </summary>
    [Serializable]
    [ValidParent(typeof(Folder))]
    [ValidParent(typeof(Factor))]
    [ValidParent(typeof(CompositeFactor))]
    public abstract class Model : IModel, INodeModel, ICreatable
    {
        [NonSerialized]
        private IModel modelParent;

        [NonSerialized]
        private Node node;

        private bool _enabled = true;
        private bool _isCreated = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Model" /> class.
        /// </summary>
        public Model()
        {
            this.Name = GetType().Name;
            this.IsHidden = false;
            this.Children = new List<IModel>();
            Enabled = true;
        }

        /// <summary>
        /// Instance of owning node.
        /// </summary>
        [JsonIgnore]
        public Node Node { get { return node; } set { node = value;  } }

        /// <summary>
        /// Gets or sets the name of the model
        /// </summary>
        public string Name { get; set; }

        /// <summary>The name of the resource.</summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets a list of child models.
        /// </summary>
        public List<IModel> Children { get; set; }

        /// <summary>
        /// Gets or sets the parent of the model.
        /// </summary>
        [JsonIgnore]
        public virtual IModel Parent { get { return modelParent; } set { modelParent = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether a model is hidden from the user.
        /// </summary>
        [JsonIgnore]
        public bool IsHidden { get; set; }

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
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                // enable / disable our children if serialisation has completed.
                if (_isCreated)
                    foreach (var child in Children)
                        child.Enabled = _enabled;
            }
        }

        /// <summary>
        /// Controls whether the model can be modified.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Full path to the model.
        /// </summary>
        public string FullPath => Node?.FullNameAndPath;

        /// <summary>
        /// Called when the model has been newly created in memory whether from
        /// cloning or deserialisation.
        /// </summary>
        public virtual void OnCreated()
        {
            _isCreated = true;
            // Check for duplicate child models (child models with the same name).
            // First, group children according to their name.
            IEnumerable<IGrouping<string, IModel>> groups = Children.GroupBy(c => c.Name);
            foreach (IGrouping<string, IModel> group in groups)
            {
                int n = group.Count();
                if (n > 1)
                    throw new Exception($"Duplicate models found: {FullPath} has {n} children named {group.Key}");
            }
        }

        /// <summary>
        /// Called when the model is about to be deserialised.
        /// </summary>
        public virtual void OnSerialising()
        {

        }

        /// <summary>
        /// Called immediately before a simulation has its links resolved and is run.
        /// It provides an opportunity for a simulation to restructure itself
        /// e.g. add / remove models.
        /// </summary>
        public virtual void OnPreLink() { }

        /// <summary>
        /// Return true iff a model with the given type can be added to the model.
        /// </summary>
        /// <param name="type">The child type.</param>
        public bool IsChildAllowable(Type type)
        {
            // Simulations objects cannot be added to any other models.
            if (typeof(Simulations).IsAssignableFrom(type))
                return false;

            // If it's not an IModel, it's not a valid child.
            if (!typeof(IModel).IsAssignableFrom(type))
                return false;

            List<Type> modelParentTypes = new();
            foreach(ValidParentAttribute validParent in ReflectionUtilities.GetAttributes(typeof(Model), typeof(ValidParentAttribute), true))
                modelParentTypes.Add(validParent.ParentType);

            bool hasValidParents = false;

            // Is allowable if one of the valid parents of this type (t) matches the parent type.
            foreach (ValidParentAttribute validParent in ReflectionUtilities.GetAttributes(type, typeof(ValidParentAttribute), true))
            {
                // Used to make objects that have no explicit ValidParent attributes allowed
                // to be placed anywhere.
                if (!modelParentTypes.Contains(validParent.ParentType))
                    hasValidParents = true;

                if (validParent != null)
                {
                    if (validParent.DropAnywhere)
                        return true;

                    if (validParent.ParentType != null && validParent.ParentType.IsAssignableFrom(GetType()))
                        return true;
                }
            }

            // If it doesn't have any valid parents, it should be able to be placed anywhere.
            if(hasValidParents)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Parent all descendant models.
        /// </summary>
        public void ParentAllDescendants()
        {
            foreach (IModel child in Children)
            {
                child.Parent = this;
                child.ParentAllDescendants();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual IEnumerable<INodeModel> GetChildren()
        {
            return Children.Cast<INodeModel>();
        }

        /// <summary>Set the name of the model.</summary>
        /// <param name="name">The new name</param>
        public virtual void Rename(string name)
        {
            Name = name;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parent"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void SetParent(INodeModel parent)
        {
            Parent = parent as IModel;
        }

        /// <summary>
        /// Add a child model.
        /// </summary>
        /// <param name="childModel">The child model.</param>
        public void AddChild(INodeModel childModel)
        {
            Children.Add(childModel as IModel);
        }

        /// <summary>
        /// Insert a child model into the children list.
        /// </summary>
        /// <param name="index">The position to insert the child into.</param>
        /// <param name="childModel">The model to insert.</param>
        public void InsertChild(int index, INodeModel childModel)
        {
            Children.Insert(index, childModel as IModel);
            childModel.SetParent(this);
        }

        /// <summary>
        /// Remove a child.
        /// </summary>
        /// <param name="childModel">The child to remove.</param>
        public void RemoveChild(INodeModel childModel)
        {
            Children.Remove(childModel as IModel);
            childModel.SetParent(null);
        }
    }
}
