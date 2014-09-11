// -----------------------------------------------------------------------
// <copyright file="Apsim.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

    /// <summary>
    /// The API for models to discover other models, get and set variables in
    /// other models and send events and subscribe to events in other models.
    /// </summary>
    public class Apsim
    {
        /// <summary>
        /// The model that this API class is relative to.
        /// </summary>
        private IModel model;

        /// <summary>
        /// Creates an instance of the APSIM API class for the given model.
        /// </summary>
        /// <param name="model">The model to create the instance for</param>
        /// <returns>The API class.</returns>
        public static Apsim Create(IModel model)
        {
            var apsimAPI = new Apsim();
            apsimAPI.model = model;
            return apsimAPI;
        }

                
        /// <summary>
        /// Returns the full path of the specified model.
        /// </summary>
        /// <param name="model">The model to return the full path for</param>
        /// <returns>The path</returns>
        public static string FullPath(IModel model)
        {
            string fullPath = "." + model.Name;
            IModel parent = model.Parent;
            while (parent != null)
            {
                fullPath = fullPath.Insert(0, "." + parent.Name);
                parent = parent.Parent;
            }

            return fullPath;
        }

        /// <summary>
        /// Return a parent node of the specified type 'typeFilter'. Will throw if not found.
        /// </summary>
        /// <param name="model">The model to get the parent for</param>
        /// <param name="typeFilter">The name of the parent model to return</param>
        /// <returns>The parent of the specified type.</returns>
        public static IModel Parent(IModel model, Type typeFilter)
        {
            IModel obj = model;
            while (obj.Parent != null && obj.GetType() != typeFilter)
            {
                obj = obj.Parent as IModel;
            }

            if (obj == null)
            {
                throw new ApsimXException(model, "Cannot find a parent of type: " + typeFilter.Name);
            }

            return obj;
        }
            
        /// <summary>
        /// Gets an array of plant models that are in scope.
        /// </summary>
        public List<ICrop2> Plants
        {
            get
            {
                var plants = new List<ICrop2>();
                foreach (var plant in FindAll(typeof(ICrop2)))
                {
                    plants.Add(plant as ICrop2);
                }

                return plants;
            }
        }

        /// <summary>
        /// Gets the value of a variable or model.
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <returns>The found object or null if not found</returns>
        public object Get(string namePath)
        {
            return Locator().Get(namePath, model as Model);
        }

        /// <summary>
        /// Get the underlying variable object for the given path.
        /// </summary>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        public IVariable GetVariableObject(string namePath)
        {
            return Locator().GetInternal(namePath, model as Model);
        }

        /// <summary>
        /// Sets the value of a variable. Will throw if variable doesn't exist.
        /// </summary>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public void Set(string namePath, object value)
        {
            Locator().Set(namePath, model as Model, value);
        }

        /// <summary>
        /// Locates and returns a model with the specified name that is in scope.
        /// </summary>
        /// <param name="namePath">The name of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public Model Find(string namePath)
        {
            return Locator().Find(namePath, model as Model);
        }

        /// <summary>
        /// Locates and returns a model with the specified type that is in scope.
        /// </summary>
        /// <param name="type">The type of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public Model Find(Type type)
        {
            return Locator().Find(type, model as Model);
        }

        /// <summary>
        /// Locates and returns all models in scope.
        /// </summary>
        /// <returns>The found models or an empty array if not found.</returns>
        public List<IModel> FindAll()
        {
            return new List<IModel>(Locator().FindAll(model as Model));
        }

        /// <summary>
        /// Locates and returns all models in scope of the specified type.
        /// </summary>
        /// <param name="typeFilter">The type of the models to return</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public List<IModel> FindAll(Type typeFilter)
        {
        	return new List<IModel>(Locator().FindAll(typeFilter, model as Model));
        }

        /// <summary>
        /// Connect the model to the others in the simulation.
        /// </summary>
        public void ResolveLinks()
        {
            var simulation = Apsim.Parent(model, typeof(Simulation)) as Simulation;
            if (simulation != null)
            {
                if (simulation.IsRunning)
                {
                    // Resolve links in this model.
                    ResolveLinksInternal(model);

                    // Resolve links in other models that point to this model.
                    ResolveExternalLinks();
                }
                else
                {
                    ResolveLinksInternal(model);
                }
            }
        }

        /// <summary>
        /// Unconnect this model from the others in the simulation.
        /// </summary>
        public void UnResolveLinks()
        {
            UnresolveLinks(model);
        }

        /// <summary>
        /// Perform a deep Copy of the this model.
        /// </summary>
        /// <returns>The clone of the model</returns>
        public static IModel Clone(IModel model)
        {
            // Get a list of all child models that we need to notify about the (de)serialisation.
            List<IModel> modelsToNotify = ChildrenRecursivelyInternal(model);

            // Get rid of our parent temporarily as we don't want to serialise that.
            IModel parent = model.Parent;
            model.Parent = null;

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                object[] args = new object[] { false };
                foreach (IModel modelToNotify in modelsToNotify)
                {
                    CallEventHandler(modelToNotify, "Serialising", args);
                }

                formatter.Serialize(stream, model);

                foreach (IModel modelToNotify in modelsToNotify)
                {
                    CallEventHandler(modelToNotify, "Serialised", args);
                }

                stream.Seek(0, SeekOrigin.Begin);

                foreach (IModel modelToNotify in modelsToNotify)
                {
                    CallEventHandler(modelToNotify, "Deserialising", args);
                }

                IModel returnObject = (IModel)formatter.Deserialize(stream);
                foreach (IModel modelToNotify in modelsToNotify)
                {
                    CallEventHandler(modelToNotify, "Deserialised", args);
                }

                // Reinstate parent
                model.Parent = parent;

                return returnObject;
            }
        }

        /// <summary>
        /// Serialize the model to a string and return the string.
        /// </summary>
        /// <returns>The string version of the model</returns>
        public static string Serialise(IModel model)
        {
            // Get a list of all child models that we need to notify about the serialisation.
            List<IModel> modelsToNotify = ChildrenRecursivelyInternal(model);
            modelsToNotify.Insert(0, model);

            // Let all models know that we're about to serialise.
            object[] args = new object[] { true };
            foreach (Model modelToNotify in modelsToNotify)
            {
                CallEventHandler(modelToNotify, "Serialising", args);
            }

            // Do the serialisation
            StringWriter writer = new StringWriter();
            writer.Write(Utility.Xml.Serialise(model, true));

            // Let all models know that we have completed serialisation.
            foreach (Model modelToNotify in modelsToNotify)
            {
                CallEventHandler(modelToNotify, "Serialised", args);
            }

            // Set the clipboard text.
            return writer.ToString();
        }

        /// <summary>
        /// Return a child model that matches the specified 'modelType'. Returns 
        /// an empty list if not found.
        /// </summary>
        /// <param name="typeFilter">The type of children to return</param>
        /// <returns>A list of all children</returns>
        public static IModel Child(IModel model, Type typeFilter)
        {
            return model.Children.Find(m => typeFilter.IsAssignableFrom(m.GetType()));
        }

        /// <summary>
        /// Return a child model that matches the specified 'name'. Returns 
        /// null if not found.
        /// </summary>
        /// <param name="name">The name of the child to return</param>
        /// <returns>A list of all children</returns>
        public static IModel Child(IModel model, string name)
        {
            return model.Children.Find(m => m.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }
        
        /// <summary>
        /// Return children that match the specified 'typeFilter'. Never returns 
        /// null. Can return empty List.
        /// </summary>
        /// <param name="typeFilter">The type of children to return</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> Children(IModel model, Type typeFilter)
        {
            return model.Children.FindAll(m => typeFilter.IsAssignableFrom(m.GetType())).ToList<IModel>();
        }
        
        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursively(IModel model)
        {
            return ChildrenRecursivelyInternal(model);
        }

        /// <summary>
        /// Return a list of all child models recursively. Only models of 
        /// the specified 'typeFilter' will be returned. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <param name="typeFilter">The type of children to return</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursively(IModel model, Type typeFilter)
        {
            return ChildrenRecursively(model).FindAll(m => typeFilter.IsAssignableFrom(m.GetType()));
        }
        
        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursivelyVisible(IModel model)
        {
            return ChildrenRecursively(model).FindAll(m => !m.IsHidden);
        }

        /// <summary>
        /// Give the specified model a unique name
        /// </summary>
        /// <param name="modelToCheck">The model to check the name of</param>
        public static void EnsureNameIsUnique(IModel modelToCheck)
        {
            string originalName = modelToCheck.Name;
            string newName = originalName;
            int counter = 0;
            List<IModel> siblings = Apsim.Siblings(modelToCheck);
            IModel child = siblings.Find(m => m.Name == newName);
            while (child != null && child != modelToCheck && counter < 10000)
            {
                counter++;
                newName = originalName + counter.ToString();
                child = siblings.Find(m => m.Name == newName);
            }

            if (counter == 1000)
            {
                throw new Exception("Cannot create a unique name for model: " + originalName);
            }

            modelToCheck.Name = newName;
        }

        /// <summary>
        /// Return all siblings of the specified model.
        /// </summary>
        /// <param name="relativeTo">The model for which siblings are to be found</param>
        /// <returns>The found siblings or an empty array if not found.</returns>
        public static List<IModel> Siblings(IModel model)
        {
            if (model != null && model.Parent != null)
            {
                return model.Parent.Children.FindAll(m => m != model).ToList<IModel>();
            }
            else
            {
                return new List<IModel>();
            }
        }

        /// <summary>
        /// Parent all children of 'model' correctly and call their OnLoaded.
        /// </summary>
        /// <param name="model">The model to parent</param>
        public static void ParentModelAndAllChildren(IModel model)
        {
            CallEventHandler(model, "Loaded", null);

            foreach (IModel child in model.Children)
            {
                child.Parent = model;
                ParentModelAndAllChildren(child);
            }
        }

        /// <summary>
        /// Gets the locater model for the specified model.
        /// </summary>
        /// <param name="model">The model to find the locator for</param>
        /// <returns>The an instance of a locater class for the specified model. Never returns null.</returns>
        private Locater Locator()
        {
            var simulation = Apsim.Parent(model, typeof(Simulation)) as Simulation;
            if (simulation == null)
            {
                // Simulation can be null if this model is not under a simulation e.g. DataStore.
                return new Locater();
            }
            else
            {
                return simulation.Locater;
            }
        }

        /// <summary>
        /// Resolve all Link fields in the specified model.
        /// </summary>
        /// <param name="modelToScan">The model to look through for links</param>
        /// <param name="linkTypeToMatch">If specified, only look for these types of links</param>
        private void ResolveLinksInternal(IModel modelToScan, Type linkTypeToMatch = null)
        {
            string errorMsg = string.Empty;

            // Go looking for [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(
                                                            modelToScan.GetType(),
                                                            BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public))
            {
                var link = Utility.Reflection.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;
                if (link != null &&
                    (linkTypeToMatch == null || field.FieldType == linkTypeToMatch))
                {
                    object linkedObject = null;

                    List<IModel> allMatches = FindAll(field.FieldType);
                    if (allMatches.Count == 1)
                    {
                        linkedObject = allMatches[0];
                    }
                    else
                    {
                        // more that one match so use name to match.
                        foreach (IModel matchingModel in allMatches)
                        {
                            if (matchingModel.Name == field.Name)
                            {
                                linkedObject = matchingModel;
                                break;
                            }
                        }

                        if ((linkedObject == null) && (!link.IsOptional))
                        {
                            errorMsg = string.Format(": Found {0} matches for {1} {2} !", allMatches.Count, field.FieldType.FullName, field.Name);
                        }
                    }

                    if (linkedObject != null)
                    {
                        field.SetValue(modelToScan, linkedObject);
                    }
                    else if (!link.IsOptional)
                    {
                        throw new ApsimXException(
                                    modelToScan,
                                    "Cannot resolve [Link] '" + field.ToString() + errorMsg);
                    }
                }
            }
        }

        /// <summary>
        /// Set to null all link fields in the specified model.
        /// </summary>
        /// <param name="model">The model to look through for links</param>
        private static void UnresolveLinks(IModel model)
        {
            // Go looking for private [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(
                                                model.GetType(),
                                                BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public))
            {
                LinkAttribute link = Utility.Reflection.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;
                if (link != null)
                {
                    field.SetValue(model, null);
                }
            }
        }

        /// <summary>
        /// Go through all other models looking for a link to the specified 'model'.
        /// Connect any links found.
        /// </summary>
        private void ResolveExternalLinks()
        {
            foreach (var externalModel in FindAll())
            {
                ResolveLinksInternal(externalModel, typeof(Model));
            }
        }

        /// <summary>
        /// Call the specified event on the specified model.
        /// </summary>
        /// <param name="model">The model to call the event on</param>
        /// <param name="eventName">The name of the event</param>
        /// <param name="args">The event arguments. Can be null</param>
        public static void CallEventHandler(IModel model, string eventName, object[] args)
        {
            foreach (EventSubscriber subscriber in FindEventSubscribers(eventName, model))
            {
                subscriber.MethodInfo.Invoke(model, args);
            }
        }

        /*
        /// <summary>
        /// A private event subscription class.
        /// </summary>
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
        /// <summary>
        /// The model that we are working for.
        /// </summary>
        private Model RelativeTo;

        /// <summary>
        /// A list of all dynamic event subscriptions e.g. from REPORT
        /// </summary>
        [NonSerialized]
        private List<DynamicEventSubscriber> EventSubscriptions;

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Subscribe to an event. Will throw if namePath doesn't point to a event publisher.
        /// </summary>
        public static void Subscribe(string namePath, EventHandler handler)
        {
            if (EventSubscriptions == null)
                EventSubscriptions = new List<DynamicEventSubscriber>();
            DynamicEventSubscriber eventSubscription = new DynamicEventSubscriber(namePath, handler, RelativeTo);
            EventSubscriptions.Add(eventSubscription);
            eventSubscription.Connect(RelativeTo);
        }

        /// <summary>
        /// Unsubscribe an event. Throws if not found.
        /// </summary>
        public void Unsubscribe(string namePath)
        {
            foreach (DynamicEventSubscriber eventSubscription in EventSubscriptions)
            {
                if (eventSubscription.publishedEventPath == namePath)
                {
                    eventSubscription.Disconnect(RelativeTo);
                    EventSubscriptions.Remove(eventSubscription);
                    return;
                }
            }
        }
        */

        /// <summary>
        /// Connect all events. Usually only called by the APSIMX infrastructure.
        /// </summary>
        /// <param name="model">The model to connect events in</param>
        public void ConnectEvents()
        {
            var simulation = Apsim.Parent(model, typeof(Simulation)) as Simulation;
            if (simulation != null)
            {
                if (simulation.IsRunning)
                {
                    // This model is being asked to connect itself AFTER events and links
                    // have already been connected.  We have to go through all event declarations
                    // event handlers, all links in this model and all links other other models
                    // that refer to this model. This will be time consuming.

                    // 1. connect all event declarations.
                    ConnectEventPublishers(model);

                    // 2. connect all event handlers.
                    ConnectEventSubscribers(model);
                }
                else
                {
                    // we can take the quicker approach and simply connect event declarations
                    // (publish) with their event handlers and assume that our event handlers will
                    // be connected by whichever model is publishing that event.
                    ConnectEventPublishers(model);
                }
            }
        }

        /// <summary>
        /// Disconnect all events. Usually only called by the APSIMX infrastructure.
        /// </summary>
        /// <param name="model">The model to disconnect events in</param>
        public void DisconnectEvents()
        {
            DisconnectEventPublishers(model);
            DisconnectEventSubscribers(model);
        }

        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursivelyInternal(IModel model)
        {
            List<IModel> models = new List<IModel>();

            foreach (Model child in model.Children)
            {
                models.Add(child);
                models.AddRange(ChildrenRecursivelyInternal(child));
            }
            return models;
        }

        /// <summary>
        /// Connect all event publishers for this model.
        /// </summary>
        /// <param name="model">The model to scan for event declarations</param>
        private static void ConnectEventPublishers(IModel model)
        {
            // Go through all events in the specified model and attach them to subscribers.
            foreach (EventPublisher publisher in FindEventPublishers(null, model))
            {
                foreach (EventSubscriber subscriber in FindEventSubscribers(publisher))
                {
                    // connect subscriber to the event.
                    Delegate eventdelegate = subscriber.GetDelegate(publisher);
                    publisher.AddEventHandler(model as Model, eventdelegate);
                }
            }
        }

        /// <summary>
        /// Connect all event subscribers for this model.
        /// </summary>
        /// <param name="model">The model to scan for event handlers</param>
        private static void ConnectEventSubscribers(IModel model)
        {
            // Connect all dynamic eventsubscriptions.
            // if (EventSubscriptions != null)
            //    foreach (DynamicEventSubscriber eventSubscription in EventSubscriptions)
            //        eventSubscription.Connect(RelativeTo);

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
        /// Disconnect all published events in the specified 'model'
        /// </summary>
        /// <param name="model">The model to scan looking for event publishers</param>
        private static void DisconnectEventPublishers(IModel model)
        {
            foreach (EventPublisher publisher in FindEventPublishers(null, model))
            {
                FieldInfo eventAsField = FindEventField(publisher);
                Delegate eventDelegate = eventAsField.GetValue(publisher.Model) as Delegate;
                if (eventDelegate != null)
                {
                    foreach (Delegate del in eventDelegate.GetInvocationList())
                    {
                        // if (model == null || del.Target == model)
                        publisher.EventInfo.RemoveEventHandler(publisher.Model, del);
                    }
                }
            }
        }

        /// <summary>
        /// Disconnect all subscribed events in the specified 'model'
        /// </summary>
        /// <param name="model">The model to scan looking for event handlers to disconnect</param>
        private static void DisconnectEventSubscribers(IModel model)
        {
            foreach (EventSubscriber subscription in FindEventSubscribers(null, model))
            {
                foreach (EventPublisher publisher in FindEventPublishers(subscription))
                {
                    FieldInfo eventAsField = publisher.Model.GetType().GetField(publisher.Name, BindingFlags.Instance | BindingFlags.NonPublic);
                    Delegate eventDelegate = eventAsField.GetValue(publisher.Model) as Delegate;
                    if (eventDelegate != null)
                    {
                        foreach (Delegate del in eventDelegate.GetInvocationList())
                        {
                            if (del.Target == model)
                                publisher.EventInfo.RemoveEventHandler(publisher.Model, del);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Locate and return the event backing field for the specified event. Returns
        /// null if not found.
        /// </summary>
        /// <param name="publisher">The event publisher to find an event declaration for</param>
        /// <returns>The event field declaration</returns>
        private static FieldInfo FindEventField(EventPublisher publisher)
        {
            Type t = publisher.Model.GetType();
            FieldInfo eventAsField = t.GetField(publisher.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            while (eventAsField == null && t.BaseType != typeof(object))
            {
                t = t.BaseType;
                eventAsField = t.GetField(publisher.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return eventAsField;
        }

        /// <summary>
        /// Look through and return all models in scope for event subscribers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        /// <param name="publisher">The event publisher to find all event subscriptions for</param>
        /// <returns>The list of event subscriptions</returns>
        private static List<EventSubscriber> FindEventSubscribers(EventPublisher publisher)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (Model model in GetModelsVisibleToEvents(publisher.Model))
            {
                subscribers.AddRange(FindEventSubscribers(publisher.Name, model));

                // Add dynamic subscriptions if they match
                // if (EventSubscriptions != null)
                //    foreach (DynamicEventSubscriber subscriber in EventSubscriptions)
                //    {
                //        if (subscriber.IsMatch(publisher))
                //            subscribers.Add(subscriber);
                //    }
            }

            return subscribers;
        }

        /// <summary>
        /// Return a list of models that are visible for event connecting purposes.
        /// </summary>
        /// <param name="relativeTo">The model to use as a base for looking for all other models in scope</param>
        /// <returns>The list of visible models for event connection</returns>
        public static List<IModel> GetModelsVisibleToEvents(Model relativeTo)
        {
            // This is different to models in scope unfortunately. Need to rethink this.
            List<IModel> models = new List<IModel>();

            // Find our parent Simulation or Zone.
            Model obj = relativeTo;
            while (obj != null && !(obj is Zone) && !(obj is Simulation))
            {
                obj = obj.Parent as Model;
            }
            if (obj == null)
                throw new ApsimXException(relativeTo, "Cannot find models to connect events to");
            if (obj is Simulation)
            {
                models.AddRange(Apsim.ChildrenRecursively(obj));
            }
            else
            {
                // return all models in zone and all direct children of zones parent.
                models.AddRange(Apsim.ChildrenRecursively(obj));
                if (obj.Parent != null)
                    models.AddRange(obj.Parent.Children);
            }

            return models;
        }

        /// <summary>
        /// Look through the specified model and return all event subscribers that match the event name. If
        /// eventName is null then all will be returned.
        /// </summary>
        /// <param name="eventName">The name of the event to look for</param>
        /// <param name="relativeTo">The model to search for event subscribers</param>
        /// <returns>The list of event subscribers found.</returns>
        private static List<EventSubscriber> FindEventSubscribers(string eventName, IModel relativeTo)
        {
            List<EventSubscriber> subscribers = new List<EventSubscriber>();
            foreach (MethodInfo method in relativeTo.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                EventSubscribeAttribute subscriberAttribute = (EventSubscribeAttribute)Utility.Reflection.GetAttribute(method, typeof(EventSubscribeAttribute), false);
                if (subscriberAttribute != null && (eventName == null || subscriberAttribute.ToString() == eventName))
                    subscribers.Add(new EventSubscriber()
                    {
                        Name = subscriberAttribute.ToString(),
                        MethodInfo = method,
                        Model = relativeTo as Model
                    });
            }
            return subscribers;
        }

        /// <summary>
        /// Look through and return all models in scope for event publishers with the specified event name.
        /// If eventName is null then all will be returned.
        /// </summary>
        /// <param name="subscriber">The event subscriber to find publishers for</param>
        /// <returns>The list of matching event publishers</returns>
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
        /// <param name="eventName">The event name to look for</param>
        /// <param name="model">The model to scan for event publishers</param>
        /// <returns>The list of matching event publishers</returns>
        private static List<EventPublisher> FindEventPublishers(string eventName, IModel model)
        {
            List<EventPublisher> publishers = new List<EventPublisher>();
            foreach (EventInfo eventInfo in model.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
            {
                if (eventName == null || eventInfo.Name == eventName)
                    publishers.Add(new EventPublisher() { EventInfo = eventInfo, Model = model as Model });
            }
            return publishers;
        }

        /// <summary>
        /// A wrapper around an event subscriber MethodInfo.
        /// </summary>
        private class EventSubscriber
        {
            /// <summary>
            /// Gets or sets the model instance containing the event hander.
            /// </summary>
            public Model Model { get; set; }

            /// <summary>
            /// Gets or sets the reflection method info for the event handler.
            /// </summary>
            public MethodInfo MethodInfo { get; set; }

            /// <summary>
            /// Gets or sets the name of the event.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Creates and returns a delegate for the event handler.
            /// </summary>
            /// <param name="publisher">The corresponding event publisher</param>
            /// <returns>The delegate. Never returns null.</returns>
            public virtual Delegate GetDelegate(EventPublisher publisher)
            {
                return Delegate.CreateDelegate(publisher.EventHandlerType, Model, MethodInfo);
            }
        }

        /// <summary>
        /// A wrapper around an event publisher EventInfo.
        /// </summary>
        private class EventPublisher
        {
            /// <summary>
            /// Gets or sets the model instance containing the event hander.
            /// </summary>
            public Model Model { get; set; }

            /// <summary>
            /// Gets or sets the reflection event info instance.
            /// </summary>
            public EventInfo EventInfo { get; set; }

            /// <summary>
            /// Gets the name of the event.
            /// </summary>
            public string Name
            {
                get
                {
                    return EventInfo.Name;
                }
            }

            /// <summary>
            /// Gets the event handler type
            /// </summary>
            public Type EventHandlerType
            {
                get
                {
                    return EventInfo.EventHandlerType;
                }
            }

            /// <summary>
            /// Adds an event subscriber to this event.
            /// </summary>
            /// <param name="model">The model instance of the subscriber</param>
            /// <param name="eventDelegate">The delegate of the event subscriber</param>
            public void AddEventHandler(Model model, Delegate eventDelegate)
            {
                EventInfo.AddEventHandler(model, eventDelegate);
            }
        }
    }
}
