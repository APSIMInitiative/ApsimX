using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


namespace Models.Core
{
    /// <summary>
    /// Base class for all models in ApsimX.
    /// </summary>
    [Serializable]
    public class Model
    {
        private string _Name = null;


        [NonSerialized]
        private List<DynamicEventSubscriber> eventSubscriptions;

        /// <summary>
        /// Called immediately after the model is XML deserialised.
        /// </summary>
        public virtual void OnLoaded() { }

        /// <summary>
        /// Called just before a simulation commences.
        /// </summary>
        public virtual void OnCommencing() { }

        /// <summary>
        /// Called just after a simulation has completed.
        /// </summary>
        public virtual void OnCompleted() { }

        /// <summary>
        /// Invoked immediately before all simulations begin running.
        /// </summary>
        public virtual void OnAllCommencing() {}

        /// <summary>
        /// Invoked after all simulations finish running.
        /// </summary>
        public virtual void OnAllCompleted() {}

        /// <summary>
        /// Called immediately before deserialising.
        /// </summary>
        public virtual void OnDeserialising(bool xmlSerialisation) { }

        /// <summary>
        /// Called immediately after deserialisation.
        /// </summary>
        public virtual void OnDeserialised(bool xmlSerialisation) { }

        /// <summary>
        /// Called immediately before serialising.
        /// </summary>
        public virtual void OnSerialising(bool xmlSerialisation) { }

        /// <summary>
        /// Called immediately after serialisation.
        /// </summary>
        public virtual void OnSerialised(bool xmlSerialisation) { }

        /// <summary>
        /// Get or set the name of the model
        /// </summary>
        public string Name
        {
            get
            {
                if (_Name == null)
                    return this.GetType().Name;
                else
                    return _Name;
            }
            set
            {
                _Name = value;
            }
        }

        /// <summary>
        /// Get or set the parent of the model.
        /// </summary>
        [XmlIgnore]
        public ModelCollection Parent { get; set; }

        /// <summary>
        /// Is this model hidden in the GUI?
        /// </summary>
        [XmlIgnore]
        public bool HiddenModel { get; set; }

        /// <summary>
        /// Get the model's full path. 
        /// Format: Simulations.SimName.PaddockName.ChildName
        /// </summary>
        public string FullPath
        {
            get
            {
                if (Parent == null)
                    return "." + Name;
                else
                    return Parent.FullPath + "." + Name;
            }
        }

        /// <summary>
        /// Return a model of the specified type that is in scope. Returns null if none found.
        /// </summary>
        public Model Find(Type modelType)
        {
            return Scope.Find(this, modelType);
        }

        /// <summary>
        /// Return a model with the specified name is in scope. Returns null if none found.
        /// </summary>
        public Model Find(string modelNameToFind)
        {
            return Scope.Find(this, modelNameToFind);
        }

        /// <summary>
        /// Return a list of all models in scope. If a Type is specified then only those models
        /// of that type will be returned. Never returns null. May return an empty array. Does not
        /// return models outside of a simulation.
        /// </summary>
        public Model[] FindAll(Type modelType = null)
        {
            return Scope.FindAll(this, modelType); 
        }

        /// <summary>
        /// Return a model or variable using the specified NamePath. Returns null if not found.
        /// </summary>
        public object Get(string namePath)
        {
            Utility.IVariable variable = Variables.Get(this, namePath);
            if (variable == null)
                return null;
            else
                return variable.Value;
        }

        /// <summary>
        /// Set the value of a variable. Will throw if variable doesn't exist.
        /// </summary>
        public void Set(string namePath, object value)
        {
            Utility.IVariable variable = Variables.Get(this, namePath);
            if (variable == null)
                throw new ApsimXException(FullPath, "Cannot set the value of variable '" + namePath + "'. Variable doesn't exist");
            else
                variable.Value = value;
        }

        /// <summary>
        /// Subscribe to an event. Will throw if namePath doesn't point to a event publisher.
        /// </summary>
        public void Subscribe(string namePath, EventHandler handler)
        {
            if (eventSubscriptions == null)
                eventSubscriptions = new List<DynamicEventSubscriber>();
            DynamicEventSubscriber eventSubscription = new DynamicEventSubscriber(namePath, handler, this);
            eventSubscriptions.Add(eventSubscription);
            eventSubscription.Connect(this);
        }

        /// <summary>
        /// Unsubscribe an event. Throws if not found.
        /// </summary>
        public void Unsubscribe(string namePath)
        {
            foreach (DynamicEventSubscriber eventSubscription in eventSubscriptions)
            {
                if (eventSubscription.publishedEventPath == namePath)
                {
                    eventSubscription.Disconnect(this);
                    eventSubscriptions.Remove(eventSubscription);
                    return;
                }
            }

            throw new ApsimXException(FullPath, "Cannot disconnect from event: " + namePath);
        }

        /// <summary>
        /// Write the specified simulation set to the specified 'stream'
        /// </summary>
        public virtual void Write(TextWriter stream)
        {
            stream.Write(Utility.Xml.Serialise(this, true));
        }

        #region Internals


        /// <summary>
        /// Perform a deep Copy of the 'source' model.
        /// </summary>
        public static Model Clone(Model source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
                throw new ApsimXException("", "Trying to clone a null model");

            // Get a list of all child models that we need to notify about the (de)serialisation.
            List<Model> modelsToNotify;
            if (source is ModelCollection)
                modelsToNotify = (source as ModelCollection).AllModels;
            else
                modelsToNotify = new List<Model>();

            // Get rid of source's parent as we don't want to serialise that.
            Models.Core.ModelCollection parent = source.Parent;
            source.Parent = null;

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                foreach (Model model in modelsToNotify)
                    model.OnSerialising(xmlSerialisation:false);

                formatter.Serialize(stream, source);

                foreach (Model model in modelsToNotify)
                    model.OnSerialised(xmlSerialisation: false);
                
                stream.Seek(0, SeekOrigin.Begin);

                foreach (Model model in modelsToNotify)
                    model.OnDeserialising(xmlSerialisation: false);
                Model returnObject = (Model)formatter.Deserialize(stream);
                foreach (Model model in modelsToNotify)
                    model.OnDeserialised(xmlSerialisation: false);

                source.Parent = parent;
                return returnObject;
            }
        }

        /// <summary>
        /// Resolve all [Link] fields in this model.
        /// </summary>
        public static void ResolveLinks(Model model)
        {
            string errorMsg = "";
            //Console.WriteLine(model.FullPath + ":");

            // Go looking for [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(model.GetType(),
                                                                        BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                                                                        BindingFlags.NonPublic | BindingFlags.Public))
            {
                Link link = Utility.Reflection.GetAttribute(field, typeof(Link), false) as Link;
                if (link != null)
                {
                    object linkedObject = null;
                    
                    // NEW SECTION
                    Model[] allMatches;
                    if (link.MustBeChild && model is ModelCollection)
                        allMatches = (model as ModelCollection).AllModelsMatching(field.FieldType).ToArray();
                    else
                        allMatches = model.FindAll(field.FieldType);
                    if (!link.MustBeChild && allMatches.Length == 1)
                        linkedObject = allMatches[0];
                    else if (allMatches.Length > 1 && model.Parent is Factorial.FactorValue)
                    {
                        // Doesn't matter what the link is being connected to if the the model passed
                        // into ResolveLinks is sitting under a FactorValue. It won't be run from
                        // under FactorValue anyway.
                        linkedObject = allMatches[0];
                    }
                    else
                    {
                        // This is primarily for PLANT where matches for things link Functions should
                        // only come from children and not somewhere else in Plant.
                        // e.g. EmergingPhase in potato has an optional link for 'Target'
                        // Potato doesn't have a target child so we don't want to use scoping 
                        // rules to find the target for some other phase.

                        // more that one match so use name to match.
                        foreach (Model matchingModel in allMatches)
                            if (matchingModel.Name == field.Name)
                            {
                                linkedObject = matchingModel;
                                break;
                            }
                        if ((linkedObject == null) && (!link.IsOptional))
                        {
                            errorMsg = string.Format(": Found {0} matches for {1} {2} !", allMatches.Length, field.FieldType.FullName, field.Name);
                        }
                    }

                    if (linkedObject != null)
                    {
                        //if (linkedObject is Model)
                        //    Console.WriteLine("    " + field.Name + " linked to " + (linkedObject as Model).FullPath);

                        field.SetValue(model, linkedObject);
                    }
                    else if (!link.IsOptional)
                        throw new ApsimXException(model.FullPath, "Cannot resolve [Link] '" + field.ToString() +
                                                            "' in class '" + model.FullPath + "'" + errorMsg);
                }
            }
        }

        /// <summary>
        /// Unresolve (set to null) all [Link] fields.
        /// </summary>
        public static void UnresolveLinks(Model model)
        {
            // Go looking for private [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(model.GetType(),
                                                                        BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                                                                        BindingFlags.NonPublic | BindingFlags.Public))
            {
                Link link = Utility.Reflection.GetAttribute(field, typeof(Link), false) as Link;
                if (link != null)
                    field.SetValue(model, null);
            }
        }

        /// <summary>
        /// Call OnCommencing in the specified model and all child models.
        /// </summary>
        protected static void CallOnCommencing(Model model)
        {
            model.OnCommencing();
        }

        /// <summary>
        /// Call OnCompleted in the specified model and all child models.
        /// </summary>
        protected static void CallOnCompleted(Model model)
        {
            model.OnCompleted();
        }

        /// <summary>
        /// Call OnLoaded in the specified model and all child models.
        /// </summary>
        protected static void CallOnLoaded(Model model)
        {
            try
            {
                model.OnLoaded();
            }
            catch (ApsimXException)
            {
            }
        }


        #endregion

        #region Event functions
        private class EventSubscriber
        {
            public Model Model;
            public MethodInfo MethodInfo;


            public virtual Delegate GetDelegate(EventPublisher publisher)
            {
                return Delegate.CreateDelegate(publisher.EventHandlerType, Model, MethodInfo);
            }
        }

        private class DynamicEventSubscriber : EventSubscriber
        {
            public string publishedEventPath;
            public EventHandler subscriber;
            public Model parent;
            public Model matchingModel;
            string ComponentName;
            string EventName;

            public DynamicEventSubscriber(string namePath, EventHandler handler, Model parentModel)
            {
                publishedEventPath = namePath;
                subscriber = handler;
                parent = parentModel;

                ComponentName = Utility.String.ParentName(publishedEventPath, '.');
                if (ComponentName == null)
                    throw new Exception("Invalid syntax for event: " + publishedEventPath);

                EventName = Utility.String.ChildName(publishedEventPath, '.');
            }

            public void Connect(Model model)
            {
                object Component = model.Get(ComponentName);
                if (Component == null)
                    throw new Exception(model.FullPath + " can not find the component: " + ComponentName);
                EventInfo ComponentEvent = Component.GetType().GetEvent(EventName);
                if (ComponentEvent == null)
                    throw new Exception("Cannot find event: " + EventName + " in model: " + ComponentName);

                ComponentEvent.AddEventHandler(Component, subscriber);
            }

            public void Disconnect(Model model)
            {
                object Component = model.Get(ComponentName);
                if (Component != null)
                {
                    EventInfo ComponentEvent = Component.GetType().GetEvent(EventName);
                    if (ComponentEvent != null)
                        ComponentEvent.RemoveEventHandler(Component, subscriber);
                }
            }

            public override Delegate GetDelegate(EventPublisher publisher)
            {
                return subscriber;
            }

            public bool IsMatch(EventPublisher publisher)
            {
                if (matchingModel == null)
                    matchingModel = parent.Get(ComponentName) as Model;
                
                return publisher.Model.FullPath == matchingModel.FullPath && EventName == publisher.Name;
            }
        }


        private class EventPublisher
        {
            public Model Model;
            public EventInfo EventInfo;
            public string Name { get { return EventInfo.Name; } }
            public Type EventHandlerType { get { return EventInfo.EventHandlerType; } }
            public void AddEventHandler(Model model, Delegate eventDelegate)
            {
                EventInfo.AddEventHandler(model, eventDelegate);
            }
        }

        /// <summary>
        /// Connect all event publishers in the specified model.
        /// </summary>
        public static void ConnectEventPublishers(Model model)
        {
            // Go through all events in the specified model and attach them to subscribers.
            foreach (EventPublisher publisher in FindEventPublishers(null, model))
            {
                foreach (EventSubscriber subscriber in FindEventSubscribers(publisher))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = subscriber.GetDelegate(publisher);
                    publisher.AddEventHandler(model, eventdelegate);
                }
            }
        }

        /// <summary>
        /// Connect all event subscribers in the specified model.
        /// </summary>
        /// <param name="model"></param>
        //public static void ConnectEventSubscribers(Model model)
        //{
        //    // Connect all dynamic eventsubscriptions.
        //    foreach (EventSubscription eventSubscription in model.eventSubscriptions)
        //        eventSubscription.Connect(model);

        //    // Go through all subscribers in the specified model and find the event publisher to connect to.
        //    foreach (EventSubscriber subscriber in FindEventSubscribers(null, model))
        //    {
        //        foreach (EventPublisher publisher in FindEventPublishers(subscriber))
        //        {
        //            // connect subscriber to the event.
        //            Delegate eventdelegate = Delegate.CreateDelegate(publisher.EventHandlerType, subscriber.Model, subscriber.MethodInfo);
        //            publisher.AddEventHandler(publisher.Model, eventdelegate);
        //        }
        //    }

        //}

        /// <summary>
        /// Disconnect all published events in all models that are in scope of 'model'
        /// </summary>
        public static void DisconnectEvents(Model model)
        {
            foreach (EventPublisher publisher in FindEventPublishers(null, model))
            {
                FieldInfo eventAsField = publisher.Model.GetType().GetField(publisher.Name, BindingFlags.Instance | BindingFlags.NonPublic);
                Delegate eventDelegate = eventAsField.GetValue(publisher.Model) as Delegate;
                if (eventDelegate != null)
                {
                    foreach (Delegate del in eventDelegate.GetInvocationList())
                    {
                        //if (model == null || del.Target == model)
                            publisher.EventInfo.RemoveEventHandler(publisher.Model, del);
                    }
                }
            }
        }

        //public static void DisconnectSubscriptions(Model model)
        //{
        //    if (model != null)
        //    {
        //        foreach (EventSubscriber subscription in FindEventSubscribers(null, model))
        //        {
        //            foreach (EventPublisher publisher in FindEventPublishers(subscription))
        //            {
        //                FieldInfo eventAsField = publisher.Model.GetType().GetField(publisher.Name, BindingFlags.Instance | BindingFlags.NonPublic);
        //                Delegate eventDelegate = eventAsField.GetValue(publisher.Model) as Delegate;
        //                if (eventDelegate != null)
        //                {
        //                    foreach (Delegate del in eventDelegate.GetInvocationList())
        //                    {
        //                        if (del.Target == model)
        //                            publisher.EventInfo.RemoveEventHandler(publisher.Model, del);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Look through and return all models in scope for event subscribers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        private static List<EventSubscriber> FindEventSubscribers(EventPublisher publisher)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (Model model in GetModelsVisibleToEvents(publisher.Model))
            {
                subscribers.AddRange(FindEventSubscribers(publisher.Name, model));

                // Add dynamic subscriptions if they match
                if (model.eventSubscriptions != null)
                    foreach (DynamicEventSubscriber subscriber in model.eventSubscriptions)
                    {
                        if (subscriber.IsMatch(publisher))
                            subscribers.Add(subscriber);
                    }

            }
            return subscribers;

        }

        private static List<Model> GetModelsVisibleToEvents(Model model)
        {
            List<Model> models = new List<Model>();

            // Find our parent Simulation or Zone.
            Model obj = model;
            while (obj != null && !(obj is Zone) && !(obj is Simulation))
            {
                obj = obj.Parent;
            }
            if (obj == null)
                throw new ApsimXException(model.FullPath, "Cannot find models to connect events to");
            if (obj is Simulation)
            {
                models.AddRange((obj as Simulation).AllModels);
            }
            else
            {
                // return all models in zone and all direct children of zones parent.
                models.AddRange((obj as Zone).AllModels);
                if (obj.Parent != null)
                    models.AddRange(obj.Parent.Models);
            }

            return models;
        }

        /// <summary>
        /// Look through the specified model and return all event subscribers that match the event name. If
        /// eventName is null then all will be returned.
        /// </summary>
        private static List<EventSubscriber> FindEventSubscribers(string eventName, Model model)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (MethodInfo method in model.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                EventSubscribe subscriberAttribute = (EventSubscribe)Utility.Reflection.GetAttribute(method, typeof(EventSubscribe), false);
                if (subscriberAttribute != null && (eventName == null || subscriberAttribute.Name == eventName))
                    subscribers.Add(new EventSubscriber() { MethodInfo = method, Model = model });
            }
            return subscribers;
        }

        ///// <summary>
        ///// Look through and return all models in scope for event publishers with the specified event name.
        ///// If eventName is null then all will be returned.
        ///// </summary>
        //private static List<EventPublisher> FindEventPublishers(EventSubscriber subscriber)
        //{
        //    List<EventPublisher> publishers = new List<EventPublisher>();
        //    foreach (Model model in subscriber.Model.FindAll())
        //        publishers.AddRange(FindEventPublishers(subscriber.Name, model));
        //    return publishers;

        //}

        /// <summary>
        /// Look through the specified model and return all event publishers that match the event name. If
        /// eventName is null then all will be returned.
        /// </summary>
        private static List<EventPublisher> FindEventPublishers(string eventName, Model model)
        {
            List<EventPublisher> publishers = new List<EventPublisher>();
            foreach (EventInfo Event in model.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
            {
                if (eventName == null || Event.Name == eventName)
                    publishers.Add(new EventPublisher() { EventInfo = Event, Model = model });
            }
            return publishers;
        }

        #endregion

    }
}
