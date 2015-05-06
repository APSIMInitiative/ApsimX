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
    using System.Xml;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// The API for models to discover other models, get and set variables in
    /// other models and send events and subscribe to events in other models.
    /// </summary>
    public static class Apsim
    {
        /// <summary>
        /// Gets the value of a variable or model.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="namePath">The name of the object to return</param>
        /// <returns>The found object or null if not found</returns>
        public static object Get(IModel model, string namePath)
        {
            return Locator(model).Get(namePath, model as Model);
        }

        /// <summary>
        /// Get the underlying variable object for the given path.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        public static IVariable GetVariableObject(IModel model, string namePath)
        {
            return Locator(model).GetInternal(namePath, model as Model);
        }

        /// <summary>
        /// Sets the value of a variable. Will throw if variable doesn't exist.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public static void Set(IModel model, string namePath, object value)
        {
            Locator(model).Set(namePath, model as Model, value);
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
        /// Locates and returns a model with the specified name that is in scope.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="namePath">The name of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public static Model Find(IModel model, string namePath)
        {
            return Locator(model).Find(namePath, model as Model);
        }

        /// <summary>
        /// Locates and returns a model with the specified type that is in scope.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="type">The type of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        public static Model Find(IModel model, Type type)
        {
            return Locator(model).Find(type, model as Model);
        }

        /// <summary>
        /// Locates and returns all models in scope.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public static List<IModel> FindAll(IModel model)
        {
            return new List<IModel>(Locator(model).FindAll(model as Model));
        }

        /// <summary>
        /// Locates and returns all models in scope of the specified type.
        /// </summary>
        /// <param name="model">The reference model</param>
        /// <param name="typeFilter">The type of the models to return</param>
        /// <returns>The found models or an empty array if not found.</returns>
        public static List<IModel> FindAll(IModel model, Type typeFilter)
        {
            return new List<IModel>(Locator(model).FindAll(typeFilter, model as Model));
        }

        /// <summary>
        /// Perform a deep Copy of the this model.
        /// </summary>
        /// <param name="model">The model to clone</param>
        /// <returns>The clone of the model</returns>
        public static IModel Clone(IModel model)
        {
            // Get a list of all child models that we need to notify about the (de)serialisation.
            List<IModel> modelsToNotify = ChildrenRecursively(model);
            modelsToNotify.Add(model);

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

        /// <summary>Adds a new model (as specified by the xml node) to the specified parent.</summary>
        /// <param name="parent">The parent to add the model to</param>
        /// <param name="node">The XML representing the new model</param>
        /// <returns>The newly created model.</returns>
        public static IModel Add(IModel parent, XmlNode node)
        {
            IModel modelToAdd = XmlUtilities.Deserialise(node, Assembly.GetExecutingAssembly()) as Model;

            // Get all child models
            List<IModel> modelsToNotify = Apsim.ChildrenRecursively(modelToAdd);

            // Call deserialised in all models.
            object[] args = new object[] { true };
            CallEventHandler(modelToAdd, "Deserialised", args);
            foreach (IModel modelToNotify in modelsToNotify)
                CallEventHandler(modelToNotify, "Deserialised", args);

            // Corrently parent all models.
            modelToAdd.Parent = parent;
            Apsim.ParentAllChildren(modelToAdd);
            parent.Children.Add(modelToAdd as Model);

            // Ensure the model name is valid.
            Apsim.EnsureNameIsUnique(modelToAdd);

            // Call OnLoaded
            Apsim.CallEventHandler(modelToAdd, "Loaded", null);
            foreach (IModel child in modelsToNotify)
                Apsim.CallEventHandler(child, "Loaded", null);

            Locator(parent).Clear();

            return modelToAdd;
        }

        /// <summary>Deletes the specified model.</summary>
        /// <param name="model">The model.</param>
        public static bool Delete(IModel model)
        {
            Locator(model.Parent).Clear();
            return model.Parent.Children.Remove(model as Model);
        }

        /// <summary>Clears the cache</summary>
        public static void ClearCache(IModel model)
        {
            Locator(model as Model).Clear();
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
                CallEventHandler(modelToNotify, "Serialising", args);
            }

            // Do the serialisation
            StringWriter writer = new StringWriter();
            writer.Write(XmlUtilities.Serialise(model, true));

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
        /// <param name="model">The parent model</param>
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
        /// <param name="model">The parent model</param>
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
        /// <param name="model">The parent model</param>
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
        /// <param name="model">The parent model</param>
        /// <returns>A list of all children</returns>
        public static List<IModel> ChildrenRecursively(IModel model)
        {
            List<IModel> models = new List<IModel>();

            foreach (Model child in model.Children)
            {
                models.Add(child);
                models.AddRange(ChildrenRecursively(child));
            }
            return models;
        }

        /// <summary>
        /// Return a list of all child models recursively. Only models of 
        /// the specified 'typeFilter' will be returned. Never returns
        /// null. Can return an empty list.
        /// </summary>
        /// <param name="model">The parent model</param>
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
        /// <param name="model">The parent model</param>
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
            Locator(modelToCheck).Clear();
        }

        /// <summary>
        /// Return all siblings of the specified model.
        /// </summary>
        /// <param name="model">The parent model</param>
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
        /// Parent all children of 'model'.
        /// </summary>
        /// <param name="model">The model to parent</param>
        public static void ParentAllChildren(IModel model)
        {
            foreach (IModel child in model.Children)
            {
                child.Parent = model;
                ParentAllChildren(child);
            }
        }

        /// <summary>
        /// Resolve all Link fields in the specified model.
        /// </summary>
        /// <param name="model">The model to look through for links</param>
        /// <param name="linkTypeToMatch">If specified, only look for these types of links</param>
        public static void ResolveLinks(IModel model, Type linkTypeToMatch = null)
        {
            string errorMsg = string.Empty;

            // Go looking for [Link]s
            foreach (FieldInfo field in ReflectionUtilities.GetAllFields(
                                                            model.GetType(),
                                                            BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public))
            {
                var link = ReflectionUtilities.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;
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
                        // more that one match so use name to match
                        foreach (IModel matchingModel in allMatches)
                        {
                            if (matchingModel.Name == field.Name)
                            {
                                linkedObject = matchingModel;
                                break;
                            }
                        }

                        // If the link isn't optional then choose the closest match.
                        if (linkedObject == null && !link.IsOptional && allMatches.Count > 1)
                        {
                            // Return the first (closest) match.
                            linkedObject = allMatches[0];
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
                                    model,
                                    "Cannot resolve [Link] '" + field.ToString() + errorMsg);
                    }
                }
            }
        }

        /// <summary>
        /// Set to null all link fields in the specified model.
        /// </summary>
        /// <param name="model">The model to look through for links</param>
        public static void UnresolveLinks(IModel model)
        {
            // Go looking for private [Link]s
            foreach (FieldInfo field in ReflectionUtilities.GetAllFields(
                                                model.GetType(),
                                                BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public))
            {
                LinkAttribute link = ReflectionUtilities.GetAttribute(field, typeof(LinkAttribute), false) as LinkAttribute;
                if (link != null)
                {
                    field.SetValue(model, null);
                }
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

        /// <summary>
        /// Subscribe to an event. Will throw if namePath doesn't point to a event publisher.
        /// </summary>
        /// <param name="model">The model containing the handler</param>
        /// <param name="eventNameAndPath">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        public static void Subscribe(IModel model, string eventNameAndPath, EventHandler handler)
        {
            // Get the name of the component and event.
            string componentName = StringUtilities.ParentName(eventNameAndPath, '.');
            if (componentName == null)
                throw new Exception("Invalid syntax for event: " + eventNameAndPath);
            string eventName = StringUtilities.ChildName(eventNameAndPath, '.');

            // Get the component.
            object component = Apsim.Get(model, componentName);
            if (component == null)
                throw new Exception(Apsim.FullPath(model) + " can not find the component: " + componentName);

            // Get the EventInfo for the published event.
            EventInfo componentEvent = component.GetType().GetEvent(eventName);
            if (componentEvent == null)
                throw new Exception("Cannot find event: " + eventName + " in model: " + componentName);

            // Subscribe to the event.
            componentEvent.AddEventHandler(component, handler);
        }

        /// <summary>
        /// Unsubscribe an event. Throws if not found.
        /// </summary>
        /// <param name="model">The model containing the handler</param>
        /// <param name="eventNameAndPath">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        public static void Unsubscribe(IModel model, string eventNameAndPath, EventHandler handler)
        {
            // Get the name of the component and event.
            string componentName = StringUtilities.ParentName(eventNameAndPath, '.');
            if (componentName == null)
                throw new Exception("Invalid syntax for event: " + eventNameAndPath);
            string eventName = StringUtilities.ChildName(eventNameAndPath, '.');

            // Get the component.
            object component = Apsim.Get(model, componentName);
            if (component == null)
                throw new Exception(Apsim.FullPath(model) + " can not find the component: " + componentName);

            // Get the EventInfo for the published event.
            EventInfo componentEvent = component.GetType().GetEvent(eventName);
            if (componentEvent == null)
                throw new Exception("Cannot find event: " + eventName + " in model: " + componentName);

            // Unsubscribe to the event.
            componentEvent.RemoveEventHandler(component, handler);
        }
        
        /// <summary>
        /// Connect all events. Usually only called by the APSIMX infrastructure.
        /// </summary>
        /// <param name="model">The model to connect events in</param>
        public static void ConnectEvents(IModel model)
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
        public static void DisconnectEvents(IModel model)
        {
            DisconnectEventPublishers(model);
            DisconnectEventSubscribers(model);
        }

        /// <summary>
        /// Return a list of all parameters (that are not references to child models). Never returns null. Can
        /// return an empty array. A parameter is a class property that is public and read/write
        /// </summary>
        /// <param name="model">The model to search</param>
        /// <param name="flags">The reflection tags to use in the search</param>
        /// <returns>The array of variables.</returns>
        public static IVariable[] FieldsAndProperties(object model, BindingFlags flags)
        {
            List<IVariable> allProperties = new List<IVariable>();
            foreach (PropertyInfo property in model.GetType().UnderlyingSystemType.GetProperties(flags))
            {
                if (property.CanRead)
                    allProperties.Add(new VariableProperty(model, property));
            }
            foreach (FieldInfo field in model.GetType().UnderlyingSystemType.GetFields(flags))
                allProperties.Add(new VariableField(model, field));
            return allProperties.ToArray();
        }

        /// <summary>
        /// Gets the locater model for the specified model.
        /// </summary>
        /// <param name="model">The model to find the locator for</param>
        /// <returns>The an instance of a locater class for the specified model. Never returns null.</returns>
        private static Locater Locator(IModel model)
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
        private static List<IModel> GetModelsVisibleToEvents(Model relativeTo)
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
            foreach (MethodInfo method in relativeTo.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                EventSubscribeAttribute subscriberAttribute = (EventSubscribeAttribute)ReflectionUtilities.GetAttribute(method, typeof(EventSubscribeAttribute), false);
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
            foreach (Model model in Apsim.FindAll(subscriber.Model))
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
