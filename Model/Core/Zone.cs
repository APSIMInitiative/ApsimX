using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Xml.Schema;

namespace Model.Core
{


    //=========================================================================
    /// <summary>
    /// A generic system that can have children
    /// </summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Zone : IXmlSerializable, IZone
    {
        protected Zone Parent;

        /// <summary>
        /// Name of the point.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Area of the zone.
        /// </summary>
        [Description("Area of zone (ha)")]
        public double Area { get; set; }

        /// <summary>
        /// A list of child models.
        /// </summary>
        public List<object> Models { get; set; }

        /// <summary>
        /// Add a model to the Models collection and ensure the name is unique.
        /// </summary>
        public void AddModel(object Model)
        {
            Models.Add(Model);
            EnsureNameIsUnique(Model);
        }


        #region XmlSerializable methods
        /// <summary>
        /// Return our schema - needed for IXmlSerializable.
        /// </summary>
        public XmlSchema GetSchema() { return null; }

        /// <summary>
        /// Read XML from specified reader. Called during Deserialisation.
        /// </summary>
        public virtual void ReadXml(XmlReader reader)
        {
            Models = new List<object>();
            reader.Read();
            while (reader.IsStartElement())
            {
                string Type = reader.Name;

                if (Type == "Name")
                {
                    Name = reader.ReadString();
                    reader.Read();
                }
                else if (Type == "Area")
                {
                    Area = Convert.ToDouble(reader.ReadString());
                    reader.Read();
                }
                else
                {
                    object NewChild = Utility.Xml.Deserialise(reader);
                    Models.Add(NewChild);
                }
            }
            reader.ReadEndElement();
            OnSerialised();
        }

        protected void OnSerialised()
        {
            // do nothing.
        }

        /// <summary>
        /// Write this point to the specified XmlWriter
        /// </summary>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Name");
            writer.WriteString(Name);
            writer.WriteEndElement();
            writer.WriteStartElement("Area");
            writer.WriteString(Area.ToString());
            writer.WriteEndElement();

            foreach (object Model in Models)
            {
                Type[] type = Utility.Reflection.GetTypeWithoutNameSpace(Model.GetType().Name);
                if (type.Length == 0)
                    throw new Exception("Cannot find a model with class name: " + Model.GetType().Name);
                if (type.Length > 1)
                    throw new Exception("Found two models with class name: " + Model.GetType().Name);

                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                XmlSerializer serial = new XmlSerializer(type[0]);
                serial.Serialize(writer, Model, ns);
            }
        }

        #endregion
 
        /// <summary>
        /// Return a full path to this system. Does not include the 'Simulation' node.
        /// Format: .PaddockName.ChildName
        /// </summary>
        public string FullPath
        {
            get
            {
                if (this is Simulation)
                    return ".";
                else if (Parent is Simulation)
                    return "." + Name;
                else
                    return Parent.FullPath + "." + Name;
            }
        }

        /// <summary>
        /// Return a model of the specified type that is in scope. Returns null if none found.
        /// </summary>
        public object Find(Type ModelType)
        {
            if (ModelType.IsAssignableFrom(this.GetType()))
                return this;
            foreach (object Child in Models)
            {
                if (ModelType.IsAssignableFrom(Child.GetType()))
                    return Child;
            }

            // If we get this far then search the simulation
            if (Parent == null)
                return null;
            else
                return Parent.Find(ModelType);
        }

        /// <summary>
        /// Return a model with the specified name is in scope. Returns null if none found.
        /// </summary>
        public object Find(string ModelName)
        {
            if (Name == ModelName)
                return this;
            foreach (object Child in Models)
            {
                if (Utility.Reflection.Name(Child) == ModelName)
                    return Child;
            }

            // If we get this far then search the simulation
            if (Parent == null)
                return null;
            else
                return Parent.Find(ModelName);
        }

        /// <summary>
        /// Return a model or variable using the specified NamePath. Returns null if not found.
        /// </summary>
        public virtual object Get(string NamePath)
        {
            object Obj;
            if (NamePath.Length > 0 && NamePath[0] == '.')
                Obj = GetRoot();
            else
                Obj = this;

            string[] NamePathBits = NamePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string PathBit in NamePathBits)
            {
                object LocalObj = null;
                if (Obj is Zone)
                    LocalObj = (Obj as Zone).FindChild(PathBit);

                if (LocalObj != null)
                    Obj = LocalObj;
                else
                {
                    object Value = Utility.Reflection.GetValueOfFieldOrProperty(PathBit, Obj);
                    if (Value == null)
                        return null;
                    else
                        Obj = Value;
                }
            }
            return Obj;
        }

        /// <summary>
        /// Return the root parent (top level zone)
        /// </summary>
        private object GetRoot()
        {
            Zone Root = this;
            while (Root.Parent != null && !(Root.Parent is Simulations))
                Root = Root.Parent;
            return Root;
        }

        /// <summary>
        /// Recursively resolve all [Link] fields. A link must be private. This method will also
        /// go through any public members that are a class or a list of classes. For each one found
        /// it will recursively call this method to resolve links in them.
        /// </summary>
        protected void ResolveLinks(object Obj)
        {
            // Go looking for private [Link]s
            foreach (FieldInfo Field in Utility.Reflection.GetAllFields(Obj.GetType(),
                                                                            BindingFlags.Instance |
                                                                            BindingFlags.NonPublic))
            {
                if (Field.IsDefined(typeof(Link), false))
                {
                    object LinkedObject = Find(Field.FieldType);
                    if (LinkedObject != null)
                        Field.SetValue(Obj, LinkedObject);
                    else
                        throw new Exception("Cannot find a component for [Link]: " + Field.ToString() + " in component: " + Obj.ToString());
                }
            }

            if (Obj is IZone || Obj is ISimulation)
            {
                List<object> Children = new List<object>();
                FindChildModels(Obj, Children);

                foreach (object Child in Children)
                {
                    if (Child is Zone)
                        (Child as Zone).ResolveLinks(Child);
                    else
                        ResolveLinks(Child);
                }
            }
        }

        /// <summary>
        /// Go looking for public, writtable, class properties that are in the "Model" namespace.
        /// For each one found, recursively call this method so that their [Link]s might be resolved.
        /// </summary>
        private void FindChildModels(object Obj, List<object> Children)
        {
            foreach (PropertyInfo Property in Obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (Property.GetType().IsClass && Property.CanWrite)
                {
                    if ( (Property.PropertyType.IsArray && Property.PropertyType.FullName.Contains("Model."))
                        || Property.PropertyType.FullName.Contains("Generic.List"))
                    {
                        // If a field is public and is a class and 
                        object Value = Property.GetValue(Obj, null);
                        if (Value != null)
                        {
                            IList List = (IList)Value;
                            if (List.Count > 0 && List[0].GetType().IsClass)
                                for (int i = 0; i < List.Count; i++)
                                {
                                    if (List[i] is Zone)
                                    {
                                        Children.Add(List[i]);
                                        Zone SubSystem = List[i] as Zone;
                                        SubSystem.Parent = this;
                                    }
                                    else if (List[i].GetType().FullName.Contains("Model"))
                                    {
                                        Children.Add(List[i]);
                                        FindChildModels(List[i], Children);
                                    }
                                }
                        }
                    }
                    else if (Property.PropertyType.IsClass && !Property.PropertyType.IsEnum &&
                             (Property.PropertyType.FullName == "System.Object" || !Property.PropertyType.FullName.Contains("System.")))
                    {
                        object Value = Property.GetValue(Obj, null);
                        if (Value != null)
                        {
                            Children.Add(Value);
                            FindChildModels(Value, Children);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initialise all components by calling their OnInitialise methods.
        /// </summary>
        protected void Initialise(object Obj)
        {
            MethodInfo OnInitialised = Obj.GetType().GetMethod("OnInitialised");
            if (OnInitialised != null)
                OnInitialised.Invoke(Obj, null);

            if (Obj is IZone || Obj is ISimulation)
            {
                List<object> Children = new List<object>();
                FindChildModels(Obj, Children);
                foreach (object Child in Children)
                    Initialise(Child);
            }
        }

        /// <summary>
        /// If the specified model has a settable name property then ensure it has a unique name.
        /// Otherwise don't do anything.
        /// </summary>
        private string EnsureNameIsUnique(object Model)
        {
            string OriginalName = Utility.Reflection.Name(Model);
            if (Utility.Reflection.NameIsSettable(Model))
            {
                string NewName = OriginalName;
                int Counter = 0;
                object Child = FindChild(NewName);
                while (Child != null && Child != Model && Counter < 10000)
                {
                    Counter++;
                    NewName = OriginalName + Counter.ToString();
                    Child = FindChild(NewName);
                }
                if (Counter == 1000)
                    throw new Exception("Cannot create a unique name for model: " + OriginalName);
                Utility.Reflection.SetName(Model, NewName);
                return NewName;
            }
            else
                return OriginalName;
        }

        /// <summary>
        /// Find a child component with the specified name.
        /// </summary>
        private object FindChild(string Name)
        {
            foreach (object Child in Models)
            {
                if (Utility.Reflection.Name(Child) == Name)
                    return Child;
            }
            return null;
        }

    }


}