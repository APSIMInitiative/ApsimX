using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;

namespace Models.Core
{
    public delegate void NullTypeDelegate();
    public class Model
    {
        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parent model.
        /// </summary>
        [XmlIgnore]
        public Model Parent { get; set; }

        /// <summary>
        /// Return a full path to this system. Does not include the 'Simulation' node.
        /// Format: .PaddockName.ChildName
        /// </summary>
        public virtual string FullPath
        {
            get
            {
                return Parent.FullPath + "." + Name;
            }
        }

        /// <summary>
        /// The simulation has completed its initialisation and all [Link] fields have been set.
        /// Overriding this method allows a model to initialise itself.
        /// </summary>
        public virtual void OnInitialised() { }

        /// <summary>
        /// Simulation is terminating. Overriding this method allows a model to perform cleanup.
        /// </summary>
        public virtual void OnCompleted() { }

        /// <summary>
        /// Returns a model of the specified type that is in scope. Returns null if none found.
        /// </summary>
        public virtual Model Find(Type ModelType)
        {
            if (ModelType.IsAssignableFrom(this.GetType()))
                return this;
            else if (Parent == null)
                return null;
            else
                return Parent.Find(ModelType);
        }

        /// <summary>
        /// Returns a model with the specified name is in scope. Returns null if none found.
        /// </summary>
        public virtual Model Find(string ModelName)
        {
            if (Name == ModelName)
                return this;
            else
                return Parent.Find(ModelName);
        }

        /// <summary>
        /// Return a model or variable value using the specified NamePath. Returns null if not found.
        /// NB: Can only pass an absolute path to a model.
        /// </summary>
        public virtual object Get(string NamePath)
        {
            if (NamePath.Length > 0 && NamePath[0] == '.')
                return Parent.Get(NamePath);

            else
                return null;
        }

        /// <summary>
        /// Recursively resolve all [Link] fields. A link must be private. This method will also
        /// go through any public members that are a model or a list of models. For each one found
        /// it will recursively call this method to resolve links in them. This is an internal
        /// method that won't normally be called by models.
        /// </summary>
        public virtual void ResolveLinks()
        {
            // Go looking for private [Link]s
            foreach (FieldInfo Field in Utility.Reflection.GetAllFields(this.GetType(),
                                                                        BindingFlags.Instance |
                                                                        BindingFlags.NonPublic))
            {
                if (Field.IsDefined(typeof(Link), false))
                {
                    object LinkedObject = Find(Field.FieldType);
                    if (LinkedObject != null)
                        Field.SetValue(this, LinkedObject);
                    else
                        throw new Exception("Cannot resolve [Link] " + Field.ToString() + ". Model type is " + this.GetType().FullName);
                }
            }

            List<Model> Children = new List<Model>();
            FindChildModels(this, Children);

            foreach (Model Child in Children)
            {
                // Set the childs parent property.
                Child.Parent = this;

                // Tell child to resolve its links.
                Child.ResolveLinks();
            }
        }

        /// <summary>
        /// Initialise all components by calling their OnInitialise methods. This is an internal
        /// method that won't normally be called by models.
        /// </summary>
        public void Initialise()
        {
            OnInitialised();

            List<Model> Children = new List<Model>();
            FindChildModels(this, Children);
            foreach (Model Child in Children)
                Child.Initialise();
        }

        /// <summary>
        /// Initialise all components by calling their OnInitialise methods. This is an internal
        /// method that won't normally be called by models.
        /// </summary>
        public void Completed()
        {
            OnCompleted();

            List<Model> Children = new List<Model>();
            FindChildModels(this, Children);
            foreach (Model Child in Children)
                Child.Completed();
        }

        /// <summary>
        /// Return a list of models in scope.
        /// </summary>
        /// <returns></returns>
        public string[] ModelsInScope()
        {
            // Go looking for a zone or Simulation.
            Model M = this;
            while (M.Parent != null && !(M is Zone))
                M = M.Parent;

            // Get a list of children of this zone.
            List<Model> AllModels = new List<Model>();
            FindChildModels(M, AllModels);

            // Convert a list of models to a list of model FullPaths
            List<string> ModelPaths = new List<string>();
            foreach (Model Model in AllModels)
                ModelPaths.Add(Model.FullPath);

            return ModelPaths.ToArray();
        }

        /// <summary>
        /// Go looking for public, writtable, class properties that are in the "Model" namespace.
        /// For each one found, recursively call this method so that their [Link]s might be resolved.
        /// </summary>
        private static void FindChildModels(Model Model, List<Model> Children)
        {
            foreach (PropertyInfo Property in Model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (Property.GetType().IsClass && Property.CanWrite)
                {
                    if ((Property.PropertyType.IsArray && Property.PropertyType.FullName.Contains("Models."))
                        || Property.PropertyType.FullName.Contains("Generic.List"))
                    {
                        // If a field is public and is a class and 
                        object Value = Property.GetValue(Model, null);
                        if (Value != null)
                        {
                            IList List = (IList)Value;
                            if (List.Count > 0 && List[0].GetType().IsClass)
                                for (int i = 0; i < List.Count; i++)
                                {
                                    if (List[i] is Model)
                                        Children.Add(List[i] as Model);
                                }
                        }
                    }
                    else if (Property.Name != "Parent" && Property.PropertyType.IsSubclassOf(typeof(Model)))
                    {
                        Model Child = Property.GetValue(Model, null) as Model;
                        if (Child != null)
                        {
                            Children.Add(Child);
                            FindChildModels(Child, Children);
                        }
                    }
                }
            }
        }


    }
}
