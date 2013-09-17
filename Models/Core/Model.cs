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
        /// Allows a model to initialise itself.
        /// </summary>
        public virtual void OnInitialised() { }

        /// <summary>
        /// Return a model of the specified type that is in scope. Returns null if none found.
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
        /// Return a model with the specified name is in scope. Returns null if none found.
        /// </summary>
        public virtual Model Find(string ModelName)
        {
            if (Name == ModelName)
                return this;
            else
                return Parent.Find(ModelName);
        }

        /// <summary>
        /// Return a model or variable using the specified NamePath. Returns null if not found.
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
        /// go through any public members that are a class or a list of classes. For each one found
        /// it will recursively call this method to resolve links in them.
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
        /// Initialise all components by calling their OnInitialise methods.
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
        /// Go looking for public, writtable, class properties that are in the "Model" namespace.
        /// For each one found, recursively call this method so that their [Link]s might be resolved.
        /// </summary>
        private static void FindChildModels(Model Model, List<Model> Children)
        {
            foreach (PropertyInfo Property in Model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (Property.GetType().IsClass && Property.CanWrite)
                {
                    if ((Property.PropertyType.IsArray && Property.PropertyType.FullName.Contains("Model."))
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
                    else if (Property.Name != "Parent" && Property.PropertyType.IsAssignableFrom(typeof(Model)))
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
