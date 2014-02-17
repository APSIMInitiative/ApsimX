using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;


namespace Models.Core
{
    /// <summary>
    /// Base class for all models in ApsimX.
    /// </summary>
    [Serializable]
    public class Model
    {
        private string _Name = null;

        /// <summary>
        /// Locate the parent with the specified type. Returns null if not found.
        /// </summary>
        protected Simulation Simulation
        {
            get
            {
                Model m = this;
                while (m != null && m.Parent != null && !(m is Simulation))
                    m = m.Parent;

                if (m == null || !(m is Simulation))
                    throw new ApsimXException(FullPath, "Cannot find root simulation.");
                return m as Simulation;
            }
        }

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


        #region Internals

        /// <summary>
        /// Resolve all [Link] fields in this model.
        /// </summary>
        public static void ResolveLinks(Model model)
        {
            // Go looking for [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(model.GetType(),
                                                                        BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                                                                        BindingFlags.NonPublic | BindingFlags.Public))
            {
                Link link = Utility.Reflection.GetAttribute(field, typeof(Link), false) as Link;
                if (link != null)
                {
                    object linkedObject = null;
                    if (link.NamePath != null)
                        linkedObject = Scope.Find(model, link.NamePath);
                    else if (model is ModelCollection)
                    {
                        // Try and get a match from a child.
                        ModelCollection modelAsCollection = model as ModelCollection;
                        List<Model> matchingModels = modelAsCollection.AllModelsMatching(field.FieldType);
                        if (matchingModels.Count == 1)
                            linkedObject = matchingModels[0];  // only 1 match of the required type.
                        else
                        {
                            // more that one match so use name to match.
                            foreach (Model matchingModel in matchingModels)
                                if (matchingModel.Name == field.Name)
                                {
                                    linkedObject = matchingModel;
                                    break;
                                }
                        }
                    }
                    if (linkedObject == null)
                    {
                        Model[] allMatches = model.FindAll(field.FieldType);
                        if (allMatches.Length == 1)
                            linkedObject = allMatches[0];
                        else if (allMatches.Length > 1 && model.Parent is Factorial.FactorValue)
                        {
                            // Doesn't matter what the link is being connected to if the the model passed
                            // into ResolveLinks is sitting under a FactorValue. It won't be run from
                            // under FactorValue anyway.
                            linkedObject = allMatches[0];
                        }
                    }

                    if (linkedObject != null)
                        field.SetValue(model, linkedObject);
                    else if (!link.IsOptional)
                        throw new ApsimXException(model.FullPath, "Cannot resolve [Link] '" + field.ToString() +
                                                            "' in class '" + model.FullPath + "'");
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
            public string Name
            {
                get
                {
                    EventSubscribe subscriberAttribute = (EventSubscribe)Utility.Reflection.GetAttribute(MethodInfo, typeof(EventSubscribe), false);
                    return subscriberAttribute.Name;
                }
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
                    Delegate eventdelegate = Delegate.CreateDelegate(publisher.EventHandlerType, subscriber.Model, subscriber.MethodInfo);
                    publisher.AddEventHandler(model, eventdelegate);
                }
            }
        }

        /// <summary>
        /// Connect all event subscribers in the specified model.
        /// </summary>
        /// <param name="model"></param>
        public static void ConnectEventSubscribers(Model model)
        {
            // Go through all subscribers in the specified model and find the event publisher to connect to.
            foreach (EventSubscriber subscriber in FindEventSubscribers(null, model))
            {
                foreach (EventPublisher publisher in FindEventPublishers(subscriber))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = Delegate.CreateDelegate(publisher.EventHandlerType, subscriber.Model, subscriber.MethodInfo);
                    publisher.AddEventHandler(publisher.Model, eventdelegate);
                }
            }

        }

        /// <summary>
        /// Disconnect all events in all models that are in scope of 'model'
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

        /// <summary>
        /// Look through and return all models in scope for event subscribers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        private static List<EventSubscriber> FindEventSubscribers(EventPublisher publisher)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (Model model in publisher.Model.FindAll())
                subscribers.AddRange(FindEventSubscribers(publisher.Name, model));
            return subscribers;

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

        /// <summary>
        /// Look through and return all models in scope for event publishers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        private static List<EventPublisher> FindEventPublishers(EventSubscriber subscriber)
        {
            List<EventPublisher> publishers = new List<EventPublisher>();
            foreach (Model model in subscriber.Model.FindAll())
                publishers.AddRange(FindEventPublishers(subscriber.Name, model));
            return publishers;

        }

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
