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
        /// Gets the full path of the model.
        /// </summary>
        /// <param name="model">The model to get the path of.</param>
        /// <returns>The full path</returns>
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
        /// Gets an array of plant models that are in scope of the specified model.
        /// </summary>
        /// <param name="model">Will look for plant models in scope of 'model'</param>
        /// <returns>The list of plant models in scope</returns>
        public static List<ICrop2> Plants(IModel model)
        {
            List<ICrop2> plants = new List<ICrop2>();
            foreach (ICrop2 plant in FindAll(model, typeof(ICrop2)))
            {
                plants.Add(plant);
            }

            return plants;
        }

        /// <summary>
        /// Return a parent node of the specified type 't'. Will throw if not found.
        /// </summary>
        /// <param name="relativeTo">The model to find a parent for</param>
        /// <param name="typeFilter">The type of parent to return</param>
        /// <returns>The parent of the specified type.</returns>
        public static IModel ParentOfType(IModel relativeTo, Type typeFilter)
        {
            IModel obj = relativeTo;
            while (obj.Parent != null && obj.GetType() != typeFilter)
            {
                obj = obj.Parent;
            }

            if (obj == null)
            {
                throw new ApsimXException(FullPath(relativeTo), "Cannot find a parent of type: " + typeFilter.Name);
            }

            return obj;
        }

        /// <summary>
        /// Gets the value of a variable or model.
        /// </summary>
        /// <param name="relativeTo">The model to use as a starting point when looking for variables in scope.</param>
        /// <param name="namePath">The name of the object to return</param>
        /// <returns>The found object or null if not found</returns>
        public static object Get(IModel relativeTo, string namePath)
        {
            return Locater(relativeTo as Model).Get(namePath, relativeTo as Model);
        }

        /// <summary>
        /// Get the underlying variable object for the given path.
        /// </summary>
        /// <param name="relativeTo">The model to use as a starting point when looking for variables in scope.</param>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        public static IVariable GetVariableObject(IModel relativeTo, string namePath)
        {
            return Locater(relativeTo as Model).GetInternal(namePath, relativeTo as Model);
        }

        /// <summary>
        /// Sets the value of a variable. Will throw if variable doesn't exist.
        /// </summary>
        /// <param name="relativeTo">The model to use as a starting point when looking for variables in scope.</param>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public static void Set(IModel relativeTo, string namePath, object value)
        {
            Locater(relativeTo as Model).Set(namePath, relativeTo as Model, value);
        }

        /// <summary>
        /// Locates and returns a model with the specified name that is in scope.
        /// </summary>
        /// <param name="relativeTo">The model to use as a starting point when looking for models in scope.</param>
        /// <param name="namePath">The name of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public static Model Find(IModel relativeTo, string namePath)
        {
            return Locater(relativeTo as Model).Find(namePath, relativeTo as Model);
        }

        /// <summary>
        /// Locates and returns a model with the specified type that is in scope.
        /// </summary>
        /// <param name="relativeTo">The model to use as a starting point when looking for models in scope.</param>
        /// <param name="type">The type of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public static Model Find(IModel relativeTo, Type type)
        {
            return Locater(relativeTo as Model).Find(type, relativeTo as Model);
        }

        /// <summary>
        /// Locates and returns all models in scope.
        /// </summary>
        /// <param name="relativeTo">The model to use as a starting point when looking for models in scope.</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public static List<IModel> FindAll(IModel relativeTo)
        {
            return new List<IModel>(Locater(relativeTo as Model).FindAll(relativeTo as Model));
        }

        /// <summary>
        /// Locates and returns all models in scope of the specified type.
        /// </summary>
        /// <param name="relativeTo">The model to use as a starting point when looking for models in scope.</param>
        /// <param name="typeFilter">The type of the models to return</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public static List<IModel> FindAll(IModel relativeTo, Type typeFilter)
        {
            return new List<IModel>(Locater(relativeTo as Model).FindAll(typeFilter, relativeTo as Model));
        }

        /// <summary>
        /// Connect this model to the others in the simulation.
        /// </summary>
        /// <param name="model">The model to resolve links in</param>
        public static void ResolveLinks(IModel model)
        {
            Simulation simulation = ParentOfType(model, typeof(Simulation)) as Simulation;
            if (simulation != null)
            {
                if (simulation.IsRunning)
                {
                    // Resolve links in this model.
                    ResolveLinksInternal(model);

                    // Resolve links in other models that point to this model.
                    ResolveExternalLinks(model);
                }
                else
                {
                    ResolveLinksInternal(model);
                }
            }
        }

        /// <summary>
        /// Connect this model to the others in the simulation.
        /// </summary>
        /// <param name="model">The model to un-resolve links in</param>
        public static void UnResolveLinks(IModel model)
        {
            UnresolveLinks(model);
        }

        /// <summary>
        /// Perform a deep Copy of the this model.
        /// </summary>
        /// <param name="model">The model to clone</param>
        /// <returns>The clone of the model</returns>
        public static Model Clone(IModel model)
        {
            // Get a list of all child models that we need to notify about the (de)serialisation.
            List<IModel> modelsToNotify = ChildrenRecursively(model);

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
                    CallEventHandler(modelToNotify, "OnSerialising", args);
                }

                formatter.Serialize(stream, model);

                foreach (IModel modelToNotify in modelsToNotify)
                {
                    CallEventHandler(modelToNotify, "OnSerialised", args);
                }

                stream.Seek(0, SeekOrigin.Begin);

                foreach (IModel modelToNotify in modelsToNotify)
                {
                    CallEventHandler(modelToNotify, "OnDeserialising", args);
                }

                Model returnObject = (Model)formatter.Deserialize(stream);
                foreach (IModel modelToNotify in modelsToNotify)
                {
                    CallEventHandler(modelToNotify, "OnDeserialised", args);
                }

                // Reinstate parent
                model.Parent = parent;

                return returnObject;
            }
        }

        /// <summary>
        /// Serialize the model to a string and return the string.
        /// </summary>
        /// <param name="model">The model to serialize</param>
        /// <returns>The string version of the model</returns>
        public static string Serialise(IModel model)
        {
            // Get a list of all child models that we need to notify about the serialisation.
            List<IModel> modelsToNotify = ChildrenRecursively(model);
            modelsToNotify.Insert(0, model);

            // Let all models know that we're about to serialise.
            object[] args = new object[] { true };
            foreach (Model modelToNotify in modelsToNotify)
            {
                CallEventHandler(modelToNotify, "OnSerialising", args);
            }

            // Do the serialisation
            StringWriter writer = new StringWriter();
            writer.Write(Utility.Xml.Serialise(model, true));

            // Let all models know that we have completed serialisation.
            foreach (Model modelToNotify in modelsToNotify)
            {
                CallEventHandler(modelToNotify, "OnSerialised", args);
            }

            // Set the clipboard text.
            return writer.ToString();
        }

        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <param name="parentModel">The parent model</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursively(IModel parentModel)
        {
            List<IModel> models = new List<IModel>();

            foreach (Model child in parentModel.Models)
            {
                models.Add(child);
                models.AddRange(ChildrenRecursively(child));
            }
            return models;
        }

        /// <summary>
        /// Return a list of all child models recursively. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <param name="parentModel">The parent model</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursivelyVisible(IModel parentModel)
        {
            return ChildrenRecursively(parentModel).FindAll(m => !m.IsHidden);
        }

        /// <summary>
        /// Return a list of all child models recursively. Only models of 
        /// the specified 'typeFilter' will be returned. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <param name="parentModel">The parent model</param>
        /// <param name="typeFilter">The type of children to return</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursivelyMatching(IModel parentModel, Type typeFilter)
        {
            return ChildrenRecursively(parentModel).FindAll(m => m.GetType() == typeFilter);
        }

        /// <summary>
        /// Return a child model that matches the specified 'modelType'. Returns 
        /// null if not found.
        /// </summary>
        /// <param name="parentModel">The parent model</param>
        /// <param name="typeFilter">The type of children to return</param>
        /// <returns>A list of all children</returns>
        public static IModel ChildMatching(IModel parentModel, Type typeFilter)
        {
            return parentModel.Models.Find(m => m.GetType() == typeFilter);
        }

        /// <summary>
        /// Return a child model that matches the specified 'name'. Returns 
        /// null if not found.
        /// </summary>
        /// <param name="parentModel">The parent model</param>
        /// <param name="name">The name of the child to return</param>
        /// <returns>A list of all children</returns>
        public static IModel ChildMatching(IModel parentModel, string name)
        {
            return parentModel.Models.Find(m => m.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Return children that match the specified 'typeFilter'. Never returns 
        /// null. Can return empty List.
        /// </summary>
        /// <param name="parentmodel">The parent model</param>
        /// <param name="typeFilter">The type of children to return</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenMatching(IModel parentmodel, Type typeFilter)
        {
            return parentmodel.Models.FindAll(m => m.GetType() == typeFilter).ToList<IModel>();
        }

        /// <summary>
        /// Add a model to the collection. Will throw if model cannot be added.
        /// </summary>
        /// <param name="parentModel">The parent model</param>
        /// <param name="childModel">The child model to add</param>
        public static void Add(IModel parentModel, IModel childModel)
        {
            EnsureNameIsUnique(childModel);
            (parentModel as Model).Models.Add(childModel as Model);
            Locater(parentModel as Model).Clear();

            // Call the model's (and all children recursively) OnLoaded method
            childModel.Parent = parentModel;
            ParentModelAndAllChildren(childModel);

            Simulation simulation = ParentOfType(parentModel, typeof(Simulation)) as Simulation;
            if (simulation != null && simulation.IsRunning)
            {
                ConnectEvents(childModel);
                ResolveLinks(childModel);
            }
        }

        /// <summary>
        /// Replace the specified 'modelToReplace' with the specified 'newModel'. Return
        /// true if successful.
        /// </summary>
        /// <param name="modelToReplace">The model to remove from the simulation</param>
        /// <param name="newModel">The new model that replaces the one removed</param>
        /// <returns>True if the model was successfully replaced</returns>
        public static bool Replace(IModel modelToReplace, IModel newModel)
        {
            // Find the model.
            int index = (modelToReplace.Parent as Model).Models.IndexOf(modelToReplace as Model);
            if (index != -1)
            {
                IModel oldModel = modelToReplace.Parent.Models[index] as IModel;

                DisconnectEvents(oldModel);
                UnResolveLinks(oldModel);

                // remove the existing model.
                oldModel.Parent.Models.RemoveAt(index);
                oldModel.Parent = null;

                // Name and parent the model we're adding.
                newModel.Name = modelToReplace.Name;
                newModel.Parent = modelToReplace.Parent;
                EnsureNameIsUnique(newModel);

                // insert the new model.
                (modelToReplace.Parent as Model).Models.Insert(index, newModel as Model);

                // clear caches.
                Locater(modelToReplace).Clear();

                // Connect our new child.
                Simulation simulation = ParentOfType(modelToReplace.Parent, typeof(Simulation)) as Simulation;
                if (simulation != null && simulation.IsRunning)
                {
                    ConnectEvents(modelToReplace);
                    ResolveLinks(modelToReplace);
                }

                ParentModelAndAllChildren(modelToReplace);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a model from the Models collection. Returns true if model was removed.
        /// </summary>
        /// <param name="modelToRemove">The model to remove from the simulation</param>
        /// <returns>True if the model was removed</returns>
        public static bool Remove(IModel modelToRemove)
        {
            // Find the model.
            int index = (modelToRemove.Parent as Model).Models.IndexOf(modelToRemove as Model);
            if (index != -1)
            {
                IModel oldModel = modelToRemove.Parent.Models[index];

                // remove the existing model.
                modelToRemove.Parent.Models.RemoveAt(index);

                // clear caches.
                Locater(modelToRemove).Clear();

                DisconnectEvents(oldModel);
                UnresolveLinks(oldModel);

                return true;
            }
            return false;
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
            List<IModel> siblings = Siblings(modelToCheck);
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
        public static List<IModel> Siblings(IModel relativeTo)
        {
            if (relativeTo.Parent == null)
            {
                return new List<IModel>();
            }

            return relativeTo.Parent.Models.FindAll(m => m != relativeTo).ToList<IModel>();
        }

        /// <summary>
        /// Parent all children of 'model' correctly and call their OnLoaded.
        /// </summary>
        /// <param name="model">The model to parent</param>
        private static void ParentModelAndAllChildren(IModel model)
        {
            CallEventHandler(model, "Loaded", null);

            foreach (IModel child in model.Models)
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
        private static Locater Locater(IModel model)
        {
            Simulation simulation = ParentOfType(model, typeof(Simulation)) as Simulation;
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
        /// <param name="model">The model to look through for links</param>
        /// <param name="linkTypeToMatch">If specified, only look for these types of links</param>
        private static void ResolveLinksInternal(IModel model, Type linkTypeToMatch = null)
        {
            string errorMsg = string.Empty;

            // Go looking for [Link]s
            foreach (FieldInfo field in Utility.Reflection.GetAllFields(
                                                            model.GetType(),
                                                            BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public))
            {
                LinkAttribute link = Utility.Reflection.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;
                if (link != null &&
                    (linkTypeToMatch == null || field.FieldType == linkTypeToMatch))
                {
                    object linkedObject = null;

                    List<IModel> allMatches = FindAll(model, field.FieldType);
                    if (allMatches.Count == 1)
                    {
                        linkedObject = allMatches[0];
                    }
                    else
                    {
                        // more that one match so use name to match.
                        foreach (Model matchingModel in allMatches)
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
                        field.SetValue(model, linkedObject);
                    }
                    else if (!link.IsOptional)
                    {
                        throw new ApsimXException(
                                    FullPath(model),
                                    "Cannot resolve [Link] '" + field.ToString() + "' in class '" + FullPath(model) + "'" + errorMsg);
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
        /// <param name="model">The model to exclude from the search</param>
        private static void ResolveExternalLinks(IModel model)
        {
            foreach (Model externalModel in FindAll(model))
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
        private static void CallEventHandler(IModel model, string eventName, object[] args)
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
        public static void ConnectEvents(IModel model)
        {
            Simulation simulation = ParentOfType(model, typeof(Simulation)) as Simulation;
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
        public static void DisconnectEvents(IModel model)
        {
            DisconnectEventPublishers(model);
            DisconnectEventSubscribers(model);
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
        private static List<Model> GetModelsVisibleToEvents(Model relativeTo)
        {
            // This is different to models in scope unfortunately. Need to rethink this.
            List<Model> models = new List<Model>();

            // Find our parent Simulation or Zone.
            Model obj = relativeTo;
            while (obj != null && !(obj is Zone) && !(obj is Simulation))
            {
                obj = obj.Parent as Model;
            }
            if (obj == null)
                throw new ApsimXException(relativeTo.FullPath, "Cannot find models to connect events to");
            if (obj is Simulation)
            {
                models.AddRange((obj as Simulation).Children.AllRecursively);
            }
            else
            {
                // return all models in zone and all direct children of zones parent.
                models.AddRange((obj as Zone).Children.AllRecursively);
                if (obj.Parent != null)
                    models.AddRange(obj.Parent.Models);
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
            foreach (MethodInfo method in relativeTo.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
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
