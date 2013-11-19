using System.Xml.Serialization;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Reflection;

namespace Models.Core
{


    //=========================================================================
    /// <summary>
    /// A generic system that can have children
    /// </summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Zone : ModelCollection, IXmlSerializable
    {
        /// <summary>
        /// Area of the zone.
        /// </summary>
        [Description("Area of zone (ha)")]
        public double Area { get; set; }

        /// <summary>
        /// A list of child models.
        /// </summary>
        public List<Model> Children { get; set; }

         /// <summary>
        /// Add a model to the Models collection. Will throw if model cannot be added.
        /// </summary>
        public override void AddModel(Model model, bool resolveLinks)
        {
            base.AddModel(model, resolveLinks);
            EnsureNameIsUnique(model);

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
            Children = new List<Model>();
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
                    Model NewChild = Utility.Xml.Deserialise(reader) as Model;
                    AddModel(NewChild, false);
                    NewChild.Parent = this;
                    EnsureNameIsUnique(NewChild);
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

            foreach (object Model in Children)
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
        /// Connect all events up in this simulation
        /// </summary>
        public static void ConnectEvents(Simulation simulation)
        {
            Model[] modelsInScope = simulation.FindAll();

            // Loop through all events in all models: for each one locate all event handlers 9subscribers) and 
            // attach them to the event.
            foreach (Model model in modelsInScope)
            {
                foreach (EventInfo Event in model.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
                {
                    foreach (Utility.ModelFunctions.EventSubscriber subscriber in Utility.ModelFunctions.FindEventSubscribers(Event.Name, modelsInScope))
                    {
                        // connect subscriber to the event.
                        Delegate eventdelegate = Delegate.CreateDelegate(Event.EventHandlerType, subscriber.model, subscriber.handler);
                        Event.AddEventHandler(model, eventdelegate);
                    }
                }


            }
        }

        /// <summary>
        /// Disconnect all events in this simulation
        /// </summary>
        protected static void DisconnectEvents(Simulation simulation)
        {
            Model[] modelsInScope = simulation.FindAll();

            // Loop through all events in all models: for each one locate all event handlers 9subscribers) and 
            // attach them to the event.
            foreach (Model model in modelsInScope)
            {
                foreach (EventInfo Event in model.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
                {
                    //foreach (EventSubscriber subscriber in FindEventSubscribers(Event.Name, modelsInScope))
                    {
                        // disconnect all subscribers from the event.
                        FieldInfo eventAsField = model.GetType().GetField(Event.Name, BindingFlags.Instance | BindingFlags.NonPublic);
                        Delegate eventDelegate = eventAsField.GetValue(model) as Delegate;
                        if (eventDelegate != null)
                        {
                            foreach (Delegate del in eventDelegate.GetInvocationList())
                                Event.RemoveEventHandler(model, del);
                        }
                    }
                }
            }
        }




        /// <summary>
        /// If the specified model has a settable name property then ensure it has a unique name.
        /// Otherwise don't do anything.
        /// </summary>
        private string EnsureNameIsUnique(object Model)
        {
            string OriginalName = Utility.Reflection.Name(Model);
            string NewName = OriginalName;
            int Counter = 0;
            object Child = LocateChild(NewName);
            while (Child != null && Child != Model && Counter < 10000)
            {
                Counter++;
                NewName = OriginalName + Counter.ToString();
                Child = LocateChild(NewName);
            }
            if (Counter == 1000)
                throw new Exception("Cannot create a unique name for model: " + OriginalName);
            Utility.Reflection.SetName(Model, NewName);
            return NewName;
        }


    }


}